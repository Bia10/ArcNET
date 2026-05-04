using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Host-facing capability discovery snapshot for the current editor backend and loaded workspace.
/// </summary>
public sealed class EditorCapabilitySummary
{
    private readonly HashSet<EditorCapability> _supported;
    private readonly HashSet<EditorCapability> _available;

    internal static readonly IReadOnlyList<EditorCapability> SupportedCapabilityOrder =
    [
        EditorCapability.WorkspaceLoadContentDirectory,
        EditorCapability.WorkspaceLoadGameInstall,
        EditorCapability.WorkspaceLoadModule,
        EditorCapability.WorkspaceComposeSaveSlot,
        EditorCapability.AssetCatalog,
        EditorCapability.AssetDependencySummary,
        EditorCapability.WorkspaceValidation,
        EditorCapability.SessionStagedUndoRedo,
        EditorCapability.SessionAppliedHistory,
        EditorCapability.SessionPartialApplySaveDiscard,
        EditorCapability.ProjectPersistence,
        EditorCapability.ProjectRestore,
        EditorCapability.DialogEditing,
        EditorCapability.ScriptEditing,
        EditorCapability.SaveEditing,
        EditorCapability.TerrainPaletteBrowsing,
        EditorCapability.TerrainLayerEditing,
        EditorCapability.TrackedTerrainToolWorkflow,
        EditorCapability.ObjectPaletteBrowsing,
        EditorCapability.ObjectPlacement,
        EditorCapability.TrackedObjectPlacementWorkflow,
        EditorCapability.ObjectInspectorSummary,
        EditorCapability.ObjectInspectorFlags,
        EditorCapability.ObjectInspectorScriptAttachments,
        EditorCapability.ObjectInspectorCritterProgression,
        EditorCapability.ObjectInspectorLight,
        EditorCapability.ObjectInspectorGenerator,
        EditorCapability.ObjectInspectorBlending,
        EditorCapability.ObjectTransformEditing,
        EditorCapability.SectorLightEditing,
        EditorCapability.SectorTileScriptEditing,
        EditorCapability.MapPreview,
        EditorCapability.MapScenePreview,
        EditorCapability.SceneHitTesting,
        EditorCapability.ArtPreview,
        EditorCapability.AudioPreviewWave,
    ];

    private EditorCapabilitySummary(
        IReadOnlyList<EditorCapability> supportedCapabilities,
        IReadOnlyList<EditorCapability> availableCapabilities
    )
    {
        SupportedCapabilities = supportedCapabilities;
        AvailableCapabilities = availableCapabilities;
        _supported = supportedCapabilities.ToHashSet();
        _available = availableCapabilities.ToHashSet();
    }

    /// <summary>
    /// Capabilities implemented by the current SDK build, ordered for stable host inspection.
    /// </summary>
    public IReadOnlyList<EditorCapability> SupportedCapabilities { get; }

    /// <summary>
    /// Capabilities that the currently loaded workspace can use immediately.
    /// This is a subset of <see cref="SupportedCapabilities"/>.
    /// </summary>
    public IReadOnlyList<EditorCapability> AvailableCapabilities { get; }

    /// <summary>
    /// Returns <see langword="true"/> when the current SDK build implements the supplied capability.
    /// </summary>
    public bool Supports(EditorCapability capability) => _supported.Contains(capability);

    /// <summary>
    /// Returns <see langword="true"/> when the supplied capability is both implemented by the SDK
    /// and currently actionable for the loaded workspace.
    /// </summary>
    public bool IsAvailable(EditorCapability capability) => _available.Contains(capability);

    internal static EditorCapabilitySummary Create(EditorWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var available = new List<EditorCapability>(capacity: SupportedCapabilityOrder.Count);
        available.Add(EditorCapability.WorkspaceLoadContentDirectory);
        available.Add(EditorCapability.AssetCatalog);
        available.Add(EditorCapability.AssetDependencySummary);
        available.Add(EditorCapability.WorkspaceValidation);
        available.Add(EditorCapability.SessionStagedUndoRedo);
        available.Add(EditorCapability.SessionAppliedHistory);
        available.Add(EditorCapability.SessionPartialApplySaveDiscard);
        available.Add(EditorCapability.ProjectPersistence);
        available.Add(EditorCapability.ProjectRestore);

        if (!string.IsNullOrWhiteSpace(workspace.GameDirectory))
            available.Add(EditorCapability.WorkspaceLoadGameInstall);

        if (workspace.Module is not null)
            available.Add(EditorCapability.WorkspaceLoadModule);

        if (workspace.HasSaveLoaded)
        {
            available.Add(EditorCapability.WorkspaceComposeSaveSlot);
            available.Add(EditorCapability.SaveEditing);
        }

        if (HasAssets(workspace, FileFormat.Dialog))
            available.Add(EditorCapability.DialogEditing);

        if (HasAssets(workspace, FileFormat.Script))
            available.Add(EditorCapability.ScriptEditing);

        if (HasAssets(workspace, FileFormat.MapProperties))
            available.Add(EditorCapability.TerrainPaletteBrowsing);

        if (HasAssets(workspace, FileFormat.Proto))
        {
            available.Add(EditorCapability.ObjectPaletteBrowsing);
            available.Add(EditorCapability.ObjectInspectorSummary);
            available.Add(EditorCapability.ObjectInspectorFlags);
            available.Add(EditorCapability.ObjectInspectorScriptAttachments);
            available.Add(EditorCapability.ObjectInspectorCritterProgression);
            available.Add(EditorCapability.ObjectInspectorLight);
            available.Add(EditorCapability.ObjectInspectorGenerator);
            available.Add(EditorCapability.ObjectInspectorBlending);
        }

        if (HasAssets(workspace, FileFormat.Art))
            available.Add(EditorCapability.ArtPreview);

        if (workspace.AudioAssets.Count > 0)
            available.Add(EditorCapability.AudioPreviewWave);

        if (HasAssets(workspace, FileFormat.Sector))
        {
            available.Add(EditorCapability.TerrainLayerEditing);
            available.Add(EditorCapability.ObjectPlacement);
            available.Add(EditorCapability.ObjectTransformEditing);
            available.Add(EditorCapability.SectorLightEditing);
            available.Add(EditorCapability.SectorTileScriptEditing);
        }

        if (workspace.Index.MapNames.Count > 0)
        {
            available.Add(EditorCapability.MapPreview);
            available.Add(EditorCapability.MapScenePreview);
            available.Add(EditorCapability.SceneHitTesting);
        }

        if (
            workspace.Index.MapNames.Count > 0
            && HasAssets(workspace, FileFormat.MapProperties)
            && HasAssets(workspace, FileFormat.Sector)
        )
        {
            available.Add(EditorCapability.TrackedTerrainToolWorkflow);
        }

        if (
            workspace.Index.MapNames.Count > 0
            && HasAssets(workspace, FileFormat.Proto)
            && HasAssets(workspace, FileFormat.Sector)
        )
        {
            available.Add(EditorCapability.TrackedObjectPlacementWorkflow);
        }

        return new EditorCapabilitySummary(
            supportedCapabilities: [.. SupportedCapabilityOrder],
            availableCapabilities: [.. available.Distinct()]
        );
    }

    private static bool HasAssets(EditorWorkspace workspace, FileFormat format) =>
        workspace.Assets.FindByFormat(format).Count > 0;
}
