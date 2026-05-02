namespace ArcNET.Editor;

/// <summary>
/// Stable host-facing editor backend capabilities that a frontend can discover at runtime.
/// </summary>
public enum EditorCapability
{
    WorkspaceLoadContentDirectory = 0,
    WorkspaceLoadGameInstall = 1,
    WorkspaceLoadModule = 2,
    WorkspaceComposeSaveSlot = 3,
    AssetCatalog = 4,
    AssetDependencySummary = 5,
    WorkspaceValidation = 6,
    SessionStagedUndoRedo = 7,
    SessionAppliedHistory = 8,
    SessionPartialApplySaveDiscard = 9,
    ProjectPersistence = 10,
    ProjectRestore = 11,
    DialogEditing = 12,
    ScriptEditing = 13,
    SaveEditing = 14,
    TerrainPaletteBrowsing = 15,
    TerrainLayerEditing = 16,
    TrackedTerrainToolWorkflow = 17,
    ObjectPaletteBrowsing = 18,
    ObjectPlacement = 19,
    TrackedObjectPlacementWorkflow = 20,
    ObjectTransformEditing = 21,
    SectorLightEditing = 22,
    SectorTileScriptEditing = 23,
    MapPreview = 24,
    MapScenePreview = 25,
    SceneHitTesting = 26,
    ArtPreview = 27,
    AudioPreviewWave = 28,
}
