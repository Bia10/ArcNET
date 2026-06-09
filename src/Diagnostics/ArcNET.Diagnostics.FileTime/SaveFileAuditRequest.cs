using ArcNET.Editor;

namespace ArcNET.Diagnostics;

public sealed record SaveFileAuditRequest(LoadedSave Save)
{
    public int ValidationIssueLimit { get; init; } = 64;

    public int MobileMdyLimit { get; init; } = 64;

    public int FieldLimit { get; init; } = 20;

    public int CharacterSarLimit { get; init; } = 32;
}
