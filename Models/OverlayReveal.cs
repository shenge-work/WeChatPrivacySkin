using System.Windows;

namespace WeChatPrivacySkin;

public enum OverlayShapeKind
{
    RoundedCard,
    Bubble,
    Cloud,
    Sponge,
    PixelBlock
}

public enum RevealStyleKind
{
    None,
    SoftSpotlight,
    WaterGlow,
    PixelBlink
}

public enum RevealZoneKind
{
    None,
    ConversationList,
    ConversationRow,
    TitleBar,
    MessageArea,
    InputArea,
    InputEditor,
    UtilityBody
}

public sealed record RevealZone(
    RevealZoneKind Kind,
    Rect RatioRect,
    double CornerRadius,
    string Hint)
{
    public static readonly RevealZone None = new(
        RevealZoneKind.None,
        Rect.Empty,
        0,
        string.Empty);

    public bool IsActive => Kind != RevealZoneKind.None && !RatioRect.IsEmpty;

    public Rect ToAbsolute(Rect bounds)
    {
        if (!IsActive || bounds.IsEmpty)
        {
            return Rect.Empty;
        }

        return new Rect(
            bounds.Left + bounds.Width * RatioRect.Left,
            bounds.Top + bounds.Height * RatioRect.Top,
            bounds.Width * RatioRect.Width,
            bounds.Height * RatioRect.Height);
    }
}
