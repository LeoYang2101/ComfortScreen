using ComfortScreen.Contracts;
using ComfortScreen.Views;
using Forms = System.Windows.Forms;

namespace ComfortScreen.Services;

public sealed class BreakOverlayService : IBreakOverlayService
{
    public async Task ShowAsync(TimeSpan duration, bool allowManualExit, CancellationToken cancellationToken)
    {
        var windows = new List<BreakOverlayWindow>();
        using var overlayExitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            foreach (var screen in Forms.Screen.AllScreens)
            {
                var window = new BreakOverlayWindow(allowManualExit)
                {
                    Left = screen.Bounds.Left,
                    Top = screen.Bounds.Top,
                    Width = screen.Bounds.Width,
                    Height = screen.Bounds.Height
                };

                if (allowManualExit)
                {
                    window.ExitRequested += (_, _) => overlayExitCts.Cancel();
                }

                windows.Add(window);
                window.Show();
            }

            var remaining = duration;
            while (remaining > TimeSpan.Zero && !overlayExitCts.IsCancellationRequested)
            {
                foreach (var window in windows)
                {
                    window.UpdateCountdown(remaining);
                }

                await Task.Delay(1000, overlayExitCts.Token);
                remaining -= TimeSpan.FromSeconds(1);
            }
        }
        catch (TaskCanceledException)
        {
            // Intentionally ignored.
        }
        finally
        {
            foreach (var window in windows)
            {
                window.Close();
            }
        }
    }
}
