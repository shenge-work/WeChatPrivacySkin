using System.Windows;

namespace WeChatPrivacySkin;

public sealed record WeChatWindowInfo(IntPtr Handle, int ProcessId, string Title, Rect Bounds);
