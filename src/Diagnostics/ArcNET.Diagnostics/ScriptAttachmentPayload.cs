namespace ArcNET.Diagnostics;

public sealed record class ScriptAttachmentPayload(
    ScriptAttachmentRecordSnapshot Script,
    NativeReadSnapshot NativeRead
);
