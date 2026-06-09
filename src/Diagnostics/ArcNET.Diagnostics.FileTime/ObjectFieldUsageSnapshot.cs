namespace ArcNET.Diagnostics;

public sealed record ObjectFieldUsageSnapshot(string Field, int Count, int ParseNoteCount, long TotalRawBytes);
