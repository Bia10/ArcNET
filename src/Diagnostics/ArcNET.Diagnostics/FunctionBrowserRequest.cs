using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class FunctionBrowserRequest(RuntimeProfileSnapshot RuntimeProfile, bool HasModuleSymbols);
