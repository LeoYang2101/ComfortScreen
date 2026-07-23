# ComfortScreen Single-Instance Protection Design

## Goal

Ensure only one ComfortScreen process initializes application services in a Windows user session. A repeated interactive launch activates the existing main window, while a repeated `--startup` launch exits silently.

## Confirmed Problem

`App.OnStartup` currently creates and starts the generic host before performing any process coordination. Every process therefore creates its own tray icon, timers, reminder services, overlay windows, hotkey registration attempt, and settings service.

The resulting duplicate processes can:

- create multiple tray icons and duplicate reminders;
- stack multiple eye-filter overlays;
- cause later global-hotkey registrations to fail;
- write the same `%LOCALAPPDATA%\ComfortScreen\settings.json` concurrently; and
- allow an interactive launch and the Windows startup launch to coexist.

## Required Behavior

### First launch

- The first process in the current Windows session becomes the primary instance.
- It starts inter-process activation listening before creating the host.
- It then initializes the host, tray icon, timers, hotkey, reminders, and overlays exactly once.
- An interactive first launch shows the main window.
- A first launch with `--startup` remains hidden in the tray.

### Repeated interactive launch

- The new process must not create or start the host.
- It sends one `Activate` request to the primary instance and exits with success.
- The primary instance shows the main window if hidden, restores it if minimized, activates it, and raises it to the foreground.

### Repeated startup launch

- The new process must not create or start the host.
- It does not activate or otherwise disturb the primary window.
- It exits silently with success.

## Architecture

Add a focused `SingleInstanceCoordinator` responsible for process ownership and activation IPC.

The coordinator owns:

- a session-scoped named mutex used to elect one primary process;
- a current-user-only named pipe used to send the `Activate` command;
- a background listener task and cancellation source owned by the primary process; and
- an `ActivationRequested` event raised when a valid command is received.

The coordinator exposes one startup operation that returns whether the caller is the primary process or a secondary process that must exit. The caller specifies whether a secondary process should notify the primary instance. This keeps interactive-versus-startup policy explicit at the application boundary while keeping mutex and pipe mechanics isolated and testable.

The mutex uses the Windows local namespace so instances in different interactive sessions do not block each other. The named pipe uses `PipeOptions.CurrentUserOnly` so another Windows user cannot send activation commands.

## Startup Data Flow

1. `App.OnStartup` registers exception handlers and parses the launch arguments.
2. `App` constructs the single-instance coordinator and subscribes to `ActivationRequested`.
3. The coordinator attempts a non-blocking mutex acquisition.
4. If the process becomes primary, the coordinator creates the first pipe server and begins listening before `App` creates the host.
5. If the process is secondary and the launch is interactive, the coordinator retries a short pipe connection and sends `Activate` once.
6. If the process is secondary and the launch uses `--startup`, it skips IPC.
7. Every secondary process calls `Shutdown(0)` and returns before `Host.CreateDefaultBuilder`, service resolution, window creation, or controller initialization.
8. The primary process continues through the existing initialization sequence.

## Window Activation

The existing shell restoration behavior becomes one reusable method on `ShellWindow`. It performs the following operations on the WPF dispatcher:

1. call `Show()` if the window is hidden;
2. restore `WindowState.Normal` if minimized;
3. call `Activate()` and `Focus()`; and
4. briefly toggle `Topmost` to raise the already-running application above other windows, then restore its normal non-topmost state.

Tray restoration and IPC activation both call this same method.

An activation command may arrive while the primary process is still building the host. In that case `App` records one pending activation flag. After `ShellWindow` has been assigned to `MainWindow` and initialized, `App` consumes the flag and activates the window. Multiple early requests coalesce into one activation.

## Error Handling

- A secondary process retries pipe connection for a short bounded interval to cover the race between mutex acquisition and pipe listener readiness.
- A secondary process always exits even if notification fails. It must never fall through to host initialization.
- Listener cancellation during normal shutdown is ignored.
- Recoverable listener or malformed-command errors are logged and the primary listener continues with a new pipe server.
- A fatal mutex acquisition error is logged through the existing startup error path and shuts down the process rather than allowing an uncoordinated instance.
- An abandoned mutex is treated as successful primary ownership so the application recovers after an abnormal process termination.
- `App.OnExit` disposes the coordinator after stopping and disposing the host.

## Components and Files

- `Infrastructure/SingleInstanceCoordinator.cs`: named mutex ownership, named-pipe client/server, activation event, bounded retry, and disposal.
- `App.xaml.cs`: launch policy, early instance gate, pending activation handling, and coordinator lifecycle.
- `Views/ShellWindow.xaml.cs`: shared show/restore/foreground activation method.
- `ComfortScreen.Tests/ComfortScreen.Tests.csproj`: standalone automated test project.
- `ComfortScreen.Tests/SingleInstanceCoordinatorTests.cs`: real named mutex and named pipe tests with unique names per test.
- `ComfortScreen.csproj`: exclude test project source files from the WPF application's default compile glob.

## Automated Testing

Tests use unique mutex and pipe names generated per test so they can run independently and in parallel without affecting the production application.

Required tests:

1. The first coordinator becomes primary and a second coordinator with the same names is secondary.
2. Disposing the primary coordinator releases ownership and allows a later coordinator to become primary.
3. An interactive secondary launch sends exactly one activation request to the primary coordinator.
4. A startup secondary launch exits without sending activation.
5. A secondary process remains secondary and returns promptly when the pipe notification cannot be delivered.
6. Coordinator disposal cancels listener work without unobserved exceptions.

## Runtime Verification

After automated tests and a Release build pass:

1. Launch the application once with `--startup` and verify it remains hidden with one process.
2. Launch it interactively and verify the existing process count remains one and the window is shown and raised.
3. Launch it interactively again while visible and verify the same window is activated without a new process.
4. Confirm only one tray icon is created.
5. Enable the eye filter and confirm there is one overlay layer rather than stacked opacity.
6. Verify the configured global hotkey remains registered and the settings file remains valid JSON.

## Out of Scope

- Forwarding arbitrary command-line arguments beyond the `Activate` command.
- Coordinating instances across separate Windows logon sessions.
- Changing settings persistence or adding file locking independently of single-instance enforcement.
- Adding updater, elevation, or administrator-process coordination.
