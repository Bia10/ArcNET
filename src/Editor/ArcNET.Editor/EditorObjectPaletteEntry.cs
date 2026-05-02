using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// One proto-backed object palette entry that hosts can browse and turn into object-brush requests.
/// </summary>
public sealed class EditorObjectPaletteEntry
{
    /// <summary>
    /// Defining proto asset.
    /// </summary>
    public required EditorAssetEntry Asset { get; init; }

    /// <summary>
    /// Parsed format of the defining asset.
    /// </summary>
    public FileFormat Format => Asset.Format;

    /// <summary>
    /// Proto number derived from the defining asset path.
    /// </summary>
    public required int ProtoNumber { get; init; }

    /// <summary>
    /// Object type stored by the proto.
    /// </summary>
    public required ObjectType ObjectType { get; init; }

    /// <summary>
    /// Stable palette category derived from <see cref="ObjectType"/>.
    /// </summary>
    public string Category => ObjectType.ToString();

    /// <summary>
    /// Relative proto subdirectory under <c>proto/</c>, or <see langword="null"/> when the entry is in the root.
    /// </summary>
    public string? PaletteGroup { get; init; }

    /// <summary>
    /// Resolved display name for the proto when one was available.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Raw message index stored in <c>ObjFName</c>, or <see langword="null"/> when absent.
    /// </summary>
    public int? NameMessageIndex { get; init; }

    /// <summary>
    /// Raw message index stored in <c>ObjFDescription</c>, or <see langword="null"/> when absent.
    /// </summary>
    public int? DescriptionMessageIndex { get; init; }

    /// <summary>
    /// Resolved description text when one was available.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Current art identifier stored in <c>ObjFCurrentAid</c>, or <see langword="null"/> when absent.
    /// </summary>
    public ArtId? CurrentArtId { get; init; }

    /// <summary>
    /// Bound ART asset path resolved through an optional resolver, or <see langword="null"/> when no binding exists.
    /// </summary>
    public string? ArtAssetPath { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when <see cref="ArtAssetPath"/> is populated.
    /// </summary>
    public bool HasArtBinding => !string.IsNullOrWhiteSpace(ArtAssetPath);

    /// <summary>
    /// Browser-friendly ART detail resolved through an optional resolver, or <see langword="null"/> when unavailable.
    /// </summary>
    public EditorArtDefinition? ArtDetail { get; init; }

    /// <summary>
    /// Preview-ready ART projection resolved through an optional resolver, or <see langword="null"/> when unavailable.
    /// </summary>
    public EditorArtPreview? ArtPreview { get; init; }

    /// <summary>
    /// Creates one stamp-from-proto brush request for this palette entry.
    /// </summary>
    public EditorMapObjectBrushRequest CreateStampRequest() => EditorMapObjectBrushRequest.StampFromProto(ProtoNumber);

    /// <summary>
    /// Creates one replace-with-proto brush request for this palette entry.
    /// </summary>
    public EditorMapObjectBrushRequest CreateReplaceRequest() =>
        EditorMapObjectBrushRequest.ReplaceWithProto(ProtoNumber);

    /// <summary>
    /// Creates one palette placement request for this entry with optional initial transforms.
    /// </summary>
    public EditorObjectPalettePlacementRequest CreatePlacementRequest(
        int deltaTileX = 0,
        int deltaTileY = 0,
        float? rotation = null,
        float? rotationPitch = null,
        bool alignToTileGrid = true
    ) =>
        EditorObjectPalettePlacementRequest.Place(
            ProtoNumber,
            deltaTileX,
            deltaTileY,
            rotation,
            rotationPitch,
            alignToTileGrid
        );

    /// <summary>
    /// Creates one named placement preset for this entry with optional initial transforms.
    /// </summary>
    public EditorObjectPalettePlacementPreset CreatePlacementPreset(
        string presetId,
        string? name = null,
        string? description = null,
        int deltaTileX = 0,
        int deltaTileY = 0,
        float? rotation = null,
        float? rotationPitch = null,
        bool alignToTileGrid = true
    ) =>
        EditorObjectPalettePlacementPreset.Create(
            presetId,
            name ?? DisplayName ?? Asset.AssetPath,
            description,
            CreatePlacementRequest(deltaTileX, deltaTileY, rotation, rotationPitch, alignToTileGrid)
        );
}
