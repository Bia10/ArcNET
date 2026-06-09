namespace ArcNET.Diagnostics;

public sealed record SaveGoldItemFileSnapshot(string Path, IReadOnlyList<SaveGoldItemEntrySnapshot> Items);
