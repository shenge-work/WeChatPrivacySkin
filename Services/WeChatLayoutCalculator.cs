using System.Windows;
using WpfPoint = System.Windows.Point;

namespace WeChatPrivacySkin;

public sealed record WeChatLayout(
    Rect ConversationList,
    Rect TitleBar,
    Rect MessageArea,
    Rect InputArea,
    Rect UtilityBody,
    IReadOnlyList<Rect> ConversationRows);

public static class WeChatLayoutCalculator
{
    public static WeChatLayout Create(Rect bounds, bool utilityLike, double scale)
    {
        scale = Math.Max(0.1, scale);
        if (utilityLike)
        {
            return CreateUtilityLayout(bounds, scale);
        }

        return CreateMainLayout(bounds, scale);
    }

    public static Rect ToRatioRect(Rect bounds, Rect absoluteRect)
    {
        if (bounds.IsEmpty || absoluteRect.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return Rect.Empty;
        }

        var intersection = Rect.Intersect(bounds, absoluteRect);
        if (intersection.IsEmpty || intersection.Width <= 0 || intersection.Height <= 0)
        {
            return Rect.Empty;
        }

        return new Rect(
            (intersection.Left - bounds.Left) / bounds.Width,
            (intersection.Top - bounds.Top) / bounds.Height,
            intersection.Width / bounds.Width,
            intersection.Height / bounds.Height);
    }

    public static Rect? FindConversationRowAt(WeChatLayout layout, WpfPoint point, double scale)
    {
        var padding = Math.Max(4, 4 * scale);
        foreach (var row in layout.ConversationRows)
        {
            var hitRect = row;
            hitRect.Inflate(0, padding);
            if (hitRect.Contains(point))
            {
                return row;
            }
        }

        return null;
    }

    private static WeChatLayout CreateMainLayout(Rect bounds, double scale)
    {
        var sideWidth = Clamp(bounds.Width * 0.28, 118 * scale, Math.Max(120 * scale, bounds.Width * 0.36));
        var titleHeight = Clamp(bounds.Height * 0.12, 44 * scale, 72 * scale);
        var inputHeight = Clamp(bounds.Height * 0.20, 82 * scale, 150 * scale);
        var contentWidth = Math.Max(1, bounds.Width - sideWidth);
        var messageHeight = Math.Max(42 * scale, bounds.Height - titleHeight - inputHeight);

        var sideRect = new Rect(bounds.Left, bounds.Top, sideWidth, bounds.Height);
        var titleRect = new Rect(bounds.Left + sideWidth, bounds.Top, contentWidth, titleHeight);
        var messageRect = new Rect(bounds.Left + sideWidth, bounds.Top + titleHeight, contentWidth, messageHeight);
        var inputRect = new Rect(bounds.Left + sideWidth, bounds.Top + titleHeight + messageHeight, contentWidth, inputHeight);

        return new WeChatLayout(
            sideRect,
            titleRect,
            messageRect,
            inputRect,
            Rect.Empty,
            CreateConversationRows(sideRect, scale));
    }

    private static WeChatLayout CreateUtilityLayout(Rect bounds, double scale)
    {
        var titleHeight = Clamp(bounds.Height * 0.18, 34 * scale, 58 * scale);
        var footerHeight = Clamp(bounds.Height * 0.20, 38 * scale, 72 * scale);
        var bodyRect = new Rect(bounds.Left, bounds.Top + titleHeight, bounds.Width, Math.Max(20 * scale, bounds.Height - titleHeight - footerHeight));
        var inputRect = new Rect(bounds.Left, bodyRect.Bottom, bounds.Width, footerHeight);

        return new WeChatLayout(
            Rect.Empty,
            new Rect(bounds.Left, bounds.Top, bounds.Width, titleHeight),
            Rect.Empty,
            inputRect,
            bodyRect,
            []);
    }

    private static IReadOnlyList<Rect> CreateConversationRows(Rect sideRect, double scale)
    {
        var rows = new List<Rect>();
        var top = sideRect.Top + 18 * scale;
        var rowHeight = Clamp(sideRect.Height * 0.105, 48 * scale, 64 * scale);
        var rowCount = Math.Max(3, Math.Min(8, (int)((sideRect.Height - 28 * scale) / rowHeight)));

        for (var i = 0; i < rowCount; i++)
        {
            rows.Add(new Rect(
                sideRect.Left + 8 * scale,
                top + i * rowHeight,
                Math.Max(1, sideRect.Width - 16 * scale),
                Math.Max(1, rowHeight - 8 * scale)));
        }

        return rows;
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Max(min, Math.Min(max, value));
    }
}
