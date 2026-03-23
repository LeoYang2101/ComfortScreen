using ComfortScreen.Models;

namespace ComfortScreen.Contracts;

public interface IOverlayService
{
    void Apply(AppSettings settings);

    void HideAll();
}
