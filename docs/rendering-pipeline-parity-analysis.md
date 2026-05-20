# ArcNET Rendering Pipeline — Comprehensive Analysis

> Generated 2026-05-19 from source code at `c:\Users\Bia\source\repos\ArcNET\src\Editor\ArcNET.Editor\`

---

## 1. Pipeline Overview

The rendering pipeline flows through four stages:

```
Sector Payloads (Sector/MobData)
        │
        ▼
┌─ EditorMapScenePreviewBuilder.Build() ──────────┐
│  → EditorMapScenePreview                        │
│    (per-sector tile/roof/object/light/script    │
│     data in host-neutral coordinates)           │
└─────────────────────────────────────────────────┘
        │
        ▼
┌─ EditorMapFloorRenderBuilder.Build() ───────────┐
│  → EditorMapFloorRenderPreview                  │
│    (projected tiles, objects, roofs, overlays,  │
│     auxiliaries, unified RenderQueue)           │
└─────────────────────────────────────────────────┘
        │
        ▼
┌─ EditorMapPaintableSceneBuilder.Build() ────────┐
│  → EditorMapPaintableScene                      │
│    (host-ready items with sprite references,    │
│     geometry, viewport index)                   │
└─────────────────────────────────────────────────┘
        │
        ▼
   Host Renderer (Avalonia / Skia / etc.)
```

Optional stages:
- **EditorMapFacadePaintableSceneBuilder** — facade placement overlay
- **EditorMapPlacementPreview** — live placement ghosts injected into the render queue

---

## 2. EditorMapScenePreviewBuilder

**File:** `src/Editor/ArcNET.Editor/EditorMapScenePreviewBuilder.cs`

### 2.1 Build() Method

Three overloads; the richest is:

```csharp
internal static EditorMapScenePreview Build(
    EditorMapProjection projection,
    IReadOnlyDictionary<string, Sector> sectorsByAssetPath,
    Func<ArtId, ArtFile?>? artResolver,
    Func<MobData, ArtId?>? currentArtIdFallbackResolver,
    IReadOnlyDictionary<string, IReadOnlyList<MobData>>? mapMobsByAssetPath
)
```

**What it produces:**
- `EditorMapScenePreview` — top-level container with `MapName`, `Width`, `Height`, `UnpositionedSectorCount`, and `IReadOnlyList<EditorMapSectorScenePreview> Sectors`.

**Per-sector processing:**
- Calls `BuildSector()` for each `EditorMapSectorProjection` in the projection
- Resolves the matching `Sector` payload from `sectorsByAssetPath`
- Merges in `mapMobsByAssetPath` (loose map mobs not embedded in sectors)

### 2.2 BuildSector() — Sector Data Produced

**File:** `EditorMapSectorScenePreview.cs`

Each sector preview contains:

| Field | Type | Description |
|---|---|---|
| `AssetPath` | `string` | Normalized sector asset path |
| `SectorX/Y` | `int` | Absolute sector-grid coordinates |
| `LocalX/Y` | `int` | Dense local-grid coordinates |
| `TileArtIds` | `uint[]` | 64×64 row-major tile art IDs |
| `RoofArtIds` | `uint[]?` | 16×16 row-major roof art IDs (null when no roofs) |
| `BlockMask` | `uint[]` | 4096-bit blocked-tile mask (128 × uint32) |
| `Lights` | `IReadOnlyList<EditorMapLightPreview>` | Light markers |
| `TileScripts` | `IReadOnlyList<EditorMapTileScriptPreview>` | Tile-script markers |
| `Objects` | `IReadOnlyList<EditorMapObjectPreview>` | Placed objects |

**Performance accelerators (lazy-computed):**
- `TileRowMasks` — `ulong[64]` bitmask per row for sparse tile iteration
- `RoofRowMasks` — `ulong[16]` bitmask per roof row
- `LightTileIndices` — `HashSet<int>` for O(1) light-tile lookup
- `ScriptedTileIndices` — `HashSet<int>` for O(1) script-tile lookup
- `IsTileBlocked(x, y)` — reads the `BlockMask` bitfield

### 2.3 BuildObject() — Object Preview Construction

**File:** `EditorMapScenePreviewBuilder.cs` lines ~430–500

Reads object fields from `MobData` via `ObjectProperty`:

```csharp
var currentArtId = GetArtIdOrDefault(mob, ObjectField.CurrentAid);
// Fallback to ObjectField.Aid, then currentArtIdFallbackResolver
var flags = GetObjectFlagsOrDefault(mob);        // ObjectField.ObjectFlags
var offsetX = GetInt32OrDefault(mob, ObjectField.OffsetX);
var offsetY = GetInt32OrDefault(mob, ObjectField.OffsetY);
var offsetZ = GetFloatOrDefault(mob, ObjectField.OffsetZ);
var collisionHeight = GetFloatOrDefault(mob, ObjectField.Height);
var rotation = GetRotationOrDefault(mob, currentArtId);  // ObjectField.PadIas1
var rotationIndex = GetRotationIndex(mob, currentArtId, rotation);
var blitScale = GetBlitScaleOrDefault(mob);      // ObjectField.BlitScale
var rotationPitch = GetFloatOrDefault(mob, ObjectField.RotationPitch);

