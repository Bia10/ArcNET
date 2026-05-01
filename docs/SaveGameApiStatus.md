# Arcanum Save Game Read/Write API — Status Report

**Scope**: ArcNET.Formats + ArcNET.GameObjects + ArcNET.GameData  
**Date**: 2026-04-08 (updated 2026-04-14, session 69)  
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
| `.tmf` town-map fog | `TownMapFog` | `TownMapFogFormat` | ✅ | ✅ | Raw bit-array: one bit per revealed town-map tile. Top-level save file, not a map-directory companion. |
| `data.sav` save-global structural blob | `DataSavFile` | `DataSavFormat` | ✅ | ✅ | Structural typed surface: verified 8-byte header, aligned `INT32[4]` rows, remainder ints, contiguous row/remainder copy/patch helpers, copy-on-write builder batching for multi-step edits, and verbatim-byte round-trip. Field semantics remain unresolved. |
| `data2.sav` save-global table | `Data2SavFile` | `Data2SavFormat` | ✅ | ✅ | Partial typed surface: verified alternating `[state, 50000+ id]` table exposed as `IdPairs`, plus structural prefix/suffix INT32 accessors, contiguous range copy/patch helpers, copy-on-write builder batching for unresolved-region edits, and verbatim-byte round-trip. |

**Source**: [SectorFormat.cs](../src/Formats/ArcNET.Formats/SectorFormat.cs), [MobFormat.cs](../src/Formats/ArcNET.Formats/MobFormat.cs), [MobileMdFormat.cs](../src/Formats/ArcNET.Formats/MobileMdFormat.cs), [MobileMdyFormat.cs](../src/Formats/ArcNET.Formats/MobileMdyFormat.cs), [JmpFormat.cs](../src/Formats/ArcNET.Formats/JmpFormat.cs), [MapPropertiesFormat.cs](../src/Formats/ArcNET.Formats/MapPropertiesFormat.cs), [TownMapFogFormat.cs](../src/Formats/ArcNET.Formats/TownMapFogFormat.cs), [DataSavFormat.cs](../src/Formats/ArcNET.Formats/DataSavFormat.cs), [Data2SavFormat.cs](../src/Formats/ArcNET.Formats/Data2SavFormat.cs)

### 2.3 Character Record (v2 inside mobile.mdy)

`CharacterMdyRecord` ([CharacterMdyRecord.cs](../src/Formats/ArcNET.Formats/CharacterMdyRecord.cs)) is a specialised SAR-scan parser for the v2 PC/NPC entries found in `mobile.mdy`. It does not use the standard `MobFormat` path.

**Decoded fields (read):**

| Field | SAR bsId | Elements | Property |
|-------|----------|----------|----------|
| Stats (28-int critter stat array: primary, derived, and progression fields) | scan by signature (elemCnt=28) | int[28] | `Stats` |
| Basic skills (12) | scan by signature (elemCnt=12) | int[12] | `BasicSkills` |
| Tech skills (4) | scan by signature (elemCnt=4) | int[4] | `TechSkills` |
| Spell/tech disciplines (25) | scan by signature (elemCnt=25) | int[25] | `SpellTech` |
| Gold | 0x4B13 | int[1] | `Gold` |
| Arrows | 0x4D68[8] | int (in 11-elem SAR) | `Arrows` |
| Total kills | 0x4D68[0] | int (in 11-elem SAR) | `TotalKills` |
| Bullets | 0x4D68[11] | int (tech chars, eCnt ≥ 12) | `Bullets` |
| Power cells | 0x4D68[12] | int (tech chars, eCnt = 13) | `PowerCells` |
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
| `WithBullets(int)` | GameStats SAR element [11] (tech chars, eCnt ≥ 12) |
| `WithPowerCells(int)` | GameStats SAR element [12] (tech chars, eCnt = 13) |
| `WithPortraitIndex(int)` | Portrait SAR element [1] |
| `WithPositionAi(int[3])` | Position/AI SAR |
| `WithHpDamage(int[4])` | HP SAR (all 4 elements) |
| `WithHpDamageValue(int)` | HP SAR element [3] only |
| `WithFatigueDamage(int[4])` | Fatigue SAR (all 4 elements) |
| `WithFatigueDamageValue(int)` | Fatigue SAR element [2] only |
| `WithRumorsRaw(ReadOnlySpan<byte>)` | Rumors SAR element data (in-place, same eCnt) |
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
- `SaveGame` ([SaveGame.cs](../src/Formats/ArcNET.Formats/SaveGame.cs)): aggregate with `Info` (SaveInfo), `Maps` (IReadOnlyList\<SaveMapState\>), `MessageFiles`, typed top-level `TownMapFogs`, typed top-level `DataSavFiles`, typed top-level `Data2SavFiles`, and raw `RawFiles` for unresolved non-map save-global blobs and future unknowns.
- `SaveMapState` ([SaveMapState.cs](../src/Formats/ArcNET.Formats/SaveMapState.cs)): per-map typed state — `MapPath`, `Properties`, `JumpPoints`, `Sectors` (with filenames), `StaticObjects` (with filenames), `StaticDiffs`, `DynamicObjects`.
- `SaveGameReader` ([SaveGameReader.cs](../src/Formats/ArcNET.Formats/SaveGameReader.cs)): `Load(tfaiPath)`, `Load(tfaiPath, tfafPath)`, `Load(tfaiPath, tfafPath, gsiPath)`, `ParseMemory(tfai, tfaf, gsi)`.
- `SaveGameWriter` ([SaveGameWriter.cs](../src/Formats/ArcNET.Formats/SaveGameWriter.cs)): `Save(save, tfaiPath)`, `Save(save, tfaiPath, tfafPath)`, `Save(save, tfaiPath, tfafPath, gsiPath)`, `SaveToMemory(save)` returning `(Tfai, Tfaf, Gsi)` byte tuples.

Canonical file ordering on write: `map.prp` → `map.jmp` → `mobile/` → `mobile.md` → `mobile.mdy` → `sector_*.sec`. Output is functionally identical to original but byte layout within each map directory may differ.
Session 54+58+65 follow-up: the earlier layer-1 implementation silently dropped top-level non-map files. `SaveGameReader`/`SaveGameWriter` now preserve them: `.tmf` stays typed via `TownMapFogs`, `data.sav` stays typed via `DataSavFiles`, `data2.sav` stays typed via `Data2SavFiles`, and other unresolved files round-trip verbatim via `RawFiles` with their original virtual paths.

**Layer 2 — ArcNET.Editor (flat dictionary, editor-focused; pre-existing)**

`ArcNET.Editor.LoadedSave` stores format-parsed dictionaries (`Mobiles`, `Sectors`, `JumpFiles`, `MapPropertiesList`, `Messages`, `TownMapFogs`, `DataSavFiles`, `Data2SavFiles`, `MobileMds`, `MobileMdys`, `Scripts`, `Dialogs`) plus `Files`, `RawFiles`, and `Index` for atomic round-trips. `SaveGameLoader` provides sync+async load, `SaveGameEditor` provides a stateful player + `.gsi` metadata editing workflow on top of `LoadedSave`, and now also exposes typed save-message staging (`GetCurrentMessageFile`, `GetPendingMessageFile`, `WithMessageFile(...)`), typed town-map-fog staging (`GetCurrentTownMapFog`, `GetPendingTownMapFog`, `WithTownMapFog(...)`), typed `data.sav` staging (`GetCurrentDataSav`, `GetPendingDataSav`, `WithDataSav(...)`), typed `data2.sav` staging (`GetCurrentData2Sav`, `GetPendingData2Sav`, `WithData2Sav(...)`), raw embedded-file staging (`GetCurrentRawFile`, `GetPendingRawFile`, `WithRawFile(...)`) for unresolved save-global blobs and parse-failed files, plus local staged command history through `CanUndo`, `CanRedo`, `Undo()`, and `Redo()`. That history snapshots the exact pending save state, including derived leader-metadata sync, until commit/discard/reset. Session 55+56+58+65+69+70 follow-up: the editor raw-file API is intentionally limited to `LoadedSave.RawFiles` (the untyped / parse-failed subset), while successful `.mes` overrides now load into `LoadedSave.Messages`, successful `data.sav` parses now load into `LoadedSave.DataSavFiles`, and successful `data2.sav` parses now load into `LoadedSave.Data2SavFiles`; typed save surfaces such as `mobile.mdy`, `.mes`, `.tmf`, `.jmp`, `data.sav`, and `data2.sav` cannot be accidentally rewritten through the generic byte API, `SaveGameEditor.WithDataSav(...)` / `WithData2Sav(...)` now also support copy-on-write builder callbacks for batched structural edits without one full raw-buffer clone per mutation, and `CommitPendingChanges()` / `DiscardPendingChanges()` clear the local save-editor history when the baseline changes. The lower-level `SaveGameUpdates.RawFileUpdates` hook remains available for tests and Probe workflows, and `SaveGameWriter` uses atomic temp-then-rename writes. This is the layer used by the Probe tool.

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

**Status: ~90% complete (session 13 update). Blessings/Curses/Schematics now implemented via structural fingerprints — no session-specific bsId required.**

Session 65+68+69 save-global API update: `data.sav` is no longer raw-only. `DataSavFormat` / `DataSavFile` now expose the verified 8-byte header, aligned `INT32[4]` row framing, remainder ints, contiguous row/remainder copy+patch helpers, copy-on-write builder batching, and byte-preserving round-trip through both `SaveGame` and `LoadedSave` / `SaveGameEditor`. The matching `data2.sav` surface now has the same builder-style batching over the verified pair table plus unresolved prefix/suffix regions. Session 69 Probe re-verification on `0178` and `0170-0178` kept the same structural boundaries: `Slot0178` still reports `data.sav` `rows=1629 remInts=2 sects=733`, `data2.sav` pair table `ints=56..477 pairs=211`, the range still has `8` exact `data.sav` front families and `9` exact `data.sav` tail families across `9` slots, and the `data2.sav` unresolved suffix still has one recurring late-game family shared by `0171`, `0172`, `0173`, `0175`, `0176`, `0177`, and `0178`. This closes the allocation-heavy multi-step edit gap, but it does **not** resolve the semantic location of `BankMoney`, `GlobalFlags`, or `GlobalVariables` within the save-global payload.

Session 12+13 RE findings (Probe mode 9 lifecycle diff Slot0120→0177; mode 10 full SAR dump):

- **PcBlessingIdx / PcBlessingTsIdx** (bits 135/136): structural fingerprint = **first `4:N:2` + `8:N:2` consecutive pair** in the post-SpellTech extended scan region. ArciMagus Slot0177 SAR#13/14: `bsId=0x48E9/0x48EA`, N=7 (gods: [1049,1051,1004,1017,1042,1025,1024]). Mode 9 lifecycle diff confirms N grew 5→7 between Slot0174→0175, proving the pair detection logic is correct. Implemented: `BlessingRaw`, `BlessingProtoElementCount`, `BlessingTsRaw`, `WithBlessingRaw`.
- **PcCurseIdx / PcCurseTsIdx** (bits 137/138): structural fingerprint = **second `4:M:2` + `8:M:2` consecutive pair**. Slot0177 SAR#16/17: `bsId=0x2AA3/0x48E2`, M=2, values [67,53]. Stable since mid-game (present in Slot0057 onwards). Implemented: `CurseRaw`, `CurseProtoElementCount`, `CurseTsRaw`, `WithCurseRaw`.
- **PcSchematicsFoundIdx** (bit 142): structural fingerprint = **standalone `4:K:2` with first element value > 1000**. Slot0177 SAR#19: `bsId=0x5228`, K=4, values [5090,4810,4010,5410] (tech schematic prototype IDs). Finalized when the next SAR has bsCnt ≠ 2 or eSize ≠ 8. Implemented: `SchematicsRaw`, `SchematicsElementCount`, `WithSchematicsRaw`.
- **V2Magic false-positive bug fixed** (session 13): A SAR whose bsCnt=2 and first bitset word=0x0F produces the byte sequence `02 00 00 00 0F 00 00 00 00 00 00 00` (= V2Magic) at the bsCnt field. Without the fix, the pre-scan nextMagicPos search found this false positive and set extLimit too early, cutting the extended scan before the Schematics SAR was processed. Fixed in `CharacterMdyRecord.Parse`: when `sarEnd2 > extLimit` and the V2Magic falls within the current SAR's byte range, advance past the SAR and retry with an updated extLimit. Specific reproduction: Slot0177 ArciMagus SAR#19 (schematic) at offset 0x0227D, bsCnt at 0x0229A = `02 00 00 00`, bitset[0] at 0x0229E = `0F 00 00 00` → V2Magic at 0x0229A. Fix verified by new test `Parse_RecoversSchematics_WhenV2MagicAppearsInSchematicsBitset`.
- **9 new tests added** (session 13): `Parse_DetectsBlessing*`, `Parse_DetectsCurse*`, `Parse_DetectsSchematics*`, `Parse_Null*`, `Parse_RecoversSchematics_WhenV2MagicAppearsInSchematicsBitset`, `With[Blessing/Curse/Schematics]Raw_PatchesAndRoundTrips`. Total test count: 294 (all passing).
- **Probe mode 7 updated**: Displays `Blessings[N]`, `Curses[M]`, `Schematics[K]` for each v2 record. Verified for Slot0177 ArciMagus.

Session 10 RE findings (Probe mode 10: full SAR element dump; Slot0013–0177):

- **Probe mode 10 added**: `probe 10 <slot4> [bsCnt]` — dumps ALL elements of every non-filler SAR in the player v2 record with full element listings. Supports optional `bsCnt` filter for targeted investigation.
- **`4:N:5` bsCnt=5 SAR pair identified**: The mystery `4:N:5` SARs (first values [72,105,327,126,…]) are **NOT global flags**. They are the **Conditions / PermanentMods** arrays (critter-shared bits 41–42):
  - SAR A (`bsId=0x4D9C` in Session B): condition prototype IDs — values are Arcanum effect prototype IDs (72, 105, 227, 264, 238, etc.)
  - SAR B (`bsId=0x5090` in Session B): condition arg0 values — mostly 5 (max active), a few 0–3 for recently-acquired conditions
  - Both SARs always appear as a matched pair with identical `eCnt` and `bsCnt=5`
  - `bsCnt=5` (160 bit-address slots) is the critter conditions namespace size. ArciMagus at level 37 has 61 active condition instances from racial bonuses, learned spells, and talents.
  - These are **not PC-exclusive** — they appear identically in NPC v2 records. Already round-tripped verbatim via `RawBytes`.
