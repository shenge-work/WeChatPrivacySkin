using System.Windows;

namespace WeChatPrivacySkin;

public partial class App : System.Windows.Application
{
    private SettingsService? _settingsService;
    private OverlayManager? _overlayManager;
    private TrayController? _trayController;
    private HotkeyManager? _hotkeyManager;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsService = new SettingsService();
        _settingsService.Load();

        _overlayManager = new OverlayManager(_settingsService);
        _overlayManager.Start();

        _trayController = new TrayController(
            _settingsService,
            showSettings: ShowSettingsWindow,
            exitApplication: Shutdown);

        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.TogglePrivacyRequested += (_, _) => TogglePrivacyMode();
        _hotkeyManager.CycleThemeRequested += (_, _) => CycleTheme();
        _hotkeyManager.Start();
    }

    private void ShowSettingsWindow()
    {
        if (_settingsService is null)
        {
            return;
        }

        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow(_settingsService);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }

        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void TogglePrivacyMode()
    {
        _settingsService?.Update(settings => settings.PrivacyEnabled = !settings.PrivacyEnabled);
    }

    private void CycleTheme()
    {
        _settingsService?.Update(settings => settings.Theme = ThemeCatalog.Next(settings.Theme));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        _trayController?.Dispose();
        _overlayManager?.Dispose();
        _settingsService?.Save();

        base.OnExit(e);
    }
}
