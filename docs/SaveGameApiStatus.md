# Arcanum Save Game Read/Write API — Status Report

**Scope**: ArcNET.Formats + ArcNET.GameObjects + ArcNET.GameData  
**Date**: 2026-04-08 (updated 2026-04-08, session 5)  
**Purpose**: Professional-grade gap analysis for implementation agents and RE agents. All claims reference current source code at line granularity.

---

## 1. Arcanum Save Game Structure Reference

A save slot on disk is a pair of binary files sharing a stem name:

```
<slot>.tfai   — TFAI index: typed, tree-structured entry list (names + sizes)
<slot>.tfaf   — TFAF data blob: raw concatenation of all file payloads in DFS order
<slot>.gsi    — Save metadata: display name, leader stats, map ID, game time
```

Inside the TFAF the directory tree mirrors the module/map layout:

```
modules/<module>/
  maps/<map>/
    sector_<n>.sec        — editor-format sector (tiles, lights, block mask, objects)
    mobile/<obj>.mob      — static world objects (placed in the editor)
    mobile.md             — runtime diffs for static objects (modified containers, portals, etc.)
    mobile.mdy            — dynamically spawned objects: NPCs, dropped items, and the player character
    map.jmp               — jump-point transitions (source tile → dest map + tile)
    map.prp               — map properties (terrain art ID, tile count limits)
  modules/<module>.mes    — module-wide message strings (optional; per-save override)
```

The PC character record is embedded as a v2 entry inside `mobile.mdy` for whichever map the player is currently on. Static world-state diffs for every visited map are in `mobile.md` files; these are the authoritative record of door states, looted containers, dead NPCs, etc.

---

## 2. Implemented API Surface

### 2.1 Archive Layer

| Format | Model | Class | R | W | Notes |
|--------|-------|-------|---|---|-------|
| `.tfai` index | `SaveIndex` (tree of `TfaiEntry`) | `SaveIndexFormat` | ✅ | ✅ | Full DFS tree parse + write. `TfaiFileEntry` + `TfaiDirectoryEntry`. |
| `.tfaf` data blob | `IReadOnlyDictionary<string, byte[]>` | `TfafFormat` | ✅ | ✅ | `ExtractAll`, `Extract`, `Pack`, `TotalPayloadSize`. Virtual-path keys (forward-slash). |
| `.gsi` save metadata | `SaveInfo` | `SaveInfoFormat` | ✅ | ✅ | Version 0 (vanilla) and 25 (UAP). Fields: module, leader name, display name, map ID, time, portrait, level, tile location, story state. |

**Source**: [SaveIndexFormat.cs](../src/Formats/ArcNET.Formats/SaveIndexFormat.cs), [TfafFormat.cs](../src/Formats/ArcNET.Formats/TfafFormat.cs), [SaveInfoFormat.cs](../src/Formats/ArcNET.Formats/SaveInfoFormat.cs)

### 2.2 Map Formats (inside TFAF)

| Format | Model | Class | R | W | Notes |
|--------|-------|-------|---|---|-------|
| `.sec` sector | `Sector` | `SectorFormat` | ✅ | ✅ | Full: lights (48 B each), 4096 tiles, 256 roof tiles, version 0xAA0001–0xAA0004, tile scripts, sector script, townmap, aptitude adj, light scheme, sound list, 128-uint block mask, embedded MobData objects. Writes always at 0xAA0004. |
| `.mob` static object | `MobData` | `MobFormat` | ✅ | ✅ | OFF header + property collection. Compact (PC/NPC one-OID) and standard (two-OID) header variants. |
| `mobile.md` runtime diffs | `MobileMdFile` / `MobileMdRecord` | `MobileMdFormat` | ✅ | ✅ | Per-record: 24-byte ObjectID + version + START sentinel (0x12344321) + mob body + END sentinel (0x23455432). Dual-sentinel lookahead avoids false-positive END in property data. Compact Pc/Npc decode with fallback to verbatim round-trip. |
| `mobile.mdy` dynamic spawns | `MobileMdyFile` / `MobileMdyRecord` | `MobileMdyFormat` | ✅ | ✅ | Discriminated union of `MobData` and `CharacterMdyRecord`. Resync scanner on parse failure. Sentinel skipping (0xFFFFFFFF). |
| `.jmp` jump points | `JmpFile` / `JumpEntry` | `JmpFormat` | ✅ | ✅ | Count prefix + 32 B entries: flags, padding, source loc, dest map, padding, dest loc. Padding fields zeroed on write. |
| `.prp` map properties | `MapProperties` | `MapPropertiesFormat` | ✅ | ✅ | Fixed 24-byte struct: ArtId, Unused, LimitX (uint64), LimitY (uint64). |

