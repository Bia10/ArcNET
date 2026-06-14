using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class SchematicGrantRequest(
    AttachedSessionSnapshot Session,
    string TargetHandleToken,
    string SchematicIdText,
    string TimeoutMillisecondsText
);
