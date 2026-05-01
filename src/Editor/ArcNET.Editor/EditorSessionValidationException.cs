namespace ArcNET.Editor;

/// <summary>
/// Thrown when a staged session apply/save would introduce one or more blocking workspace validation errors.
/// </summary>
public sealed class EditorSessionValidationException : InvalidOperationException
{
    /// <summary>
    /// Initializes the exception with the blocking validation findings.
    /// </summary>
    public EditorSessionValidationException(EditorWorkspaceValidationReport validation)
        : base(CreateMessage(validation))
    {
        ArgumentNullException.ThrowIfNull(validation);
        Validation = validation;
    }

    /// <summary>
    /// Blocking validation findings that prevented the staged session changes from being applied or saved.
    /// </summary>
    public EditorWorkspaceValidationReport Validation { get; }

    private static string CreateMessage(EditorWorkspaceValidationReport validation)
    {
        ArgumentNullException.ThrowIfNull(validation);

        var errorCount = validation.Issues.Count;
        return errorCount == 1
            ? "Cannot apply staged session changes because they introduce 1 blocking workspace validation error."
            : $"Cannot apply staged session changes because they introduce {errorCount} blocking workspace validation errors.";
    }
}
