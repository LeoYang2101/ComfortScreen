using ComfortScreen.Contracts;
using ComfortScreen.Models;

namespace ComfortScreen.Services;

public sealed class AutoModeService : IAutoModeService
{
    public EyeMode? DetermineMode(AppSettings settings, DateTime now)
    {
        if (
            settings.SolarModeEnabled
            && SolarTimeCalculator.TryGetSunriseSunset(
                DateOnly.FromDateTime(now),
                settings.Latitude,
                settings.Longitude,
                TimeZoneInfo.Local,
                out var sunrise,
                out var sunset
            )
        )
        {
            var current = TimeOnly.FromDateTime(now);
            return current >= sunrise && current < sunset ? EyeMode.Day : EyeMode.Night;
        }

        if (!settings.TimedModeEnabled)
        {
            return null;
        }

        var currentTime = TimeOnly.FromDateTime(now);
        var dayStart = settings.DayModeStart;
        var nightStart = settings.NightModeStart;

        var isNight = nightStart > dayStart
            ? currentTime >= nightStart || currentTime < dayStart
            : currentTime >= nightStart && currentTime < dayStart;

        return isNight ? EyeMode.Night : EyeMode.Day;
    }
}
