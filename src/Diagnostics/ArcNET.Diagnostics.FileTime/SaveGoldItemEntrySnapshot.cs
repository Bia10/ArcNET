namespace ArcNET.Diagnostics;

public sealed record SaveGoldItemEntrySnapshot(
    byte[] ObjectIdBytes,
    int Quantity,
    bool HasParent,
    bool FoundInPlayerCharacter,
    IReadOnlyList<SaveGoldItemPropertySnapshot> PositiveInt32Properties
);
