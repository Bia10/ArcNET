# ArcNET

![.NET](https://img.shields.io/badge/net10.0-5C2D91?logo=.NET&labelColor=gray)
![C#](https://img.shields.io/badge/C%23-14.0-239120?labelColor=gray)
[![Build Status](https://github.com/Bia10/ArcNET/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/Bia10/ArcNET/actions/workflows/dotnet.yml)
[![License](https://img.shields.io/github/license/Bia10/ArcNET)](https://github.com/Bia10/ArcNET/blob/main/LICENSE)

Read/write SDK for Arcanum: Of Steamworks and Magick Obscura game data formats.

## Repository structure

```
src/
  Core/       ArcNET.Core         — SpanReader/SpanWriter, primitives (Location, ArtId, Color, GameObjectGuid)
  GameObjects/ ArcNET.GameObjects  — Game object model, GameObjectStore
  Formats/    ArcNET.Formats      — Binary format parsers (MessageFormat, SectorFormat, FileFormat lookup)
  GameData/   ArcNET.GameData     — High-level GameDataLoader (file discovery, message loading)
  Archive/    ArcNET.Archive      — DAT archive read support
  Patch/      ArcNET.Patch        — HighResConfig INI parser/writer, GitHubReleaseClient
  App/        ArcNET.App          — Console CLI application (Spectre.Console)
  Benchmarks/ ArcNET.Benchmarks   — BenchmarkDotNet perf benchmarks
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
dotnet format style             # naming conventions, usings
dotnet format analyzers         # Roslyn analyzer violations
```

## Public API Reference

See [docs/PublicApi.md](docs/PublicApi.md) for the complete public API reference.

## License

MIT — Copyright (c) 2026 Bia10
