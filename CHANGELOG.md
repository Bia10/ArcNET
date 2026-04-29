# Changelog

All notable changes to this project are documented in [GitHub Releases](https://github.com/Bia10/ArcNET/releases).

This project uses [MinVer](https://github.com/adamralph/minver) for semantic versioning based on git tags.

## [Unreleased]

### Added

- **ArcNET.Core** — `ref struct SpanReader` / `SpanWriter` replacing `BinaryReader`/`BinaryWriter`; all primitive types (`ArtId`, `Location`, `Color`, `GameObjectGuid`, `PrefixedString`) as `readonly record struct` with `ISpanFormattable` + `IUtf8SpanFormattable`; `SpanReaderExtensions` / `SpanWriterExtensions` for domain-specific reads and writes; `EnumLookup<TEnum>` with `FrozenDictionary` backing.
- **ArcNET.GameObjects** — `GameObjectHeader` with bitmap field-presence logic; all 22 type-class data models (Wall, Portal, Container, Scenery, Projectile, Trap, Weapon, Ammo, Armor, Gold, Food, Scroll, Key, KeyRing, Written, Generic, Critter, PC, NPC, Unknown) with explicit ordered `Read` + `Write` methods; flag enums annotated with `[Flags]`; `GameObjectStore` injectable store.
- **ArcNET.Formats** — `FileFormat` enum + `FileFormatExtensions` (`FrozenDictionary`-backed, O(1)); `MessageFormat`, `SectorFormat`, `FacWalkFormat`, `ArtFormat`, `MobFormat`, `ProtoFormat`, `DialogFormat`, `JmpFormat`, `ScriptFormat`, `TerrainFormat`, `MapPropertiesFormat`, `SaveIndexFormat`, `SaveInfoFormat`, `TextDataFormat`, `TfafFormat` — all with `Parse` / `Write` / `ParseMemory` / `ParseFile` overloads.
- **ArcNET.GameData** — `GameDataStore` injectable store with dirty tracking, `ObjectChanged` event, `FindByGuid` (lazy `FrozenDictionary`), and per-source-file `MessagesBySource` / `SectorsBySource` / `ProtosBySource` / `MobsBySource` / `ScriptsBySource` / `DialogsBySource` maps; `GameDataLoader` dual-source API (directory + in-memory, concurrent) for MES/SEC/PRO/MOB/SCR/DLG content; `GameDataSaver` preserving original file paths on round-trip; `GameDataExporter` with `System.Text.Json` source generation (AOT-safe).
- **ArcNET.Archive** — `DatArchive` using `MemoryMappedFile` (zero-copy entry access); `DatExtractor` and `DatPacker`; entry table backed by `FrozenDictionary<string, ArchiveEntry>` (OrdinalIgnoreCase).
- **ArcNET.Patch** — `HighResConfig` INI parser/writer; `GitHubReleaseClient` (replaces `LibGit2Sharp`); `PatchInstaller` / `PatchUninstaller`.
- **ArcNET.Dumpers** — human-readable text dumpers for all parsed formats: `ArtDumper`, `DialogDumper`, `FacWalkDumper`, `ItemDumper`, `JmpDumper`, `MapPropertiesDumper`, `MessageDumper`, `MobDumper`, `ProtoDumper`, `SaveIndexDumper`, `SaveInfoDumper`, `ScriptDumper`, `SectorDumper`, `TerrainDumper`.
- **ArcNET.BinaryPatch** — JSON-driven binary patch system: `ProtoFieldPatch`, `MobFieldPatch`, `RawBinaryPatch`; `BinaryPatcher` (apply / revert / verify with `.bak` backups); `PatchDiscovery` (scan directory for JSON descriptors); `JsonPatchLoader`; `PatchStateStore` (`.arcnet-patches.json` state file). Bangellian Chest inventory-corruption fix shipped as built-in JSON patch.
- **ArcNET.App** — Console entry point with `Spectre.Console` FigletText banner, interactive menu, `ParseExtractedData`, `InstallHighResPatch`, `UninstallHighResPatch` handlers, and `arcnet editor` live-inspection commands for workspace summaries, skipped-input triage, and asset/reference queries.
- **ArcNET.Benchmarks** — `SpanReaderBench`, `MessageFormatBench`, `SectorFormatBench`.
- TUnit tests across 7 test projects (Core, GameObjects, Formats, GameData, Archive, Patch, BinaryPatch).
- GitHub Actions CI (build + test + format-check jobs).
- Central Package Management via `src/Directory.Packages.props`.

### Changed

- Migrated from `net5.0` to `net10.0` / C# 14.
- Replaced static `GameObjectManager` with injectable `GameDataStore`.
- Replaced monolithic `Parser.cs` (~650 lines) with `GameDataLoader`.
- Replaced `Newtonsoft.Json` with `System.Text.Json`.
- Replaced `LibGit2Sharp` with plain `HttpClient`.
- Replaced reflection-based `[Order(n)]` deserialization with explicit per-type `Read`/`Write`.
- `ArcNET.Editor` install-backed workspaces now materialize `.scr` and `.dlg` assets, script/dialog definition lookup now follows the live numeric-prefix-plus-suffix naming used by real archive content including multiple assets per numeric ID, and the editor index now exposes higher-level script attachment and dialog graph summaries for those assets.

### Removed

- `ArcNET.DataTypes` and `ArcNET.Terminal` legacy projects.
- `ArcNET.Utilities` legacy project (split into `ArcNET.Patch` + `ArcNET.Archive`).
- `LibGit2Sharp`, `Newtonsoft.Json`, `Bia10.Utils` dependencies.
