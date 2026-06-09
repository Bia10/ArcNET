namespace ArcNET.Diagnostics;

public sealed record class InterceptTarget(
    string Key,
    uint Address,
    uint? Rva,
    string Site,
    string Summary,
    string Resolution
);
