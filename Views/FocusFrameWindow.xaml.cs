using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace WeChatPrivacySkin;

public partial class FocusFrameWindow : Window
{
    private IntPtr _windowHandle;

    public FocusFrameWindow(IntPtr targetHandle)
    {
        TargetHandle = targetHandle;
        InitializeComponent();
        try
        {
            new WindowInteropHelper(this).Owner = targetHandle;
        }
        catch
        {
            // The WeChat window can close while the frame is being created.
        }
    }

    public IntPtr TargetHandle { get; }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _windowHandle = new WindowInteropHelper(this).Handle;
        var extendedStyle = NativeMethods.GetWindowLongPtr(_windowHandle, NativeMethods.GWL_EXSTYLE).ToInt64();
        extendedStyle |= NativeMethods.WS_EX_TOOLWINDOW |
                         NativeMethods.WS_EX_TRANSPARENT |
                         NativeMethods.WS_EX_NOACTIVATE;
        extendedStyle &= ~NativeMethods.WS_EX_APPWINDOW;
        NativeMethods.SetWindowLongPtr(_windowHandle, NativeMethods.GWL_EXSTYLE, new IntPtr(extendedStyle));
    }

    public void UpdateFrom(WeChatWindowInfo target, AppSettings settings)
    {
        var theme = ThemeCatalog.Get(settings.ThemePackId);
        var accent = new SolidColorBrush(MediaColor.FromArgb(
            220,
            theme.AccentColor.R,
            theme.AccentColor.G,
            theme.AccentColor.B));
        Frame.BorderBrush = accent;
        Frame.BorderThickness = new Thickness(theme.FrameThickness);
        Frame.CornerRadius = new CornerRadius(theme.CornerRadius + 6);

        Glow.BorderBrush = new SolidColorBrush(MediaColor.FromArgb(
            180,
            theme.SecondaryAccentColor.R,
            theme.SecondaryAccentColor.G,
            theme.SecondaryAccentColor.B));
        Glow.CornerRadius = new CornerRadius(theme.CornerRadius + 12);

        SideBadge.Background = new SolidColorBrush(MediaColor.FromArgb(
            230,
            theme.PanelColor.R,
            theme.PanelColor.G,
            theme.PanelColor.B));
        SideBadge.BorderBrush = accent;
        SideBadge.BorderThickness = new Thickness(1);
        SideBadge.CornerRadius = new CornerRadius(Math.Max(6, theme.CornerRadius));
        SideBadgeText.Text = PrivacyModeCatalog.DisplayName(settings.Privacy.Mode);
        SideBadgeText.Foreground = new SolidColorBrush(theme.PrimaryTextColor);

        PositionOver(target, settings, theme);
    }

    private void PositionOver(WeChatWindowInfo target, AppSettings settings, ThemePack theme)
    {
        var outset = theme.Outset;
        var left = target.Bounds.Left - outset;
        var top = target.Bounds.Top - outset;
        var width = target.Bounds.Width + outset * 2;
        var height = target.Bounds.Height + outset * 2;

        if (_windowHandle != IntPtr.Zero)
        {
            var zOrder = settings.OverlayAlwaysOnTop
                ? NativeMethods.HWND_TOPMOST
                : NativeMethods.HWND_NOTOPMOST;

            NativeMethods.SetWindowPos(
                _windowHandle,
                zOrder,
                (int)Math.Round(left),
                (int)Math.Round(top),
                (int)Math.Round(width),
                (int)Math.Round(height),
                NativeMethods.SWP_NOACTIVATE |
                NativeMethods.SWP_SHOWWINDOW |
                NativeMethods.SWP_NOOWNERZORDER);
        }

        var scale = GetDipScale(_windowHandle, target.Handle);
        Width = Math.Max(1, width * scale);
        Height = Math.Max(1, height * scale);

        if (_windowHandle == IntPtr.Zero)
        {
            Left = left * scale;
            Top = top * scale;
        }
    }

    private static double GetDipScale(IntPtr preferredDpiHandle, IntPtr fallbackDpiHandle)
    {
        var dpi = preferredDpiHandle == IntPtr.Zero ? 0 : NativeMethods.GetDpiForWindow(preferredDpiHandle);
        if (dpi == 0 && fallbackDpiHandle != IntPtr.Zero)
        {
            dpi = NativeMethods.GetDpiForWindow(fallbackDpiHandle);
        }

        return 96.0 / Math.Max(96, dpi);
    }
}
