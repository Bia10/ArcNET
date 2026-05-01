using ArcNET.Formats;
using static ArcNET.Editor.DialogValidationIssue;

namespace ArcNET.Editor;

/// <summary>
/// Validates dialog files and authored dialog builder output for structural and local semantic problems.
/// </summary>
public static class DialogValidator
{
    /// <summary>
    /// Validates one dialog file for duplicate entry numbers, negative IQ values, and missing positive response targets.
    /// Returns all findings; an empty list means the dialog is locally clean under the current rule set.
    /// </summary>
    public static IReadOnlyList<DialogValidationIssue> Validate(DlgFile dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        return CollectIssues(issues =>
            ValidateEntries(
                dialog.Entries.Select(static entry => new ValidationEntry(entry.Num, entry.Iq, entry.ResponseVal)),
                issues
            )
        );
    }

    internal static IReadOnlyList<DialogValidationIssue> Validate(EditorDialogDefinition dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        return CollectIssues(issues =>
            ValidateEntries(
                dialog.Nodes.Select(static node => new ValidationEntry(
                    node.EntryNumber,
                    node.IntelligenceRequirement,
                    node.ResponseTargetNumber
                )),
                issues
            )
        );
    }

    private static IReadOnlyList<DialogValidationIssue> CollectIssues(Action<List<DialogValidationIssue>> validate)
    {
        var issues = new List<DialogValidationIssue>();
        validate(issues);
        return issues;
    }

    private static void ValidateEntries(IEnumerable<ValidationEntry> entries, List<DialogValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var orderedEntries = entries.ToArray();
        var seenEntryNumbers = new HashSet<int>();
        var entryNumbers = orderedEntries.Select(static entry => entry.EntryNumber).ToHashSet();

        foreach (var entry in orderedEntries)
        {
            if (!seenEntryNumbers.Add(entry.EntryNumber))
            {
                issues.Add(
                    Error(
                        DialogValidationCode.DuplicateEntryNumber,
                        entry.EntryNumber,
                        $"Duplicate entry number {entry.EntryNumber} appears more than once."
                    )
                );
            }

            if (entry.IntelligenceRequirement < 0)
            {
                issues.Add(
                    Error(
                        DialogValidationCode.NegativeIntelligenceRequirement,
                        entry.EntryNumber,
                        $"Negative IQ requirement {entry.IntelligenceRequirement} is invalid. Expected 0 for NPC lines or a positive value for PC options.",
                        intelligenceRequirement: entry.IntelligenceRequirement
                    )
                );
            }

            if (entry.ResponseTargetNumber > 0 && !entryNumbers.Contains(entry.ResponseTargetNumber))
            {
                issues.Add(
                    Warning(
                        DialogValidationCode.MissingResponseTarget,
                        entry.EntryNumber,
                        $"Positive response target {entry.ResponseTargetNumber} does not exist in the dialog.",
                        responseTargetNumber: entry.ResponseTargetNumber
                    )
                );
            }
        }
    }

    private readonly record struct ValidationEntry(
        int EntryNumber,
        int IntelligenceRequirement,
        int ResponseTargetNumber
    );
}
