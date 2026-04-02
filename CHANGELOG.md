# Changelog

All notable changes to this project are documented in [GitHub Releases](https://github.com/Bia10/ArcNET/releases).

This project uses [MinVer](https://github.com/adamralph/minver) for semantic versioning based on git tags.

## [Unreleased]

### Added

- **ArcNET.Core** — `ref struct SpanReader` / `SpanWriter` replacing `BinaryReader`/`BinaryWriter`; all primitive types (`ArtId`, `Location`, `Color`, `GameObjectGuid`, `PrefixedString`) as `readonly record struct` with `ISpanFormattable` + `IUtf8SpanFormattable`; `SpanReaderExtensions` / `SpanWriterExtensions` for domain-specific reads and writes; `EnumLookup<TEnum>` with `FrozenDictionary` backing.
- **ArcNET.GameObjects** — `GameObjectHeader` with bitmap field-presence logic; all 22 type-class data models (Wall, Portal, Container, Scenery, Projectile, Trap, Weapon, Ammo, Armor, Gold, Food, Scroll, Key, KeyRing, Written, Generic, Critter, PC, NPC, Unknown) with explicit ordered `Read` + `Write` methods; flag enums annotated with `[Flags]`; `GameObjectStore` injectable store.
- **ArcNET.Formats** — `FileFormat` enum + `FileFormatExtensions` (`FrozenDictionary`-backed); `MessageFormat`, `SectorFormat`, `FacWalkFormat`, `TextDataFormat` with static `Parse` / `Write` / convenience overloads; stub classes for PRO, MOB, ART, JMP, SCR, DLG, TDF, PRP.
- **ArcNET.GameData** — `GameDataStore` injectable store with dirty tracking and `ObjectChanged` event; `GameDataLoader` dual-source API (directory + in-memory); `GameDataSaver` dual-target API; `GameDataExporter` with `System.Text.Json` source generation.
- **ArcNET.Archive** — `DatArchive` using `MemoryMappedFile` (zero-copy entry access); `DatExtractor` and `DatPacker`; entry table backed by `FrozenDictionary<string, ArchiveEntry>` (OrdinalIgnoreCase).
- **ArcNET.Patch** — `HighResConfig` INI parser/writer; `GitHubReleaseClient` (replaces `LibGit2Sharp`); `PatchInstaller` / `PatchUninstaller`.
- **ArcNET.App** — Console entry point with `Spectre.Console` FigletText banner, interactive menu, `ParseExtractedData`, `InstallHighResPatch`, `UninstallHighResPatch` handlers.
- **ArcNET.Benchmarks** — `SpanReaderBench`, `MessageFormatBench`, `SectorFormatBench`.
- 77 TUnit tests across 6 test projects (Core, GameObjects, Formats, GameData, Archive, Patch).
- GitHub Actions CI (build + test + format-check jobs).
- Central Package Management via `src/Directory.Packages.props`.

### Changed

- Migrated from `net5.0` to `net10.0` / C# 14.
- Replaced static `GameObjectManager` with injectable `GameDataStore`.
- Replaced monolithic `Parser.cs` (~650 lines) with `GameDataLoader`.
- Replaced `Newtonsoft.Json` with `System.Text.Json`.
- Replaced `LibGit2Sharp` with plain `HttpClient`.
- Replaced reflection-based `[Order(n)]` deserialization with explicit per-type `Read`/`Write`.

### Removed

- `ArcNET.DataTypes` and `ArcNET.Terminal` legacy projects.
- `ArcNET.Utilities` legacy project (split into `ArcNET.Patch` + `ArcNET.Archive`).
- `LibGit2Sharp`, `Newtonsoft.Json`, `Bia10.Utils` dependencies.
