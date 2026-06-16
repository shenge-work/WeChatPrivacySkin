using System.Text.Json.Serialization;

namespace WeChatPrivacySkin;

public sealed class AppSettings
{
    public PrivacyProfile Privacy { get; set; } = new();

    public string ThemePackId { get; set; } = ThemeCatalog.DefaultThemeId;

    public double OverlayOpacity { get; set; } = 0.78;

    public bool OverlayAlwaysOnTop { get; set; } = true;

    public bool DecorativeMotionEnabled { get; set; } = true;

    public string? BackgroundImagePath { get; set; }

    public string WeChatExecutablePath { get; set; } = @"C:\Program Files\Tencent\Weixin\Weixin.exe";

    public List<WindowRule> WindowRules { get; set; } = WindowRule.Defaults();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? PrivacyEnabled { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SkinTheme? Theme { get; set; }
}
