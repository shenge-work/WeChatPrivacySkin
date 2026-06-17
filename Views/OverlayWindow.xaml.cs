using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;
using MediaPen = System.Windows.Media.Pen;
using ShapeEllipse = System.Windows.Shapes.Ellipse;
using ShapePath = System.Windows.Shapes.Path;
using ShapeRectangle = System.Windows.Shapes.Rectangle;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfPoint = System.Windows.Point;

namespace WeChatPrivacySkin;

public partial class OverlayWindow : Window
{
    private IntPtr _windowHandle;

    public OverlayWindow(IntPtr targetHandle)
    {
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

    public void UpdateFrom(
        PrivacyDecision decision,
        AppSettings settings,
        Rect? activeWeChatBounds,
        RevealZone? revealZone = null)
    {
        var theme = ThemeCatalog.Get(settings.ThemePackId);
        var skin = OverlaySkinCatalog.Get(settings.OverlaySkinId);
        var zone = revealZone ?? RevealZone.None;
        var placement = PositionOver(decision.Window, settings, theme);
        ApplyTheme(settings, theme, skin, decision, placement, zone);
        ApplyWindowRegion(placement.OverlayBoundsPx, decision.Window.Bounds, activeWeChatBounds, zone);
    }

    private OverlayPlacement PositionOver(WeChatWindowInfo target, AppSettings settings, ThemePack theme)
    {
        var outset = theme.Outset;
        var overlayBounds = new Rect(
            target.Bounds.Left - outset,
            target.Bounds.Top - outset,
            target.Bounds.Width + outset * 2,
            target.Bounds.Height + outset * 2);

        if (_windowHandle != IntPtr.Zero)
        {
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
        }

        var scale = GetDipScale(_windowHandle, target.Handle);
        Width = Math.Max(1, overlayBounds.Width * scale);
        Height = Math.Max(1, overlayBounds.Height * scale);

        if (_windowHandle == IntPtr.Zero)
        {
            Left = overlayBounds.Left * scale;
            Top = overlayBounds.Top * scale;
        }

        var targetBoundsDip = new Rect(
            outset * scale,
            outset * scale,
            target.Bounds.Width * scale,
            target.Bounds.Height * scale);

        return new OverlayPlacement(overlayBounds, targetBoundsDip, scale);
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

    private void ApplyTheme(
        AppSettings settings,
        ThemePack theme,
        OverlaySkin skin,
        PrivacyDecision decision,
        OverlayPlacement placement,
        RevealZone revealZone)
    {
        var mode = settings.Privacy.Mode;
        var boost = mode is PrivacyMode.MeetingShare or PrivacyMode.AwayCover or PrivacyMode.CleanScreen ? 0.12 : 0;
        var overlayOpacity = (byte)Math.Round(Math.Clamp(settings.OverlayOpacity + boost, 0.35, 0.98) * 255);
        var panelOpacity = (byte)Math.Round(Math.Clamp(settings.OverlayOpacity + 0.08 + boost, 0.45, 0.99) * 255);

        ImageLayer.Fill = CreateImageBrush(settings.BackgroundImagePath);
        PatternLayer.Fill = CreatePatternBrush(theme);
        TintLayer.Fill = CreateTintBrush(theme, overlayOpacity);

        ShapeCanvas.Children.Clear();
        SkeletonCanvas.Children.Clear();
        DecorationCanvas.Children.Clear();
        SkinCanvas.Children.Clear();

        if (skin.RenderKind == OverlaySkinRenderKind.ThemeShape)
        {
            DrawShapeChrome(theme, placement, panelOpacity);
        }

        if (decision.Window.IsUtilityLike)
        {
            DrawUtilitySkeleton(theme, placement, revealZone);
        }
        else
        {
            DrawWeChatSkeleton(theme, placement, revealZone);
        }

        DrawCaption(theme, mode, decision, placement);
        DrawDecorations(theme, placement, settings.DecorativeMotionEnabled && theme.MotionDefault);
        OverlaySkinRenderer.Draw(SkinCanvas, skin, theme, settings.CustomSkinImagePath, placement.TargetBoundsDip, placement.Scale, Width, Height);
        ApplyCornerSticker(theme);
        ApplyRevealHint(theme, placement, revealZone);
    }

    private void DrawShapeChrome(ThemePack theme, OverlayPlacement placement, byte panelOpacity)
    {
        var chromeRect = placement.TargetBoundsDip;
        chromeRect.Inflate(Math.Max(3, theme.Outset * placement.Scale * 0.62), Math.Max(3, theme.Outset * placement.Scale * 0.62));
        chromeRect = ClampToWindow(chromeRect, placement);

        var shape = new ShapePath
        {
            Data = CreateShapeGeometry(theme.ShapeKind, chromeRect, theme.CornerRadius * placement.Scale),
            Fill = new SolidColorBrush(MediaColor.FromArgb(panelOpacity, theme.PanelColor.R, theme.PanelColor.G, theme.PanelColor.B)),
            Stroke = new SolidColorBrush(MediaColor.FromArgb(210, theme.AccentColor.R, theme.AccentColor.G, theme.AccentColor.B)),
            StrokeThickness = Math.Max(1, theme.FrameThickness * placement.Scale),
            StrokeLineJoin = PenLineJoin.Round,
            SnapsToDevicePixels = true
        };
        ShapeCanvas.Children.Add(shape);

        var glowRect = chromeRect;
        glowRect.Inflate(Math.Max(4, theme.Outset * placement.Scale * 0.24), Math.Max(4, theme.Outset * placement.Scale * 0.24));
        glowRect = ClampToWindow(glowRect, placement);

        var glow = new ShapePath
        {
            Data = CreateShapeGeometry(theme.ShapeKind, glowRect, (theme.CornerRadius + 8) * placement.Scale),
            Fill = WpfBrushes.Transparent,
            Stroke = new SolidColorBrush(MediaColor.FromArgb(92, theme.SecondaryAccentColor.R, theme.SecondaryAccentColor.G, theme.SecondaryAccentColor.B)),
            StrokeThickness = Math.Max(1, theme.FrameThickness * placement.Scale * 1.15),
            StrokeLineJoin = PenLineJoin.Round
        };
        ShapeCanvas.Children.Add(glow);

        if (theme.ShapeKind == OverlayShapeKind.Sponge)
        {
            DrawSpongePores(chromeRect, theme, placement.Scale);
        }
    }

    private void DrawWeChatSkeleton(ThemePack theme, OverlayPlacement placement, RevealZone revealZone)
    {
        var bounds = placement.TargetBoundsDip;
        var scale = placement.Scale;
        var sideWidth = Clamp(bounds.Width * 0.28, 118 * scale, Math.Max(120 * scale, bounds.Width * 0.36));
        var titleHeight = Clamp(bounds.Height * 0.12, 44 * scale, 72 * scale);
        var inputHeight = Clamp(bounds.Height * 0.20, 82 * scale, 150 * scale);
        var contentWidth = Math.Max(1, bounds.Width - sideWidth);
        var messageHeight = Math.Max(42 * scale, bounds.Height - titleHeight - inputHeight);

        var sideRect = new Rect(bounds.Left, bounds.Top, sideWidth, bounds.Height);
        var titleRect = new Rect(bounds.Left + sideWidth, bounds.Top, contentWidth, titleHeight);
        var messageRect = new Rect(bounds.Left + sideWidth, bounds.Top + titleHeight, contentWidth, messageHeight);
        var inputRect = new Rect(bounds.Left + sideWidth, bounds.Top + titleHeight + messageHeight, contentWidth, inputHeight);

        AddRoundedRect(SkeletonCanvas, sideRect, theme.CornerRadius * scale, Fill(theme.BackgroundColor, 118), null, 0);
        AddRoundedRect(SkeletonCanvas, titleRect, theme.CornerRadius * scale, Fill(theme.PanelColor, 138), null, 0);
        AddRoundedRect(SkeletonCanvas, messageRect, 0, Fill(theme.BackgroundColor, 68), null, 0);
        AddRoundedRect(SkeletonCanvas, inputRect, Math.Max(8, theme.CornerRadius * scale), Fill(theme.PanelColor, 128), null, 0);

        AddLine(SkeletonCanvas, bounds.Left + sideWidth, bounds.Top + 12 * scale, bounds.Left + sideWidth, bounds.Bottom - 12 * scale, theme.AccentColor, 74, Math.Max(1, scale));
        AddLine(SkeletonCanvas, titleRect.Left + 14 * scale, titleRect.Bottom, titleRect.Right - 14 * scale, titleRect.Bottom, theme.AccentColor, 58, Math.Max(1, scale));
        AddLine(SkeletonCanvas, inputRect.Left + 14 * scale, inputRect.Top, inputRect.Right - 14 * scale, inputRect.Top, theme.AccentColor, 58, Math.Max(1, scale));

        DrawConversationRows(theme, sideRect, scale);
        DrawTitleSkeleton(theme, titleRect, scale);
        DrawMessageSkeleton(theme, messageRect, scale);
        DrawInputSkeleton(theme, inputRect, scale);
        DrawRevealChrome(theme, placement, revealZone);
    }

    private void DrawUtilitySkeleton(ThemePack theme, OverlayPlacement placement, RevealZone revealZone)
    {
        var bounds = placement.TargetBoundsDip;
        var scale = placement.Scale;
        var titleHeight = Clamp(bounds.Height * 0.18, 34 * scale, 58 * scale);
        var footerHeight = Clamp(bounds.Height * 0.20, 38 * scale, 72 * scale);
        var bodyRect = new Rect(bounds.Left, bounds.Top + titleHeight, bounds.Width, Math.Max(20 * scale, bounds.Height - titleHeight - footerHeight));
        var footerRect = new Rect(bounds.Left, bodyRect.Bottom, bounds.Width, footerHeight);

        AddRoundedRect(SkeletonCanvas, bounds, theme.CornerRadius * scale, Fill(theme.PanelColor, 124), null, 0);
        AddRoundedRect(SkeletonCanvas, new Rect(bounds.Left, bounds.Top, bounds.Width, titleHeight), theme.CornerRadius * scale, Fill(theme.BackgroundColor, 94), null, 0);
        AddRoundedRect(SkeletonCanvas, bodyRect, 0, Fill(theme.BackgroundColor, 62), null, 0);
        AddRoundedRect(SkeletonCanvas, footerRect, theme.CornerRadius * scale, Fill(theme.PanelColor, 112), null, 0);

        AddRoundedRect(SkeletonCanvas, new Rect(bounds.Left + 18 * scale, bounds.Top + 14 * scale, bounds.Width * 0.46, 10 * scale), 5 * scale, Fill(theme.PlaceholderColor, 185), null, 0);
        AddRoundedRect(SkeletonCanvas, new Rect(bodyRect.Left + 20 * scale, bodyRect.Top + 22 * scale, bodyRect.Width * 0.72, 12 * scale), 6 * scale, Fill(theme.PlaceholderColor, 170), null, 0);
        AddRoundedRect(SkeletonCanvas, new Rect(bodyRect.Left + 20 * scale, bodyRect.Top + 46 * scale, bodyRect.Width * 0.56, 12 * scale), 6 * scale, Fill(theme.PlaceholderColor, 150), null, 0);
        AddRoundedRect(SkeletonCanvas, new Rect(footerRect.Right - 98 * scale, footerRect.Top + 16 * scale, 78 * scale, 22 * scale), 11 * scale, Fill(theme.AccentColor, 145), null, 0);
        DrawRevealChrome(theme, placement, revealZone);
    }

    private void DrawConversationRows(ThemePack theme, Rect sideRect, double scale)
    {
        var top = sideRect.Top + 18 * scale;
        var rowHeight = Clamp(sideRect.Height * 0.105, 48 * scale, 64 * scale);
        var rows = Math.Max(3, Math.Min(8, (int)((sideRect.Height - 28 * scale) / rowHeight)));

        for (var i = 0; i < rows; i++)
        {
            var rowTop = top + i * rowHeight;
            var rowOpacity = i == 1 ? (byte)92 : (byte)42;
            AddRoundedRect(
                SkeletonCanvas,
                new Rect(sideRect.Left + 8 * scale, rowTop, sideRect.Width - 16 * scale, rowHeight - 8 * scale),
                12 * scale,
                Fill(theme.PanelColor, rowOpacity),
                null,
                0);
            AddEllipse(
                SkeletonCanvas,
                new Rect(sideRect.Left + 18 * scale, rowTop + 9 * scale, 30 * scale, 30 * scale),
                Fill(theme.PlaceholderColor, 170));
            AddRoundedRect(
                SkeletonCanvas,
                new Rect(sideRect.Left + 58 * scale, rowTop + 12 * scale, sideRect.Width * 0.46, 8 * scale),
                4 * scale,
                Fill(theme.PlaceholderColor, 155),
                null,
                0);
            AddRoundedRect(
                SkeletonCanvas,
                new Rect(sideRect.Left + 58 * scale, rowTop + 28 * scale, sideRect.Width * 0.34, 7 * scale),
                3.5 * scale,
                Fill(theme.PlaceholderColor, 98),
                null,
                0);
        }
    }

    private void DrawTitleSkeleton(ThemePack theme, Rect titleRect, double scale)
    {
        AddRoundedRect(
            SkeletonCanvas,
            new Rect(titleRect.Left + 24 * scale, titleRect.Top + 18 * scale, Math.Min(titleRect.Width * 0.36, 210 * scale), 12 * scale),
            6 * scale,
            Fill(theme.PlaceholderColor, 170),
            null,
            0);

        for (var i = 0; i < 3; i++)
        {
            AddEllipse(
                SkeletonCanvas,
                new Rect(titleRect.Right - (76 - i * 22) * scale, titleRect.Top + 18 * scale, 10 * scale, 10 * scale),
                Fill(theme.SecondaryAccentColor, 112));
        }
    }

    private void DrawMessageSkeleton(ThemePack theme, Rect messageRect, double scale)
    {
        var bubbleMax = Math.Max(80 * scale, messageRect.Width * 0.52);
        var y = messageRect.Top + 26 * scale;
        var heights = new[] { 34, 42, 30, 50, 36 };

        for (var i = 0; i < heights.Length && y < messageRect.Bottom - 34 * scale; i++)
        {
            var fromLeft = i % 2 == 0;
            var width = Math.Min(bubbleMax, messageRect.Width * (0.35 + (i % 3) * 0.08));
            var height = heights[i] * scale;
            var x = fromLeft ? messageRect.Left + 26 * scale : messageRect.Right - width - 28 * scale;
            var fill = fromLeft ? Fill(theme.PanelColor, 142) : Fill(theme.SecondaryAccentColor, 102);
            AddRoundedRect(SkeletonCanvas, new Rect(x, y, width, height), 16 * scale, fill, null, 0);
            AddRoundedRect(SkeletonCanvas, new Rect(x + 14 * scale, y + 12 * scale, width * 0.62, 7 * scale), 3.5 * scale, Fill(theme.PlaceholderColor, 128), null, 0);
            if (height > 34 * scale)
            {
                AddRoundedRect(SkeletonCanvas, new Rect(x + 14 * scale, y + 25 * scale, width * 0.42, 7 * scale), 3.5 * scale, Fill(theme.PlaceholderColor, 92), null, 0);
            }

            y += height + 18 * scale;
        }
    }

    private void DrawInputSkeleton(ThemePack theme, Rect inputRect, double scale)
    {
        var iconTop = inputRect.Top + 16 * scale;
        for (var i = 0; i < 5; i++)
        {
            AddEllipse(
                SkeletonCanvas,
                new Rect(inputRect.Left + (24 + i * 26) * scale, iconTop, 12 * scale, 12 * scale),
                Fill(theme.AccentColor, 104));
        }

        AddRoundedRect(
            SkeletonCanvas,
            new Rect(inputRect.Left + 24 * scale, inputRect.Top + 44 * scale, inputRect.Width - 48 * scale, Math.Max(18 * scale, inputRect.Height - 60 * scale)),
            12 * scale,
            Fill(theme.BackgroundColor, 88),
            null,
            0);
    }

    private void DrawCaption(ThemePack theme, PrivacyMode mode, PrivacyDecision decision, OverlayPlacement placement)
    {
        var scale = placement.Scale;
        var text = new TextBlock
        {
            Text = $"{ModeTitle(mode)} · {decision.Window.Kind}",
            FontSize = Math.Max(11, 12 * scale),
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(theme.PrimaryTextColor),
            Opacity = 0.82
        };
        Canvas.SetLeft(text, placement.TargetBoundsDip.Left + 16 * scale);
        Canvas.SetTop(text, placement.TargetBoundsDip.Bottom - 28 * scale);
        SkeletonCanvas.Children.Add(text);
    }

    private void DrawRevealChrome(ThemePack theme, OverlayPlacement placement, RevealZone revealZone)
    {
        if (!revealZone.IsActive)
        {
            return;
        }

        var rect = revealZone.ToAbsolute(placement.TargetBoundsDip);
        if (rect.IsEmpty)
        {
            return;
        }

        var strokeColor = theme.RevealStyle == RevealStyleKind.WaterGlow
            ? theme.SecondaryAccentColor
            : theme.AccentColor;
        var strokeThickness = Math.Max(2, theme.FrameThickness * placement.Scale);
        var outer = rect;
        outer.Inflate(strokeThickness * 1.6, strokeThickness * 1.6);

        AddRoundedRect(
            SkeletonCanvas,
            outer,
            Math.Max(6, revealZone.CornerRadius * placement.Scale),
            WpfBrushes.Transparent,
            new SolidColorBrush(MediaColor.FromArgb(205, strokeColor.R, strokeColor.G, strokeColor.B)),
            strokeThickness);
    }

    private void DrawDecorations(ThemePack theme, OverlayPlacement placement, bool motionEnabled)
    {
        if (theme.ShapeKind != OverlayShapeKind.Sponge)
        {
            return;
        }

        var scale = placement.Scale;
        var bounds = placement.TargetBoundsDip;
        var oceanBrush = new SolidColorBrush(MediaColor.FromArgb(112, theme.SecondaryAccentColor.R, theme.SecondaryAccentColor.G, theme.SecondaryAccentColor.B));

        for (var i = 0; i < 10; i++)
        {
            var size = (6 + (i % 4) * 5) * scale;
            var left = (12 + i * 37) * scale % Math.Max(1, Width - size);
            var top = (18 + i * 53) * scale % Math.Max(1, Height - size);
            var bubble = new ShapeEllipse
            {
                Width = size,
                Height = size,
                Fill = WpfBrushes.Transparent,
                Stroke = oceanBrush,
                StrokeThickness = Math.Max(1, scale),
                Opacity = motionEnabled ? 0.72 : 0.48
            };
            Canvas.SetLeft(bubble, left);
            Canvas.SetTop(bubble, top);
            DecorationCanvas.Children.Add(bubble);
        }

        var coral = new ShapePath
        {
            Data = CreateCoralGeometry(new WpfPoint(bounds.Left + 18 * scale, bounds.Bottom - 8 * scale), scale),
            Fill = WpfBrushes.Transparent,
            Stroke = new SolidColorBrush(MediaColor.FromArgb(210, theme.DecorationColor.R, theme.DecorationColor.G, theme.DecorationColor.B)),
            StrokeThickness = Math.Max(3, 3 * scale),
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round
        };
        DecorationCanvas.Children.Add(coral);

        var star = new ShapePath
        {
            Data = CreateStarGeometry(new WpfPoint(bounds.Right - 26 * scale, bounds.Top + 24 * scale), 16 * scale, 7 * scale, 5),
            Fill = new SolidColorBrush(MediaColor.FromArgb(220, theme.DecorationColor.R, theme.DecorationColor.G, theme.DecorationColor.B)),
            Stroke = new SolidColorBrush(MediaColor.FromArgb(190, theme.PrimaryTextColor.R, theme.PrimaryTextColor.G, theme.PrimaryTextColor.B)),
            StrokeThickness = Math.Max(0.8, scale)
        };
        DecorationCanvas.Children.Add(star);

        var wave = new ShapePath
        {
            Data = CreateWaveGeometry(new Rect(bounds.Left, bounds.Bottom - 18 * scale, bounds.Width, 12 * scale), scale),
            Fill = WpfBrushes.Transparent,
            Stroke = new SolidColorBrush(MediaColor.FromArgb(156, theme.SecondaryAccentColor.R, theme.SecondaryAccentColor.G, theme.SecondaryAccentColor.B)),
            StrokeThickness = Math.Max(2, 2 * scale),
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        DecorationCanvas.Children.Add(wave);
    }

    private void DrawSpongePores(Rect chromeRect, ThemePack theme, double scale)
    {
        var poreBrush = new SolidColorBrush(MediaColor.FromArgb(74, 152, 115, 38));
        var poreStroke = new SolidColorBrush(MediaColor.FromArgb(88, theme.AccentColor.R, theme.AccentColor.G, theme.AccentColor.B));
        var poreData = new[]
        {
            (0.16, 0.18, 12.0),
            (0.38, 0.12, 8.0),
            (0.68, 0.20, 14.0),
            (0.84, 0.36, 9.0),
            (0.24, 0.46, 15.0),
            (0.52, 0.52, 10.0),
            (0.72, 0.66, 13.0),
            (0.32, 0.76, 9.0)
        };

        foreach (var (xRatio, yRatio, radius) in poreData)
        {
            var size = radius * scale;
            AddEllipse(
                ShapeCanvas,
                new Rect(
                    chromeRect.Left + chromeRect.Width * xRatio,
                    chromeRect.Top + chromeRect.Height * yRatio,
                    size,
                    size * 0.78),
                poreBrush,
                poreStroke,
                Math.Max(0.8, scale));
        }
    }

    private void ApplyCornerSticker(ThemePack theme)
    {
        CornerSticker.Background = new SolidColorBrush(MediaColor.FromArgb(232, theme.DecorationColor.R, theme.DecorationColor.G, theme.DecorationColor.B));
        CornerSticker.CornerRadius = new CornerRadius(Math.Max(8, theme.CornerRadius));
        CornerStickerText.Text = theme.BadgeText;
        CornerStickerText.Foreground = new SolidColorBrush(theme.PrimaryTextColor);
    }

    private void ApplyRevealHint(ThemePack theme, OverlayPlacement placement, RevealZone revealZone)
    {
        if (!revealZone.IsActive)
        {
            RevealHint.Visibility = Visibility.Collapsed;
            return;
        }

        var rect = revealZone.ToAbsolute(placement.TargetBoundsDip);
        RevealHint.Background = new SolidColorBrush(MediaColor.FromArgb(230, theme.PanelColor.R, theme.PanelColor.G, theme.PanelColor.B));
        RevealHint.BorderBrush = new SolidColorBrush(MediaColor.FromArgb(220, theme.AccentColor.R, theme.AccentColor.G, theme.AccentColor.B));
        RevealHint.BorderThickness = new Thickness(Math.Max(1, placement.Scale));
        RevealHintText.Text = revealZone.Hint;
        RevealHintText.Foreground = new SolidColorBrush(theme.PrimaryTextColor);

        RevealHint.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        var left = Math.Clamp(rect.Left + 8 * placement.Scale, 8, Math.Max(8, Width - RevealHint.DesiredSize.Width - 8));
        var top = rect.Top >= 42 * placement.Scale
            ? rect.Top - 38 * placement.Scale
            : Math.Min(rect.Bottom + 8 * placement.Scale, Math.Max(8, Height - RevealHint.DesiredSize.Height - 8));
        Canvas.SetLeft(RevealHint, left);
        Canvas.SetTop(RevealHint, top);
        RevealHint.Visibility = Visibility.Visible;
    }

    private static Geometry CreateShapeGeometry(OverlayShapeKind shapeKind, Rect rect, double radius)
    {
        return shapeKind switch
        {
            OverlayShapeKind.Bubble => CreateBubbleGeometry(rect, radius),
            OverlayShapeKind.Cloud => CreateCloudGeometry(rect),
            OverlayShapeKind.Sponge => CreateSpongeGeometry(rect),
            OverlayShapeKind.PixelBlock => CreatePixelBlockGeometry(rect),
            _ => new RectangleGeometry(rect, radius, radius)
        };
    }

    private static Geometry CreateBubbleGeometry(Rect rect, double radius)
    {
        var group = new GeometryGroup { FillRule = FillRule.Nonzero };
        group.Children.Add(new RectangleGeometry(rect, radius, radius));
        group.Children.Add(new EllipseGeometry(new WpfPoint(rect.Left + rect.Width * 0.18, rect.Bottom - rect.Height * 0.04), rect.Width * 0.055, rect.Height * 0.055));
        group.Children.Add(new EllipseGeometry(new WpfPoint(rect.Left + rect.Width * 0.09, rect.Bottom + rect.Height * 0.02), rect.Width * 0.035, rect.Height * 0.035));
        return group;
    }

    private static Geometry CreateCloudGeometry(Rect rect)
    {
        var group = new GeometryGroup { FillRule = FillRule.Nonzero };
        group.Children.Add(new RectangleGeometry(new Rect(rect.Left + rect.Width * 0.08, rect.Top + rect.Height * 0.30, rect.Width * 0.84, rect.Height * 0.54), rect.Height * 0.16, rect.Height * 0.16));
        group.Children.Add(new EllipseGeometry(new WpfPoint(rect.Left + rect.Width * 0.22, rect.Top + rect.Height * 0.34), rect.Width * 0.18, rect.Height * 0.24));
        group.Children.Add(new EllipseGeometry(new WpfPoint(rect.Left + rect.Width * 0.48, rect.Top + rect.Height * 0.24), rect.Width * 0.23, rect.Height * 0.30));
        group.Children.Add(new EllipseGeometry(new WpfPoint(rect.Left + rect.Width * 0.72, rect.Top + rect.Height * 0.36), rect.Width * 0.18, rect.Height * 0.22));
        return group;
    }

    private static Geometry CreateSpongeGeometry(Rect rect)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(new WpfPoint(rect.Left + rect.Width * 0.08, rect.Top + rect.Height * 0.08), true, true);
        context.BezierTo(
            new WpfPoint(rect.Left + rect.Width * 0.20, rect.Top - rect.Height * 0.02),
            new WpfPoint(rect.Left + rect.Width * 0.38, rect.Top + rect.Height * 0.06),
            new WpfPoint(rect.Left + rect.Width * 0.50, rect.Top + rect.Height * 0.03),
            true,
            false);
        context.BezierTo(
            new WpfPoint(rect.Left + rect.Width * 0.72, rect.Top - rect.Height * 0.02),
            new WpfPoint(rect.Right - rect.Width * 0.02, rect.Top + rect.Height * 0.12),
            new WpfPoint(rect.Right - rect.Width * 0.06, rect.Top + rect.Height * 0.28),
            true,
            false);
        context.BezierTo(
            new WpfPoint(rect.Right + rect.Width * 0.02, rect.Top + rect.Height * 0.44),
            new WpfPoint(rect.Right - rect.Width * 0.10, rect.Top + rect.Height * 0.58),
            new WpfPoint(rect.Right - rect.Width * 0.05, rect.Top + rect.Height * 0.76),
            true,
            false);
        context.BezierTo(
            new WpfPoint(rect.Right - rect.Width * 0.16, rect.Bottom + rect.Height * 0.06),
            new WpfPoint(rect.Left + rect.Width * 0.64, rect.Bottom - rect.Height * 0.02),
            new WpfPoint(rect.Left + rect.Width * 0.50, rect.Bottom - rect.Height * 0.04),
            true,
            false);
        context.BezierTo(
            new WpfPoint(rect.Left + rect.Width * 0.32, rect.Bottom + rect.Height * 0.04),
            new WpfPoint(rect.Left + rect.Width * 0.08, rect.Bottom - rect.Height * 0.10),
            new WpfPoint(rect.Left + rect.Width * 0.10, rect.Bottom - rect.Height * 0.26),
            true,
            false);
        context.BezierTo(
            new WpfPoint(rect.Left - rect.Width * 0.02, rect.Top + rect.Height * 0.62),
            new WpfPoint(rect.Left + rect.Width * 0.05, rect.Top + rect.Height * 0.34),
            new WpfPoint(rect.Left + rect.Width * 0.08, rect.Top + rect.Height * 0.08),
            true,
            false);
        geometry.Freeze();
        return geometry;
    }

    private static Geometry CreatePixelBlockGeometry(Rect rect)
    {
        var group = new GeometryGroup { FillRule = FillRule.Nonzero };
        var unit = Math.Max(4, Math.Min(rect.Width, rect.Height) * 0.055);
        group.Children.Add(new RectangleGeometry(new Rect(rect.Left + unit, rect.Top, rect.Width - unit * 2, rect.Height)));
        group.Children.Add(new RectangleGeometry(new Rect(rect.Left, rect.Top + unit, rect.Width, rect.Height - unit * 2)));
        return group;
    }

    private static Geometry CreateCoralGeometry(WpfPoint anchor, double scale)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(anchor, false, false);
        context.LineTo(new WpfPoint(anchor.X + 4 * scale, anchor.Y - 32 * scale), true, false);
        context.LineTo(new WpfPoint(anchor.X - 10 * scale, anchor.Y - 48 * scale), true, false);
        context.BeginFigure(new WpfPoint(anchor.X + 4 * scale, anchor.Y - 28 * scale), false, false);
        context.LineTo(new WpfPoint(anchor.X + 22 * scale, anchor.Y - 42 * scale), true, false);
        context.LineTo(new WpfPoint(anchor.X + 18 * scale, anchor.Y - 60 * scale), true, false);
        context.BeginFigure(new WpfPoint(anchor.X + 2 * scale, anchor.Y - 18 * scale), false, false);
        context.LineTo(new WpfPoint(anchor.X - 18 * scale, anchor.Y - 28 * scale), true, false);
        geometry.Freeze();
        return geometry;
    }

    private static Geometry CreateStarGeometry(WpfPoint center, double outerRadius, double innerRadius, int points)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        for (var i = 0; i < points * 2; i++)
        {
            var radius = i % 2 == 0 ? outerRadius : innerRadius;
            var angle = -Math.PI / 2 + i * Math.PI / points;
            var point = new WpfPoint(center.X + Math.Cos(angle) * radius, center.Y + Math.Sin(angle) * radius);
            if (i == 0)
            {
                context.BeginFigure(point, true, true);
            }
            else
            {
                context.LineTo(point, true, false);
            }
        }

        geometry.Freeze();
        return geometry;
    }

