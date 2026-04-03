namespace ArcNET.BinaryPatch.State;

/// <summary>Represents one applied patch set recorded in the game-directory state file.</summary>
public sealed class PatchStateEntry
{
    /// <summary>The <see cref="BinaryPatchSet.Name"/> of the applied set.</summary>
    public string PatchSetName { get; set; } = "";

    /// <summary>The <see cref="BinaryPatchSet.Version"/> of the applied set.</summary>
    public string PatchSetVersion { get; set; } = "";

    /// <summary>UTC timestamp at which the set was applied.</summary>
    public DateTimeOffset AppliedAt { get; set; }
}

/// <summary>Root object persisted to <c>.arcnet-patches.json</c> inside the game directory.</summary>
public sealed class PatchState
{
    /// <summary>All currently applied patch sets, in apply-order.</summary>
    public List<PatchStateEntry> Applied { get; set; } = [];
}