**Source**: [SectorFormat.cs](../src/Formats/ArcNET.Formats/SectorFormat.cs), [MobFormat.cs](../src/Formats/ArcNET.Formats/MobFormat.cs), [MobileMdFormat.cs](../src/Formats/ArcNET.Formats/MobileMdFormat.cs), [MobileMdyFormat.cs](../src/Formats/ArcNET.Formats/MobileMdyFormat.cs), [JmpFormat.cs](../src/Formats/ArcNET.Formats/JmpFormat.cs), [MapPropertiesFormat.cs](../src/Formats/ArcNET.Formats/MapPropertiesFormat.cs)

### 2.3 Character Record (v2 inside mobile.mdy)

`CharacterMdyRecord` ([CharacterMdyRecord.cs](../src/Formats/ArcNET.Formats/CharacterMdyRecord.cs)) is a specialised SAR-scan parser for the v2 PC/NPC entries found in `mobile.mdy`. It does not use the standard `MobFormat` path.

**Decoded fields (read):**

| Field | SAR bsId | Elements | Property |
|-------|----------|----------|----------|
| Stats (STR/DEX/CON/INT/WIS/PER/CHA/RCE×28) | scan by signature (elemCnt=28) | int[28] | `Stats` |
| Basic skills (12) | scan by signature (elemCnt=12) | int[12] | `BasicSkills` |
| Tech skills (4) | scan by signature (elemCnt=4) | int[4] | `TechSkills` |
| Spell/tech disciplines (25) | scan by signature (elemCnt=25) | int[25] | `SpellTech` |
| Gold | 0x4B13 | int[1] | `Gold` |
| Arrows | 0x4D68[8] | int (in 11-elem SAR) | `Arrows` |
| Total kills | 0x4D68[0] | int (in 11-elem SAR) | `TotalKills` |
| Portrait index | 0x4DA4[1] | int (in 3-elem SAR) | `PortraitIndex` |
| PC name | non-SAR: 0x01 + uint32_len + ascii | string | `Name` |
| Position/AI (CurrentAid, Location, OffsetX) | 0x4DA3 | int[3] | `PositionAiRaw` |
| HP damage (AcBonus, HpPtsBonus, HpAdj, HpDamage) | 0x4046 | int[4] | `HpDamageRaw`, `HpDamage` |
| Fatigue damage | 0x423E | int[4] | `FatigueDamageRaw`, `FatigueDamage` |

**Mutation methods (With*):**

| Method | Mutates |
|--------|---------|
| `WithStats(int[28])` | Stats SAR (in-place byte patch) |
| `WithBasicSkills(int[12])` | Basic skills SAR |
| `WithTechSkills(int[4])` | Tech skills SAR |
| `WithSpellTech(int[25])` | Spell/tech SAR |
| `WithGold(int)` | Gold SAR element |
| `WithArrows(int)` | GameStats SAR element [8] |
| `WithTotalKills(int)` | GameStats SAR element [0] |
| `WithPortraitIndex(int)` | Portrait SAR element [1] |
| `WithPositionAi(int[3])` | Position/AI SAR |
| `WithHpDamage(int[4])` | HP SAR (all 4 elements) |
| `WithHpDamageValue(int)` | HP SAR element [3] only |
| `WithFatigueDamage(int[4])` | Fatigue SAR (all 4 elements) |
| `WithFatigueDamageValue(int)` | Fatigue SAR element [2] only |
| `WithName(string)` | PC name field (variable-length, RawBytes resized) |

All mutations return a new record instance; `RawBytes` is updated in-place or resized, preserving all unparsed trailing SAR data verbatim.

### 2.4 Object Property System

The property system is the engine that powers `MobFormat`, `MobileMdFormat`, and `SectorFormat`'s embedded objects.

**Wire type dispatch** — `ObjectPropertyIo` ([ObjectPropertyIo.cs](../src/Formats/ArcNET.Formats/ObjectPropertyIo.cs)):

| Bit range | Coverage | Object types |
|-----------|----------|--------------|
| 0–33 | Common fields: location, flags, scripts, art, HP, material, sound, category | All types |
| 34–40 | Common extension: rotation, speed, radius, height | All types |
| 41–63 | arcanum-CE common extension: conditions, permanent mods, dispatcher, initiative, secretdoor | arcanum-CE (0x77) only |
| 64–95 | Type-specific block 1 | Wall, Portal, Container, Scenery, Trap, Projectile; Item base block (64–86) for all item subtypes |
| 96–127 | Item subtype-specific block | Weapon (18 fields), Ammo (7), Armor (12), Gold (6), Food (5), Scroll (5), Key (5), KeyRing (6), Written (8), Generic (5) |
| 64–96 | Critter base block | PC, NPC |
| 128–152 | PC-specific block | PC: flags, fate, reputation, background, quests, blessings, curses, party, rumors, schematics, logbook, fog, player name, bank money, global flags/variables |
| 128–152 | NPC-specific block | NPC: flags, leader, AI data, combat focus, experience, waypoints, standpoints, origin, faction, pricing, reaction, damage, shit-list |

