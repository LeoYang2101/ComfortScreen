namespace ComfortScreen.Services;

public static class SolarTimeCalculator
{
    private const double Zenith = 90.8333;

    public static bool TryGetSunriseSunset(
        DateOnly date,
        double latitude,
        double longitude,
        TimeZoneInfo timeZone,
        out TimeOnly sunrise,
        out TimeOnly sunset)
    {
        sunrise = default;
        sunset = default;

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            return false;
        }

        var dayOfYear = date.DayOfYear;
        if (!TryComputeUtc(dayOfYear, latitude, longitude, isSunrise: true, out var sunriseUtcHour))
        {
            return false;
        }

        if (!TryComputeUtc(dayOfYear, latitude, longitude, isSunrise: false, out var sunsetUtcHour))
        {
            return false;
        }

        var localOffset = timeZone.GetUtcOffset(date.ToDateTime(TimeOnly.MinValue)).TotalHours;
        sunrise = ToTimeOnly(sunriseUtcHour + localOffset);
        sunset = ToTimeOnly(sunsetUtcHour + localOffset);

        return true;
    }

    private static bool TryComputeUtc(
        int dayOfYear,
        double latitude,
        double longitude,
        bool isSunrise,
        out double utcHour)
    {
        utcHour = 0;

        var lngHour = longitude / 15.0;
        var approximateTime = dayOfYear + ((isSunrise ? 6 : 18) - lngHour) / 24.0;

        var meanAnomaly = (0.9856 * approximateTime) - 3.289;

        var trueLongitude = meanAnomaly
                            + (1.916 * Math.Sin(DegToRad(meanAnomaly)))
                            + (0.020 * Math.Sin(2 * DegToRad(meanAnomaly)))
                            + 282.634;
        trueLongitude = NormalizeDegrees(trueLongitude);

        var rightAscension = RadToDeg(Math.Atan(0.91764 * Math.Tan(DegToRad(trueLongitude))));
        rightAscension = NormalizeDegrees(rightAscension);

        var lQuadrant = Math.Floor(trueLongitude / 90.0) * 90.0;
        var raQuadrant = Math.Floor(rightAscension / 90.0) * 90.0;
        rightAscension += lQuadrant - raQuadrant;
        rightAscension /= 15.0;

        var sinDeclination = 0.39782 * Math.Sin(DegToRad(trueLongitude));
        var cosDeclination = Math.Cos(Math.Asin(sinDeclination));

        var cosHourAngle =
            (Math.Cos(DegToRad(Zenith)) - (sinDeclination * Math.Sin(DegToRad(latitude))))
            / (cosDeclination * Math.Cos(DegToRad(latitude)));

        if (cosHourAngle is > 1 or < -1)
        {
            return false;
        }

        var hourAngle = isSunrise
            ? 360 - RadToDeg(Math.Acos(cosHourAngle))
            : RadToDeg(Math.Acos(cosHourAngle));
        hourAngle /= 15.0;

        var localMeanTime = hourAngle + rightAscension - (0.06571 * approximateTime) - 6.622;
        utcHour = NormalizeHours(localMeanTime - lngHour);

        return true;
    }

    private static double DegToRad(double degrees)
    {
        return (Math.PI / 180.0) * degrees;
    }

    private static double RadToDeg(double radians)
    {
        return (180.0 / Math.PI) * radians;
    }

    private static double NormalizeDegrees(double value)
    {
        var normalized = value % 360.0;
        return normalized < 0 ? normalized + 360.0 : normalized;
    }

    private static double NormalizeHours(double value)
    {
        var normalized = value % 24.0;
        return normalized < 0 ? normalized + 24.0 : normalized;
    }

    private static TimeOnly ToTimeOnly(double hours)
    {
        var normalized = NormalizeHours(hours);
        var totalSeconds = (int)Math.Round(normalized * 3600, MidpointRounding.AwayFromZero);
        totalSeconds %= 24 * 3600;
        return TimeOnly.MinValue.Add(TimeSpan.FromSeconds(totalSeconds));
    }
}