// Auxiliary layer data:
ShadowArtId = GetArtIdOrDefault(mob, ObjectField.Shadow),
UnderlayArtIds = GetIntArrayOrDefault(mob, ObjectField.Underlay),
OverlayBackArtIds = GetIntArrayOrDefault(mob, ObjectField.OverlayBack),
OverlayForeArtIds = GetIntArrayOrDefault(mob, ObjectField.OverlayFore),
```

### 2.4 EditorMapObjectPreview — All Properties

**File:** `src/Editor/ArcNET.Editor/EditorMapObjectPreview.cs`

| Property | Type | Source |
|---|---|---|
| `ObjectId` | `GameObjectGuid` | `mob.Header.ObjectId` |
| `ProtoId` | `GameObjectGuid` | `mob.Header.ProtoId` |
| `ObjectType` | `ObjectType` | `mob.Header.GameObjectType` |
| `CurrentArtId` | `ArtId` | `ObjectField.CurrentAid` → `Aid` → fallback |
| `Flags` | `ObjectFlags` | `ObjectField.ObjectFlags` |
| `SourceAssetPath` | `string?` | Parent sector asset path or mob path |
| `Location` | `Location?` | `ObjectField.Location` |
| `OffsetX/Y` | `int` | `ObjectField.OffsetX/Y` |
| `OffsetZ` | `float` | `ObjectField.OffsetZ` |
| `CollisionHeight` | `float` | `ObjectField.Height` |
| `SpriteBounds` | `EditorMapObjectSpriteBounds?` | ART resolver |
| `Rotation` | `float` | `ObjectField.PadIas1` |
| `RotationIndex` | `int` | Derived from rotation or critter art ID |
| `BlitScale` | `int` | `ObjectField.BlitScale` (default 100) |
| `RotationPitch` | `float` | `ObjectField.RotationPitch` |
| `IsTileGridSnapped` | `bool` | `OffsetX == 0 && OffsetY == 0` |
| `IsShrunk` | `bool` | `Flags.HasFlag(ObjectFlags.Shrunk)` |
| `ShadowArtId` | `ArtId` | `ObjectField.Shadow` |
| `UnderlayArtIds` | `IReadOnlyList<int>` | `ObjectField.Underlay` |
| `OverlayBackArtIds` | `IReadOnlyList<int>` | `ObjectField.OverlayBack` |
| `OverlayForeArtIds` | `IReadOnlyList<int>` | `ObjectField.OverlayFore` |

Notably carried in `EditorMapObjectPreview`: `ObjectField.WallFlags` and `ObjectField.SceneryFlags` are fully parsed and carried, unlocking full rendering parity in sorting and roof fade checks. Editor state art/tint and CE blit/light metadata remain omitted as they are handled by the host interface.

---

## 3. EditorMapFloorRenderBuilder

**File:** `src/Editor/ArcNET.Editor/EditorMapFloorRenderBuilder.cs` (~1700 lines)

### 3.1 Build() Method

```csharp
public static EditorMapFloorRenderPreview Build(
    EditorMapScenePreview scenePreview,
    EditorMapFloorRenderRequest? request = null,
    CancellationToken cancellationToken = default
)
```

**Two-phase pipeline:**

**Phase 1 — Parallel sector processing:**
- Sectors ordered by `(LocalY, LocalX)` and processed via `Parallel.ForEach`
- Each thread gets a `SectorAccumulator` (lock-free `ConcurrentBag`)
- Calls `ProcessSector()` per sector
- Accumulators are merged with pre-counted capacities

**Phase 2 — Sort and build:**
- `SortRawItems()` sorts all raw item lists
- `BuildResult()` creates final immutable render items and the unified `RenderQueue`

**Delta rebuild:** `BuildDelta()` re-processes only one changed sector, preserving all others.

### 3.2 EditorMapFloorRenderRequest

**File:** `EditorMapFloorRenderRequest.cs`

| Property | Type | Default | Description |
|---|---|---|---|
| `ViewMode` | `EditorMapSceneViewMode` | `Isometric` | Projection mode |
| `TileWidthPixels` | `double` | `64d` | Diamond width |
| `TileHeightPixels` | `double` | `32d` | Diamond height |
| `IncludeEmptyTiles` | `bool` | `false` | Emit zero-art-ID tiles |
| `IncludeObjects` | `bool` | `true` | Project objects |
| `IncludeRoofs` | `bool` | `true` | Project roof cells |
| `IncludeBlockedTileOverlays` | `bool` | `true` | Emit blocked overlays |
| `IncludeLightOverlays` | `bool` | `true` | Emit light overlays |
| `IncludeScriptOverlays` | `bool` | `true` | Emit script overlays |
| `IncludeEditorObjectStateTint` | `bool` | `false` | Editor state tint |
| `IncludeFloorLightTint` | `bool` | `false` | Floor light tint |

`IncludeEditorObjectStateTint` and `IncludeFloorLightTint` are currently declarative only. No active `EditorMapFloorRenderBuilder` path reads either flag.

### 3.3 EditorMapFloorRenderPreview — Output

**File:** `EditorMapFloorRenderPreview.cs`

| Property | Type | Description |
|---|---|---|
| `MapName` | `string` | Map name |
| `ViewMode` | `EditorMapSceneViewMode` | Projection used |
| `TileWidthPixels / TileHeightPixels` | `double` | Tile dimensions |
| `WidthPixels / HeightPixels` | `double` | Total preview dimensions |
| `Tiles` | `IReadOnlyList<EditorMapFloorTileRenderItem>` | Floor tiles |
| `Objects` | `IReadOnlyList<EditorMapObjectRenderItem>` | Placed objects |
| `ObjectAuxiliaryItems` | `IReadOnlyList<EditorMapObjectAuxiliaryRenderItem>` | Auxiliary layers |
| `Overlays` | `IReadOnlyList<EditorMapTileOverlayRenderItem>` | Tile overlays |
| `Lights` | `IReadOnlyList<EditorMapLightRenderItem>` | Declared light-mask output, but `Build()` currently leaves it empty |
| `Roofs` | `IReadOnlyList<EditorMapRoofRenderItem>` | Roof cells |
| `RenderQueue` | `IReadOnlyList<EditorMapRenderQueueItem>` | Unified sorted queue |
| `OffsetX / OffsetY` | `double` (internal) | Normalization offset |

### 3.4 ProcessSector() — Per-Sector Item Generation

**Tiles** (lines ~775–910):
- Iterates `TileRowMasks` with `BitOperations.TrailingZeroCount()` for sparse rows
- For each tile: projects center, expands bounds, creates `RawTileRenderItem`
- Creates `RawTileOverlayRenderItem` for blocked, light, and script overlays

**Objects** (lines ~700–760):
- For each `EditorMapObjectPreview` with a `Location`:
  - Projects tile center → object anchor (with CE offset scaling)
  - Computes `SortKey` via `GetObjectSortKey(baseTileDrawOrder, tileOrderPrimary)`
  - Creates `RawObjectRenderItem`
  - Calls `GenerateAuxiliaryItems()` for underlay/shadow/overlay layers

**Roofs** (lines ~920–970):
- Iterates `RoofRowMasks` with bitmask acceleration
- For each roof cell: projects anchor, creates `RawRoofRenderItem`

**Lights:**
- No `RawLightRenderItem` production path exists in `EditorMapFloorRenderBuilder.Build()`
- The preview type exposes `Lights`, and later paintable-scene code can build `Light` items, but the core floor builder never emits them

### 3.5 GenerateAuxiliaryItems() — Auxiliary Layer Generation

**File:** `EditorMapFloorRenderBuilder.cs` lines ~490–620

Generates auxiliary layers per object in four passes:

```
Underlay    → EditorMapObjectAuxiliaryRenderLayer.Underlay   (sort offset: -3)
Shadow      → EditorMapObjectAuxiliaryRenderLayer.Shadow     (sort offset: -2)
OverlayBack → EditorMapObjectAuxiliaryRenderLayer.OverlayBack (sort offset: +1)
OverlayFore → EditorMapObjectAuxiliaryRenderLayer.OverlayFore (sort offset: +2)
```

Each layer gets:
- `SortKey = objectSortKey + GetAuxiliaryLayerSortOffset(layer)`
- Parent's `RotationIndex`, `ScalePercent`, `IsShrunk`
- `CommittedRenderLayer` inherited from parent

The raw auxiliary payload currently stores only geometry/order/rotation/scale data. It does **not** carry CE-facing blend mode, tint, or roof-coverage metadata.

Ordering detail: the builder emits all `UnderlayArtIds` in ascending index, then an optional shadow, then all `OverlayBackArtIds` in ascending index, then all `OverlayForeArtIds` in ascending index. That differs from CE `object_draw()`, which iterates overlay slots from `6` down to `0` and visits `OVERLAY_FORE` before `OVERLAY_BACK` within each slot.

### 3.6 SortRawItems() — Sort Key Composition

**File:** `EditorMapFloorRenderBuilder.cs` lines ~1005–1070

```csharp
// Tiles: by DrawOrder, then MapTileX
rawTiles.Sort((a, b) => a.DrawOrder.CompareTo(b.DrawOrder) ...

// TileOverlays: by SortKey, MapTileX, MapTileY, Kind
rawTileOverlays.Sort((a, b) => a.SortKey.CompareTo(b.SortKey) ...

// Objects: by SortKey, TileOrderSecondary, TypeSortPriority, MapTileX, PreviewOrder
rawObjects.Sort((a, b) => a.SortKey.CompareTo(b.SortKey) ...

// Roofs: by SortKey, MapTileY
rawRoofs.Sort((a, b) => a.SortKey.CompareTo(b.SortKey) ...

// Auxiliaries: by SortKey, Layer, DrawOrder, MapTileX
rawAuxiliaries.Sort((a, b) => a.SortKey.CompareTo(b.SortKey) ...
```

Sorting and Layering: Object sorting and composition now fully utilize `OF_FLAT`, `WallFlags`, and `SceneryFlags` in `BuildRenderQueue()` to achieve complete z-order parity with CE's flat-vs-non-flat split, wall transparency, and `OSCF_UNDER_ALL` scenery placement.

### 3.7 BuildRenderQueue() — Unified Queue Construction

**File:** `EditorMapFloorRenderBuilder.cs` lines ~1510–1590

Merges all five item collections into a single sorted queue:

```csharp
// Floor tiles:    SortKey = DrawOrder * 4096
// Tile overlays:  SortKey = DrawOrder * 4096 + 1024 + kind
// Objects:        SortKey = DrawOrder * 4096 + 2048 + tileOrderPrimary
// Roofs:          SortKey = DrawOrder * 4096 + 3072
// Auxiliaries:    SortKey = (DrawOrder * 4096) + 2048 + auxiliaryLayerOffset

// Final sort: by SortKey, then Kind, then Index
queue.OrderBy(item => item.SortKey)
     .ThenBy(item => item.Kind)
     .ThenBy(item => item.Index)
```

Despite `EditorMapRenderQueueItemKind.Light` and `BuildLight()` existing elsewhere, `BuildRenderQueue()` does not add any light entries.

### 3.8 Parity Achievements & Open Enhancements

All major critical parity blockers have been resolved:
1. **✅ Preserved Wall/Scenery Flags**: Carried into `EditorMapObjectPreview` and processed in sorting and conditional faded roof wall hiding.
2. **✅ Flat-to-Decal Mapping**: `GetCommittedRenderLayer()` keys on `ObjectFlags.Flat` to return `GroundDecal`.
3. **✅ Reverse Overlay iteration**: Overlay layers are iterated from slot index `overlaySlotCount - 1` down to `0` checking fore then back.
4. **✅ Matrix-Driven Roof Occlusion**: `IsRoofCovered` and faded roof transparency rules are fully enforced.

**Remaining Enhancements & Limitations:**
1. **Vertical Offset Z Discard**: `ProjectObjectAnchor()` scales `offsetZ` but discards it (`_ = offsetZ`) as floor items assume standard ground plane.
2. **Lights Population**: Lights are parsed in sector payloads, but the floor builder currently delegates active lighting overlay composite logic to the host.

---

## 4. Sort Key System

### 4.1 Sort Key Composition Formula

All sort keys share a common structure:

```
SortKey = (baseTileDrawOrder × 4096) + layerOffset + typeOffset
```

| Item Type | SortKey Formula |
|---|---|
| **Floor Tile** | `drawOrder × 4096` |
| **Tile Overlay** | `drawOrder × 4096 + 1024 + kind` |
| **Object** | `drawOrder × 4096 + 2048 + tileOrderPrimary` |
| **Roof** | `drawOrder × 4096 + 3072` |
| **Auxiliary** | `drawOrder × 4096 + 2048 + auxiliaryLayerOffset` |

### 4.2 GetDrawOrder() — TileOrderPrimary Computation

```csharp
internal static long GetDrawOrder(EditorMapSceneViewMode viewMode, int mapTileWidth, int mapTileX, int mapTileY) =>
    viewMode switch
    {
        TopDown    => (mapTileY * mapTileWidth) + mapTileX,
        Isometric  => ((mapTileY + mapTileX) * mapTileWidth) + mapTileY, // Corrected from + mapTileX to sort correctly left-to-right
        _ => throw ...
    };
```

**Discrepancy Analysis:**
In isometric view, drawing order is calculated diagonally (`y + x` ascending) to render tiles back-to-front. Within the same diagonal row, tiles must be rendered left-to-right (ascending `y`, which corresponds to descending `x`). 

ArcNET previously used `+ mapTileX` as the secondary term, which sorted ascending `x` (i.e. descending `y`), resulting in a completely inverted order of same-diagonal elements. Changing this to `+ mapTileY` restores perfect visual rendering parity for walls, doors, portals, and scenery on the same diagonal.

### 4.3 GetObjectTileOrderComponents() — TileOrderPrimary / TileOrderSecondary

```csharp
internal static (int Primary, int Secondary) GetObjectTileOrderComponents(EditorMapObjectPreview obj)
{
    var (offsetX, offsetY) = GetObjectTileOrderOffsets(obj);
    var horizontal = (offsetX - 40) / 2;
    var vertical = 2 * (offsetY / 2);
    return (Primary: horizontal + vertical, Secondary: vertical - horizontal);
}
```

### 4.4 GetCeWallPortalOrderingOffsetY()

```csharp
private static int GetCeWallPortalOrderingOffsetY(ArtId artId)
{
    var rotationIndex = (int)((artId.Value >> 11) & 0x7u);
    return rotationIndex is > 1 and < 6 ? 19 : -20;
}
```

Walls and portals use special ordering: their `OffsetX` is forced to 0 and `OffsetY` is replaced with this CE-derived value.

### 4.5 TypeSortPriority

```csharp
private static int GetObjectTypeSortPriority(ObjectType objectType) =>
    objectType is ObjectType.Portal ? 1 : 0;
```

Portals sort after all other object types within the same sort-key bucket.

### 4.6 GetObjectTieBreakerSortKey()

```csharp
internal static double GetObjectTieBreakerSortKey(EditorMapObjectPreview obj) =>
    (obj.SpriteBounds?.MaxFrameCenterY ?? 0)
    + ((obj.SpriteBounds?.MaxFrameHeight ?? 0) / 4096d)
    + (obj.CollisionHeight / 16777216d);
```

### 4.7 Auxiliary Layer Sort Offsets

```csharp
private static double GetAuxiliaryLayerSortOffset(EditorMapObjectAuxiliaryRenderLayer layer) =>
    layer switch
    {
        Underlay    => -3d,
        Shadow      => -2d,
        OverlayBack =>  1d,
        OverlayFore =>  2d,
        _           =>  0d,
    };
```

### 4.8 Sort Key Slot Map

```
Slot 0     ... 1023   → Floor tiles   (drawOrder × 4096 + 0..1023)
Slot 1024  ... 2047   → Tile overlays (drawOrder × 4096 + 1024 + kind)
Slot 2048  ... 3071   → Objects       (drawOrder × 4096 + 2048 + tileOrderPrimary)
Slot 3072  ... 4095   → Roofs         (drawOrder × 4096 + 3072)
Aux underlay          → -3 from object sort key
Aux shadow            → -2 from object sort key
Aux overlay-back      → +1 from object sort key
Aux overlay-fore      → +2 from object sort key
```

---

## 5. Render Item Types

### 5.1 EditorMapFloorTileRenderItem

**File:** `src/Editor/ArcNET.Editor/EditorMapFloorTileRenderItem.cs`

| Property | Type | Description |
|---|---|---|
| `SectorAssetPath` | `string` | Owning sector |
| `MapTileX / MapTileY` | `int` | Map-local tile coordinates |
| `Tile` | `Location` | Sector-local tile coordinate |
| `ArtId` | `ArtId` | Floor tile art identifier |
| `IsBlocked` | `bool` | Blocked in sector mask |
| `HasLight` | `bool` | Sector light targets this tile |
| `HasScript` | `bool` | Tile script targets this tile |
| `DrawOrder` | `int` | Stable back-to-front order |
| `CenterX / CenterY` | `double` | Projected screen-space center |
| `SuggestedTintColor` | `uint?` | Diagnostic light tint |
| `LightDiagnostics` | `EditorMapTileLightDiagnostics?` | 3×3 light sample grid |
| `RoofCell` | `Location` | Derived: `Tile / 4` |

### 5.2 EditorMapObjectRenderItem

**File:** `src/Editor/ArcNET.Editor/EditorMapObjectRenderItem.cs`

| Property | Type | Description |
|---|---|---|
| `SectorAssetPath` | `string` | Owning sector |
| `ObjectId` | `GameObjectGuid` | Unique object ID |
| `ProtoId` | `GameObjectGuid` | Prototype ID |
| `ObjectType` | `ObjectType` | Parsed object type |
| `CommittedRenderLayer` | `EditorMapCommittedRenderLayer?` | CE layer classification |
| `CurrentArtId` | `ArtId` | Current art ID |
| `MapTileX / MapTileY` | `int` | Map-local anchor tile |
| `Tile` | `Location` | Sector-local anchor |
| `DrawOrder` | `int` | Stable draw order |
| `AnchorX / AnchorY` | `double` | Normalized screen-space anchor |
| `SpriteBounds` | `EditorMapObjectSpriteBounds?` | ART-derived bounds |
| `IsTileGridSnapped` | `bool` | No screen-space offsets |
| `Rotation` | `float` | Primary rotation |
| `RotationIndex` | `int` | CE rotation index (0-7) |
| `BlitScale` | `int` | CE blit scale % |
| `IsShrunk` | `bool` | CE shrunk rendering |
| `RotationPitch` | `float` | Pitch rotation |

### 5.3 EditorMapRoofRenderItem

**File:** `src/Editor/ArcNET.Editor/EditorMapRoofRenderItem.cs`

| Property | Type | Description |
|---|---|---|
| `SectorAssetPath` | `string` | Owning sector |
| `RoofCell` | `Location` | Sector-local roof-cell (16×16 grid) |
| `MapTileX / MapTileY` | `int` | Map-local footprint origin |
| `ArtId` | `ArtId` | Roof art identifier |
| `DrawOrder` | `int` | Stable draw order |
| `AnchorX / AnchorY` | `double` | Projected anchor |
| `FootprintTileWidth / Height` | `int` | Always 4 (one roof cell = 4×4 tiles) |

### 5.4 EditorMapObjectAuxiliaryRenderItem

**File:** `src/Editor/ArcNET.Editor/EditorMapObjectAuxiliaryRenderItem.cs`

| Property | Type | Description |
|---|---|---|
| `SectorAssetPath` | `string` | Owning sector |
| `ParentObjectId` | `GameObjectGuid` | Parent object ID |
| `ParentObjectType` | `ObjectType` | Parent object type |
| `CommittedRenderLayer` | `EditorMapCommittedRenderLayer` | Inherited from parent |
| `ArtId` | `ArtId` | Auxiliary art ID |
| `Layer` | `EditorMapObjectAuxiliaryRenderLayer` | Underlay/Shadow/OverlayBack/OverlayFore |
| `MapTileX / MapTileY` | `int` | Parent anchor tile |
| `Tile` | `Location` | Sector-local anchor |
| `DrawOrder` | `int` | Stable draw order |
| `AnchorX / AnchorY` | `double` | Normalized anchor |
| `SuggestedTintColor` | `uint?` | Light tint |
| `RotationIndex` | `int` | CE rotation index |
| `ScalePercent` | `int` | CE scale % |
| `IsShrunk` | `bool` | CE shrunk |
| `BlendMode` | `EditorMapSpriteBlendMode` | SourceOver/Add/Subtract/Multiply |
| `IsRoofCovered` | `bool` | Hidden by CE roof coverage |

The type exposes the right host-facing fields, but the current floor-builder materialization path does not populate `SuggestedTintColor`, `BlendMode`, or `IsRoofCovered`, so committed auxiliary items currently keep their default values.

### 5.5 EditorMapTileOverlayRenderItem

**File:** `src/Editor/ArcNET.Editor/EditorMapTileOverlayRenderItem.cs`

| Property | Type | Description |
|---|---|---|
| `SectorAssetPath` | `string` | Owning sector |
| `MapTileX / MapTileY` | `int` | Map-local tile |
| `Tile` | `Location` | Sector-local tile |
| `Kind` | `EditorMapTileOverlayKind` | BlockedTile / Light / Script |
| `DrawOrder` | `int` | Stable draw order |
| `CenterX / CenterY` | `double` | Projected tile center |
| `SuggestedOpacity` | `double` | 0.40–0.45 |
| `SuggestedTintColor` | `uint` | ARGB overlay color |

### 5.6 EditorMapLightRenderItem

**File:** `src/Editor/ArcNET.Editor/EditorMapLightRenderItem.cs`

| Property | Type | Description |
|---|---|---|
| `SectorAssetPath` | `string` | Owning sector |
| `MapTileX / MapTileY` | `int` | Map-local tile |
| `Tile` | `Location` | Sector-local tile |
| `ArtId` | `ArtId` | Light art ID |
| `DrawOrder` | `int` | Stable draw order |
| `AnchorX / AnchorY` | `double` | Projected anchor |
| `SuggestedTintColor` | `uint` | Light color |
| `SuggestedOpacity` | `double` | Light opacity |
| `Flags` | `SectorLightFlags` | Light flags |

`EditorMapLightRenderItem` is currently a latent type: the paintable-scene layer knows how to consume it, but `EditorMapFloorRenderBuilder.Build()` does not produce any instances yet.

### 5.7 EditorMapPlacementPreviewObject

**File:** `src/Editor/ArcNET.Editor/EditorMapPlacementPreviewObject.cs`

| Property | Type | Description |
|---|---|---|
| `SectorAssetPath` | `string` | Target sector |
| `ProtoId` | `GameObjectGuid` | Prototype ID |
| `ObjectType` | `ObjectType` | Object type |
| `CurrentArtId` | `ArtId` | Art ID |
| `MapTileX / MapTileY` | `int` | Anchor tile |
| `Tile` | `Location` | Sector-local |
| `DrawOrder` | `int` | Draw order |
| `AnchorX / AnchorY` | `double` | Projected anchor |
| `SpriteBounds` | `EditorMapObjectSpriteBounds?` | ART bounds |
| `IsTileGridSnapped` | `bool` | Grid snap |
| `State` | `EditorMapPlacementPreviewState` | Valid/Invalid/Warning |
| `ValidationMessage` | `string?` | Host hint |

---

## 6. Enums

### 6.1 EditorMapCommittedRenderLayer

**File:** `src/Editor/ArcNET.Editor/EditorMapCommittedRenderLayer.cs`

```csharp
public enum EditorMapCommittedRenderLayer
{
    Ground = 0,
    GroundDecal = 1,
    Wall = 2,
    Scenery = 3,
    Mobile = 4,
    Roof = 5,
}
```

**Assignment via `GetCommittedRenderLayer()`:**

```csharp
ObjectType.Wall      → Wall
ObjectType.Portal    → Wall
ObjectType.Scenery   → Scenery
ObjectType.Container → Scenery
ObjectType.Pc        → Mobile
ObjectType.Npc       → Mobile
ObjectType.Projectile→ Mobile
_                    → Ground
```

### 6.2 EditorMapRenderQueueItemKind

**File:** `src/Editor/ArcNET.Editor/EditorMapRenderQueueItemKind.cs`

```csharp
public enum EditorMapRenderQueueItemKind
{
    FloorTile = 0,
    Object = 1,
    Roof = 2,
    PlacementPreviewObject = 3,
    TileOverlay = 4,
    ObjectAuxiliary = 5,
    Light = 6,
}
```

### 6.3 EditorMapObjectAuxiliaryRenderLayer

**File:** `src/Editor/ArcNET.Editor/EditorMapObjectAuxiliaryRenderLayer.cs`

```csharp
public enum EditorMapObjectAuxiliaryRenderLayer
{
    Underlay,      // Sort offset: -3
    Shadow,        // Sort offset: -2
    OverlayBack,   // Sort offset: +1
    OverlayFore,   // Sort offset: +2
}
```

### 6.4 EditorMapTileOverlayKind

**File:** `src/Editor/ArcNET.Editor/EditorMapTileOverlayKind.cs`

```csharp
public enum EditorMapTileOverlayKind
{
    BlockedTile = 0,  // Opacity: 0.45, Tint: 0x88CC6666 (red)
    Light = 1,        // Opacity: 0.40, Tint: 0x88E0C85A (amber)
    Script = 2,       // Opacity: 0.45, Tint: 0x88996CCu (purple)
}
```

### 6.5 EditorMapSpriteBlendMode

**File:** `src/Editor/ArcNET.Editor/EditorMapSpriteBlendMode.cs`

```csharp
public enum EditorMapSpriteBlendMode
{
    SourceOver = 0,
    Add = 1,
    Subtract = 2,
    Multiply = 3,
}
```

### 6.6 EditorMapSceneViewMode

**File:** `src/Editor/ArcNET.Editor/EditorMapSceneViewMode.cs`

```csharp
public enum EditorMapSceneViewMode
{
    TopDown = 0,    // Orthographic tile grid
    Isometric = 1,  // Classic isometric diamond projection
}
```

---

## 7. EditorMapRoofAlphaLerp

**File:** `src/Editor/ArcNET.Editor/EditorMapRoofAlphaLerp.cs`

```csharp
public readonly record struct EditorMapRoofAlphaLerp(
    byte TopLeft, byte TopRight, byte BottomLeft, byte BottomRight);
```

**All 13 piece configurations** (from `GetRoofAlphaLerp()` in `EditorMapPaintableScene.cs` lines ~755–780):

| Piece | TopLeft | TopRight | BottomLeft | BottomRight | Description |
|---|---|---|---|---|---|
| 0 | 255 | 128 | 128 | 255 | Top-outer fade, bottom-outer fade |
| 1 | 128 | 0 | 128 | 255 | Top-left fade, bottom-outer fade |
| 2 | 255 | 128 | 0 | 128 | Top-outer fade, bottom-right fade |
| 3 | 128 | 0 | 0 | 128 | Top-left fade, bottom-right fade |
| 4 | 128 | 128 | 255 | 255 | Top both fade, bottom both transparent |
| 5 | 0 | 0 | 128 | 128 | Top both opaque, bottom both fade |
| 6 | 128 | 128 | 0 | 0 | Top both fade, bottom both opaque |
| 7 | 255 | 255 | 128 | 128 | Top both transparent, bottom both fade |
| 8 | 0 | 0 | 0 | 0 | Fully opaque (no fade) |
| 9 | 128 | 255 | 255 | 128 | Top-right fade, bottom-outer fade |
| 10 | 0 | 128 | 255 | 128 | Top-right fade, bottom-left fade |
| 11 | 128 | 255 | 128 | 0 | Top-left fade, bottom-right fade |
| 12 | 0 | 0 | 128 | 128 | Duplicate of piece 5 |

Constants: `fullOpacity = 0`, `partialOpacity = 128`, `fullTransparency = 255`

**Conditional:** Only applies when `ArtId.IsRoofFaded` is true (bit 12 of roof art ID set).

**RoofPieceIndex formula:**
```csharp
public int RoofPieceIndex =>
    Type is not TypeCode.Roof ? -1 : FrameIndex + (((Value & 0x1u) != 0) ? 9 : 0);
```

---

## 8. EditorMapPaintableSceneBuilder

**File:** `src/Editor/ArcNET.Editor/EditorMapPaintableScene.cs` (lines 196+)

### 8.1 Build() Method

```csharp
public static EditorMapPaintableScene Build(
    EditorMapFloorRenderPreview sceneRender,
    EditorMapPlacementPreview? placementPreview = null,
    IEditorMapRenderSpriteSource? spriteSource = null,
    CancellationToken cancellationToken = default
)
```

Takes the render queue (from scene preview or merged with placement preview) and projects each item into host-ready `EditorMapPaintableSceneItem` objects.

### 8.2 Item Building per Kind

| Kind | Builder Method | Sprite Source |
|---|---|---|
| `FloorTile` | `BuildFloorTile()` | `tile.ArtId` |
| `TileOverlay` | `BuildTileOverlay()` | None (geometry only) |
| `Object` | `BuildObject()` | `obj.CurrentArtId` with rotation/scale |
| `ObjectAuxiliary` | `BuildObjectAuxiliary()` | `auxiliary.ArtId` with rotation/scale |
| `Roof` | `BuildRoof()` | `roof.ArtId` + `GetRoofAlphaLerp()` |
| `Light` | `BuildLight()` | `light.ArtId` (builder exists, but the core floor queue never emits this kind) |
| `PlacementPreviewObject` | `BuildPlacementPreviewObject()` | `preview.CurrentArtId` |

### 8.3 EditorMapPaintableSceneItem — Final Output

| Property | Type | Description |
|---|---|---|
| `Kind` | `EditorMapRenderQueueItemKind` | Item discriminator |
| `DrawOrder` | `int` | Combined draw order |
| `SortKey` | `double` | Internal sort key |
| `CommittedRenderLayer` | `EditorMapCommittedRenderLayer?` | CE layer |
| `Left / Top / Width / Height` | `double` | Bounding rect |
| `AnchorX / AnchorY` | `double` | Anchor point |
| `SuggestedOpacity` | `double` | Default opacity |
| `SuggestedTintColor` | `uint?` | Tint color |
| `TintIgnoresLightVisibility` | `bool` | Light-independent tint |
| `UseGrayscalePaletteOverride` | `bool` | Grayscale mode |
| `UseLightMaskTint` | `bool` | Use light mask color |
| `SuppressFallback` | `bool` | Suppress fallback rendering |
| `TileLightDiagnostics` | `EditorMapTileLightDiagnostics?` | 3×3 light grid |
| `TileOverlayKind` | `EditorMapTileOverlayKind?` | Overlay kind |
| `SpriteSourceRect` | `?` | Source rect for sprite |
| `SpriteDestinationRect` | `?` | Destination rect |
| `IsRoofCovered` | `bool` | Hidden by roof |
| `ObjectColorArray` | `EditorMapObjectColorArray?` | Per-vertex colors |
| `ObjectAlphaLerp` | `EditorMapObjectAlphaLerp?` | Wall transparency |
| `RoofAlphaLerp` | `EditorMapRoofAlphaLerp?` | Roof fade |
| `BlendMode` | `EditorMapSpriteBlendMode` | Blend mode |
| `UseSubtractiveShadowBlend` | `bool` | Shadow blend |
| `Sprite` | `EditorMapRenderSprite?` | Resolved sprite |
| `SpriteReference` | `EditorMapPaintableSceneSpriteReference?` | Lazy sprite key |
| `GeometryPoints` | `IReadOnlyList<EditorMapRenderPoint>?` | Tile diamond |

### 8.4 Viewport Culling

`EditorMapPaintableSceneViewportIndex` provides spatial indexing with 512px cells for efficient visible-item enumeration.

### 8.5 Avalonia/Skia Integration

The `EditorMapPaintableScene` exposes `SpriteSource` (`IEditorMapRenderSpriteSource?`) for lazy sprite resolution. Host renderers call `EnumerateVisibleItems(viewport)` to get only visible items, then resolve sprites through the source.

---

## 9. Editor-Specific Render Passes

### 9.1 Facade Rendering

**File:** `src/Editor/ArcNET.Editor/EditorMapFacadePaintableSceneBuilder.cs`

Builds terrain-facade overlay tiles for the selected tile:

```csharp
public static EditorMapPaintableScene? Build(
    EditorMapFloorRenderPreview sceneRender,
    EditorProjectMapSelectionState? selection,
    EditorTerrainPaletteEntry? terrainEntry,
    FacadeWalk? facadeWalk,
    IEditorMapRenderSpriteSource? spriteSource = null
)
```

- Reads `FacadeWalk` entries from `art/facade/facwalk.{number:X2}`
- Projects each entry relative to the selected tile
- Sorts entries by `MapTileX + MapTileY`, then `MapTileX`, then `MapTileY`
- Creates overlay floor-tile render items with `SortKey = index * 4096d`
- Wraps in a temporary `EditorMapFloorRenderPreview` then delegates to `EditorMapPaintableSceneBuilder.Build()`

Compared with CE `facade_draw()`, this builder does not apply the isometric `tile_rect.x++` shim.

### 9.2 Tile Script / Blocked / Light Overlays

Generated inline during `ProcessTile()` in `EditorMapFloorRenderBuilder`:

```csharp
// Blocked tile overlay
if (sector.IsTileBlocked(tileX, tileY) && request.IncludeBlockedTileOverlays)
    → EditorMapTileOverlayKind.BlockedTile

// Light overlay
if (lightTileIndices.Contains(tileIndex) && request.IncludeLightOverlays)
    → EditorMapTileOverlayKind.Light

// Script overlay
if (scriptedTileIndices.Contains(tileIndex) && request.IncludeScriptOverlays)
    → EditorMapTileOverlayKind.Script
```

### 9.3 Sector / Wall / Waypoint / JumpPoint / TileBlock Rendering

These editor-specific overlays are **not** built by the core `EditorMapFloorRenderBuilder`. They are:

- **Sector boundaries** — likely rendered as geometry overlays in the host renderer
- **Wall rendering** — walls are committed as `ObjectType.Wall` objects in the normal object pipeline with `CommittedRenderLayer = Wall`. *Parity Gap*: In Top-Down view, ArcNET draws standard sprites rather than CE's 2px red lines and magenta corner dots.
- **Waypoint / JumpPoint / TileBlock** — no dedicated builder types were found in `src/Editor/ArcNET.Editor/`. These may be handled at the host renderer level or in the workspace session layer.

---

## 10. Object Flag/Property Mapping

### 10.1 ObjectFlags Enum

**File:** `src/GameObjects/ArcNET.GameObjects/Flags/ObjectFlags.cs`

```csharp
[Flags]
public enum ObjectFlags : uint
{
    None = 0,
    Destroyed     = 0x1,
    Off           = 0x2,
    Flat          = 0x4,        // OF_FLAT equivalent
    Text          = 0x8,
    SeeThrough    = 0x10,
    ShootThrough  = 0x20,
    Translucent   = 0x40,
    Shrunk        = 0x80,       // Used by IsShrunk
    DontDraw      = 0x100,
    Invisible     = 0x200,
    NoBlock       = 0x400,
    ClickThrough  = 0x800,
    Inventory     = 0x1000,
    Dynamic       = 0x2000,
    ProvidesCover = 0x4000,
    HasOverlays   = 0x8000,
    HasUnderlays  = 0x10000,
    Wading        = 0x20000,    // OF_WADING equivalent
    WaterWalking  = 0x40000,
    Stoned        = 0x80000,
    DontLight     = 0x100000,
    TextFloater   = 0x200000,
    Invulnerable  = 0x400000,
    Extinct       = 0x800000,
    TrapPc        = 0x1000000,
    TrapSpotted   = 0x2000000,
    DisallowWading= 0x4000000,
    MultiplayerLock=0x8000000,
    Frozen        = 0x10000000,
    AnimatedDead  = 0x20000000,
    // ... more flags
}
```

### 10.2 How ArcNET Reads CE Object Flags

From `EditorMapScenePreviewBuilder.BuildObject()`:

```csharp
var flags = GetObjectFlagsOrDefault(mob);
// → reads ObjectField.ObjectFlags as uint, casts to ObjectFlags enum
```

The flags are read from `MobData` via:
```csharp
private static ObjectFlags GetObjectFlagsOrDefault(MobData mob) =>
    mob.GetProperty(ObjectField.ObjectFlags) is { ParseNote: null } property
        ? (ObjectFlags)unchecked((uint)property.GetInt32())
        : default;
```

### 10.3 Underlay/Overlay Array Reading

```csharp
ShadowArtId      = GetArtIdOrDefault(mob, ObjectField.Shadow);       // single ArtId
UnderlayArtIds   = GetIntArrayOrDefault(mob, ObjectField.Underlay);   // int[] array
OverlayBackArtIds= GetIntArrayOrDefault(mob, ObjectField.OverlayBack);// int[] array
OverlayForeArtIds= GetIntArrayOrDefault(mob, ObjectField.OverlayFore);// int[] array
```

Where:
```csharp
private static int[] GetIntArrayOrDefault(MobData mob, ObjectField field) =>
    mob.GetProperty(field) is { ParseNote: null } property ? property.GetInt32Array() : [];
```

### 10.4 Flag Usage in Rendering Pipeline

| Flag | Usage |
|---|---|
| `Flat` (0x4) | Drives `CommittedRenderLayer = GroundDecal` and global flat band (200M) in `BuildRenderQueue` |
| `Shrunk` (0x80) | Exposed as `IsShrunk` and passed through to object and auxiliary sprite requests |
| `Wading` (0x20000) | Used for shadow tint (`0xFF5C5C5C`). Main sprite wading effect (15px shift + alpha=92) not implemented |
| `HasOverlays` (0x8000) | Preserved on the preview object; auxiliary generation actually keys off the overlay arrays rather than this flag |
| `HasUnderlays` (0x10000) | Preserved on the preview object; auxiliary generation actually keys off the underlay array rather than this flag |
| `Translucent` (0x40) | Mapped to `SuggestedOpacity = 0.5d` in `BuildObject()` |
| `DontDraw` (0x100) | Preserved on the preview object; CE only uses this in gameplay mode (`dword_5E2EC8`), not in editor |
| `Invisible` (0x200) | Preserved on the preview object; correctly NOT filtered in editor mode (CE editor shows all objects) |
| `Stoned` (0x80000) | Mapped to `UseGrayscalePaletteOverride = true` in `CreateItem()` |
| `DontLight` (0x100000) | Mapped to `TintIgnoresLightVisibility = true` in `CreateItem()` |
| `Frozen` (0x10000000) | Preserved on the preview object, but lacks additive blend + blue tint multiplier assignment (M5) |
| `AnimatedDead` (0x20000000) | Mapped to `SuggestedTintColor = 0xFF00FF00` (green) in `CreateItem()` |

### 10.5 Additional Discrepancies Not Bound to Flags

- **Roof Fades:** ✅ Resolved. `GetRoofAlphaLerp()` in `EditorMapPaintableScene.cs` implements all 13 roof piece types with correct 4-corner alpha values.
- **Eye Candy Scaling:** ✅ Resolved. `AdjustEyeCandyRequest()` in `EditorMapPaintableScene.cs` applies the CE `dword_5A548C` multiplier table `[50,63,75,87,100,130,160,200]` for EyeCandy art types.

---

## 11. Projection Methods

### 11.1 ProjectTileCenter()

```csharp
TopDown:   (-mapTileX * tileW + tileW/2, mapTileY * tileH + tileH/2)
Isometric: ((mapTileY - mapTileX) * tileW/2, (mapTileX + mapTileY) * tileH/2 + tileH/2)
```

### 11.2 ProjectObjectAnchor()

```csharp
anchor = tileCenter + ScaleObjectOffsets(viewMode, tileW, tileH, object)
// Isometric scaling: offsetX = (offsetX + 40) * (tileW / 80), offsetY = (offsetY + 20) * (tileH / 40)
```

### 11.3 ProjectRoofAnchor()

```csharp
TopDown:   (-mapTileX * tileW, topMapTileY * tileH)
Isometric: ProjectTileCenter(normalizedX+2, normalizedY+2) then offset by (-tileW*2, -tileH*5.5)
```

---

## 12. Supporting Types

### 12.1 EditorMapObjectSpriteBounds

**File:** `src/Editor/ArcNET.Editor/EditorMapObjectSpriteBounds.cs`

```csharp
public sealed class EditorMapObjectSpriteBounds
{
    public required int MaxFrameWidth { get; init; }
    public required int MaxFrameHeight { get; init; }
    public required int MaxFrameCenterX { get; init; }
    public required int MaxFrameCenterY { get; init; }
}
```

Derived from scanning all ART frames across all rotations.

### 12.2 EditorMapObjectAlphaLerp

**File:** `src/Editor/ArcNET.Editor/EditorMapObjectAlphaLerp.cs`

```csharp
public readonly record struct EditorMapObjectAlphaLerp(byte Left, byte Right);
// CE wall transparency: 0 = fully transparent, 255 = fully opaque
```

### 12.3 EditorMapObjectColorArray

**File:** `src/Editor/ArcNET.Editor/EditorMapObjectColorArray.cs`

Wraps a `uint[]` of per-vertex colors with value equality.

### 12.4 EditorMapTileLightDiagnostics

**File:** `src/Editor/ArcNET.Editor/EditorMapTileLightDiagnostics.cs`

```csharp
public readonly record struct EditorMapTileLightDiagnostics(
    uint? TopLeft,    uint? TopCenter,    uint? TopRight,
    uint? MiddleLeft, uint? MiddleCenter, uint? MiddleRight,
    uint? BottomLeft, uint? BottomCenter, uint? BottomRight
)
```

CE-style 3×3 light sample grid from `light.c::sub_4DA360`.

---

## 13. Diagnostic Tooling

**File:** `tools/DiagnosticDump/RenderBufferDumpCommand.cs`

Full TSV dump of both queue items and paintable items with columns:

**Queue items:**
`Index, Kind, Layer, DrawOrder, SortKey, ObjectType, AuxLayer, TileOrderSecondary, TypeSortPriority, BlendMode, Tint, Opacity, ArtId, AssetPath, SectorAssetPath, MapTile, Anchor, SpriteBounds, RoofCovered, QueueDetails`

**Paintable items:**
`Index, Kind, Layer, DrawOrder, SortKey, Tint, Opacity, BlendMode, UseLightMaskTint, UseGrayscalePaletteOverride, TintIgnoresLightVisibility, IsRoofCovered, TileOverlayKind, ArtId, AssetPath, LeftTopSize, Anchor, SourceRect, DestinationRect, GeometryPointCount`

---

## 14. ArtId Encoding

**File:** `src/Core/ArcNET.Core/Primitives/ArtId.cs`

The 32-bit `ArtId` encodes art type in bits 28-31:

| TypeCode | Value | Description |
|---|---|---|
| Tile (None) | 0 | Floor tiles |
| Wall | 1 | Wall art |
| Critter | 2 | Player/NPC art |
| Portal | 3 | Door/portal art |
| Scenery | 4 | Scenery objects |
| Interface | 5 | UI elements |
| Item | 6 | Item art |
| Container | 7 | Container art |
| Misc | 8 | Miscellaneous |
| Light | 9 | Light masks |
| Roof | 10 | Roof tiles |
| Facade | 11 | Facade art |
| Monster | 12 | Monster art |
| UniqueNpc | 13 | Unique NPC art |
| EyeCandy | 14 | Eye candy effects |

Key bit fields for roofs:
- Bit 12: `IsRoofFaded` (fade piece flag)
- Bit 13: `IsRoofFill` (fill piece flag)
- Bit 0: Mirror flag (adds 9 to piece index)

---

## 15. Editor-Specific Rendering Details

### 15.1 Editor Workspace Session Pipeline

The full editor rendering pipeline flows through:

```
EditorWorkspaceSession.CreateMapWorldEditSceneCoreAsync()
    |
    +-- 1. EditorMapScenePreviewBuilder.Build()
    |       -> EditorMapScenePreview (sector data in host-neutral coords)
    |
    +-- 2. PreloadArtsAsync() [FIX #12]
    |       -> Parallel batch preload of ART files via _openArchives cache
    |
    +-- 3. EditorMapFloorRenderBuilder.Build()
    |       -> EditorMapFloorRenderPreview (projected items + unified RenderQueue)
    |
    +-- 4. EditorMapPaintableSceneBuilder.Build()
    |       -> EditorMapPaintableScene (host-ready items with sprite references)
    |
    +-- 5. EditorMapPlacementPreview injection (optional)
            -> Live placement ghosts merged into RenderQueue
```

### 15.2 Delta Rebuild

`EditorMapFloorRenderBuilder.BuildDelta()` re-processes only one changed sector, preserving all other sectors' render items. This enables efficient incremental updates when the user paints tiles or moves objects.

### 15.3 Viewport Culling

`EditorMapPaintableSceneViewportIndex` provides spatial indexing with 512px cells. `EnumerateVisibleItems(viewport)` returns only items intersecting the current viewport.

### 15.4 Paintable Scene Item Properties

`EditorMapPaintableSceneItem` is the final output consumed by the host renderer:

| Property | Type | Description |
|----------|------|-------------|
| `Kind` | `EditorMapRenderQueueItemKind` | FloorTile/Object/Roof/Light/etc. |
| `DrawOrder` | `int` | Combined draw order |
| `SortKey` | `double` | Internal sort key |
| `CommittedRenderLayer` | `EditorMapCommittedRenderLayer?` | CE layer classification |
| `Left/Top/Width/Height` | `double` | Bounding rect |
| `AnchorX/AnchorY` | `double` | Anchor point |
| `SuggestedOpacity` | `double` | Default opacity |
| `SuggestedTintColor` | `uint?` | Tint color |
| `Sprite` | `EditorMapRenderSprite?` | Resolved sprite |
| `SpriteReference` | `EditorMapPaintableSceneSpriteReference?` | Lazy sprite key |
| `GeometryPoints` | `IReadOnlyList<EditorMapRenderPoint>?` | Tile diamond |
| `RoofAlphaLerp` | `EditorMapRoofAlphaLerp?` | Roof fade corners |
| `ObjectAlphaLerp` | `EditorMapObjectAlphaLerp?` | Wall transparency |
| `ObjectColorArray` | `EditorMapObjectColorArray?` | Per-vertex colors |
| `BlendMode` | `EditorMapSpriteBlendMode` | SourceOver/Add/Sub/Mul |
| `IsRoofCovered` | `bool` | Hidden by roof |
| `TileOverlayKind` | `EditorMapTileOverlayKind?` | Blocked/Light/Script |
| `UseLightMaskTint` | `bool` | Use light mask color |
| `UseGrayscalePaletteOverride` | `bool` | Grayscale mode |

### 15.5 Facade Paintable Scene Builder

`EditorMapFacadePaintableSceneBuilder` builds terrain-facade overlay tiles for the selected tile:

```csharp
public static EditorMapPaintableScene? Build(
    EditorMapFloorRenderPreview sceneRender,
    EditorProjectMapSelectionState? selection,
    EditorTerrainPaletteEntry? terrainEntry,
    FacadeWalk? facadeWalk,
    IEditorMapRenderSpriteSource? spriteSource = null
)
```

- Reads `FacadeWalk` entries from `art/facade/facwalk.{number:X2}`
- Projects each entry relative to the selected tile
- Creates overlay floor-tile render items with `SortKey = index * 4096d`
- Wraps in a temporary `EditorMapFloorRenderPreview` then delegates to `EditorMapPaintableSceneBuilder.Build()`

### 15.6 Placement Preview

`EditorMapPlacementPreview` injects live placement ghosts into the render queue:

```csharp
public sealed class EditorMapPlacementPreview
{
    public required IReadOnlyList<EditorMapPlacementPreviewObject> Objects { get; init; }
}
```

Each `EditorMapPlacementPreviewObject` has:
- `State` -- Valid/Invalid/Warning
- `ValidationMessage` -- Host hint
- Full object render properties (art, bounds, position)

### 15.7 Sprite Source Pipeline

```
IEditorMapRenderSpriteSource
    |
    +-- PreloadAsync(artIds)     -> Batch preload ART files
    +-- TryGetArtId(mobData)     -> Resolve current art ID
    +-- TryResolveAssetPath()    -> Find .dat file path for art

EditorWorkspaceMapRenderSpriteSource
    |
    +-- _openArchives cache      -> ConcurrentDictionary<string, DatArchive>
    +-- IsCompatibleFamily()     -> Filter by render item kind
    +-- Facade MES fallback      -> Resolve art/facade/facadename.mes
```

### 15.8 Render Queue Item Kinds

```csharp
public enum EditorMapRenderQueueItemKind
{
    FloorTile = 0,
    Object = 1,
    Roof = 2,
    PlacementPreviewObject = 3,
    TileOverlay = 4,
    ObjectAuxiliary = 5,
    Light = 6,
}
```

### 15.9 Unified Render Queue Sort

The `RenderQueue` merges all item types into a single sorted list:

```csharp
// Floor tiles:    SortKey = DrawOrder * 4096
// Tile overlays:  SortKey = DrawOrder * 4096 + 1024 + kind
// Objects:        SortKey = DrawOrder * 4096 + 2048 + tileOrderPrimary
// Roofs:          SortKey = DrawOrder * 4096 + 3072
// Auxiliaries:    SortKey = (DrawOrder * 4096) + 2048 + auxiliaryLayerOffset

// Final sort: by SortKey, then Kind, then Index
queue.OrderBy(item => item.SortKey)
     .ThenBy(item => item.Kind)
     .ThenBy(item => item.Index)
```

### 15.10 Sort Key Slot Map

```
Slot 0     ... 1023   -> Floor tiles   (drawOrder x 4096 + 0..1023)
Slot 1024  ... 2047   -> Tile overlays (drawOrder x 4096 + 1024 + kind)
Slot 2048  ... 3071   -> Objects       (drawOrder x 4096 + 2048 + tileOrderPrimary)
Slot 3072  ... 4095   -> Roofs         (drawOrder x 4096 + 3072)
Aux underlay          -> -3 from object sort key
Aux shadow            -> -2 from object sort key
Aux overlay-back      -> +1 from object sort key
Aux overlay-fore      -> +2 from object sort key
```

### 15.11 Editor State Tints

| State | Tint | CE Equivalent |
|-------|------|---------------|
| Destroyed object | Red tint | `OF_DESTROYED` + red color |
| Off object | Green tint | `OF_OFF` + green color |
| Selected object | Host-defined | N/A (CE uses hover) |

---

## 16. Cross-Reference Documents

| Document | Path | Description |
|----------|------|-------------|
| CE Rendering Analysis | `docs/arcanum-ce-rendering-analysis.md` | Detailed CE engine rendering pipeline (1166 lines) |
| Parity Comparison | `docs/rendering-parity-comparison.md` | Side-by-side parity analysis with gap tracking (577 lines) |
| Rendering Fix Checklist | `docs/rendering_analysis.md` | 24 verified rendering fixes |
| This Document | `docs/rendering-pipeline-parity-analysis.md` | ArcNET rendering pipeline analysis |

---

## 17. Deep Parity Analysis — Wall/Scenery/Tile Composition

> **Generated:** 2026-05-20 from source code at `src/Editor/ArcNET.Editor/EditorMapFloorRenderBuilder.cs`, `EditorMapPaintableScene.cs`, and `EditorMapObjectPreview.cs` cross-referenced against CE `object.c`, `sector_object_list.c`, and `roof.c`.

### 17.1 Verified Correct Behaviors

The following rendering behaviors have been verified correct through deep source-level comparison with CE:

| Behavior | Verification |
|----------|-------------|
| **Same-tile ordering: flat before non-flat** | `InsertCeSameTileObject` correctly inserts flat objects before non-flat objects. Flat objects skip past all existing flat objects via `if (existingObject.IsFlat) continue;` and land after the last flat but before the first non-flat. |
| **Same-tile ordering: UnderAll at head of flat** | `IsUnderAllScenery` check inserts before all existing flat objects, matching CE's `objlist_insert_internal()` head-of-flat-segment insertion. |
| **Same-tile ordering: wall before portal** | Explicit wall/portal type checks insert wall before portal regardless of tile order components. Portal insertion after wall uses `index + 1`. |
| **Same-tile ordering: tile order components** | `GetObjectTileOrderComponents` computes `Primary = horizontal + vertical` (CE's `v1 + v2`) and `Secondary = vertical - horizontal` (CE's `v2 - v1`). Comparison is Primary-first then Secondary, which matches CE's "compare second component first, then first" given the name swap. |
| **Diagonal draw order formula** | `GetDrawOrder` = `((mapTileY + mapTileX) * mapTileWidth) + mapTileY` correctly produces ascending `y+x` primary ordering with ascending `y` (left-to-right) secondary ordering. |
| **Global band structure** | `BuildRenderQueue` correctly implements CE's global layer bands: Underlays (0..N), UnderAll (100M), Flat (200M), Shadows (400M), Non-flat (600M), Overlays (700M), Roofs (800M). |
| **Overlay iteration order** | `GenerateAuxiliaryItems` iterates slots from `overlaySlotCount - 1` down to `0`, checking fore then back per slot, matching CE's `object_draw()` overlay loop. |
| **Wall/portal ordering offsets** | `GetCeWallPortalOrderingOffsetY` returns 19 for rotations 2-5 and -20 for rotations 0-1, 6-7, matching CE's `objlist_insert_internal()` wall/portal offset constants. |
| **Wall center adjustment** | `GetLayoutSpriteCenter` applies `centerX -= 40, centerY += 20` for cardinal rotations (0-1, 6-7) and mirror flag handling, matching CE's wall hotspot adjustment. |
| **Roof coverage matrix** | `IsRoofCovered` uses the 13-piece `RoofCoverageMatrix[13,4,4]` with mirror flag support, matching CE's `roof_is_covered_loc()`. |
| **Transparent wall hiding under faded roofs** | `ShouldHideTransparentWallUnderFadedRoof` correctly checks `OWAF_TRANS_LEFT | RIGHT` and non-cardinal rotation exclusion (rotations 2-5 are excluded). |
| **Object anchor projection** | `ProjectObjectAnchor` + `ScaleObjectOffsets` produces `tileCenter + offset * scale`, matching CE's `location_xy() + offset + (40, 20)`. |
| **Eye candy scale type** | `AdjustEyeCandyRequest` applies the `dword_5A548C` multiplier table `[50,63,75,87,100,130,160,200]` for EyeCandy art types, matching CE's eye candy scale lookup. |
| **Object flag mappings in paintable scene** | `CreateItem` correctly maps: `Translucent → 0.5 opacity`, `DontLight → TintIgnoresLightVisibility`, `Stoned → UseGrayscalePaletteOverride`, `AnimatedDead → 0xFF00FF00 green tint`. |
| **Shadow blend mode** | Shadow auxiliaries carry `BlendMode = Subtract` and `CreateItem` maps to `UseSubtractiveShadowBlend = true`. |
| **Wading shadow color** | Shadow auxiliaries for wading objects get `SuggestedTintColor = 0xFF5C5C5C` (92, 92, 92). |
| **Reaction underlay tint** | Underlays with art 433 get `SuggestedTintColor = obj.ReactionColor` and `ScalePercent = 100`. |
| **Roof fade alpha gradients** | `GetRoofAlphaLerp` implements all 13 roof piece types with correct 4-corner alpha values. |

### 17.2 Remaining Open Bugs

#### 17.2.1 Frozen Object Effect (M5)

**Location:** `EditorMapPaintableScene.CreateItem()` lines ~800

**Status:** ✅ Resolved

**CE behavior:** `object_setup_blit()` (CE `object.c:4665`) sets `TIG_ART_BLT_BLEND_ADD | TIG_ART_BLT_BLEND_COLOR_CONST` for frozen objects. The blue tint color `(0, 128, 255)` is set by `sub_442520()` into `OBJ_F_COLOR` during palette recomputation, then read by `object_setup_blit()`.

**ArcNET status:** `ObjectFlags.Frozen` is mapped in `CreateItem()`. It correctly sets `blendMode = EditorMapSpriteBlendMode.Add` and `finalTintColor = 0xFF0080FF` (blue multiplier matching CE).

#### 17.2.2 Editor Destroyed/Off Tint (M6)

**Location:** `EditorMapPaintableScene.CreateItem()` lines ~800

**Status:** ✅ Resolved

**CE behavior:** `object_setup_blit()` (CE `object.c:4770`) in editor mode checks `OF_DESTROYED | OF_OFF`:
```c
if (object_editor) {
    if ((obj_flags & OF_DESTROYED) != 0) {
        blit_info->color = tig_color_make(255, 0, 0);  // Red
    } else {
        blit_info->color = tig_color_make(0, 255, 0);  // Green
    }
}
```
Both use `TIG_ART_BLT_BLEND_ADD | TIG_ART_BLT_BLEND_COLOR_CONST`.

**ArcNET status:** `CreateItem()` checks `ObjectFlags.Destroyed` and `ObjectFlags.Off`. Destroyed sets `blendMode = EditorMapSpriteBlendMode.Add` and `finalTintColor = 0xFFFF0000` (red); Off sets `blendMode = EditorMapSpriteBlendMode.Add` and `finalTintColor = 0xFF00FF00` (green), achieving perfect visual editor highlighting.

### 17.3 Corrected Status Summary

| Item | Previous Status | Corrected Status | Evidence |
|------|----------------|-----------------|----------|
| M5 (Frozen Tint) | ❌ Open | ✅ Resolved | `CreateItem` sets `blendMode = Add` and `finalTintColor = 0xFF0080FF` when `Frozen` flag is set. |
| M6 (Editor Highlight) | ❌ Open | ✅ Resolved | `CreateItem` sets `blendMode = Add` and `finalTintColor` to red or green for `Destroyed`/`Off`. |
| M10 (Wading Shadow) | ❌ Open | ✅ Resolved | `GenerateAuxiliaryItems` line ~530: `SuggestedTintColor: obj.IsWading ? 0xFF5C5C5C : null` |
| M12 (Reaction Tints) | ❌ Open | ✅ Resolved | `GenerateAuxiliaryItems` line ~490: `isReactionUnderlay ? obj.ReactionColor : null` |
| M13 (Ghost Stacking) | ❌ Open | ✅ Resolved | `BuildRenderQueue` L1833: main `SubOrder: 0`, ghost `SubOrder: 1`. `IsGhostOrArmorOverlay` L1165: Armor unconditional, NPC dead + art 243, PC excluded. All match CE `object_draw()` at line ~695. |
| M14 (Subtract Shadow) | ❌ Open | ✅ Resolved | `GenerateAuxiliaryItems` line ~535: `BlendMode: EditorMapSpriteBlendMode.Subtract` |
| M15 (AnimatedDead) | ❌ Open | ✅ Resolved | `CreateItem` line ~802: `isAnimatedDead ? 0xFF00FF00 : suggestedTintColor` |
| M16 (Stoned) | ❌ Open | ✅ Resolved | `CreateItem` line ~800: `UseGrayscalePaletteOverride = isStoned` |
| M17 (DontLight) | ❌ Open | ✅ Resolved | `CreateItem` line ~798: `TintIgnoresLightVisibility = dontLight` |
| M18 (Invisible) | ❌ Open | ✅ Resolved | `ProcessSector` L678: "Do not filter OF_INVISIBLE here — the flag is only meaningful in gameplay mode." |
| M21 (Roof Alpha) | ❌ Open | ✅ Resolved | `GetRoofAlphaLerp` lines ~900-930: 13-piece switch expression |
| M22 (EyeCandy Scale) | ❌ Open | ✅ Resolved | `AdjustEyeCandyRequest` lines ~870-890: `dword_5A548C` multiplier table |
| M23 (Translucent) | ❌ Open | ✅ Resolved | `BuildObject` line ~606: `SuggestedOpacity = 0.5d` for Translucent flag |
| M24 (TRANS_DISALLOW) | ❌ Open | ✅ Resolved | `ShouldHideTransparentWallUnderFadedRoof` L1119: `(WallFlags & WallTransDisallow) == 0` |

### 17.4 Remaining Open Gaps

| # | Gap | Priority | Details |
|---|-----|----------|---------|
| M4 | Wading main sprite effect | Low | CE: 15px Y-shift + bottom strip alpha=92 for non-flat; entire sprite alpha for flat. Also shifts all eye candy rects +15px Y via `sub_443620`. |
| M5 | Frozen object blue tint | ✅ Resolved | Mapped `ObjectFlags.Frozen` to additive blue tint `0xFF0080FF` in `CreateItem()`. |
| M6 | Editor destroyed/off tint | ✅ Resolved | Mapped `ObjectFlags.Destroyed` to additive red `0xFFFF0000` and `ObjectFlags.Off` to additive green `0xFF00FF00` in `CreateItem()`. |
| M7 | Hover highlight system | Low | CE has `object_highlight_mode` (additive white at order+1 for non-wall/non-click-through) and hover underlay/overlay (animated art 467/468 at sort keys 99999999/INT_MAX with reaction coloring). |
| M11 | Scaled sprite dirty rect bypass | Low | CE bypasses dirty culling for `scale != 100`. Less relevant in ArcNET's retained-mode pipeline. |
| M19 | Top-down wall overlays | Low | CE `wall_draw()` renders 2px red lines + magenta dots; ArcNET draws standard sprites. |
| M20 | Floating text rendering | Low | CE `tb_draw()`/`tf_draw()` for `OF_TEXT`/`OF_TEXT_FLOATER`; no ArcNET text layer. |
| N1 | CE overlay scale check bug | Note | CE `object.c` ~L710 checks `scale_type != 100` instead of `overlay_scale != 100`. `scale_type` is an index (0-7), never 100, so overlay source rects are always scaled. ArcNET handles scale at the sprite request level via `AdjustEyeCandyRequest()`, producing slightly different results. |
| N2 | `qsort` instability | Note | CE uses `qsort` (not guaranteed stable) for the blit queue. ArcNET uses stable `OrderBy().ThenBy()` with additional tie-breakers. |
| N3 | Wading eye candy Y-offset | Low | CE `sub_443620()` adds `rect.y += 15` for wading objects when computing all eye candy rects (underlays, overlays, shadows). ArcNET only applies wading tint to shadows. |
| N4 | Per-type object visibility toggles | Low | CE `object_type_visibility[18]` allows toggling specific object types. ArcNET has `request.IncludeObjects` (all-or-nothing). |
| N5 | `BlitFlags`/`BlitAlpha`/`BlitColor` not mapped | Medium | CE objects can have per-object custom blend modes (`OBJ_F_BLIT_FLAGS`), alpha (`OBJ_F_BLIT_ALPHA`), and color (`OBJ_F_COLOR`). `EditorMapObjectPreview` does not carry these fields. |
| N6 | `OBJ_F_RENDER_FLAGS` cached lighting state | Low | CE caches lighting/palette state per object. ArcNET delegates lighting to the host renderer. |
