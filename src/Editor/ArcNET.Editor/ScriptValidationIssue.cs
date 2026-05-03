namespace ArcNET.Editor;

/// <summary>
/// A single validation finding produced by <see cref="ScriptValidator"/>.
/// </summary>
public sealed record ScriptValidationIssue
{
    /// <summary>
    /// Stable machine-readable rule for this validation finding.
    /// </summary>
    public required ScriptValidationCode Code { get; init; }

    /// <summary>
    /// Severity of the finding.
    /// </summary>
    public required ScriptValidationSeverity Severity { get; init; }

    /// <summary>
    /// Attachment-slot index the finding relates to, or <see langword="null"/> when the issue applies to the script as a whole.
    /// </summary>
    public int? AttachmentSlotIndex { get; init; }

    /// <summary>
    /// Human-readable description of the problem.
    /// </summary>
    public required string Message { get; init; }

    /// <inheritdoc/>
    public override string ToString() =>
        AttachmentSlotIndex.HasValue
            ? $"[{Severity}] slot {AttachmentSlotIndex.Value}: {Message}"
            : $"[{Severity}] {Message}";

    internal static ScriptValidationIssue Error(ScriptValidationCode code, int? attachmentSlotIndex, string message) =>
        new()
        {
            Code = code,
            Severity = ScriptValidationSeverity.Error,
            AttachmentSlotIndex = attachmentSlotIndex,
            Message = message,
        };

    internal static ScriptValidationIssue Warning(
        ScriptValidationCode code,
        int? attachmentSlotIndex,
        string message
    ) =>
        new()
        {
            Code = code,
            Severity = ScriptValidationSeverity.Warning,
            AttachmentSlotIndex = attachmentSlotIndex,
            Message = message,
        };

    internal static ScriptValidationIssue Info(ScriptValidationCode code, int? attachmentSlotIndex, string message) =>
        new()
        {
            Code = code,
            Severity = ScriptValidationSeverity.Info,
            AttachmentSlotIndex = attachmentSlotIndex,
            Message = message,
        };
}
