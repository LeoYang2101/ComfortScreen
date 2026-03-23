using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ComfortScreen.Contracts;
using ComfortScreen.Models;

namespace ComfortScreen.Services;

public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _settingsPath;
    private readonly IStartupService _startupService;

    public SettingsService(IStartupService startupService)
    {
        _startupService = startupService;

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ComfortScreen");
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(AppContext.BaseDirectory, "data");
        }

        Directory.CreateDirectory(root);
        _settingsPath = Path.Combine(root, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            settings = ApplyLegacyThemeCompatibility(settings, json);
            settings.StartWithWindows = GetStartupEnabled(settings.StartWithWindows);
            return settings;
        }
        catch
        {
            return new AppSettings
            {
                StartWithWindows = GetStartupEnabled(false)
            };
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private bool GetStartupEnabled(bool fallback)
    {
        try
        {
            return _startupService.IsEnabled();
        }
        catch
        {
            return fallback;
        }
    }

    private static AppSettings ApplyLegacyThemeCompatibility(AppSettings settings, string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("ThemeMode", out _))
            {
                return settings;
            }

            if (document.RootElement.TryGetProperty("Theme", out _))
            {
                settings.ThemeMode = AppThemeMode.Light;
            }
        }
        catch
        {
            // If compatibility parsing fails, keep deserialized values.
        }

        return settings;
    }
}
