using System.IO;
using System.Text;
using System.Windows;

namespace WeChatPrivacySkin;

public sealed class WeChatWindowLocator
{
    private static readonly TimeSpan ProcessPathCacheTtl = TimeSpan.FromSeconds(10);
    private readonly Dictionary<uint, ProcessPathCacheEntry> _processPathCache = new();

    public IReadOnlyList<WeChatWindowInfo> FindVisibleWindows(AppSettings settings)
    {
        var results = new List<WeChatWindowInfo>();

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

            var processPath = GetProcessPath(processId);

            if (!IsWeChatProcess(processPath, settings))
            {
                return true;
            }

            var bounds = GetWindowBounds(hWnd);
            if (bounds.Width < 80 || bounds.Height < 50)
            {
                return true;
            }

            var title = GetWindowTitle(hWnd);
            var kind = ClassifyWindow(title, bounds);
            results.Add(new WeChatWindowInfo(
                hWnd,
                unchecked((int)processId),
                title,
                bounds,
                kind,
                IsUtilityLike(kind, bounds)));

            return true;
        }, IntPtr.Zero);

        return results;
    }

    private string? GetProcessPath(uint processId)
    {
        var now = DateTime.UtcNow;
        if (_processPathCache.TryGetValue(processId, out var entry) && entry.ExpiresAtUtc > now)
        {
            return entry.Path;
        }

        var processPath = QueryProcessPath(processId);
        _processPathCache[processId] = new ProcessPathCacheEntry(processPath, now.Add(ProcessPathCacheTtl));

        foreach (var expiredProcessId in _processPathCache
                     .Where(pair => pair.Value.ExpiresAtUtc <= now)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            _processPathCache.Remove(expiredProcessId);
        }

        return processPath;
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

    private static WeChatWindowKind ClassifyWindow(string title, Rect bounds)
    {
        if (ContainsAny(title, "登录", "扫码", "确认登录"))
        {
            return WeChatWindowKind.Login;
        }

        if (ContainsAny(title, "文件", "File", "传输"))
        {
            return WeChatWindowKind.FileTransfer;
        }

        if (ContainsAny(title, "图片", "照片", "视频", "Image", "Photo", "Video"))
        {
            return WeChatWindowKind.ImagePreview;
        }

        if (ContainsAny(title, "搜索", "Search"))
        {
            return WeChatWindowKind.Search;
        }

        if (ContainsAny(title, "收藏", "Favorite"))
        {
            return WeChatWindowKind.Favorite;
        }

        if (ContainsAny(title, "发送给", "转发", "Forward"))
        {
            return WeChatWindowKind.Forward;
        }

        if (ContainsAny(title, "通知", "提醒", "Notification"))
        {
            return WeChatWindowKind.Notification;
        }

        if (bounds.Width < 360 || bounds.Height < 260)
        {
            return WeChatWindowKind.UtilityPopup;
        }

        return WeChatWindowKind.MainOrChat;
    }

    private static bool IsUtilityLike(WeChatWindowKind kind, Rect bounds)
    {
        return kind != WeChatWindowKind.MainOrChat || bounds.Width < 520 || bounds.Height < 360;
    }

    private static bool ContainsAny(string text, params string[] terms)
    {
        return terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record ProcessPathCacheEntry(string? Path, DateTime ExpiresAtUtc);
}
