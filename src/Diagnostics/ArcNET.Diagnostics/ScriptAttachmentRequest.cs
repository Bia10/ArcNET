namespace ArcNET.Diagnostics;

public sealed record class ScriptAttachmentRequest(
    AttachedSessionSnapshot Session,
    string HandleToken,
    string AttachmentPointText
);
