using System.Windows;
using ComfortScreen.Models;

namespace ComfortScreen.Contracts;

public interface IThemeService
{
    void ApplyTheme(AppThemeMode themeMode, Window window);
}
