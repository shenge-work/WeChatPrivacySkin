using WpfPoint = System.Windows.Point;

namespace WeChatPrivacySkin;

internal struct POINT
{
    public int X;
    public int Y;

    public readonly WpfPoint ToPoint() => new(X, Y);
}
