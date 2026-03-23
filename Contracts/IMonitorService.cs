using ComfortScreen.Models;

namespace ComfortScreen.Contracts;

public interface IMonitorService
{
    IReadOnlyList<MonitorTargetItem> GetTargets();
}
