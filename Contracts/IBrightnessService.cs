namespace ComfortScreen.Contracts;

public interface IBrightnessService
{
    Task<bool> TrySetBrightnessAsync(int brightness, CancellationToken cancellationToken = default);
}
