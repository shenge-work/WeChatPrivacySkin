namespace WeChatPrivacySkin;

public sealed record PrivacyDecision(
    WeChatWindowInfo Window,
    bool ShouldCover,
    bool IsActive,
    string Reason);

public sealed record PrivacySnapshot(
    IReadOnlyList<WeChatWindowInfo> Windows,
    WeChatWindowInfo? ActiveWindow,
    IReadOnlyList<PrivacyDecision> Decisions,
    PrivacyMode Mode,
    bool PrivacyEnabled);

public sealed class PrivacyPolicyService
{
    public PrivacySnapshot CreateSnapshot(
        IReadOnlyList<WeChatWindowInfo> windows,
        IntPtr foregroundWindow,
        AppSettings settings)
    {
        var activeWindow = windows.FirstOrDefault(window => window.Handle == foregroundWindow);
        var decisions = windows
            .Select(window => CreateDecision(window, activeWindow, settings))
            .ToArray();

        return new PrivacySnapshot(
            windows,
            activeWindow,
            decisions,
            settings.Privacy.Mode,
            settings.Privacy.Enabled);
    }

    private static PrivacyDecision CreateDecision(
        WeChatWindowInfo window,
        WeChatWindowInfo? activeWindow,
        AppSettings settings)
    {
        if (!settings.Privacy.Enabled)
        {
            return new PrivacyDecision(window, false, false, "隐私保护已关闭");
        }

        var isActive = activeWindow?.Handle == window.Handle;
        var mode = settings.Privacy.Mode;

        if (mode is PrivacyMode.AwayCover or PrivacyMode.CleanScreen)
        {
            return new PrivacyDecision(window, true, isActive, PrivacyModeCatalog.DisplayName(mode));
        }

        if (mode == PrivacyMode.MeetingShare && window.IsUtilityLike && !isActive)
        {
            return new PrivacyDecision(window, true, false, "会议共享：微信弹窗已脱敏");
        }

        if (mode == PrivacyMode.FocusChat && !isActive)
        {
            return new PrivacyDecision(window, true, false, "专注聊天：非当前窗口已隐藏");
        }

        if (!isActive)
        {
            return new PrivacyDecision(window, true, false, "非当前微信窗口已脱敏");
        }

        return new PrivacyDecision(window, false, true, "当前微信窗口保持可见");
    }
}