- **Session 9 hypothesis corrected**: The session 9 `4:N:2/3` INT32 arrays with first values [72,105,327,…] are the **same conditions field** at earlier levels (fewer instances → lower `eCnt`). As the character progresses, `eCnt` grows; the diff engine treated each unique `eCnt` as a different fingerprint. They are NOT global flags.
- **PC-exclusive SAR count = 0**: Full scan of all v2 records in Slot0171 (Session A) confirms zero bsIds are exclusive to the PC record. Every SAR the PC has is also found in at least one NPC. Verified across 125 PC bsIds / 176 NPC bsIds.
- **Remaining GAP-3 items absent from test saves**: Global flags, Global variables, Bank money, Blessings/Curses, Schematics, and Fog of war produce **no SARs** in ArciMagus saves. Three explanations:
  1. ArciMagus is a test character not progressed through story content (no global flags triggered, no bank deposits, no blessings received, no schematics found as a pure-magic build).
  2. These fields may live in `mobile.md` (OFF format) only, surfaced already by `MobData.ToGameObject() → ObjectPc`, not duplicated in the v2 SAR.
  3. Fog of war (`Int32` scalar, bit 144) is map-specific and may only appear per-map in OFF format.
- **OFF format coverage confirmed**: `ObjectPc` already implements `PcGlobalFlags`, `PcGlobalVariables`, `PcBankMoney`, `PcBlessing`, `PcBlessingTs`, `PcCurse`, `PcCurseTs`, `PcSchematicsFound`, and `PcFogMask` for the OFF mobile.md path. These are fully accessible via the GAP-2 bridge (`MobData.ToGameObject()`).

Session 9 RE findings (cross-save diff using Probe mode 9, slots 0013–0120):
- **Rumors** (`8:N:39`): structural fingerprint `eSize=8, bcCnt=39` confirmed stable across all sessions. eCnt grows as rumors are learned; first appears ~level 10 (Slot0033). Now implemented as `RumorsRaw`/`RumorsCount`/`WithRumorsRaw`.
- **GameStats growth**: `4:11:3` switches to `4:13:3` at character level 12 (Slot0040⁁0041). Confirmed existing code handles this correctly.
- **`24:N:2/3` arrays**: companion/follower OID handles (24B structure × N). Appear as followers join/leave.
- **`8:N:2` arrays**: smaller handle arrays (2–3 entries), likely NPC party handles.

All `With*` methods whose field offsets can be derived without RE are implemented. Test coverage for all implemented fields was completed in session 5 (27 new tests in `CharacterMdyRecordTests.cs`). The remaining gaps are blocked on obtaining a story-progression test save.

| Field | SAR/Encoding | Status |
|-------|-------------|--------|
| ~~Bullets count~~ | ~~0x4D68 element [11] (GameStats SAR, eCnt ≥ 12)~~ | ✅ Done (`Bullets`, `WithBullets`) — session 7 |
| ~~Power cells count~~ | ~~0x4D68 element [12] (GameStats SAR, eCnt = 13)~~ | ✅ Done (`PowerCells`, `WithPowerCells`) — session 7 |
| ~~Max followers~~ | ~~0x4DA4[0]~~ | ✅ Done (`WithMaxFollowers`) — tested session 5 |
| ~~HP damage~~ | ~~bsId=0x4046, INT32[4] — pre-stat region~~ | ✅ Done (`WithHpDamage`, `WithHpDamageValue`) — tested session 5 |
| ~~Fatigue damage~~ | ~~bsId=0x423E, INT32[4] — pre-stat region~~ | ✅ Done (`WithFatigueDamage`, `WithFatigueDamageValue`) — tested session 5 |
| ~~Position / AI~~ | ~~bsId=0x4DA3, INT32[3] — pre-stat region~~ | ✅ Done (`WithPositionAi`) — tested session 5 |
| Global flags | PC field bit 147 — OFF format: `ObjectPc.PcGlobalFlags` via GAP-2 bridge | Blocked: absent from ArciMagus v2 records; needs story-progression save for v2 SAR bsId RE |
| Global variables | PC field bit 148 — OFF format: `ObjectPc.PcGlobalVariables` via GAP-2 bridge | Blocked: same constraint |
| Bank money | PC field bit 146 — OFF format: `ObjectPc.PcBankMoney` via GAP-2 bridge | Blocked: ArciMagus never used bank; absent from v2 records |
| ~~Quest state~~ | ~~eSize=16, bcCnt=37 structural fingerprint (bsId varies per session)~~ | ✅ Done (`QuestCount`, `QuestDataRaw`, `QuestBitsetRaw`, `WithQuestDataRaw`, `WithQuestStateRaw`) — session 8 |
| ~~Reputation~~ | ~~eSize=4, eCnt=19, bcCnt=3 structural fingerprint (bit 130 PcReputationIdx)~~ | ✅ Done (`ReputationRaw`) — session 8 |
| Blessings / Curses | PC field bits 135–138 — structural fingerprint: consecutive `4:N:2` + `8:N:2` pairs | ✅ Done (`BlessingRaw`, `BlessingTsRaw`, `CurseRaw`, `CurseTsRaw`, `With[Blessing/Curse]Raw`) — session 12; V2Magic FP fix session 13 |
| ~~Rumors known~~ | ~~PC field bit 140 — PcRumorIdx, eSize=8 bcCnt=39 structural fingerprint (bsId varies per session)~~ | ✅ Done (`RumorsRaw`, `RumorsCount`, `WithRumorsRaw`) — session 9 |
| Schematics found | PC field bit 142 — standalone `4:K:2` with firstVal > 1000 | ✅ Done (`SchematicsRaw`, `SchematicsElementCount`, `WithSchematicsRaw`) — session 12; V2Magic FP fix session 13 |
| ~~Fog of war mask~~ | ~~Stored in top-level `.tmf` town-map fog bit-arrays in the tested save corpus, not as a v2 SAR field~~ | ✅ Done (`TownMapFogFormat`, `LoadedSave.TownMapFogs`, `SaveGameUpdates.UpdatedTownMapFogs`) — session 51 |

**Path to close the remaining items**: Provide a save from a standard story-progression character (has reached Tarant, deposited bank money, received blessings from a god, found tech schematics). The v2 SAR structure fingerprints can then be identified within one Probe session using `probe 10` and the PC-exclusive analysis.

**Complexity**: Low per field once the structural fingerprint is known. OFF-format access via GAP-2 bridge is already fully functional.

Session 15 RE findings (Probe modes 9/10/13/14; Slot0013–0178):

- **Reputation faction slot IDs confirmed** (Mode 14, Slot0178): The reputation SAR has `eSize=4, eCnt=19, bsCnt=3`. The bsCnt=3 bitset (3 × 32-bit words = 96 bit-address slots) stores the faction slot IDs for each entry. Confirmed IDs via new `CharacterMdyRecord.ReputationFactionSlots` property: `[0,1,2,3,4,5,6,7,8,9,10,11,12, 64,65,66,67,68,69]` — 13 primary factions (slots 0–12) followed by 6 additional factions (slots 64–69). Mode 14 now shows each entry as `Slot/Value` instead of a raw 0–18 index. Notable values in Slot0178: slot0=1471 (Arcanum total kills?), slot1=30535 (positive rep), slot4=1 (binary flag), slot8=−800 (enemy faction), slot64–69 alternating large/small values.
- **Quest state bitmask discovered** (Mode 14, Slot0178): `WithQuestStateRaw` currently treats the state field as a simple enum (0=not-started, 1=in-progress, 2=completed, 4=secondary). Mode 14 dump of Slot0178 revealed undocumented states in the wild: **3** (1 quest: proto 1119), **256/0x100** (4 quests: protos 1041, 1096, 1115, 1138), **258/0x102** (3 quests: protos 1074, 1109, 1158). These suggest the field is a **bitmask**: bit 0 = in-progress / triggered, bit 1 = primary objective completed, bit 2 = secondary objective completed, bit 8 = unknown (failed? botched? time-expired?). At the time this looked like a proto-range cluster; session 17 later disproved the narrower “Mastery College only” interpretation once quest labels were resolved. **No code change made** — `WithQuestStateRaw` remains a raw byte-array patcher; callers are responsible for providing correct state values.
- **Slot0178 profile** (Mode 10, forced lv=45 cheat save): 107 non-filler SARs, 91688 bytes. 7 blessings (god proto IDs: 1049, 1051, 1004, 1017, 1042, 1025, 1024), 2 curses (IDs: 67, 53), 4 schematics (IDs: 4010, 4810, 5090, 5410), 78 quests (all match confirmed bitset), 116 rumors. All previously-implemented fields (`ReputationRaw`, `BlessingRaw`, `CurseRaw`, `SchematicsRaw`, `QuestDataRaw`, `RumorsRaw`, `SpellTech`) match expected values — no regressions detected.
- **Mode 13 slot arg-padding bug fixed** (session 15): Running `probe 13 13 178` (start slot=13) caused a `FileNotFoundException` because `args[1]="13"` produced slot stem `"Slot13"` instead of `"Slot0013"`. Fixed: `slot4` now uses `args[1].PadLeft(4,'0')[..4]`; mode 13 added to the load-exclusion list (`testMode is not (9 or 11 or 13)`).
- **Mode 13 extended** (session 15): now tracks `hp_dmg` (HP damage SAR element[0]) and `fat_dmg` (fatigue damage SAR element[0]) in addition to the previously tracked fields. Also tracks per-faction reputation diffs as `rep[i]:oldVal→newVal` for each of the 19 reputation elements that changed between successive slots. Baseline output line now includes `hp_dmg=X fat_dmg=Y rep=[…]`. Key observations from Slot0170→0178 run: Slot0174 shows rep[0]/[5]/[6] changes (level-up alignment event?); Slot0175 shows rep[0]/[1]/[2]/[11]/[12] changes plus blessings 5→7; Slot0178 shows lv:3→45/XP:+975000/align:0→100/MasteryCol:0→−1 (cheat).
- **`SarUtils.DecodeReputation()` helper added** (session 15): `public static List<(int Slot, int Value)> DecodeReputation(SarEntry, int[])` — pairs each element index with its faction slot ID for display/analysis.
- **`CharacterMdyRecord.ReputationFactionSlots`** property added (session 15): reads the reputation SAR's bsCnt=3 bitset (starts at `ReputationDataOffset + 19*4`) and returns the 19 set-bit indices as `int[]?`. Returns `null` when no reputation SAR is present (early saves). Confirmed layout: `[0..12, 64..69]`.

Session 16 RE findings (Probe modes 9/13/14; Slot0170–0178 / Slot0178):

- **Quest-state transitions are now visible even when `QuestCount` stays flat** (Mode 13): late-game saves in the Slot0170→0178 range contain real per-quest state flips that the earlier count-only tracker hid. Verified examples: Slot0175 reports `q1109: completed(primary) → completed(primary)|bit8? [0x102]`; Slot0176 reports `q1026: completed(primary) → completed(secondary)` and `q1143: completed(primary) → completed(secondary)` with no ambiguity from quest-count churn. This strengthens the session-15 conclusion that the quest state field behaves as a **bitmask**, not a closed enum.
- **Slot0178 quest-state census now has a stable decoder view** (Mode 14): state summary for the lv=45 cheat save is `active: 1`, `completed(primary): 24`, `active|completed(primary) [0x003]: 1`, `completed(secondary): 45`, `bit8? [0x100]: 4`, `completed(primary)|bit8? [0x102]: 3`. In other words, all previously observed raw values (`0x003`, `0x100`, `0x102`) survive a full single-slot decode and are no longer one-off anomalies.
- **Reputation decode is now actually slot-aware**: the session-15 helper existed, but it still paired values with sequential element indices instead of the SAR bitset's set-bit positions. `SarUtils.DecodeReputation()` now uses the parsed bit slots, so Mode 14 and the newer Mode 13 deltas both emit true faction slot IDs. Re-verified on Slot0178: `[0..12, 64..69]` with values such as `slot0=1471`, `slot1=30535`, `slot8=-800`, `slot64=30286`.
- **Mode 9 no longer collapses duplicate SAR fingerprints**: repeated fingerprints are now tracked as separate lifecycle rows (`24:2:2`, `24:2:2#2`, etc.) instead of being merged into one synthetic row per snapshot. This exposed duplicate `4:2:2` / `24:2:2` records in the late-game range that were previously invisible in the lifecycle table.
- **Mode 9 same-size diffs now report bitset-slot movement**: for unchanged `eSize/eCnt/bsCnt` records, the diff output now prints slot-delta information such as `slots:[0,1]→[3,4]` when the bitset membership changes. This catches structural rewiring that an element-only diff would miss.

Session 17 RE findings (Probe modes 13/14 + DAT archive inspection; Slot0170–0178 / Slot0178):

- **Quest label source resolved**: the usable quest-log text table for this retail install is `modules/Arcanum.dat:mes/gamequestlogdumb.mes`, not `quests.mes`. Probe now loads **161** quest labels from that DAT entry. The similarly named `Module template\Rules\gamequest.mes` / `Module template\mes\gamequestlog*.mes` entries inside `Arcanum3.dat` are one-line template placeholders and are ignored.
- **The `0x100` / `0x102` quest-state bit is not Mastery-College-specific**: named Mode 14 output shows `0x100` on `1041 "Appuhlbeez will get me inna Batez house..."`, `1096 "Richard Leekz the blacksmith in Stillwader want me find his frend Siruhs."`, `1115 "Take heeling poshuhn to Adkuhn Chambrz so he see agin..."`, and `1138 "Find qur cre fix for Cynthia Witz wherewolfes curs."`; `0x102` appears on `1074 "Sinteea Bogz axed me help her excape from camp on the Isele of Deespare."`, `1109 "Gildr Nite Walk say he give me 1500 coin..."`, and `1158 "Dead guy Bargo want me kill bad preest guy Arbaluh..."`. These span unrelated Bates, Isle of Despair, Stillwater, Caladon, and Half-Ogre content, so session 15’s Mastery-College hypothesis is closed as incorrect.
- **Named late-game transitions are now directly visible**: Mode 13 verifies `Slot0175` flips `q1109` (`Gildr Nite Walk...`) from `completed(primary)` to `completed(primary)|bit8? [0x102]`; `Slot0176` flips `q1026` (`Go to Caluhdun tempuhl of Puhnareezs and talk to Kan Hoo hah?`) and `q1143` (`Artur Tiruhn wnt me find proov of haf oger breedeeng...`) from `completed(primary)` to `completed(secondary)`. The bitmask model remains intact; only the quest-family interpretation changed.

Session 18 RE findings (Probe mode 9 similarity rerun; Slot0170–0178):

- **Occurrence-order false positives closed**: the old Mode 9 `4:5:2` bless→effects mutation was a pairing artifact. With similarity-based duplicate matching, Slot0170’s blessing-style `[1049,1051,1004,1017,1042]` row no longer gets compared against Slot0171’s `[72,105,50,327,158]` / `[0,1,7,3,5]` effects rows; they now resolve as separate lifecycle tracks.
- **Large late-game jumps contain real reorder churn**: Slot0174→0175 and Slot0177→0178 both reorder many same-fingerprint duplicate arrays. The new `aN→bM` labels show that much of the previous Mode 9 noise was record-order movement, not in-place mutation of a single logical field.
- **The cleaned diff still preserves the real deltas**: after removing the false pairings, the genuine late-game changes remain visible in Mode 9 and line up with the earlier Mode 13 results — especially the Slot0174→0175 reputation deltas, blessing-count growth, and quest/rumor expansion.

