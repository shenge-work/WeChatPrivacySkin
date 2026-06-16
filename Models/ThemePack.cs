using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace WeChatPrivacySkin;

public enum SkinTheme
{
    OfficeLight,
    NightDark,
    StealthPrivacy
}

public enum ThemePatternKind
{
    Grid,
    Dots,
    Diagonal,
    Pixels,
    Paper,
    Neon,
    Bubbles
}

public sealed record ThemeAsset(string Kind, string? Path, double Opacity = 1.0);

public sealed record ThemePack(
    string Id,
    string DisplayName,
    string Category,
    string Description,
    MediaColor OverlayColor,
    MediaColor PanelColor,
    MediaColor AccentColor,
    MediaColor SecondaryAccentColor,
    MediaColor PrimaryTextColor,
    MediaColor SecondaryTextColor,
    MediaColor PlaceholderColor,
    MediaColor BackgroundColor,
    MediaColor DecorationColor,
    double CornerRadius,
    double FrameThickness,
    double Outset,
    ThemePatternKind Pattern,
    string BadgeText,
    bool MotionDefault,
    IReadOnlyList<ThemeAsset>? Assets = null,
    OverlayShapeKind ShapeKind = OverlayShapeKind.RoundedCard,
    RevealStyleKind RevealStyle = RevealStyleKind.SoftSpotlight,
    string BackgroundStyle = "flat",
    IReadOnlyList<string>? Decorations = null);

public static class ThemeCatalog
{
    public const string DefaultThemeId = "office-light";

    private static readonly ThemePack[] ThemePacks =
    [
        new ThemePack(
            "office-light",
            "办公浅色",
            "专业",
            "干净克制的默认主题，适合普通办公环境。",
            C(238, 244, 242),
            C(249, 252, 251),
            C(42, 125, 97),
            C(109, 178, 154),
            C(25, 34, 38),
            C(86, 100, 105),
            C(190, 207, 201),
            C(246, 250, 248),
            C(190, 229, 214),
            10,
            3,
            10,
            ThemePatternKind.Grid,
            "SAFE",
            false),
        new ThemePack(
            "night-dark",
            "夜间深色",
            "专业",
            "低亮度深色主题，适合夜间办公和会议。",
            C(23, 26, 32),
            C(31, 36, 45),
            C(125, 170, 255),
            C(98, 216, 199),
            C(245, 248, 255),
            C(186, 194, 210),
            C(92, 103, 123),
            C(15, 17, 21),
            C(72, 104, 172),
            12,
            3,
            12,
            ThemePatternKind.Dots,
            "NIGHT",
            false),
        new ThemePack(
            "stealth-privacy",
            "低调隐私",
            "隐私",
            "更强对比和更少装饰，适合敏感办公场景。",
            C(12, 18, 21),
            C(18, 27, 29),
            C(49, 218, 165),
            C(84, 160, 255),
            C(236, 255, 248),
            C(169, 207, 196),
            C(55, 91, 82),
            C(6, 11, 13),
            C(38, 92, 78),
            8,
            4,
            8,
            ThemePatternKind.Diagonal,
            "LOCK",
            false),
        new ThemePack(
            "kawaii-note",
            "可爱便签",
            "可爱",
            "奶油色、贴纸角标和柔和气泡，适合轻松桌面。",
            C(255, 242, 229),
            C(255, 251, 243),
            C(255, 142, 170),
            C(114, 198, 184),
            C(76, 54, 65),
            C(133, 102, 111),
            C(245, 197, 209),
            C(255, 247, 235),
            C(255, 207, 108),
            20,
            4,
            18,
            ThemePatternKind.Dots,
            "KAWAII",
            true),
        new ThemePack(
            "anime-neon",
            "动漫霓虹",
            "动漫",
            "高饱和霓虹线条和深色背景，适合更有角色感的桌面。",
            C(28, 17, 45),
            C(38, 24, 59),
            C(255, 92, 204),
            C(79, 229, 255),
            C(255, 245, 255),
            C(214, 181, 235),
            C(105, 71, 133),
            C(18, 11, 34),
            C(89, 236, 255),
            16,
            4,
            18,
            ThemePatternKind.Neon,
            "NEON",
            true),
        new ThemePack(
            "pixel-game",
            "像素游戏",
            "游戏",
            "方块颗粒、硬边框和像素节奏，适合复古风桌面。",
            C(31, 36, 49),
            C(43, 50, 68),
            C(105, 232, 137),
            C(255, 218, 96),
            C(246, 251, 242),
            C(190, 204, 190),
            C(86, 99, 116),
            C(20, 24, 33),
            C(255, 218, 96),
            4,
            5,
            14,
            ThemePatternKind.Pixels,
            "8BIT",
            false),
        new ThemePack(
            "paper-journal",
            "纸感手账",
            "手账",
            "纸纹、墨绿色和便签感边框，适合柔和专注。",
            C(244, 238, 223),
            C(255, 252, 241),
            C(91, 126, 88),
            C(198, 142, 96),
            C(55, 48, 39),
            C(116, 101, 83),
            C(211, 196, 165),
            C(250, 244, 228),
            C(218, 184, 122),
            14,
            3,
            16,
            ThemePatternKind.Paper,
            "NOTE",
            false),
        new ThemePack(
            "sponge-ocean",
            "海底海绵",
            "可爱",
            "原创海底海绵风格，黄色多孔轮廓、海水气泡和卡通海底装饰。",
            C(42, 172, 189),
            C(255, 221, 86),
            C(255, 187, 45),
            C(79, 219, 222),
            C(72, 54, 28),
            C(104, 85, 48),
            C(255, 240, 134),
            C(32, 146, 178),
            C(255, 133, 96),
            26,
            5,
            26,
            ThemePatternKind.Bubbles,
            "OCEAN",
            true,
            null,
            OverlayShapeKind.Sponge,
            RevealStyleKind.WaterGlow,
            "underwater",
            ["bubbles", "coral", "starfish", "waves"])
    ];

    public static IReadOnlyList<ThemePack> All => ThemePacks;

    public static ThemePack Get(string? id)
    {
        return ThemePacks.FirstOrDefault(theme => string.Equals(theme.Id, id, StringComparison.OrdinalIgnoreCase))
               ?? ThemePacks[0];
    }

    public static bool Contains(string? id)
    {
        return ThemePacks.Any(theme => string.Equals(theme.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public static string Next(string? currentId)
    {
        var current = Get(currentId);
        var index = Array.IndexOf(ThemePacks, current);
        return ThemePacks[(index + 1 + ThemePacks.Length) % ThemePacks.Length].Id;
    }

    public static string MapLegacy(SkinTheme legacy) => legacy switch
    {
        SkinTheme.NightDark => "night-dark",
        SkinTheme.StealthPrivacy => "stealth-privacy",
        _ => DefaultThemeId
    };

    private static MediaColor C(byte r, byte g, byte b) => MediaColor.FromRgb(r, g, b);
}
