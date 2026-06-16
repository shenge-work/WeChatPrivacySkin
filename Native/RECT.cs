using System.Windows;

namespace WeChatPrivacySkin;

internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public readonly int Width => Math.Max(0, Right - Left);

    public readonly int Height => Math.Max(0, Bottom - Top);

    public readonly Rect ToRect() => new(Left, Top, Width, Height);
}
