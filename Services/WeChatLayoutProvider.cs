using System.Windows;

namespace WeChatPrivacySkin;

public sealed class WeChatLayoutProvider
{
    private static readonly TimeSpan SuccessTtl = TimeSpan.FromMilliseconds(1500);
    private static readonly TimeSpan FailureCooldown = TimeSpan.FromMilliseconds(5000);
    private static readonly TimeSpan IdleEntryTtl = TimeSpan.FromSeconds(20);

    private readonly object _gate = new();
    private readonly Dictionary<IntPtr, LayoutCacheEntry> _entries = new();

    public WeChatLayout GetLayout(WeChatWindowInfo window, bool preciseEnabled, out bool isPrecise)
    {
        isPrecise = false;
        var fallback = WeChatLayoutCalculator.Create(window.Bounds, window.IsUtilityLike, 1);
        if (!preciseEnabled || window.IsUtilityLike || window.Handle == IntPtr.Zero || window.Bounds.IsEmpty)
        {
            return fallback;
        }

        var now = DateTime.UtcNow;
        lock (_gate)
        {
            if (!_entries.TryGetValue(window.Handle, out var entry) || entry.Bounds != window.Bounds)
            {
                entry = new LayoutCacheEntry(window.Bounds);
                _entries[window.Handle] = entry;
            }

            entry.LastAccessUtc = now;
            if (entry.Layout is not null && now - entry.LastSuccessUtc <= SuccessTtl)
            {
                isPrecise = true;
                return entry.Layout;
            }

            if (entry.ProbeTask is null && now >= entry.FailureCooldownUntil)
            {
                StartProbe(window.Handle, window.Bounds, entry);
            }

            isPrecise = entry.Layout is not null;
            return entry.Layout ?? fallback;
        }
    }

    public void Prune(IReadOnlySet<IntPtr> liveHandles)
    {
        var now = DateTime.UtcNow;
        lock (_gate)
        {
            foreach (var handle in _entries.Keys.ToArray())
            {
                var entry = _entries[handle];
                if (!liveHandles.Contains(handle) || now - entry.LastAccessUtc > IdleEntryTtl)
                {
                    _entries.Remove(handle);
                }
            }
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _entries.Clear();
        }
    }

    private void StartProbe(IntPtr handle, Rect bounds, LayoutCacheEntry entry)
    {
        entry.ProbeTask = Task.Run(() => WeChatUiAutomationLayoutProbe.TryCreate(handle, bounds, bounds, false, 1));
        _ = entry.ProbeTask.ContinueWith(task =>
        {
            var now = DateTime.UtcNow;
            WeChatLayout? layout = null;
            if (task.Status == TaskStatus.RanToCompletion)
            {
                layout = task.Result;
            }
            else
            {
                _ = task.Exception;
            }

            lock (_gate)
            {
                if (!_entries.TryGetValue(handle, out var current) ||
                    current.Bounds != bounds ||
                    current.ProbeTask != task)
                {
                    return;
                }

                current.ProbeTask = null;
                if (layout is not null)
                {
                    current.Layout = layout;
                    current.LastSuccessUtc = now;
                    current.FailureCooldownUntil = DateTime.MinValue;
                }
                else
                {
                    current.FailureCooldownUntil = now.Add(FailureCooldown);
                }
            }
        }, TaskScheduler.Default);
    }

    private sealed class LayoutCacheEntry(Rect bounds)
    {
        public Rect Bounds { get; } = bounds;
        public WeChatLayout? Layout { get; set; }
        public DateTime LastSuccessUtc { get; set; } = DateTime.MinValue;
        public DateTime FailureCooldownUntil { get; set; } = DateTime.MinValue;
        public DateTime LastAccessUtc { get; set; } = DateTime.UtcNow;
        public Task<WeChatLayout?>? ProbeTask { get; set; }
    }
}
