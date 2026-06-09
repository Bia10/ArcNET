namespace ArcNET.Diagnostics;

public sealed record class ModuleSymbolQueryRequest(string? Filter, int Limit = 100, bool DuplicatesOnly = false);
