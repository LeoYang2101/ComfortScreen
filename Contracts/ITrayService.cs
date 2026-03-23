namespace ComfortScreen.Contracts;

public interface ITrayService : IDisposable
{
    void Initialize(Action restoreAction, Action toggleFilterAction, Action quickNightModeAction, Action exitAction);

    void UpdateText(string text);
}
