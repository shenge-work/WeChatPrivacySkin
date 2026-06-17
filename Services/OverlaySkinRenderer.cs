using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;
using ShapeEllipse = System.Windows.Shapes.Ellipse;
using ShapePath = System.Windows.Shapes.Path;
using ShapePolygon = System.Windows.Shapes.Polygon;
using ShapeRectangle = System.Windows.Shapes.Rectangle;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfPoint = System.Windows.Point;

namespace WeChatPrivacySkin;

public static class OverlaySkinRenderer
{
    private static string? _cachedPngPath;
    private static ImageBrush? _cachedPngBrush;

    public static void Draw(
        Canvas canvas,
        OverlaySkin skin,
        ThemePack theme,
        string? customSkinPath,
        Rect targetBounds,
        double scale,
        double viewportWidth,
        double viewportHeight)
    {
        canvas.Children.Clear();
        canvas.Clip = new RectangleGeometry(new Rect(0, 0, Math.Max(1, viewportWidth), Math.Max(1, viewportHeight)));

        if (targetBounds.Width <= 0 || targetBounds.Height <= 0 || skin.RenderKind == OverlaySkinRenderKind.ThemeShape)
        {
            return;
        }

        switch (skin.RenderKind)
        {
            case OverlaySkinRenderKind.CustomPng:
                DrawCustomPng(canvas, skin, customSkinPath, targetBounds);
                break;
            case OverlaySkinRenderKind.BuiltInVector:
                DrawBuiltIn(canvas, skin, theme, targetBounds, scale);
                break;
        }
    }

    public static void DrawPreview(Canvas canvas, OverlaySkin skin, ThemePack theme, string? customSkinPath)
    {
        canvas.Children.Clear();
        canvas.Width = 174;
        canvas.Height = 76;
        var target = new Rect(12, 6, 150, 64);

        AddRoundedRect(canvas, new Rect(0, 0, canvas.Width, canvas.Height), 10, Fill(theme.BackgroundColor, 210), Fill(skin.PreviewAccent, 190), 1.4);
        if (skin.RenderKind == OverlaySkinRenderKind.ThemeShape)
        {
            AddRoundedRect(canvas, new Rect(30, 18, 114, 38), 16, Fill(theme.PanelColor, 226), Fill(theme.AccentColor, 220), 2);
            AddText(canvas, skin.DisplayName, 87, 32, 11, theme.PrimaryTextColor, FontWeights.SemiBold);
            return;
        }

        Draw(canvas, skin, theme, customSkinPath, target, 0.5, canvas.Width, canvas.Height);
    }

    private static void DrawBuiltIn(Canvas canvas, OverlaySkin skin, ThemePack theme, Rect targetBounds, double scale)
    {
        switch (skin.BuiltInKind)
        {
            case BuiltInSkinKind.ShyGirlCoverFace:
                DrawShyGirlCoverFace(canvas, skin, theme, targetBounds, scale);
                break;
            case BuiltInSkinKind.OceanSpongeBuddy:
                DrawOceanSpongeBuddy(canvas, skin, theme, targetBounds, scale);
                break;
            case BuiltInSkinKind.PolygonCrystal:
                DrawPolygonCrystal(canvas, skin, theme, targetBounds, scale);
                break;
        }
    }

