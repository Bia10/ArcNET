# ArcNET

![.NET](https://img.shields.io/badge/net10.0-5C2D91?logo=.NET&labelColor=gray)
![C#](https://img.shields.io/badge/C%23-14.0-239120?labelColor=gray)
[![Build Status](https://github.com/Bia10/ArcNET/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/Bia10/ArcNET/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/Bia10/ArcNET/branch/main/graph/badge.svg)](https://codecov.io/gh/Bia10/ArcNET)
[![License](https://img.shields.io/github/license/Bia10/ArcNET)](https://github.com/Bia10/ArcNET/blob/main/LICENSE)

Modular SDK for **Arcanum: Of Steamworks and Magick Obscura** editors, content tooling, and save workflows.
Span-based, low-allocation, UI-agnostic library APIs — usable from console tools, Avalonia editors, Blazor WASM, and anything in between.

⭐ Please star this project if you find it useful. ⭐

[Packages](#packages) · [Quick Example](#quick-example) · [NuGet Publishing](docs/NuGetPublishing.md) · [Example Catalogue](docs/examples.md) · [Editor Implementation Targets](docs/EditorImplementationTargets.md) · [Editor SDK Roadmap](docs/EditorSdkRoadmap.md) · [Public API](docs/PublicApi.md)

---

## Packages

| Package | NuGet | Description | Status |
|---|---|---|---|
| `ArcNET.Core` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Core?label=NuGet)](https://www.nuget.org/packages/ArcNET.Core) | `SpanReader` / `SpanWriter`, primitive types (`Location`, `ArtId`, `Color`, `GameObjectGuid`, `PrefixedString`) | 🚧 WIP |
| `ArcNET.GameObjects` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.GameObjects?label=NuGet)](https://www.nuget.org/packages/ArcNET.GameObjects) | Full game-object model — 22 typed data classes with explicit `Read` + `Write` | 🚧 WIP |
| `ArcNET.Formats` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Formats?label=NuGet)](https://www.nuget.org/packages/ArcNET.Formats) | Binary format parsers/writers for content, archives, and save-game formats including MES, SEC, ART, DLG, SCR, PRO, MOB, JMP, TFAI, TFAF, GSI, TMF, and structural save-global files | 🚧 WIP |
| `ArcNET.Archive` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Archive?label=NuGet)](https://www.nuget.org/packages/ArcNET.Archive) | DAT archive pack / unpack backed by `MemoryMappedFile`; TFAF sub-archive support | 🚧 WIP |
| `ArcNET.GameData` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.GameData?label=NuGet)](https://www.nuget.org/packages/ArcNET.GameData) | `GameDataLoader`, `GameDataStore`, `GameDataSaver`, and `GameDataExporter` for loose/extracted content workflows with preserved relative source paths and per-source MES/SEC/PRO/MOB/SCR/DLG tracking | 🚧 WIP |
| `ArcNET.Patch` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Patch?label=NuGet)](https://www.nuget.org/packages/ArcNET.Patch) | HighRes patch configuration, installer, and uninstaller | 🚧 WIP |
| `ArcNET.Dumpers` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Dumpers?label=NuGet)](https://www.nuget.org/packages/ArcNET.Dumpers) | Human-readable text dumpers for game data, archive, and save formats | 🚧 WIP |
| `ArcNET.BinaryPatch` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.BinaryPatch?label=NuGet)](https://www.nuget.org/packages/ArcNET.BinaryPatch) | JSON-driven binary patching for bug fixes and mod authoring — field-level PRO/MOB mutations and raw byte patches with backup/revert/verify | 🚧 WIP |
| `ArcNET.Editor` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Editor?label=NuGet)](https://www.nuget.org/packages/ArcNET.Editor) | Unified editor workspace/session loading for real installs or loose content plus optional saves (`EditorWorkspaceLoader`), parsed asset catalog plus loose-vs-DAT provenance (`EditorWorkspace.Assets`), load diagnostics for skipped archives/assets (`EditorWorkspace.LoadReport`), cross-file workspace validation findings including install-aware proto display-name coverage plus dialog-local authoring checks (`EditorWorkspace.Validation`), map/sector/message/proto/script/dialog/art index queries including asset-path dependency summaries, sector summaries, sector environment-scheme lookups, lightweight map projection lookup plus normalized preview flags and density bands, multi-asset script/dialog definition lookup, script attachment summaries, dialog-node graph summaries (`EditorWorkspace.Index`), graph-aware dialog composition plus transactional dialog editing and flow insertion helpers, typed script condition/action and operand composition, and local dialog/script validation helpers (`DialogBuilder`, `DialogEditor`, `ScriptBuilder`, `DialogValidator`, `ScriptBuilder.Validate()`, `ScriptValidator`), save-game editing pipeline (`LoadedSave`, `SaveGameEditor`, `SaveGameLoader`, `SaveGameWriter`), and fluent builders for authoring workflows | 🚧 WIP |

All packages target `net10.0` and are AOT / trim compatible. Runtime dependencies are intentionally small; the shared non-BCL package currently used across the core libraries is `Bia.ValueBuffers` for low-allocation buffer building.

The packable NuGet libraries are intended to remain multiplatform across Windows, Linux, and macOS. Platform-specific tooling such as probes and other local diagnostics is outside that package contract.

Package versions are now explicit per library via `src/ArcNET.PackageVersions.props`. Use `dotnet Build.cs package-version ArcNET.Core` to inspect the current version, then tag `ArcNET.Core-v<that version>` to publish that package. See [docs/NuGetPublishing.md](docs/NuGetPublishing.md) for the local pack and CI publish flow.

The NuGet packages are already consumable, but the overall SDK is still under active construction toward the full modular editor goal described in [docs/EditorSdkRoadmap.md](docs/EditorSdkRoadmap.md). For the concrete near-term build order, use [docs/EditorImplementationTargets.md](docs/EditorImplementationTargets.md).

---

## Quick Example

```csharp
using ArcNET.Formats;

// Parse a MES message file from disk — one allocation (File.ReadAllBytes)
MesFile mesFile = MessageFormat.ParseFile("arcanum/mes/game.mes");
IReadOnlyList<MessageEntry> messages = mesFile.Entries;

// Or from a buffer you already own — zero extra allocations
ReadOnlyMemory<byte> buf = await File.ReadAllBytesAsync("game.mes");
mesFile = MessageFormat.ParseMemory(buf);

// Serialize back to bytes
byte[] bytes = MessageFormat.WriteToArray(in mesFile);
```

For more examples see the [Example Catalogue](docs/examples.md).

---

## Example Catalogue

The [docs/examples.md](docs/examples.md) file contains copy-paste-ready examples for every library and format:

- **ArcNET.Formats** — MES, SEC, ART, DLG, SCR, PRO, MOB, JMP, FAC, TDF, GSI, TFAI, TFAF, PRP parsers; round-trip serialization; file discovery
- **ArcNET.Archive** — open, enumerate, extract single/all entries, read without extracting, pack a directory, TFAF sub-archive
- **ArcNET.GameObjects** — read full game objects, read headers only, `GameObjectStore`
- **ArcNET.GameData** — load MES/SEC/PRO/MOB/SCR/DLG from directory or in-memory buffers (per-source origin tracking), save to disk / memory restoring original source paths, dirty tracking, AOT-safe JSON export
- **ArcNET.Patch** — install / uninstall the HighRes patch, read and modify `HighResConfig`
- **ArcNET.Dumpers** — human-readable text dumps for all parsed formats (mob, proto, sector, art, dialog, script, message, etc.)
- **ArcNET.BinaryPatch** — JSON-driven binary patching: field-level PRO/MOB mutations, raw byte offsets, backup/revert/verify, patch state tracking
- **ArcNET.Editor** — unified editor workspace loading for real installs or loose content plus optional save slots via `EditorWorkspaceLoader`, parsed asset catalog plus winning-source provenance via `EditorWorkspace.Assets`, skipped-load diagnostics via `EditorWorkspace.LoadReport`, cross-file workspace validation findings including install-aware proto display-name coverage plus dialog-local authoring checks via `EditorWorkspace.Validation`, map/sector/message/proto/script/dialog/art lookup plus asset-path dependency summaries, sector summaries, sector environment-scheme queries, lightweight map projection lookup plus normalized preview flags and density bands, plus script attachment and dialog-node graph summaries via `EditorWorkspace.Index`, graph-aware dialog composition via `DialogBuilder`, transactional dialog editing plus insert-after graph splicing via `DialogEditor`, typed script condition/action and operand composition plus local script validation via `ScriptBuilder` / `ScriptBuilder.Validate()` / `ScriptValidator`, local dialog validation via `DialogValidator`, save-game round-trip via `LoadedSave` / `SaveGameEditor`, and fluent `MobDataBuilder`, `CharacterBuilder`, `DialogBuilder`, `SectorBuilder`, and `ScriptBuilder` for constructing or editing objects
- **ArcNET.Core** — low-level `SpanReader` / `SpanWriter`, primitive round-trips, `EnumLookup`

---

## Package dependency graph

```
ArcNET.App (exe)
  ├── ArcNET.BinaryPatch
  │     ├── ArcNET.Formats
  │     │     ├── ArcNET.GameObjects → ArcNET.Core
  │     │     └── ArcNET.Core
  │     └── ArcNET.Archive → ArcNET.Core
  ├── ArcNET.Dumpers
  │     ├── ArcNET.Formats  (→ see above)
  │     ├── ArcNET.GameObjects → ArcNET.Core
  │     └── ArcNET.Archive  → ArcNET.Core
  ├── ArcNET.Editor
  │     ├── ArcNET.GameData
  │     │     ├── ArcNET.Formats  (→ see above)
  │     │     ├── ArcNET.GameObjects → ArcNET.Core
  │     │     └── ArcNET.Core
  │     ├── ArcNET.Formats  (→ see above)
  │     ├── ArcNET.GameObjects → ArcNET.Core
  │     └── ArcNET.Archive  → ArcNET.Core
  ├── ArcNET.GameData
  │     ├── ArcNET.Formats  (→ see above)
  │     ├── ArcNET.GameObjects → ArcNET.Core
  │     └── ArcNET.Core
  ├── ArcNET.Archive  → ArcNET.Core
  └── ArcNET.Patch    → ArcNET.Core
```

---

## Building

```shell
dotnet tool restore
dotnet build ArcNET.Build.slnx -c Release
```

## NuGet Packaging

```shell
dotnet Build.cs list-packages
dotnet Build.cs package-version ArcNET.Core
dotnet Build.cs pack
dotnet Build.cs pack ArcNET.Core
```

Tagging `ArcNET.<Package>-v<semver>` publishes only that package via GitHub Actions. The full release contract is documented in [docs/NuGetPublishing.md](docs/NuGetPublishing.md).

CI validates package packing on Windows, Ubuntu, and macOS for the publishable library projects.

## Testing

TUnit tests use the Microsoft Testing Platform runner:

```shell
dotnet Build.cs test
dotnet dotnet-coverage collect "dotnet run --project src/Core/ArcNET.Core.Tests -c Release" --output artifacts/TestResults/ArcNET.Core.Tests.coverage.cobertura.xml --output-format cobertura
```

## Formatting

```shell
dotnet csharpier format .                    # whitespace + brace style (run first)
dotnet format style ArcNET.Build.slnx        # naming conventions, usings
dotnet format analyzers ArcNET.Build.slnx    # Roslyn analyzer violations
```

---

## Public API Reference

See [docs/PublicApi.md](docs/PublicApi.md) for the complete public API reference.

---

## Credits

Reverse-engineering references that informed the binary format implementations:

- **[arcanum-ce](https://github.com/arcanum-ce/arcanum-ce)** — C rewrite of the Arcanum engine; primary source for object field tables (`obj.c`, `obj_flags.h`), archive format (`database.h`), script structures (`script.h`), sector layout (`sector.h`), and more. See [references.md](references.md) for the full cross-reference.
- **[GrognardsFromHell/OpenTemple](https://github.com/GrognardsFromHell/OpenTemple)** — ToEE / Arcanum open-source engine; used for save-game format details (`SaveGameInfoReader.cs`, `ArchiveIndexReader.cs`).
- **[AxelStrem/ArtConverter](https://github.com/AxelStrem/ArtConverter)** — Reference implementation for Arcanum ART sprite format (`artconverter.cpp`).

---

## License

MIT — Copyright (c) 2026 Bia10
