using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.GameData.SaveGames;

/// <summary>
/// Shared validation finding that mirrors the legacy editor surface while keeping
/// diagnostics and other non-editor callers on the package-local namespace.
/// </summary>
public sealed record SaveValidationIssue
{
    public required SaveValidationSeverity Severity { get; init; }

    public string? FilePath { get; init; }

    public required string Message { get; init; }

    public override string ToString() =>
        FilePath is null ? $"[{Severity}] {Message}" : $"[{Severity}] {FilePath}: {Message}";

    internal static SaveValidationIssue FromLegacy(ArcNET.Editor.SaveValidationIssue issue) =>
        new()
        {
            Severity = issue.Severity switch
            {
                ArcNET.Editor.SaveValidationSeverity.Error => SaveValidationSeverity.Error,
                ArcNET.Editor.SaveValidationSeverity.Warning => SaveValidationSeverity.Warning,
                _ => SaveValidationSeverity.Info,
            },
            FilePath = issue.FilePath,
            Message = issue.Message,
        };
}

public enum SaveValidationSeverity
{
    Info,
    Warning,
    Error,
}

/// <summary>
/// Shared save-slot validator surface that delegates to the legacy implementation
/// while projecting findings into package-local validation types.
/// </summary>
public static class SaveGameValidator
{
    public static IReadOnlyList<SaveValidationIssue> Validate(LoadedSave save) =>
        MapIssues(ArcNET.Editor.SaveGameValidator.Validate(save));

    public static IReadOnlyList<SaveValidationIssue> ValidateMobileMd(string virtualPath, MobileMdFile md) =>
        MapIssues(ArcNET.Editor.SaveGameValidator.ValidateMobileMd(virtualPath, md));

    public static IReadOnlyList<SaveValidationIssue> ValidateMobileMdy(string virtualPath, MobileMdyFile mdy) =>
        MapIssues(ArcNET.Editor.SaveGameValidator.ValidateMobileMdy(virtualPath, mdy));

    public static IReadOnlyList<SaveValidationIssue> ValidateMob(string virtualPath, MobData mob) =>
        MapIssues(ArcNET.Editor.SaveGameValidator.ValidateMob(virtualPath, mob));

    private static IReadOnlyList<SaveValidationIssue> MapIssues(
        IReadOnlyList<ArcNET.Editor.SaveValidationIssue> legacyIssues
    ) => [.. legacyIssues.Select(SaveValidationIssue.FromLegacy)];
}
