using ArcNET.Formats;
using static ArcNET.Editor.ScriptValidationIssue;

namespace ArcNET.Editor;

/// <summary>
/// Validates compiled script files and authored script builder output for local authoring problems.
/// </summary>
public static class ScriptValidator
{
    private const int MaxDescriptionLength = 40;
    private const int MaxKnownAttachmentSlot = (int)ScriptAttachmentPoint.CriticalMiss;

    /// <summary>
    /// Validates one script file for description truncation or character-loss risks plus non-empty attachment slots
    /// that ArcNET does not currently name. Returns all findings; an empty list means the script is locally clean
    /// under the current rule set.
    /// </summary>
    public static IReadOnlyList<ScriptValidationIssue> Validate(ScrFile script)
    {
        ArgumentNullException.ThrowIfNull(script);

        return CollectIssues(issues =>
        {
            ValidateDescription(script.Description, issues);
            ValidateAttachmentSlots(GetActiveAttachmentSlots(script), issues);
        });
    }

    internal static IReadOnlyList<ScriptValidationIssue> Validate(EditorScriptDefinition script)
    {
        ArgumentNullException.ThrowIfNull(script);

        return CollectIssues(issues =>
        {
            ValidateDescription(script.Description, issues);
            ValidateAttachmentSlots(script.ActiveAttachmentSlots, issues);
        });
    }

    internal static IEnumerable<int> GetActiveAttachmentSlots(ScrFile script)
    {
        ArgumentNullException.ThrowIfNull(script);

        for (var i = 0; i < script.Entries.Count; i++)
        {
            var entry = script.Entries[i];
            if (IsEmptyCondition(entry) && IsEmptyAction(entry.Action) && IsEmptyAction(entry.Else))
                continue;

            yield return i;
        }
    }

    internal static bool IsKnownAttachmentSlot(int slot) => slot >= 0 && slot <= MaxKnownAttachmentSlot;

    private static IReadOnlyList<ScriptValidationIssue> CollectIssues(Action<List<ScriptValidationIssue>> validate)
    {
        var issues = new List<ScriptValidationIssue>();
        validate(issues);
        return issues;
    }

    private static void ValidateDescription(string description, List<ScriptValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(description);

        if (description.Length > MaxDescriptionLength)
        {
            issues.Add(
                Warning(
                    ScriptValidationCode.DescriptionTooLong,
                    null,
                    $"Description length {description.Length} exceeds the 40 ASCII characters preserved on disk and will be truncated when written."
                )
            );
        }

        if (description.Any(static ch => ch > 0x7F))
        {
            issues.Add(
                Warning(
                    ScriptValidationCode.DescriptionContainsNonAscii,
                    null,
                    "Description contains non-ASCII characters that will be replaced with '?' when written to the .scr file."
                )
            );
        }
    }

    private static void ValidateAttachmentSlots(
        IEnumerable<int> activeAttachmentSlots,
        List<ScriptValidationIssue> issues
    )
    {
        ArgumentNullException.ThrowIfNull(activeAttachmentSlots);

        var unknownSlots = activeAttachmentSlots
            .Where(static slot => !IsKnownAttachmentSlot(slot))
            .OrderBy(static slot => slot)
            .ToArray();
        if (unknownSlots.Length == 0)
            return;

        issues.Add(
            Info(
                ScriptValidationCode.UnknownAttachmentSlot,
                null,
                $"Uses non-empty attachment slot(s) that ArcNET does not name yet: {string.Join(", ", unknownSlots)}."
            )
        );
    }

    private static bool IsEmptyCondition(ScriptConditionData condition)
    {
        if (condition.Type != (int)ScriptConditionType.True)
            return false;

        var opTypes = condition.OpTypes;
        var opValues = condition.OpValues;
        for (var i = 0; i < 8; i++)
        {
            if (opTypes[i] != 0 || opValues[i] != 0)
                return false;
        }

        return true;
    }

    private static bool IsEmptyAction(ScriptActionData action)
    {
        if (action.Type != (int)ScriptActionType.DoNothing)
            return false;

        var opTypes = action.OpTypes;
        var opValues = action.OpValues;
        for (var i = 0; i < 8; i++)
        {
            if (opTypes[i] != 0 || opValues[i] != 0)
                return false;
        }

        return true;
    }
}
