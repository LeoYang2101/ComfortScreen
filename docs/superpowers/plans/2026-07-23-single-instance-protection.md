# ComfortScreen Single-Instance Protection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ensure that only one ComfortScreen process initializes services per Windows session, while repeated interactive launches activate the existing window and repeated `--startup` launches exit silently.

**Architecture:** Add an internal `SingleInstanceCoordinator` that elects the primary process with a `Local\` named mutex held by a dedicated owner thread and receives one-line `Activate` commands through a current-user-only named pipe. `App` performs this gate before `base.OnStartup` or host creation, queues early activation on the WPF dispatcher, and delegates all window restoration to one reusable `ShellWindow.RestoreAndActivate()` method.

**Tech Stack:** .NET 8, WPF, `System.Threading.Mutex`, `System.IO.Pipes`, xUnit, Microsoft.NET.Test.Sdk

---

## File map

- Create `Infrastructure/SingleInstanceCoordinator.cs`: mutex ownership, pipe client/server, activation event, bounded notification retry, and deterministic disposal.
- Create `ComfortScreen.Tests/ComfortScreen.Tests.csproj`: Windows-targeted xUnit test project referencing the WPF application.
- Create `ComfortScreen.Tests/SingleInstanceCoordinatorTests.cs`: real mutex/pipe tests using unique names.
- Modify `ComfortScreen.csproj`: exclude nested test source files from the WPF project's default compile glob.
- Modify `AssemblyInfo.cs`: expose internal coordinator types to the test assembly.
- Modify `App.xaml.cs`: perform the gate before host initialization, dispatch activation, coalesce early requests, and dispose coordination state.
- Modify `Views/ShellWindow.xaml.cs`: provide the shared restore/foreground operation used by tray and IPC activation.

### Task 1: Add the test project and write the first failing coordinator tests

**Files:**
- Create: `ComfortScreen.Tests/ComfortScreen.Tests.csproj`
- Create: `ComfortScreen.Tests/SingleInstanceCoordinatorTests.cs`
- Modify: `ComfortScreen.csproj`
- Modify: `AssemblyInfo.cs`

- [ ] **Step 1: Exclude nested test sources from the application project**

Add this item group to `ComfortScreen.csproj` after the package references:

```xml
  <ItemGroup>
    <Compile Remove="ComfortScreen.Tests\**\*.cs" />
  </ItemGroup>
```

- [ ] **Step 2: Expose internal implementation types to the test assembly**

Add the import and attribute to `AssemblyInfo.cs`:

```csharp
using System.Runtime.CompilerServices;
using System.Windows;

[assembly: InternalsVisibleTo("ComfortScreen.Tests")]
```

Keep the existing `ThemeInfo` attribute unchanged.

- [ ] **Step 3: Create the xUnit project**

Create `ComfortScreen.Tests/ComfortScreen.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ComfortScreen.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Write the first two failing tests**

Create `ComfortScreen.Tests/SingleInstanceCoordinatorTests.cs`:

```csharp
using ComfortScreen.Infrastructure;

namespace ComfortScreen.Tests;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public void Start_FirstCoordinatorIsPrimary_SecondCoordinatorIsSecondary()
    {
        var (mutexName, pipeName) = CreateNames();
        using var primary = CreateCoordinator(mutexName, pipeName);
        using var secondary = CreateCoordinator(mutexName, pipeName);

        Assert.Equal(SingleInstanceStartResult.Primary, primary.Start(notifyPrimary: false));
        Assert.Equal(SingleInstanceStartResult.Secondary, secondary.Start(notifyPrimary: false));
    }

    [Fact]
    public void Dispose_PrimaryReleasesOwnership_ForLaterCoordinator()
    {
        var (mutexName, pipeName) = CreateNames();
        var primary = CreateCoordinator(mutexName, pipeName);
        Assert.Equal(SingleInstanceStartResult.Primary, primary.Start(notifyPrimary: false));

        primary.Dispose();

        using var later = CreateCoordinator(mutexName, pipeName);
        Assert.Equal(SingleInstanceStartResult.Primary, later.Start(notifyPrimary: false));
    }

    private static SingleInstanceCoordinator CreateCoordinator(
        string mutexName,
        string pipeName,
        TimeSpan? notificationTimeout = null)
    {
        return new SingleInstanceCoordinator(
            mutexName,
            pipeName,
            logException: _ => { },
            notificationTimeout ?? TimeSpan.FromMilliseconds(500));
    }

    private static (string MutexName, string PipeName) CreateNames()
    {
        var id = Guid.NewGuid().ToString("N");
        return ($@"Local\ComfortScreen.Tests.{id}", $"ComfortScreen.Tests.{id}");
    }
}
```

