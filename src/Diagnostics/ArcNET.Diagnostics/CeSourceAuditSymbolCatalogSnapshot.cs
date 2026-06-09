namespace ArcNET.Diagnostics;

public sealed record class CeSourceAuditSymbolCatalogSnapshot(
    string ModulePath,
    string ModuleFileName,
    int FunctionCount,
    int UniqueNameCount,
    int DuplicateNameCount
);
