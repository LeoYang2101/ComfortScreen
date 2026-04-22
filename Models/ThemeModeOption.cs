namespace ComfortScreen.Models;

public sealed record ThemeModeOption(string DisplayName, AppThemeMode Mode)
{
    public override string ToString() => DisplayName;
}
