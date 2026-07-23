using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace ComfortScreen.Infrastructure;

internal enum SingleInstanceStartResult
{
    Primary,
    Secondary
}

internal sealed class SingleInstanceCoordinator : IDisposable
{
    private const string ActivationCommand = "Activate";
    private static readonly TimeSpan ListenerRetryDelay = TimeSpan.FromMilliseconds(100);

    private readonly string _mutexName;
    private readonly string _pipeName;
    private readonly Action<Exception> _logException;
    private readonly TimeSpan _notificationTimeout;
    private readonly ManualResetEventSlim _releaseMutex = new(false);
    private readonly object _pipeSync = new();
    private readonly CancellationTokenSource _listenerCancellation = new();

    private Thread? _mutexThread;
    private NamedPipeServerStream? _currentServer;
    private Task? _listenerTask;
    private int _started;
    private int _disposed;

    internal SingleInstanceCoordinator(Action<Exception>? logException = null)
        : this(
            @"Local\ComfortScreen.SingleInstance",
            $"ComfortScreen.SingleInstance.{Process.GetCurrentProcess().SessionId}",
            logException,
            TimeSpan.FromMilliseconds(500))
    {
    }

    internal SingleInstanceCoordinator(
        string mutexName,
        string pipeName,
        Action<Exception>? logException = null,
        TimeSpan? notificationTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mutexName);
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);

        var effectiveNotificationTimeout = notificationTimeout ?? TimeSpan.FromMilliseconds(500);
        if (effectiveNotificationTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(notificationTimeout),
                "The notification timeout must be greater than zero.");
        }

        _mutexName = mutexName;
        _pipeName = pipeName;
        _logException = logException ?? (_ => { });
        _notificationTimeout = effectiveNotificationTimeout;
    }

    internal event EventHandler? ActivationRequested;

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
}
