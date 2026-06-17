using System.Windows;
using System.Windows.Automation;
using AutomationCondition = System.Windows.Automation.Condition;
using WpfPoint = System.Windows.Point;

namespace WeChatPrivacySkin;

public static class WeChatUiAutomationLayoutProbe
{
    private const double EdgeTolerance = 6;

    public static WeChatLayout? TryCreate(
        IntPtr windowHandle,
        Rect physicalWindowBounds,
        Rect outputBounds,
        bool utilityLike,
        double outputScale)
    {
        if (windowHandle == IntPtr.Zero ||
            utilityLike ||
            physicalWindowBounds.IsEmpty ||
            outputBounds.IsEmpty)
        {
            return null;
        }

        try
        {
            var root = AutomationElement.FromHandle(windowHandle);
            if (root is null)
            {
                return null;
            }

            var fallbackPhysical = WeChatLayoutCalculator.Create(physicalWindowBounds, false, 1);
            var candidates = CollectCandidates(root, physicalWindowBounds);
            if (candidates.Count == 0)
            {
                return null;
            }

            var rowRects = DetectConversationRows(candidates, fallbackPhysical);
            var inputEditor = DetectInputEditor(candidates, fallbackPhysical);
            if (rowRects.Count < 2 && inputEditor is null)
            {
                return null;
            }

            var sideRight = rowRects.Count >= 2
                ? Math.Clamp(rowRects.Max(rect => rect.Right) + 8, physicalWindowBounds.Left + 120, physicalWindowBounds.Left + physicalWindowBounds.Width * 0.42)
                : fallbackPhysical.ConversationList.Right;

            var sideWidth = Math.Max(1, sideRight - physicalWindowBounds.Left);
            var titleHeight = fallbackPhysical.TitleBar.Height;
            var inputArea = fallbackPhysical.InputArea;
            var contentWidth = Math.Max(1, physicalWindowBounds.Width - sideWidth);
            var title = new Rect(physicalWindowBounds.Left + sideWidth, physicalWindowBounds.Top, contentWidth, titleHeight);
            var message = new Rect(
                physicalWindowBounds.Left + sideWidth,
                physicalWindowBounds.Top + titleHeight,
                contentWidth,
                Math.Max(1, inputArea.Top - physicalWindowBounds.Top - titleHeight));
            var side = new Rect(physicalWindowBounds.Left, physicalWindowBounds.Top, sideWidth, physicalWindowBounds.Height);

            var rows = rowRects.Count >= 2
                ? rowRects.Select(rect => MapRect(physicalWindowBounds, outputBounds, rect)).ToArray()
                : WeChatLayoutCalculator.Create(outputBounds, false, outputScale).ConversationRows;

            return new WeChatLayout(
                MapRect(physicalWindowBounds, outputBounds, side),
                MapRect(physicalWindowBounds, outputBounds, title),
                MapRect(physicalWindowBounds, outputBounds, message),
                MapRect(physicalWindowBounds, outputBounds, inputArea),
                inputEditor is null
                    ? WeChatLayoutCalculator.Create(outputBounds, false, outputScale).InputEditor
                    : MapRect(physicalWindowBounds, outputBounds, inputEditor.Value),
                Rect.Empty,
                rows);
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<ElementCandidate> CollectCandidates(AutomationElement root, Rect windowBounds)
    {
        var results = new List<ElementCandidate>();
        var elements = root.FindAll(TreeScope.Descendants, AutomationCondition.TrueCondition);
        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            var rect = element.Current.BoundingRectangle;
            if (!IsUsableRect(rect, windowBounds))
            {
                continue;
            }

            results.Add(new ElementCandidate(
                rect,
                element.Current.ControlType,
                element.Current.ClassName ?? string.Empty));
        }

        return results;
    }

    private static IReadOnlyList<Rect> DetectConversationRows(
        IReadOnlyList<ElementCandidate> candidates,
        WeChatLayout fallback)
    {
        var side = fallback.ConversationList;
        var rows = candidates
            .Where(candidate => IsRowLike(candidate, side))
            .Select(candidate => candidate.Rect)
            .OrderBy(rect => rect.Top)
            .ToArray();

        if (rows.Length < 2)
        {
            return [];
        }

        var merged = new List<Rect>();
        foreach (var row in rows)
        {
            if (merged.Count > 0 && Math.Abs(merged[^1].Top - row.Top) < 8)
            {
                merged[^1] = Rect.Union(merged[^1], row);
                continue;
            }

            merged.Add(row);
        }

        var normalized = merged
            .Where(rect => rect.Width >= side.Width * 0.45 && rect.Height is >= 24 and <= 92)
            .Take(12)
            .ToArray();

        return normalized.Length >= 2 ? normalized : [];
    }

    private static Rect? DetectInputEditor(
        IReadOnlyList<ElementCandidate> candidates,
        WeChatLayout fallback)
    {
        var input = fallback.InputArea;
        var candidatesInInput = candidates
            .Where(candidate => IsInputEditorLike(candidate, input))
            .OrderByDescending(candidate => candidate.Rect.Width * candidate.Rect.Height)
            .ToArray();

        return candidatesInInput.FirstOrDefault()?.Rect;
    }

    private static bool IsRowLike(ElementCandidate candidate, Rect side)
    {
        var rect = candidate.Rect;
        if (!side.Contains(new WpfPoint(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2)))
        {
            return false;
        }

        if (rect.Width < side.Width * 0.45 || rect.Height < 24 || rect.Height > 92)
        {
            return false;
        }

        return candidate.ControlType == ControlType.ListItem ||
               candidate.ControlType == ControlType.DataItem ||
               candidate.ControlType == ControlType.Button ||
               candidate.ControlType == ControlType.Pane ||
               candidate.ControlType == ControlType.Group;
    }

    private static bool IsInputEditorLike(ElementCandidate candidate, Rect input)
    {
        var rect = candidate.Rect;
        if (!input.Contains(new WpfPoint(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2)))
        {
            return false;
        }

        if (rect.Width < input.Width * 0.35 || rect.Height < 18)
        {
            return false;
        }

        return candidate.ControlType == ControlType.Edit ||
               candidate.ControlType == ControlType.Document ||
               candidate.ClassName.Contains("edit", StringComparison.OrdinalIgnoreCase);
    }

    private static Rect MapRect(Rect physicalWindowBounds, Rect outputBounds, Rect physicalRect)
    {
        var ratio = WeChatLayoutCalculator.ToRatioRect(physicalWindowBounds, physicalRect);
        return WeChatLayoutCalculator.FromRatioRect(outputBounds, ratio);
    }

    private static bool IsUsableRect(Rect rect, Rect windowBounds)
    {
        if (rect.IsEmpty ||
            double.IsNaN(rect.Left) ||
            double.IsInfinity(rect.Left) ||
            rect.Width < 4 ||
            rect.Height < 4)
        {
            return false;
        }

        var expanded = windowBounds;
        expanded.Inflate(EdgeTolerance, EdgeTolerance);
        return expanded.Contains(new WpfPoint(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2));
    }

    private sealed record ElementCandidate(Rect Rect, ControlType ControlType, string ClassName);
}
