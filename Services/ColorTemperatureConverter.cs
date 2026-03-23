namespace ComfortScreen.Services;

public static class ColorTemperatureConverter
{
    public static System.Windows.Media.Color ToColor(double kelvin)
    {
        var temp = Math.Clamp(kelvin, 1000, 6500) / 100.0;

        double red;
        double green;
        double blue;

        if (temp <= 66)
        {
            red = 255;
            green = 99.4708025861 * Math.Log(temp) - 161.1195681661;
            if (temp <= 19)
            {
                blue = 0;
            }
            else
            {
                blue = 138.5177312231 * Math.Log(temp - 10) - 305.0447927307;
            }
        }
        else
        {
            red = 329.698727446 * Math.Pow(temp - 60, -0.1332047592);
            green = 288.1221695283 * Math.Pow(temp - 60, -0.0755148492);
            blue = 255;
        }

        return System.Windows.Media.Color.FromRgb(
            ToByte(red),
            ToByte(green),
            ToByte(blue));
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Clamp(value, 0, 255);
    }
}
