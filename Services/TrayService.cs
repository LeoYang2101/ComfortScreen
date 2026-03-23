using ComfortScreen.Contracts;
using Forms = System.Windows.Forms;

namespace ComfortScreen.Services;

public sealed class TrayService : ITrayService
{
    private Forms.NotifyIcon? _notifyIcon;

    public void Initialize(Action restoreAction, Action toggleFilterAction, Action quickNightModeAction, Action exitAction)
    {
        if (_notifyIcon is not null)
        {
            return;
        }

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Shield,
            Visible = true,
            Text = "ComfortScreen"
        };

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("打开面板", null, (_, _) => restoreAction());
        menu.Items.Add("切换护眼开关", null, (_, _) => toggleFilterAction());
        menu.Items.Add("快速夜间模式", null, (_, _) => quickNightModeAction());
        menu.Items.Add("退出", null, (_, _) => exitAction());

        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => restoreAction();
    }

    public void UpdateText(string text)
    {
        if (_notifyIcon is null)
        {
            return;
        }

        _notifyIcon.Text = text.Length <= 63 ? text : text[..63];
    }

    public void Dispose()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _notifyIcon = null;
    }
}
