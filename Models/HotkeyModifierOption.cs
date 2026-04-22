namespace ComfortScreen.Models;

public sealed record HotkeyModifierOption(string DisplayName, int Modifiers)
{
    public override string ToString() => DisplayName;
}
