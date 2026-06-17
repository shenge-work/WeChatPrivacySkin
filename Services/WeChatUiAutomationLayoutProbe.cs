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
            var fallbackOutput = WeChatLayoutCalculator.Create(outputBounds, false, outputScale);
            var candidates = CollectCandidates(root, physicalWindowBounds);
            if (candidates.Count == 0)
            {
                return null;
            }

            var rowRects = DetectConversationRows(candidates, fallbackPhysical);
            var inputEditor = DetectInputEditor(candidates, fallbackPhysical);
            var inputArea = DetectInputArea(candidates, fallbackPhysical, inputEditor) ?? fallbackPhysical.InputArea;
            if (rowRects.Count < 2 && inputEditor is null)
            {
                return null;
            }

            var sideRight = ResolveSideRight(physicalWindowBounds, fallbackPhysical, rowRects, inputArea);

            var sideWidth = Math.Max(1, sideRight - physicalWindowBounds.Left);
            var titleHeight = fallbackPhysical.TitleBar.Height;
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
                : fallbackOutput.ConversationRows;

            return new WeChatLayout(
                MapRect(physicalWindowBounds, outputBounds, side),
                MapRect(physicalWindowBounds, outputBounds, title),
                MapRect(physicalWindowBounds, outputBounds, message),
                MapRect(physicalWindowBounds, outputBounds, inputArea),
                inputEditor is null
                    ? fallbackOutput.InputEditor
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
        var input = CreateInputSearchArea(fallback);
        var candidatesInInput = candidates
            .Where(candidate => IsInputEditorLike(candidate, input))
            .OrderByDescending(ScoreInputEditorCandidate)
            .ThenByDescending(candidate => candidate.Rect.Width)
            .ThenBy(candidate => candidate.Rect.Height)
            .ToArray();

        return candidatesInInput.FirstOrDefault()?.Rect;
    }

    private static Rect? DetectInputArea(
        IReadOnlyList<ElementCandidate> candidates,
        WeChatLayout fallback,
        Rect? inputEditor)
    {
        var inputSearchArea = CreateInputSearchArea(fallback);
        var panels = candidates
            .Where(candidate => IsInputPanelLike(candidate, fallback, inputSearchArea, inputEditor))
            .OrderByDescending(candidate => ScoreInputPanelCandidate(candidate, inputEditor))
            .ThenBy(candidate => candidate.Rect.Top)
            .ThenBy(candidate => candidate.Rect.Width * candidate.Rect.Height)
            .ToArray();

        var panel = panels.FirstOrDefault();
        if (panel is null)
        {
            return inputEditor is null ? null : CreateInputAreaFromEditor(fallback, inputEditor.Value);
        }

        var rect = panel.Rect;
        if (inputEditor is not null && (rect.Height <= inputEditor.Value.Height + 8 || rect.Width < inputEditor.Value.Width))
        {
            return CreateInputAreaFromEditor(fallback, inputEditor.Value);
        }

        return rect;
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

        if (rect.Height > input.Height * 0.72 || rect.Bottom > input.Bottom - 24)
        {
            return false;
        }

        return candidate.ControlType == ControlType.Edit ||
               candidate.ControlType == ControlType.Document ||
               candidate.ClassName.Contains("edit", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInputPanelLike(
        ElementCandidate candidate,
        WeChatLayout fallback,
        Rect inputSearchArea,
        Rect? inputEditor)
    {
        var rect = candidate.Rect;
        if (!inputSearchArea.Contains(new WpfPoint(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2)))
        {
            return false;
        }

        if (rect.Height < 44 || rect.Height > Math.Max(240, fallback.InputArea.Height * 1.9))
        {
            return false;
        }

        if (rect.Width < Math.Max(120, fallback.MessageArea.Width * 0.45))
        {
            return false;
        }

        if (inputEditor is not null && !ContainsRectWithTolerance(rect, inputEditor.Value, 8))
        {
            return false;
        }

        return candidate.ClassName.Contains("ChatInput", StringComparison.OrdinalIgnoreCase) ||
               candidate.ClassName.Contains("Input", StringComparison.OrdinalIgnoreCase) ||
               candidate.ControlType == ControlType.Pane ||
               candidate.ControlType == ControlType.Group;
    }

    private static Rect CreateInputSearchArea(WeChatLayout fallback)
    {
        var input = fallback.InputArea;
        var topExpansion = Math.Max(72, input.Height * 0.55);
        return new Rect(
            fallback.MessageArea.Left,
            Math.Max(fallback.TitleBar.Bottom, input.Top - topExpansion),
            fallback.MessageArea.Width,
            input.Height + topExpansion + 8);
    }

    private static Rect CreateInputAreaFromEditor(WeChatLayout fallback, Rect editor)
    {
        var top = Math.Min(editor.Top - 10, fallback.InputArea.Top);
        var bottom = Math.Max(editor.Bottom + 48, fallback.InputArea.Bottom);
        return new Rect(
            Math.Min(editor.Left - 20, fallback.InputArea.Left),
            top,
            Math.Max(editor.Width + 40, fallback.InputArea.Width),
            Math.Max(1, bottom - top));
    }

    private static int ScoreInputEditorCandidate(ElementCandidate candidate)
    {
        var score = 0;
        if (candidate.ClassName.Contains("ChatInputField", StringComparison.OrdinalIgnoreCase))
        {
            score += 80;
        }

        if (candidate.ControlType == ControlType.Edit)
        {
            score += 40;
        }
        else if (candidate.ControlType == ControlType.Document)
        {
            score += 28;
        }

        if (candidate.ClassName.Contains("edit", StringComparison.OrdinalIgnoreCase))
        {
            score += 18;
        }

        return score;
    }

    private static int ScoreInputPanelCandidate(ElementCandidate candidate, Rect? inputEditor)
    {
        var score = 0;
        if (candidate.ClassName.Contains("ChatInputView", StringComparison.OrdinalIgnoreCase))
        {
            score += 80;
        }
        else if (candidate.ClassName.Contains("ChatInput", StringComparison.OrdinalIgnoreCase))
        {
            score += 52;
        }
        else if (candidate.ClassName.Contains("Input", StringComparison.OrdinalIgnoreCase))
        {
            score += 24;
        }

        if (inputEditor is not null && ContainsRectWithTolerance(candidate.Rect, inputEditor.Value, 8))
        {
            score += 20;
        }

        return score;
    }

    private static double ResolveSideRight(
        Rect physicalWindowBounds,
        WeChatLayout fallback,
        IReadOnlyList<Rect> rowRects,
        Rect inputArea)
    {
        var min = physicalWindowBounds.Left + 120;
        var max = physicalWindowBounds.Left + physicalWindowBounds.Width * 0.50;
        if (rowRects.Count >= 2)
        {
            return Math.Clamp(rowRects.Max(rect => rect.Right) + 8, min, max);
        }

        if (!inputArea.IsEmpty && inputArea.Left > min && inputArea.Left < max)
        {
            return Math.Clamp(inputArea.Left, min, max);
        }

        return fallback.ConversationList.Right;
    }

    private static bool ContainsRectWithTolerance(Rect outer, Rect inner, double tolerance)
    {
        return outer.Left <= inner.Left + tolerance &&
               outer.Top <= inner.Top + tolerance &&
               outer.Right >= inner.Right - tolerance &&
               outer.Bottom >= inner.Bottom - tolerance;
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
