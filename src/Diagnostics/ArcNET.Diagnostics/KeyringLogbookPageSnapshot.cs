namespace ArcNET.Diagnostics;

public sealed record class KeyringLogbookPageSnapshot(IReadOnlyList<KeyringLogbookEntrySnapshot> Entries);
