using ArcNET.Core;
using ArcNET.GameData;

namespace ArcNET.Editor;

/// <summary>
/// High-level editor session that composes loose game data and an optional save slot into
/// one frontend-facing SDK surface.
/// </summary>
/// <remarks>
/// This type is intentionally UI-agnostic. Desktop, web, and CLI frontends can bind to one
/// workspace instance instead of manually coordinating <see cref="GameDataLoader"/>,
/// <see cref="SaveGameLoader"/>, <see cref="GameDataStore"/>, and <see cref="LoadedSave"/>.
/// The workspace may be backed either by a loose/extracted content directory or by a real
/// game installation whose DAT archives are read directly.
/// </remarks>
public sealed class EditorWorkspace
{
    /// <summary>
    /// Loose or extracted content directory associated with this workspace.
    /// For install-backed workspaces this is the conventional <c>data</c> override directory
    /// under <see cref="GameDirectory"/>, even when the actual content was also loaded from DAT archives.
    /// </summary>
    public required string ContentDirectory { get; init; }

    /// <summary>
    /// Optional Arcanum installation root used for installation-type detection and
    /// future install-root services.
    /// </summary>
    public string? GameDirectory { get; init; }

    /// <summary>
    /// Installation type detected from <see cref="GameDirectory"/>, or <see langword="null"/>
    /// when the workspace was loaded without an installation root.
    /// </summary>
    public ArcanumInstallationType? InstallationType { get; init; }

    /// <summary>
    /// Parsed loose game data used by frontend editors, browsers, and validation tools.
    /// </summary>
    public required GameDataStore GameData { get; init; }

    /// <summary>
    /// Read-only catalog of parsed game-data assets and their winning provenance.
    /// </summary>
    public EditorAssetCatalog Assets { get; init; } = EditorAssetCatalog.Empty;

    /// <summary>
    /// Higher-level workspace index for message ownership, proto lookup, and reverse proto references.
    /// </summary>
    public EditorAssetIndex Index { get; init; } = EditorAssetIndex.Empty;

    /// <summary>
    /// Diagnostics captured while the workspace was loaded.
    /// </summary>
    public EditorWorkspaceLoadReport LoadReport { get; init; } = EditorWorkspaceLoadReport.Empty;

    /// <summary>
    /// Cross-file validation findings derived from the indexed workspace assets.
    /// </summary>
    public EditorWorkspaceValidationReport Validation { get; init; } = EditorWorkspaceValidationReport.Empty;

    /// <summary>
    /// Optional loaded save slot associated with this workspace.
    /// </summary>
    public LoadedSave? Save { get; init; }

    /// <summary>
    /// Save directory used when <see cref="Save"/> was loaded.
    /// </summary>
    public string? SaveFolder { get; init; }

    /// <summary>
    /// Save slot name used when <see cref="Save"/> was loaded.
    /// </summary>
    public string? SaveSlotName { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this workspace includes a loaded save slot.
    /// </summary>
    public bool HasSaveLoaded => Save is not null;

    /// <summary>
    /// Creates a stateful <see cref="SaveGameEditor"/> for the loaded save slot.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the workspace was loaded without a save slot.
    /// </exception>
    public SaveGameEditor CreateSaveEditor()
    {
        if (Save is null)
        {
            throw new InvalidOperationException(
                "This workspace was loaded without a save slot. Provide SaveFolder and SaveSlotName to EditorWorkspaceLoader first."
            );
        }

        return new SaveGameEditor(Save);
    }
}