- [ ] **Step 5: Run the tests and verify RED**

Run:

```powershell
dotnet test .\ComfortScreen.Tests\ComfortScreen.Tests.csproj --configuration Debug
```

Expected: build fails with `CS0234` or `CS0246` because `SingleInstanceCoordinator` and `SingleInstanceStartResult` do not exist yet. This is the intended RED state.

### Task 2: Implement mutex election and ownership release

**Files:**
- Create: `Infrastructure/SingleInstanceCoordinator.cs`
- Test: `ComfortScreen.Tests/SingleInstanceCoordinatorTests.cs`

- [ ] **Step 1: Add the minimal mutex-only implementation**

Create `Infrastructure/SingleInstanceCoordinator.cs` with the following initial implementation:

```csharp
using System.Diagnostics;

namespace ComfortScreen.Infrastructure;

internal enum SingleInstanceStartResult
{
    Primary,
    Secondary
}

internal sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly string _mutexName;
    private readonly string _pipeName;
    private readonly Action<Exception> _logException;
    private readonly TimeSpan _notificationTimeout;
    private readonly ManualResetEventSlim _releaseMutex = new(false);
    private Thread? _mutexThread;
    private int _started;
    private int _disposed;

    internal SingleInstanceCoordinator(
        string mutexName,
        string pipeName,
        Action<Exception>? logException = null,
        TimeSpan? notificationTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mutexName);
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);

        _mutexName = mutexName;
        _pipeName = pipeName;
        _logException = logException ?? (_ => { });
        _notificationTimeout = notificationTimeout ?? TimeSpan.FromMilliseconds(500);
    }

    internal event EventHandler? ActivationRequested;

    internal SingleInstanceStartResult Start(bool notifyPrimary)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            throw new InvalidOperationException("The single-instance coordinator has already been started.");
        }

        _ = notifyPrimary;
        _ = _pipeName;
        _ = _logException;
        _ = _notificationTimeout;
        _ = ActivationRequested;

        return AcquireMutexOwnership()
            ? SingleInstanceStartResult.Primary
            : SingleInstanceStartResult.Secondary;
    }

    private bool AcquireMutexOwnership()
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _mutexThread = new Thread(() => RunMutexOwner(completion))
        {
            IsBackground = true,
            Name = "ComfortScreen single-instance mutex owner"
        };
        _mutexThread.Start();
        return completion.Task.GetAwaiter().GetResult();
    }

    private void RunMutexOwner(TaskCompletionSource<bool> completion)
    {
        Mutex? mutex = null;
        var ownsMutex = false;

        try
        {
            mutex = new Mutex(initiallyOwned: false, _mutexName);
            try
            {
                ownsMutex = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                ownsMutex = true;
            }

            completion.TrySetResult(ownsMutex);
            if (ownsMutex)
            {
                _releaseMutex.Wait();
                mutex.ReleaseMutex();
            }
        }
        catch (Exception ex)
        {
            if (!completion.TrySetException(ex))
            {
                _logException(ex);
            }
        }
        finally
        {
            mutex?.Dispose();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _releaseMutex.Set();
        _mutexThread?.Join();
        _releaseMutex.Dispose();
    }
}
```

The dedicated owner thread is intentional: Windows mutex ownership is thread-affine, and it prevents same-process tests from being misclassified as reentrant primary owners while still allowing abandoned mutex recovery.

- [ ] **Step 2: Run the coordinator tests and verify GREEN**

Run:

```powershell
dotnet test .\ComfortScreen.Tests\ComfortScreen.Tests.csproj --configuration Debug
```

Expected: 2 tests pass, 0 fail.

- [ ] **Step 3: Commit the now-green test scaffold and mutex election**

```powershell
git add ComfortScreen.csproj AssemblyInfo.cs ComfortScreen.Tests Infrastructure\SingleInstanceCoordinator.cs
git commit -m "feat: add session mutex ownership"
```

