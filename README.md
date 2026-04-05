# ArcNET

![.NET](https://img.shields.io/badge/net10.0-5C2D91?logo=.NET&labelColor=gray)
![C#](https://img.shields.io/badge/C%23-14.0-239120?labelColor=gray)
[![Build Status](https://github.com/Bia10/ArcNET/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/Bia10/ArcNET/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/Bia10/ArcNET/branch/main/graph/badge.svg)](https://codecov.io/gh/Bia10/ArcNET)
[![License](https://img.shields.io/github/license/Bia10/ArcNET)](https://github.com/Bia10/ArcNET/blob/main/LICENSE)

Read/write SDK for **Arcanum: Of Steamworks and Magick Obscura** game data formats.
Span-based, zero-allocation binary parsing with a UI-agnostic library API — usable from console tools, Avalonia editors, Blazor WASM, and anything in between.

⭐ Please star this project if you find it useful. ⭐

[Packages](#packages) · [Quick Example](#quick-example) · [Example Catalogue](docs/examples.md) · [Public API](docs/PublicApi.md)

---

## Packages

| Package | NuGet | Status | Description |
|---|---|---|---|
| `ArcNET.Core` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Core?label=NuGet)](https://www.nuget.org/packages/ArcNET.Core) | ✅ Ready | `SpanReader` / `SpanWriter`, primitive types (`Location`, `ArtId`, `Color`, `GameObjectGuid`, `PrefixedString`) |
| `ArcNET.GameObjects` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.GameObjects?label=NuGet)](https://www.nuget.org/packages/ArcNET.GameObjects) | ✅ Ready | Full game-object model — 22 typed data classes with explicit `Read` + `Write` |
| `ArcNET.Formats` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Formats?label=NuGet)](https://www.nuget.org/packages/ArcNET.Formats) | ✅ Ready | Binary format parsers/writers for MES, SEC, ART, DLG, SCR, PRO, MOB, JMP, FAC, TDF, GSI, TFAI, TFAF, PRP |
| `ArcNET.Archive` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Archive?label=NuGet)](https://www.nuget.org/packages/ArcNET.Archive) | ✅ Ready | DAT archive pack / unpack backed by `MemoryMappedFile`; TFAF sub-archive support |
| `ArcNET.GameData` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.GameData?label=NuGet)](https://www.nuget.org/packages/ArcNET.GameData) | ✅ Ready | `GameDataLoader` (MES, SEC, PRO, MOB wired; per-source origin tracking), `GameDataStore` with dirty tracking + GUID index + `*BySource` maps, `GameDataSaver` (preserves file origins), `GameDataExporter` (AOT-safe JSON) |
| `ArcNET.Patch` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Patch?label=NuGet)](https://www.nuget.org/packages/ArcNET.Patch) | ✅ Ready | HighRes patch configuration, installer, and uninstaller |
| `ArcNET.Dumpers` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Dumpers?label=NuGet)](https://www.nuget.org/packages/ArcNET.Dumpers) | ✅ Ready | Human-readable text dumpers for all game data formats (MES, SEC, ART, DLG, SCR, PRO, MOB, JMP, FAC, TDF, GSI, TFAI, TFAF, PRP) |
| `ArcNET.BinaryPatch` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.BinaryPatch?label=NuGet)](https://www.nuget.org/packages/ArcNET.BinaryPatch) | ✅ Ready | JSON-driven binary patching for bug fixes and mod authoring — field-level PRO/MOB mutations and raw byte patches with backup/revert/verify |
| `ArcNET.Editor` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Editor?label=NuGet)](https://www.nuget.org/packages/ArcNET.Editor) | ✅ Ready | Save-game editing pipeline (`SaveGame`, `SaveGameLoader`, `SaveGameWriter`) and fluent builders (`MobDataBuilder`, `CharacterBuilder`, `DialogBuilder`, `SectorBuilder`, `ScriptBuilder`) |

All packages target `net10.0`, carry no dependencies outside the BCL, and are AOT / trim compatible.

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
- **ArcNET.GameData** — load MES/SEC/PRO/MOB from directory or in-memory buffers (per-source origin tracking), save to disk / memory restoring original filenames, dirty tracking, AOT-safe JSON export
- **ArcNET.Patch** — install / uninstall the HighRes patch, read and modify `HighResConfig`
- **ArcNET.Dumpers** — human-readable text dumps for all parsed formats (mob, proto, sector, art, dialog, script, message, etc.)
- **ArcNET.BinaryPatch** — JSON-driven binary patching: field-level PRO/MOB mutations, raw byte offsets, backup/revert/verify, patch state tracking
- **ArcNET.Editor** — save-game round-trip: load a save slot (`SaveGameLoader`), inspect and mutate parsed mobiles / sectors / scripts, write back (`SaveGameWriter`); fluent `MobDataBuilder`, `CharacterBuilder`, `DialogBuilder`, `SectorBuilder`, and `ScriptBuilder` for constructing or editing objects
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

## Testing

TUnit tests use the Microsoft Testing Platform runner:

```shell
dotnet run --project src/Core/ArcNET.Core.Tests -c Release
dotnet run --project src/GameObjects/ArcNET.GameObjects.Tests -c Release
dotnet run --project src/Formats/ArcNET.Formats.Tests -c Release
dotnet run --project src/GameData/ArcNET.GameData.Tests -c Release
dotnet run --project src/Archive/ArcNET.Archive.Tests -c Release
dotnet run --project src/Patch/ArcNET.Patch.Tests -c Release
dotnet run --project src/BinaryPatch/ArcNET.BinaryPatch.Tests -c Release
dotnet run --project src/Editor/ArcNET.Editor.Tests -c Release
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
