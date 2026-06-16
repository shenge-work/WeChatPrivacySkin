using System.Windows.Input;
using System.Windows.Interop;

namespace WeChatPrivacySkin;

public sealed class HotkeyManager : IDisposable
{
    private const int TogglePrivacyHotkeyId = 1001;
    private const int CycleThemeHotkeyId = 1002;

    private HwndSource? _source;

    public event EventHandler? TogglePrivacyRequested;

    public event EventHandler? CycleThemeRequested;

    public void Start()
    {
        if (_source is not null)
        {
            return;
        }

        var parameters = new HwndSourceParameters("WeChatPrivacySkinHotkeys")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);

        var modifiers = HotkeyNativeMethods.MOD_CONTROL |
                        HotkeyNativeMethods.MOD_ALT |
                        HotkeyNativeMethods.MOD_NOREPEAT;

        HotkeyNativeMethods.RegisterHotKey(
            _source.Handle,
            TogglePrivacyHotkeyId,
            modifiers,
            unchecked((uint)KeyInterop.VirtualKeyFromKey(Key.P)));

        HotkeyNativeMethods.RegisterHotKey(
            _source.Handle,
            CycleThemeHotkeyId,
            modifiers,
            unchecked((uint)KeyInterop.VirtualKeyFromKey(Key.T)));
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != HotkeyNativeMethods.WM_HOTKEY)
        {
            return IntPtr.Zero;
        }

        handled = true;
        switch (wParam.ToInt32())
        {
            case TogglePrivacyHotkeyId:
                TogglePrivacyRequested?.Invoke(this, EventArgs.Empty);
                break;
            case CycleThemeHotkeyId:
                CycleThemeRequested?.Invoke(this, EventArgs.Empty);
                break;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_source is null)
        {
            return;
        }

        HotkeyNativeMethods.UnregisterHotKey(_source.Handle, TogglePrivacyHotkeyId);
        HotkeyNativeMethods.UnregisterHotKey(_source.Handle, CycleThemeHotkeyId);
        _source.RemoveHook(WndProc);
        _source.Dispose();
        _source = null;
    }
}
