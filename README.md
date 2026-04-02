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
| `ArcNET.Formats` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Formats?label=NuGet)](https://www.nuget.org/packages/ArcNET.Formats) | ✅ Ready | Binary format parsers/writers for MES, SEC, ART, DLG, SCR, PRO, MOB, JMP, FAC, TDF, GSI, SVG, TER, PRP |
| `ArcNET.Archive` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Archive?label=NuGet)](https://www.nuget.org/packages/ArcNET.Archive) | ✅ Ready | DAT archive pack / unpack backed by `MemoryMappedFile`; TFAF sub-archive support |
| `ArcNET.GameData` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.GameData?label=NuGet)](https://www.nuget.org/packages/ArcNET.GameData) | 🚧 Preview | `GameDataLoader` (messages fully wired; other formats in progress), `GameDataStore` with dirty tracking + GUID index, `GameDataSaver`, `GameDataExporter` |
| `ArcNET.Patch` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Patch?label=NuGet)](https://www.nuget.org/packages/ArcNET.Patch) | ✅ Ready | HighRes patch configuration, installer, and uninstaller |

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

- **ArcNET.Formats** — MES, SEC, ART, DLG, SCR, PRO, MOB, JMP, FAC, TDF, GSI, SVG, TER, PRP parsers; round-trip serialization; file discovery
- **ArcNET.Archive** — open, enumerate, extract single/all entries, read without extracting, pack a directory, TFAF sub-archive
- **ArcNET.GameObjects** — read full game objects, read headers only, `GameObjectStore`
- **ArcNET.GameData** — load from directory or in-memory buffers, save to disk / memory, dirty tracking, JSON export
- **ArcNET.Patch** — install / uninstall the HighRes patch, read and modify `HighResConfig`
- **ArcNET.Core** — low-level `SpanReader` / `SpanWriter`, primitive round-trips, `EnumLookup`

---

## Package dependency graph

```
ArcNET.App (exe)
  ├── ArcNET.GameData
  │     ├── ArcNET.Formats
  │     │     ├── ArcNET.GameObjects → ArcNET.Core
  │     │     └── ArcNET.Core
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

## License

MIT — Copyright (c) 2026 Bia10
