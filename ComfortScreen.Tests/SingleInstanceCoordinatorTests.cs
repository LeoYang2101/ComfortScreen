using System.Diagnostics;
using ComfortScreen.Infrastructure;
using Xunit;

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
