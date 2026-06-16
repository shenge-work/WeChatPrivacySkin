using System.IO;
using System.Text;
using System.Windows;

namespace WeChatPrivacySkin;

public sealed class WeChatWindowLocator
{
    public IReadOnlyList<WeChatWindowInfo> FindVisibleWindows(AppSettings settings)
    {
        var results = new List<WeChatWindowInfo>();
        var processPathCache = new Dictionary<uint, string?>();

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd) || NativeMethods.IsIconic(hWnd) || IsCloaked(hWnd))
            {
                return true;
            }

            NativeMethods.GetWindowThreadProcessId(hWnd, out var processId);
            if (processId == 0)
            {
                return true;
            }

            if (!processPathCache.TryGetValue(processId, out var processPath))
            {
                processPath = QueryProcessPath(processId);
                processPathCache[processId] = processPath;
            }

            if (!IsWeChatProcess(processPath, settings))
            {
                return true;
            }

            var bounds = GetWindowBounds(hWnd);
            if (bounds.Width < 120 || bounds.Height < 90)
            {
                return true;
            }

            results.Add(new WeChatWindowInfo(
                hWnd,
                unchecked((int)processId),
                GetWindowTitle(hWnd),
                bounds));

            return true;
        }, IntPtr.Zero);

        return results;
    }

    private static bool IsWeChatProcess(string? processPath, AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(processPath))
        {
            return false;
        }

        var configuredPath = settings.WeChatExecutablePath;
        if (!string.IsNullOrWhiteSpace(configuredPath) &&
            string.Equals(processPath, configuredPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var fileName = Path.GetFileName(processPath);
        if (!string.Equals(fileName, "Weixin.exe", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return processPath.Contains(@"\Tencent\Weixin\", StringComparison.OrdinalIgnoreCase);
    }

    private static string? QueryProcessPath(uint processId)
    {
        var handle = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION,
            false,
            processId);

        if (handle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var capacity = 2048;
            var builder = new StringBuilder(capacity);
            return NativeMethods.QueryFullProcessImageName(handle, 0, builder, ref capacity)
                ? builder.ToString()
                : null;
        }
        finally
        {
            NativeMethods.CloseHandle(handle);
        }
    }

    private static Rect GetWindowBounds(IntPtr hWnd)
    {
        if (NativeMethods.DwmGetWindowAttribute(
                hWnd,
                NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
                out RECT frameRect,
                System.Runtime.InteropServices.Marshal.SizeOf<RECT>()) == 0)
        {
            return frameRect.ToRect();
        }

        return NativeMethods.GetWindowRect(hWnd, out var rect) ? rect.ToRect() : Rect.Empty;
    }

    private static bool IsCloaked(IntPtr hWnd)
    {
        return NativeMethods.DwmGetWindowAttributeInt(
            hWnd,
            NativeMethods.DWMWA_CLOAKED,
            out int cloaked,
            sizeof(int)) == 0 && cloaked != 0;
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        var length = NativeMethods.GetWindowTextLength(hWnd);
        if (length <= 0)
        {
            return "微信窗口";
        }

        var builder = new StringBuilder(length + 1);
        NativeMethods.GetWindowText(hWnd, builder, builder.Capacity);
        return string.IsNullOrWhiteSpace(builder.ToString()) ? "微信窗口" : builder.ToString();
    }
}
