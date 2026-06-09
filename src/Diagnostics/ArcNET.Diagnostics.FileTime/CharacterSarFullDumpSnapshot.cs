namespace ArcNET.Diagnostics;

public sealed record CharacterSarFullDumpSnapshot(IReadOnlyList<CharacterSarFullDumpEntrySnapshot> Entries)
{
    public int PrintedCount => Entries.Count;
}
