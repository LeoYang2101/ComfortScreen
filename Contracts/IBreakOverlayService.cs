namespace ComfortScreen.Contracts;

public interface IBreakOverlayService
{
    Task ShowAsync(TimeSpan duration, bool allowManualExit, CancellationToken cancellationToken);
}
