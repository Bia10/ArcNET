# ArcNET.GameData — Road to publish

## Priority 1 — Correctness bugs (break existing functionality)

- [x] **1. Fix `MessageEntry` loss in `GameDataStore`**
  - Change `List<string> _messages` → `List<MessageEntry> _messages`
  - Change `AddMessage(string text)` → `AddMessage(MessageEntry entry)`
  - Change public `IReadOnlyList<string> Messages` → `IReadOnlyList<MessageEntry> Messages`
  - File: `src/GameData/ArcNET.GameData/GameDataStore.cs`

- [x] **2. Fix loader to pass full `MessageEntry`**
  - Change `store.AddMessage(e.Text)` → `store.AddMessage(e)` in both `LoadEntryFromFileAsync` and `LoadEntryFromMemoryAsync`
  - File: `src/GameData/ArcNET.GameData/GameDataLoader.cs`

- [x] **3. Fix saver to write correct index and sound fields**
  - `SaveMessagesToFile` / `SaveMessagesToMemory` must use `entry.Index` (not list position) and respect `entry.SoundId`
  - Format: `{index}{text}` or `{index}{soundId}{text}`
  - File: `src/GameData/ArcNET.GameData/GameDataSaver.cs`

- [x] **4. Fix `GameDataExporter` DTO**
  - Add `sealed record MessageEntryDto(int Index, string? SoundId, string Text)`
  - Replace `IReadOnlyList<string> Messages` with `IReadOnlyList<MessageEntryDto> Messages` in `GameDataExportDto`
  - Register in `GameDataJsonContext`
  - File: `src/GameData/ArcNET.GameData/GameDataExporter.cs`

---

## Priority 2 — Core missing functionality (Sector / Proto / Mob pipeline)

- [x] **5. Add `Sectors`, `Protos`, `Mobs` to `GameDataStore`**
  - Add `List<Sector> _sectors` + `AddSector(Sector)` + `IReadOnlyList<Sector> Sectors`
  - Add `List<ProtoData> _protos` + `AddProto(ProtoData)` + `IReadOnlyList<ProtoData> Protos`
  - Add `List<MobData> _mobs` + `AddMob(MobData)` + `IReadOnlyList<MobData> Mobs`
  - Update `Clear()` to reset all three
  - File: `src/GameData/ArcNET.GameData/GameDataStore.cs`

- [x] **6. Wire Sector / Proto / Mob loading in `GameDataLoader`**
  - In both `LoadEntryFromFileAsync` and `LoadEntryFromMemoryAsync` add cases:
    - `FileFormat.Sector` → `SectorFormat.ParseFile` / `ParseMemory` → `store.AddSector`
    - `FileFormat.Proto` → `ProtoFormat.ParseFile` / `ParseMemory` → `store.AddProto`
    - `FileFormat.Mob` → `MobFormat.ParseFile` / `ParseMemory` → `store.AddMob`
  - File: `src/GameData/ArcNET.GameData/GameDataLoader.cs`

- [x] **7. Add sector / proto / mob save logic to `GameDataSaver`**
  - Add `SaveSectorsToDirectory(GameDataStore, string outputDir)` — `SectorFormat.WriteToFile` per entry
  - Add `SaveProtosToDirectory(GameDataStore, string outputDir)` — `ProtoFormat.WriteToFile` per entry
  - Add `SaveMobsToDirectory(GameDataStore, string outputDir)` — `MobFormat.WriteToFile` per entry
  - Add `*ToMemory` byte-array overloads for each
  - Update `SaveToDirectoryAsync` and `SaveToMemory` to call them
  - File: `src/GameData/ArcNET.GameData/GameDataSaver.cs`

- [x] **8. Extend `GameDataExporter` DTO for Sector / Proto / Mob**
  - Add `sealed record SectorDto(...)`, `ProtoDto(...)`, `MobDto(...)` with relevant fields
  - Register new DTOs in `GameDataJsonContext`
  - Extend `GameDataExportDto` with `Sectors`, `Protos`, `Mobs` properties
  - Update `ToDto` / `ExportToJson` conversion
  - File: `src/GameData/ArcNET.GameData/GameDataExporter.cs`

---

## Priority 3 — Test coverage

- [x] **9. Create `GameDataSaverTests.cs`**
  - `SaveMessagesToMemory` round-trip (index preserved, soundId preserved)
  - `SaveMessagesToFile` writes correct format
  - `SaveToMemory` includes messages key

- [x] **10. Create `GameDataExporterTests.cs`**
  - `ExportToJson` with known store contents → verify JSON has correct index/text
  - Deserialize JSON back and compare

- [x] **11. Expand `GameDataLoaderTests.cs`**
  - `LoadMessages` with sample .mes bytes
  - `LoadFromMemoryAsync` with sample bytes
  - FacadeWalk filename detection (`facwalk.*`)
  - Cancellation token propagation (cancelled token → throws)
  - Progress reporting fires [0..1]

---

## Design issues

- [x] **G4 — Multi-file message save is destructive**: Added `MessagesBySource` (`IReadOnlyDictionary<string, IReadOnlyList<MessageEntry>>`) to `GameDataStore`; loader populates it via internal `AddMessage(entry, sourcePath)` overload; `SaveToDirectoryAsync` and `SaveToMemory` write per-source filenames when origin info is present, fall back to `game.mes` when not.

- [x] **G6 — No file origin for Sector / Proto / Mob**: Added `SectorsBySource`, `ProtosBySource`, `MobsBySource` to `GameDataStore`; loader populates via internal overloads; `SaveSectorsToDirectory`, `SaveProtosToDirectory`, `SaveMobsToDirectory`, `SaveToMemory` use source filenames when available.

- [x] **G2 — `GameDataStore.Objects` always empty**: `LoadEntryFromFileAsync` and `LoadEntryFromMemoryAsync` now call `store.AddObject(proto.Header)` and `store.AddObject(mob.Header)` after wiring Proto/Mob, so `Objects` is populated from loaded .pro and .mob files.

- [x] **G5 — `LoadMessages` silently drops duplicate indices**: `TryAdd` replaced with `InvalidOperationException` that names the offending file and duplicated index.

---

## Priority 4 — NativeAOT infrastructure (completed)

- [x] **AOT-1: Test projects incorrectly inherited `IsAotCompatible=true`**
  - Root cause: `src/Tests/Directory.Build.props` is orphaned — test projects live alongside library projects
    (e.g. `src/Core/ArcNET.Core.Tests/`) and are not in the `src/Tests/` subtree, so MSBuild never imports it.
  - Fix: added `<IsAotCompatible Condition="$(MSBuildProjectName.EndsWith('.Tests'))">false</IsAotCompatible>`
    in `src/Directory.Build.props`, immediately after the `<IsAotCompatible>true</IsAotCompatible>` line,
    so the `Enable*Analyzer` conditions that follow evaluate the correct value.
  - Effect: AOT/trim/single-file analyzers now run only on the 6 library projects; test projects opt out.
  - Build: 0 errors, 0 warnings (Release).

---

## Results summary

All 11 P1–P3 items complete. AOT-1 infrastructure fix applied. All 4 G design issues resolved (G2, G4, G5, G6). 44/44 GameData tests pass; full solution builds clean (0 errors, 0 warnings, Release).
