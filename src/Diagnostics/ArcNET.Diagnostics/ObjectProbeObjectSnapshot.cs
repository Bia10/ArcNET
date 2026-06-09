namespace ArcNET.Diagnostics;

public sealed record class ObjectProbeObjectSnapshot(
    string HandleHex,
    string ResolutionSource,
    string ObjectTypeText,
    string ObjectIdText,
    string PrototypeText,
    string PrototypeHandleText,
    string AddressText,
    string StatusText,
    IReadOnlyList<ObjectProbeSectionSnapshot> Sections,
    IReadOnlyList<ObjectProbeDetailSnapshot> Details
);
