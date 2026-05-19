# Arcanum CE — Object & Overlay Rendering Technical Analysis

> Source: [arcanum-ce](https://github.com/arcanum-ce/arcanum-ce) reverse-engineered C source.  
> Files analyzed: `obj.c/.h`, `obj_flags.c/.h`, `object.c/.h`, `wall.c/.h`, `facade.c/.h`, `roof.c/.h`, `light.c/.h`

---

## Table of Contents

1. [Object Data Model (`obj.c` / `obj.h`)](#1-obj)
2. [Object Flags (`obj_flags.c` / `obj_flags.h`)](#2-obj_flags)
3. [Object Rendering (`object.c` / `object.h`)](#3-object)
4. [Wall Rendering (`wall.c` / `wall.h`)](#4-wall)
5. [Facade System (`facade.c` / `facade.h`)](#5-facade)
6. [Roof Rendering (`roof.c` / `roof.h`)](#6-roof)
7. [Shadow System (`light.c` / `light.h`)](#7-shadow)
8. [CE-Specific Fixes & Modifications](#8-ce)

---

## 1. Object Data Model

### 1.1 `Object` Struct (obj.c)

```c
typedef struct Object {
    /* 0000 */ int type;                    // ObjectType enum (WALL, PORTAL, SCENERY, NPC, etc.)
    /* 0008 */ ObjectID oid;                // Persistent object ID (GUID or block ID)
    /* 0020 */ ObjectID prototype_oid;      // Prototype reference (OID_TYPE_BLOCKED = is a prototype)
    /* 0038 */ int64_t prototype_obj;       // Cached handle to prototype
    /* 0040 */ int field_40;
    /* 0044 */ int16_t modified;            // Dirty flag for save diffs
    /* 0046 */ int16_t num_fields;
    /* 0048 */ int* field_48;               // Field metadata indices
    /* 004C */ int* field_4C;               // Override bitmask (per-field changed bits)
    /* 0050 */ intptr_t* data;              // Field storage (scalars, arrays, pointers)
    /* 0054 */ intptr_t transient_properties[19]; // Runtime-only fields (RENDER_*, LIGHT_*, etc.)
} Object;
```

**Key CE note**: The `data` and `transient_properties` types are changed to `intptr_t` (from `int` in original) to correctly handle pointer-width fields. This is critical on 64-bit builds.

### 1.2 Object Field Enum — Rendering-Relevant Fields

```c
// Persistent fields (stored in save files)
OBJ_F_CURRENT_AID     // Current art ID (sprite)
OBJ_F_LOCATION         // World location (int64_t: high 32=y, low 32=x)
OBJ_F_OFFSET_X         // Pixel offset from tile center
OBJ_F_OFFSET_Y         // Pixel offset from tile center
OBJ_F_SHADOW           // Base shadow art ID
OBJ_F_OVERLAY_FORE     // Foreground overlays (array of 7 art IDs)
OBJ_F_OVERLAY_BACK     // Background overlays (array of 7 art IDs)
OBJ_F_UNDERLAY         // Underlays (array of 4 art IDs)
OBJ_F_BLIT_FLAGS       // Custom blending flags
OBJ_F_BLIT_COLOR       // Custom blend color
OBJ_F_BLIT_ALPHA       // Custom alpha value
OBJ_F_BLIT_SCALE       // Scale percentage (100 = normal)
OBJ_F_LIGHT_FLAGS      // Light rendering flags
OBJ_F_LIGHT_AID        // Light sprite art ID
OBJ_F_LIGHT_COLOR      // Light color (RGB packed)
OBJ_F_OVERLAY_LIGHT_FLAGS  // Per-overlay light flags
OBJ_F_OVERLAY_LIGHT_AID    // Per-overlay light art ID
OBJ_F_OVERLAY_LIGHT_COLOR  // Per-overlay light color
OBJ_F_FLAGS            // Object flags (OF_* — see §2)
OBJ_F_SPELL_FLAGS      // Spell-related flags (OSF_*)

// Transient fields (runtime-only, not serialized)
OBJ_F_RENDER_COLOR     // Cached render color
OBJ_F_RENDER_COLORS    // Per-vertex color array pointer
OBJ_F_RENDER_PALETTE   // Cached render palette
OBJ_F_RENDER_SCALE     // Cached scale
OBJ_F_RENDER_ALPHA     // Cached alpha array
OBJ_F_RENDER_X         // Cached screen X
OBJ_F_RENDER_Y         // Cached screen Y
OBJ_F_RENDER_WIDTH     // Cached render width
OBJ_F_RENDER_HEIGHT    // Cached render height
OBJ_F_PALETTE          // Base palette
OBJ_F_COLOR            // Base color
OBJ_F_COLORS           // Per-vertex colors
OBJ_F_RENDER_FLAGS     // Internal render state (ORF_*)
OBJ_F_TEMP_ID          // Temp art ID during animation
OBJ_F_LIGHT_HANDLE     // Light handle
OBJ_F_OVERLAY_LIGHT_HANDLES // Per-overlay light handles
OBJ_F_SHADOW_HANDLES   // Shadow node handles (array of up to 5)
OBJ_F_INTERNAL_FLAGS   // Internal serialization flags
OBJ_F_FIND_NODE        // Sector linked-list node
```

---

## 2. Object Flags

### 2.1 `ObjectFlags` (OF_*) — Core Rendering Flags

These are the flags stored in `OBJ_F_FLAGS` that directly affect rendering:

| Flag | Value | Rendering Effect |
|------|-------|------------------|
| `OF_DESTROYED` | `0x00000001` | Object is destroyed; in editor mode, drawn with red tint |
| `OF_OFF` | `0x00000002` | Object is deactivated; in editor mode, drawn with green tint |
| `OF_FLAT` | `0x00000004` | **Flat object** — rendered in the flat z-order group (floor-level) |
| `OF_TEXT` | `0x00000008` | Text overlay object |
| `OF_SEE_THROUGH` | `0x00000010` | Object can be seen through |
| `OF_SHOOT_THROUGH` | `0x00000020` | Projectiles pass through |
| `OF_TRANSLUCENT` | `0x00000040` | Object rendered with translucency |
| `OF_SHRUNK` | `0x00000080` | **Shrunk object** — sprite dimensions halved (÷2), source rect doubled (×2) |
| `OF_DONTDRAW` | `0x00000100` | **Object completely hidden** from rendering (checked against `dword_5E2EC8` mask) |
| `OF_INVISIBLE` | `0x00000200` | Object invisible unless `OSF_DETECTING_INVISIBLE` is active |
| `OF_NO_BLOCK` | `0x00000400` | No collision blocking |
| `OF_CLICK_THROUGH` | `0x00000800` | Cannot be hovered/interacted with |
| `OF_INVENTORY` | `0x00001000` | Object is in inventory |
| `OF_DYNAMIC` | `0x00002000` | Object moves |
| `OF_PROVIDES_COVER` | `0x00004000` | Provides cover in combat |
| `OF_HAS_OVERLAYS` | `0x00008000` | **Has overlay eye candies** — enables overlay rendering loop |
| `OF_HAS_UNDERLAYS` | `0x00010000` | **Has underlay eye candies** — enables underlay rendering loop |
| `OF_WADING` | `0x00020000` | Object is in water — adds 15px vertical offset to screen pos, lower 15px drawn with alpha=92 |
| `OF_WATER_WALKING` | `0x00040000` | Wading offset suppressed when combined with `OF_WADING` |
| `OF_STONED` | `0x00080000` | Petrified appearance |
| `OF_DONTLIGHT` | `0x00100000` | Object unaffected by lighting |
| `OF_TEXT_FLOATER` | `0x00200000` | Floating text |
| `OF_FROZEN` | `0x10000000` | **Frozen object** — rendered with additive blending + color tint |
| `OF_ANIMATED_DEAD` | `0x20000000` | Undead animation style |

### 2.2 Rendering Decision Flow in `object_draw()`

```
For each tile in sector rect:
  For each object at tile:
    1. Skip if roof covers location (unless check_faded)
    2. Skip if obj_type not in visibility table
    3. Skip if (dword_5E2F88 & flags) != 0  →  OF_DESTROYED|OF_OFF in gameplay mode
    4. Skip transitional walls when roof is faded
    5. Compute screen position = tile_xy + OFFSET_X/Y + (40, 20)
    6. Underlays (if OF_HAS_UNDERLAYS): slots [0..3], order [0..N]
    7. Overlays  (if OF_HAS_OVERLAYS): slots [6..0], field order FORE then BACK, order [700M..700M+N]
    8. Shadows (if non-flat): order [400M..400M+N]
    9. Water wading layer: consumes one flat/non-flat order slot before the main sprite
    10. Main sprite: order [flat: 200M+, non_flat: 600M+]
    11. Highlight layer (CE): order [main order + 1]
    12. Flush all pending blits, sorted by order ascending
```

### 2.3 `ObjectRenderFlags` (ORF_*) — Internal Transient Render State

```c
#define ORF_01000000  0x01000000  // Palette recomputed
#define ORF_02000000  0x02000000  // Render colors recomputed
#define ORF_04000000  0x04000000  // Shadow computed
#define ORF_08000000  0x08000000  // (unused)
#define ORF_10000000  0x10000000  // Shadow was successfully applied
#define ORF_20000000  0x20000000  // (unused)
#define ORF_40000000  0x40000000  // (unused)
#define ORF_80000000  0x80000000  // (unused)
```

`ORF_02000000 | ORF_04000000` are cleared on dirty-rect invalidation in `object_ping()`, forcing re-computation of palettes and shadows on the next draw.

### 2.4 Wall Flags (OWAF_*)

```c
#define OWAF_TRANS_DISALLOW  0x0001  // Wall blocks transparency
#define OWAF_TRANS_LEFT      0x0002  // Left-transparency wall
#define OWAF_TRANS_RIGHT     0x0004  // Right-transparency wall
#define OWAF_TRANS_ALL       0x0008  // Full transparency wall
```

Walls with `OWAF_TRANS_LEFT | OWAF_TRANS_RIGHT` are conditionally hidden when the roof at that location is faded (only for specific rotations — rot 0, 1, 6, 7).

### 2.5 Scenery Flags (OSCF_*)

```c
#define OSCF_NO_AUTO_ANIMATE    0x0001  // No automatic animation cycling
#define OSCF_BUSTED             0x0002  // Destroyed/busted state
#define OSCF_NOCTURNAL          0x0004  // Only appears at night
#define OSCF_IS_FIRE            0x0010  // Fire effect
#define OSCF_UNDER_ALL          0x0200  // Render under all objects (z-order: 100M)
#define OSCF_RESPAWNING         0x0400  // Currently respawning
```

Scenery with `OSCF_UNDER_ALL` gets a special low sort order of `100000000` (100M), placing it beneath all flat objects.

---

## 3. Object Rendering (`object.c` / `object.h`)

### 3.1 Key Structs

```c
typedef struct ObjectBlitInfo {
    /* 0000 */ TigArtBlitInfo blit_info;  // Art system blit parameters
    /* 002C */ int order;                  // Sort key (lower = drawn first)
    /* 0030 */ int rect_index;             // Index into pending_rects array
} ObjectBlitInfo;

typedef struct ObjectBlitRectInfo {
    /* 0000 */ TigRect src_rect;           // Source rectangle in sprite
    /* 0010 */ TigRect dst_rect;           // Destination rectangle on screen
} ObjectBlitRectInfo;

typedef struct ObjectRenderColors {
    int colors[160];                       // Per-vertex color table
} ObjectRenderColors;
```

### 3.2 Sort Key / Draw Order System

The rendering uses a **deferred blit queue**. All visible sprites are enqueued with an integer sort key (`order`), then the entire queue is sorted and flushed in order. The sort groups are:

| Order Range | Layer | Description |
|------------|-------|-------------|
| `0..N` | **Underlays** | Eye candy underlays (OF_HAS_UNDERLAYS). `underlay_order` starts at 0. |
| `100000000` (100M) | **OSCF_UNDER_ALL scenery** | Scenery objects with `OSCF_UNDER_ALL` flag |
| `200000000..200M+N` | **Flat objects** | Objects with `OF_FLAT` flag. `flat_order` starts at 200M. |
| `400000000..400M+N` | **Shadows** | Shadow sprites (non-flat objects only). `shadow_order` starts at 400M. |
| `600000000..600M+N` | **Non-flat objects** | Standing objects, critters, items. `non_flat_order` starts at 600M. |
| `700000000..700M+N` | **Overlays** | Eye candy overlays (OF_HAS_OVERLAYS). `overlay_order` starts at 700M. |
| `99999999` | **Hover underlay** | Pulsing underlay on hovered critter/NPC |
| `INT_MAX` | **Hover overlay** | Hover highlight overlay (always on top) |

**Within each group**, objects are drawn in the order they are encountered during the sector tile traversal (top-to-bottom, left-to-right within each sector row). Each object increments its group's counter, so objects at later tiles get higher order values.

For objects that share the **same tile**, `object_draw()` does not invent a second ordering rule at draw time. It simply walks the per-tile linked list already maintained by `sector_object_list.c::objlist_insert_internal`, which enforces these CE-specific rules:

- `OF_FLAT` objects are inserted before non-flat objects.
- `OBJ_TYPE_SCENERY` with `OSCF_UNDER_ALL` is inserted at the head of the flat segment.
- Among non-flat walls and portals, walls are inserted before portals.
- Other non-flat same-tile objects are sorted by `sub_4F2230()` / `sub_4B93F0()` results: compare the second component first, then the first component.

This linked-list ordering is critical for editor parity because CE's visible same-tile ordering is partly decided **before** `object_draw()` starts enqueuing blits.

### 3.3 Hover Highlighting

Two modes:
1. **Critter/NPC hover** (`dword_5E2E94 = true`): Animated underlay (art ID 467/555 for combat mode) + overlay (art ID 468). Color from `object_reaction_colors[]` based on NPC reaction level. Underlay at `99999999`, overlay at `INT_MAX`.
2. **Object hover** (`dword_5E2E94 = false`): Object drawn again with additive white blend `(200,200,200)` at `INT_MAX`.

### 3.4 Object Setup Blit (`object_setup_blit`)

This function configures the `TigArtBlitInfo` based on the object's flags:

1. **Frozen** (`OF_FROZEN`): Additive + color-const blend (blue/white tint)
2. **Custom BLIT_ADD**: Uses `OBJ_F_BLIT_FLAGS` additive blend
3. **Custom BLIT_MUL**: Uses `OBJ_F_BLIT_FLAGS` multiplicative blend
4. **Custom ALPHA_CONST**: Uses `OBJ_F_BLIT_ALPHA` for transparency
5. **Default**: Uses `OBJ_F_RENDER_FLAGS` (cached lighting/palette flags)
6. **Eye candy translucency**: Auto-adds `TIG_ART_BLT_BLEND_ADD` for translucent eye candy art
7. **Editor mode**: Destroyed objects → red tint; off objects → green tint

### 3.5 Scale & Shrink

- **`OBJ_F_BLIT_SCALE`**: Percentage scale (100 = normal). Affects hot-spot, width, height calculations via float multiplication.
- **`OF_SHRUNK`**: Halves all sprite dimensions. Source rectangle is doubled to compensate.
- **Eye candy scale types**: `dword_5A548C[]` = `{50, 63, 75, 87, 100, 130, 160, 200}`. When scale_type ≠ 4, the object's base scale is further multiplied by this factor.

### 3.6 Water Wading

When `OF_WADING` is set and `OF_WATER_WALKING` is not:
- Screen position Y is shifted down by 15 pixels (`sub_443620`)
- The bottom 15 pixels of the sprite are drawn separately with `alpha = 92` (≈36% opacity)
- For flat objects, the entire sprite is drawn with wading alpha

### 3.7 Dirty Rect Invalidation

The `object_ping()` function processes dirty rectangles:
1. Iterates dirty rect linked list
2. For each dirty rect, converts to location rect, then sector rect
3. For all objects in affected sectors:
   - Clears `ORF_02000000 | ORF_04000000` (forces palette/shadow recomputation)
   - Invalidates the object's bounding rectangle
4. Clears `object_dirty` flag

### 3.8 Object Type Visibility

`object_type_visibility[18]` — per-type boolean array. `object_type_toggle()` enables/disables rendering of specific object types (WALL, PORTAL, SCENERY, etc.).

### 3.9 `object_get_rect`

Computes the screen-space bounding rectangle of an object, accounting for scale, shrink, wading offset, and art frame data (hot spot, dimensions).

---

## 4. Wall Rendering (`wall.c` / `wall.h`)

### 4.1 Overview

Walls are rendered **only in top-down view** (`VIEW_TYPE_TOP_DOWN`). In isometric view, walls are just regular objects handled by `object_draw()`.

### 4.2 Functions

| Function | Address | Description |
|----------|---------|-------------|
| `wall_init` | `0x4DF390` | Initializes wall system, stores window handle |
| `wall_draw` | `0x4DF500` | Entry point — calls `sub_4E1C00` + `sub_4E1EB0` |
| `sub_4E1C00` | `0x4E1C00` | Draws wall lines in top-down view |
| `sub_4E1EB0` | `0x4E1EB0` | Draws wall corner/intersection dots |
| `wall_delete` | `0x4E18F0` | Handles wall deletion + cascading piece updates |
| `sub_4E2C50` | `0x4E2C50` | Rebuilds curved wall piece sequences |

### 4.3 Wall Piece System (P_Piece)

Walls use a `p_piece` index (0–45) embedded in the art ID to represent different wall segments:

- **Pieces 0–8**: Straight and corner segments
- **Pieces 9–20**: First set of curved walls (deleted via `sub_4E20A0`)
- **Pieces 21–33**: Second set of curved walls (deleted via `sub_4E25B0`)
- **Pieces 34–45**: Third set of curved walls (deleted via `sub_4E2C50`)

Each curved piece has `cw_size` (clockwise extent) and `ccw_size` (counter-clockwise extent). When a wall is deleted, the system walks adjacent locations in both directions, reassigning piece indices to maintain continuity.

### 4.4 Top-Down Wall Drawing

`sub_4E1C00`: For each wall object in the location rect:
- Computes rotation from art ID
- Snaps odd rotations down to even
- Draws a colored rectangle (red, 2px wide) based on rotation:
  - Rotation 0: Vertical line at tile right edge
  - Rotation 2: Horizontal line at tile bottom edge
  - Rotation 4: Vertical line at tile left edge
  - Default: Horizontal line at tile top edge

`sub_4E1EB0`: Draws intersection markers:
- Aligns to 4-tile grid boundaries
- Draws magenta (255,0,100) dots at grid intersections
- Dot size = `zoom / 4`, clamped to `[4, zoom]`

### 4.5 Transitional Walls

Walls with `OWAF_TRANS_LEFT | OWAF_TRANS_RIGHT` are special:
- In `object_draw()`, they are conditionally skipped when the roof at that location is faded
- Only skipped for rotations 0, 1, 6, 7 (checked via `object_render_check_rotation`)

---

## 5. Facade System (`facade.c` / `facade.h`)

### 5.1 Overview

The facade system renders building facades — the wall textures visible on the exterior of buildings. It uses art IDs loaded from walkmask data.

### 5.2 Key State

```c
static int dword_5FF570;                    // Facade height (rows)
static int dword_5FF574;                    // Facade width (columns)
static int64_t qword_5FF578;               // Origin location (top-left corner)
static tig_art_id_t* dword_5FF5A0;         // Art ID array [width × height]
static TigVideoBuffer** dword_5FF5A4;      // Video buffer array (top-down mode only)
static int dword_5FF588;                    // Sector/area ID
```

### 5.3 Initialization & Loading

`facade_init()` stores window handle and invalidation function.

`sub_4CA0F0()` (the actual loader):
1. Calls `walkmask_load()` to get facade art IDs, width, and height
2. Centers the facade around the given location: `origin = (loc_x - width/2, loc_y - height/2)`
3. In top-down mode: creates `TigVideoBuffer` for each non-invalid tile

`sub_4CA240()` (cleanup):
- Destroys all video buffers
- Frees art ID array

### 5.4 Rendering

`facade_draw()`:
1. Clips `loc_rect` to facade bounds via `sub_4CA6B0()`
2. Iterates tiles in the visible rect
3. For each tile with a valid art ID:
   - Computes screen position via `location_xy()`
   - In isometric mode: offsets X by +1
   - In isometric mode: blits art via `tig_window_blit_art()`
   - In top-down mode: copies from pre-rendered video buffer via `tig_window_copy_from_vbuffer()`

### 5.5 Coordinate System

The facade uses a flat 2D grid stored in `dword_5FF5A0[]` indexed by `[row * width + col]`. The origin location (`qword_5FF578`) maps the grid to world coordinates.

---

## 6. Roof Rendering (`roof.c` / `roof.h`)

### 6.1 Roof Art ID Encoding

Roof art IDs encode:
- **Piece type** (4 bits): Which roof piece shape (13 types)
- **Fill bit**: Whether this is a "fill" piece (empty space under roof)
- **Fade bit**: Whether this piece should fade (player is underneath)

### 6.2 Roof Piece Types

13 piece types, each with a 4×4 occupancy grid (`byte_5A53A4`):

| Piece | Name | Description |
|-------|------|-------------|
| 0 | `NORTH_WEST_OUTSIDE` | Corner, NW outside |
| 1 | `WEST` | West edge |
| 2 | `NORTH` | North edge |
| 3 | `NORTH_WEST_INSIDE` | Corner, NW inside |
| 4 | `SOUTH_WEST_OUTSIDE` | Corner, SW outside |
| 5 | `SOUTH_WEST_INSIDE` | Corner, SW inside |
| 6 | `NORTH_EAST_INSIDE` | Corner, NE inside |
| 7 | `NORTH_EAST_OUTSIDE` | Corner, NE outside |
| 8 | `CENTER` | Full center piece (all 16 cells filled) |
| 9 | `SOUTH_EAST_OUTSIDE` | Corner, SE outside |
| 10 | `SOUTH` | South edge |
| 11 | `EAST` | East edge |
| 12 | `SOUTH_EAST_INSIDE` | Corner, SE inside |

### 6.3 Fade System

Roofs use a gradient fade system loaded from `art\roof\roofshade.mes`:

```c
static uint8_t roof_full_opacity;       // Entry 0: Full opacity value
static uint8_t roof_partial_opacity;    // Entry 1: Partial opacity value
static uint8_t roof_full_transparency;  // Entry 2: Full transparency value
```

When a roof piece has the fade bit set (`tig_art_roof_id_fade_get(aid)`), each corner of the 2×2 quad gets different alpha values based on the piece type. The blit uses `TIG_ART_BLT_BLEND_ALPHA_LERP_BOTH` for bilinear interpolation between the 4 corner alpha values.

Example fade assignments:
- **CENTER**: All 4 corners = `roof_full_opacity` (fully opaque)
- **NORTH_WEST_OUTSIDE**: `[full_trans, partial, partial, full_trans]` (corners fade away)
- **WEST**: `[partial, full, full, partial]` (west edge fades)
- **SOUTH**: `[full, partial, full_trans, partial]` (south edge fades)

### 6.4 Roof Coverage Check

`roof_is_covered_loc()` determines if a location is under a non-faded roof:
1. Gets the roof art ID at `(loc_x + 3, loc_y + 3)` (offset to 4-tile grid center)
2. Rejects fill pieces
3. Looks up the 4×4 occupancy grid for the piece type
4. Checks if the specific tile within the 4×4 grid is occupied
5. If `check_faded` is false, also rejects faded pieces

### 6.5 Fill System

Roof pieces can be "filled" — meaning the space underneath is empty (no interior). The `roof_fill()` function propagates fill state to adjacent roof pieces via recursive 4-directional calls with directional blocking:
- Direction 1 (west): Blocks NW_OUTSIDE, WEST, SW_OUTSIDE
- Direction 3 (north): Blocks NW_OUTSIDE, NORTH, NE_OUTSIDE
- Direction 5 (east): Blocks SE_OUTSIDE, EAST, NE_OUTSIDE
- Direction 7 (south): Blocks SW_OUTSIDE, SOUTH, SE_OUTSIDE

### 6.6 Roof Recalculation

`roof_recalc()` recalculates fill state based on piece type and tile position within the 4×4 grid:
- Uses `tile_id_from_loc()` to get tile coordinates
- Computes `v5 = tile & 3` and `v6 = (tile >> 6) % 4` for position within the 4×4 grid
- Each piece type has different fill logic based on these sub-coordinates

### 6.7 Rendering

`roof_draw()`:
1. Only renders in isometric view when `roof_enabled` is true
2. Iterates locations in steps of 4 (matching the 4×4 tile grid)
3. Skips fill pieces
4. Computes screen position via `roof_xy()` (offsets by -120, -200 from tile center)
5. For faded pieces with hardware acceleration: uses `TIG_ART_BLT_BLEND_ALPHA_LERP_BOTH` with per-corner alpha
6. For non-faded or software rendering: uses `TIG_ART_BLT_BLEND_ALPHA_CONST`
7. Uses scratch video buffer for rendering

### 6.8 Roof Hit Testing

`roof_hit_test()`:
1. Converts screen (x, y) to location (offset by +120 in y)
2. Gets roof art ID, rejects fill pieces
3. Checks pixel-level collision via `sub_502FD0()`

---

## 7. Shadow System (`light.c` / `light.h`)

### 7.1 Shadow Struct

```c
#define SHADOW_HANDLE_MAX 5

typedef struct Shadow {
    /* 0000 */ tig_art_id_t art_id;       // Shadow sprite art ID
    /* 0004 */ TigPalette* palette;        // Shadow palette
    /* 0008 */ tig_color_t color;          // Shadow color (typically dark gray)
    /* 000C */ struct Shadow* next;        // Linked list pointer
} Shadow;
```

### 7.2 Shadow Application

`shadow_apply()` computes shadows for non-flat objects:
1. Reads `OBJ_F_SHADOW` base shadow art ID
2. Creates shadow sprites from multiple light sources
3. Sorts shadows by some criteria (bubble sort in the code)
4. Stores up to `SHADOW_HANDLE_MAX - 1` shadows in `OBJ_F_SHADOW_HANDLES` array
5. Shadow color: `tig_color_make(gray * 0.4, gray * 0.4, gray * 0.4)` — 40% of light gray
6. Sets `ORF_10000000` flag to indicate shadows were successfully applied

### 7.3 Shadow Rendering in `object_draw()`

Shadows are rendered in the `shadow_order` group (400M+), between flat and non-flat objects:
- Only for non-flat objects (flat objects have no shadow)
- Rendered with `TIG_ART_BLT_BLEND_COLOR_CONST | TIG_ART_BLT_BLEND_SUB` (subtractive blend)
- Each shadow art ID is rendered at the object's position with the shadow's color
- Wading objects get shadow color multiplied by `(92, 92, 92)` (darker)

---

## 8. CE-Specific Fixes & Modifications

### 8.1 Underlay Shrink Fix (object.c:586)

**Problem**: Reaction underlays on shrunk critters were incorrectly scaled, creating a mismatch with the hover underlay.

**Fix**: When rendering underlays, the `OF_SHRUNK` flag is masked off:
```c
sub_443620(obj_flags & ~OF_SHRUNK, 100, (int)loc_x, (int)loc_y, art_id, &eye_candy_rect);
```
This makes underlays always render at 100% scale regardless of the object's shrunk state.

### 8.2 Ghost Overlay Z-Order Fix (object.c:695)

**Problem**: Ghost overlays (art ID 243) on dead non-decaying NPCs were rendered in the overlay group (700M+), causing them to appear above walls and roofs.

**Fix**: Ghost overlays are moved to the non-flat object group (600M+):
```c
if (obj_type == OBJ_TYPE_ARMOR
    || (obj_type == OBJ_TYPE_NPC
        && critter_is_dead(obj_node->obj)
        && tig_art_num_get(art_id) == 243)) {
    order = non_flat_order++;
} else {
    order = overlay_order++;
}
```

### 8.3 Highlight Mode (object.c)

**New feature**: `object_highlight_mode` flag enables highlighting all interactive objects:
- Non-wall, non-click-through objects are drawn again with additive white blend
- Highlighted sprites get `order + 1` (one level above the normal sprite)
- This prevents leakage behind walls/roofs that would occur with `INT_MAX` ordering

### 8.4 Scaled Sprite Dirty Rect Fix (object.c:856)

**Problem**: Dirty rectangles and scaling don't play well together. When a scaled sprite intersects a dirty rectangle, the source rectangle calculation introduces rounding errors, causing visual artifacts (especially visible when hovering over dead bodies or during movement).

**Fix**: For scaled sprites (`scale != 100`), bypass dirty rectangles entirely:
- Blit the entire scaled sprite (even outside the dirty rect)
- Mark the entire object rectangle as dirty for the next frame
- This trades potential one-frame artifacts for consistent rendering

### 8.5 Data Type Fix (obj.c)

**Change**: `data` and `transient_properties` fields in `Object` changed from `int*`/`int[]` to `intptr_t*`/`intptr_t[]`. This is required for 64-bit correctness since many fields store pointers (render colors, palettes, shadow handles).

---

## Appendix A: Object Type Masks

```c
#define OBJ_TM_WALL        0x00000001
#define OBJ_TM_PORTAL      0x00000002
#define OBJ_TM_CONTAINER   0x00000004
#define OBJ_TM_SCENERY     0x00000008
#define OBJ_TM_PROJECTILE  0x00000010
#define OBJ_TM_WEAPON      0x00000020
#define OBJ_TM_AMMO        0x00000040
#define OBJ_TM_ARMOR       0x00000080
#define OBJ_TM_GOLD        0x00000100
#define OBJ_TM_FOOD        0x00000200
#define OBJ_TM_SCROLL      0x00000400
#define OBJ_TM_KEY         0x00000800
#define OBJ_TM_KEY_RING    0x00001000
#define OBJ_TM_WRITTEN     0x00002000
#define OBJ_TM_GENERIC     0x00004000
#define OBJ_TM_PC          0x00008000
#define OBJ_TM_NPC         0x00010000
#define OBJ_TM_TRAP        0x00020000

#define OBJ_TM_ITEM   (WEAPON|AMMO|ARMOR|GOLD|FOOD|SCROLL|KEY|KEY_RING|WRITTEN|GENERIC)
#define OBJ_TM_CRITTER (PC|NPC)
#define OBJ_TM_ALL    (WALL|PORTAL|CONTAINER|SCENERY|PROJECTILE|ITEM|PC|NPC|TRAP)
```

## Appendix B: Eye Candy Scale Table

```c
static int dword_5A548C[8] = {
    50,   // scale_type 0 → 50%
    63,   // scale_type 1 → 63%
    75,   // scale_type 2 → 75%
    87,   // scale_type 3 → 87%
    100,  // scale_type 4 → 100% (no change)
    130,  // scale_type 5 → 130%
    160,  // scale_type 6 → 160%
    200,  // scale_type 7 → 200%
};
```

---

## 9. Coordinate System

### 9.1 Location Encoding

A world location is a packed `int64_t`:

```c
#define LOCATION_GET_X(l) ((l) & 0xFFFFFFFF)
#define LOCATION_GET_Y(l) (((l) >> 32) & 0xFFFFFFFF)
#define LOCATION_MAKE(x, y) ((x) | ((y) << 32))
```

- X in bits 0–31, Y in bits 32–63
- Both are signed 32-bit integers
- Location limits: up to `0x100000000` (4,294,967,296) per axis

### 9.2 Sector Encoding

```c
#define SECTOR_X(a) ((a) & 0x3FFFFFF)
#define SECTOR_Y(a) (((a) >> 26) & 0x3FFFFFF)
#define SECTOR_MAKE(a, b) ((a) | ((b) << 26))
```

Each sector spans 64×64 tiles. Sector-local tile index:

```c
#define TILE_X(tile) ((tile) & 0x3F)
#define TILE_Y(tile) (((tile) >> 6) & 0x3F)
#define TILE_MAKE(x, y) ((x) | ((y) << 6))
```

### 9.3 Isometric Projection (`location_xy`)

```c
void location_xy(int64_t loc, int64_t* sx, int64_t* sy) {
    if (isometric) {
        *sx = origin_x + 40 * (loc_y - loc_x - 1);
        *sy = origin_y + 20 * (loc_y + loc_x);
    } else {
        *sx = origin_x - zoom * loc_x;
        *sy = origin_y + zoom * loc_y;
    }
}
```

**Tile diamond:** 80 px wide × 40 px tall.

### 9.4 Inverse Projection (`location_at`)

```c
// Isometric screen → world:
dx = (sx - origin_x) / 2;
dy = sy - origin_y;
world_x = (dy - dx) / 40;
world_y = (dy + dx) / 40;
```

### 9.5 Object Anchor Offset

```c
location_xy(loc, &loc_x, &loc_y);
loc_x += OBJ_F_OFFSET_X;
loc_y += OBJ_F_OFFSET_Y;
loc_x += 40;   // tile center X (half of 80)
loc_y += 20;   // tile center Y (half of 40)
```

### 9.6 TileOrder Computation (`sub_4B93F0`)

Computes the sort order components for objects within a tile:

```c
void sub_4B93F0(int offset_x, int offset_y, int* horizontal, int* vertical) {
    int v1 = (offset_x - 40) / 2;
    int v2 = 2 * (offset_y / 2);
    *horizontal = v2 - v1;      // Primary sort component
    *vertical   = v1 + v2;      // Secondary sort component
}
```

### 9.7 Location Normalization (`location_normalize`)

Snaps a location+offset pair to the nearest tile:

```c
bool location_normalize(int64_t* loc, int* offset_x, int* offset_y) {
    // 1. Compute absolute screen position: xy + offset + (40, 20)
    // 2. Convert back to location via location_at()
    // 3. Compute new offset as remainder
    // 4. Return false if location didn't change
}
```

---

## 10. Main Draw Loop

### 10.1 GameDrawInfo Structure

The host window's redraw cycle populates a `GameDrawInfo`:

```c
typedef struct GameDrawInfo {
    TigRect* screen_rect;       // Screen-space clipping rect
    LocRect* loc_rect;          // World-coordinate bounds
    SectorRect* sector_rect;    // Sector grid for tile iteration
    SectorListNode* sectors;    // Linked list of visible sectors
    TigRectListNode** rects;    // Linked list of dirty rects
} GameDrawInfo;
```

### 10.2 Module Draw Order

The main game and the editor use different fixed draw sequences in `gamelib.c`.

**Game mode (`gamelib_draw_game`)**

```
 1. light_draw()
 2. tile_draw()
 3. sub_43C690()
 4. object_draw()
 5. roof_draw()
 6. tb_draw()
 7. tf_draw()
 8. tc_draw()
```

**Editor mode (`gamelib_draw_editor`)**

```
 1. light_draw()
 2. tile_draw()
 3. facade_draw()
 4. jumppoint_draw()
 5. tile_script_draw()
 6. tileblock_draw()
 7. object_draw()
 8. sector_draw()
 9. wall_draw()
10. wp_draw()
11. roof_draw()
12. tb_draw()
```

For the parity work in `ArcanumEditor`, the stages that matter most are `tile_draw()`, `facade_draw()`, `object_draw()`, and `roof_draw()`. In isometric view, walls are still drawn as part of `object_draw()`, not `wall_draw()`.

### 10.3 Dirty Rect System

Each module clips its drawing against the dirty rect list (`TigRectListNode`). Only the intersection of each item's bounding rect with the dirty rects is actually blitted to the screen. The `object_ping()` function processes dirty rects between frames to invalidate cached render state.

### 10.4 ViewOptions

```c
typedef struct ViewOptions {
    int type;   // VIEW_TYPE_ISOMETRIC (0) or VIEW_TYPE_TOP_DOWN (1)
    int zoom;   // Zoom level (top-down only, 12–64)
} ViewOptions;
```

---

## 11. Tile Drawing

**File:** `src/game/tile.c`

### 11.1 Isometric Tile Drawing (`tile_draw_iso`)

```
For each tile in loc_rect (row-major):
  1. Get tile art_id from sector->tiles.art_ids[TILE_MAKE(tile_x & 0x3F, tile_y & 0x3F)]
  2. Compute screen position via location_xy()
  3. Create 80×40 destination rect
  4. Clip against dirty rects
  5. Blit art to window via tig_window_blit_art()
```

### 11.2 Top-Down Tile Drawing (`tile_draw_topdown`)

Uses a tile cache (`TileCacheEntry[64]`) with scaled art:

```
For each tile in loc_rect:
  1. Get tile art_id
  2. Look up or create cached video buffer at zoom×zoom pixels
  3. Scaling uses precomputed mapping tables (dword_602DE4/dword_602DE8)
     with sub-pixel sampling and perspective correction
  4. Copy from cache to window
```

Scale factors vary by zoom level (0.5–0.94).

### 11.3 Tile Art ID Format

Tile art IDs encode:
- **num1/num2**: Tile number pair for terrain blending
- **type**: Indoor (0) / Outdoor (1)
- **flippable1/flippable2**: Whether tile edges are flippable
- **variation**: Random variation (0–15)
- **palette**: Palette index

### 11.4 Tile Properties

```c
bool tile_is_blocking(int64_t loc, bool check_facade);
bool tile_is_soundproof(int64_t loc);
bool tile_is_sinkable(int64_t loc);
bool tile_is_slippery(int64_t loc);
```

Tiles of type `TIG_ART_TYPE_FACADE` are walkable based on `tig_art_facade_id_walkable_get()`.

---

## 12. ART System

**File:** `first_party/tig/src/art.c`, `first_party/tig/include/tig/art.h`

### 12.1 Art Types

```c
typedef enum TigArtType {
    TIG_ART_TYPE_TILE,        // 0 — Floor tiles
    TIG_ART_TYPE_WALL,        // 1 — Wall art
    TIG_ART_TYPE_CRITTER,     // 2 — Player/NPC art
    TIG_ART_TYPE_PORTAL,      // 3 — Door/portal art
    TIG_ART_TYPE_SCENERY,     // 4 — Scenery objects
    TIG_ART_TYPE_INTERFACE,   // 5 — UI elements
    TIG_ART_TYPE_ITEM,        // 6 — Item art
    TIG_ART_TYPE_CONTAINER,   // 7 — Container art
    TIG_ART_TYPE_MISC,        // 8 — Miscellaneous
    TIG_ART_TYPE_LIGHT,       // 9 — Light masks
    TIG_ART_TYPE_ROOF,        // 10 — Roof tiles
    TIG_ART_TYPE_FACADE,      // 11 — Facade art
    TIG_ART_TYPE_MONSTER,     // 12 — Monster art
    TIG_ART_TYPE_UNIQUE_NPC,  // 13 — Unique NPC art
    TIG_ART_TYPE_EYE_CANDY,   // 14 — Eye candy effects
} TigArtType;
```

### 12.2 Animation Data

```c
typedef struct TigArtAnimData {
    TigArtAnimFlags flags;
    int fps;
    int bpp;             // Bits per pixel
    int action_frame;
    int num_frames;
    unsigned int color_key;   // Transparency color key
    TigPalette* palette1;
    TigPalette* palette2;
} TigArtAnimData;
```

### 12.3 Frame Data

```c
typedef struct TigArtFrameData {
    int width;      // Frame width in pixels
    int height;     // Frame height in pixels
    int hot_x;      // Hotspot X (center/anchor X)
    int hot_y;      // Hotspot Y (center/anchor Y)
    int offset_x;   // Render offset X
    int offset_y;   // Render offset Y
} TigArtFrameData;
```

### 12.4 Art ID Bit Layout

The 32-bit art ID encodes type in bits 28–31. Additional fields are type-specific:

| Art Type | Key Fields |
|----------|-----------|
| Tile | num1, num2, flippable, type, palette |
| Wall | num, p_piece (6 bits), variation, rotation (3 bits), palette, damaged |
| Critter | gender, body_type, armor, shield, frame, rotation (3 bits), anim, weapon, palette |
| Roof | piece (4 bits), fade (bit 12), fill (bit 13), mirror (bit 0) |
| EyeCandy | translucency flag |
| Scenery | num, type, frame, rotation, palette |

### 12.5 Roof Art ID Details

```c
// Roof piece index
int roof_piece = tig_art_roof_id_piece_get(art_id);  // 0–12
bool is_faded = tig_art_roof_id_fade_get(art_id);     // bit 12
bool is_fill  = tig_art_roof_id_fill_get(art_id);     // bit 13
// Mirror flag (bit 0): adds 9 to piece index for alpha lookup
int roof_piece_index = frame_index + ((art_id & 1) ? 9 : 0);
```

### 12.6 Blit Flags

Full set of blend modes:

| Flag | Value | Description |
|------|-------|-------------|
| `TIG_ART_BLT_FLIP_X` | 0x01 | Horizontal flip |
| `TIG_ART_BLT_FLIP_Y` | 0x02 | Vertical flip |
| `TIG_ART_BLT_PALETTE_ORIGINAL` | 0x04 | Force base art palette |
| `TIG_ART_BLT_PALETTE_OVERRIDE` | 0x08 | Force custom palette |
| `TIG_ART_BLT_BLEND_ADD` | 0x10 | Additive blend |
| `TIG_ART_BLT_BLEND_SUB` | 0x20 | Subtractive blend |
| `TIG_ART_BLT_BLEND_MUL` | 0x40 | Multiplicative blend |
| `TIG_ART_BLT_BLEND_ALPHA_AVG` | 0x80 | Average alpha |
| `TIG_ART_BLT_BLEND_ALPHA_CONST` | 0x100 | Constant alpha |
| `TIG_ART_BLT_BLEND_ALPHA_SRC` | 0x200 | Source alpha |
| `TIG_ART_BLT_BLEND_ALPHA_LERP_X` | 0x400 | Horizontal linear alpha |
| `TIG_ART_BLT_BLEND_ALPHA_LERP_Y` | 0x800 | Vertical linear alpha |
| `TIG_ART_BLT_BLEND_ALPHA_LERP_BOTH` | 0x1000 | Bilinear alpha (4 corners) |
| `TIG_ART_BLT_BLEND_COLOR_CONST` | 0x2000 | Constant color tint |
| `TIG_ART_BLT_BLEND_COLOR_ARRAY` | 0x4000 | Per-vertex color array |
| `TIG_ART_BLT_BLEND_ALPHA_STIPPLE_S` | 0x8000 | Source stipple dither |
| `TIG_ART_BLT_BLEND_ALPHA_STIPPLE_D` | 0x10000 | Destination stipple dither |
| `TIG_ART_BLT_BLEND_COLOR_LERP` | 0x20000 | Linear color interpolation |
| `TIG_ART_BLT_SCRATCH_VALID` | 0x1000000 | Scratch buffer available |

### 12.7 Blit Info Structure

```c
typedef struct TigArtBlitInfo {
    TigArtBlitFlags flags;
    tig_art_id_t art_id;
    TigRect* src_rect;
    TigPalette* palette;
    tig_color_t color;             // For COLOR_CONST blend
    uint32_t* field_14;            // Color array pointer
    TigRect* field_18;             // Color lerp rect
    uint8_t alpha[4];              // Per-corner alpha [TL, TR, BR, BL]
    TigVideoBuffer* dst_video_buffer;
    TigRect* dst_rect;
    TigVideoBuffer* scratch_video_buffer;
} TigArtBlitInfo;
```

---

## 13. Sector Subsystems

### 13.1 Sector Structure (Full Layout)

```c
typedef struct Sector {
    /* 0x0000 */ SectorFlags flags;
    /* 0x0008 */ int64_t id;
    /* 0x0010 */ DateTime datetime;
    /* 0x0018 */ SectorLightList lights;        // Linked list of Light*
    /* 0x0020 */ SectorTileList tiles;          // 4096 tile art IDs + diff mask
    /* 0x4224 */ SectorRoofList roofs;          // 256 roof art IDs
    /* 0x4628 */ TileScriptList tile_scripts;   // Per-tile script refs
    /* 0x4630 */ SectorScriptList sector_scripts;
    /* 0x4640 */ int townmap_info;
    /* 0x4644 */ int aptitude_adj;
    /* 0x4648 */ int light_scheme;
    /* 0x464C */ SectorSoundList sounds;
    /* 0x4658 */ SectorBlockList blocks;        // 4096-bit blocked tile mask
    /* 0x485C */ SectorObjectList objects;      // 4096 linked list heads
} Sector;
```

### 13.2 Tile List

```c
typedef struct SectorTileList {
    tig_art_id_t art_ids[4096];   // 64×64 row-major
    uint32_t difmask[128];        // 4096-bit dirty mask (128 × uint32)
    int dif;                      // Any-dirty flag
} SectorTileList;
```

### 13.3 Roof List

```c
typedef struct SectorRoofList {
    tig_art_id_t art_ids[256];    // 16×16 row-major (one per 4×4 tile block)
    int empty;                    // No-roofs flag
} SectorRoofList;
```

### 13.4 Object List

```c
typedef struct SectorObjectList {
    ObjectNode* heads[4096];      // Per-tile linked list heads
} SectorObjectList;
```

Each `ObjectNode`:
```c
typedef struct ObjectNode {
    int64_t obj;                  // Object handle
    struct ObjectNode* next;
} ObjectNode;
```

### 13.5 Block List

```c
typedef struct SectorBlockList {
    uint32_t blocked[128];        // 4096-bit blocked tile mask
} SectorBlockList;
```

Bit test: `blocked[tile_id / 32] & (1 << (tile_id % 32))`

### 13.6 Light List

```c
typedef struct SectorLightList {
    Light* head;                  // Linked list of lights in this sector
} SectorLightList;
```

### 13.7 Sector Cache

LRU cache with default capacity 16 (range 8–128):

```c
typedef struct SectorCacheEntry {
    bool used;
    int refcount;
    unsigned int timestamp;
    int field_C;
    Sector sector;
} SectorCacheEntry;
```

Sectors are locked (`sector_lock`) before reading and unlocked (`sector_unlock`) after. The cache evicts least-recently-used sectors when full.

---

## 14. Lighting System

**File:** `src/game/light.c`

### 14.1 Light Data

```c
typedef struct LightSerializedData {
    int64_t obj;              // Associated object handle (0 for standalone)
    int64_t loc;              // World location
    int offset_x;             // Screen offset X
    int offset_y;             // Screen offset Y
    unsigned int flags;       // LF_* flags
    tig_art_id_t art_id;      // Light mask art ID
    uint8_t r, b, g;          // RGB color (NOTE: b before g in serialization!)
    tig_color_t tint_color;   // Packed tint color
    int palette;              // Always 0
    int padding_2C;
} LightSerializedData;        // 0x30 bytes
```

### 14.2 Light Rendering

`light_draw()` → `light_render_internal()`:

1. Computes viewport center location
2. Computes light buffer origin from center
3. Locks lighter/darker video buffers
4. For each visible sector:
   - For each light in sector:
     - Computes light screen rect from art frame data
     - Clips to dirty rects
     - Blits light mask art with color tint to lighter/darker buffers
5. Composites light buffers onto the main window

### 14.3 Light Sampling (`sub_4D89E0`)

Samples accumulated light color at a screen position:

1. Builds a 1536×1024 search rect around position
2. Converts to location rect, creates sector list
3. For each sector, for each light:
   - Checks if position is within light's bounding rect
   - Samples light mask art pixel at the position
   - If pixel is not color key: accumulates R, G, B components
4. Returns blended light color
5. Also sets indoor/outdoor ambient color based on tile type

### 14.4 Indoor/Outdoor Palettes

```c
static tig_color_t light_outdoor_color;   // Default: white (255,255,255)
static tig_color_t light_indoor_color;    // Default: white (255,255,255)
```

Switched per-tile via `tig_art_tile_id_type_get()`.

### 14.5 Light Animation

Lights with multi-frame art IDs can animate via time events:

```c
void light_start_animating(Light* light) {
    // If light has >1 frame and LF_ANIMATING not set:
    // Schedule TIMEEVENT_TYPE_LIGHT at 1000/fps ms interval
    // Set LF_ANIMATING flag
}
```

### 14.6 Light Buffers

Two video buffers for compositing:

```c
static TigVideoBuffer* lighter_vb;   // Additive light pass
static TigVideoBuffer* darker_vb;    // Darker pass
```

Hardware-accelerated mode uses `TIG_ART_BLT_BLEND_COLOR_CONST`. Software fallback uses palette-based light/dark lookups.

### 14.7 Ambient Palettes

`light_ambient_palettes_init()` builds indoor and outdoor palette tables based on the current light scheme and indoor/outdoor colors. These are applied to tile and object art during rendering.

---

## 15. Editor vs. Game Mode Differences

### 15.1 Initialization

All modules receive `GameInitInfo.editor` flag:

```c
// Editor mode: show all objects, no visibility filtering
dword_5E2F88 = 0;           // No skip flags (no OF_DESTROYED|OF_OFF filtering)
dword_5E2EC8 = 0;           // No dontdraw filter

// Game mode: filter destroyed/off/dontdraw
dword_5E2F88 = OF_DESTROYED | OF_OFF;
dword_5E2EC8 = OF_DONTDRAW;
```

### 15.2 Sector Loading

- **Editor:** `sector_load_editor()` — loads raw `.sec` sector files directly
- **Game:** `sector_load_game()` — loads base sector + applies save difference files

### 15.3 Wall Drawing

- **Editor:** `wall_draw()` may render in both views
- **Game:** `wall_draw()` only renders in top-down view

### 15.4 Object Rendering in Editor

In editor mode, destroyed objects are drawn with a red tint, off objects with a green tint:

```c
if (object_editor) {
    if (flags & OF_DESTROYED) {
        // Apply red color tint
    } else if (flags & OF_OFF) {
        // Apply green color tint
    }
}
```

---

## 16. Key Constants

| Constant | Value | Description |
|----------|-------|-------------|
| Tile width (isometric) | 80 px | Diamond width |
| Tile height (isometric) | 40 px | Diamond height |
| Sector size | 64×64 tiles | Per-sector grid |
| Roof cell size | 4×4 tiles | Per-roof-cell footprint |
| Roof cells per sector | 16×16 (256) | Roof grid resolution |
| Object X center offset | +40 px | From tile top-left to center |
| Object Y center offset | +20 px | From tile top-left to center |
| Roof X sprite offset | -120 px | From normalized center |
| Roof Y sprite offset | -200 px | From normalized center |
| Underlay sort order | 0 | Integer sort key base |
| Flat sort order | 200,000,000 | Flat-object integer sort key base |
| OSCF_UNDER_ALL sort | 100,000,000 | Scenery under everything |
| Shadow sort order | 400,000,000 | Integer sort key base |
| Non-flat sort order | 600,000,000 | Standing objects |
| Overlay sort order | 700,000,000 | Eye candy overlays |
| Hover underlay order | 99,999,999 | Pulsing hover effect |
| Hover overlay order | INT_MAX | Always on top |
| Object blit scale default | 100 | 100% = no scaling |
| Object scale table | [50,63,75,87,100,130,160,200] | 8 discrete scale levels |
| Max text bubbles | 8 | Simultaneous |
| Text bubble size | 200×200 px | Video buffer |
| Sector cache capacity | 16 (default) | LRU cache (8–128 range) |
| Tile cache capacity | 64 | Top-down mode |
| Roof shade MES path | `art\roof\roofshade.mes` | Fade opacity values |
| Wall transparency flag | `OWAF_TRANS_LEFT \| RIGHT` | Under faded roofs |
| Wading vertical offset | 15 px | Downward shift |
| Wading alpha | 92 | ~36% opacity |
| Extended blit border | ±256 px | Content rect extension |
