using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class ObjectExplorerRequest(RuntimeProfileSnapshot RuntimeProfile, bool HasModuleSymbols);
