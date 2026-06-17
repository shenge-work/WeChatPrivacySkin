using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace WeChatPrivacySkin;

public enum OverlaySkinRenderKind
{
    ThemeShape,
    BuiltInVector,
    CustomPng
}

public enum BuiltInSkinKind
{
    None,
    ShyGirlCoverFace,
    OceanSpongeBuddy,
    PolygonCrystal
}

public sealed record OverlaySkin(
    string Id,
    string DisplayName,
    string Category,
    string Description,
    OverlaySkinRenderKind RenderKind,
    BuiltInSkinKind BuiltInKind,
    MediaColor PreviewAccent,
    double DefaultScale,
    double Opacity);

public static class OverlaySkinCatalog
{
    public const string DefaultSkinId = "theme-default";
    public const string CustomPngSkinId = "custom-png";

    private static readonly OverlaySkin[] Skins =
    [
        new OverlaySkin(
            DefaultSkinId,
            "主题默认",
            "基础",
            "沿用当前颜色主题的外框、角标和装饰。",
            OverlaySkinRenderKind.ThemeShape,
            BuiltInSkinKind.None,
            C(42, 125, 97),
            1,
            1),
        new OverlaySkin(
            "shy-girl-cover-face",
            "遮脸女孩",
            "原创",
            "双手遮住脸庞的害羞卡通造型。",
            OverlaySkinRenderKind.BuiltInVector,
            BuiltInSkinKind.ShyGirlCoverFace,
            C(255, 142, 170),
            0.86,
            0.92),
        new OverlaySkin(
            "ocean-sponge-buddy",
            "海底方块海绵",
            "原创",
            "黄色多孔海绵伙伴、气泡和海底柔光。",
            OverlaySkinRenderKind.BuiltInVector,
            BuiltInSkinKind.OceanSpongeBuddy,
            C(255, 187, 45),
            0.88,
            0.9),
        new OverlaySkin(
            "polygon-crystal",
            "多边形晶体",
            "几何",
            "星形与多边形组合，展示非矩形视觉轮廓。",
            OverlaySkinRenderKind.BuiltInVector,
            BuiltInSkinKind.PolygonCrystal,
            C(79, 229, 255),
            0.86,
            0.86),
        new OverlaySkin(
            CustomPngSkinId,
            "自定义 PNG",
            "导入",
            "选择本地透明 PNG 作为外形皮肤。",
            OverlaySkinRenderKind.CustomPng,
            BuiltInSkinKind.None,
            C(114, 198, 184),
            0.92,
            0.92)
    ];

    public static IReadOnlyList<OverlaySkin> All => Skins;

    public static OverlaySkin Get(string? id)
    {
        return Skins.FirstOrDefault(skin => string.Equals(skin.Id, id, StringComparison.OrdinalIgnoreCase))
               ?? Skins[0];
    }

    public static bool Contains(string? id)
    {
        return Skins.Any(skin => string.Equals(skin.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    private static MediaColor C(byte r, byte g, byte b) => MediaColor.FromRgb(r, g, b);
}
