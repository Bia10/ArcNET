namespace ArcNET.Diagnostics;

public sealed record class CeSourceFunctionSnapshot(
    string Name,
    string RelativePath,
    int LineNumber,
    string Area,
    bool IsStatic,
    string Signature,
    CeSourceCoverageSnapshot Coverage,
    CeSourceSymbolCoverageSnapshot Symbol
);
