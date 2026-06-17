using System.Drawing;
using System.Drawing.Drawing2D;

namespace WeChatPrivacySkin;

public static class TrayIconFactory
{
    public static Icon Create(AppSettings settings)
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var shadow = new SolidBrush(Color.FromArgb(58, 12, 73, 61));
        using var bubble = new LinearGradientBrush(
            new Rectangle(3, 3, 25, 25),
            Color.FromArgb(45, 184, 132),
            Color.FromArgb(119, 224, 174),
            45f);
        using var cream = new SolidBrush(Color.FromArgb(244, 255, 248));
        using var shield = new SolidBrush(Color.FromArgb(34, 143, 104));
        using var white = new Pen(Color.White, 2.2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        graphics.FillPath(shadow, RoundedBubble(5, 6, 23, 22, 8));
        graphics.FillPath(bubble, RoundedBubble(3, 4, 24, 22, 8));
        graphics.FillPolygon(bubble, [new PointF(9, 23), new PointF(13, 22), new PointF(10, 29)]);
        graphics.FillPolygon(cream, ShieldPoints(16, 7, 8, 17));
        graphics.FillPolygon(shield, ShieldPoints(16, 10, 5, 11));
        graphics.DrawLines(white, [new PointF(12.7f, 16.5f), new PointF(15.2f, 19.2f), new PointF(20.2f, 13.8f)]);

        var dotColor = StatusColor(settings);
        using var dotBrush = new SolidBrush(dotColor);
        using var dotBorder = new Pen(Color.White, 1.5f);
        graphics.FillEllipse(dotBrush, 22, 5, 8, 8);
        graphics.DrawEllipse(dotBorder, 22, 5, 8, 8);

        var handle = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(handle).Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }

    private static Color StatusColor(AppSettings settings)
    {
        if (!settings.Privacy.Enabled)
        {
            return Color.FromArgb(142, 148, 150);
        }

        if (settings.Privacy.Strategy == ProtectionStrategy.AutoMinimizeOnExternalClick)
        {
            return Color.FromArgb(67, 149, 255);
        }

        return settings.Privacy.Mode == PrivacyMode.MeetingShare
            ? Color.FromArgb(255, 179, 64)
            : Color.FromArgb(39, 201, 120);
    }

    private static GraphicsPath RoundedBubble(float x, float y, float width, float height, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static PointF[] ShieldPoints(float cx, float y, float halfWidth, float height) =>
    [
        new PointF(cx, y),
        new PointF(cx + halfWidth, y + 3.5f),
        new PointF(cx + halfWidth * 0.72f, y + height * 0.66f),
        new PointF(cx, y + height),
        new PointF(cx - halfWidth * 0.72f, y + height * 0.66f),
        new PointF(cx - halfWidth, y + 3.5f)
    ];
}