    private static Geometry CreateWaveGeometry(Rect rect, double scale)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        var y = rect.Top + rect.Height * 0.5;
        context.BeginFigure(new WpfPoint(rect.Left, y), false, false);
        var step = 38 * scale;
        for (var x = rect.Left; x < rect.Right; x += step)
        {
            context.BezierTo(
                new WpfPoint(x + step * 0.25, y - rect.Height * 0.8),
                new WpfPoint(x + step * 0.75, y + rect.Height * 0.8),
                new WpfPoint(x + step, y),
                true,
                false);
        }

        geometry.Freeze();
        return geometry;
    }

    private static MediaBrush CreateTintBrush(ThemePack theme, byte overlayOpacity)
    {
        if (string.Equals(theme.BackgroundStyle, "underwater", StringComparison.OrdinalIgnoreCase))
        {
            return new LinearGradientBrush(
                new GradientStopCollection
                {
                    new(MediaColor.FromArgb(overlayOpacity, 34, 178, 205), 0),
                    new(MediaColor.FromArgb(overlayOpacity, theme.BackgroundColor.R, theme.BackgroundColor.G, theme.BackgroundColor.B), 0.52),
                    new(MediaColor.FromArgb(overlayOpacity, 12, 92, 128), 1)
                },
                90);
        }

        return new SolidColorBrush(MediaColor.FromArgb(
            overlayOpacity,
            theme.OverlayColor.R,
            theme.OverlayColor.G,
            theme.OverlayColor.B));
    }

    private static MediaBrush CreateImageBrush(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return WpfBrushes.Transparent;
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
            return WpfBrushes.Transparent;
        }
    }

    private static MediaBrush CreatePatternBrush(ThemePack theme)
    {
        var group = new DrawingGroup();
        var transparent = new SolidColorBrush(Colors.Transparent);
        var accent = new SolidColorBrush(MediaColor.FromArgb(52, theme.AccentColor.R, theme.AccentColor.G, theme.AccentColor.B));
        var secondary = new SolidColorBrush(MediaColor.FromArgb(42, theme.SecondaryAccentColor.R, theme.SecondaryAccentColor.G, theme.SecondaryAccentColor.B));
        group.Children.Add(new GeometryDrawing(transparent, null, new RectangleGeometry(new Rect(0, 0, 28, 28))));

        switch (theme.Pattern)
        {
            case ThemePatternKind.Bubbles:
                group.Children.Add(new GeometryDrawing(null, new MediaPen(accent, 1.2), new EllipseGeometry(new WpfPoint(7, 7), 3.4, 3.4)));
                group.Children.Add(new GeometryDrawing(null, new MediaPen(secondary, 1), new EllipseGeometry(new WpfPoint(21, 19), 4.6, 4.6)));
                group.Children.Add(new GeometryDrawing(secondary, null, new EllipseGeometry(new WpfPoint(16, 6), 1.4, 1.4)));
                break;
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
            Viewport = new Rect(0, 0, 28, 28),
            ViewportUnits = BrushMappingMode.Absolute,
            Opacity = theme.Pattern == ThemePatternKind.Bubbles ? 0.76 : 0.6
        };
        brush.Freeze();
        return brush;
    }

    private void ApplyWindowRegion(Rect overlayBounds, Rect targetBounds, Rect? activeBounds, RevealZone revealZone)
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
            SubtractRectRegion(fullRegion, overlayBounds, activeBounds.Value);
        }

        if (revealZone.IsActive)
        {
            var revealBounds = revealZone.ToAbsolute(targetBounds);
            SubtractRectRegion(fullRegion, overlayBounds, revealBounds);
        }

        if (NativeMethods.SetWindowRgn(_windowHandle, fullRegion, true) == 0)
        {
            NativeMethods.DeleteObject(fullRegion);
        }
    }

    private static void SubtractRectRegion(IntPtr baseRegion, Rect overlayBounds, Rect cutoutBounds)
    {
        var intersection = Rect.Intersect(overlayBounds, cutoutBounds);
        if (intersection.IsEmpty || intersection.Width <= 0 || intersection.Height <= 0)
        {
            return;
        }

        var cutout = NativeMethods.CreateRectRgn(
            Math.Max(0, (int)Math.Floor(intersection.Left - overlayBounds.Left)),
            Math.Max(0, (int)Math.Floor(intersection.Top - overlayBounds.Top)),
            Math.Min((int)Math.Ceiling(overlayBounds.Width), (int)Math.Ceiling(intersection.Right - overlayBounds.Left)),
            Math.Min((int)Math.Ceiling(overlayBounds.Height), (int)Math.Ceiling(intersection.Bottom - overlayBounds.Top)));

        if (cutout == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.CombineRgn(baseRegion, baseRegion, cutout, NativeMethods.RGN_DIFF);
        NativeMethods.DeleteObject(cutout);
    }

    private static void AddRoundedRect(
        Canvas canvas,
        Rect rect,
        double radius,
        MediaBrush fill,
        MediaBrush? stroke,
        double strokeThickness)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var shape = new ShapeRectangle
        {
            Width = rect.Width,
            Height = rect.Height,
            RadiusX = Math.Max(0, radius),
            RadiusY = Math.Max(0, radius),
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = strokeThickness,
            SnapsToDevicePixels = true
        };
        Canvas.SetLeft(shape, rect.Left);
        Canvas.SetTop(shape, rect.Top);
        canvas.Children.Add(shape);
    }

    private static void AddEllipse(
        Canvas canvas,
        Rect rect,
        MediaBrush fill,
        MediaBrush? stroke = null,
        double strokeThickness = 0)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var shape = new ShapeEllipse
        {
            Width = rect.Width,
            Height = rect.Height,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = strokeThickness
        };
        Canvas.SetLeft(shape, rect.Left);
        Canvas.SetTop(shape, rect.Top);
        canvas.Children.Add(shape);
    }

    private static void AddLine(Canvas canvas, double x1, double y1, double x2, double y2, MediaColor color, byte alpha, double thickness)
    {
        var path = new ShapePath
        {
            Data = new LineGeometry(new WpfPoint(x1, y1), new WpfPoint(x2, y2)),
            Stroke = new SolidColorBrush(MediaColor.FromArgb(alpha, color.R, color.G, color.B)),
            StrokeThickness = thickness
        };
        canvas.Children.Add(path);
    }

    private static MediaBrush Fill(MediaColor color, byte alpha)
    {
        return new SolidColorBrush(MediaColor.FromArgb(alpha, color.R, color.G, color.B));
    }

    private static Rect ClampToWindow(Rect rect, OverlayPlacement placement)
    {
        var bounds = new Rect(0, 0, Math.Max(1, placement.OverlayBoundsPx.Width * placement.Scale), Math.Max(1, placement.OverlayBoundsPx.Height * placement.Scale));
        var left = Math.Max(bounds.Left + 2, rect.Left);
        var top = Math.Max(bounds.Top + 2, rect.Top);
        var right = Math.Min(bounds.Right - 2, rect.Right);
        var bottom = Math.Min(bounds.Bottom - 2, rect.Bottom);
        return right <= left || bottom <= top ? bounds : new Rect(left, top, right - left, bottom - top);
    }

    private static double Clamp(double value, double min, double max)
    {
        if (max < min)
        {
            return min;
        }

        return Math.Min(Math.Max(value, min), max);
    }

    private static string ModeTitle(PrivacyMode mode) => mode switch
    {
        PrivacyMode.MeetingShare => "会议共享保护",
        PrivacyMode.AwayCover => "离席保护",
        PrivacyMode.CleanScreen => "快速净屏",
        PrivacyMode.FocusChat => "专注聊天",
        _ => "微信隐私守护"
    };

    private readonly record struct OverlayPlacement(Rect OverlayBoundsPx, Rect TargetBoundsDip, double Scale);
}
