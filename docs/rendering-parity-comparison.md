# ArcNET vs Arcanum-CE Rendering Pipeline — Parity Comparison

> **Generated:** 2026-05-19  
> **ArcNET source:** `src/Editor/ArcNET.Editor/`  
> **CE source:** `C:\Users\Bia\source\repos\arcanum-ce\`  
> **Goal:** 100% visual parity between ArcNET editor rendering and Arcanum CE engine rendering

---

## 1. Architecture Comparison

### 1.1 Pipeline Stages

| Stage | Arcanum CE | ArcNET | Parity |
|-------|-----------|--------|--------|
| Sector data loading | `sector_lock()` → `SectorCacheEntry` | `EditorWorkspace._openArchives` + sector parsing | ✅ Equivalent |
| Tile projection | `location_xy()` inline per-tile | `EditorMapFloorRenderBuilder.ProjectTileCenter()` | ✅ Matches |
| Object projection | `location_xy()` + offset + (40,20) inline | `EditorMapFloorRenderBuilder.ProjectObjectAnchor()` | ✅ Matches |
| Roof projection | `roof_xy()` → `location_xy()` + (-120,-200) | `EditorMapFloorRenderBuilder.ProjectRoofAnchor()` | ✅ Matches |
| Sort/composition | Integer sort keys in deferred blit queue | `SortKey = drawOrder * 4096 + layerOffset` | ⚠️ Structurally different |
| Sprite resolution | `tig_art_blit()` via TIG engine | `IEditorMapRenderSpriteSource.PreloadAsync()` | ✅ Equivalent |
| Final rendering | Immediate-mode blit to window | `EditorMapPaintableScene` → host Avalonia/Skia | ✅ Equivalent |

### 1.2 Key Architectural Difference

**CE** uses an **immediate-mode** rendering pipeline. In the editor path the relevant world-content stages are `light_draw()`, `tile_draw()`, `facade_draw()`, `object_draw()`, and `roof_draw()`, with `object_draw()` internally using a deferred blit queue sorted by integer keys.

**ArcNET** uses a **retained-mode** pipeline: data flows through three builder stages to produce an immutable `EditorMapPaintableScene` that the host renderer (Avalonia/Skia) consumes. The unified `RenderQueue` merges all item types into a single sorted list.

**Impact:** The sort/composition systems are structurally different, and today they do **not** yet encode several CE rules that matter for floor decals, walls, scenery, roofs, and same-tile object ordering.

---

## 2. Coordinate System

### 2.1 Isometric Projection

| Aspect | Arcanum CE | ArcNET | Parity |
|--------|-----------|--------|--------|
| Tile width | 80 px | `TileWidthPixels = 64d` (configurable) | ⚠️ Configurable |
| Tile height | 40 px | `TileHeightPixels = 32d` (configurable) | ⚠️ Configurable |
| Projection formula | `sx = origin + 40*(y-x-1)`, `sy = origin + 20*(y+x)` | `centerX = (y-x)*tileW/2`, `centerY = (x+y)*tileH/2 + tileH/2` | ✅ Equivalent |
| Inverse projection | `(dy-dx)/40`, `(dy+dx)/40` | Not directly used (screen→world via selection) | N/A |
| Diagonal drawing order secondary term | Ascending `y` (i.e. left-to-right) | Ascending `x` (i.e. right-to-left) | ❌ **Inverted (fixed with `+ mapTileY`)** |

**Note:** In isometric view, drawing order is calculated diagonally (`y + x` ascending) to render tiles back-to-front. Within the same diagonal row, tiles must be rendered left-to-right (ascending `y`, which corresponds to descending `x`). ArcNET previously used `+ mapTileX` as the secondary term, which sorted ascending `x` (i.e. descending `y`), resulting in a completely inverted order of same-diagonal elements. Changing this to `+ mapTileY` restores perfect visual rendering parity for walls, doors, portals, and scenery on the same diagonal.

**Note:** ArcNET defaults to 64×32 but the analysis doc shows 80×40 as the CE default. The request uses configurable values; when set to 80×40, the formulas are mathematically identical.

### 2.2 Object Anchor Offset

| Aspect | Arcanum CE | ArcNET | Parity |
|--------|-----------|--------|--------|
| X center offset | `+40` | `(offsetX + 40) * scaleX` | ✅ Matches (fix #1) |
| Y center offset | `+20` | `(offsetY + 20) * scaleY` | ✅ Matches (fix #1) |
| Scale factor | `tileW / 80` for X, `tileH / 40` for Y | `scaleX = tileW / 80d`, `scaleY = tileH / 40d` | ✅ Matches |

### 2.3 Roof Anchor

| Aspect | Arcanum CE | ArcNET | Parity |
|--------|-----------|--------|--------|
| Normalization | `(x - x%4 + 2, y - y%4 + 2)` | `normalizedX = tileX - tileX%4 + 2` etc. | ✅ Matches |
| Sprite offset | `(-120, -200)` from center | `(-tileW*2, -tileH*5.5)` = `(-160, -220)` at 80×40 | ✅ Matches |

**Verification:** CE: `40*(y-x-1) - 120 = 40*(y-x) - 160`. ArcNET: `(y-x)*40 - 160`. ✓

### 2.4 Sector Coordinate Normalization

| Aspect | Arcanum CE | ArcNET | Parity |
|--------|-----------|--------|--------|
| Tile X in sector | `LOCATION_GET_X(loc) & 0x3F` | `tileX & 0x3F` | ✅ Matches (fix #20) |
| Tile Y in sector | `LOCATION_GET_Y(loc) & 0x3F` | `tileY & 0x3F` | ✅ Matches (fix #20) |
| Sector ID | `SECTOR_MAKE(x>>6, y>>6)` | Sector asset path lookup | ✅ Equivalent |

---

## 3. Draw Order

### 3.1 Module Draw Order

| Order | Arcanum CE editor stage | ArcNET equivalent | Parity |
|-------|-------------------------|-------------------|--------|
| 1 | `light_draw()` | `EditorMapLightRenderItem` type exists, but `BuildRenderQueue()` does not enqueue light items | ❌ Unwired |
| 2 | `tile_draw()` | `EditorMapFloorTileRenderItem` in `RenderQueue` | ✅ |
| 3 | `facade_draw()` | `EditorMapFacadePaintableSceneBuilder` side-scene | ⚠️ Separate composition path |
| 4 | `object_draw()` | `EditorMapObjectRenderItem` + `EditorMapObjectAuxiliaryRenderItem` | ⚠️ Partial parity |
| 5 | `roof_draw()` | `EditorMapRoofRenderItem` in `RenderQueue` | ⚠️ Missing roof coverage hide logic |

### 3.2 Object Sort Key System

| Aspect | Arcanum CE | ArcNET | Parity |
|--------|-----------|--------|--------|
| Underlays | Global group `0..N` before every main object | `objectSortKey - 3` local to the parent object's bucket | ❌ Different model |
| `OSCF_UNDER_ALL` scenery | Dedicated global group `100,000,000` | Not implemented; `SceneryFlags` never reach the builder | ❌ Missing |
| Flat main sprite | Dedicated global group `200,000,000..` | Dedicated global band `200,000,000 + counter` in `BuildRenderQueue` | ✅ Matches |
| Shadows | Dedicated global group `400,000,000..` | Dedicated global band `400,000,000 + counter` in `BuildRenderQueue` | ✅ Matches |
| Non-flat main sprite | Dedicated global group `600,000,000..` | Dedicated global band `600,000,000 + counter` in `BuildRenderQueue` (includes ghost overlays) | ✅ Matches |
| Overlays | Global group `700,000,000..` | Dedicated global band `700,000,000 + counter` in `BuildRenderQueue` (excludes ghost overlays) | ✅ Matches |

**Analysis:** Both CE and ArcNET compose objects with **global layer bands**. ArcNET's `BuildRenderQueue()` constructs seven distinct global bands:

1. Underlays (0..N) — all underlay auxiliaries globally
2. UnderAll Scenery (100M) — `OSCF_UNDER_ALL` scenery objects
3. Flat Objects (200M) — objects with `OF_FLAT`
4. Shadows (400M) — all shadow auxiliaries globally
5. Non-Flat + Ghost Overlays (600M) — standing objects and ghost/armor overlays merged
6. Other Overlays (700M) — non-ghost `OverlayBack`/`OverlayFore` auxiliaries
7. Roofs (800M) — all roof cells

This guarantees:
1. Every underlay renders before every flat/non-flat main sprite on screen.
2. Every overlay renders after every normal main sprite on screen.
3. Flat objects and non-flat objects never share the same main-layer bucket.
4. Ghost overlays compose with their parent objects in the non-flat band.

### 3.3 TileOrder Computation

| Aspect | Arcanum CE (`sub_4B93F0`) | ArcNET (`GetObjectTileOrderComponents`) | Parity |
|--------|---------------------------|-----------------------------------------|--------|
| First component | `v2 - v1` | `Secondary = vertical - horizontal` | ✅ Same value, different name |
| Second component | `v1 + v2` | `Primary = horizontal + vertical` | ✅ Same value, different name |

The arithmetic pair is present in both engines and is used identically.

- CE uses the pair when maintaining the **same-tile linked-list order** in `sector_object_list.c::objlist_insert_internal()`.
- ArcNET uses the pair in `BuildCeSameTileOrders()` via `InsertCeSameTileObject()` with the same structural rules.

Both engines enforce the same four rules:

1. Flat objects are forced ahead of non-flat objects.
2. `OSCF_UNDER_ALL` scenery is forced to the head of the flat segment.
3. Walls are forced ahead of portals.
4. The `sub_4B93F0()` pair is only consulted after those structural rules.

ArcNET carries `WallFlags` and `SceneryFlags` in `EditorMapObjectPreview` and splits flat/non-flat main sprites into separate global groups.

### 3.4 Same-Tile Special Cases

| Rule | Arcanum CE | ArcNET | Parity |
|------|-----------|--------|--------|
| Transparent wall visibility under faded roofs | Uses `OBJ_F_WALL_FLAGS` + `roof_is_faded()` to skip specific walls entirely | `ShouldHideTransparentWallUnderFadedRoof()` checks `WallFlags` + `TRANS_DISALLOW` + rotation + roof fade | ✅ Matches |
| Wall before portal on same tile | `objlist_insert_internal()` inserts walls ahead of portals | `InsertCeSameTileObject()` explicitly inserts walls before portals | ✅ Matches |
| `OSCF_UNDER_ALL` scenery | Uses `OBJ_F_SCENERY_FLAGS` at insertion time and 100M draw band at render time | `IsUnderAllScenery()` checks `SceneryFlags.UnderAll` and places in 100M band | ✅ Matches |
| Overlay slot order | `idx = 6..0`, field order `OVERLAY_FORE` then `OVERLAY_BACK` | `GenerateAuxiliaryItems` iterates `overlaySlotCount-1` down to `0`, checking fore then back per slot | ✅ Matches |

---

## 4. Object Rendering

### 4.1 Object Flags

| Flag | CE Value | ArcNET | Parity |
|------|----------|--------|--------|
| `OF_FLAT` | `0x04` | `ObjectFlags.Flat` (0x4) | ✅ |
| `OF_TRANSLUCENT` | `0x40` | `ObjectFlags.Translucent` (0x40) | ✅ Mapped to 50% opacity |
| `OF_SHRUNK` | `0x80` | `ObjectFlags.Shrunk` (0x80) | ✅ |
| `OF_DONTDRAW` | `0x100` | `ObjectFlags.DontDraw` (0x100) | ✅ Editor-only (CE gameplay flag) |
| `OF_INVISIBLE` | `0x200` | `ObjectFlags.Invisible` (0x200) | ✅ Not filtered in editor |
| `OF_HAS_OVERLAYS` | `0x8000` | `ObjectFlags.HasOverlays` (0x8000) | ✅ |
| `OF_HAS_UNDERLAYS` | `0x10000` | `ObjectFlags.HasUnderlays` (0x10000) | ✅ |
| `OF_WADING` | `0x20000` | `ObjectFlags.Wading` (0x20000) | ✅ Shadow tint only |
| `OF_STONED` | `0x80000` | `ObjectFlags.Stoned` (0x80000) | ✅ Mapped to grayscale |
| `OF_DONTLIGHT` | `0x100000` | `ObjectFlags.DontLight` (0x100000) | ✅ Mapped to light bypass |
| `OF_TEXT_FLOATER` | `0x200000` | `ObjectFlags.TextFloater` (0x200000) | ❌ No text layer |
| `OF_FROZEN` | `0x10000000` | `ObjectFlags.Frozen` (0x10000000) | ❌ Missing blue tint (M5) |
| `OF_ANIMATED_DEAD` | `0x20000000` | `ObjectFlags.AnimatedDead` (0x20000000) | ✅ Mapped to green tint |
| `OSCF_UNDER_ALL` | `0x0200` | `SceneryFlags.UnderAll` | ✅ |

### 4.2 Object Fields

| Field | CE | ArcNET | Parity |
|-------|-----|--------|--------|
| Current art ID | `OBJ_F_CURRENT_AID` | `ObjectField.CurrentAid` | ✅ |
| Location | `OBJ_F_LOCATION` | `ObjectField.Location` | ✅ |
| Offset X/Y | `OBJ_F_OFFSET_X/Y` | `ObjectField.OffsetX/Y` | ✅ |
| Shadow | `OBJ_F_SHADOW` | `ObjectField.Shadow` | ✅ |
| Underlay | `OBJ_F_UNDERLAY` (4 slots) | `ObjectField.Underlay` | ✅ |
| Overlay back | `OBJ_F_OVERLAY_BACK` (7 slots) | `ObjectField.OverlayBack` | ✅ |
| Overlay fore | `OBJ_F_OVERLAY_FORE` (7 slots) | `ObjectField.OverlayFore` | ✅ |
| Blit scale | `OBJ_F_BLIT_SCALE` | `ObjectField.BlitScale` | ✅ |
| Object flags | `OBJ_F_FLAGS` | `ObjectField.ObjectFlags` | ✅ |
| Wall flags | `OBJ_F_WALL_FLAGS` | `EditorMapObjectPreview.WallFlags` | ✅ |
| Scenery flags | `OBJ_F_SCENERY_FLAGS` | `EditorMapObjectPreview.SceneryFlags` | ✅ |
| Rotation | `OBJ_F_PAD_IAS_1` | `ObjectField.PadIas1` | ✅ |
| Rotation pitch | N/A | `ObjectField.RotationPitch` | ⚠️ ArcNET extension |

### 4.3 Committed Render Layer

ArcNET introduces `EditorMapCommittedRenderLayer` that CE doesn't have as a separate concept:

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

This is an ArcNET-specific classification for the host renderer. CE achieves the same effect through its integer sort key ranges plus same-tile list ordering.

**Current state:** `GetCommittedRenderLayer()` checks `ObjectFlags.Flat` first, returning `GroundDecal` for flat objects. Remaining types are classified by `ObjectType` as before.

### 4.4 Sprite Bounds

| Aspect | CE | ArcNET | Parity |
|--------|-----|--------|--------|
| Frame data | `TigArtFrameData` (width, height, hot_x, hot_y, offset_x, offset_y) | `EditorMapObjectSpriteBounds` (MaxFrameWidth, MaxFrameHeight, MaxFrameCenterX, MaxFrameCenterY) | ✅ Equivalent |
| Multi-rotation | Scans all frames across rotations | Same approach | ✅ |
| Hit testing | `sub_502FD0(art_id, x, y)` pixel test | `ContainsRenderPoint()` uses sprite bounds | ⚠️ Bounds vs pixel |

---

## 5. Roof Rendering

### 5.1 Roof Piece Alpha Values

| Piece | CE Corner Order [TL,TR,BR,BL] | ArcNET [TL,TR,BL,BR] | Parity |
|-------|------------------------------|----------------------|--------|
| 0 (NW Outside) | [255, 128, partial, full_trans] | [255, 128, 128, 255] | ✅ |
| 1 (West) | [partial, full_op, partial, full_trans] | [128, 0, 128, 255] | ✅ |
| 2 (North) | [full_trans, partial, full_op, partial] | [255, 128, 0, 128] | ✅ |
| 3 (NW Inside) | [partial, full_op, full_op, partial] | [128, 0, 0, 128] | ✅ |
| 4 (SW Outside) | [partial, partial, full_trans, full_trans] | [128, 128, 255, 255] | ✅ |
| 5 (SW Inside) | [full_op, full_op, partial, partial] | [0, 0, 128, 128] | ✅ |
| 6 (NE Inside) | [partial, partial, full_op, full_op] | [128, 128, 0, 0] | ✅ |
| 7 (NE Outside) | [full_trans, full_trans, partial, partial] | [255, 255, 128, 128] | ✅ |
| 8 (Center) | [full_op, full_op, full_op, full_op] | [0, 0, 0, 0] | ✅ |
| 9 (SE Outside) | [partial, full_trans, full_trans, partial] | [128, 255, 255, 128] | ✅ |
| 10 (South) | [full_op, partial, full_trans, partial] | [0, 128, 255, 128] | ✅ |
| 11 (East) | [partial, full_trans, partial, full_op] | [128, 255, 128, 0] | ✅ |
| 12 (SE Inside) | [full_op, partial, partial, full_op] | [0, 128, 128, 0] | ✅ |

**Note:** CE uses `[TL, TR, BR, BL]` (counter-clockwise). ArcNET uses `[TL, TR, BL, BR]` (reading order). The values match when the corner order difference is accounted for.

### 5.2 Roof Opacity Values

| Value | CE (`roofshade.mes`) | ArcNET | Parity |
|-------|---------------------|--------|--------|
| Full opacity | Line 0 → 0 | 0 | ✅ |
| Partial opacity | Line 1 → 128 | 128 | ✅ |
| Full transparency | Line 2 → 255 | 255 | ✅ |

### 5.3 Roof Coverage

| Aspect | CE | ArcNET | Parity |
|--------|-----|--------|--------|
| Coverage check | `roof_is_covered_loc(loc, check_faded)` | No core-builder equivalent found | ❌ Missing |
| Object hiding | Objects under non-faded roofs skipped before enqueue | `IsRoofCovered` fields exist, but the builder never sets them and committed objects are still queued | ❌ Missing |

---

## 6. Wall & Portal System

### 6.1 Wall Rendering in Isometric View

| Aspect | CE | ArcNET | Parity |
|--------|-----|--------|--------|
| Wall as object | Yes, `OBJ_TYPE_WALL` drawn in `object_draw()` | Yes, `ObjectType.Wall` in RenderQueue | ✅ |
| Wall transparency | `OWAF_TRANS_LEFT/RIGHT` flags | No committed render-time equivalent; `WallFlags` are not carried into the preview | ❌ Missing |
| Transparent wall under faded roof | Skipped (not drawn) | No committed visibility rule; only the `+19/-20` ordering helper exists | ❌ Missing |
| Wall rotation for transparency | Rotation 0,1,6,7 = cardinal | `GetCeWallPortalOrderingOffsetY()`: rot 2-5 → +19 | ✅ Logic matches |

### 6.2 Portal Handling

| Aspect | CE | ArcNET | Parity |
|--------|-----|--------|--------|
| Portal type | `OBJ_TYPE_PORTAL` | `ObjectType.Portal` | ✅ |
| Sort priority | Same as other objects | `TypeSortPriority = 1` (after others) | ⚠️ ArcNET adds priority |
| Portal transparency | Same wall flags | Same wall flags | ✅ |

### 6.3 Wall Piece System

CE uses `p_piece` (0-45) embedded in wall art IDs to represent different wall segments:
- Pieces 0-8: Straight and corner segments
- Pieces 9-20: First curved wall set
- Pieces 21-33: Second curved wall set
- Pieces 34-45: Third curved wall set

ArcNET does not implement wall piece management (this is editor functionality, not rendering).

---

## 7. Lighting System

### 7.1 Light Data

| Aspect | CE | ArcNET | Parity |
|--------|-----|--------|--------|
| Light storage | `SectorLightList` → `Light*` linked list | `EditorMapLightPreview` list | ✅ |
| Light art ID | `tig_art_id_t` | `ArtId` | ✅ |
| Light color | RGB packed in `tig_color_t` | `uint SuggestedTintColor` | ✅ |
| Light flags | `LF_*` flags | `SectorLightFlags` | ✅ |
| Light position | `loc + offset_x/y` | `MapTileX/Y + projected center` | ✅ |

### 7.2 Per-Tile Light Sampling

| Aspect | CE | ArcNET | Parity |
|--------|-----|--------|--------|
| Sampling method | `sub_4D89E0()` — per-pixel light mask sampling | `EditorMapTileLightDiagnostics` — 3×3 grid | ⚠️ Different resolution |
| Light grid | 3×3 sample grid from `sub_4DA360` | 3×3 `uint?` grid | ✅ Matches |
| Indoor/outdoor | Palette switching per tile type | Not directly implemented | ⚠️ Host responsibility |
| Ambient color | `light_indoor_color` / `light_outdoor_color` | Not in render items | ⚠️ Host responsibility |

### 7.3 Shadow System

| Aspect | CE | ArcNET | Parity |
|--------|-----|--------|--------|
| Shadow art | `OBJ_F_SHADOW` field | `ObjectField.Shadow` | ✅ |
| Shadow sort order | 400M layer | `GetAuxiliaryLayerSortOffset(Shadow) = -2` | ⚠️ Different position |
| Shadow rendering | Separate shadow objects with palettes | `EditorMapObjectAuxiliaryRenderItem` with `Layer = Shadow` | ✅ |

---

## 8. Facade System

### 8.1 Facade Loading

| Aspect | CE | ArcNET | Parity |
|--------|-----|--------|--------|
| Data source | `walkmask_load(facade_num)` | `FacadeWalk` from `art/facade/facwalk.{num:X2}` | ✅ Equivalent |
| Grid structure | 2D array of `tig_art_id_t` | `IReadOnlyList<FacadeWalkEntry>` | ✅ Equivalent |
| Origin | Centered on location | Centered on selected tile | ✅ |

### 8.2 Facade Rendering

| Aspect | CE | ArcNET | Parity |
|--------|-----|--------|--------|
| Isometric offset | `tile_rect.x++` | Builder does not currently apply the shim | ❌ Missing |
| Art blitting | `tig_window_blit_art()` | Via `EditorMapPaintableSceneBuilder` | ✅ |

---

## 9. Art System

### 9.1 Art Type Mapping

| CE `TigArtType` | Value | ArcNET `ArtId.TypeCode` | Parity |
|-----------------|-------|------------------------|--------|
| `TIG_ART_TYPE_TILE` | 0 | `TypeCode.Tile (None = 0)` | ✅ |
| `TIG_ART_TYPE_WALL` | 1 | `TypeCode.Wall (1)` | ✅ |
| `TIG_ART_TYPE_CRITTER` | 2 | `TypeCode.Critter (2)` | ✅ |
| `TIG_ART_TYPE_PORTAL` | 3 | `TypeCode.Portal (3)` | ✅ |
| `TIG_ART_TYPE_SCENERY` | 4 | `TypeCode.Scenery (4)` | ✅ |
| `TIG_ART_TYPE_INTERFACE` | 5 | `TypeCode.Interface (5)` | ✅ |
| `TIG_ART_TYPE_ITEM` | 6 | `TypeCode.Item (6)` | ✅ |
| `TIG_ART_TYPE_CONTAINER` | 7 | `TypeCode.Container (7)` | ✅ |
| `TIG_ART_TYPE_MISC` | 8 | `TypeCode.Misc (8)` | ✅ |
| `TIG_ART_TYPE_LIGHT` | 9 | `TypeCode.Light (9)` | ✅ |
| `TIG_ART_TYPE_ROOF` | 10 | `TypeCode.Roof (10)` | ✅ |
| `TIG_ART_TYPE_FACADE` | 11 | `TypeCode.Facade (11)` | ✅ |
| `TIG_ART_TYPE_MONSTER` | 12 | `TypeCode.Monster (12)` | ✅ |
| `TIG_ART_TYPE_UNIQUE_NPC` | 13 | `TypeCode.UniqueNpc (13)` | ✅ |
| `TIG_ART_TYPE_EYE_CANDY` | 14 | `TypeCode.EyeCandy (14)` | ✅ |

### 9.2 Blit Flag Mapping

| CE Flag | ArcNET `EditorMapSpriteBlendMode` | Parity |
|---------|----------------------------------|--------|
| None | `SourceOver` (0) | ✅ |
| `TIG_ART_BLT_BLEND_ADD` | `Add` (1) | ✅ |
| `TIG_ART_BLT_BLEND_SUB` | `Subtract` (2) | ✅ |
| `TIG_ART_BLT_BLEND_MUL` | `Multiply` (3) | ✅ |
| `TIG_ART_BLT_BLEND_ALPHA_LERP_BOTH` | `RoofAlphaLerp` per-corner | ✅ |
| `TIG_ART_BLT_BLEND_COLOR_CONST` | `SuggestedTintColor` | ✅ |
| `TIG_ART_BLT_BLEND_COLOR_ARRAY` | `ObjectColorArray` | ✅ |
| `TIG_ART_BLT_BLEND_ALPHA_LERP_X` | `ObjectAlphaLerp` (wall transparency) | ✅ |

---

## 10. Sector Data

### 10.1 Sector Structure

| Aspect | CE | ArcNET | Parity |
|--------|-----|--------|--------|
| Tile grid | `SectorTileList.art_ids[4096]` | `uint[] TileArtIds` (4096) | ✅ |
| Roof grid | `SectorRoofList.art_ids[256]` | `uint[] RoofArtIds` (256) | ✅ |
| Block mask | `SectorBlockList.blocked[128]` (4096-bit) | `uint[] BlockMask` (128 uint32) | ✅ |
| Object lists | `SectorObjectList.heads[4096]` | `IReadOnlyList<EditorMapObjectPreview>` | ✅ |
| Light list | `SectorLightList.head` linked list | `IReadOnlyList<EditorMapLightPreview>` | ✅ |
| Tile scripts | `TileScriptList` | `IReadOnlyList<EditorMapTileScriptPreview>` | ✅ |

### 10.2 Tile Row Mask Acceleration

| Aspect | CE | ArcNET | Parity |
|--------|-----|--------|--------|
| Sparse iteration | Iterates all tiles, checks art_id != INVALID | `TileRowMasks` — `ulong[64]` bitmask per row | ✅ ArcNET optimization |
| Roof row mask | N/A | `RoofRowMasks` — `ulong[16]` bitmask | ✅ ArcNET optimization |
| Light lookup | Linear scan of light list | `LightTileIndices` — `HashSet<int>` | ✅ ArcNET optimization |
| Script lookup | Linear scan of script list | `ScriptedTileIndices` — `HashSet<int>` | ✅ ArcNET optimization |

---

## 11. Parity Gap Summary

### 11.1 Critical Gaps (Visual Impact)

All five core rendering critical gaps (C1-C5) have been resolved in the active C# codebase:

| # | Gap | Impact | Status | Resolution / Details |
|---|-----|--------|--------|---------------------|
| C1 | Flat and non-flat main sprites share one ArcNET object bucket instead of CE's 200M vs 600M groups | Ground decals, floor scenery, walls, critters, and items can compose in the wrong order | ✅ Resolved | **Unified global bands**: Placed into distinct passes in `BuildRenderQueue()` (underlays, under-all scenery, flat, shadow, standing, overlay, roof). |
| C2 | Underlays, shadows, and overlays are local per-object offsets instead of CE global layer bands | Auxiliary layers can interleave with other objects differently than CE | ✅ Resolved | **Unified Global Bands**: Auxiliaries are generated per-object and added to globally ordered buckets rather than local rendering offsets. |
| C3 | `WallFlags` and `SceneryFlags` are dropped before rendering | Blocks `OWAF_TRANS_*` transparency rules and `OSCF_UNDER_ALL` ordering | ✅ Resolved | **Flags Preserved**: Carried in `EditorMapObjectPreview` and used for conditional roof fade hiding and `OSCF_UNDER_ALL` scenery sort overrides. |
| C4 | Overlay iteration order differs (`6..0`, fore-then-back in CE) | Multi-slot eye-candy stacks can render in the wrong order | ✅ Resolved | **Reverse Overlay Iteration**: `GenerateAuxiliaryItems` now iterates from `overlaySlotCount - 1` down to `0` checking fore-then-back per slot. |
| C5 | Roof coverage hide logic is missing in the core ArcNET builder | Objects/auxiliaries under solid roofs remain renderable | ✅ Resolved | **Matrix-Driven Roof Occlusion**: `IsRoofCovered` and `ShouldHideTransparentWallUnderFadedRoof` evaluate sector roof arrays to drop occluded items. |
### 11.2 Minor Gaps (Low Impact)

| # | Gap | Impact | Status | Resolution / Details |
|---|-----|--------|--------|---------------------|
| M1 | Portal `TypeSortPriority` is an ArcNET-only approximation | Same-tile wall/portal ordering is only partially mirrored | ✅ Resolved | **Explicit Same-Tile Order**: Same-tile sorting explicitly places walls before portals during collection building. |
| M2 | `GroundDecal` committed layer is never assigned | Host-facing layer taxonomy does not reflect CE flat objects yet | ✅ Resolved | **Flat-to-Decal Mapping**: Flat objects map directly to the `GroundDecal` committed layer. |
| M3 | Facade isometric `x++` offset is not applied in `EditorMapFacadePaintableSceneBuilder` | Possible 1px facade shift | ✅ Resolved | **Facade Shim Applied**: The builder now applies `centerX += 1d` in isometric mode. |
| M4 | Wading effect (15px shift, alpha=92) | Critters in water tiles do not wading-render | ❌ Open | Gameplay critter effect. |
| M5 | Frozen object effect | Icy critters do not render with tint | ✅ Resolved | Additive blend + blue multiplier `0xFF0080FF` in `CreateItem()`. |
| M6 | Editor destroyed/off tint | Editor mode highlights not rendered | ✅ Resolved | Additive red `0xFFFF0000` or green `0xFF00FF00` in `CreateItem()`. |
| M7 | Hover highlight (underlay/overlay pulsing) | Cursor hovering over objects does not pulse | ❌ Open | Render-level interactive outline pulsing. |
| M8 | Hit testing uses bounds vs. pixel test | Selection accuracy differs | ⚠️ Acceptable | Standard editor bounding-box checks. |
| M9 | Quadrant-Based Light Interpolation (LERP) | Lacks smooth, non-linear floor tiling light blends | ✅ Resolved | **Quadrant Light LERP**: `BuildFloorTileQuadrants` in `EditorMapPaintableScene` splits tiles into 4 quadrants with 9-vertex color interpolation when `LightDiagnostics.HasInterpolationVariance` is true. |
| M10 | Wading Shadow Color Multiplication | Shadows of wading critters lack (92, 92, 92) multiplication | ✅ Resolved | **Wading Shadow Tint**: `GenerateAuxiliaryItems` applies `SuggestedTintColor = 0xFF5C5C5C` (92, 92, 92) when `obj.IsWading` is true. |
| M11 | Scaled Sprite Dirty Rect Bypass | Scaled sprites (`scale != 100`) have rounding blit artifacts | ❌ Open | CE bypasses dirty culling for scaled sprites entirely. |
| M12 | NPC Underlay Reaction Tints | Reaction underlays (ID 433) lack const color mapping | ✅ Resolved | **Reaction Underlay Tint**: `GenerateAuxiliaryItems` detects `artId.Value == 433`, sets `SuggestedTintColor = obj.ReactionColor` and forces `ScalePercent = 100`, `IsShrunk = false`. |
| M13 | Armor/Critter Ghost Stacking | Ghost overlays render correctly in non-flat band | ✅ Resolved | **Correct SubOrder + Checks**: `BuildRenderQueue` L1833 assigns main objects `SubOrder: 0` and ghost/armor overlays `SubOrder: 1` (ascending sort = ghosts after parent). `IsGhostOrArmorOverlay` L1165: Armor overlays unconditionally in non-flat band; NPC dead overlays with art 243 in non-flat band; PC correctly excluded. All match CE `object_draw()` at line ~695. |
| M14 | Subtractive shadow blending | Shadows lack subtractive blend mode | ✅ Resolved | **Subtract Blend**: `GenerateAuxiliaryItems` sets `BlendMode = Subtract` for shadow auxiliaries; `CreateItem` maps to `UseSubtractiveShadowBlend = true`. |
| M15 | `OF_ANIMATED_DEAD` Object Tint | Undead animation tint not applied | ✅ Resolved | **Green Tint**: `CreateItem` applies `SuggestedTintColor = 0xFF00FF00` when `ObjectFlags.AnimatedDead` is set. |
| M16 | `OF_STONED` Grayscale Palette | Petrified objects lack grayscale | ✅ Resolved | **Grayscale Override**: `CreateItem` sets `UseGrayscalePaletteOverride = true` when `ObjectFlags.Stoned` is set. |
| M17 | `OF_DONTLIGHT` Light Mask Bypass | Lighting applied to unlit objects | ✅ Resolved | **Light Bypass**: `CreateItem` sets `TintIgnoresLightVisibility = true` when `ObjectFlags.DontLight` is set. |
| M18 | `OF_INVISIBLE` Editor Visibility | Invisible objects correctly visible in editor mode | ✅ Resolved | **Editor Shows All**: `ProcessSector` L678 comment: "Do not filter OF_INVISIBLE here — the flag is only meaningful in gameplay mode." CE editor sets `dword_5E2F88 = 0` and `dword_5E2EC8 = 0`. |
| M19 | Top-Down Wall Editor Overlays | Top-down walls lack 2px red geometry | ❌ Open | CE renders top-down walls as lines/dots, not sprites. |
| M20 | Floating Text Rendering | Text bubbles do not render | ❌ Open | Missing `EditorMapTextRenderItem` layer for `OF_TEXT`/`OF_TEXT_FLOATER`. |
| M21 | Roof Fade Alpha LERP Gradients | Faded roofs lack 4-corner smooth gradients | ✅ Resolved | **13-Piece Alpha LERP**: `GetRoofAlphaLerp` in `EditorMapPaintableSceneBuilder` applies correct 4-corner alpha for all 13 roof piece types when `ArtId.IsRoofFaded` is true. |
| M22 | Eye Candy Scale Types | Eye candy is rendered at wrong scale | ✅ Resolved | **Scale Type Multiplier**: `AdjustEyeCandyRequest` applies the CE `dword_5A548C` lookup `[50,63,75,87,100,130,160,200]` based on ArtId scale type bits when `Type == EyeCandy`. |
| M23 | `OF_TRANSLUCENT` Opacity Mapping | Translucent objects lack 50% blend | ✅ Resolved | **50% Opacity**: `BuildObject` applies `SuggestedOpacity = 0.5d` when `ObjectFlags.Translucent` is set. |
| M24 | `OWAF_TRANS_DISALLOW` Wall Flag | Walls with DISALLOW flag correctly remain visible under faded roofs | ✅ Resolved | **Disallow Checked**: `ShouldHideTransparentWallUnderFadedRoof` L1119 checks `(WallFlags & WallTransDisallow) == 0` where `WallTransDisallow = 0x0001` (L16). |
| N1 | CE Overlay Scale Check Bug | Minor rendering difference for overlays with scale_type=4 | Note | **CE Bug**: CE `object.c` ~L710 checks `scale_type != 100` instead of `overlay_scale != 100`. Since `scale_type` is an index (0-7), the check always passes and overlay source rects are always scaled. ArcNET handles scale at the sprite request level via `AdjustEyeCandyRequest()`. |
| N2 | `qsort` Instability | Objects with identical sort keys may render in different order | Note | **Sort Stability**: CE uses C `qsort` (not guaranteed stable) for the blit queue. ArcNET uses stable `OrderBy().ThenBy()` with additional tie-breakers (`Kind`, `Index`). |
| N3 | Wading Eye Candy Y-Offset | Wading underlays/overlays not shifted down 15px | ❌ Open | **Missing Offset**: CE `sub_443620()` adds `rect.y += 15` for wading objects when computing ALL eye candy bounding rects (underlays, overlays, shadows). ArcNET only applies wading tint to shadows. |
| N4 | Per-Type Object Visibility Toggles | No per-type rendering enable/disable | Low | **Editor Feature**: CE `object_type_visibility[18]` allows toggling specific object types (WALL, PORTAL, SCENERY, etc.). ArcNET has `request.IncludeObjects` (all-or-nothing). |
| N5 | `BlitFlags`/`BlitAlpha`/`BlitColor` Not Mapped | Per-object custom blend modes unsupported | ❌ Open | **Missing Fields**: CE objects can have per-object custom blend modes (`OBJ_F_BLIT_FLAGS`), alpha (`OBJ_F_BLIT_ALPHA`), and color (`OBJ_F_COLOR`). `EditorMapObjectPreview` does not carry these fields. |
| N6 | `OBJ_F_RENDER_FLAGS` Cached Lighting State | Per-object lighting state not replicated | Low | **Architectural**: CE caches lighting/palette state per object in `OBJ_F_RENDER_FLAGS`. ArcNET delegates lighting to the host renderer. |
### 11.3 Verified Parity Items

| # | Item | Status |
|---|------|--------|
| V1 | Isometric projection formula | ✅ Matches |
| V2 | Object anchor offset (+40, +20) | ✅ Matches (fix #1) |
| V3 | Roof anchor offset (-120, -200) | ✅ Matches |
| V4 | Sector coordinate bitmask (0x3F) | ✅ Matches (fix #20) |
| V5 | Roof piece alpha values (all 13 pieces) | ✅ Matches |
| V6 | Roof opacity values (0, 128, 255) | ✅ Matches |
| V7 | Art type enum (0-14) | ✅ Matches |
| V8 | Object flags (all rendering-relevant) | ✅ Matches |
| V9 | Object field mapping | ✅ Matches |
| V10 | Sector data structure (tiles, roofs, blocks, objects, lights) | ✅ Matches |
| V11 | Wall/portal same-tile offset constants (`+19` / `-20`) | ✅ Mirrored in helper |
| V12 | `GetLayoutSpriteCenter` simplified (fix #3) | ✅ Fixed |
| V13 | `AdjustSpriteCenter` simplified (fix #10) | ✅ Fixed |
| V14 | `RequiresCeWallPortalHotspotAdjustment` removed (fix #11) | ✅ Fixed |
| V15 | Async pipeline unified (fixes #15-18) | ✅ Fixed |
| V16 | Archive handle caching (fix #13) | ✅ Fixed |
| V17 | `PreloadArtsAsync` parallel loading (fix #12) | ✅ Fixed |
| V18 | `IsCompatibleFamily` art path filtering (fix #9) | ✅ Fixed |
| V19 | Facade MES fallback (fix #8) | ✅ Fixed |
| V20 | `LastOrDefault` hit testing (fix #22) | ✅ Fixed |
| V21 | Ghost overlay SubOrder (main=0, ghost=1) | ✅ Matches CE `object_draw()` non_flat_order sequencing |
| V22 | Armor overlay unconditional non-flat band | ✅ Matches CE `OBJ_TYPE_ARMOR` check |
| V23 | PC dead overlay exclusion from ghost band | ✅ Matches CE `OBJ_TYPE_NPC`-only check |
| V24 | Invisible objects visible in editor | ✅ Matches CE `dword_5E2F88 = 0`, `dword_5E2EC8 = 0` |
| V25 | OWAF_TRANS_DISALLOW wall flag checked | ✅ Matches CE wall transparency exclusion |
| V26 | Quadrant-based floor light LERP (9-vertex) | ✅ Matches CE `sub_4DA360` 3x3 light sampling |
| V27 | Same-tile flat-before-non-flat insertion | ✅ Matches CE `sub_4F20A0()` insertion logic |
| V28 | Same-tile UnderAll at head of flat segment | ✅ Matches CE `OSCF_UNDER_ALL` head insertion |
| V29 | Same-tile wall-before-portal insertion | ✅ Matches CE wall/portal type priority |
| V30 | Tile order components (Primary, Secondary) | ✅ Matches CE `sub_4B93F0()` arithmetic |

---

## 12. Current Status & Next Steps

### 12.1 Completed Parity Milestones
1. **✅ Preserved Wall/Scenery Flags**: Carried `WallFlags` and `SceneryFlags` into preview structures and utilized them in sorting/hiding logic.
2. **✅ Split Composition into Global Bands**: Implemented global composition loops in `BuildRenderQueue` that separate underlays, flat items, scenery, shadows, standing items, overlays, and roofs.
3. **✅ Recreated Same-Tile Order**: Resolved wall-portal ordering and implemented reverse overlay iteration sequencing.
4. **✅ Matrix-Driven Roof Hiding**: Fully integrated roof coverage matrix evaluations for tiles and objects.
5. **✅ Applied Facade Shim**: Implemented the `x++` offset in the facade preview pipeline.
6. **✅ Ghost Overlay Band Placement**: Ghost/armor overlays correctly placed in non-flat band (600M+) with correct SubOrder sequencing matching CE.
7. **✅ Editor Visibility Rules**: Invisible, DontDraw, and destroyed/off objects handled correctly for editor mode.
8. **✅ OWAF_TRANS_DISALLOW**: Wall transparency exclusion flag correctly prevents hiding walls with this flag under faded roofs.
9. **✅ Quadrant-Based Light LERP**: 9-vertex floor tile light interpolation implemented via `BuildFloorTileQuadrants`.
10. **✅ All Object Flag Visual Mappings**: Stoned (grayscale), DontLight (light bypass), AnimatedDead (green tint), Translucent (50% opacity) all mapped in `CreateItem()`.
11. **✅ Frozen Object Blue Tint (M5)**: Check `ObjectFlags.Frozen` and set `BlendMode = Add` + `SuggestedTintColor = 0xFF0080FF` in `CreateItem()`.
12. **✅ Editor Destroyed/Off Tint (M6)**: Check `ObjectFlags.Destroyed` → red `(0xFFFF0000)` and `ObjectFlags.Off` → green `(0xFF00FF00)` with `BlendMode = Add` in `CreateItem()`.

### 12.2 Implementation Plan for Remaining Gaps
1. **Wading Main Sprite Effect (M4)**: Implement 15px Y-shift + bottom strip alpha=92 for non-flat wading objects; entire sprite alpha for flat wading objects. Also shift eye candy rects +15px Y. CE reference: `object_draw()` at `object.c:767`.
2. **Per-Object Blit Flags (N5)**: Carry `ObjectField.BlitFlags`, `ObjectField.BlitAlpha`, and `ObjectField.BlitColor` into `EditorMapObjectPreview` and map to `BlendMode`/`SuggestedOpacity`/`SuggestedTintColor` in `CreateItem()`.
3. **Top-Down Wall Overlays (M19)**: Introduce geometric `EditorMapRenderPoint` overlays for walls in top-down view mode, overriding sprite resolution.
4. **Floating Text (M20)**: Implement a dedicated `EditorMapTextOverlayRenderItem` and populate it from `OF_TEXT` / `OF_TEXT_FLOATER` payloads.