### Task 3: Add activation IPC, bounded failure handling, and clean listener disposal

**Files:**
- Modify: `ComfortScreen.Tests/SingleInstanceCoordinatorTests.cs`
- Modify: `Infrastructure/SingleInstanceCoordinator.cs`

- [ ] **Step 1: Add the four remaining coordinator tests**

Insert these tests before the helper methods in `SingleInstanceCoordinatorTests.cs`:

```csharp
    [Fact]
    public void Start_InteractiveSecondary_SendsExactlyOneActivation()
    {
        var (mutexName, pipeName) = CreateNames();
        using var activationReceived = new ManualResetEventSlim(false);
        var activationCount = 0;
        using var primary = CreateCoordinator(mutexName, pipeName);
        primary.ActivationRequested += (_, _) =>
        {
            Interlocked.Increment(ref activationCount);
            activationReceived.Set();
        };
        Assert.Equal(SingleInstanceStartResult.Primary, primary.Start(notifyPrimary: false));

        using var secondary = CreateCoordinator(mutexName, pipeName);
        Assert.Equal(SingleInstanceStartResult.Secondary, secondary.Start(notifyPrimary: true));

        Assert.True(activationReceived.Wait(TimeSpan.FromSeconds(2)));
        Assert.False(SpinWait.SpinUntil(
            () => Volatile.Read(ref activationCount) > 1,
            TimeSpan.FromMilliseconds(200)));
        Assert.Equal(1, Volatile.Read(ref activationCount));
    }

    [Fact]
    public void Start_StartupSecondary_DoesNotSendActivation()
    {
        var (mutexName, pipeName) = CreateNames();
        using var activationReceived = new ManualResetEventSlim(false);
        using var primary = CreateCoordinator(mutexName, pipeName);
        primary.ActivationRequested += (_, _) => activationReceived.Set();
        Assert.Equal(SingleInstanceStartResult.Primary, primary.Start(notifyPrimary: false));

        using var secondary = CreateCoordinator(mutexName, pipeName);
        Assert.Equal(SingleInstanceStartResult.Secondary, secondary.Start(notifyPrimary: false));

        Assert.False(activationReceived.Wait(TimeSpan.FromMilliseconds(300)));
    }

    [Fact]
    public void Start_WhenPrimaryPipeIsMissing_ReturnsSecondaryWithinBoundedTime()
    {
        var (mutexName, primaryPipeName) = CreateNames();
        using var primary = CreateCoordinator(mutexName, primaryPipeName);
        Assert.Equal(SingleInstanceStartResult.Primary, primary.Start(notifyPrimary: false));

        var missingPipeName = $"ComfortScreen.Tests.Missing.{Guid.NewGuid():N}";
        using var secondary = CreateCoordinator(
            mutexName,
            missingPipeName,
            TimeSpan.FromMilliseconds(250));
        var stopwatch = Stopwatch.StartNew();

        var result = secondary.Start(notifyPrimary: true);

        stopwatch.Stop();
        Assert.Equal(SingleInstanceStartResult.Secondary, result);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Elapsed: {stopwatch.Elapsed}");
    }

    [Fact]
    public void Dispose_WhileListenerIsWaiting_CompletesCleanly()
    {
        var (mutexName, pipeName) = CreateNames();
        var coordinator = CreateCoordinator(mutexName, pipeName);
        Assert.Equal(SingleInstanceStartResult.Primary, coordinator.Start(notifyPrimary: false));
        var stopwatch = Stopwatch.StartNew();

        var exception = Record.Exception(coordinator.Dispose);

        stopwatch.Stop();
        Assert.Null(exception);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Elapsed: {stopwatch.Elapsed}");
    }
```

Add `using System.Diagnostics;` at the top of the test file.

- [ ] **Step 2: Run the interactive activation test and verify RED**

Run:

```powershell
dotnet test .\ComfortScreen.Tests\ComfortScreen.Tests.csproj --configuration Debug --filter "FullyQualifiedName~Start_InteractiveSecondary_SendsExactlyOneActivation"
```

Expected: FAIL because the mutex-only implementation never raises `ActivationRequested`.

- [ ] **Step 3: Replace the mutex-only coordinator with the complete pipe implementation**

Keep the enum, constructor, mutex owner thread, and disposal idempotence from Task 2, then add:

