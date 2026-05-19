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
| Flat main sprite | Dedicated global group `200,000,000..` | Same main-object bucket as non-flat objects | ❌ Missing separation |
| Shadows | Dedicated global group `400,000,000..` | `objectSortKey - 2` local to the parent object's bucket | ❌ Different model |
| Non-flat main sprite | Dedicated global group `600,000,000..` | Same main-object bucket as flat objects | ❌ Missing separation |
| Overlays | Global group `700,000,000..` | `objectSortKey + 1/+2` local to the parent object's bucket | ❌ Different model |

**Analysis:** CE composes objects with **global layer bands**. ArcNET composes them with **per-object local offsets** inside one shared `drawOrder * 4096` slot system. That is not a cosmetic implementation detail; it changes inter-object ordering:

1. CE guarantees every underlay renders before every flat/non-flat main sprite on screen.
2. CE guarantees every overlay renders after every normal main sprite on screen.
3. CE guarantees flat objects and non-flat objects never share the same main-layer bucket.

ArcNET currently guarantees only relative ordering around one parent object. It does not yet reproduce CE's global underlay, flat, shadow, non-flat, and overlay bands.

### 3.3 TileOrder Computation

| Aspect | Arcanum CE (`sub_4B93F0`) | ArcNET (`GetObjectTileOrderComponents`) | Parity |
|--------|---------------------------|-----------------------------------------|--------|
| First component | `v2 - v1` | `Secondary = vertical - horizontal` | ✅ Same value, different name |
| Second component | `v1 + v2` | `Primary = horizontal + vertical` | ✅ Same value, different name |

The arithmetic pair is present in both engines, but it is used differently.

- CE uses the pair when maintaining the **same-tile linked-list order** in `sector_object_list.c::objlist_insert_internal()`.
- ArcNET uses the second CE component as part of the main `SortKey`, then uses the first CE component only as a tie-breaker.

That difference matters because CE same-tile ordering is not just math:

1. Flat objects are forced ahead of non-flat objects.
2. `OSCF_UNDER_ALL` scenery is forced to the head of the flat segment.
3. Walls are forced ahead of portals.
4. The `sub_4B93F0()` pair is only consulted after those structural rules.

ArcNET cannot reproduce all four rules today because it does not carry `WallFlags` or `SceneryFlags`, and it does not split flat/non-flat main sprites into separate global groups.

### 3.4 Same-Tile Special Cases

| Rule | Arcanum CE | ArcNET | Parity |
|------|-----------|--------|--------|
| Transparent wall visibility under faded roofs | Uses `OBJ_F_WALL_FLAGS` + `roof_is_faded()` to skip specific walls entirely | `EditorMapObjectPreview` does not carry `WallFlags`; helper only mirrors the `+19/-20` ordering offsets | ❌ Missing data and behavior |
| Wall before portal on same tile | `objlist_insert_internal()` inserts walls ahead of portals | `TypeSortPriority` pushes portals later in tie cases | ⚠️ Partial approximation |
| `OSCF_UNDER_ALL` scenery | Uses `OBJ_F_SCENERY_FLAGS` at insertion time and 100M draw band at render time | `EditorMapObjectPreview` does not carry `SceneryFlags` | ❌ Missing data and behavior |
| Overlay slot order | `idx = 6..0`, field order `OVERLAY_FORE` then `OVERLAY_BACK` | Emits all `OverlayBack` entries in ascending index, then all `OverlayFore` entries in ascending index | ❌ Different order |

---

## 4. Object Rendering

### 4.1 Object Flags

| Flag | CE Value | ArcNET | Parity |
|------|----------|--------|--------|
| `OF_FLAT` | `0x04` | `ObjectFlags.Flat` (0x4) | ✅ |
| `OF_TRANSLUCENT` | `0x40` | `ObjectFlags.Translucent` (0x40) | ✅ |
| `OF_SHRUNK` | `0x80` | `ObjectFlags.Shrunk` (0x80) | ✅ |
| `OF_DONTDRAW` | `0x100` | `ObjectFlags.DontDraw` (0x100) | ✅ |
| `OF_INVISIBLE` | `0x200` | `ObjectFlags.Invisible` (0x200) | ✅ |
| `OF_HAS_OVERLAYS` | `0x8000` | `ObjectFlags.HasOverlays` (0x8000) | ✅ |
| `OF_HAS_UNDERLAYS` | `0x10000` | `ObjectFlags.HasUnderlays` (0x10000) | ✅ |
| `OF_WADING` | `0x20000` | `ObjectFlags.Wading` (0x20000) | ✅ |
| `OF_DONTLIGHT` | `0x100000` | `ObjectFlags.DontLight` (0x100000) | ✅ |
| `OSCF_UNDER_ALL` | `0x0200` | Not tracked | ❌ Missing |

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
| Wall flags | `OBJ_F_WALL_FLAGS` | Not carried into `EditorMapObjectPreview` | ❌ Missing |
| Scenery flags | `OBJ_F_SCENERY_FLAGS` | Not carried into `EditorMapObjectPreview` | ❌ Missing |
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

