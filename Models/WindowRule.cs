using System.Text.Json.Serialization;

namespace WeChatPrivacySkin;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WeChatWindowKind
{
    MainOrChat,
    FileTransfer,
    ImagePreview,
    Search,
    Favorite,
    Forward,
    Login,
    Notification,
    UtilityPopup
}

public sealed class WindowRule
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string TitleContains { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WeChatWindowKind Kind { get; set; } = WeChatWindowKind.MainOrChat;

    public bool Enabled { get; set; } = true;

    public bool CoverWhenNotForeground { get; set; } = true;

    public bool CoverInMeeting { get; set; } = true;

    public bool CoverInAway { get; set; } = true;

    public static List<WindowRule> Defaults() =>
    [
        Rule("chat", "聊天与主窗口", string.Empty, WeChatWindowKind.MainOrChat),
        Rule("file-transfer", "文件传输助手/传输窗口", "文件", WeChatWindowKind.FileTransfer),
        Rule("image-preview", "图片与视频预览", "图片", WeChatWindowKind.ImagePreview),
        Rule("search", "搜索窗口", "搜索", WeChatWindowKind.Search),
        Rule("favorite", "收藏窗口", "收藏", WeChatWindowKind.Favorite),
        Rule("forward", "转发/发送给", "发送给", WeChatWindowKind.Forward),
        Rule("login", "登录与确认窗口", "登录", WeChatWindowKind.Login),
        Rule("notification", "通知与提醒弹窗", "通知", WeChatWindowKind.Notification),
        Rule("utility-popup", "其他微信弹窗", string.Empty, WeChatWindowKind.UtilityPopup)
    ];

    private static WindowRule Rule(string id, string name, string title, WeChatWindowKind kind) => new()
    {
        Id = id,
        DisplayName = name,
        TitleContains = title,
        Kind = kind
    };
}
