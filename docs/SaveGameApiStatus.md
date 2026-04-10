# Arcanum Save Game Read/Write API — Status Report

**Scope**: ArcNET.Formats + ArcNET.GameObjects + ArcNET.GameData  
**Date**: 2026-04-08 (updated 2026-04-10, session 20)  
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

**Status: ~90% complete (session 13 update). Blessings/Curses/Schematics now implemented via structural fingerprints — no session-specific bsId required.**

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
| Fog of war mask | PC field bit 144 — OFF format: `ObjectPc.PcFogMask` via GAP-2 bridge | Blocked: probably per-map in mobile.md only |

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
| `CharacterMdyRecord` With* mutations (secondary fields) | **~90%** (GAP-3 session 15: `ReputationFactionSlots` property + quest-state bitmask discovery; session 12+13: Blessings/Curses/Schematics added; session 9: Rumors; session 8: Quest+Reputation; session 6: Effects+EffectCauses; session 5: HP/fatigue/positionAI/MaxFollowers) |
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
**Consumer-facing save API completeness**: ~97% (GAP-1, GAP-2, GAP-5, GAP-6, GAP-7, GAP-8 closed; GAP-3 ~90%; GAP-4 open)

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
- **Mode 13 field evolution** (`probe 13 [first [last]]`): new mode. Iterates all save slots in a range, compares each to the previous, and prints one timestamped delta line per slot where any tracked field changed. Silent for unchanged slots. Tracked fields: `lv`, `XP`, `align`, `fate`, `magicPts`, `techPts`, `gold`, `quests`, `rumors`, `blessings`, `curses`, `schematics`, and all 25 `SpellTech` discipline ranks by name (Conv, Div, Air, Earth, Fire, Water, Force, Mental, Meta, Morph, Nature, NecroBlk, NecroWht, Phantasm, Summon, Temporal, MasteryCol, Herb, Chem, Elec, Explos, Gun, Mech, Smithy, Therap). Marks level-up slots with `*** LEVEL UP ***`. Useful for tracing exact level-up events, spell acquisition order, alignment drift, and meta/tech point accumulation across a long playthrough.

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
- **Slot-pair churn is quantified**: each transition line now includes `Σ: new/gone/move/chg` and top fingerprint-count summaries for `MOVE` and `CHG`, so spikes like `0174→0175` and `0177→0178` can be triaged before reading the per-row details.

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
