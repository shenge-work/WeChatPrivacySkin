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
        var palette = ThemeCatalog.Get(settings.Theme);
        Frame.BorderBrush = new SolidColorBrush(MediaColor.FromArgb(
            220,
            palette.AccentColor.R,
            palette.AccentColor.G,
            palette.AccentColor.B));

        PositionOver(target, settings);
    }

    private void PositionOver(WeChatWindowInfo target, AppSettings settings)
    {
        var dpi = NativeMethods.GetDpiForWindow(target.Handle);
        if (dpi == 0)
        {
            dpi = 96;
        }

        var scale = 96.0 / dpi;
        Width = Math.Max(1, target.Bounds.Width * scale);
        Height = Math.Max(1, target.Bounds.Height * scale);
        Left = target.Bounds.Left * scale;
        Top = target.Bounds.Top * scale;

        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        var zOrder = settings.OverlayAlwaysOnTop
            ? NativeMethods.HWND_TOPMOST
            : NativeMethods.HWND_NOTOPMOST;

        NativeMethods.SetWindowPos(
            _windowHandle,
            zOrder,
            (int)Math.Round(target.Bounds.Left),
            (int)Math.Round(target.Bounds.Top),
            (int)Math.Round(target.Bounds.Width),
            (int)Math.Round(target.Bounds.Height),
            NativeMethods.SWP_NOACTIVATE |
            NativeMethods.SWP_SHOWWINDOW |
            NativeMethods.SWP_NOOWNERZORDER);
    }
}
