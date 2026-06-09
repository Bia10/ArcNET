namespace ArcNET.Diagnostics;

public sealed record MobileMdyAuditSnapshot(
    string Path,
    int RecordCount,
    int MobRecordCount,
    int CharacterRecordCount,
    int DuplicateObjectIdCount,
    int PropertyCount,
    int PropertyParseNoteCount,
    IReadOnlyList<MobileMdyObjectTypeSnapshot> ObjectTypes
);
