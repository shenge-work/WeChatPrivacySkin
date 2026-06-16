using System.Text.Json.Serialization;

namespace WeChatPrivacySkin;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PrivacyMode
{
    DailyProtection,
    MeetingShare,
    AwayCover,
    CleanScreen,
    FocusChat
}

public sealed class PrivacyProfile
{
    public bool Enabled { get; set; } = true;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PrivacyMode Mode { get; set; } = PrivacyMode.DailyProtection;

    public bool CoverPopups { get; set; } = true;

    public bool CoverUtilityWindows { get; set; } = true;

    public bool ShowMeetingWarning { get; set; } = true;

    public bool HideWindowTitleHints { get; set; } = true;
}

public static class PrivacyModeCatalog
{
    public static readonly PrivacyMode[] OrderedModes =
    [
        PrivacyMode.DailyProtection,
        PrivacyMode.MeetingShare,
        PrivacyMode.AwayCover,
        PrivacyMode.CleanScreen,
        PrivacyMode.FocusChat
    ];

    public static string DisplayName(PrivacyMode mode) => mode switch
    {
        PrivacyMode.MeetingShare => "会议共享",
        PrivacyMode.AwayCover => "离席保护",
        PrivacyMode.CleanScreen => "快速净屏",
        PrivacyMode.FocusChat => "专注聊天",
        _ => "日常保护"
    };

    public static string Description(PrivacyMode mode) => mode switch
    {
        PrivacyMode.MeetingShare => "共享整个屏幕时强化遮罩，并提醒单窗口共享的风险。",
        PrivacyMode.AwayCover => "临时离开工位时遮住所有微信窗口。",
        PrivacyMode.CleanScreen => "一键遮住所有微信窗口，适合有人靠近或临时投屏。",
        PrivacyMode.FocusChat => "只保留当前聊天窗口可见，减少其他微信窗口干扰。",
        _ => "当前微信窗口可见，其他微信窗口自动脱敏。"
    };

    public static PrivacyMode Next(PrivacyMode current)
    {
        var index = Array.IndexOf(OrderedModes, current);
        return OrderedModes[(index + 1 + OrderedModes.Length) % OrderedModes.Length];
    }
}
