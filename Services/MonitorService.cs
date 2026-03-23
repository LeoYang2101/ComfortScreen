using ComfortScreen.Contracts;
using ComfortScreen.Models;
using Forms = System.Windows.Forms;

namespace ComfortScreen.Services;

public sealed class MonitorService : IMonitorService
{
    public IReadOnlyList<MonitorTargetItem> GetTargets()
    {
        var targets = new List<MonitorTargetItem>
        {
            new("*", "全部显示器", true)
        };

        foreach (var screen in Forms.Screen.AllScreens)
        {
            var displayName = screen.Primary
                ? $"{screen.DeviceName} (主屏)"
                : screen.DeviceName;

            targets.Add(new MonitorTargetItem(screen.DeviceName, displayName, false));
        }

        return targets;
    }
}
