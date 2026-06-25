namespace ArcNET.GameData.Workspace;

/// <summary>
/// Canonical spell metadata projected from the original Arcanum spell enum.
/// </summary>
public sealed record WorkspaceSpellCatalogEntry(int SpellId, string Name, int CollegeId, string CollegeName, int Level);
