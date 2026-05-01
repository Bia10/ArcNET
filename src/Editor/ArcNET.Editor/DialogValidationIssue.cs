using static ArcNET.Editor.DialogValidationIssue;

namespace ArcNET.Editor;

/// <summary>
/// A single validation finding produced by <see cref="DialogValidator"/>.
/// </summary>
public sealed record DialogValidationIssue
{
    /// <summary>
    /// Stable machine-readable rule for this validation finding.
    /// </summary>
    public required DialogValidationCode Code { get; init; }

    /// <summary>
    /// Severity of the finding.
    /// </summary>
    public required DialogValidationSeverity Severity { get; init; }

    /// <summary>
    /// Entry number the finding relates to, or <see langword="null"/> when the issue applies to the dialog as a whole.
    /// </summary>
    public int? EntryNumber { get; init; }

    /// <summary>
    /// Human-readable description of the problem.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Invalid IQ requirement when <see cref="Code"/> is <see cref="DialogValidationCode.NegativeIntelligenceRequirement"/>.
    /// </summary>
    public int? IntelligenceRequirement { get; init; }

    /// <summary>
    /// Missing response target when <see cref="Code"/> is <see cref="DialogValidationCode.MissingResponseTarget"/>.
    /// </summary>
    public int? ResponseTargetNumber { get; init; }

    /// <inheritdoc/>
    public override string ToString() =>
        EntryNumber.HasValue ? $"[{Severity}] entry {EntryNumber.Value}: {Message}" : $"[{Severity}] {Message}";

    internal static DialogValidationIssue Error(
        DialogValidationCode code,
        int? entryNumber,
        string message,
        int? intelligenceRequirement = null,
        int? responseTargetNumber = null
    ) =>
        new()
        {
            Code = code,
            Severity = DialogValidationSeverity.Error,
            EntryNumber = entryNumber,
            Message = message,
            IntelligenceRequirement = intelligenceRequirement,
            ResponseTargetNumber = responseTargetNumber,
        };

    internal static DialogValidationIssue Warning(
        DialogValidationCode code,
        int? entryNumber,
        string message,
        int? intelligenceRequirement = null,
        int? responseTargetNumber = null
    ) =>
        new()
        {
            Code = code,
            Severity = DialogValidationSeverity.Warning,
            EntryNumber = entryNumber,
            Message = message,
            IntelligenceRequirement = intelligenceRequirement,
            ResponseTargetNumber = responseTargetNumber,
        };

    internal static DialogValidationIssue Info(
        DialogValidationCode code,
        int? entryNumber,
        string message,
        int? intelligenceRequirement = null,
        int? responseTargetNumber = null
    ) =>
        new()
        {
            Code = code,
            Severity = DialogValidationSeverity.Info,
            EntryNumber = entryNumber,
            Message = message,
            IntelligenceRequirement = intelligenceRequirement,
            ResponseTargetNumber = responseTargetNumber,
        };
}
