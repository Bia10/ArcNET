using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class TimelineRequest(RuntimeProfileSnapshot RuntimeProfile, bool HasModuleSymbols);