```csharp
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
```

Add production defaults and state:

```csharp
    private const string ActivationCommand = "Activate";
    private static readonly TimeSpan ListenerRetryDelay = TimeSpan.FromMilliseconds(100);
    private readonly object _pipeSync = new();
    private readonly CancellationTokenSource _listenerCancellation = new();
    private NamedPipeServerStream? _currentServer;
    private Task? _listenerTask;

    internal SingleInstanceCoordinator(Action<Exception>? logException = null)
        : this(
            @"Local\ComfortScreen.SingleInstance",
            $"ComfortScreen.SingleInstance.{Process.GetCurrentProcess().SessionId}",
            logException,
            TimeSpan.FromMilliseconds(500))
    {
    }
```

Replace `Start` with:

```csharp
    internal SingleInstanceStartResult Start(bool notifyPrimary)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            throw new InvalidOperationException("The single-instance coordinator has already been started.");
        }

        if (AcquireMutexOwnership())
        {
            var firstServer = CreatePipeServer();
            if (!RegisterCurrentServer(firstServer))
            {
                throw new ObjectDisposedException(nameof(SingleInstanceCoordinator));
            }

            _listenerTask = ListenAsync(firstServer, _listenerCancellation.Token);
            return SingleInstanceStartResult.Primary;
        }

        if (notifyPrimary)
        {
            TryNotifyPrimary();
        }

        return SingleInstanceStartResult.Secondary;
    }
```

Add the pipe methods:

```csharp
    private NamedPipeServerStream CreatePipeServer()
    {
        return new NamedPipeServerStream(
            _pipeName,
            PipeDirection.In,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
    }

    private bool RegisterCurrentServer(NamedPipeServerStream server)
    {
        lock (_pipeSync)
        {
            if (Volatile.Read(ref _disposed) != 0 || _listenerCancellation.IsCancellationRequested)
            {
                server.Dispose();
                return false;
            }

            _currentServer = server;
            return true;
        }
    }

    private void ClearCurrentServer(NamedPipeServerStream server)
    {
        lock (_pipeSync)
        {
            if (ReferenceEquals(_currentServer, server))
            {
                _currentServer = null;
            }
        }
    }

    private async Task ListenAsync(NamedPipeServerStream firstServer, CancellationToken cancellationToken)
    {
        NamedPipeServerStream? server = firstServer;

        while (server is not null && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                using var reader = new StreamReader(
                    server,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: true,
                    bufferSize: 1024,
                    leaveOpen: true);
                var command = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

                if (string.Equals(command, ActivationCommand, StringComparison.Ordinal))
                {
                    ActivationRequested?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    _logException(new InvalidDataException("Received an invalid single-instance command."));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected during normal shutdown.
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected when disposal closes a waiting pipe server.
            }
            catch (Exception ex)
            {
                _logException(ex);
            }
            finally
            {
                ClearCurrentServer(server);
                server.Dispose();
                server = null;
            }

            while (!cancellationToken.IsCancellationRequested && server is null)
            {
                try
                {
                    var nextServer = CreatePipeServer();
                    if (RegisterCurrentServer(nextServer))
                    {
                        server = nextServer;
                    }
                }
                catch (Exception ex)
                {
                    _logException(ex);
                    try
                    {
                        await Task.Delay(ListenerRetryDelay, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }
    }

    private void TryNotifyPrimary()
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? lastError = null;

        while (stopwatch.Elapsed < _notificationTimeout)
        {
            var remaining = _notificationTimeout - stopwatch.Elapsed;
            var attemptMilliseconds = Math.Clamp((int)remaining.TotalMilliseconds, 1, 100);

            try
            {
                using var client = new NamedPipeClientStream(
                    ".",
                    _pipeName,
                    PipeDirection.Out,
                    PipeOptions.Asynchronous);
                client.Connect(attemptMilliseconds);
                using var writer = new StreamWriter(
                    client,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    bufferSize: 1024,
                    leaveOpen: false)
                {
                    AutoFlush = true
                };
                writer.WriteLine(ActivationCommand);
                return;
            }
            catch (TimeoutException ex)
            {
                lastError = ex;
            }
            catch (IOException ex)
            {
                lastError = ex;
            }
        }

        _logException(lastError ?? new TimeoutException("The primary instance pipe was not available."));
    }
```