    private static void DrawShyGirlCoverFace(Canvas canvas, OverlaySkin skin, ThemePack theme, Rect targetBounds, double scale)
    {
        var frame = FitSkinRect(targetBounds, 0.72, skin.DefaultScale);
        var opacity = skin.Opacity;
        var hair = MediaColor.FromRgb(74, 47, 63);
        var hairLight = MediaColor.FromRgb(118, 78, 96);
        var skinTone = MediaColor.FromRgb(255, 214, 192);
        var blush = MediaColor.FromRgb(255, 132, 158);
        var sleeve = theme.SecondaryAccentColor;

        AddEllipse(canvas, new Rect(frame.Left + frame.Width * 0.07, frame.Top + frame.Height * 0.05, frame.Width * 0.86, frame.Height * 0.56), Fill(hair, (byte)(205 * opacity)));
        AddRoundedRect(canvas, new Rect(frame.Left + frame.Width * 0.17, frame.Top + frame.Height * 0.30, frame.Width * 0.66, frame.Height * 0.52), frame.Width * 0.18, Fill(hair, (byte)(178 * opacity)), null, 0);
        AddEllipse(canvas, new Rect(frame.Left + frame.Width * 0.21, frame.Top + frame.Height * 0.19, frame.Width * 0.58, frame.Width * 0.58), Fill(skinTone, (byte)(236 * opacity)), Fill(theme.DecorationColor, 110), 1.2 * scale);

        AddPath(canvas, CreateHairBangGeometry(frame), Fill(hairLight, (byte)(215 * opacity)), null, 0);
        AddEllipse(canvas, new Rect(frame.Left + frame.Width * 0.24, frame.Top + frame.Height * 0.38, frame.Width * 0.13, frame.Width * 0.08), Fill(blush, (byte)(150 * opacity)));
        AddEllipse(canvas, new Rect(frame.Left + frame.Width * 0.63, frame.Top + frame.Height * 0.38, frame.Width * 0.13, frame.Width * 0.08), Fill(blush, (byte)(150 * opacity)));

        AddRoundedRect(canvas, new Rect(frame.Left + frame.Width * 0.06, frame.Top + frame.Height * 0.61, frame.Width * 0.88, frame.Height * 0.26), frame.Width * 0.16, Fill(sleeve, (byte)(148 * opacity)), null, 0);
        AddRoundedRect(canvas, new Rect(frame.Left + frame.Width * 0.11, frame.Top + frame.Height * 0.42, frame.Width * 0.34, frame.Height * 0.17), frame.Width * 0.08, Fill(skinTone, (byte)(246 * opacity)), Fill(theme.DecorationColor, 105), 1.1 * scale, -15);
        AddRoundedRect(canvas, new Rect(frame.Left + frame.Width * 0.55, frame.Top + frame.Height * 0.42, frame.Width * 0.34, frame.Height * 0.17), frame.Width * 0.08, Fill(skinTone, (byte)(246 * opacity)), Fill(theme.DecorationColor, 105), 1.1 * scale, 15);

        AddEllipse(canvas, new Rect(frame.Left + frame.Width * 0.37, frame.Top + frame.Height * 0.52, frame.Width * 0.06, frame.Width * 0.035), Fill(blush, (byte)(95 * opacity)));
        AddEllipse(canvas, new Rect(frame.Left + frame.Width * 0.57, frame.Top + frame.Height * 0.52, frame.Width * 0.06, frame.Width * 0.035), Fill(blush, (byte)(95 * opacity)));
        AddText(canvas, "SHY", frame.Left + frame.Width * 0.5, frame.Top + frame.Height * 0.82, Math.Max(10, 14 * scale), theme.PrimaryTextColor, FontWeights.Bold, opacity * 0.72);
    }

