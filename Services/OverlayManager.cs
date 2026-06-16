using System.Windows;
using System.Windows.Threading;
using WpfPoint = System.Windows.Point;

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

        var theme = ThemeCatalog.Get(settings.ThemePackId);
        var cursorPoint = settings.Privacy.Mode == PrivacyMode.FocusChat && TryGetCursorPoint(out var point)
            ? point
            : (WpfPoint?)null;
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
                overlay.UpdateFrom(decision, settings, activeBounds, ResolveRevealZone(window, theme, settings, cursorPoint));
                overlay.Show();
            }
            else
            {
                overlay.UpdateFrom(decision, settings, activeBounds, ResolveRevealZone(window, theme, settings, cursorPoint));
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

    private static bool TryGetCursorPoint(out WpfPoint point)
    {
        if (NativeMethods.GetCursorPos(out var nativePoint))
        {
            point = nativePoint.ToPoint();
            return true;
        }

        point = default;
        return false;
    }

    private static RevealZone ResolveRevealZone(
        WeChatWindowInfo window,
        ThemePack theme,
        AppSettings settings,
        WpfPoint? cursorPoint)
    {
        if (settings.Privacy.Mode != PrivacyMode.FocusChat || cursorPoint is null)
        {
            return RevealZone.None;
        }

        var overlayBounds = CreateOverlayBounds(window, theme);
        if (!overlayBounds.Contains(cursorPoint.Value) || !window.Bounds.Contains(cursorPoint.Value))
        {
            return RevealZone.None;
        }

        var ratioPoint = new WpfPoint(
            Math.Clamp((cursorPoint.Value.X - window.Bounds.Left) / Math.Max(1, window.Bounds.Width), 0, 1),
            Math.Clamp((cursorPoint.Value.Y - window.Bounds.Top) / Math.Max(1, window.Bounds.Height), 0, 1));

        foreach (var zone in GetRevealZones(window))
        {
            if (zone.RatioRect.Contains(ratioPoint))
            {
                return zone;
            }
        }

        return RevealZone.None;
    }

    private static Rect CreateOverlayBounds(WeChatWindowInfo window, ThemePack theme)
    {
        var outset = theme.Outset;
        return new Rect(
            window.Bounds.Left - outset,
            window.Bounds.Top - outset,
            window.Bounds.Width + outset * 2,
            window.Bounds.Height + outset * 2);
    }

    private static IEnumerable<RevealZone> GetRevealZones(WeChatWindowInfo window)
    {
        if (window.IsUtilityLike)
        {
            yield return new RevealZone(RevealZoneKind.TitleBar, new Rect(0, 0, 1, 0.18), 12, "标题区");
            yield return new RevealZone(RevealZoneKind.UtilityBody, new Rect(0, 0.18, 1, 0.62), 14, "内容区");
            yield return new RevealZone(RevealZoneKind.InputArea, new Rect(0, 0.80, 1, 0.20), 12, "操作区");
            yield break;
        }

        yield return new RevealZone(RevealZoneKind.ConversationList, new Rect(0, 0, 0.28, 1), 14, "会话列表");
        yield return new RevealZone(RevealZoneKind.TitleBar, new Rect(0.28, 0, 0.72, 0.12), 12, "标题区");
        yield return new RevealZone(RevealZoneKind.MessageArea, new Rect(0.28, 0.12, 0.72, 0.68), 18, "消息区");
        yield return new RevealZone(RevealZoneKind.InputArea, new Rect(0.28, 0.80, 0.72, 0.20), 16, "输入区");
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
