namespace ArcNET.Diagnostics;

public sealed record class ScriptAttachmentSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    string RequestedAttachmentPointText,
    int? AttachmentPoint,
    string AttachmentPointName,
    string TargetHandleText,
    string TargetText,
    ScriptAttachmentRecordSnapshot? Script,
    NativeReadSnapshot? NativeRead,
    IReadOnlyList<string> Notes
);