Replace `Dispose` with:

```csharp
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _listenerCancellation.Cancel();
        NamedPipeServerStream? server;
        lock (_pipeSync)
        {
            server = _currentServer;
            _currentServer = null;
        }

        server?.Dispose();
        _listenerTask?.GetAwaiter().GetResult();
        _listenerCancellation.Dispose();

        _releaseMutex.Set();
        _mutexThread?.Join();
        _releaseMutex.Dispose();
    }
```

- [ ] **Step 4: Run all coordinator tests and verify GREEN**

Run:

```powershell
dotnet test .\ComfortScreen.Tests\ComfortScreen.Tests.csproj --configuration Debug
```

Expected: 6 tests pass, 0 fail, with no unhandled task exceptions.

- [ ] **Step 5: Commit activation IPC**

```powershell
git add Infrastructure\SingleInstanceCoordinator.cs ComfortScreen.Tests\SingleInstanceCoordinatorTests.cs
git commit -m "feat: notify the primary instance over a named pipe"
```

### Task 4: Gate application startup and reuse window activation

**Files:**
- Modify: `App.xaml.cs`
- Modify: `Views/ShellWindow.xaml.cs`

- [ ] **Step 1: Add the coordinator to application startup state**

Add `using ComfortScreen.Infrastructure;` and these fields to `App`:

```csharp
    private IHost? _host;
    private SingleInstanceCoordinator? _singleInstanceCoordinator;
    private bool _pendingActivation;
```

- [ ] **Step 2: Move the single-instance gate before WPF and host initialization**

At the start of the existing `try` block in `OnStartup`, before `base.OnStartup(e)`, use:

```csharp
            var launchContext = CreateLaunchContext(e.Args);
            _singleInstanceCoordinator = new SingleInstanceCoordinator(LogException);
            _singleInstanceCoordinator.ActivationRequested += OnActivationRequested;

            var instanceResult = _singleInstanceCoordinator.Start(
                notifyPrimary: !launchContext.IsStartupLaunch);
            if (instanceResult == SingleInstanceStartResult.Secondary)
            {
                Shutdown(0);
                return;
            }

            base.OnStartup(e);
```

Remove the old later declaration of `launchContext`. Do not create the host, resolve services, initialize the tray, start timers, register hotkeys, or create overlays before this block returns `Primary`.

- [ ] **Step 3: Dispatch and coalesce activation requests**

Add this method to `App`:

```csharp
    private void OnActivationRequested(object? sender, EventArgs e)
    {
        if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
        {
            return;
        }

        Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() =>
            {
                if (MainWindow is ShellWindow window)
                {
                    window.RestoreAndActivate();
                    return;
                }

                _pendingActivation = true;
            }));
    }
```

Immediately after `window.InitializeShell()`, consume one early request:

```csharp
            if (_pendingActivation)
            {
                _pendingActivation = false;
                window.RestoreAndActivate();
            }
```

Keep the existing first-launch visibility rule: interactive primary launches call `window.Show()`, while startup primary launches stay hidden. Place the pending activation block after that visibility rule so an interactive secondary arriving during startup can show a startup-launched primary.

- [ ] **Step 4: Dispose the coordinator after host shutdown**

In `OnExit`, preserve the host stop/dispose behavior and add coordinator cleanup in a nested `finally` so mutex release is guaranteed:

```csharp
        finally
        {
            try
            {
                _host?.Dispose();
            }
            finally
            {
                _singleInstanceCoordinator?.Dispose();
            }
        }
```

- [ ] **Step 5: Make shell restoration reusable and foreground-capable**

In `ShellWindow.xaml.cs`, change the controller subscription:

```csharp
        _controller.RestoreRequested += (_, _) => RestoreAndActivate();
```

Replace the private `RestoreFromTray` method with:

```csharp
    public void RestoreAndActivate()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Focus();
        Topmost = true;
        Topmost = false;
    }
```

- [ ] **Step 6: Build and run coordinator tests**

Run:

```powershell
dotnet test .\ComfortScreen.Tests\ComfortScreen.Tests.csproj --configuration Debug
dotnet build .\ComfortScreen.csproj --configuration Release
```

Expected: all 6 tests pass; Release build succeeds with 0 errors and 0 warnings. The build is the automated integration check for WPF API signatures and project compile globs; foreground behavior is verified in Task 5 because it depends on the live Windows shell.