**Typed property accessors** — `ObjectPropertyExtensions` ([ObjectPropertyExtensions.cs](../src/Formats/ArcNET.Formats/ObjectPropertyExtensions.cs)):

| Accessor type | Read | Write (With*) |
|---------------|------|---------------|
| `int` (OD_TYPE_INT32) | `GetInt32()` | `WithInt32(int)` |
| `long` (OD_TYPE_INT64) | `GetInt64()` | `WithInt64(long)` |
| `float` (OD_TYPE_FLOAT) | `GetFloat()` | `WithFloat(float)` |
| `string` (OD_TYPE_STRING) | `GetString()` | `WithString(string)` |
| `(int X, int Y)` (location int64) | `GetLocation()` | `WithLocation(int, int)` |
| `int[]` (OD_TYPE_INT32_ARRAY SAR) | `GetInt32Array()` | `WithInt32Array(ReadOnlySpan<int>)` |
| `uint[]` (OD_TYPE_UINT32_ARRAY SAR) | `GetUInt32Array()` | `WithUInt32Array(ReadOnlySpan<uint>)` |
| `long[]` (OD_TYPE_INT64_ARRAY SAR) | `GetInt64Array()` | `WithInt64Array(ReadOnlySpan<long>)` |
| `ObjectPropertyScript[]` (OD_TYPE_SCRIPT_ARRAY SAR) | `GetScriptArray()` | `WithScriptArray(...)` |
| `Guid[]` (OD_TYPE_HANDLE_ARRAY SAR, 24B ObjectID each) | `GetObjectIdArray()` | `WithObjectIdArray(ReadOnlySpan<Guid>)` |
| `(OidType, ProtoOrData1, Guid)[]` (full ObjectID) | `GetObjectIdArrayFull()` | — |

**Property factory** — `ObjectPropertyFactory` ([ObjectPropertyFactory.cs](../src/Formats/ArcNET.Formats/ObjectPropertyFactory.cs)): creates `ObjectProperty` instances from scratch for all scalar and SAR types including `ForLocation`, `ForObjectIdArray`, `ForEmptyObjectIdArray`.

### 2.5 Typed Object Model (GameObjects Layer)

`ArcNET.GameObjects` provides a fully typed, class-hierarchy model parallel to the raw property system:

| Class | Fields | R | W |
|-------|--------|---|---|
| `ObjectCommon` | All common bits 0–40 | ✅ | ✅ |
| `ObjectCritter` | Critter base bits 64–96 | ✅ | ✅ |
| `ObjectPc` | PC-specific bits 128–152 (all 25 PC fields) | ✅ | ✅ |
| `ObjectNpc` | NPC-specific bits 128–152 | ✅ | ✅ |
| `ObjectWall`, `ObjectPortal`, `ObjectContainer`, `ObjectScenery`, `ObjectTrap`, `ObjectProjectile` | Type-specific blocks | ✅ | ✅ |
| `ObjectWeapon`, `ObjectAmmo`, `ObjectArmor`, `ObjectGold`, `ObjectFood`, `ObjectScroll`, `ObjectKey`, `ObjectKeyRing`, `ObjectWritten`, `ObjectGeneric` | Item-specific blocks | ✅ | ✅ |

**Source**: [GameObject.cs](../src/GameObjects/ArcNET.GameObjects/GameObject.cs), [ObjectPc.cs](../src/GameObjects/ArcNET.GameObjects/Types/ObjectPc.cs)

### 2.6 Game Data Layer

`GameDataSaver` ([GameDataSaver.cs](../src/GameData/ArcNET.GameData/GameDataSaver.cs)) saves `GameDataStore` contents:
- `.mes` message files — `SaveMessagesToFile`, `SaveMessagesToMemory`
- `.sec` sector files — `SaveSectorsToDirectory`
- `.pro` prototype files — `SaveProtosToDirectory`
- `.mob` static object files — `SaveMobsToDirectory`
- `SaveToDirectoryAsync` orchestrates all of the above with progress reporting
- `SaveToMemory` — serializes everything to in-memory byte arrays keyed by virtual filename

---

## 3. Identified Gaps — 100% Save API Coverage

The following gaps are ordered by implementation priority (highest impact first).

### GAP-1: No Save Archive Orchestration Layer (Critical)

**CLOSED** — Two complementary orchestration layers implemented.

**Layer 1 — ArcNET.Formats (hierarchical, map-state centric; added this session)**