    private static void DrawOceanSpongeBuddy(Canvas canvas, OverlaySkin skin, ThemePack theme, Rect targetBounds, double scale)
    {
        var frame = FitSkinRect(targetBounds, 0.82, skin.DefaultScale);
        var opacity = skin.Opacity;
        var sponge = MediaColor.FromRgb(255, 218, 74);
        var spongeDark = MediaColor.FromRgb(221, 157, 39);
        var ocean = MediaColor.FromRgb(61, 207, 213);

        AddEllipse(canvas, new Rect(frame.Left - frame.Width * 0.08, frame.Top + frame.Height * 0.10, frame.Width * 1.16, frame.Height * 0.78), Fill(ocean, (byte)(50 * opacity)));
        AddPath(canvas, CreateRoundedWobbleGeometry(frame), Fill(sponge, (byte)(218 * opacity)), Fill(spongeDark, (byte)(210 * opacity)), Math.Max(2, 3 * scale));

        var pores = new[]
        {
            (0.22, 0.20, 0.06), (0.60, 0.17, 0.045), (0.78, 0.35, 0.055),
            (0.30, 0.47, 0.05), (0.55, 0.58, 0.07), (0.23, 0.72, 0.045), (0.74, 0.73, 0.05)
        };
        foreach (var (x, y, r) in pores)
        {
            AddEllipse(canvas, new Rect(frame.Left + frame.Width * x, frame.Top + frame.Height * y, frame.Width * r, frame.Width * r * 0.76), Fill(spongeDark, (byte)(92 * opacity)));
        }

        AddEllipse(canvas, new Rect(frame.Left + frame.Width * 0.30, frame.Top + frame.Height * 0.32, frame.Width * 0.13, frame.Width * 0.13), Fill(Colors.White, (byte)(230 * opacity)), Fill(theme.PrimaryTextColor, 135), 1.1 * scale);
        AddEllipse(canvas, new Rect(frame.Left + frame.Width * 0.57, frame.Top + frame.Height * 0.32, frame.Width * 0.13, frame.Width * 0.13), Fill(Colors.White, (byte)(230 * opacity)), Fill(theme.PrimaryTextColor, 135), 1.1 * scale);
        AddEllipse(canvas, new Rect(frame.Left + frame.Width * 0.35, frame.Top + frame.Height * 0.37, frame.Width * 0.035, frame.Width * 0.035), Fill(theme.PrimaryTextColor, (byte)(190 * opacity)));
        AddEllipse(canvas, new Rect(frame.Left + frame.Width * 0.62, frame.Top + frame.Height * 0.37, frame.Width * 0.035, frame.Width * 0.035), Fill(theme.PrimaryTextColor, (byte)(190 * opacity)));
        AddPath(canvas, CreateSmileGeometry(frame), null, Fill(theme.PrimaryTextColor, (byte)(165 * opacity)), Math.Max(1.4, 2.4 * scale));

        AddPath(canvas, CreateWaveGeometry(new Rect(frame.Left + frame.Width * 0.08, frame.Bottom - frame.Height * 0.13, frame.Width * 0.84, frame.Height * 0.08)), null, Fill(ocean, (byte)(180 * opacity)), Math.Max(2, 2.6 * scale));
        for (var i = 0; i < 5; i++)
        {
            var size = frame.Width * (0.035 + i * 0.006);
            AddEllipse(canvas, new Rect(frame.Left + frame.Width * (0.02 + i * 0.2), frame.Top + frame.Height * (0.02 + (i % 2) * 0.12), size, size), WpfBrushes.Transparent, Fill(ocean, (byte)(150 * opacity)), Math.Max(1, scale));
        }
    }

    private static void DrawPolygonCrystal(Canvas canvas, OverlaySkin skin, ThemePack theme, Rect targetBounds, double scale)
    {
        var frame = FitSkinRect(targetBounds, 1.18, skin.DefaultScale);
        var opacity = skin.Opacity;
        var center = new WpfPoint(frame.Left + frame.Width * 0.5, frame.Top + frame.Height * 0.5);
        var outer = Math.Min(frame.Width, frame.Height) * 0.48;

        AddPath(canvas, CreateStarGeometry(center, outer, outer * 0.52, 8), Fill(theme.SecondaryAccentColor, (byte)(58 * opacity)), Fill(theme.SecondaryAccentColor, (byte)(210 * opacity)), Math.Max(2, 3 * scale));
        AddPolygon(canvas,
            [
                new WpfPoint(center.X, center.Y - outer * 0.72),
                new WpfPoint(center.X + outer * 0.62, center.Y - outer * 0.16),
                new WpfPoint(center.X + outer * 0.38, center.Y + outer * 0.62),
                new WpfPoint(center.X - outer * 0.40, center.Y + outer * 0.62),
                new WpfPoint(center.X - outer * 0.64, center.Y - outer * 0.16)
            ],
            Fill(skin.PreviewAccent, (byte)(112 * opacity)),
            Fill(theme.AccentColor, (byte)(225 * opacity)),
            Math.Max(2, 2.4 * scale));
        AddLine(canvas, center, new WpfPoint(center.X, center.Y - outer * 0.72), theme.AccentColor, (byte)(168 * opacity), Math.Max(1, scale));
        AddLine(canvas, center, new WpfPoint(center.X + outer * 0.62, center.Y - outer * 0.16), theme.AccentColor, (byte)(130 * opacity), Math.Max(1, scale));
        AddLine(canvas, center, new WpfPoint(center.X - outer * 0.40, center.Y + outer * 0.62), theme.AccentColor, (byte)(130 * opacity), Math.Max(1, scale));
        AddText(canvas, "POLY", center.X, center.Y + outer * 0.06, Math.Max(10, 16 * scale), theme.PrimaryTextColor, FontWeights.Bold, opacity * 0.72);
    }

