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

| Package | Description | NuGet | Status |
|---|---|---|---|
| `ArcNET.Core` | `SpanReader` / `SpanWriter`, primitive types (`Location`, `ArtId`, `Color`, `GameObjectGuid`, `PrefixedString`) | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Core?label=NuGet)](https://www.nuget.org/packages/ArcNET.Core) | 🚧 WIP |
| `ArcNET.GameObjects` | Full game-object model — 22 typed data classes with explicit `Read` + `Write` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.GameObjects?label=NuGet)](https://www.nuget.org/packages/ArcNET.GameObjects) | 🚧 WIP |
| `ArcNET.Formats` | Binary format parsers/writers: MES, SEC, ART, DLG, SCR, PRO, MOB, JMP, TFAI, TFAF, GSI, TMF, and structural save-global files | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Formats?label=NuGet)](https://www.nuget.org/packages/ArcNET.Formats) | 🚧 WIP |
| `ArcNET.Archive` | DAT archive pack / unpack backed by `MemoryMappedFile`; TFAF sub-archive support | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Archive?label=NuGet)](https://www.nuget.org/packages/ArcNET.Archive) | 🚧 WIP |
| `ArcNET.GameData` | `GameDataLoader`, `GameDataStore`, `GameDataSaver`, and `GameDataExporter` for loose/extracted content with per-source MES/SEC/PRO/MOB/SCR/DLG tracking | [![NuGet](https://img.shields.io/nuget/v/ArcNET.GameData?label=NuGet)](https://www.nuget.org/packages/ArcNET.GameData) | 🚧 WIP |
| `ArcNET.Diagnostics` | Runtime diagnostics foundation, launch planning, probe services, and HighRes patch orchestration shared by debugger and console tooling | — | 🚧 WIP |
| `ArcNET.Dumpers` | Human-readable text dumpers for game data, archive, and save formats | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Dumpers?label=NuGet)](https://www.nuget.org/packages/ArcNET.Dumpers) | 🚧 WIP |
| `ArcNET.BinaryPatch` | JSON-driven binary patching — field-level PRO/MOB mutations and raw byte patches with backup/revert/verify | [![NuGet](https://img.shields.io/nuget/v/ArcNET.BinaryPatch?label=NuGet)](https://www.nuget.org/packages/ArcNET.BinaryPatch) | 🚧 WIP |
| `ArcNET.Editor` | Unified editor workspace loading, asset catalog with DAT/loose provenance, map/sector/proto/dialog/script/art index queries, dialog and script composition builders, save-game editing pipeline. See [Editor SDK Roadmap](docs/EditorSdkRoadmap.md). | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Editor?label=NuGet)](https://www.nuget.org/packages/ArcNET.Editor) | 🚧 WIP |

All packages target `net10.0` and are AOT / trim compatible. Runtime dependencies are intentionally small; the shared non-BCL package currently used across the core libraries is `Bia.ValueBuffers` for low-allocation buffer building.

The packable NuGet libraries are intended to remain multiplatform across Windows, Linux, and macOS. Shared diagnostics foundations now live under `src/Diagnostics/*`; platform-specific hosts such as probe and research consoles build on top of that layer.

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
- **ArcNET.Diagnostics** — runtime workspace composition, launch planning, probe foundations, and HighRes patch install / uninstall helpers
- **ArcNET.Dumpers** — human-readable text dumps for all parsed formats (mob, proto, sector, art, dialog, script, message, etc.)
- **ArcNET.BinaryPatch** — JSON-driven binary patching: field-level PRO/MOB mutations, raw byte offsets, backup/revert/verify, patch state tracking
- **ArcNET.Editor** — workspace loading (`EditorWorkspaceLoader`), asset catalog with DAT/loose provenance (`EditorWorkspace.Assets`), load diagnostics (`EditorWorkspace.LoadReport`), workspace validation (`EditorWorkspace.Validation`), map/sector/proto/dialog/script/art index queries (`EditorWorkspace.Index`), dialog and script builders (`DialogBuilder`, `DialogEditor`, `ScriptBuilder`), save-game round-trip (`LoadedSave`, `SaveGameEditor`), and fluent content builders
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
  └── ArcNET.Archive  → ArcNET.Core
```

---

## Building

```shell
dotnet tool restore
dotnet build ArcNET.slnx -c Release
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
dotnet csharpier format .               # whitespace + brace style (run first)
dotnet format style ArcNET.slnx         # naming conventions, usings
dotnet format analyzers ArcNET.slnx     # Roslyn analyzer violations
```

---

## Public API Reference

See [docs/PublicApi.md](docs/PublicApi.md) for the complete public API reference.

---

## Credits

Reverse-engineering references that informed the binary format implementations:

- **[arcanum-ce](https://github.com/alexbatalov/arcanum-ce)** — C rewrite of the Arcanum engine; primary source for object field tables (`obj.c`, `obj_flags.h`), archive format (`database.h`), script structures (`script.h`), sector layout (`sector.h`), and more. See [references.md](references.md) for the full cross-reference.
- **[GrognardsFromHell/OpenTemple](https://github.com/GrognardsFromHell/OpenTemple)** — ToEE / Arcanum open-source engine; used for save-game format details (`SaveGameInfoReader.cs`, `ArchiveIndexReader.cs`).
- **[AxelStrem/ArtConverter](https://github.com/AxelStrem/ArtConverter)** — Reference implementation for Arcanum ART sprite format (`artconverter.cpp`).

---

## License

MIT — Copyright (c) 2026 Bia10
