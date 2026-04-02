# ArcNET

![.NET](https://img.shields.io/badge/net10.0-5C2D91?logo=.NET&labelColor=gray)
![C#](https://img.shields.io/badge/C%23-14.0-239120?labelColor=gray)
[![Build Status](https://github.com/Bia10/ArcNET/actions/workflows/dotnet.yml/badge.svg?branch=net10-refactor)](https://github.com/Bia10/ArcNET/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/Bia10/ArcNET/branch/main/graph/badge.svg)](https://codecov.io/gh/Bia10/ArcNET)
[![License](https://img.shields.io/github/license/Bia10/ArcNET)](https://github.com/Bia10/ArcNET/blob/main/LICENSE)

Read/write SDK for **Arcanum: Of Steamworks and Magick Obscura** game data formats.
Span-based, zero-allocation binary parsing with a UI-agnostic library API — usable from console tools, Avalonia editors, Blazor WASM, and anything in between.

⭐ Please star this project if you find it useful. ⭐

[Packages](#packages) · [Quick Example](#quick-example) · [Example Catalogue](#example-catalogue) · [Public API](docs/PublicApi.md)

---

## Packages

| Package | NuGet | Description |
|---|---|---|
| `ArcNET.Core` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Core?label=NuGet)](https://www.nuget.org/packages/ArcNET.Core) | `SpanReader` / `SpanWriter`, primitive types (`Location`, `ArtId`, `Color`, `GameObjectGuid`) |
| `ArcNET.GameObjects` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.GameObjects?label=NuGet)](https://www.nuget.org/packages/ArcNET.GameObjects) | Full game-object model — 22 typed data classes with explicit `Read` + `Write` |
| `ArcNET.Formats` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Formats?label=NuGet)](https://www.nuget.org/packages/ArcNET.Formats) | Binary format parsers/writers for MES, SEC, FAC, TDF and more |
| `ArcNET.GameData` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.GameData?label=NuGet)](https://www.nuget.org/packages/ArcNET.GameData) | `GameDataLoader` (directory + in-memory), `GameDataStore`, `GameDataSaver` |
| `ArcNET.Archive` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Archive?label=NuGet)](https://www.nuget.org/packages/ArcNET.Archive) | DAT archive pack / unpack backed by `MemoryMappedFile` |
| `ArcNET.Patch` | [![NuGet](https://img.shields.io/nuget/v/ArcNET.Patch?label=NuGet)](https://www.nuget.org/packages/ArcNET.Patch) | HighRes patch configuration, installer, and uninstaller |

All packages target `net10.0` and carry no dependencies outside the BCL.

---

## Quick Example

```csharp
using ArcNET.Formats;

// Parse a MES message file from disk — one allocation (File.ReadAllBytes)
IReadOnlyList<MessageEntry> messages = MessageFormat.ParseFile("arcanum/mes/game.mes");

// Or from a buffer you already own — zero extra allocations
ReadOnlyMemory<byte> buf = await File.ReadAllBytesAsync("game.mes");
messages = MessageFormat.ParseMemory(buf);

// Serialize back to bytes
byte[] bytes = MessageFormat.WriteToArray(messages);
```

For more examples see [Example Catalogue](#example-catalogue).

---

## Example Catalogue

### Open and extract a DAT archive

```csharp
using ArcNET.Archive;

using DatArchive archive = DatArchive.Open("arcanum.dat");

// Enumerate the entry table (FrozenDictionary — O(1) lookup, no allocation)
foreach (ArchiveEntry entry in archive.Entries)
    Console.WriteLine($"{entry.Path}  {entry.UncompressedSize:N0} bytes");

// Extract all entries to a directory
await DatExtractor.ExtractAllAsync(archive, outputDir: "extracted/");

// Read a single entry without loading the whole archive
ReadOnlyMemory<byte> data = archive.GetEntryData("art/CRITTERS/critter.art");
```

### Load and query game data

```csharp
using ArcNET.GameData;

// Source 1: extracted directory on disk
GameDataStore store = await new GameDataLoader().LoadFromDirectoryAsync("extracted/");

Console.WriteLine($"Loaded {store.Messages.Count} messages, {store.Objects.Count} objects");

// Source 2: pre-loaded byte blobs (editor / unit-test friendly — no filesystem needed)
IReadOnlyDictionary<string, ReadOnlyMemory<byte>> blobs = LoadBlobsFromSomewhere();
store = await new GameDataLoader().LoadFromMemoryAsync(blobs);

// Save changed files back
await new GameDataSaver().SaveToDirectoryAsync(store, "output/");

// Or serialize to memory for in-process round-trips
IReadOnlyDictionary<string, byte[]> result = new GameDataSaver().SaveToMemory(store);
```

### Parse a game object from raw bytes

```csharp
using ArcNET.Core;
using ArcNET.GameObjects;

byte[] raw = File.ReadAllBytes("critter.mob");
var reader = new SpanReader(raw);

GameObjectHeader header = GameObjectHeader.Read(ref reader);
Console.WriteLine($"Type: {header.ObjectType}  GUID: {header.Guid}");
```

### Install the HighRes patch

```csharp
using ArcNET.Patch;

var installer = new PatchInstaller();
await installer.InstallAsync(gameDir: @"C:\Games\Arcanum");

// Remove the patch later
var uninstaller = new PatchUninstaller();
await uninstaller.UninstallAsync(gameDir: @"C:\Games\Arcanum");
```

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