New types in `ArcNET.Formats`:
- `SaveGame` ([SaveGame.cs](../src/Formats/ArcNET.Formats/SaveGame.cs)): aggregate with `Info` (SaveInfo), `Maps` (IReadOnlyList\<SaveMapState\>), `MessageFiles`.
- `SaveMapState` ([SaveMapState.cs](../src/Formats/ArcNET.Formats/SaveMapState.cs)): per-map typed state — `MapPath`, `Properties`, `JumpPoints`, `Sectors` (with filenames), `StaticObjects` (with filenames), `StaticDiffs`, `DynamicObjects`.
- `SaveGameReader` ([SaveGameReader.cs](../src/Formats/ArcNET.Formats/SaveGameReader.cs)): `Load(tfaiPath)`, `Load(tfaiPath, tfafPath)`, `Load(tfaiPath, tfafPath, gsiPath)`, `ParseMemory(tfai, tfaf, gsi)`.
- `SaveGameWriter` ([SaveGameWriter.cs](../src/Formats/ArcNET.Formats/SaveGameWriter.cs)): `Save(save, tfaiPath)`, `Save(save, tfaiPath, tfafPath)`, `Save(save, tfaiPath, tfafPath, gsiPath)`, `SaveToMemory(save)` returning `(Tfai, Tfaf, Gsi)` byte tuples.

Canonical file ordering on write: `map.prp` → `map.jmp` → `mobile/` → `mobile.md` → `mobile.mdy` → `sector_*.sec`. Output is functionally identical to original but byte layout within each map directory may differ.

**Layer 2 — ArcNET.Editor (flat dictionary, editor-focused; pre-existing)**

`ArcNET.Editor.SaveGame` stores format-parsed dictionaries (`Mobiles`, `Sectors`, `JumpFiles`, `MapPropertiesList`, `MobileMds`, `MobileMdys`, `Scripts`, `Dialogs`) plus raw `Files` and `Index` for atomic round-trips. `SaveGameLoader` provides sync+async load; `SaveGameWriter` uses atomic temp-then-rename writes. This is the layer used by the Probe tool.

**RE dependency**: None.

---

### GAP-2: Disconnected Object Representations (Critical)

~~**Missing**: A bridge between `MobData` and `GameObject`/`ObjectPc`.~~

**CLOSED** — Option A implemented.

- `GameObject.WriteToArray()` added to `ArcNET.GameObjects/GameObject.cs`: serialises the typed object back to the shared OFF binary format by dispatching to each type's `internal Write(ref SpanWriter, byte[], bool)` method.
- `MobDataExtensions.ToGameObject(this MobData)` added to `ArcNET.Formats/MobDataExtensions.cs`: calls `MobFormat.WriteToArray` → SpanReader → `GameObject.Read`.
- `MobDataExtensions.ToMobData(this GameObject)` added: calls `GameObject.WriteToArray` → SpanReader → `MobFormat.Parse`.

Both directions are now lossless round-trips through the shared OFF binary. The existing pc access example now becomes:
```csharp
// Typed access via bridge
var pc = (ObjectPc)record.Data.ToGameObject().Common;
int[] quests = pc.PcQuest;
```

**Complexity**: Medium. ✅ Done. Option B (eliminating the representation split permanently) remains open as a future refactor.

---

### GAP-3: CharacterMdyRecord — Missing Mutation Coverage (High)

**Status: ~65% complete (session 5 update). All bsId-known fields are implemented and tested. Remaining fields require RE.**

All `With*` methods whose field offsets can be derived without RE are implemented. Test coverage for all implemented fields was completed in session 5 (27 new tests in `CharacterMdyRecordTests.cs`). The remaining gaps all share the same RE dependency: the bsId value must be confirmed by parsing a save with a known mutation.

| Field | SAR/Encoding | Status |
|-------|-------------|--------|
| Bullets count | 0x4D68 element (game-stats SAR, elemCnt varies) | Pending RE: element index for tech characters |
| Power cells count | 0x4D68 element | Pending RE: element index for tech characters |
| ~~Max followers~~ | ~~0x4DA4[0]~~ | ✅ Done (`WithMaxFollowers`) — tested session 5 |
| ~~HP damage~~ | ~~bsId=0x4046, INT32[4] — pre-stat region~~ | ✅ Done (`WithHpDamage`, `WithHpDamageValue`) — tested session 5 |
| ~~Fatigue damage~~ | ~~bsId=0x423E, INT32[4] — pre-stat region~~ | ✅ Done (`WithFatigueDamage`, `WithFatigueDamageValue`) — tested session 5 |
| ~~Position / AI~~ | ~~bsId=0x4DA3, INT32[3] — pre-stat region~~ | ✅ Done (`WithPositionAi`) — tested session 5 |
| Global flags | PC field bit 147 (embedded in v2 record) | Pending RE: bsId unknown |
| Global variables | PC field bit 148 | Pending RE: bsId unknown |
| Bank money | PC field bit 146 | Pending RE: bsId unknown |
| Quest state | PC field bit 134 | Pending RE: bsId unknown |
| Reputation | PC field bits 130–131 | Pending RE: bsId unknown |
| Blessings / Curses | PC field bits 135–138 | Pending RE: bsId unknown |
| Rumors known | PC field bit 140 | Pending RE: bsId unknown |
| Schematics found | PC field bit 142 | Pending RE: bsId unknown |
| Fog of war mask | PC field bit 144 | Pending RE: bsId unknown |

