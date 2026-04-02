# ArcNET

![.NET](https://img.shields.io/badge/net10.0-5C2D91?logo=.NET&labelColor=gray)
![C#](https://img.shields.io/badge/C%23-14.0-239120?labelColor=gray)
[![Build Status](https://github.com/Bia10/ArcNET/actions/workflows/dotnet.yml/badge.svg?branch=net10-refactor)](https://github.com/Bia10/ArcNET/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/Bia10/ArcNET/branch/main/graph/badge.svg)](https://codecov.io/gh/Bia10/ArcNET)
[![License](https://img.shields.io/github/license/Bia10/ArcNET)](https://github.com/Bia10/ArcNET/blob/main/LICENSE)

[![NuGet ArcNET.Core](https://img.shields.io/nuget/v/ArcNET.Core?label=ArcNET.Core)](https://www.nuget.org/packages/ArcNET.Core)
[![NuGet ArcNET.GameObjects](https://img.shields.io/nuget/v/ArcNET.GameObjects?label=ArcNET.GameObjects)](https://www.nuget.org/packages/ArcNET.GameObjects)
[![NuGet ArcNET.Formats](https://img.shields.io/nuget/v/ArcNET.Formats?label=ArcNET.Formats)](https://www.nuget.org/packages/ArcNET.Formats)
[![NuGet ArcNET.GameData](https://img.shields.io/nuget/v/ArcNET.GameData?label=ArcNET.GameData)](https://www.nuget.org/packages/ArcNET.GameData)
[![NuGet ArcNET.Archive](https://img.shields.io/nuget/v/ArcNET.Archive?label=ArcNET.Archive)](https://www.nuget.org/packages/ArcNET.Archive)
[![NuGet ArcNET.Patch](https://img.shields.io/nuget/v/ArcNET.Patch?label=ArcNET.Patch)](https://www.nuget.org/packages/ArcNET.Patch)

Read/write SDK for Arcanum: Of Steamworks and Magick Obscura game data formats.

## Repository structure

```
src/
  Core/       ArcNET.Core         — SpanReader/SpanWriter, primitives (Location, ArtId, Color, GameObjectGuid)
  GameObjects/ ArcNET.GameObjects  — Game object model, 22 type classes with Read + Write, GameObjectStore
  Formats/    ArcNET.Formats      — Binary format parsers/writers (MES, SEC, FAC, TDF, ...)
  GameData/   ArcNET.GameData     — GameDataLoader (dual-source: dir + memory), GameDataStore, GameDataSaver
  Archive/    ArcNET.Archive      — DAT archive read/write (MemoryMappedFile-backed)
  Patch/      ArcNET.Patch        — HighResConfig patch management, GitHubReleaseClient
  App/        ArcNET.App          — Console CLI application (Spectre.Console)
  Benchmarks/ ArcNET.Benchmarks   — BenchmarkDotNet perf benchmarks
```

## Package dependency graph

```
ArcNET.App (exe)
  ├── ArcNET.GameData
  │     ├── ArcNET.Formats
  │     │     ├── ArcNET.GameObjects
  │     │     │     └── ArcNET.Core
  │     │     └── ArcNET.Core
  │     ├── ArcNET.GameObjects
  │     └── ArcNET.Core
  ├── ArcNET.Archive
  │     └── ArcNET.Core
  └── ArcNET.Patch
        └── ArcNET.Core
```

## Building

```shell
dotnet tool restore
dotnet build ArcNET.Build.slnx -c Release
```

## Testing

TUnit tests use the Microsoft Testing Platform runner. Run each test project via `dotnet run`:

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
dotnet csharpier format .       # whitespace + brace style (run FIRST)
dotnet format style ArcNET.Build.slnx    # naming conventions, usings
dotnet format analyzers ArcNET.Build.slnx  # Roslyn analyzer violations
```

## Quick-start examples

### Parse a MES message file

```csharp
using ArcNET.Formats;

// From file
var messages = MessageFormat.ParseFile("path/to/game.mes");

// From memory (editor / Blazor scenario)
ReadOnlyMemory<byte> buf = File.ReadAllBytes("game.mes");
var messages = MessageFormat.ParseMemory(buf);

// Write back
byte[] bytes = MessageFormat.WriteToArray(messages);
```

### Open a DAT archive

```csharp
using ArcNET.Archive;

using var archive = DatArchive.Open("arcanum.dat");
foreach (var entry in archive.Entries)
    Console.WriteLine($"{entry.Path}  {entry.UncompressedSize} bytes");

// Extract all
await DatExtractor.ExtractAllAsync(archive, outputDir: "extracted/");
```

### Load game data

```csharp
using ArcNET.GameData;

// From extracted directory
var store = await GameDataLoader.LoadFromDirectoryAsync("extracted/");
Console.WriteLine($"Loaded {store.Messages.Count} messages");

// Save back
await GameDataSaver.SaveToDirectoryAsync(store, "output/");
```

### Parse a game object from raw bytes

```csharp
using ArcNET.Core;
using ArcNET.GameObjects;
using ArcNET.GameObjects.Types;

byte[] raw = File.ReadAllBytes("critter.mob");
var reader = new SpanReader(raw);
var header = GameObjectHeader.Read(ref reader);

// type dispatch based on header.GameObjectType
```

### Install HighRes patch

```csharp
using ArcNET.Patch;

var installer = new PatchInstaller();
await installer.InstallAsync(gameDir: @"C:\Games\Arcanum");
```

## Public API Reference

See [docs/PublicApi.md](docs/PublicApi.md) for the complete public API reference.

## License

MIT — Copyright (c) 2026 Bia10
