namespace ComfortScreen.Models;

public enum AppLaunchMode
{
    Interactive,
    Startup
}

public sealed class StartupLaunchContext
{
    public StartupLaunchContext(AppLaunchMode launchMode)
    {
        LaunchMode = launchMode;
    }

    public AppLaunchMode LaunchMode { get; }

    public bool IsStartupLaunch => LaunchMode == AppLaunchMode.Startup;
}
