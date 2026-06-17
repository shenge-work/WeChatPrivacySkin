using System.Text;
using System.Windows;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using WpfPoint = System.Windows.Point;

namespace WeChatPrivacySkin;

public sealed class AutoMinimizeManager : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly WeChatWindowLocator _windowLocator = new();
    private readonly DispatcherTimer _timer;
    private bool _lastLeftButtonDown;
    private IntPtr? _lastForegroundWeChatHandle;
    private DateTime? _pendingMinimizeAt;

    public AutoMinimizeManager(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _settingsService.SettingsChanged += OnSettingsChanged;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(80)
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
        if (!IsEnabled(settings))
        {
            Reset();
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
            Reset();
        }
    }

    private void RefreshCore()
    {
        var settings = _settingsService.Current;
        if (!IsEnabled(settings))
        {
            Reset();
            return;
        }

        var windows = _windowLocator.FindVisibleWindows(settings);
        TrackForegroundWeChat(windows);

        if (_pendingMinimizeAt is not null && _pendingMinimizeAt <= DateTime.UtcNow)
        {
            _pendingMinimizeAt = null;
            MinimizeTargets(windows, settings);
        }

        var leftButtonDown = IsLeftButtonDown();
        var clickStarted = leftButtonDown && !_lastLeftButtonDown;
        _lastLeftButtonDown = leftButtonDown;

        if (!clickStarted || windows.Count == 0 || !TryGetCursorPoint(out var cursorPoint))
        {
            return;
        }

        if (IsCursorInsideAnyWeChatWindow(cursorPoint, windows) || IsCursorOverTaskbar(cursorPoint))
        {
            return;
        }

        var delay = TimeSpan.FromMilliseconds(settings.Privacy.AutoMinimize.DelayMilliseconds);
        if (delay <= TimeSpan.Zero)
        {
            MinimizeTargets(windows, settings);
            return;
        }

        _pendingMinimizeAt = DateTime.UtcNow.Add(delay);
    }

    private void TrackForegroundWeChat(IReadOnlyList<WeChatWindowInfo> windows)
    {
        var foreground = NativeMethods.GetForegroundWindow();
        var foregroundWeChat = windows.FirstOrDefault(window => window.Handle == foreground);
        if (foregroundWeChat is not null)
        {
            _lastForegroundWeChatHandle = foregroundWeChat.Handle;
        }
    }

    private void MinimizeTargets(IReadOnlyList<WeChatWindowInfo> windows, AppSettings settings)
    {
        if (settings.Privacy.AutoMinimize.MinimizeAllVisibleWindows)
        {
            foreach (var window in windows)
            {
                NativeMethods.ShowWindowAsync(window.Handle, NativeMethods.SW_MINIMIZE);
            }

            return;
        }

        if (_lastForegroundWeChatHandle is { } handle && windows.Any(window => window.Handle == handle))
        {
            NativeMethods.ShowWindowAsync(handle, NativeMethods.SW_MINIMIZE);
        }
    }

    private static bool IsEnabled(AppSettings settings)
    {
        return settings.Privacy.Enabled &&
               settings.Privacy.Strategy == ProtectionStrategy.AutoMinimizeOnExternalClick;
    }

    private static bool IsLeftButtonDown()
    {
        return (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LBUTTON) & 0x8000) != 0;
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

    private static bool IsCursorInsideAnyWeChatWindow(WpfPoint cursorPoint, IReadOnlyList<WeChatWindowInfo> windows)
    {
        return windows.Any(window => window.Bounds.Contains(cursorPoint));
    }

    private static bool IsCursorOverTaskbar(WpfPoint cursorPoint)
    {
        return IsCursorOverShellTaskbar(cursorPoint) || IsCursorOverScreenTaskbarArea(cursorPoint);
    }

    private static bool IsCursorOverShellTaskbar(WpfPoint cursorPoint)
    {
        var foundTaskbar = false;
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            var className = GetClassName(hWnd);
            if (!string.Equals(className, "Shell_TrayWnd", StringComparison.Ordinal) &&
                !string.Equals(className, "Shell_SecondaryTrayWnd", StringComparison.Ordinal))
            {
                return true;
            }

            if (NativeMethods.GetWindowRect(hWnd, out var rect) && rect.ToRect().Contains(cursorPoint))
            {
                foundTaskbar = true;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        return foundTaskbar;
    }

    private static bool IsCursorOverScreenTaskbarArea(WpfPoint cursorPoint)
    {
        foreach (var screen in Forms.Screen.AllScreens)
        {
            if (Contains(screen.Bounds.Left, screen.Bounds.Top, screen.Bounds.Width, screen.Bounds.Height, cursorPoint))
            {
                var work = screen.WorkingArea;
                if (!Contains(work.Left, work.Top, work.Width, work.Height, cursorPoint))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool Contains(int left, int top, int width, int height, WpfPoint point)
    {
        return point.X >= left &&
               point.X < left + width &&
               point.Y >= top &&
               point.Y < top + height;
    }

    private static string GetClassName(IntPtr hWnd)
    {
        var builder = new StringBuilder(256);
        var length = NativeMethods.GetClassName(hWnd, builder, builder.Capacity);
        return length > 0 ? builder.ToString() : string.Empty;
    }

    private void Reset()
    {
        _lastLeftButtonDown = IsLeftButtonDown();
        _pendingMinimizeAt = null;
    }

    public void Dispose()
    {
        _timer.Stop();
        _settingsService.SettingsChanged -= OnSettingsChanged;
        Reset();
    }
}