    private static void DrawCustomPng(Canvas canvas, OverlaySkin skin, string? customSkinPath, Rect targetBounds)
    {
        var brush = GetPngBrush(customSkinPath);
        if (brush is null)
        {
            AddText(canvas, "PNG", targetBounds.Left + targetBounds.Width * 0.5, targetBounds.Top + targetBounds.Height * 0.5, 18, skin.PreviewAccent, FontWeights.Bold, 0.75);
            return;
        }

        var rect = targetBounds;
        rect.Inflate(targetBounds.Width * (skin.DefaultScale - 1) * 0.5, targetBounds.Height * (skin.DefaultScale - 1) * 0.5);
        var image = new ShapeRectangle
        {
            Width = rect.Width,
            Height = rect.Height,
            Fill = brush,
            Opacity = skin.Opacity
        };
        Canvas.SetLeft(image, rect.Left);
        Canvas.SetTop(image, rect.Top);
        canvas.Children.Add(image);
    }

    private static ImageBrush? GetPngBrush(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            !File.Exists(path) ||
            !string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(_cachedPngPath, path, StringComparison.OrdinalIgnoreCase) && _cachedPngBrush is not null)
        {
            return _cachedPngBrush;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            _cachedPngBrush = new ImageBrush(bitmap)
            {
                Stretch = Stretch.Uniform,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };
            _cachedPngBrush.Freeze();
            _cachedPngPath = path;
            return _cachedPngBrush;
        }
        catch
        {
            _cachedPngPath = null;
            _cachedPngBrush = null;
            return null;
        }
    }

    private static Rect FitSkinRect(Rect targetBounds, double aspectRatio, double scale)
    {
        var width = targetBounds.Width * scale;
        var height = width / aspectRatio;
        var maxHeight = targetBounds.Height * 0.96;
        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * aspectRatio;
        }

        return new Rect(
            targetBounds.Left + (targetBounds.Width - width) * 0.5,
            targetBounds.Top + (targetBounds.Height - height) * 0.52,
            width,
            height);
    }

    private static Geometry CreateHairBangGeometry(Rect frame)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(new WpfPoint(frame.Left + frame.Width * 0.24, frame.Top + frame.Height * 0.26), true, true);
        context.BezierTo(new WpfPoint(frame.Left + frame.Width * 0.34, frame.Top + frame.Height * 0.10), new WpfPoint(frame.Left + frame.Width * 0.52, frame.Top + frame.Height * 0.11), new WpfPoint(frame.Left + frame.Width * 0.57, frame.Top + frame.Height * 0.30), true, false);
        context.BezierTo(new WpfPoint(frame.Left + frame.Width * 0.46, frame.Top + frame.Height * 0.25), new WpfPoint(frame.Left + frame.Width * 0.36, frame.Top + frame.Height * 0.31), new WpfPoint(frame.Left + frame.Width * 0.24, frame.Top + frame.Height * 0.26), true, false);
        geometry.Freeze();
        return geometry;
    }

    private static Geometry CreateRoundedWobbleGeometry(Rect rect)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(new WpfPoint(rect.Left + rect.Width * 0.10, rect.Top + rect.Height * 0.08), true, true);
        context.BezierTo(new WpfPoint(rect.Left + rect.Width * 0.28, rect.Top - rect.Height * 0.02), new WpfPoint(rect.Left + rect.Width * 0.50, rect.Top + rect.Height * 0.06), new WpfPoint(rect.Left + rect.Width * 0.68, rect.Top + rect.Height * 0.03), true, false);
        context.BezierTo(new WpfPoint(rect.Right + rect.Width * 0.04, rect.Top + rect.Height * 0.08), new WpfPoint(rect.Right - rect.Width * 0.04, rect.Top + rect.Height * 0.32), new WpfPoint(rect.Right - rect.Width * 0.02, rect.Top + rect.Height * 0.50), true, false);
        context.BezierTo(new WpfPoint(rect.Right + rect.Width * 0.02, rect.Top + rect.Height * 0.78), new WpfPoint(rect.Right - rect.Width * 0.18, rect.Bottom + rect.Height * 0.03), new WpfPoint(rect.Left + rect.Width * 0.54, rect.Bottom - rect.Height * 0.02), true, false);
        context.BezierTo(new WpfPoint(rect.Left + rect.Width * 0.25, rect.Bottom + rect.Height * 0.04), new WpfPoint(rect.Left - rect.Width * 0.02, rect.Bottom - rect.Height * 0.18), new WpfPoint(rect.Left + rect.Width * 0.06, rect.Top + rect.Height * 0.56), true, false);
        context.BezierTo(new WpfPoint(rect.Left - rect.Width * 0.04, rect.Top + rect.Height * 0.34), new WpfPoint(rect.Left + rect.Width * 0.02, rect.Top + rect.Height * 0.15), new WpfPoint(rect.Left + rect.Width * 0.10, rect.Top + rect.Height * 0.08), true, false);
        geometry.Freeze();
        return geometry;
    }

    private static Geometry CreateSmileGeometry(Rect frame)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(new WpfPoint(frame.Left + frame.Width * 0.38, frame.Top + frame.Height * 0.52), false, false);
        context.BezierTo(new WpfPoint(frame.Left + frame.Width * 0.45, frame.Top + frame.Height * 0.60), new WpfPoint(frame.Left + frame.Width * 0.56, frame.Top + frame.Height * 0.60), new WpfPoint(frame.Left + frame.Width * 0.63, frame.Top + frame.Height * 0.52), true, false);
        geometry.Freeze();
        return geometry;
    }

    private static Geometry CreateWaveGeometry(Rect rect)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        var y = rect.Top + rect.Height * 0.5;
        context.BeginFigure(new WpfPoint(rect.Left, y), false, false);
        var step = rect.Width / 4;
        for (var x = rect.Left; x < rect.Right; x += step)
        {
            context.BezierTo(new WpfPoint(x + step * 0.25, y - rect.Height), new WpfPoint(x + step * 0.75, y + rect.Height), new WpfPoint(x + step, y), true, false);
        }
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

    private static void AddRoundedRect(Canvas canvas, Rect rect, double radius, MediaBrush fill, MediaBrush? stroke, double strokeThickness, double angle = 0)
    {
        var shape = new ShapeRectangle
        {
            Width = rect.Width,
            Height = rect.Height,
            RadiusX = Math.Max(0, radius),
            RadiusY = Math.Max(0, radius),
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = strokeThickness,
            RenderTransformOrigin = new WpfPoint(0.5, 0.5)
        };
        if (Math.Abs(angle) > 0.01)
        {
            shape.RenderTransform = new RotateTransform(angle);
        }

        Canvas.SetLeft(shape, rect.Left);
        Canvas.SetTop(shape, rect.Top);
        canvas.Children.Add(shape);
    }

    private static void AddEllipse(Canvas canvas, Rect rect, MediaBrush fill, MediaBrush? stroke = null, double strokeThickness = 0)
    {
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

    private static void AddPath(Canvas canvas, Geometry geometry, MediaBrush? fill, MediaBrush? stroke, double strokeThickness)
    {
        canvas.Children.Add(new ShapePath
        {
            Data = geometry,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = strokeThickness,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        });
    }

    private static void AddPolygon(Canvas canvas, IList<WpfPoint> points, MediaBrush fill, MediaBrush stroke, double strokeThickness)
    {
        canvas.Children.Add(new ShapePolygon
        {
            Points = new PointCollection(points),
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = strokeThickness,
            StrokeLineJoin = PenLineJoin.Round
        });
    }

    private static void AddLine(Canvas canvas, WpfPoint start, WpfPoint end, MediaColor color, byte alpha, double thickness)
    {
        canvas.Children.Add(new ShapePath
        {
            Data = new LineGeometry(start, end),
            Stroke = Fill(color, alpha),
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        });
    }

    private static void AddText(Canvas canvas, string text, double centerX, double centerY, double fontSize, MediaColor color, FontWeight weight, double opacity = 1)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = weight,
            Foreground = Fill(color, (byte)Math.Round(255 * Math.Clamp(opacity, 0, 1)))
        };
        textBlock.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(textBlock, centerX - textBlock.DesiredSize.Width * 0.5);
        Canvas.SetTop(textBlock, centerY - textBlock.DesiredSize.Height * 0.5);
        canvas.Children.Add(textBlock);
    }

    private static SolidColorBrush Fill(MediaColor color, byte alpha)
    {
        return new SolidColorBrush(MediaColor.FromArgb(alpha, color.R, color.G, color.B));
    }
}
