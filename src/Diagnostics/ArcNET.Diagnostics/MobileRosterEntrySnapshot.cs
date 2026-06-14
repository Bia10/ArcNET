namespace ArcNET.Diagnostics;

public sealed record class MobileRosterEntrySnapshot(
    string HandleHex,
    string DisplayText,
    string ObjectTypeText,
    string ObjectIdText,
    string PrototypeText,
    string PrototypeHandleText,
    string StatusText,
    int? ProtoNumber,
    string ResolutionSource
);
