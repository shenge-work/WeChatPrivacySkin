using System.Drawing;
using Forms = System.Windows.Forms;

namespace WeChatPrivacySkin;

public sealed class TrayController : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly Action _showManagement;
    private readonly Action _exitApplication;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _fallbackMenu;
    private Icon _currentIcon;

    public TrayController(
        SettingsService settingsService,
        Action showManagement,
        Action exitApplication)
    {
        _settingsService = settingsService;
        _showManagement = showManagement;
        _exitApplication = exitApplication;

        var exitItem = new Forms.ToolStripMenuItem("退出工具");
        exitItem.Click += (_, _) => _exitApplication();

        _fallbackMenu = new Forms.ContextMenuStrip();
        _fallbackMenu.Items.Add(exitItem);

        _currentIcon = TrayIconFactory.Create(_settingsService.Current);
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = BuildTooltip(),
            Icon = _currentIcon,
            Visible = true
        };
        _notifyIcon.MouseUp += OnMouseUp;
        _notifyIcon.DoubleClick += (_, _) => TogglePrivacyEnabled();

        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    private void OnMouseUp(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button != Forms.MouseButtons.Right)
        {
            return;
        }

        if ((Forms.Control.ModifierKeys & Forms.Keys.Shift) == Forms.Keys.Shift)
        {
            _fallbackMenu.Show(Forms.Cursor.Position);
            return;
        }

        _showManagement();
    }

    private void TogglePrivacyEnabled()
    {
        _settingsService.Update(settings => settings.Privacy.Enabled = !settings.Privacy.Enabled);
    }

    private void OnSettingsChanged(object? sender, AppSettings e)
    {
        _notifyIcon.Text = BuildTooltip();
        RefreshTrayIcon();
    }

    private string BuildTooltip()
    {
        var settings = _settingsService.Current;
        var enabled = settings.Privacy.Enabled ? "开启" : "关闭";
        var mode = PrivacyModeCatalog.DisplayName(settings.Privacy.Mode);
        var theme = ThemeCatalog.Get(settings.ThemePackId).DisplayName;
        return $"微信隐私皮肤 · {enabled} · {mode} · {theme}";
    }

    private void RefreshTrayIcon()
    {
        var previous = _currentIcon;
        _currentIcon = TrayIconFactory.Create(_settingsService.Current);
        _notifyIcon.Icon = _currentIcon;
        previous.Dispose();
    }

    public void Dispose()
    {
        _settingsService.SettingsChanged -= OnSettingsChanged;
        _notifyIcon.MouseUp -= OnMouseUp;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _currentIcon.Dispose();
        _fallbackMenu.Dispose();
    }
}