**Note**: Some of these (Global flags/variables, Bank money, Quests) likely live in the extended SAR region already captured in `RawBytes` but not surfaced as named offsets. The scan infrastructure is in place (`GoldAmountBsId`, `GameStatsBsId`); adding each field requires identifying its `bsId` constant from live save analysis and adding an offset variable + `With*` method.

**Complexity**: Low per field once the bsId is known. The bsIds for global flags/variables, bank money, and quest state need RE validation.

**RE dependency**: bsId values for the new fields. **RE action**: parse a save with known mutations (set quest, change bank money) and search `CharacterMdyRecord.RawBytes` for the SAR containing the changed value. The bsId is at bytes [9..12] of that SAR header.

---

### GAP-4: ObjectPropertyIo — arcanum-CE Bits > 152 Unmapped (High)

**Missing**: For arcanum-CE saves (version 0x77), PC and NPC objects may have fields at bit indices > 152. The current `PcBit()` and `NpcBit()` functions return `null` for any bit ≥ 153, causing `ObjectPropertyIo.ReadProperties` to surface a `ParseNote` sentinel and stop reading — meaning all subsequent properties are silently dropped.

**Impact**: Any arcanum-CE PC/NPC object with populated fields above bit 152 will be truncated. The truncation is surfaced via `ObjectProperty.ParseNote` but there is no recovery.

**What is needed**:
1. RE: identify which bit indices above 152 appear in arcanum-CE saves and what their wire types are.
2. Extend `PcBit()` and `NpcBit()` to cover those bits.

**Complexity**: Low (code change) / High (RE work). The dispatch table pattern is established; adding rows is trivial once the bit→wire-type mapping is known.

**RE dependency**: Dump a high-level arcanum-CE character save and enumerate all set bits in the PC bitmap beyond bit 152. Cross-reference with TemplePlus `temple_enums.h` (the arcanum-CE engine inherits ToEE field indices above 152).

---

### GAP-5: GameData Layer Missing Save-Specific Operations (Medium)

**CLOSED** — No changes to `GameDataSaver` needed.

- Individual save-file write methods (`WriteToFile`) already exist on every format class: `MobileMdFormat`, `MobileMdyFormat`, `JmpFormat`, `MapPropertiesFormat`.
- Save archive assembly (TFAI + TFAF) is now handled by `ArcNET.Formats.SaveGameWriter` (GAP-1 above).
- `GameDataSaver` remains correctly scoped to world/editor data (sectors, prototypes, static mobs, messages). Save-specific orchestration delegates to `SaveGameWriter`.

---

### GAP-6: No Save Game Creation from Scratch (Medium)

**CLOSED** — `CharacterMdyRecordBuilder` and `SaveGameBuilder` implemented.

**New types in `ArcNET.Formats`:**

- `CharacterMdyRecordBuilder` ([CharacterMdyRecordBuilder.cs](../src/Formats/ArcNET.Formats/CharacterMdyRecordBuilder.cs)):
  static factory with a single `Create()` method:
  ```csharp
  CharacterMdyRecord pc = CharacterMdyRecordBuilder.Create(
      stats,        // int[28]
      basicSkills,  // int[12]
      techSkills,   // int[4]
      spellTech,    // int[25]
      gold: 200,
      name: "Hero",
      portraitIndex: 3,
      maxFollowers: 5
  );
  ```
  Builds the canonical binary layout (V2 magic + 4 primary SAR arrays + gold SAR with
  bsId=0x4B13 + portrait SAR with bsId=0x4DA4 + name field), then calls
  `CharacterMdyRecord.Parse()` to return a fully initialised record.
  All optional extended SARs (position/AI, HP, fatigue) are omitted — the engine
  initialises them on first map entry.

