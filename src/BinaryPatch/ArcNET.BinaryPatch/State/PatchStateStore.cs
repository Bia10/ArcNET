using System.Text.Json;

namespace ArcNET.BinaryPatch.State;

/// <summary>
/// Persists and reads the <see cref="PatchState"/> file that tracks which patch sets have
/// been applied to a game installation.
/// </summary>
/// <remarks>
/// The state file is written as <c>.arcnet-patches.json</c> in the game directory root.
/// It is created on first apply and removed when all entries are cleared (full revert).
/// </remarks>
public static class PatchStateStore
{
    private const string FileName = ".arcnet-patches.json";

    private static string StatePath(string gameDir) => Path.Combine(gameDir, FileName);

    /// <summary>Loads the current <see cref="PatchState"/> from the game directory.</summary>
    /// <returns>The persisted state, or an empty state when the file does not exist.</returns>
    public static PatchState Load(string gameDir)
    {
        var path = StatePath(gameDir);
        if (!File.Exists(path))
            return new PatchState();

        var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
        return JsonSerializer.Deserialize(json, PatchStateJsonContext.Default.PatchState) ?? new PatchState();
    }

    /// <summary>
    /// Records that <paramref name="patchSet"/> was just applied and saves the updated state.
    /// </summary>
    /// <remarks>
    /// If a record for the same <see cref="BinaryPatchSet.Name"/> already exists it is
    /// replaced (re-apply overwrites the timestamp).
    /// </remarks>
    public static PatchState RecordApply(string gameDir, BinaryPatchSet patchSet)
    {
        var state = Load(gameDir);
        var name = patchSet.Name;

        state.Applied.RemoveAll(e => e.PatchSetName == name);
        state.Applied.Add(
            new PatchStateEntry
            {
                PatchSetName = name,
                PatchSetVersion = patchSet.Version,
                AppliedAt = DateTimeOffset.UtcNow,
            }
        );

        Save(gameDir, state);
        return state;
    }

    /// <summary>
    /// Removes the record for <paramref name="patchSet"/> and saves the updated state.
    /// Deletes the state file when the applied list becomes empty.
    /// </summary>
    public static PatchState RecordRevert(string gameDir, BinaryPatchSet patchSet)
    {
        var state = Load(gameDir);
        state.Applied.RemoveAll(e => e.PatchSetName == patchSet.Name);

        var filePath = StatePath(gameDir);
        if (state.Applied.Count == 0)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        else
        {
            Save(gameDir, state);
        }

        return state;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="patchSet"/> is listed as applied
    /// (does not inspect actual file bytes — use <see cref="BinaryPatcher.Verify"/> for that).
    /// </summary>
    public static bool IsRecorded(string gameDir, BinaryPatchSet patchSet) =>
        Load(gameDir).Applied.Exists(e => e.PatchSetName == patchSet.Name);

    private static void Save(string gameDir, PatchState state)
    {
        var json = JsonSerializer.Serialize(state, PatchStateJsonContext.Default.PatchState);
        File.WriteAllText(StatePath(gameDir), json, System.Text.Encoding.UTF8);
    }
}
