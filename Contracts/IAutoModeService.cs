using ComfortScreen.Models;

namespace ComfortScreen.Contracts;

public interface IAutoModeService
{
    EyeMode? DetermineMode(AppSettings settings, DateTime now);
}
