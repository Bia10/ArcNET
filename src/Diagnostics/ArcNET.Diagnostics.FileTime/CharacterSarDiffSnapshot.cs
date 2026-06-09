namespace ArcNET.Diagnostics;

public sealed record CharacterSarDiffSnapshot(IReadOnlyList<CharacterSarDiffEntrySnapshot> Entries)
{
    public bool HasChanges => Entries.Count > 0;
}
