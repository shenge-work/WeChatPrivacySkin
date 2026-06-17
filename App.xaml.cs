using System.Windows;

namespace WeChatPrivacySkin;

public partial class App : System.Windows.Application
{
    private SettingsService? _settingsService;
    private OverlayManager? _overlayManager;
    private AutoMinimizeManager? _autoMinimizeManager;
    private TrayController? _trayController;
    private HotkeyManager? _hotkeyManager;
    private ManagementWindow? _managementWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsService = new SettingsService();
        _settingsService.Load();

        _overlayManager = new OverlayManager(_settingsService);
        _overlayManager.Start();

        _autoMinimizeManager = new AutoMinimizeManager(_settingsService);
        _autoMinimizeManager.Start();

        _trayController = new TrayController(
            _settingsService,
            showManagement: ShowManagementWindow,
            exitApplication: Shutdown);

        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.TogglePrivacyRequested += (_, _) => TogglePrivacyMode();
        _hotkeyManager.CycleThemeRequested += (_, _) => CycleTheme();
        _hotkeyManager.CleanScreenRequested += (_, _) => ActivateCleanScreen();
        _hotkeyManager.Start();

        if (e.Args.Any(arg => string.Equals(arg, "--show-management", StringComparison.OrdinalIgnoreCase)))
        {
            Dispatcher.BeginInvoke(ShowManagementWindow);
        }
    }

    private void ShowManagementWindow()
    {
        if (_settingsService is null || _overlayManager is null)
        {
            return;
        }

        if (_managementWindow is null)
        {
            _managementWindow = new ManagementWindow(_settingsService, _overlayManager, Shutdown);
            _managementWindow.Closed += (_, _) => _managementWindow = null;
        }

        _managementWindow.Show();
        _managementWindow.Activate();
    }

    private void TogglePrivacyMode()
    {
        _settingsService?.Update(settings => settings.Privacy.Enabled = !settings.Privacy.Enabled);
    }

    private void CycleTheme()
    {
        _settingsService?.Update(settings => settings.ThemePackId = ThemeCatalog.Next(settings.ThemePackId));
    }

    private void ActivateCleanScreen()
    {
        _settingsService?.Update(settings =>
        {
            settings.Privacy.Enabled = true;
            settings.Privacy.Strategy = ProtectionStrategy.OverlayMask;
            settings.Privacy.Mode = settings.Privacy.Mode == PrivacyMode.CleanScreen
                ? PrivacyMode.DailyProtection
                : PrivacyMode.CleanScreen;
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        _trayController?.Dispose();
        _autoMinimizeManager?.Dispose();
        _overlayManager?.Dispose();
        _settingsService?.Save();

        base.OnExit(e);
    }
}
