namespace ComfortScreen.Models;

public sealed record MonitorTargetItem(string DeviceName, string DisplayName, bool IsAll)
{
    public override string ToString() => DisplayName;
}
