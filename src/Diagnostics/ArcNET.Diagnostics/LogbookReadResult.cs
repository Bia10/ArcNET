namespace ArcNET.Diagnostics;

public sealed record class LogbookReadResult(LogbookPayload Data, IReadOnlyList<string> Notes);
