using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using ComfortScreen.Contracts;
using ComfortScreen.Models;
using ComfortScreen.Services;
using ComfortScreen.ViewModels;
using ComfortScreen.Views;
using ComfortScreen.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ComfortScreen;

public partial class App : System.Windows.Application
{
    private static readonly string CrashLogPath = Path.Combine(AppContext.BaseDirectory, "startup-error.log");
    private static readonly string StartupArgument = "--startup";
    private IHost? _host;

    public static T GetService<T>()
        where T : notnull
    {
        if (Current is not App app)
        {
            throw new InvalidOperationException("Application instance is not available.");
        }

        if (app._host is null)
        {
            throw new InvalidOperationException("Host services are not available.");
        }

        return app._host.Services.GetRequiredService<T>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            LogException(args.ExceptionObject as Exception ?? new Exception("Unknown unhandled exception"));
        };

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            base.OnStartup(e);
            var launchContext = CreateLaunchContext(e.Args);

            _host = CreateHostBuilder(launchContext).Build();
            _host.Start();

            BootstrapThemeResources();

            var window = GetService<ShellWindow>();
            MainWindow = window;
            window.InitializeShell();

            if (!launchContext.IsStartupLaunch)
            {
                window.Show();
            }
        }
        catch (Exception ex)
        {
            LogException(ex);
            ShowAppDialog("ComfortScreen", "应用启动失败，错误日志已写入 startup-error.log。", MessageBoxButton.OK, AppDialogKind.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _host?.StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
        }
        catch
        {
            // Intentionally ignore shutdown errors.
        }
        finally
        {
            _host?.Dispose();
        }

        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception);
        e.Handled = true;
        ShowAppDialog("ComfortScreen", "运行时出现异常，错误日志已写入 startup-error.log。", MessageBoxButton.OK, AppDialogKind.Error);
    }

    private static void LogException(Exception ex)
    {
        try
        {
            var content = new StringBuilder()
                .AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().FullName}")
                .AppendLine(ex.Message)
                .AppendLine(ex.StackTrace)
                .AppendLine()
                .ToString();

            File.AppendAllText(CrashLogPath, content, Encoding.UTF8);
        }
        catch
        {
            // Intentionally swallow logging failures.
        }
    }

    private void BootstrapThemeResources()
    {
        var controller = GetService<ComfortScreenController>();
        var themeService = GetService<IThemeService>();

        // Seed mutable theme brushes before any real window is created so DynamicResource
        // bindings resolve to the updatable application-level instances from the start.
        themeService.ApplyTheme(controller.Settings.ThemeMode, new Window());
    }

    private void ShowAppDialog(string title, string message, MessageBoxButton buttons, AppDialogKind kind)
    {
        try
        {
            if (_host is not null)
            {
                var owner = MainWindow is { IsLoaded: true, IsVisible: true } ? MainWindow : null;
                GetService<IAppDialogService>().Show(title, message, buttons, kind, owner);
                return;
            }
        }
        catch
        {
            // Fall back to a directly created themed window if the host is not fully available.
        }

        var window = new ThemedMessageWindow(title, message, buttons, kind)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        window.ShowDialog();
    }

    private static StartupLaunchContext CreateLaunchContext(IReadOnlyList<string> args)
    {
        var launchMode = args.Any(arg => string.Equals(arg, StartupArgument, StringComparison.OrdinalIgnoreCase))
            ? AppLaunchMode.Startup
            : AppLaunchMode.Interactive;

        return new StartupLaunchContext(launchMode);
    }

    private static IHostBuilder CreateHostBuilder(StartupLaunchContext launchContext)
    {
        return Host.CreateDefaultBuilder().ConfigureServices(
            services =>
            {
                services.AddSingleton(launchContext);
                services.AddSingleton<IStartupService, RegistryStartupService>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IOverlayService, OverlayManager>();
                services.AddSingleton<IBrightnessService, PowerShellBrightnessService>();
                services.AddSingleton<IMonitorService, MonitorService>();
                services.AddSingleton<IAppDialogService, AppDialogService>();
                services.AddSingleton<IReminderDialogService, ReminderDialogService>();
                services.AddSingleton<IBreakOverlayService, BreakOverlayService>();
                services.AddSingleton<ITrayService, TrayService>();
                services.AddSingleton<IHotkeyService, HotkeyService>();
                services.AddSingleton<IAutoModeService, AutoModeService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ComfortScreenController>();

                services.AddSingleton<ShellViewModel>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<EyeCareViewModel>();
                services.AddSingleton<DisplayViewModel>();
                services.AddSingleton<ReminderViewModel>();
                services.AddSingleton<SettingsViewModel>();

                services.AddSingleton<ShellWindow>();
                services.AddSingleton<DashboardPage>();
                services.AddSingleton<EyeCarePage>();
                services.AddSingleton<DisplayPage>();
                services.AddSingleton<ReminderPage>();
                services.AddSingleton<SettingsPage>();
            }
        );
    }
}
