using ComfortScreen.Models;

namespace ComfortScreen.Contracts;

public interface ISettingsService
{
    AppSettings Load();

    void Save(AppSettings settings);
}
