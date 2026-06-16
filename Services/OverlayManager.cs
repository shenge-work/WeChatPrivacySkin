using System.Windows.Threading;

namespace WeChatPrivacySkin;

public sealed class OverlayManager : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly WeChatWindowLocator _windowLocator = new();
    private readonly PrivacyPolicyService _privacyPolicy = new();
    private readonly Dictionary<IntPtr, OverlayWindow> _overlays = new();
    private readonly DispatcherTimer _timer;
    private FocusFrameWindow? _focusFrame;

    public event EventHandler<PrivacySnapshot>? SnapshotChanged;

    public PrivacySnapshot? LastSnapshot { get; private set; }

    public OverlayManager(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _settingsService.SettingsChanged += OnSettingsChanged;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        _timer.Tick += (_, _) => Refresh();
    }

    public void Start()
    {
        _timer.Start();
        Refresh();
    }

    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        if (!settings.Privacy.Enabled)
        {
            CloseAll();
            return;
        }

        Refresh();
    }

    private void Refresh()
    {
        try
        {
            RefreshCore();
        }
        catch
        {
            CloseAll();
        }
    }

    private void RefreshCore()
    {
        var settings = _settingsService.Current;
        if (!settings.Privacy.Enabled)
        {
            CloseAll();
            return;
        }

        var windows = _windowLocator.FindVisibleWindows(settings);
        var foreground = NativeMethods.GetForegroundWindow();
        var snapshot = _privacyPolicy.CreateSnapshot(windows, foreground, settings);
        LastSnapshot = snapshot;
        SnapshotChanged?.Invoke(this, snapshot);

        var activeWindowIsVisible = snapshot.ActiveWindow is not null &&
                                    settings.Privacy.Mode is not PrivacyMode.AwayCover and not PrivacyMode.CleanScreen;
        var activeBounds = activeWindowIsVisible ? snapshot.ActiveWindow?.Bounds : null;
        var liveHandles = new HashSet<IntPtr>();

        UpdateFocusFrame(activeWindowIsVisible ? snapshot.ActiveWindow : null, settings);

        foreach (var decision in snapshot.Decisions)
        {
            var window = decision.Window;
            if (!decision.ShouldCover)
            {
                CloseOverlay(window.Handle);
                continue;
            }

            liveHandles.Add(window.Handle);
            if (!_overlays.TryGetValue(window.Handle, out var overlay))
            {
                overlay = new OverlayWindow(window.Handle);
                _overlays[window.Handle] = overlay;
                overlay.UpdateFrom(decision, settings, activeBounds);
                overlay.Show();
            }
            else
            {
                overlay.UpdateFrom(decision, settings, activeBounds);
            }
        }

        foreach (var staleHandle in _overlays.Keys.Where(handle => !liveHandles.Contains(handle)).ToArray())
        {
            CloseOverlay(staleHandle);
        }
    }

    private void UpdateFocusFrame(WeChatWindowInfo? foregroundWeChat, AppSettings settings)
    {
        if (foregroundWeChat is null)
        {
            CloseFocusFrame();
            return;
        }

        if (_focusFrame is null || _focusFrame.TargetHandle != foregroundWeChat.Handle)
        {
            CloseFocusFrame();
            _focusFrame = new FocusFrameWindow(foregroundWeChat.Handle);
            _focusFrame.UpdateFrom(foregroundWeChat, settings);
            _focusFrame.Show();
            return;
        }

        _focusFrame.UpdateFrom(foregroundWeChat, settings);
    }

    private void CloseFocusFrame()
    {
        if (_focusFrame is null)
        {
            return;
        }

        _focusFrame.Close();
        _focusFrame = null;
    }

    private void CloseOverlay(IntPtr handle)
    {
        if (!_overlays.Remove(handle, out var overlay))
        {
            return;
        }

        overlay.Close();
    }

    private void CloseAll()
    {
        CloseFocusFrame();
        foreach (var handle in _overlays.Keys.ToArray())
        {
            CloseOverlay(handle);
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _settingsService.SettingsChanged -= OnSettingsChanged;
        CloseAll();
    }
}
