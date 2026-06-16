using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace WeChatPrivacySkin;

public sealed class TrayController : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly Action _showSettings;
    private readonly Action _exitApplication;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _privacyItem;
    private readonly Forms.ToolStripMenuItem _themeMenu;
    private readonly Forms.ToolStripMenuItem _opacityMenu;

    public TrayController(
        SettingsService settingsService,
        Action showSettings,
        Action exitApplication)
    {
        _settingsService = settingsService;
        _showSettings = showSettings;
        _exitApplication = exitApplication;

        _privacyItem = new Forms.ToolStripMenuItem("开启隐私模式");
        _privacyItem.Click += (_, _) =>
            _settingsService.Update(settings => settings.PrivacyEnabled = !_settingsService.Current.PrivacyEnabled);

        _themeMenu = new Forms.ToolStripMenuItem("主题");
        foreach (var theme in ThemeCatalog.All)
        {
            var item = new Forms.ToolStripMenuItem(ThemeCatalog.DisplayName(theme)) { Tag = theme };
            item.Click += (_, _) =>
            {
                if (item.Tag is SkinTheme selectedTheme)
                {
                    _settingsService.Update(settings => settings.Theme = selectedTheme);
                }
            };
            _themeMenu.DropDownItems.Add(item);
        }

        _opacityMenu = new Forms.ToolStripMenuItem("遮罩强度");
        foreach (var value in new[] { 0.55, 0.70, 0.82, 0.92 })
        {
            var label = $"{value:P0}";
            var item = new Forms.ToolStripMenuItem(label) { Tag = value };
            item.Click += (_, _) =>
            {
                if (item.Tag is double opacity)
                {
                    _settingsService.Update(settings => settings.OverlayOpacity = opacity);
                }
            };
            _opacityMenu.DropDownItems.Add(item);
        }

        var openSettingsItem = new Forms.ToolStripMenuItem("设置...");
        openSettingsItem.Click += (_, _) => _showSettings();

        var chooseBackgroundItem = new Forms.ToolStripMenuItem("选择背景图片...");
        chooseBackgroundItem.Click += (_, _) => ChooseBackgroundImage();

        var clearBackgroundItem = new Forms.ToolStripMenuItem("清除背景图片");
        clearBackgroundItem.Click += (_, _) => _settingsService.Update(settings => settings.BackgroundImagePath = null);

        var exitItem = new Forms.ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => _exitApplication();

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(_privacyItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_themeMenu);
        menu.Items.Add(_opacityMenu);
        menu.Items.Add(chooseBackgroundItem);
        menu.Items.Add(clearBackgroundItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(openSettingsItem);
        menu.Items.Add(exitItem);

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "微信隐私皮肤",
            Icon = System.Drawing.SystemIcons.Shield,
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => _showSettings();

        _settingsService.SettingsChanged += OnSettingsChanged;
        RefreshMenu();
    }

    private void OnSettingsChanged(object? sender, AppSettings e)
    {
        RefreshMenu();
    }

    private void RefreshMenu()
    {
        var settings = _settingsService.Current;
        _privacyItem.Checked = settings.PrivacyEnabled;

        foreach (Forms.ToolStripMenuItem item in _themeMenu.DropDownItems.OfType<Forms.ToolStripMenuItem>())
        {
            item.Checked = item.Tag is SkinTheme theme && theme == settings.Theme;
        }

        foreach (Forms.ToolStripMenuItem item in _opacityMenu.DropDownItems.OfType<Forms.ToolStripMenuItem>())
        {
            item.Checked = item.Tag is double value && Math.Abs(value - settings.OverlayOpacity) < 0.01;
        }
    }

    private void ChooseBackgroundImage()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择遮罩背景图片",
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.webp|所有文件|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            _settingsService.Update(settings => settings.BackgroundImagePath = dialog.FileName);
        }
    }

    public void Dispose()
    {
        _settingsService.SettingsChanged -= OnSettingsChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
