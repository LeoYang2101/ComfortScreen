namespace ComfortScreen.Contracts;

public interface IStartupService
{
    bool IsEnabled();

    void SetEnabled(bool enabled);

    string GetCommandLine();
}