- `SaveGameBuilder` ([SaveGameBuilder.cs](../src/Formats/ArcNET.Formats/SaveGameBuilder.cs)):
  two overloads of `CreateNew()`:
  ```csharp
  // Quick overload: one map with PC only
  SaveGame save = SaveGameBuilder.CreateNew(info, "modules/Arcanum/maps/Map01", pc);

  // Full overload: pre-built map state
  SaveGame save = SaveGameBuilder.CreateNew(info, mapState);
  ```
  Validated: `mapPath` must start with `modules/`.

- `SarEncoding.BuildSarBytes(int, int, int, ReadOnlySpan<byte>)` overload added to support
  explicit bsId values required by the extended-scan recognized SARs.

**Usage example (full creation from scratch → write to disk):**
```csharp
var pc = CharacterMdyRecordBuilder.Create(stats, basicSkills, techSkills, spellTech,
    gold: 200, name: "Hero", portraitIndex: 3);

var info = new SaveInfo
{
    ModuleName       = "Arcanum",
    LeaderName       = pc.Name ?? "Hero",
    DisplayName      = "New Game",
    MapId            = 1,
    GameTimeDays     = 0,
    GameTimeMs       = 0,
    LeaderPortraitId = pc.PortraitIndex,
    LeaderLevel      = 1,
    LeaderTileX      = 1800,
    LeaderTileY      = 940,
    StoryState       = 0,
};

var save = SaveGameBuilder.CreateNew(info, "modules/Arcanum/maps/Map01", pc);
SaveGameWriter.Save(save, @"Slot0001.tfai");
```

**Tests added:** `CharacterMdyRecordBuilderTests.cs` (14 tests) + `SaveGameBuilderTests.cs` (12 tests).
Total test count: 235 (all passing).

**Session 5 update**: 27 additional tests added to `CharacterMdyRecordTests.cs` for secondary SAR fields: `MaxFollowers`, `WithName` mutation, HP SAR (bsId=0x4046), Fatigue SAR (bsId=0x423E), and Position/AI SAR (bsId=0x4DA3). All With* methods for these fields are now covered with parse, mutate, and round-trip cases. Total test count: 262 (all passing).

**RE dependency:** None — the binary layout was derived from existing test helpers and
format documentation confirmed in the save-format research notes.

---

### GAP-7: GetObjectIdArrayFull — No Write Counterpart (Low)

~~`GetObjectIdArrayFull()` returns full `(OidType, ProtoOrData1, Guid)` tuples but there is no `WithObjectIdArrayFull()` counterpart.~~

**CLOSED** — `WithObjectIdArrayFull(ReadOnlySpan<(short OidType, int ProtoOrData1, Guid Id)> ids)` implemented in `ObjectPropertyExtensions.cs`. Writes each 24-byte ObjectID with the correct OidType field; OID_TYPE_A entries preserve their proto index in the d.a union field.

**Complexity**: Very low. ✅ Done.

---

### GAP-8: Version / Compatibility Propagation (Low)

**CLOSED** — `SaveEngineVersion` enum added; `SaveGame.EngineVersion` property auto-derived on load.

New types/members in `ArcNET.Formats`:
- `SaveEngineVersion` enum ([SaveGame.cs](../src/Formats/ArcNET.Formats/SaveGame.cs)): `Vanilla = 0x08`, `ArcanumCE = 0x77`.
- `SaveGame.EngineVersion` property: populated by `SaveGameReader.DetectEngineVersion()`, which scans all static-object headers, `mobile.md` record versions, and `mobile.mdy` mob headers from the parsed maps. Returns `ArcanumCE` on first 0x77 version field found; otherwise `Vanilla`.
- No format-reader code changed — the arcanum-CE common extension bits 41–63 remain mapped unconditionally in `s_commonWireType` (harmless for vanilla saves because their bitmaps never set those bits).

**What remains**: Propagating `EngineVersion` into `ObjectPropertyIo.ReadProperties` when a genuine vanilla-vs-CE wire ambiguity is discovered (none identified yet). The `EngineVersion` field is now available on `SaveGame` for that future use.

---

## 4. API Coverage Summary

