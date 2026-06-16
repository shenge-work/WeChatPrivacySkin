using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;
using MediaPen = System.Windows.Media.Pen;
using WpfPoint = System.Windows.Point;

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

    public void UpdateFrom(PrivacyDecision decision, AppSettings settings, Rect? activeWeChatBounds)
    {
        var theme = ThemeCatalog.Get(settings.ThemePackId);
        ApplyTheme(settings, theme, decision);
        var overlayBounds = PositionOver(decision.Window, settings, theme);
        ApplyActiveWindowCutout(overlayBounds, activeWeChatBounds);
    }

    private Rect PositionOver(WeChatWindowInfo target, AppSettings settings, ThemePack theme)
    {
        var dpi = NativeMethods.GetDpiForWindow(target.Handle);
        if (dpi == 0)
        {
            dpi = 96;
        }

        var scale = 96.0 / dpi;
        var outset = theme.Outset;
        var overlayBounds = new Rect(
            target.Bounds.Left - outset,
            target.Bounds.Top - outset,
            target.Bounds.Width + outset * 2,
            target.Bounds.Height + outset * 2);

        Width = Math.Max(1, overlayBounds.Width * scale);
        Height = Math.Max(1, overlayBounds.Height * scale);
        Left = overlayBounds.Left * scale;
        Top = overlayBounds.Top * scale;

        if (_windowHandle == IntPtr.Zero)
        {
            return overlayBounds;
        }

        var zOrder = settings.OverlayAlwaysOnTop
            ? NativeMethods.HWND_TOPMOST
            : NativeMethods.HWND_NOTOPMOST;

        NativeMethods.SetWindowPos(
            _windowHandle,
            zOrder,
            (int)Math.Round(overlayBounds.Left),
            (int)Math.Round(overlayBounds.Top),
            (int)Math.Round(overlayBounds.Width),
            (int)Math.Round(overlayBounds.Height),
            NativeMethods.SWP_NOACTIVATE |
            NativeMethods.SWP_SHOWWINDOW |
            NativeMethods.SWP_NOOWNERZORDER);

        return overlayBounds;
    }

    private void ApplyTheme(AppSettings settings, ThemePack theme, PrivacyDecision decision)
    {
        var mode = settings.Privacy.Mode;
        var boost = mode is PrivacyMode.MeetingShare or PrivacyMode.AwayCover or PrivacyMode.CleanScreen ? 0.12 : 0;
        var overlayOpacity = (byte)Math.Round(Math.Clamp(settings.OverlayOpacity + boost, 0.35, 0.98) * 255);
        var panelOpacity = (byte)Math.Round(Math.Clamp(settings.OverlayOpacity + 0.08 + boost, 0.45, 0.99) * 255);

        TintLayer.Fill = new SolidColorBrush(MediaColor.FromArgb(
            overlayOpacity,
            theme.OverlayColor.R,
            theme.OverlayColor.G,
            theme.OverlayColor.B));

        PatternLayer.Fill = CreatePatternBrush(theme);
        ImageLayer.Fill = CreateImageBrush(settings.BackgroundImagePath);

        Panel.Background = new SolidColorBrush(MediaColor.FromArgb(
            panelOpacity,
            theme.PanelColor.R,
            theme.PanelColor.G,
            theme.PanelColor.B));
        Panel.BorderBrush = new SolidColorBrush(MediaColor.FromArgb(210, theme.AccentColor.R, theme.AccentColor.G, theme.AccentColor.B));
        Panel.CornerRadius = new CornerRadius(theme.CornerRadius);

        OuterFrame.BorderBrush = new SolidColorBrush(MediaColor.FromArgb(190, theme.AccentColor.R, theme.AccentColor.G, theme.AccentColor.B));
        OuterFrame.CornerRadius = new CornerRadius(theme.CornerRadius + 8);
        OuterFrame.BorderThickness = new Thickness(theme.FrameThickness);

        CornerSticker.Background = new SolidColorBrush(MediaColor.FromArgb(230, theme.DecorationColor.R, theme.DecorationColor.G, theme.DecorationColor.B));
        CornerSticker.CornerRadius = new CornerRadius(Math.Max(8, theme.CornerRadius));
        CornerStickerText.Text = theme.BadgeText;
        CornerStickerText.Foreground = new SolidColorBrush(theme.PrimaryTextColor);

        StatusDot.Background = new SolidColorBrush(theme.AccentColor);
        TitleText.Text = ModeTitle(mode);
        SubtitleText.Text = decision.Reason + ModeSuffix(mode, settings);
        TitleText.Foreground = new SolidColorBrush(theme.PrimaryTextColor);
        SubtitleText.Foreground = new SolidColorBrush(theme.SecondaryTextColor);
        WatermarkText.Text = $"{theme.DisplayName} · {PrivacyModeCatalog.DisplayName(mode)}";
        WatermarkText.Foreground = new SolidColorBrush(theme.SecondaryTextColor);

        foreach (var shape in PlaceholderShapes())
        {
            shape.Background = new SolidColorBrush(MediaColor.FromArgb(
                180,
                theme.PlaceholderColor.R,
                theme.PlaceholderColor.G,
                theme.PlaceholderColor.B));
        }
    }

    private static string ModeTitle(PrivacyMode mode) => mode switch
    {
        PrivacyMode.MeetingShare => "会议共享保护",
        PrivacyMode.AwayCover => "离席保护",
        PrivacyMode.CleanScreen => "快速净屏",
        PrivacyMode.FocusChat => "专注聊天",
        _ => "微信隐私守护"
    };

    private static string ModeSuffix(PrivacyMode mode, AppSettings settings)
    {
        if (mode == PrivacyMode.MeetingShare && settings.Privacy.ShowMeetingWarning)
        {
            return "。共享单个微信窗口可能绕过外层遮罩。";
        }

        return string.Empty;
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

    private static MediaBrush CreatePatternBrush(ThemePack theme)
    {
        var group = new DrawingGroup();
        var transparent = new SolidColorBrush(Colors.Transparent);
        var accent = new SolidColorBrush(MediaColor.FromArgb(46, theme.AccentColor.R, theme.AccentColor.G, theme.AccentColor.B));
        var secondary = new SolidColorBrush(MediaColor.FromArgb(36, theme.SecondaryAccentColor.R, theme.SecondaryAccentColor.G, theme.SecondaryAccentColor.B));
        group.Children.Add(new GeometryDrawing(transparent, null, new RectangleGeometry(new Rect(0, 0, 24, 24))));

        switch (theme.Pattern)
        {
            case ThemePatternKind.Dots:
                group.Children.Add(new GeometryDrawing(accent, null, new EllipseGeometry(new WpfPoint(6, 6), 2.2, 2.2)));
                group.Children.Add(new GeometryDrawing(secondary, null, new EllipseGeometry(new WpfPoint(18, 18), 1.8, 1.8)));
                break;
            case ThemePatternKind.Diagonal:
            case ThemePatternKind.Neon:
                group.Children.Add(new GeometryDrawing(null, new MediaPen(accent, 2), new LineGeometry(new WpfPoint(0, 24), new WpfPoint(24, 0))));
                group.Children.Add(new GeometryDrawing(null, new MediaPen(secondary, 1), new LineGeometry(new WpfPoint(-8, 24), new WpfPoint(24, -8))));
                break;
            case ThemePatternKind.Pixels:
                group.Children.Add(new GeometryDrawing(accent, null, new RectangleGeometry(new Rect(0, 0, 8, 8))));
                group.Children.Add(new GeometryDrawing(secondary, null, new RectangleGeometry(new Rect(16, 16, 8, 8))));
                break;
            case ThemePatternKind.Paper:
                group.Children.Add(new GeometryDrawing(null, new MediaPen(accent, 0.8), new LineGeometry(new WpfPoint(0, 7), new WpfPoint(24, 8))));
                group.Children.Add(new GeometryDrawing(null, new MediaPen(secondary, 0.8), new LineGeometry(new WpfPoint(0, 18), new WpfPoint(24, 17))));
                break;
            default:
                group.Children.Add(new GeometryDrawing(accent, null, new RectangleGeometry(new Rect(0, 0, 12, 12))));
                group.Children.Add(new GeometryDrawing(secondary, null, new RectangleGeometry(new Rect(12, 12, 12, 12))));
                break;
        }

        var brush = new DrawingBrush(group)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, 24, 24),
            ViewportUnits = BrushMappingMode.Absolute,
            Opacity = 0.6
        };
        brush.Freeze();
        return brush;
    }

    private void ApplyActiveWindowCutout(Rect overlayBounds, Rect? activeBounds)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        var fullRegion = NativeMethods.CreateRectRgn(
            0,
            0,
            Math.Max(1, (int)Math.Round(overlayBounds.Width)),
            Math.Max(1, (int)Math.Round(overlayBounds.Height)));

        if (fullRegion == IntPtr.Zero)
        {
            return;
        }

        if (activeBounds is not null)
        {
            var intersection = Rect.Intersect(overlayBounds, activeBounds.Value);
            if (!intersection.IsEmpty && intersection.Width > 0 && intersection.Height > 0)
            {
                var cutout = NativeMethods.CreateRectRgn(
                    Math.Max(0, (int)Math.Floor(intersection.Left - overlayBounds.Left)),
                    Math.Max(0, (int)Math.Floor(intersection.Top - overlayBounds.Top)),
                    Math.Min((int)Math.Ceiling(overlayBounds.Width), (int)Math.Ceiling(intersection.Right - overlayBounds.Left)),
                    Math.Min((int)Math.Ceiling(overlayBounds.Height), (int)Math.Ceiling(intersection.Bottom - overlayBounds.Top)));

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
