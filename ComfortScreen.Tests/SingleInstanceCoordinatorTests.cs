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