- [ ] **Step 7: Commit application integration**

```powershell
git add App.xaml.cs Views\ShellWindow.xaml.cs
git commit -m "feat: enforce single-instance startup"
```

### Task 5: Verify the real Windows startup and activation behavior

**Files:**
- Verify only; do not modify dependency versions.

- [ ] **Step 1: Publish or locate the Release executable**

Run:

```powershell
dotnet build .\ComfortScreen.csproj --configuration Release
$comfortScreenExe = (Resolve-Path '.\bin\Release\net8.0-windows\ComfortScreen.exe').Path
```

Expected: the executable path resolves successfully.

- [ ] **Step 2: Start one hidden primary instance**

Run:

```powershell
$primary = Start-Process -FilePath $comfortScreenExe -ArgumentList '--startup' -PassThru
Start-Sleep -Seconds 2
@(Get-Process -Name ComfortScreen -ErrorAction SilentlyContinue).Count
```

Expected: process count is `1`; no main window is shown; one tray icon exists.

- [ ] **Step 3: Start an interactive secondary and confirm activation without duplication**

Run:

```powershell
$secondary = Start-Process -FilePath $comfortScreenExe -PassThru
$secondary.WaitForExit(3000)
@{
    SecondaryExited = $secondary.HasExited
    SecondaryExitCode = if ($secondary.HasExited) { $secondary.ExitCode } else { $null }
    ProcessCount = @(Get-Process -Name ComfortScreen -ErrorAction SilentlyContinue).Count
}
```

Expected: `SecondaryExited=True`, `SecondaryExitCode=0`, `ProcessCount=1`, and the existing main window is visible and foregrounded.

- [ ] **Step 4: Repeat while the window is visible**

Run the same secondary-launch block again.

Expected: the same window is raised, process count remains `1`, and no additional tray icon appears.

- [ ] **Step 5: Verify duplicate-sensitive services and persistence**

With the primary still running:

1. Enable the eye filter and confirm there is one overlay layer rather than stacked opacity.
2. Trigger the configured global hotkey and confirm it remains registered.
3. Confirm only one reminder is produced for a single interval.
4. Validate the persisted settings file:

```powershell
$settingsPath = Join-Path $env:LOCALAPPDATA 'ComfortScreen\settings.json'
Get-Content -Raw -LiteralPath $settingsPath | ConvertFrom-Json | Out-Null
```

Expected: JSON parsing succeeds.

- [ ] **Step 6: Close the verification instance through the tray Exit command**

Expected: the ComfortScreen process exits and the tray icon disappears. Do not force-kill it unless normal exit fails.

### Task 6: Final verification and branch handoff

**Files:**
- Review all files changed by Tasks 1-4.

- [ ] **Step 1: Run fresh full verification**

Run:

```powershell
dotnet test .\ComfortScreen.Tests\ComfortScreen.Tests.csproj --configuration Release
dotnet build .\ComfortScreen.csproj --configuration Release --no-restore
git diff --check
git status --short
```

Expected: 6 tests pass; build succeeds with 0 errors and 0 warnings; `git diff --check` prints nothing; status contains only intentional changes or is clean after commits.

- [ ] **Step 2: Review the diff against the approved design**

Run:

```powershell
git diff origin/master...HEAD -- ComfortScreen.csproj AssemblyInfo.cs Infrastructure\SingleInstanceCoordinator.cs App.xaml.cs Views\ShellWindow.xaml.cs ComfortScreen.Tests
```

Confirm all of the following:

- The gate occurs before `base.OnStartup`, host construction, tray/timer/hotkey/overlay initialization.
- Every secondary returns from startup even if named-pipe notification fails.
- `--startup` secondary launches never send `Activate`.
- The production pipe name includes the current session id and uses `PipeOptions.CurrentUserOnly`.
- Early activation coalesces and runs on the WPF dispatcher.
- Listener continuations use `ConfigureAwait(false)` and disposal releases pipe and mutex resources.
- Dependency versions remain unchanged.

- [ ] **Step 3: Use the finishing-development-branch workflow**

Load `superpowers:finishing-a-development-branch`, present its integration options, and only merge, push, or clean up after the user chooses. Preserve the repository identity `LeoYang2101 <LeoYang210@163.com>` for every commit.
