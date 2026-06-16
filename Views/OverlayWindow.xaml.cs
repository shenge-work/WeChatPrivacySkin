using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;

namespace WeChatPrivacySkin;

public partial class OverlayWindow : Window
{
    private readonly IntPtr _targetHandle;
    private IntPtr _windowHandle;

    public OverlayWindow(IntPtr targetHandle)
    {
        _targetHandle = targetHandle;
        InitializeComponent();
        try
        {
            new WindowInteropHelper(this).Owner = targetHandle;
        }
        catch
        {
            // The target window can close between enumeration and overlay creation.
        }
    }

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

    public void UpdateFrom(WeChatWindowInfo target, AppSettings settings, Rect? activeWeChatBounds)
    {
        ApplyTheme(settings);
        PositionOver(target, settings);
        ApplyActiveWindowCutout(target.Bounds, activeWeChatBounds);
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

    private void ApplyTheme(AppSettings settings)
    {
        var palette = ThemeCatalog.Get(settings.Theme);
        var overlayOpacity = (byte)Math.Round(Math.Clamp(settings.OverlayOpacity, 0.35, 0.95) * 255);
        var panelOpacity = (byte)Math.Round(Math.Clamp(settings.OverlayOpacity + 0.08, 0.45, 0.98) * 255);

        TintLayer.Fill = new SolidColorBrush(MediaColor.FromArgb(
            overlayOpacity,
            palette.OverlayColor.R,
            palette.OverlayColor.G,
            palette.OverlayColor.B));

        PatternLayer.Fill = CreatePatternBrush(palette);
        ImageLayer.Fill = CreateImageBrush(settings.BackgroundImagePath);

        Panel.Background = new SolidColorBrush(MediaColor.FromArgb(
            panelOpacity,
            palette.PanelColor.R,
            palette.PanelColor.G,
            palette.PanelColor.B));
        Panel.BorderBrush = new SolidColorBrush(MediaColor.FromArgb(210, palette.AccentColor.R, palette.AccentColor.G, palette.AccentColor.B));

        StatusDot.Background = new SolidColorBrush(palette.AccentColor);
        TitleText.Foreground = new SolidColorBrush(palette.PrimaryTextColor);
        SubtitleText.Foreground = new SolidColorBrush(palette.SecondaryTextColor);
        WatermarkText.Foreground = new SolidColorBrush(palette.SecondaryTextColor);

        foreach (var shape in PlaceholderShapes())
        {
            shape.Background = new SolidColorBrush(MediaColor.FromArgb(
                180,
                palette.PlaceholderColor.R,
                palette.PlaceholderColor.G,
                palette.PlaceholderColor.B));
        }
    }

    private IEnumerable<System.Windows.Controls.Border> PlaceholderShapes()
    {
        yield return AvatarPlaceholder;
        yield return LineOne;
        yield return LineTwo;
        yield return LineThree;
        yield return ContentLineOne;
        yield return ContentLineTwo;
        yield return ContentLineThree;
        yield return ContentLineFour;
    }

    private static MediaBrush CreateImageBrush(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return System.Windows.Media.Brushes.Transparent;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            var brush = new ImageBrush(bitmap)
            {
                Stretch = Stretch.UniformToFill,
                Opacity = 0.38
            };
            brush.Freeze();
            return brush;
        }
        catch
        {
            return System.Windows.Media.Brushes.Transparent;
        }
    }

    private static MediaBrush CreatePatternBrush(ThemePalette palette)
    {
        var group = new DrawingGroup();
        var transparent = new SolidColorBrush(Colors.Transparent);
        var accent = new SolidColorBrush(MediaColor.FromArgb(38, palette.AccentColor.R, palette.AccentColor.G, palette.AccentColor.B));
        group.Children.Add(new GeometryDrawing(transparent, null, new RectangleGeometry(new Rect(0, 0, 18, 18))));
        group.Children.Add(new GeometryDrawing(accent, null, new RectangleGeometry(new Rect(0, 0, 9, 9))));
        group.Children.Add(new GeometryDrawing(accent, null, new RectangleGeometry(new Rect(9, 9, 9, 9))));

        var brush = new DrawingBrush(group)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, 18, 18),
            ViewportUnits = BrushMappingMode.Absolute,
            Opacity = 0.6
        };
        brush.Freeze();
        return brush;
    }

    private void ApplyActiveWindowCutout(Rect targetBounds, Rect? activeBounds)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        var fullRegion = NativeMethods.CreateRectRgn(
            0,
            0,
            Math.Max(1, (int)Math.Round(targetBounds.Width)),
            Math.Max(1, (int)Math.Round(targetBounds.Height)));

        if (fullRegion == IntPtr.Zero)
        {
            return;
        }

        if (activeBounds is not null)
        {
            var intersection = Rect.Intersect(targetBounds, activeBounds.Value);
            if (!intersection.IsEmpty && intersection.Width > 0 && intersection.Height > 0)
            {
                var cutout = NativeMethods.CreateRectRgn(
                    Math.Max(0, (int)Math.Floor(intersection.Left - targetBounds.Left)),
                    Math.Max(0, (int)Math.Floor(intersection.Top - targetBounds.Top)),
                    Math.Min((int)Math.Ceiling(targetBounds.Width), (int)Math.Ceiling(intersection.Right - targetBounds.Left)),
                    Math.Min((int)Math.Ceiling(targetBounds.Height), (int)Math.Ceiling(intersection.Bottom - targetBounds.Top)));

                if (cutout != IntPtr.Zero)
                {
                    NativeMethods.CombineRgn(fullRegion, fullRegion, cutout, NativeMethods.RGN_DIFF);
                    NativeMethods.DeleteObject(cutout);
                }
            }
        }

        if (NativeMethods.SetWindowRgn(_windowHandle, fullRegion, true) == 0)
        {
            NativeMethods.DeleteObject(fullRegion);
        }
    }
}
