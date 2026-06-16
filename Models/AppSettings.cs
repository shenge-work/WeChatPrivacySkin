using System.Text.Json.Serialization;

namespace WeChatPrivacySkin;

public sealed class AppSettings
{
    public bool PrivacyEnabled { get; set; } = true;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SkinTheme Theme { get; set; } = SkinTheme.OfficeLight;

    public double OverlayOpacity { get; set; } = 0.78;

    public bool OverlayAlwaysOnTop { get; set; } = true;

    public string? BackgroundImagePath { get; set; }

    public string WeChatExecutablePath { get; set; } = @"C:\Program Files\Tencent\Weixin\Weixin.exe";
}