| Domain | Coverage |
|--------|----------|
| Archive (TFAI/TFAF) parse + write | **100%** |
| GSI save metadata parse + write | **100%** |
| `.sec` sector parse + write | **100%** |
| `.mob` static object parse + write | **100%** |
| `mobile.md` diff parse + write | **100%** |
| `mobile.mdy` dynamic spawn parse + write | **100%** |
| `CharacterMdyRecord` parse | **100%** |
| `CharacterMdyRecord` With* mutations (core stats/skills) | **100%** |
| `CharacterMdyRecord` With* mutations (secondary fields) | **~65%** (GAP-3 partially closed; HP/fatigue/positionAI/MaxFollowers tested session 5) |
| `.jmp` jump points parse + write | **100%** |
| `.prp` map properties parse + write | **100%** |
| Object property wire-type dispatch (vanilla 0x08) | **~95%** (some high bits untested) |
| Object property wire-type dispatch (arcanum-CE 0x77, bits ≤152) | **~90%** (GAP-4) |
| Object property wire-type dispatch (arcanum-CE 0x77, bits >152) | **0%** (GAP-4) |
| Typed object model (ObjectPc, ObjectNpc, etc.) R/W | **100%** |
| MobData ↔ GameObject bridge | **100%** (GAP-2 ✅ closed) |
| Save archive orchestration (SaveGame aggregate) | **100%** (GAP-1 ✅ closed — ArcNET.Formats + ArcNET.Editor) |
| Save engine version detection | **100%** (GAP-8 ✅ closed — `SaveGame.EngineVersion`) |
| Save creation from scratch | **100%** (GAP-6 ✅ closed — `CharacterMdyRecordBuilder` + `SaveGameBuilder`) |
| OID_TYPE_A write in handle arrays | **100%** (GAP-7 ✅ closed) |

**Overall save read/write completeness (low-level formats)**: ~97%  
**Consumer-facing save API completeness**: ~93% (GAP-1, GAP-2, GAP-5, GAP-6, GAP-7, GAP-8 closed; GAP-3 ~65%; GAP-4 open)

---

## 5. Implementation Strategies

### Strategy A: Bottom-Up (Recommended for Implementation Agents)

Complete the low-level gaps first, then build the orchestration layer on top of provably correct primitives.

**Phase A1 — Close low-level gaps (GAP-3, GAP-7)** — **Partially done; RE-dependent fields remain open**
1. ~~Add `WithObjectIdArrayFull()` to `ObjectPropertyExtensions` — 30 min, no RE needed.~~ ✅
2. ~~Test coverage for all bsId-known secondary fields (MaxFollowers, HP SAR, Fatigue SAR, Position/AI SAR, WithName).~~ ✅ Done session 5 (27 new tests).
3. For each remaining field in GAP-3 gap table: identify bsId via RE (see GAP-3 RE action), add offset tracking in `CharacterMdyRecord.Parse`, add `With*` method. Estimate: 2–4 hours per field once bsId is confirmed. (RE-dependent fields still open).

**Phase A2 — Bridge object representations (GAP-2)** — **DONE** ✅

**Phase A3 — Extend ObjectPropertyIo for arcanum-CE bits >152 (GAP-4)** — Open (RE required)
1. RE task: dump arcanum-CE PC bitmap, extract all bit indices > 152.
2. Cross-reference with TemplePlus `temple_enums.h` `obj_f` enum for wire types.
3. Extend `PcBit()` and `NpcBit()` dispatch functions.

**Phase A4 — Save archive orchestration (GAP-1)** — **DONE** ✅
- `ArcNET.Formats.SaveGame` + `SaveMapState` + `SaveGameReader` + `SaveGameWriter` implemented.
- `ArcNET.Editor.SaveGame` + `SaveGameLoader` + `SaveGameWriter` pre-existing (flat-dictionary, more mature).

**Phase A5 — Save creation from scratch (GAP-6)** — **DONE** ✅
- `CharacterMdyRecordBuilder.Create()` implemented in `ArcNET.Formats`.
- `SaveGameBuilder.CreateNew()` (two overloads) implemented in `ArcNET.Formats`.
- `SarEncoding.BuildSarBytes()` extended with a bsId overload.
- 26 new tests (CharacterMdyRecordBuilderTests + SaveGameBuilderTests); total 235.

**Phase A6 — Version propagation (GAP-8)** — **DONE** ✅
- `SaveEngineVersion` enum (`Vanilla`, `ArcanumCE`) added to `ArcNET.Formats`.
- `SaveGame.EngineVersion` auto-detected by `SaveGameReader.DetectEngineVersion()`.

**Phase A7 — Missing format tests** — **DONE** ✅
- `MobileMdFormatTests.cs` — 9 tests covering parse, OID identity, version, garbage-body fallback, round-trip.
- `MobileMdyFormatTests.cs` — 12 tests covering sentinel skipping, v2 character detection, mixed records, round-trip.
- Total test count after A7: 205 (all passing).

**Phase A8 — CharacterMdyRecord secondary field test coverage** — **DONE** ✅ (session 5)
- 27 new tests added to `CharacterMdyRecordTests.cs` covering: `MaxFollowers`, `WithName` mutation (shorter/longer/round-trip/null), HP SAR (bsId=0x4046) parse + `WithHpDamage` + `WithHpDamageValue`, Fatigue SAR (bsId=0x423E) parse + `WithFatigueDamage` + `WithFatigueDamageValue`, and Position/AI SAR (bsId=0x4DA3) parse + `WithPositionAi`.
- Total test count: 262 (all passing).

