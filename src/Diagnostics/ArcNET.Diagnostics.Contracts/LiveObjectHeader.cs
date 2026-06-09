namespace ArcNET.Diagnostics.Contracts;

public readonly record struct LiveObjectHeader(
    int ObjectTypeRaw,
    string? ObjectTypeName,
    LiveOid ObjectId,
    LiveOid PrototypeId,
    string PrototypeHandle
);
