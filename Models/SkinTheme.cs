using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace WeChatPrivacySkin;

public enum SkinTheme
{
    OfficeLight,
    NightDark,
    StealthPrivacy
}

public sealed record ThemePalette(
    string DisplayName,
    MediaColor OverlayColor,
    MediaColor PanelColor,
    MediaColor AccentColor,
    MediaColor PrimaryTextColor,
    MediaColor SecondaryTextColor,
    MediaColor PlaceholderColor);

public static class ThemeCatalog
{
    private static readonly SkinTheme[] OrderedThemes =
    [
        SkinTheme.OfficeLight,
        SkinTheme.NightDark,
        SkinTheme.StealthPrivacy
    ];

    public static IReadOnlyList<SkinTheme> All => OrderedThemes;

    public static ThemePalette Get(SkinTheme theme) => theme switch
    {
        SkinTheme.NightDark => new ThemePalette(
            "夜间深色",
            MediaColor.FromRgb(23, 26, 32),
            MediaColor.FromRgb(31, 36, 45),
            MediaColor.FromRgb(125, 170, 255),
            MediaColor.FromRgb(245, 248, 255),
            MediaColor.FromRgb(186, 194, 210),
            MediaColor.FromRgb(92, 103, 123)),
        SkinTheme.StealthPrivacy => new ThemePalette(
            "低调隐私",
            MediaColor.FromRgb(12, 18, 21),
            MediaColor.FromRgb(18, 27, 29),
            MediaColor.FromRgb(49, 218, 165),
            MediaColor.FromRgb(236, 255, 248),
            MediaColor.FromRgb(169, 207, 196),
            MediaColor.FromRgb(55, 91, 82)),
        _ => new ThemePalette(
            "办公浅色",
            MediaColor.FromRgb(238, 244, 242),
            MediaColor.FromRgb(249, 252, 251),
            MediaColor.FromRgb(42, 125, 97),
            MediaColor.FromRgb(25, 34, 38),
            MediaColor.FromRgb(86, 100, 105),
            MediaColor.FromRgb(190, 207, 201))
    };

    public static SkinTheme Next(SkinTheme current)
    {
        var index = Array.IndexOf(OrderedThemes, current);
        return OrderedThemes[(index + 1 + OrderedThemes.Length) % OrderedThemes.Length];
    }

    public static string DisplayName(SkinTheme theme) => Get(theme).DisplayName;
}
