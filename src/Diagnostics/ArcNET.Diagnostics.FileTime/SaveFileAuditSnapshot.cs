namespace ArcNET.Diagnostics;

public sealed record SaveFileAuditSnapshot(
    DateTimeOffset CapturedAt,
    string LeaderName,
    int LeaderLevel,
    int MapId,
    SaveTypedAssetSummarySnapshot Assets,
    SaveValidationSummarySnapshot Validation,
    SaveObjectFieldAuditSnapshot Objects,
    PlayerCharacterAuditSnapshot? PlayerCharacter,
    IReadOnlyList<SaveParseErrorSnapshot> ParseErrors,
    IReadOnlyList<SaveValidationIssueSnapshot> ValidationIssues,
    IReadOnlyList<MobileMdyAuditSnapshot> MobileMdys,
    int ValidationIssueLimit,
    int MobileMdyLimit
);