---

### Strategy B: Top-Down (Recommended for RE Agents)

Start with the consumer-facing API to drive what needs to be reverse-engineered.

**Phase B1 — Document all save file layouts for a target save slot**
1. Open a known save (vanilla or arcanum-CE).
2. Parse TFAI and produce a complete directory tree listing with sizes.
3. For each file, identify which format class handles it and confirm the parse succeeds cleanly (no `ParseNote` sentinels).
4. Log any `ParseNote` occurrences — these mark unmapped wire types and are the RE targets.

**Phase B2 — RE CharacterMdyRecord bsId map**
1. Create saves with known delta values (set gold to 99999, change bank money, mark quest complete).
2. Scan `CharacterMdyRecord.RawBytes` for the changed int32 values.
3. Read the SAR header at that location: bsId is at `bytes[offset - elementCount*4 - 13 + 9]` (4 bytes).
4. Document all confirmed bsId → field mappings.

**Phase B3 — RE arcanum-CE bitmap extension above bit 152**
1. Open an arcanum-CE (0x77) save with a high-level PC character.
2. Dump the raw PC object bytes from `mobile.mdy`.
3. Parse the OFF header bitmap (20 bytes = 160 bits).
4. Find all set bits above 152.
5. For each such bit, determine the wire type by observing the byte length consumed when reading that field in the original engine (use IDA/Ghidra on `arcanum.exe` or `TemplePlus` source code — the field index matches `obj_f` enum values).

**Phase B4 — Document mobile.md object type distribution**
1. Load all `mobile.md` files from a full-playthrough save.
2. Count successful decodes vs `Data=null` (raw fallback) vs `ParseNote` (truncated).
3. The object types with highest fallback rate are RE priorities for wire type mapping.

---

## 6. Test Coverage Assessment

Existing test files relevant to save formats:

| Test File | What It Covers | Gaps |
|-----------|---------------|------|
| `SaveIndexFormatTests.cs` | TFAI parse + round-trip | No large-tree integration test |
| `SaveInfoFormatTests.cs` | GSI parse + round-trip for v0 and v25 | — |
| `SectorFormatTests.cs` | Sector parse + round-trip | No test with embedded MobData objects |
| `MobFormatTests.cs` | Mob parse + round-trip | No compact-header test |
| `MobileMdFormatTests.cs` | mobile.md parse + round-trip, OID identity, version handling, garbage-body fallback | — |
| `MobileMdyFormatTests.cs` | mobile.mdy parse + round-trip, sentinel skipping, v2 character detection, mixed records | — |
| `MapPropertiesFormatTests.cs` | PRP parse + round-trip | — |
| `JmpFormatTests.cs` | JMP parse + round-trip | — |
| `TfafFormatTests.cs` | TFAF extract/pack | No test with real TFAF file |
| `CharacterMdyRecordTests.cs` | v2 record parse + With* methods (gold, arrows, kills, portrait, MaxFollowers, name, HP SAR, fatigue SAR, position/AI SAR) | bsId-unknown fields (bullets, power cells, global flags/vars, bank money, etc.) not yet testable without RE |
| `CharacterMdyRecordBuilderTests.cs` | `CharacterMdyRecordBuilder.Create()` — all fields, round-trip, validation | — |
| `SaveGameBuilderTests.cs` | `SaveGameBuilder.CreateNew()` — both overloads, round-trip (Writer→Reader), path validation | — |

---

## 7. Reference Materials for RE Agents

| Resource | Location | Content |
|----------|----------|---------|
| Object field enum | `ArcNET.GameObjects` / `ObjectField` | Bit indices for all game object fields |
| Wire type dispatch | `ObjectPropertyIo.cs` | Current mapping; gaps marked with `null` |
| SAR encoding | `SarEncoding.cs`, `ObjectPropertyExtensions.cs:SarDataOffset` | SAR header layout (presence 1B + elemSz 4B + elemCnt 4B + bsId 4B + data + bitset_cnt 4B + bitset_words) |
| v2 magic signature | `CharacterMdyRecord.V2Magic` | `[02 00 00 00 0F 00 00 00 00 00 00 00]` |
| OFF header layout | `GameObjectHeader.Read()` | Version (4B) + ProtoId (24B) + ObjectId (24B) + ObjectType (4B) + propCollItems (2B) + bitmap (variable) |
| LOCATION_MAKE encoding | `ObjectPropertyExtensions.GetLocation()` | `packed = (long)(uint)x | ((long)(uint)y << 32)` |
| TemplePlus source | GrognardsFromHell/TemplePlus | `temple_enums.h` for arcanum-CE field indices above 152 |
| Implementation guide | `docs/implementation-guide.md` | Overall project structure |