**Current limitation:** `GetCommittedRenderLayer()` keys only on `ObjectType`. It does not use `OF_FLAT`, and `GroundDecal` is never assigned, so the enum does not currently encode CE's flat/non-flat split.

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
| M5 | Frozen object effect (additive + blue tint) | Icy critters do not render with tint | ❌ Open | Optional visual shader. |
| M6 | Editor destroyed/off tint (red/green) | Editor mode highlights not rendered | ❌ Open | Handled by host-level highlight overlays. |
| M7 | Hover highlight (underlay/overlay pulsing) | Cursor hovering over objects does not pulse | ❌ Open | Render-level interactive outline pulsing. |
| M8 | Hit testing uses bounds vs. pixel test | Selection accuracy differs | ⚠️ Acceptable | Standard editor bounding-box checks. |
| M9 | Quadrant-Based Light Interpolation (LERP) | Lacks smooth, non-linear floor tiling light blends | ❌ Open | **New CE Discovery**: Splits tile into 4 sub-quadrants to blend lighting colors across 9 vertices. |
| M10 | Wading Shadow Color Multiplication | Shadows of wading critters lack (92, 92, 92) multiplication | ❌ Open | **New CE Discovery**: Found in `object.c` wading checks. |
| M11 | Scaled Sprite Dirty Rect Bypass | Scaled sprites (`scale != 100`) have rounding blit artifacts | ❌ Open | **New CE Discovery**: CE bypasses dirty culling for scaled sprites entirely. |
| M12 | NPC Underlay Reaction Tints | Reaction underlays (ID 433) lack const color mapping | ❌ Open | **New CE Discovery**: Underlays are colored according to critter reaction flags. |
| M13 | Armor/Critter Ghost Stacking | Visual ghost overlay (ID 243) renders in wrong z-order band | ❌ Open | **New CE Discovery**: Ghost overlay enqueues in standard non-flat object bucket. |
| M14 | Subtractive shadow blending | Shadows lack subtractive blend mode | ❌ Open | **New CE Discovery**: Shadows blit with `TIG_ART_BLT_BLEND_SUB | TIG_ART_BLT_BLEND_COLOR_CONST`. |
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

---

## 12. Current Status & Next Steps

### 12.1 Completed Parity Milestones
1. **✅ Preserved Wall/Scenery Flags**: Carried `WallFlags` and `SceneryFlags` into preview structures and utilized them in sorting/hiding logic.
2. **✅ Split Composition into Global Bands**: Implemented global composition loops in `BuildRenderQueue` that separate underlays, flat items, scenery, shadows, standing items, overlays, and roofs.
3. **✅ Recreated Same-Tile Order**: Resolved wall-portal ordering and implemented reverse overlay iteration sequencing.
4. **✅ Matrix-Driven Roof Hiding**: Fully integrated roof coverage matrix evaluations for tiles and objects.
5. **✅ Applied Facade Shim**: Implemented the `x++` offset in the facade preview pipeline.

### 12.2 Implementation Plan for Newly Discovered Gaps
1. **Quadrant-Based Floor Light LERP (M9)**: Update `EditorMapPaintableSceneBuilder` to check tile lighting interpolation and divide floor tile blits into four quadrants, interpolating corner colors across the grid.
2. **Subtractive Shadow Blending (M14)**: Set shadow blend mode in the builder to subtractive with a constant multiplier matching the shadow's source color.
3. **Wading Shadow Blend (M10)**: Apply `(92, 92, 92)` constant color multiplier to the shadow blit payload for active wading objects.
4. **Scaled Sprite Dirty Culling Bypass (M11)**: Detect non-100% scale sprites in culling checks, bypass viewport culling, and dirty the complete bounds for the subsequent frame.
5. **NPC Reaction Underlay Tinting (M12)**: Implement a reaction-to-color lookup mapping the NPC reaction level to underlay CONST_COLOR.