Session 19 RE findings (Probe modes 9/10/13/14; Slot0170–0178 / Slot0178):

- **The duplicate low-arity packets are structurally valid, not scan garbage**: Slot0178 Mode 10 with `bsCnt=2` shows the player v2 record beginning with repeated `24:2:2`, `4:2:2`, and many `4:4:2` packets before the canonical primary arrays (`Stats` = SAR#12, `BasicSkills` = SAR#13, `SpellTech` = SAR#15). Their bitset populations exactly match `eCnt` (for example the repeated `4:4:2` packets all carry slot set `[0,1,2,3]`), which rules out the working hypothesis that the late-game Mode 9 duplicate noise was mostly false-positive SAR parsing.
- **The remaining late-game noise is reorder churn among real packets**: once the parser-false-positive hypothesis was closed, the useful improvement was output classification rather than matcher replacement. On the rerun, the large Slot0174→0175 and Slot0175→0176 jumps still contain many duplicate-row moves, but the actual payload deltas continue to line up with Mode 13/14 — especially reputation changes, quest growth/state flips, and blessing growth 5→7.
- **The primary signal survives the cleaner presentation**: after separating reorder-only rows from payload deltas, Mode 9 still surfaces the same verified late-game changes as before: Slot0174→0175 reputation `rep[0/1/2/11/12]`, quest growth `72→75`, blessing growth `5→7`; Slot0175→0176 reputation `rep[0/1/2/5/6]` and the quest-state changes seen in Mode 13 remain actual `CHG` lines.

Session 20 RE findings (Probe modes 9/13; Slot0170–0178):

- **Mode 9 now quantifies duplicate churn instead of dumping every orphan track**: the lifecycle output starts with a per-fingerprint summary (`slot span`, `dup@present`, `tracks`, `multi`, `one`, `chg`) and suppresses one-slot lifecycle rows from the detail table. On the Slot0170–0178 rerun it omitted **137** one-slot lifecycle rows; **21** fingerprints only appear as single-slot singletons.
- **Late-game churn is concentrated in a small set of low-arity INT32 fingerprints**: the summary table shows `4:2:2` at **40 tracks / 16 recurring / 24 one-slot / 9 changed** with multiplicity **5–29**, `4:12:2` at **16 / 7 / 9 / 6** with multiplicity **4–10**, `4:28:2` at **11 / 7 / 4 / 4** with multiplicity **3–8**, `4:7:2` at **11 / 8 / 3 / 3** with multiplicity **2–7**, `4:4:2` at **24 / 10 / 14 / 1** with multiplicity **7–15**, and `4:25:2` at **16 / 8 / 8 / 1** with multiplicity **3–10**. `24:2:2` remains duplicate-heavy (**10 / 3 / 7 / 0**) but contributes no payload deltas. This closes the remaining ambiguity about where the late-game Mode 9 noise actually lives.
- **Transition summaries now isolate the real spikes**: `0174→0175` reports `new=32 gone=11 move=15 chg=23`, with `CHG fp` dominated by `4:2:2×9`, `4:28:2×6`, `4:12:2×3`, and `4:7:2×2`; `0175→0176` reports `new=8 gone=44 move=11 chg=13`, with move churn concentrated in `4:2:2×4` and `4:7:2×3`; `0177→0178` reports `new=56 gone=0 move=14 chg=10`, with move churn concentrated in `4:2:2×5`, `4:12:2×3`, `4:4:2×3`, and `4:28:2×2`. The slot summaries now make it obvious which ranges are structural churn versus semantic state changes.
- **Mode 13 still matches the aggregated Mode 9 picture**: the rerun confirms the main semantic transitions remain `0174→0175` (reputation deltas, quest growth `72→75`, blessing growth `5→7`) and `0175→0176` (reputation deltas plus quest-state flips). `0177→0178` remains mostly cheat-save structural churn plus the level/XP jump. This validates the new renderer rather than changing the underlying interpretation.

Session 21 tooling improvements (Probe modes 9/13; Slot0170–0178):

- **Duplicate `SaveGame.cs` removed**: The session 20 rename (`ArcNET.Editor.SaveGame` → `LoadedSave`) created `LoadedSave.cs` but did not delete the original `SaveGame.cs`, leaving both in the project and causing a `CS0101` duplicate-class build failure. `SaveGame.cs` removed; build restored.
- **Mode 9 CHG output now shows labeled element indices**: Stats, BasicSkills, TechSkills, and SpellTech diffs now render named fields via `SarUtils.GetElementLabel(...)` instead of raw numeric indices. Session 24 completed the 28-stat map so the current labels now match the shared `CharacterRecord` model end-to-end: `STR/DEX/CON/BEA/INT/PER/WIL/CHA`, derived critter stats through `MTApt`, then `lv/XP/align/fate/unspent/magicPts/techPts/poisonLvl/age/gender/race`.
- **Pointer-like element diffs are now suppressed**: Values with `|v| > 200 000 000` are treated as runtime dispatch-table addresses. When both the old and new value of an element diff qualify, the diff is omitted from the `CHG` line and a `(N ptr-noise diffs suppressed)` note is appended. When only one side is pointer-like, the diff is shown (a pointer ↔ game-value transition is a real event). Implemented via `SarUtils.IsPointerLike(int)` and `SarUtils.PartitionElementDiffs(...)`.
- **Value-aware `4:7:2` annotation replaces the generic "Blessings×7 or NPC-dispatch" label**: `SarUtils.AnnotateSarValue(SarEntry)` inspects the first-values array and returns `"NPC-dispatch ptrs INT32[7]"` when any non-(-1) value is pointer-like, or `"ProtoIdArray INT32[N] (bless/schematics)"` when all non-(-1) values are > 500 (blessing/schematic proto IDs). Mode 9 now shows the former for the many NPC dispatch arrays and `Blessings×7` for the actual blessing record, with the change confirmed in the `0174→0175` diff.
- **Mode 13 now tracks PC stat-array and basic-skill deltas**: `baseStats[…]` and `skills[…]` are appended to each diff line when the PC's Stats indices `0..15` or BasicSkills values change between consecutive slots. These changes are also wired into the `anyDiff` gate so unchanged slots are still suppressed.
- **`4:2:2` fingerprint annotation updated** from `"Curses INT32[2]"` to `"Conditions/Curse INT32[2]"` to reflect that the vast majority of `4:2:2` SARs in any character record are NPC condition/effect data rather than PC curse arrays.
- **Stats array index [21] is now treated as `unspent`**: the level-up correlation noted in session 21 is consistent with the shared `CharacterRecord.UnspentPoints` mapping used by the editor-side v2 codec.
- **The old `s25` placeholder is closed as `age`**: session 24 aligned Probe with the same 28-stat map used by `CharacterRecord`, which identifies indices `24..27` as `poisonLvl`, `age`, `gender`, and `race`.

Session 22 RE findings (Probe mode 9; Slot0170–0178):

- **Mode 9 lifecycle track detail enhanced with bsId column and value-aware annotation**: the multi-slot track table now shows a `bsId` column (single hex value when stable, `"varies"` when inconsistent across the track's history) beside the updated annotation column (uses `AnnotateSarValue` instead of `AnnotateFingerprint`, so the table now shows `ProtoIdArray`, `CondFlag`, `CondProto/CurseProto`, and `NPC-dispatch ptrs` where previously all `4:2:2` and `4:4:2` rows appeared identical). On the 0170–0178 rerun, every multi-slot track shows `bsId=varies`, confirming bsIds are session-specific across the cheat-cycled save range. Short-lived single-transition tracks (e.g. `24:5:2` GONE@0174 with `bsId=0x5322`, `24:5:2#2` with `bsId=0x4C5B`) are the exception — they span only one slot transition and so observe the same bsId on both sides, proving bsIds are **stable within a continuous game session** but **reassigned when a new session starts**. Practical rule confirmed: field identification across different save files must rely on structural fingerprints + value ranges, never on bsId alone.
- **`[DISC]` marker added to mode 9 slot-pair diffs**: when the level drops by more than 3 between two consecutive snapshots, the pair header line is suffixed with `[DISC]` to flag a likely state-switch boundary (different save state loaded rather than incremental progression). On the 0170–0178 run, `0170→0171` (lv44→10) and `0175→0176` (lv45→3) are flagged; the four upward-level transitions are not (they may still be discontinuous but the heuristic is level-drop only).
- **`4:2:2` annotation now sub-categorises by value range**: `AnnotateSarValue` for `4:2:2` (eSize=4, eCnt=2, bsCnt=2) now returns `"CondFlag INT32[2]"` when either value is ≤10 (small condition flag), `"CondProto/CurseProto INT32[2]"` when both values are in 30–500 (condition or curse proto-ID pair), and falls back to `"Conditions/Curse INT32[2]"` otherwise. On the 0170–0178 run: `4:2:2#3` `[2,0]` → CondFlag; `4:2:2#4` `[50,50]→[67,53]` → CondProto/CurseProto (this is the confirmed curse SAR); `4:2:2#6` and many other tracks in the 30–500 range → CondProto/CurseProto. The combined bsId + sub-category output makes the PC curse SAR (values [67,53]) visually distinct from the many Conditions/PermanentMods SARs with values like [2,0] in the same fingerprint family.
- **`4:7:2` blessing SAR now correctly shows as `ProtoIdArray INT32[7]` in lifecycle detail**: with the switch to `AnnotateSarValue` in the lifecycle table, `4:7:2#3` (values `[1049,1051,1004,1017,1042,1025,1024]`, all >500) is annotated as `ProtoIdArray INT32[7] (bless/schematics)`, while `4:7:2#4–6` (values `[-1,-1,…]`) retain the ambiguous `Blessings×7 or NPC-dispatch INT32[7]` label. This separation required no additional code beyond the annotation-function swap.
- **Mode 13 is unchanged** — all findings from sessions 15–21 remain current. The session 22 improvements are entirely in the mode 9 lifecycle renderer.

Session 23 RE findings (Probe modes 9/13; Slot0170–0178):

- **Mode 9 transition sections now use value-aware annotations end-to-end**: `NEW`, `GONE`, and `MOVE` rows now use the same `AnnotateSarValue` classifications as lifecycle/`CHG` output instead of falling back to fingerprint-only labels. Verified on the rerun: `0171→0172 NEW` now labels the new `4:7:2` pair as `NPC-dispatch`, and `0174→0175 NEW` isolates the real blessing-growth row as `4:7:2[b3] (ProtoIdArray...)` alongside the other `4:7:2` additions.
- **The blessing `5→7` transition is now visible directly in the slot-pair header**: on `0174→0175`, the old blessing proto row appears in `GONE` as `4:5:2 (ProtoIdArray INT32[5])` while the replacement row appears in `NEW` as `4:7:2 (ProtoIdArray INT32[7])`. This makes the previously-verified blessing-count jump readable without consulting the lifecycle table.
- **Probe console output is now ASCII-safe in this environment**: mode headers, arrows, multiplicity markers, truncation, and the slot-pair summary prefix were normalized from Unicode (`→`, `×`, `…`, `Σ`) to ASCII (`->`, `x`, `...`, `SUM`). The `0170–0178` Mode 9 rerun and companion Mode 13 rerun both captured cleanly with no mojibake.

Session 24 editor/API + RE follow-up:

- **`SaveGameEditor.WithCharacter(...)` now matches its documented first-match semantics**: the old implementation rewrote every matching v2 character record in a `mobile.mdy`; it now stops after the first match, which makes predicate-based updates deterministic instead of fan-out edits.
- **`SaveGameEditor` now has a player-focused workflow**: `WithPlayerCharacter(...)` removes the manual `mobile.mdy` path plumbing for the common case, and `SaveAsync(...)` is now exposed directly at the editor-session layer in addition to the lower-level writer API.
- **Player edits now keep `.gsi` leader metadata aligned**: when the original player record is edited through `SaveGameEditor`, `LeaderName`, `LeaderLevel`, and `LeaderPortraitId` are synchronized from the pending player record before write, so the save-slot metadata no longer drifts behind the character payload.
- **Probe's 28-stat labels now align with the editor-side character model**: the earlier `PER/WIL` swap is corrected, index `16` is now labeled `MTApt`, index `21` is `unspent`, and indices `24..27` are `poisonLvl`, `age`, `gender`, and `race`. This closes the old `s21` / `s25` placeholder interpretation gap and removes the incorrect `RCE` label from the middle of the array.

--- GAP-3 partially closed in session 6: session-6 findings below.

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

**Session 6 update**: Live-save Probe RE (Slot0013 ArciMagus level-6) completed full SAR dump of the PC v2 record, mapping 300+ bsIds. New properties `Effects` (bsId=0x49FC) and `EffectCauses` (bsId=0x49FD) added with 5 new tests. `GoldHandleBsId=0x4D77` documented as a constant. Primary array bsIds (Stats=0x4299, BasicSkills=0x43C3, TechSkills=0x4A07, SpellTech=0x4A08) documented in code comments. Total test count: 267 (all passing). Bank money / reputation / quest / fog-of-war bsIds remain unknown pending a more advanced save (see GAP-3 session 6 table above).

**Session 7 update**: `Bullets` and `PowerCells` properties implemented using `GameStatsBsId=0x4D68` elements [11] and [12] respectively (eCnt 12 and 13 for tech chars). `WithBullets(int)` and `WithPowerCells(int)` mutation methods added. GameStats matching condition relaxed from `eCnt == 11` to `eCnt >= 11 && eCnt <= 13`.

**Session 7 RE findings — bsId stability**:
- All bsIds in v2 records are session-specific — assigned at game-object creation time, not prototype definition time. Every new game start generates entirely different bsIds for all SARs across the whole record (Stats, BasicSkills, Gold, GameStats, Portrait, HP, Fatigue — all of them).
- The "known" bsIds (0x4D68, 0x4B13, 0x4DA4, 0x4299, 0x43C3, 0x4046, etc.) are specific to the ArciMagus "test10" playthrough (Slot0013/0014 only). Saves Slot0015–Slot0178 are from different game sessions with completely different bsIds and will report Gold=0, Arrows=0, HP=0 from the extended scanner.
- The element-count signature scanner for the 4 primary arrays (Stats/BasicSkills/TechSkills/SpellTech) works across sessions because it matches by unique element count, not bsId. This is the only session-agnostic path.
- **Quest state**: Strong structural match found: `eSize=16 bcCnt=37` SAR present in all tested saves (Slot0100: bsId=0x45C7 eCnt=34; Slot0120: bsId=0x6AFD eCnt=46). The growing eCnt tracks quest progress. The bsId varies by session but the `eSize=16, bcCnt=37` fingerprint is stable.
- **Bank money**: NOT present as a direct INT32[1] SAR in non-Session-A saves. In those sessions, gold/money appears to be stored only in inventory items (Gold objects). The bsId=0x4B13 gold cache is absent or session-specific.
- **Reputation candidate**: An INT32[19] SAR is present in all tested saves with TotalKills at [0] and a level-correlated value at [10]; structure suggests a counter-set or reputation array but exact field assignment is unconfirmed.

**Session 8 update**: Quest state and Reputation fully implemented by structural fingerprinting. Quest: `eSize=16, bcCnt=37` (stable); exposes `QuestCount`, `QuestDataRaw`, `QuestBitsetRaw`, `WithQuestDataRaw`, `WithQuestStateRaw`. Verified Slot0013: QuestCount=9, Slot0120: QuestCount=46. Reputation: `eSize=4, eCnt=19, bcCnt=3` (PC field bit 130); exposes `ReputationRaw` (INT32[19]); absent in early saves (Slot0013), present in later saves (Slot0120: `[1031,17068,40,28345,1,30617,150,27355,-750,17130,100,17110,-77,30286,3,30286,3,17307,3]`). 12 new tests added; total test count: 279 (all passing).

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
| `CharacterMdyRecord` With* mutations (secondary fields) | **~90%** (GAP-3 session 15: `ReputationFactionSlots` property + quest-state bitmask discovery; session 12+13: Blessings/Curses/Schematics added; session 9: Rumors; session 8: Quest+Reputation; session 6: Effects+EffectCauses; session 5: HP/fatigue/positionAI/MaxFollowers; session 27: `WithReputationRaw` added) |
| `CharacterRecord` extended fields (quest/rep/bless/curse/schematics/rumors) | **100%** (session 27: all 6 extended field groups exposed via properties + Builder.With* + ApplyTo) |
| `.jmp` jump points parse + write | **100%** |
| `.prp` map properties parse + write | **100%** |
| `.tmf` town-map fog parse + write | **100%** |
| `data.sav` typed load/edit/save | **100%** (sessions 65+68+69 — `DataSavFormat`, `SaveGame.DataSavFiles`, `LoadedSave.DataSavFiles`, `SaveGameEditor.WithDataSav`, structural quad/remainder range helpers, and copy-on-write builder batching) |
| `data2.sav` typed load/edit/save | **100%** (sessions 58+66+67+69 — `Data2SavFormat`, `SaveGame.Data2SavFiles`, `LoadedSave.Data2SavFiles`, `SaveGameEditor.WithData2Sav`, structural `PrefixIntCount` / `SuffixIntCount`, contiguous `CopyPrefix/CopySuffix`, `WithPrefix/WithSuffix` single-int + range helpers, and copy-on-write builder batching) |
| Save-slot `.mes` override typed load/edit/save | **100%** (session 56 — `LoadedSave.Messages` + `SaveGameEditor.WithMessageFile` + `SaveGameUpdates.UpdatedMessages`) |
| Top-level unresolved raw save-global file round-trip (unknown blobs) | **100%** in both editor and format layers |
| Object property wire-type dispatch (vanilla 0x08) | **~95%** (some high bits untested) |
| Object property wire-type dispatch (arcanum-CE 0x77, bits ≤152) | **~90%** (GAP-4) |
| Object property wire-type dispatch (arcanum-CE 0x77, bits >152) | **0%** (GAP-4) |
| Typed object model (ObjectPc, ObjectNpc, etc.) R/W | **100%** |
| MobData ↔ GameObject bridge | **100%** (GAP-2 ✅ closed) |
| Save archive orchestration (SaveGame aggregate) | **100%** (GAP-1 ✅ closed — ArcNET.Formats + ArcNET.Editor) |
| Save engine version detection | **100%** (GAP-8 ✅ closed — `SaveGame.EngineVersion`) |
| Save creation from scratch | **100%** (GAP-6 ✅ closed — `CharacterMdyRecordBuilder` + `SaveGameBuilder`) |
| OID_TYPE_A write in handle arrays | **100%** (GAP-7 ✅ closed) |

**Overall save read/write completeness (low-level formats)**: ~98%  
**Consumer-facing save API completeness**: ~98% (GAP-1, GAP-2, GAP-5, GAP-6, GAP-7, GAP-8 closed; `data.sav` and `data2.sav` now have typed editor surfaces; GAP-3 semantic field identification remains open; GAP-4 open)

**Session 12+13 tooling**: Probe mode 10 full SAR element dump with optional bsCnt filter. Mode 9 lifecycle diff (Slot0120→0177) used to confirm blessing N-growth (5→7) and identify schematics as standalone `4:K:2`. V2Magic false-positive fix applied to `CharacterMdyRecord.Parse`; 9 new structural-fingerprint tests added.

**Session 14 tooling**: Mode 9 element-level diff engine and Mode 13 character field evolution tracker added. See [SarUtils.cs](../src/Probe/SarUtils.cs) and [Program.cs](../src/Probe/Program.cs).

**Session 15 tooling**: Mode 13 slot arg-padding bug fixed; Mode 13 extended with `hp_dmg`, `fat_dmg`, per-faction `rep[i]` diff tracking; Mode 14 reputation output now shows actual bitset-slot IDs via `CharacterMdyRecord.ReputationFactionSlots`; `4:7:2` annotation updated to distinguish Blessings from NPC dispatch-table. New Slot0178 (lv=45 cheat) analyzed: quest-state bitmask `0x100` / `0x102` discovered (undocumented).

**Session 16 tooling**: Mode 9 fingerprint lifecycle is now occurrence-aware (`#2`, `#3`, …) and same-size SAR diffs include bitset-slot movement. Mode 13 now reports quest-state transitions even when quest counts do not change and keys reputation deltas by the real faction slot IDs. `SarUtils.DecodeReputation()` now uses parsed SAR bit slots instead of raw element indices, and Mode 14 uses the shared quest-state formatter for `0x003`, `0x100`, and `0x102`.

**Session 17 tooling**: Probe now resolves quest labels from loose `.mes` files or DAT archives. On the current retail install it finds **161** labels in `modules/Arcanum.dat:mes/gamequestlogdumb.mes` and ignores the one-line `Module template` quest files in `Arcanum3.dat`. Modes 13 and 14 now print quest additions and state transitions with names when this lookup is available.

**Session 18 tooling**: Mode 9 duplicate-group matching is now similarity-based for repeated fingerprints. Matching uses bitset overlap plus INT32 value similarity with a threshold, lifecycle rows are built from adjacent-slot matches instead of raw occurrence order, reordered duplicate matches are labeled as `aN→bM`, and the score-only pseudo-`CHG` noise from the first similarity pass was removed.

**Session 19 tooling**: Mode 9 now emits reorder-only duplicate movement as `MOVE:` summaries and reserves `CHG:` for payload or bitset-slot deltas. The Slot0170–0178 rerun stays rich enough for RE work, but the late-game duplicate churn is no longer interleaved with the real state changes that match Mode 13 and Mode 14.

**Session 20 tooling**: Mode 9 now begins with a per-fingerprint lifecycle summary that shows slot span, multiplicity among populated slots, total track count, recurring-track count, one-slot count, and changed-track count. One-slot lifecycle rows are suppressed from the detail table with an explicit omitted-row count, and each slot-to-slot diff now prints `Σ: new/gone/move/chg` plus top `MOVE fp` / `CHG fp` fingerprint tallies.

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
- `ArcNET.Editor.LoadedSave` + `SaveGameEditor` + `SaveGameLoader` + `SaveGameWriter` implemented as the flat-dictionary editor layer.

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
**Session 6 update**: 5 new tests + `Effects`/`EffectCauses` properties added to `CharacterMdyRecord`. All bsIds from the PC v2 record mapped via live-save Probe dump. `EffectsBsId=0x49FC`, `EffectCausesBsId=0x49FD` constants added; `GoldHandleBsId=0x4D77` documented. Stats/BasicSkills/TechSkills/SpellTech bsIds documented as comments. Total test count: 267 (all passing).

**Session 11 tooling update**: Probe project refactored from 4366 lines to ~800 lines (82% reduction).
- `SarUtils.cs` extracted: `ParseSars`, `FormatElements`, `FindPlayerRecord`, `AnnotateFingerprint`, `AnnotateBsId` shared across all SAR modes.
- `BinaryDiff.cs` extracted: `FindDiffRegions` (gap-merge + context), `PrintHexDiff` (side-by-side `[XX]` format), `CompareInnerFiles`.
- ~3000-line unconditional research block removed (all findings already documented here).
- **Mode 11** added: `probe 11 <slotA> <slotB>` — binary diff of all inner files between two save slots, with SAR-level diff for changed `mobile.mdy` files.
- **Mode 12** added: `probe 12 [slot4]` — on-demand diagnostics (inner files, PC info, type distribution, player v2 summary).
- `testMode` now parsed before `slot4` (fixes argument parsing order bug for modes 7/10).

**Session 14 tooling update**: Probe diffing engine and `SarUtils.cs` significantly enhanced.
- `SarEntry.FirstVals` now reads all elements for `eSize=4` SARs (up to 512 ints instead of 4), giving the diff engine full element coverage.
- `SlotSnapshot` now carries a `CharacterMdyRecord? Character` field so Mode 9 and Mode 13 can use decoded properties without re-parsing.
- `SarUtils.CompareElements(SarEntry, SarEntry)` added: returns `(index, oldVal, newVal)` list for all differing INT32 elements between two same-eCnt SARs.
- `StringExtensions.TruncateAnnotation()` added: clips annotation labels to 12 chars for compact diff output.
- `AnnotateFingerprint()` extended: `4:N:4` pattern now annotated as `Conditions/PermanentMods INT32[N] (bsCnt4)` (previously fell through to the generic wildcard).
- **Mode 9 element-level diff** (`probe 9 [first [last]]`): slot-by-slot diff now shows per-element changes for matching INT32 SARs — `CHG: 4:28:2 [Stats] [17]:36→37 [18]:450000→512000` — instead of only detecting fingerprint presence/absence. Displays up to 12 element diffs inline with `+Nmore` for the rest. SAR annotation labels truncated to 12 chars to keep lines compact.
- **Mode 13 field evolution** (`probe 13 [first [last]]`): new mode. Iterates all save slots in a range, compares each to the previous, and prints one timestamped delta line per slot where any tracked field changed. Silent for unchanged slots. Tracked fields: `lv`, `XP`, `align`, `fate`, `magicPts`, `techPts`, `gold`, `quests`, `rumors`, `blessings`, `curses`, `schematics`, stat-array deltas (`baseStats[...]`), basic-skill deltas (`skills[...]`), and all 25 `SpellTech` discipline ranks by name (Conv, Div, Air, Earth, Fire, Water, Force, Mental, Meta, Morph, Nature, NecroBlk, NecroWht, Phantasm, Summon, Temporal, MasteryCol, Herb, Chem, Elec, Explos, Gun, Mech, Smithy, Therap). Marks level-up slots with `*** LEVEL UP ***`. Useful for tracing exact level-up events, spell acquisition order, alignment drift, and meta/tech point accumulation across a long playthrough.

**Session 15 tooling update**: Probe modes 13/14 extended; `SarUtils.cs` and `CharacterMdyRecord.cs` updated.
- **Mode 13 slot arg-padding bug fixed**: `probe 13 13 178` previously failed with `FileNotFoundException` because `args[1]="13"` produced slot stem `"Slot13"` instead of `"Slot0013"`. Fixed: `slot4 = args[1].PadLeft(4,'0')[..4]`; mode 13 added to load-exclusion guard (`testMode is not (9 or 11 or 13)`).
- **Mode 13 extended — HP/fatigue/reputation tracking**: Adds `hp_dmg`, `fat_dmg` (damage SAR element[0] for each), and `rep[i]:old→new` per-faction reputation diffs to the change delta. Baseline output line now includes `hp_dmg=X fat_dmg=Y rep=[slot0=V,…]`. Example output from Slot0170→0178 run:
  - `[Slot0174]: rep[0]:1419→1424  rep[5]:30617→30616  rep[6]:150→500`
  - `[Slot0175]: rep[0]:1424→1457  rep[1]:27365→30438  rep[2]:45→48  rep[11]:28359→17104  rep[12]:-80→-100  bless:5→7`
  - `[Slot0178]: lv:3→45  XP:+975000  align:0→100  MasteryCol:0→-1  *** LEVEL UP ***`
- **Mode 14 reputation output improved**: Each faction entry now displayed as `FactionSlot / Value` (e.g., `slot0=1471  slot1=30535`) using `CharacterMdyRecord.ReputationFactionSlots`. Previously showed raw sequential indices 0–18. Confirmed faction slot layout: `[0..12, 64..69]` (13 primary + 6 additional = 19 total).
- **`SarUtils.AnnotateFingerprint()` updated**: `4:7:2` annotation changed from vague `"HP-adj/Blessings INT32[7]"` to `"Blessings×7 or NPC-dispatch INT32[7]"` with inline disambiguation comment (Blessings: all values > 1000 i.e. proto IDs; NPC dispatch: mostly −1 with occasional negative memory addresses).
- **`SarUtils.DecodeReputation()` added**: `public static List<(int Slot, int Value)> DecodeReputation(SarEntry sar, int[] repRaw)` — pairs each element by index with its faction slot ID for display and analysis.
- **`CharacterMdyRecord.ReputationFactionSlots`** property added: reads bsCnt=3 bitset from `ReputationDataOffset + 19*4`, extracts all set bits as faction slot indices, returns `int[]?` (null when no reputation SAR). Returns `[0,1,2,3,4,5,6,7,8,9,10,11,12,64,65,66,67,68,69]` for all fully-progressed saves tested.
- **Open investigation — quest state bitmask**: Quest state `0x100` and `0x102` observed in Slot0178 (4 and 3 quests respectively). Current `WithQuestStateRaw` treats state as a raw value; no code change required. Bit semantics: `0x01`=triggered/in-progress, `0x02`=primary completed, `0x04`=secondary completed, `0x100`=unknown (failed/botched?). Session 17 resolved the quest-label source to `modules/Arcanum.dat:mes/gamequestlogdumb.mes` and disproved the earlier Mastery-College-only hypothesis, but the semantic meaning of bit 8 itself remains open.

**Session 16 tooling update**: Probe diff/evolution output refined; reputation decode bug fixed.
- **`SarEntry.BitSlots` added in `SarUtils.cs`**: `ParseSars()` now records the set-bit indices from each SAR bitset instead of throwing them away after `bsId` extraction. This lets later analysis distinguish identical `eSize/eCnt/bsCnt` packets that target different logical slots.
- **Mode 9 duplicate-fingerprint pairing fixed**: lifecycle and per-slot diff now group SARs by fingerprint and compare them by occurrence order inside each group. Output labels repeated records as `fingerprint#2`, `fingerprint#3`, etc. instead of collapsing them to the first match.
- **Mode 9 bitset-delta reporting added**: when two same-fingerprint SARs have unchanged element counts but different bitset membership, the `CHG` line now prints `slots:[old]→[new]`. Example from late-game runs: some `4:2:2` records now show `slots:[0,1]→[3,4]`, which the old diff engine silently treated as a generic same-shape change.
- **Mode 13 quest delta logic generalized**: it no longer waits for `QuestCount` to change. `QuestChanges13(...)` now compares quest proto/state maps directly and emits `q<proto>:oldState→newState` transitions alongside `quest+[]` / `quest-[]` additions and removals.
- **Mode 13 reputation diff corrected to use faction slot IDs**: `BuildRepMap13(...)` pairs `ReputationRaw` with decoded SAR bit slots, so deltas now read `rep[0]:1424→1457` / `rep[11]:28359→17104` in terms of the actual faction slot numbers rather than the packed element indices.
- **Shared quest-state formatter added**: `SarUtils.FormatQuestState(int)` now renders both known low bits and the currently-unknown `0x100` bit, e.g. `active|completed(primary) [0x003]`, `bit8? [0x100]`, `completed(primary)|bit8? [0x102]`. Mode 14 uses this formatter for the quest book dump; `CharacterMdyRecord.QuestEntries` comment updated to describe the field as a bitmask with observed late-game `0x100` values.
- **`SarUtils.DecodeReputation()` fixed**: the helper now prefers the parsed SAR bit slots over the caller's array index. Session-15 Mode 14 output was visually correct because `CharacterMdyRecord.ReputationFactionSlots` already existed, but the helper itself was still wrong; session 16 fixes the helper so all callers get the same slot-aware behavior.

**Session 18 tooling update**: Mode 9 matching and lifecycle tracking were refined again after the occurrence-order approach still produced false late-game diffs.
- **Similarity-based duplicate matching**: `SarUtils.MatchSarGroups(...)` now pairs repeated fingerprints by bitset overlap plus INT32 value similarity instead of raw ordinal position.
- **Lifecycle rows now follow adjacent-slot continuity**: repeated fingerprints such as `4:5:2`, `4:7:2`, `4:12:2`, and `4:28:2` are tracked from one slot to the next using the same matcher, which prevents unrelated rows from being stitched into one synthetic history.
- **Duplicate reorders are explicit in the diff**: Mode 9 labels duplicate matches as `aN→bM` when a row is matched to a different ordinal in the next slot, making record-order churn visible instead of implicit.
- **False `CHG` noise removed**: the first similarity pass surfaced score-only matches as changes even when no value, slot, or reorder delta existed; the final pass only emits `CHG` when there is a real element delta, bitset-slot move, or duplicate-row reorder.

**Session 19 tooling update**: the late-game rerun showed that many repeated `4:2:2`, `4:4:2`, and `24:2:2` packets in the player v2 record are real packets with valid bitsets, so the next refinement stayed in the renderer instead of the parser.
- **Reorder-only duplicate matches now use `MOVE:`**: Mode 9 no longer prints duplicate-row churn as `CHG` when the matched packets are content-identical and only changed ordinal position.
- **`CHG:` stays semantic**: payload changes and bitset-slot movement remain under `CHG`, so the output lines that survive are the ones that match Mode 13/14 findings.
- **Late-game review is materially cleaner**: Slot0174→0175 and Slot0175→0176 still show heavy duplicate-row movement, but the console now separates that movement from reputation, quest, blessing, and other actual field deltas.

**Session 20 tooling update**: the remaining weakness after session 19 was the lifecycle table itself: it still dumped every transient duplicate row, so the user had to visually infer where the churn was concentrated.
- **Lifecycle summary added**: Mode 9 now emits a per-fingerprint summary before the detail rows. The summary reports `slot span`, `dup@present`, `tracks`, `multi`, `one`, and `chg`, which turns the late-game duplicate storm into a ranked list of the actual noisy fingerprints.
- **One-slot orphans are suppressed, not deleted**: the detailed lifecycle table now omits one-slot rows and prints how many were suppressed. On the Slot0170–0178 rerun this collapsed 137 low-signal rows while keeping the recurring tracks and changed tracks visible.
- **Slot-pair churn is quantified**: each transition line now includes a `new/gone/move/chg` summary and top fingerprint-count summaries for `MOVE` and `CHG`, so spikes like `0174→0175` and `0177→0178` can be triaged before reading the per-row details.

**Session 21 tooling update**: the late-game output was readable enough to start distinguishing payload change from field identity, so the next pass focused on labels and noise suppression rather than matching.
- **CHG rows now show named element labels**: Stats, BasicSkills, TechSkills, and SpellTech diffs use `SarUtils.GetElementLabel(...)`, so output reads `[STR]`, `[DEX]`, `[XP]`, `[Conv]`, etc. instead of raw numeric indices.
- **Pointer-only element churn is suppressed**: `SarUtils.IsPointerLike(...)` and `PartitionElementDiffs(...)` now drop pointer-to-pointer diffs from `CHG` lines and append a suppression note instead.
- **Mode 13 now includes base stats and basic skills**: reruns print `baseStats[...]` and `skills[...]` deltas alongside the previously tracked spell, quest, rumor, and reputation changes.

**Session 22 tooling update**: mode 9 lifecycle detail was upgraded from fingerprint-only rows to track rows that expose the session-local identity hints without pretending bsIds are cross-session stable.
- **Lifecycle rows now show `bsId` and value-aware annotation**: the detail table prints a `bsId` column (`0xNNNN` or `varies`) plus the `AnnotateSarValue(...)` label for each multi-slot track.
- **Discontinuous slot boundaries are flagged**: `[DISC]` now appears when the next snapshot drops more than 3 levels, marking likely state-switch boundaries.
- **`4:2:2` tracks are sub-categorized**: `AnnotateSarValue(...)` now distinguishes `CondFlag`, `CondProto/CurseProto`, and the broader `Conditions/Curse` fallback for the duplicate-heavy two-int packet family.

**Session 23 tooling update**: the remaining gap after session 22 was that the transition header still fell back to fingerprint-only labels, and the terminal capture still mangled several Unicode console glyphs.
- **`NEW` / `GONE` / `MOVE` now use `AnnotateSarValue(...)`**: the slot-pair header itself now highlights `ProtoIdArray`, `NPC-dispatch`, `CondFlag`, and `CondProto/CurseProto` rows.
- **Probe console strings are now ASCII-safe**: current mode 9 / 13 / 10 / 12 / 14 / help output uses `->`, `x`, `...`, and `SUM:` instead of the Unicode glyphs that were rendering as mojibake in this environment.

**Session 24 tooling update**: this pass tightened the editor session API and aligned Probe's stat-array vocabulary with the editor-side v2 model.
- **`SaveGameEditor` now exposes the common player-edit path directly**: `WithPlayerCharacter(...)` and `SaveAsync(...)` sit on top of the existing `LoadedSave` + `SaveGameWriter` pipeline, so callers no longer need to rediscover the player's `mobile.mdy` path for routine edits.
- **Player writes now synchronize `.gsi` leader metadata**: `LeaderName`, `LeaderLevel`, and `LeaderPortraitId` are derived from the pending player record when `SaveGameEditor` saves a player edit.
- **Probe's `4:28:2` labels now use the same 28-stat map as `CharacterRecord`**: the old `PER/WIL` swap is gone, the placeholder `s21` / `s25` labels are replaced with `unspent` / `age`, and the remaining tail fields are labeled `poisonLvl`, `gender`, and `race`.

**Session 25 tooling update**: session 24 could keep leader metadata aligned automatically, but callers still had no ergonomic way to edit the rest of `.gsi` without dropping to the lower-level writer APIs.
- **`SaveInfo` now has a lightweight copy/update helper**: `SaveInfo.With(...)` provides record-style field replacement for module, display, time, location, story, and leader metadata fields without introducing a separate builder type.
- **`SaveGameEditor` now exposes explicit metadata queue + inspection APIs**: `WithSaveInfo(...)`, `GetCurrentSaveInfo()`, and `GetPendingSaveInfo()` let callers stage `.gsi` changes directly on the editor session.
- **Explicit `.gsi` edits compose with player sync**: `DisplayName`, time, map, tile, and story-state edits remain queued as requested, while `LeaderName`, `LeaderLevel`, and `LeaderPortraitId` still follow the pending player record whenever that record is edited in the same session.

**Session 26 editor API update**: this pass closed the last practical gaps in the `CharacterRecord`/`Builder` editing surface.
- **`CharacterRecord` now exposes `Bullets` and `PowerCells`**: both properties are decoded from `CharacterMdyRecord` (which already had them from session 7), propagated through `From(...)`, round-tripped via `ApplyTo(...)` using `WithBullets`/`WithPowerCells`, and exposed on `CharacterRecord.Builder.WithBullets(int)` / `WithPowerCells(int)`. On magic-focused characters where the underlying GameStats SAR has fewer than 12 elements the mutations are silently no-ops (matching the existing `CharacterMdyRecord.WithBullets` contract).
- **`CharacterRecord.Builder` now exposes derived-stat setters**: `WithCarryWeight`, `WithDamageBonus`, `WithAcAdjustment`, `WithSpeed`, `WithHealRate`, `WithPoisonRecovery`, `WithReactionModifier`, `WithMaxFollowers`, and `WithMagickTechAptitude` were always stored internally (stats array indices 8–16) but had no public `With*` methods. They are now fully addressable without constructors or raw array copies.
- **Scalar HP / fatigue convenience methods added**: `Builder.WithHpDamage(int)` patches only element [3] of the HP SAR and `Builder.WithFatigueDamage(int)` patches only element [2] of the fatigue SAR, preserving the other three elements. The previous raw-array forms (`WithHpDamageRaw`, `WithFatigueDamageRaw`) remain available.
- **Probe Mode 15 (`npc-scan`) added**: `probe 15 <slot4> [all]` lists every v2 character record in every `mobile.mdy` file of a save slot — PC-complete records always shown, NPC-only (incomplete) records shown when the `all` flag is passed. Output includes level, XP, race/gender, alignment, gold, magic/tech points, ammo, HP/fatigue damage, and a non-zero basic-skills digest.
- **Mode 13 (`field-evolution`) now tracks Bullets and PowerCells**: the evolution tracker compares `character.Bullets` and `character.PowerCells` between successive snapshots and emits `bullets:A->B` / `powerCells:A->B` delta lines when they change. The baseline print and the tracked-field header comment both updated.
- **9 new editor tests added**: `Builder_WithHpDamage_SetsElement3_PreservesOthers`, `Builder_WithHpDamage_WhenRawIsNull_CreatesFreshArray`, `Builder_WithFatigueDamage_SetsElement2_PreservesOthers`, `Builder_WithMaxFollowers_RoundTrips`, `Builder_WithMagickTechAptitude_RoundTrips`, `CharacterRecord_Bullets_DefaultsToZero_OnMagicChar`, `CharacterRecord_PowerCells_DefaultsToZero_OnMagicChar`, `Builder_WithBullets_SetsField_RetainedInRecord`, `Builder_WithPowerCells_SetsField_RetainedInRecord`. All 151 editor tests passing.

**Session 27 editor API update**: this pass exposed the extended v2 record fields (quest, reputation, blessings, curses, schematics, rumors) at the high-level `CharacterRecord` editor layer and added a full character summary Probe mode.
- **`CharacterRecord` now exposes all extended v2 character fields**: `QuestCount`, `QuestDataRaw`, `QuestBitsetRaw`, `ReputationRaw` (INT32[19]), `BlessingProtoElementCount`, `BlessingRaw`, `BlessingTsRaw`, `CurseProtoElementCount`, `CurseRaw`, `CurseTsRaw`, `SchematicsElementCount`, `SchematicsRaw`, `RumorsCount`, and `RumorsRaw`. These were already decoded at the `CharacterMdyRecord` format layer (sessions 8–15) but were never bridged to the editor model.
- **`CharacterRecord.Builder` has matching `With*` methods for all new fields**: `WithQuestDataRaw(byte[])`, `WithQuestBitsetRaw(int[])`, `WithReputationRaw(int[])`, `WithBlessingRaw(int[])`, `WithBlessingTsRaw(byte[])`, `WithCurseRaw(int[])`, `WithCurseTsRaw(byte[])`, `WithSchematicsRaw(int[])`, `WithRumorsRaw(byte[])`.
- **`CharacterRecord.ApplyTo(CharacterMdyRecord)` now applies all 6 extended field groups**: quest state, reputation, blessings, curses, schematics, and rumors are conditionally written back when their corresponding properties are non-null, using the existing `WithQuestStateRaw`, `WithReputationRaw`, `WithBlessingRaw`, `WithCurseRaw`, `WithSchematicsRaw`, and `WithRumorsRaw` mutation methods.
- **`WithReputationRaw` added to `CharacterMdyRecord.Mutations.cs`**: replaces the 19 faction-reputation INT32 elements in-place; requires exactly `ReputationSarElementCount` (19) values; no-ops when `ReputationDataOffset < 0`.
- **`TryFindPlayerCharacter` in `SaveGameEditor` improved**: now prefers `HasCompleteData && Name != null` as the primary predicate and falls back to `HasCompleteData` alone. This prevents companion NPCs (which also have all 4 primary SAR arrays) from being returned as the player in saves with party members.
- **Finding — PC Name is absent from v2 `mobile.mdy` records in UAP saves**: Mode 15 (`npc-scan`) shows all records as `(no name)` in UAP test saves. The PC name comes exclusively from the `.gsi` `LeaderName` field. Reliable player detection for RE tools must use `SarUtils.FindPlayerRecord` (QuestCount > 0 heuristics), not the name field.
- **Probe Mode 16 (`char-summary`) added**: full character summary using `SarUtils.FindPlayerRecord` for correct PC detection. Output sections: PRIMARY ATTRIBUTES, DERIVED STATS, PROGRESSION, BASIC SKILLS, TECH SKILLS, SPELL COLLEGES (non-zero), TECH DISCIPLINES (non-zero), QUEST LOG (count + raw/bitset byte counts), REPUTATION (non-zero faction values), BLESSINGS (proto IDs), CURSES (proto IDs), SCHEMATICS (proto IDs), RUMORS (count + raw byte count). Verified against Slot0178: 78 quests, 7 blessings `[1049,1051,1004,1017,1042,1025,1024]`, 2 curses `[67,53]`, 4 schematics `[5090,4810,4010,5410]`, 116 rumors, 19 faction reputation array.
- **9 new editor tests added**: `CharacterRecord_From_ExposesQuestCount`, `CharacterRecord_From_ExposesReputation`, `CharacterRecord_From_ExposesBlessings`, `CharacterRecord_From_ExposesSchematicsAndRumors`, `Builder_WithReputationRaw_RoundTrips`, `Builder_WithQuestDataRaw_RoundTrips`, `Builder_WithSchematicsRaw_RoundTrips`, `Builder_WithRumorsRaw_RoundTrips`, `ApplyTo_WithReputation_PreservesReputationInRoundTrip`. All 160 editor tests passing.

**Sessions 28-48: ValueBuffers 0.9.0 adoption (no save API surface changes)**
- Internal refactoring only: all dumpers (`ArtDumper`, `JmpDumper`, `MessageDumper`, `FacWalkDumper`, `SectorDumper`, `TerrainDumper`, `SaveInfoDumper`, `MapPropertiesDumper`, `DialogDumper`, `ItemDumper`, `MobDumper`, `ScriptDumper`), `GameDataSaver`, and `Probe/SarUtils.cs` now use the `Bia.ValueBuffers` 0.9.0 API (`ValueStringBuilder`, `ValueByteBuffer`, interpolated-string handler, bounded `AppendEnclosedJoin`, `scoped`-span helper boundaries).
- `SarEncoding.BuildSarBytes` already used `ValueByteBuffer`; remaining heap allocation in hot analysis paths removed.
- Save API surface unchanged. Test counts confirmed: Core 13/13, Formats 294/294, Archive 10/10, GameData 52/52, Editor 160/160.
- `Bia.ValueBuffers` pinned at `0.9.0` in `src/Directory.Packages.props`. Only live upstream ask: `P8` (ref-helper interpolation boundary).
- See `tasks/ValueBuffersAdoptionReview.md` for the full adoption inventory.

**Session 49: probe verification + doc update (no new code changes)**
- All probe modes (13, 16) verified against Slot0170-0178 post-ValueBuffers-adoption; output matches session 21/27 findings exactly.
- Mode 13 confirmed: field-evolution traces for lv/XP/align/fate/gold/quests/quest-state/rumors/blessings/curses/schematics/hp\_dmg/fat\_dmg/bullets/powerCells/rep/SpellTech/baseStats/skills all correct.
- Mode 16 confirmed: Slot0178 char-summary shows 78 quests, 7 blessings, 2 curses, 4 schematics, 116 rumors, 19 faction reputation, all matching prior sessions.
- GAP-3 and GAP-4 remain blocked on RE (no story-progression or arcanum-CE saves available in test corpus).

**Session 50: probe mode 17 — data.sav RE dump**
- Added probe mode 17 (`pc-data`) as `data.sav` hex + INT32 dump for GAP-3 reverse-engineering.
- mobile.md PC records are ALL compact (propCollItems=0); no ObjectPc fields (GlobalFlags/BankMoney/etc.) stored there.
- CharacterRecord (mobile.mdy) already covers gold, HP/fatigue, quests, blessings, curses, schematics, rumors, reputation, and portrait. Player name still comes primarily from `.gsi` `LeaderName` in UAP saves.
- **data.sav** (26,082 B, version=25): Dense binary (47.8% non-zero INT32s). Live mode 17 output shows an 8-byte header (`25`, `32`), 6,520 full INT32s plus 2 trailing bytes, and a leading preview dominated by repeated 4-int rows (`a`, `b`, `2072`, `d`) after the header. It appears to be world-object-delta / dif state, NOT a GlobalFlags array. No ASCII strings; no PrefixedString player name.
- **data2.sav** (1,972 B, version=25): Sparser. Contains faction IDs in 169–194 range (NPC reactions), BEEFCAFE sentinel pattern, and a later-confirmed alternating `[state, 50000+ id]` table. Session 57 mode 17 verification pinned that table down as `211` pairs spanning IDs `50000..55500` at ints `56..477` in normal late-game saves (`58..479` in the `Slot0171` outlier). It behaves like a compact save-global state table, but session-53 correlation and the newer pair-table diffing both ruled out a simple pure reputation-array or pure town-map-fog interpretation.
- **Finding**: GlobalFlags and GlobalVariables are NOT in mobile.md, mobile.mdy, data.sav, or data2.sav based on current analysis. Their storage format remains unknown (possibly in `.dif` sector-delta files or a yet-unidentified file).
- BankMoney and GlobalFlags/GlobalVariables remain blocked pending further `.dif` and data.sav full-scan analysis.

**Session 51: town-map fog format + mode 17 cleanup**
- Added `TownMapFog` / `TownMapFogFormat` in `ArcNET.Formats`: `.tmf` files now have explicit parse + write support as raw bit-arrays with revealed-tile / coverage helpers.
- Added typed editor-layer plumbing: `LoadedSave.TownMapFogs`, `SaveGameUpdates.UpdatedTownMapFogs`, and `SaveGameLoader` / `SaveGameWriter` support for `.tmf` files. New tests verify typed load + write through the editor save pipeline.
- Probe mode 17 was refactored off the temporary `StringBuilder` / `List<(int,int)>` path used in session 50. It now uses `ValueStringBuilder`, prints header ints, sentinel counts, and a 4-int preview after the 8-byte header, and no longer claims fog is stored in `data.sav` / `data2.sav`.
- Verified counts after the change: Formats `298/298`, Editor `162/162`, Probe build succeeds, and live mode 17 output for Slot0178 matches the corrected `data.sav` / `data2.sav` description above.

**Session 52: editor raw-file workflow + mode 17 range diff**
- `SaveGameEditor` now exposes a first-class raw embedded-file workflow for unresolved save-global files: `GetCurrentRawFile(path)`, `GetPendingRawFile(path)`, `WithRawFile(path, byte[])`, and `WithRawFile(path, update)`. These stage bytes through the existing `SaveGameUpdates.RawFileUpdates` path, compose with pending player / `.gsi` edits, and were verified by 4 new editor tests. Current editor test count: `166/166`.
- Probe mode 17 (`pc-data`) now accepts a slot range (`probe 17 <first> <last>`) and prints per-slot `data.sav` / `data2.sav` summaries plus consecutive diff statistics: size/header changes, non-zero density deltas, leading shared INT32 prefix length, changed-INT32 count, and hot changing INT32 indices.
- Verified late-game range `0170-0178`:
  - `data.sav` is structurally volatile across consecutive slots. Header word 1 changes across the range (`19`, `32`, `9`, `31`, `31`, `32`, `32`, `32`, `32`), changed-INT32 counts remain high (`1024-3697`), and most transitions share only `1-2` leading INT32s before diverging (`prefixInts=1/2`; `0173->0174` is the one large same-session exception at `1135`). This rules out a simple append-only log or flat global-flag array signature.
  - `data2.sav` is much more stable: header remains `25/0` across the full range, size stays `1972` bytes except `Slot0171` (`1980`), and stable-size transitions only change `2-18` INT32s. The hottest indices in this run are `[44]`, `[50]`, `[8]`, `[5]`, `[49]`, `[479]`, and `[480]`, which strengthens the “small fixed-layout state table” hypothesis over a large object-delta blob.
- GAP-3 remains open: the range diff did not surface any obvious flat indexed-array signature for `GlobalFlags`, `GlobalVariables`, or `BankMoney`. Current evidence still says `mobile.md` / `mobile.mdy` are not the source, `data2.sav` is too small/stable to look like the missing large flag payload, and `data.sav` appears to be broader save-global / world-delta state rather than a dedicated flag table.

**Session 53: editor town-map-fog workflow + mode 17 typed-context correlation**
- `SaveGameEditor` now exposes a first-class typed town-map-fog workflow: `GetCurrentTownMapFog(path)`, `GetPendingTownMapFog(path)`, `WithTownMapFog(path, TownMapFog)`, and `WithTownMapFog(path, update)`. These queue through `SaveGameUpdates.UpdatedTownMapFogs`, compose with pending player / `.gsi` / raw-file edits, and were verified by 4 new editor tests. Current editor test count: `170/170`.
- Probe mode 17 (`pc-data`) now prints decoded slot context beside the raw save-global summaries: player `quests`, `rumors`, `bless`, `curse`, `schem`, reputation presence, plus `.tmf` file count and total revealed tiles. Each slot-to-slot diff now also emits a `typed:` line summarizing count deltas, `repChanged`, and `tmfChanged` / revealed-tile deltas.
- Verified late-game range `0170-0178` with the new correlation output:
  - `.tmf` state changes are real but modest and sparse compared to the raw blob churn: `0172->0173` changed `2` fog files / `+31` revealed tiles, `0174->0175` changed `1` file / `+19` tiles, and `0175->0176` changed `3` files / `+93` tiles. Slot totals grow from `56` files / `72625` revealed tiles at `0170` to `60` files / `72779` tiles at `0176-0178`.
  - `data2.sav` still mutates on transitions where both decoded reputation and `.tmf` state are unchanged (`0177->0178`: `changedInts=3`, `repChanged=0`, `tmfChanged=0`). This rules out the stronger hypothesis that `data2.sav` is simply the reputation array or the town-map-fog mirror.
  - Reputation deltas do sometimes line up with `data2.sav` changes (`0173->0174`, `0174->0175`, `0175->0176`), so `data2.sav` may still contain some compact reputation-adjacent or world-state bookkeeping. The important narrowed conclusion is negative: it is not a pure typed mirror of either the 19-entry reputation table or the `.tmf` payloads.
- GAP-3 remains open: `GlobalFlags`, `GlobalVariables`, and `BankMoney` are still not identified in the tested corpus. Session 53 strengthens the current boundary: `mobile.md`, `mobile.mdy`, `.tmf`, and the decoded reputation array are not enough to explain the remaining `data.sav` / `data2.sav` mutations.

**Session 54: format-layer top-level save-global preservation**
- `ArcNET.Formats.SaveGame` now preserves the full non-map save surface instead of only module `.mes` overrides: typed top-level `.tmf` files round-trip through `TownMapFogs`, and unresolved save-global blobs round-trip through `RawFiles` with their original virtual paths.
- `SaveGameReader` no longer silently drops non-map / non-`.mes` payloads. `.tmf` is parsed via `TownMapFogFormat`; every other unresolved file is retained verbatim in `RawFiles`.
- `SaveGameWriter` now re-emits those files and rebuilds the TFAI root tree for both top-level files (`Tsen Ang.tmf`, `data.sav`, `data2.sav`) and arbitrary nested raw paths. This closes the last known preservation mismatch between the hierarchical `ArcNET.Formats` save aggregate and the editor-layer `LoadedSave` workflow.
- Added 2 focused format tests in `SaveGameBuilderTests.cs`: one for default empty top-level file collections and one proving `SaveGameWriter -> SaveGameReader` round-trip for `.tmf`, `data.sav`, `data2.sav`, and a nested raw path. Verified with Formats `300/300`, Editor `170/170` (one transient `LoadAsync_Progress_ReachesOne` timing failure did not reproduce on rerun), and Probe mode 17 over `0170-0178`.

**Session 55: editor raw-file boundary + mode 17 window localization**
- `LoadedSave` now exposes `RawFiles` as a first-class subset of `Files`, and `SaveGameLoader` populates it only for paths with no successful typed surface. In the current code that means unresolved save-global blobs (`data.sav`, `data2.sav`), unknown files, and typed-format files that failed to parse during load; successful `.mes` overrides moved to `LoadedSave.Messages` in session 56.
- `SaveGameEditor.GetCurrentRawFile(...)`, `GetPendingRawFile(...)`, and `WithRawFile(...)` now operate only on `LoadedSave.RawFiles`. This prevents accidental generic-byte edits to typed paths such as `mobile.mdy`, `.tmf`, and `.jmp` while preserving the lower-level `SaveGameUpdates.RawFileUpdates` escape hatch for explicit test/probe scenarios.
- Probe mode 17 (`pc-data`) was extended again for late-game `data2.sav` RE. In range mode it now detects and prints compact contiguous INT32 replacement windows when same-size diffs collapse to a localized edit, and the reporting path was tightened to match the current low-allocation `ValueStringBuilder` style instead of building intermediate summary strings/lists.
- Verified live on `0170-0178`: `data2.sav` now surfaces two real local replacement windows instead of only hot-index counts — `0172->0173` shows `window@5: 4->4 ints -[0,2,-1,1] +[1,2,-1,0]`, and `0176->0177` shows `window@44: 7->7 ints -[21,169,187,193,194,186,2] +[18,169,187,193,194,186,3]`. The stable middle values in the second window strengthen the current interpretation that `data2.sav` contains small fixed-layout records rather than a single flat reputation mirror.
- Negative result: `data.sav` still produced **no** compact insert/remove windows across `0170-0178`; its late-game transitions remain broad rewrites (`changedInts=1024-3697`, size swings `17082-51322` bytes). Current evidence still points to a large world-state / save-global blob rather than a tiny dedicated global-flags or bank-money table.
- Session 55 verification was limited to formatting + Probe by request: `dotnet format ArcNET.slnx --no-restore --include src/Probe/Commands/PcDataCommand.cs src/Probe/Commands/HelpCommand.cs src/Editor/ArcNET.Editor/LoadedSave.cs src/Editor/ArcNET.Editor/SaveGameLoader.cs src/Editor/ArcNET.Editor/SaveGameEditor.cs src/Editor/ArcNET.Editor/SaveGameUpdates.cs src/Editor/ArcNET.Editor/SaveGameWriter.cs src/Editor/ArcNET.Editor.Tests/SaveGameEditorTests.cs`, `dotnet csharpier format ...`, `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- 17 0170 0178`, and `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- 17 0178`. No test projects were rerun in this session.

**Session 56: typed save-message editor workflow + mode 17 slot traces**
- `LoadedSave` now includes typed `Messages`, `SaveGameLoader` parses successful `.mes` overrides into that dictionary, `SaveGameUpdates` adds `UpdatedMessages`, and `SaveGameWriter` writes them back through `MessageFormat`. Successful `.mes` loads are no longer exposed through `RawFiles`.
- `SaveGameEditor` now exposes `GetCurrentMessageFile(path)`, `GetPendingMessageFile(path)`, `WithMessageFile(path, MesFile)`, and `WithMessageFile(path, update)`, so save-slot message overrides can be staged alongside player, `.gsi`, `.tmf`, and raw save-global edits.
- Focused editor coverage now includes typed `.mes` load, exclusion from `RawFiles`, staged replacement, chained transforms, missing-path no-op, and save/load round-trip persistence. Current editor test project total: `179/179` passing.
- Probe mode 17 now traces each detected same-size localized `data2.sav` window across all loaded slots after the diff summary. Verified on `0170-0178`: the `[5..8]` trace toggles between `[0,2,-1,1]` and `[1,2,-1,0]`, while the `[44..50]` trace preserves a stable faction-style middle core across the late-game saves and makes `Slot0171` stand out as a structurally different outlier instead of part of the same late-game pattern.
- Mode 17 numeric output is now culture-stable: the captured single-slot and range artifacts use plain ASCII digits and `.` decimal formatting instead of locale-dependent renderings such as `26 082` or `47,8%`.
- Verification completed with `dotnet format ArcNET.slnx --no-restore --include src/Editor/ArcNET.Editor/LoadedSave.cs src/Editor/ArcNET.Editor/SaveGameLoader.cs src/Editor/ArcNET.Editor/SaveGameEditor.cs src/Editor/ArcNET.Editor/SaveGameUpdates.cs src/Editor/ArcNET.Editor/SaveGameWriter.cs src/Editor/ArcNET.Editor.Tests/SaveGameEditorTests.cs src/Editor/ArcNET.Editor.Tests/SaveGameTests.cs src/Probe/Commands/PcDataCommand.cs`, `dotnet csharpier format src/Editor/ArcNET.Editor/LoadedSave.cs src/Editor/ArcNET.Editor/SaveGameLoader.cs src/Editor/ArcNET.Editor/SaveGameEditor.cs src/Editor/ArcNET.Editor/SaveGameUpdates.cs src/Editor/ArcNET.Editor/SaveGameWriter.cs src/Editor/ArcNET.Editor.Tests/SaveGameEditorTests.cs src/Editor/ArcNET.Editor.Tests/SaveGameTests.cs src/Probe/Commands/PcDataCommand.cs`, `dotnet run --project src/Editor/ArcNET.Editor.Tests/ArcNET.Editor.Tests.csproj -c Release -p:CSharpier_Bypass=true` (`179/179`), `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- 17 0178 | Tee-Object tasks/session56_mode17_0178.txt`, and `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- 17 0170 0178 | Tee-Object tasks/session56_mode17_0170_0178.txt`.

**Session 57: mode 17 `50000+` ID pair-table decoder**
- Probe mode 17 now detects the large alternating `[state, 50000+ id]` table inside `data2.sav` instead of leaving that region as anonymous raw INT32 noise. The detector is structural: it finds the longest monotonic alternating run whose state values stay within `-1..32` and whose paired IDs stay within `50000..60000`.
- Verified on `0170-0178`: every late-game save carries a `211`-pair table spanning IDs `50000..55500`. The standard late-game layout is ints `56..477`; `Slot0171` shifts the same table to `58..479`, which cleanly matches its already-known outlier prefix without implying a different table schema.
- Single-slot mode now surfaces that structure directly. On `Slot0178`, `data2.sav` reports `50000+ ID pair table: ints=56..477, pairs=211, ids=50000..55500, nonZero=79, max=18`, plus the leading non-zero entries (`50200:2`, `50201:1`, `50300:2`, `50301:3`, `50302:1`, ...).
- Range mode now emits decoded pair-table deltas by `50000+` ID. The real late-game semantic change in this table is concentrated in `0174->0175`, where only five IDs changed: `52201:1->2`, `52203:1->2`, `52300:0->1`, `54002:4->5`, and `54102:4->5`.
- The negative result is equally useful: `0176->0177` and `0177->0178` change none of the `211` decoded pair-table IDs even though raw `data2.sav` still mutates. That cleanly separates the large `50000+` table from the smaller prefix windows (`[44..50]` and `[5..8]`), so the remaining unexplained churn is now localized outside the exploration-style pair table.
- No new typed editor API was added in this session. The new finding is good read-only RE evidence, but it is not yet a write-safe format model for `data2.sav`.
- Verification completed with `dotnet format ArcNET.slnx --no-restore --include src/Probe/Commands/PcDataCommand.cs`, `dotnet csharpier format src/Probe/Commands/PcDataCommand.cs`, `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- 17 0178 | Tee-Object tasks/session57_mode17_0178.txt`, and `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- 17 0170 0178 | Tee-Object tasks/session57_mode17_0170_0178.txt`.

**Session 58: typed `data2.sav` format + editor surface**
- Added `Data2SavFile` / `Data2SavFormat` in `ArcNET.Formats`. The parser reuses the verified structural detector for the alternating `[state, 50000+ id]` table, exposes that table as `IdPairs`, and preserves every unresolved byte in `RawBytes`. The writer patches only the decoded value slots and validates that the on-disk paired IDs still match the typed entries before writing.
- Wired `data2.sav` through the hierarchical save layer: `SaveGame` now carries typed `Data2SavFiles`, `SaveGameReader` parses top-level `data2.sav` into that collection, and `SaveGameWriter` writes it back through `Data2SavFormat` instead of treating it as an untyped raw blob.
- Wired the same typed surface through the editor layer: `LoadedSave.Data2SavFiles`, `SaveGameLoader`, `SaveGameUpdates.UpdatedData2SavFiles`, `SaveGameWriter`, and `SaveGameEditor.GetCurrentData2Sav(...)` / `GetPendingData2Sav(...)` / `WithData2Sav(...)` now provide a first-class edit path. Successful `data2.sav` parses are excluded from `RawFiles`.
- Added focused coverage for the new model and wiring: `Data2SavFormatTests` covers parse, unchanged round-trip, single-value patching, and missing-table rejection; formats total is now `304/304`. Editor / save-pipeline coverage now includes typed `data2.sav` load, exclusion from `RawFiles`, staged updates, chained updates, missing-path no-op behavior, and save/load round-trip persistence; editor total is now `186/186`.
- Probe mode 17 now reuses the shared typed parser through `LoadedSave.Data2SavFiles` instead of maintaining a separate decoder. Live verification still matches the session-57 RE findings: `Slot0178` reports the typed table at `ints=56..477`, `pairs=211`, `ids=50000..55500`, `nonZero=79`, `max=18`; the `0170-0178` range still shows the `Slot0171` outlier shift to `58..479`, only five decoded pair-ID changes on `0174->0175`, and no decoded pair-table changes on `0176->0177` or `0177->0178` even though the smaller prefix windows continue to mutate.
- The negative boundary remains open and unchanged: `data2.sav` is now a write-safe partial typed format for the verified `50000+` table, but the smaller prefix windows (`[5..8]`, `[44..50]`) and the broader `data.sav` payload still remain unresolved save-global state outside the current typed surface.
- Verification completed with `dotnet build src/Formats/ArcNET.Formats/ArcNET.Formats.csproj -c Release -p:CSharpier_Bypass=true`, `dotnet build src/Editor/ArcNET.Editor/ArcNET.Editor.csproj -c Release -p:CSharpier_Bypass=true`, `dotnet build src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true`, `dotnet run --project src/Formats/ArcNET.Formats.Tests/ArcNET.Formats.Tests.csproj -c Release -p:CSharpier_Bypass=true` (`304/304`), `dotnet run --project src/Editor/ArcNET.Editor.Tests/ArcNET.Editor.Tests.csproj -c Release -p:CSharpier_Bypass=true` (`186/186`), `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- 17 0178`, and `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- 17 0170 0178`.

**Session 61: mode 17 `data.sav` aligned-quad summary**
- Probe mode 17 (`pc-data`) now adds a bounded structural pass for `data.sav`: after the 8-byte header it treats the payload as aligned `INT32[4]` rows and prints row counts, distinct `(b,c,d)` signature counts, top repeated signatures, and longest contiguous runs. The implementation stays in the current low-allocation Probe style (`ValueStringBuilder`, bounded summaries only) rather than building large intermediate analysis objects.
- Live Slot0178 verification now exposes concrete row structure in the previously opaque blob. `data.sav` contains `1629` aligned quads plus `2` remainder ints and `587` distinct `(b,c,d)` signatures. The dominant signature is `b=0 c=0 d=0x00000000 x719` with longest runs `250`, `237`, `14`, and `11`; the second largest is `b=131072 c=131072 d=0x00020000 x224` with a `210`-row run; early non-zero clusters include `b=25 c=2072 d=0x0259A2C0 x16` and `b=18 c=2072 d=0x02441780 x8`.
- Range verification on `0170-0178` shows the row framing is stable across the late-game corpus even while file size changes heavily. Every `data.sav` remains quad-aligned after the header with row counts `1061..3207`, remainder ints `0..3`, distinct signature counts `185..1303`, and a dominant zero-signature run that stays about `250` rows wide (`250` for most slots, `251` in `Slot0171`). This materially strengthens the working interpretation that `data.sav` is a mixed-section record store with large reserved/zero spans, not a compact flag array or a free-form string/blob payload.
- The negative boundary remains: this session still did not identify `BankMoney`, `GlobalFlags`, or `GlobalVariables`, and it did not add a write-safe typed `data.sav` model. The useful new constraint is that future RE can now work against real aligned row sections instead of treating the file as undifferentiated raw noise.
- Verification completed with `dotnet csharpier format src/Probe/Commands/PcDataCommand.cs`, `dotnet build src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true`, `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- --out tasks/session61_mode17_0178.txt 17 0178`, and `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- --out tasks/session61_mode17_0170_0178.txt 17 0170 0178`.

**Session 62: mode 17 `data.sav` contiguous-section outline**
- Probe mode 17 (`pc-data`) now extends the aligned-quad pass from aggregate counts into ordered contiguous-section summaries. In addition to the existing signature histogram, it now reports total section count, zero-section count, longest zero-section start/length, bounded leading-section preview, bounded trailing-section preview, and a compact `quad16` section-delta line in range mode. The implementation stays within the current low-allocation Probe style: bounded arrays plus `ValueStringBuilder`, no unbounded per-row reporting.
- Live Slot0178 verification now exposes a concrete ordered front-matter layout for `data.sav`. After the 8-byte header the first six contiguous sections are: rows `0..2` `b=7 c=2072 d=0x02441780`, rows `3..10` `b=18 c=2072 d=0x02441780`, row `11` `b=18 c=2072 d=0x02559988`, row `12` `b=25 c=2072 d=0x0255C2E8`, rows `13..15` `b=25 c=2072 d=0x0257DEE8`, and rows `16..31` `b=25 c=2072 d=0x0259A2C0`. The file then enters its longest zero section at row `32` for `250` rows. Slot0178 totals: `733` contiguous sections and `66` zero-signature sections.
- Range verification on `0170-0178` shows that the dominant zero plateau remains structurally stable while the front-matter length shifts by save/session. Total section counts vary `224..1950`, zero-section counts vary `23..228`, but the longest zero section stays `250` rows wide in every late-game slot except the `Slot0171` outlier (`251`). Its start row tracks the variable front-matter length instead of random churn: `19` in `Slot0170`, `32` in `Slot0171` and `Slot0175-0178`, `9` in `Slot0172`, and `31` in `Slot0173-0174`. This sharpens the session-61 interpretation: `data.sav` behaves like a mixed-section record store with a variable-length tagged prefix feeding into large reserved/zero plateaus, not a flat compact flag array.
- The negative boundary remains unchanged: session 62 still did not identify `BankMoney`, `GlobalFlags`, or `GlobalVariables`, and it did not add a write-safe typed `data.sav` model. The useful new constraint is narrower search space: future RE can focus on the small non-zero front-matter sections and the late trailing tail packets rather than treating the whole file as uniformly opaque.
- Verification completed with `dotnet format ArcNET.slnx --no-restore --include src/Probe/Commands/PcDataCommand.cs`, `dotnet csharpier format src/Probe/Commands/PcDataCommand.cs`, `dotnet build src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true`, `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- --out tasks/session62_mode17_0178.txt 17 0178`, and `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- --out tasks/session62_mode17_0170_0178.txt 17 0170 0178`.

**Session 63: mode 17 `data.sav` front-matter section tracing**
- Probe mode 17 (`pc-data`) now adds a compact `front16` tracing surface for `data.sav`. Each slot summary prints the full ordered pre-zero front-matter section sequence as `b/c/d x len` runs, and each slot-to-slot diff now prints a `front16` delta line with pre-zero row count, section count, shared leading-section prefix length (`samePrefix`), and the first differing section on each side. The implementation stays in the current low-allocation Probe style: it reuses the existing aligned-quad run list and renders bounded summaries through `ValueStringBuilder`.
- Live Slot0178 verification now exposes the whole tagged prefix in one line: before the longest zero plateau, `data.sav` has `32` rows across `6` sections with sequence `7/2072/02441780 x3`, `18/2072/02441780 x8`, `18/2072/02559988 x1`, `25/2072/0255C2E8 x1`, `25/2072/0257DEE8 x3`, `25/2072/0259A2C0 x16`. This matches the longer ordered dump from session 62 while making the front-matter shape easy to compare across slots.
- Range verification on `0170-0178` shows that the pre-zero section sequence is stable only for `Slot0173 -> Slot0174`; every other adjacent transition changes from the very first section onward (`samePrefix=0`), even when the zero plateau still begins at row `32` (`Slot0175 -> Slot0178`). The late-game corpus therefore contains distinct front-matter families rather than one fixed prefix with a few local edits: `Slot0170` is a `c=2003` 4-section family, `Slot0171` is an 8-section low-`c` (`61/80`) outlier, `Slot0172` collapses to a single `0/2033/... x9` section, `Slot0173-0174` share a 6-section `c=2034` family, `Slot0175` uses a 5-section `c=2045/2048/2049` family, `Slot0176` shifts to a 6-section `c=2072` family starting `25/26`, `Slot0177` has a different 6-section `2072` family starting `26/7`, and `Slot0178` has the verified `7/18/18/25/25/25` family. This rules out the stronger hypothesis that the front matter is a single fixed-layout header with only a few toggled counters.
- The negative boundary remains unchanged: session 63 still did not identify `BankMoney`, `GlobalFlags`, or `GlobalVariables`, and it still did not justify a write-safe typed `data.sav` model. The useful new constraint is sharper than session 62: future RE should compare recurring signature families within one front-matter variant at a time instead of assuming a single cross-slot schema for the entire prefix.
- Verification completed with `dotnet format ArcNET.slnx --no-restore --include src/Probe/Commands/PcDataCommand.cs`, `dotnet csharpier format src/Probe/Commands/PcDataCommand.cs`, `dotnet build src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true`, `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- --out tasks/session63_mode17_0178.txt 17 0178`, and `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- --out tasks/session63_mode17_0170_0178.txt 17 0170 0178`.

**Session 64: mode 17 `data.sav` exact family grouping**
- Probe mode 17 (`pc-data`) now adds a `Front-matter families` summary block in range mode. After printing the per-slot `front16` sequences, it groups `data.sav` pre-zero prefixes by exact `(rows, sections, full b/c/d x len sequence)` family and lists which slots share each exact shape. The implementation stays in the current low-allocation Probe style: it reuses the existing `front16` run list, builds a bounded canonical sequence key, and removed the only new stackalloc-in-loop warning sites before final verification.
- Live verification on `0170-0178` shows **8 exact `front16` families across 9 slots**. Only one family recurs at all: `Slot0173` and `Slot0174` share the exact 6-section `c=2034` sequence. The other 7 families are single-slot variants.
- The stronger late-game boundary is now explicit rather than inferred: even the visually related `rows=32`, `sects=6`, `c=2072` saves are **not** the same exact family. `Slot0176`, `Slot0177`, and `Slot0178` each land in different exact-family buckets, so future RE must compare one exact front-matter family at a time instead of pooling all `c=2072` prefixes together.
- No new editor API or typed `data.sav` model was added in this session. The new evidence is still read-only structural RE only; it narrows the comparison strategy but does not yet justify a write-safe decoded surface for the unresolved `data.sav` payload.
- Verification completed with `dotnet format ArcNET.slnx --no-restore --include src/Probe/Commands/PcDataCommand.cs`, `dotnet csharpier format src/Probe/Commands/PcDataCommand.cs`, `dotnet build src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true`, and `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- --out tasks/session64_mode17_0170_0178.txt 17 0170 0178`.

**Session 65: typed `data.sav` format + editor surface**
- Added `DataSavQuadRow`, `DataSavFile`, and `DataSavFormat` in `ArcNET.Formats`. The model stays structural rather than semantic: it preserves the original payload in `RawBytes`, exposes the verified 8-byte header plus aligned `INT32[4]` row framing and remainder ints, and provides targeted patch helpers (`WithHeader`, `WithQuadRow`, `WithRemainderInt`) without inventing unverified field names.
- Wired `data.sav` through the hierarchical save layer: `SaveGame` now carries typed `DataSavFiles`, `SaveGameReader` parses top-level `data.sav` into that collection, and `SaveGameWriter` writes it back through `DataSavFormat` instead of treating it as a raw blob.
- Wired the same typed surface through the editor layer: `LoadedSave.DataSavFiles`, `SaveGameLoader`, `SaveGameUpdates.UpdatedDataSavFiles`, `SaveGameWriter`, and `SaveGameEditor.GetCurrentDataSav(...)` / `GetPendingDataSav(...)` / `WithDataSav(...)` now provide a first-class edit path. Successful `data.sav` parses are excluded from `RawFiles`.
- Probe mode 17 now reuses the shared typed parser through `LoadedSave.DataSavFiles` instead of maintaining a separate `data.sav` byte path. Live verification still matches the session-64 structural findings: `Slot0178` reports `header=25/32`, `6520` ints plus `2` trailing bytes, `1629` aligned rows, `587` distinct `(b,c,d)` signatures, `733` contiguous sections, longest zero section `@32 x250`, and the same `7/18/18/25/25/25` front-matter family; the `0170-0178` range still shows the same eight exact front-matter families and the same `data2.sav` pair-table / window behavior as sessions 64 and 58.
- Added focused coverage for the new model and wiring: `DataSavFormatTests` covers parse, unchanged round-trip, structural patching, and too-short rejection; format/editor integration tests now cover typed `data.sav` load, exclusion from `RawFiles`, staged edits, chained edits, missing-path no-op behavior, and save/load round-trip persistence. Verified totals: Formats `308/308`, Editor `193/193`.
- Verification completed with `dotnet build src/Formats/ArcNET.Formats/ArcNET.Formats.csproj -c Release -p:CSharpier_Bypass=true`, `dotnet build src/Editor/ArcNET.Editor/ArcNET.Editor.csproj -c Release -p:CSharpier_Bypass=true`, `dotnet build src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true`, `dotnet run --project src/Formats/ArcNET.Formats.Tests/ArcNET.Formats.Tests.csproj -c Release -p:CSharpier_Bypass=true` (`308/308`), `dotnet run --project src/Editor/ArcNET.Editor.Tests/ArcNET.Editor.Tests.csproj -c Release -p:CSharpier_Bypass=true` (`193/193`), `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- --out tasks/session65_mode17_0178.txt 17 0178`, and `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- --out tasks/session65_mode17_0170_0178.txt 17 0170 0178`.
- The remaining boundary is now narrower and clearer: `data.sav` has a write-safe structural typed/editor surface, but `BankMoney`, `GlobalFlags`, `GlobalVariables`, and any other higher-level semantics inside that payload are still unresolved RE work.

**Session 66: `data2.sav` structural unresolved-region surface**
- Extended the partial typed `data2.sav` model in `ArcNET.Formats` without inventing new semantics. `Data2SavFile` now exposes the unresolved non-table regions as structural `PrefixIntCount` / `SuffixIntCount`, `GetPrefixInt(...)` / `GetSuffixInt(...)`, and `WithPrefixInt(...)` / `WithSuffixInt(...)`, keeping investigation and editor workflows on the typed surface instead of forcing a fallback to raw-byte patching.
- Reused that boundary through the existing editor/save pipeline: `SaveGameEditor.WithData2Sav(...)` and `SaveGameUpdates.UpdatedData2SavFiles` now support structural prefix/suffix edits through the typed model, and round-trip coverage verifies those edits persist alongside decoded `50000+` pair-table updates. Verified totals after the new tests: Formats `309/309`, Editor `194/194`.
- Probe mode 17 (`pc-data`) now surfaces the unresolved `data2.sav` regions explicitly. Single-slot output prints unresolved prefix/suffix counts plus bounded previews around the verified `50000+` table, and range mode now emits `unresolved50000: prefix=... changed=... suffix=... changed=...` on every slot transition, even when the decoded pair table itself is unchanged.
- Live verification on `0170-0178` sharpened the current `data2.sav` boundary without overclaiming semantics. Across the stable late-game saves (`0172-0178`), the unresolved regions stay structurally fixed at `prefixInts=56` and `suffixInts=15`; `Slot0171` remains the known outlier at `prefixInts=58`. The corrected range run shows the late-game churn is mostly in the unresolved prefix, while the suffix only spikes on `0173->0174` and `0174->0175` (`suffix changed=10` on both transitions). Verified examples: `0172->0173 prefix changed=2 / suffix changed=0`, `0175->0176 5 / 0`, `0176->0177 2 / 0`, `0177->0178 3 / 0`.
- Single-slot `Slot0178` output now exposes the unresolved `data2.sav` shape directly: `prefixInts=56` with head `[25,0,11,2,-1,1,2,-1]`, tail `[194,186,2,BEEFCAFE,BEEFCAFE,0,BEEFCAFE,211]`, and `suffixInts=15` with leading values `[BEEFCAFE,-1,0,-1,0,-1,0,-1]`. This is still structural evidence only, but it gives the next RE pass a concrete typed window to compare instead of another anonymous raw blob region.
- Verification completed with `dotnet format ArcNET.slnx --no-restore --include src/Formats/ArcNET.Formats/Data2SavFile.cs src/Formats/ArcNET.Formats.Tests/Data2SavFormatTests.cs src/Editor/ArcNET.Editor.Tests/SaveGameEditorTests.cs src/Editor/ArcNET.Editor.Tests/SaveGameTests.cs src/Probe/Commands/PcDataCommand.cs`, `dotnet csharpier format src/Formats/ArcNET.Formats/Data2SavFile.cs src/Formats/ArcNET.Formats.Tests/Data2SavFormatTests.cs src/Editor/ArcNET.Editor.Tests/SaveGameEditorTests.cs src/Editor/ArcNET.Editor.Tests/SaveGameTests.cs src/Probe/Commands/PcDataCommand.cs`, `dotnet run --project src/Formats/ArcNET.Formats.Tests/ArcNET.Formats.Tests.csproj -c Release -p:CSharpier_Bypass=true` (`309/309`), `dotnet run --project src/Editor/ArcNET.Editor.Tests/ArcNET.Editor.Tests.csproj -c Release -p:CSharpier_Bypass=true` (`194/194`), `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- --out tasks/session66_mode17_0178.txt 17 0178`, and `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- --out tasks/session66_mode17_0170_0178.txt 17 0170 0178`.

**Session 67: `data2.sav` unresolved-range batching + family grouping**
- Extended the partial typed `data2.sav` model without inventing new semantics. `Data2SavFile` now adds contiguous unresolved-region helpers `CopyPrefixInts(...)`, `CopySuffixInts(...)`, `WithPrefixInts(...)`, and `WithSuffixInts(...)` so editor/RE callers can inspect or patch verified prefix/suffix windows in a single operation instead of chaining one-int helpers and paying one raw-byte copy per int.
- Reused that range surface through the existing editor/save pipeline rather than adding another editor-specific layer. `SaveGameEditor.WithData2Sav(...)` already accepts the typed model, and focused editor coverage now verifies contiguous prefix/suffix window edits survive pending-state composition and save/load round-trip. Verified totals after the new tests: Formats `311/311`, Editor `195/195`.
- Probe mode 17 (`pc-data`) now groups exact unresolved `data2.sav` prefix and suffix families across slot ranges using the shared typed parser. This keeps the RE output aligned with the same structural boundary exposed by `Data2SavFile` instead of introducing another ad-hoc raw-byte decoder for those regions.
- Live verification on `0170-0178` produced a sharper family-level boundary. The unresolved prefix has **9 exact families across 9 slots** with no recurrence, while the unresolved suffix has **3 exact families** with one recurring late-game family shared by `0171`, `0172`, `0173`, `0175`, `0176`, `0177`, and `0178`; the only suffix outliers in this corpus are `0170` and `0174`.
- The family summaries make the smaller unresolved windows easier to isolate without overclaiming semantics. In the recurring late-game prefix families the stable head stays anchored at `25,0,11,2,-1,...` and the stable tail stays anchored near `194,186/559,...,BEEFCAFE,...,211`; the recurring suffix family stays mostly in the `BEEFCAFE/-1/0` sentinel pattern, while the `0174` suffix outlier begins `BEEFCAFE,0,1,1,1,2,1,3`. This is still structural evidence only, but it narrows which unresolved windows are worth comparing next.
- Verification completed with `dotnet format ArcNET.slnx --no-restore --include src/Formats/ArcNET.Formats/Data2SavFile.cs src/Formats/ArcNET.Formats.Tests/Data2SavFormatTests.cs src/Probe/Commands/PcDataCommand.cs src/Editor/ArcNET.Editor.Tests/SaveGameEditorTests.cs`, `dotnet csharpier format src/Formats/ArcNET.Formats/Data2SavFile.cs src/Formats/ArcNET.Formats.Tests/Data2SavFormatTests.cs src/Probe/Commands/PcDataCommand.cs src/Editor/ArcNET.Editor.Tests/SaveGameEditorTests.cs`, `dotnet run --project src/Formats/ArcNET.Formats.Tests/ArcNET.Formats.Tests.csproj -c Release -p:CSharpier_Bypass=true` (`311/311`), `dotnet run --project src/Editor/ArcNET.Editor.Tests/ArcNET.Editor.Tests.csproj -c Release -p:CSharpier_Bypass=true` (`195/195`), `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- --out tasks/session67_mode17_0178.txt 17 0178`, and `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- --out tasks/session67_mode17_0170_0178.txt 17 0170 0178`.

**Session 68: `data.sav` range helpers + tail-family tracing**
- Extended the structural typed `data.sav` model without inventing new semantics. `DataSavFile` now adds contiguous helpers `CopyQuadRows(...)`, `CopyRemainderInts(...)`, `WithQuadRows(...)`, and `WithRemainderInts(...)`, so editor/RE callers can inspect or patch verified aligned-row / remainder-int ranges in one operation instead of chaining per-row or per-int edits.
- Reused that range surface through the existing editor/save pipeline rather than adding another editor-only abstraction. `SaveGameEditor.WithDataSav(...)` already stages the typed model, and focused editor coverage now verifies contiguous `data.sav` row/remainder edits survive pending-state composition and save/load round-trip. Verified totals after the new tests: Formats `313/313`, Editor `197/197`.
- Probe mode 17 (`pc-data`) now treats the region after the longest zero plateau in `data.sav` as a first-class structural surface. Single-slot output prints `tail after longest zero`, and range mode now emits compact `tail16` summaries, pairwise tail deltas, and exact tail-family grouping using the shared typed parser on the current low-allocation path.
- Live Slot0178 verification now exposes a concrete tail boundary instead of treating everything after the zero plateau as undifferentiated residue. After the longest zero run at rows `32..281`, `data.sav` tail analysis begins at row `282` and covers `1347` rows across `726` contiguous sections.
- Range verification on `0170-0178` shows the tail boundary is structurally stable enough to compare, but still not semantically decoded. Tail start rows track the end of the longest zero plateau (`269`, `283`, `259`, `281`, `281`, `282`, `282`, `282`, `282`), and adjacent late-game saves can share meaningful leading tail prefixes (`samePrefix=20` on `0175->0176`, `samePrefix=77` on `0176->0177` and `0177->0178`). But exact family grouping still yields **9 exact tail families across 9 slots** with **no recurrence** in the current corpus.
- The new tail summaries therefore tighten the RE boundary without overclaiming semantics: the post-zero region is clearly structured and comparable, but the current `0170-0178` data does not yet support a reusable decoded tail schema. This session improves the editor/API surface and narrows future RE work; it does **not** claim `BankMoney`, `GlobalFlags`, or `GlobalVariables` have been found.
- Verification completed with `dotnet format ArcNET.slnx --no-restore --include src/Formats/ArcNET.Formats/DataSavFile.cs src/Formats/ArcNET.Formats.Tests/DataSavFormatTests.cs src/Editor/ArcNET.Editor.Tests/SaveGameEditorTests.cs src/Probe/Commands/PcDataCommand.cs`, `dotnet csharpier format src/Formats/ArcNET.Formats/DataSavFile.cs src/Formats/ArcNET.Formats.Tests/DataSavFormatTests.cs src/Editor/ArcNET.Editor.Tests/SaveGameEditorTests.cs src/Probe/Commands/PcDataCommand.cs`, `dotnet run --project src/Formats/ArcNET.Formats.Tests/ArcNET.Formats.Tests.csproj -c Release -p:CSharpier_Bypass=true` (`313/313`), `dotnet run --project src/Editor/ArcNET.Editor.Tests/ArcNET.Editor.Tests.csproj -c Release -p:CSharpier_Bypass=true` (`197/197`), `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- --out tasks/session68_mode17_0178.txt 17 0178`, and `dotnet run --project src/Probe/Probe.csproj -c Release -p:WarningsNotAsErrors=CA2014 -p:CSharpier_Bypass=true -- --out tasks/session68_mode17_0170_0178.txt 17 0170 0178`.

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
| `CharacterMdyRecordTests.cs` | v2 record parse + With* methods (gold, arrows, kills, portrait, MaxFollowers, name, HP SAR, fatigue SAR, position/AI SAR) + Effects + EffectCauses (session 6) | bsId-unknown fields (bullets, power cells, bank money, reputation, quests, fog-of-war) not yet testable without a more advanced save |
| `CharacterMdyRecordBuilderTests.cs` | `CharacterMdyRecordBuilder.Create()` — all fields, round-trip, validation | — |
| `SaveGameBuilderTests.cs` | `SaveGameBuilder.CreateNew()` — both overloads, round-trip (Writer→Reader), top-level `.tmf` + raw-file round-trip, path validation | — |

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
