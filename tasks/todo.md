# ArcNET — .NET 10 Architectural Rewrite Plan

> **Goal**: Transform ArcNET from a net5.0 WIP terminal tool into a professional .NET 10 mono-repo with
> NuGet-published core libraries and a UI-agnostic SDK, plus a single console executable on top.
> Consumers of the libraries can be Avalonia, Uno Platform, TUI, Blazor, or any other host.
>
> **Scaffolding authority**: Follow `C:\Users\Bia\source\repos\SolutionScafolding\Scaffold_LibSolution_AgentGuide.md`
> (lib guide — the more updated one). Pull in app-guide patterns only for the hierarchical `Directory.Build.props`
> and the solution folder layout (Phase 3 of the app guide).
>
> **Formatting rule**: always run `dotnet csharpier format .` first, then `dotnet format style` and
> `dotnet format analyzers` — never bare `dotnet format`. See lib guide Phase 1.16a.

---

## Resolved inputs (Phase 0 variables)

| Variable          | Value                                               |
| ----------------- | --------------------------------------------------- |
| `LIBNAME`         | `ArcNET` (root; each sub-package prefixes this)     |
| `ROOTNS`          | `ArcNET`                                            |
| `DESCRIPTION`     | `Read/write SDK for Arcanum: Of Steamworks and Magick Obscura game data formats` |
| `AUTHOR`          | `Bia10`                                             |
| `DOTNET_VERSION`  | `net10.0`                                           |
| `CSHARP_VERSION`  | `14.0`                                              |
| `SDK_VERSION`     | `10.0.201`                                          |
| `NEEDS_UNSAFE`    | `true` (binary readers use Span<byte>/unsafe)       |
| `NEEDS_AOT`       | `false` (game tool, not a trimmed mobile lib)       |
| `WRAPS_NATIVE`    | `false`                                             |
| `GITHUB_URL`      | `https://github.com/Bia10/ArcNET`                  |
| `LICENSE`         | `MIT`                                               |
| `YEAR`            | `2026`                                              |
| `COMPANY`         | `Bia10`                                             |
| `NEEDS_GENERATOR` | `false`                                             |
| `TARGET_PLATFORMS`| `win-x64`                                           |

---

## Target solution structure

```
ArcNET/                                   ← repo root
├── .github/
│   ├── workflows/dotnet.yml
│   ├── ISSUE_TEMPLATE/
│   ├── PULL_REQUEST_TEMPLATE.md
│   └── dependabot.yml
├── .config/dotnet-tools.json             ← CSharpier local tool
├── docs/
│   └── PublicApi.md                      ← auto-generated (one per lib, or combined)
├── tasks/
│   ├── todo.md                           ← this file
│   └── lessons.md
├── src/
│   ├── Directory.Build.props             ← Tier 1: global (all projects)
│   ├── Directory.Build.targets
│   ├── Directory.Packages.props          ← Central Package Management
│   │
│   ├── Core/
│   │   ├── ArcNET.Core/                  ← NuGet: primitives (no dependencies)
│   │   └── ArcNET.Core.Tests/
│   │
│   ├── GameObjects/
│   │   ├── ArcNET.GameObjects/           ← NuGet: full game-object model
│   │   └── ArcNET.GameObjects.Tests/
│   │
│   ├── Formats/
│   │   ├── ArcNET.Formats/               ← NuGet: readers + writers for all file formats
│   │   └── ArcNET.Formats.Tests/
│   │
│   ├── GameData/
│   │   ├── ArcNET.GameData/              ← NuGet: high-level data management, GameObjectManager
│   │   └── ArcNET.GameData.Tests/
│   │
│   ├── Archive/
│   │   ├── ArcNET.Archive/               ← NuGet: DAT archive pack/unpack
│   │   └── ArcNET.Archive.Tests/
│   │
│   ├── Patch/
│   │   ├── ArcNET.Patch/                 ← NuGet: HighRes patch + UAP install management
│   │   └── ArcNET.Patch.Tests/
│   │
│   ├── App/
│   │   └── Directory.Build.props         ← Tier 3: OutputType=Exe, version stamp
│   │
│   └── ArcNET.App/                       ← Exe: console entry point (NOT NuGet)
│
├── benchmarks/
│   └── ArcNET.Benchmarks/                ← BenchmarkDotNet perf suite
├── global.json
├── nuget.config
├── ArcNET.slnx
├── Build.cs                              ← task runner (build/test/format/bench/publish)
├── Icon.png                              ← 128x128 (required for NuGet packages)
├── .editorconfig
├── .csharpierrc.json
├── .gitattributes
├── .gitignore
├── .jscpd.json
├── .markdownlint.json
├── .pre-commit-config.yaml
├── codecov.yml
├── LICENSE
├── SECURITY.md
├── CONTRIBUTING.md
├── CHANGELOG.md
└── README.md
```

### NuGet package dependency graph

```
ArcNET.Core          (no deps — primitives only)
ArcNET.GameObjects   → ArcNET.Core
ArcNET.Formats       → ArcNET.Core, ArcNET.GameObjects
ArcNET.GameData      → ArcNET.Core, ArcNET.GameObjects, ArcNET.Formats
ArcNET.Archive       → ArcNET.Core
ArcNET.Patch         → ArcNET.Core
ArcNET.App (exe)     → ArcNET.GameData, ArcNET.Archive, ArcNET.Patch + Spectre.Console
```

---

## Library responsibilities

### `ArcNET.Core`
Namespace: `ArcNET.Core`
- Shared value types: `ArtId`, `Location`, `Color`, `GameObjectGuid`, `PrefixedString`
- Enums used across layers: `GameObjectType`, `Material`, `Category`
- Extension methods on `BinaryReader`/`BinaryWriter` for Arcanum primitives
  - `ReadLocation()`, `ReadGameObjectGuid()`, `ReadArtId()`, `ReadPrefixedString()`
  - Symmetric write counterparts for all of them
- `IBinarySerializable` interface: `Read(BinaryReader)` / `Write(BinaryWriter)`
- **No external NuGet dependencies**

### `ArcNET.GameObjects`
Namespace: `ArcNET.GameObjects`
- `GameObjectHeader` with `Bitmap` field-presence logic
- Base `GameObject` + `IGameObject`
- All 22 type classes (PC, NPC, Critter, Weapon, Armor, Ammo, Container, Portal, Gold, Food, Scroll, Key, KeyRing, Written, Generic, Scenery, Wall, Projectile, Trap, Unknown)
- All flag enums (`CritterFlags1`, `CritterFlags2`, `BlitFlags`, `SpellFlags`, `BlockingFlags`, ...)
- Specialized classes: `Monster`, `Unique`, `Entity`, `NPC` (game-world instances)
- Supporting types: `Inventory`, `InventorySource`, `InventorySourceBuy`, `Script`, `Overlay`
- Remove `[Order(n)]`-reflection pattern → explicit ordered `Read`/`Write` per type
- Depends on: `ArcNET.Core`

### `ArcNET.Formats`
Namespace: `ArcNET.Formats`
- Format-specific reader+writer pairs, one class per format:
  - `FacWalkReader` / `FacWalkWriter`
  - `MessageReader` / `MessageWriter` (MES)
  - `SectorReader` / `SectorWriter` (SEC)
  - `ProtoReader` / `ProtoWriter` (PRO)
  - `MobReader` / `MobWriter` (MOB)
  - `ArtReader` / `ArtWriter` (ART)
  - `JmpReader` / `JmpWriter` (JMP)
  - `ScriptReader` / `ScriptWriter` (SCR)
  - `DialogReader` / `DialogWriter` (DLG)
  - `TerrainReader` / `TerrainWriter` (TDF)
  - `MapPropertiesReader` / `MapPropertiesWriter` (PRP)
- Common reader/writer infrastructure:
  - `IFormatReader<T>` / `IFormatWriter<T>` interfaces
  - `FormatDetector` — file-extension-to-format mapping
- `TextDataReader` stays (MES, TDF text variants)
- Depends on: `ArcNET.Core`, `ArcNET.GameObjects`

### `ArcNET.GameData`
Namespace: `ArcNET.GameData`
- `GameDataStore` — replaces static `GameObjectManager`; injectable, thread-safe
- `GameDataLoader` — orchestrates multi-file parsing (replaces `Parser.cs`)
- `GameDataLoader.LoadFromDirectory(string path)` — single entry point for consumers
- JSON export via `System.Text.Json` (replace Newtonsoft.Json)
- Depends on: `ArcNET.Core`, `ArcNET.GameObjects`, `ArcNET.Formats`

### `ArcNET.Archive`
Namespace: `ArcNET.Archive`
- `DatArchive` — open/read/write Arcanum `.dat` archives
- `DatExtractor` — extract all or selected files
- `DatPacker` — pack directory into `.dat`
- Depends on: `ArcNET.Core`

### `ArcNET.Patch`
Namespace: `ArcNET.Patch`
- `HighResConfig` — read/write HighRes patch configuration
- `PatchInstaller` / `PatchUninstaller` — install UAP + HighRes
- Remove `LibGit2Sharp` — use `Process.Start("git", ...)` or plain HTTP download
- Depends on: `ArcNET.Core`

### `ArcNET.App` (exe)
Namespace: `ArcNET`
- Console entry point using `Spectre.Console`
- Interactive menu driven by `GameDataLoader` and `GameDataStore`
- HighRes patch install/uninstall UI
- Explicit dependency on `Spectre.Console` (only layer that may)
- **No game logic** — pure thin orchestration of library calls
- Depends on: all six libraries above

---

## Modern C# 14 / .NET 10 idioms

This section defines the language and runtime features that agents **must** apply throughout the rewrite.
They are not optional polish — they shape the public API surface and determine whether consumers can build editors on top of the libraries.

### 1. Span-based parsing core — `SpanReader` and `SpanWriter`

Replace `BinaryReader` / `BinaryWriter` with a pair of `ref struct` cursors defined in `ArcNET.Core`.
These are the **only** way formats read from / write to raw bytes.

```csharp
// ArcNET.Core — SpanReader.cs
public ref struct SpanReader(ReadOnlySpan<byte> data)
{
    private ReadOnlySpan<byte> _remaining = data;
    public int Position { get; private set; }
    public int Remaining => _remaining.Length;

    public byte   ReadByte()   { var v = _remaining[0]; Advance(1); return v; }
    public ushort ReadUInt16() { var v = BinaryPrimitives.ReadUInt16LittleEndian(_remaining); Advance(2); return v; }
    public uint   ReadUInt32() { var v = BinaryPrimitives.ReadUInt32LittleEndian(_remaining); Advance(4); return v; }
    public int    ReadInt32()  { var v = BinaryPrimitives.ReadInt32LittleEndian(_remaining);  Advance(4); return v; }
    public ReadOnlySpan<byte> ReadBytes(int count) { var s = _remaining[..count]; Advance(count); return s; }

    // Domain-specific helpers live in ArcNET.Core extension methods:
    // SpanReaderExtensions.ReadLocation(ref this SpanReader r) → Location
    // SpanReaderExtensions.ReadArtId(ref this SpanReader r)    → ArtId
    // etc.

    private void Advance(int count) { _remaining = _remaining[count..]; Position += count; }
}

// ArcNET.Core — SpanWriter.cs
public ref struct SpanWriter(IBufferWriter<byte> output)
{
    private readonly IBufferWriter<byte> _output = output;

    public void WriteByte(byte v)     { var s = _output.GetSpan(1); s[0] = v; _output.Advance(1); }
    public void WriteUInt16(ushort v) { var s = _output.GetSpan(2); BinaryPrimitives.WriteUInt16LittleEndian(s, v); _output.Advance(2); }
    public void WriteUInt32(uint v)   { var s = _output.GetSpan(4); BinaryPrimitives.WriteUInt32LittleEndian(s, v); _output.Advance(4); }
    public void WriteInt32(int v)     { var s = _output.GetSpan(4); BinaryPrimitives.WriteInt32LittleEndian(s, v);  _output.Advance(4); }
    public void WriteBytes(ReadOnlySpan<byte> data) { data.CopyTo(_output.GetSpan(data.Length)); _output.Advance(data.Length); }
}
```

**Why `IBufferWriter<byte>` on the writer side**: it decouples the format logic from any concrete output.
The same `Write()` call works against `ArrayBufferWriter<byte>` (in-memory), `PipeWriter` (streaming),
a pre-allocated `Span<byte>` via `FixedSizeBufferWriter`, or a `MemoryStream` wrapper.
Consumers never need to know where the bytes go.

### 2. Format API shape (the public contract)

Every format class in `ArcNET.Formats` exposes this exact shape:

```csharp
// Static Parse + Format methods — no allocation for the parse itself
public static T Parse(scoped ref SpanReader reader);           // ← ref struct input
public static void Write(in T value, ref SpanWriter writer);   // ← in = no copy for structs

// Convenience overloads for disk/stream consumers (thin wrappers only):
public static T ParseFile(string path);                        // ReadAllBytes → Parse
public static T ParseMemory(ReadOnlyMemory<byte> memory);      // Memory.Span → Parse
public static byte[] WriteToArray(in T value);                 // ArrayBufferWriter → Write → ToArray
public static void WriteToFile(in T value, string path);       // WriteToArray → File.WriteAllBytes
```

This means:
- An **in-memory editor** calls `Parse(ref reader)` on a buffer it already owns — zero extra allocations.
- A **file-backed tool** calls `ParseFile(path)` — one `File.ReadAllBytes` allocation, nothing more.
- A **streaming pipeline** calls `Parse(ref reader)` on a `SequenceReader<byte>`-derived cursor.
- A **Blazor WASM editor** calls `ParseMemory(jsMemory)` on a `ReadOnlyMemory<byte>` it received from JS interop.

### 3. Value types — `readonly record struct`

All primitives in `ArcNET.Core` are `readonly record struct`. This gives:
- Stack allocation (no heap pressure in hot parsing loops)
- Structural equality for free (testing, diffing)
- `with` expressions for non-destructive mutation (editor-friendly)
- `ISpanFormattable` implementation for allocation-free display

```csharp
public readonly record struct Location(short X, short Y) : ISpanFormattable
{
    public bool TryFormat(Span<char> dest, out int written, ReadOnlySpan<char> format, IFormatProvider? provider)
        => dest.TryWrite($"({X}, {Y})", out written);
    public string ToString(string? format, IFormatProvider? provider) => $"({X}, {Y})";
}

public readonly record struct ArtId(uint Value) : ISpanFormattable, IUtf8SpanFormattable { ... }
public readonly record struct GameObjectGuid(uint Type, uint Foo0, uint Foo2, uint Guid) : ISpanFormattable { ... }
public readonly record struct Color(byte R, byte G, byte B) : ISpanFormattable { ... }
```

### 4. `FrozenDictionary` / `FrozenSet` for lookup tables

Any table that is built once and read many times uses frozen collections:

```csharp
// ArcNET.Formats — FormatDetector.cs
public static class FormatDetector
{
    // Built once at startup, then frozen — zero allocation per lookup
    private static readonly FrozenDictionary<string, FileFormat> s_byExtension =
        new Dictionary<string, FileFormat>(StringComparer.OrdinalIgnoreCase)
        {
            [".sec"] = FileFormat.Sector,
            [".pro"] = FileFormat.Proto,
            [".mes"] = FileFormat.Message,
            // ...
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static FileFormat Detect(ReadOnlySpan<char> extension) =>
        s_byExtension.TryGetValue(extension, out var fmt) ? fmt : FileFormat.Unknown;
}

// ArcNET.GameObjects — flag lookup tables also use FrozenSet
private static readonly FrozenSet<GameObjectType> s_critterTypes =
    new[] { GameObjectType.PC, GameObjectType.NPC }.ToFrozenSet();
```

### 5. `in` / `ref` / `scoped` / `out` usage rules

| Scenario | Keyword |
|---|---|
| Passing a `readonly record struct` to a method that only reads it | `in` — avoids defensive copy |
| Passing `SpanReader` / `SpanWriter` (ref struct) | `ref` — mutates the cursor position |
| Ref struct parameter that must not escape the method | `scoped ref` |
| Format round-trip test output | `out` |
| Short-lived buffer | `stackalloc Span<byte>` for ≤ 256 bytes, `ArrayPool<byte>.Shared.Rent()` for larger |

```csharp
// ✅ Correct: ref struct cursor passed by ref; result is a value type
public static Location ReadLocation(ref SpanReader reader)
{
    var x = reader.ReadInt16();
    var y = reader.ReadInt16();
    return new Location(x, y);
}

// ✅ Correct: in for a struct that won't be mutated
public static void WriteLocation(in Location loc, ref SpanWriter writer)
{
    writer.WriteInt16(loc.X);
    writer.WriteInt16(loc.Y);
}

// ✅ Correct: stackalloc for small fixed-size scratch
Span<byte> header = stackalloc byte[16];
reader.ReadBytes(16).CopyTo(header);
```

### 6. Dual I/O surface — file-on-disk vs. client-memory

The design goal is that an **editor** (Avalonia, Blazor) can:
1. Load a file into a `byte[]` / `IMemoryOwner<byte>`
2. Parse it to a mutable model
3. Modify the model
4. Write it back to bytes in-memory without ever touching the filesystem

```
┌──────────────────────────────────────────────────────────┐
│                  Editor / Consumer                        │
│  e.g. Avalonia, Blazor WASM, TUI                         │
└───────────────────┬──────────────────────────────────────┘
                    │  IGameObject (mutable, editor-friendly)
                    │  GameDataStore.Objects, .Sectors, etc.
┌───────────────────▼──────────────────────────────────────┐
│               ArcNET.GameData                             │
│  GameDataStore  — mutable, observable                     │
│  GameDataLoader — disk or memory source                   │
│  GameDataSaver  — to disk or memory target                │
└───────────────────┬──────────────────────────────────────┘
                    │  IFormatReader<T> / IFormatWriter<T>
                    │  Format.Parse(ref SpanReader)
                    │  Format.Write(in T, ref SpanWriter)
┌───────────────────▼──────────────────────────────────────┐
│               ArcNET.Formats                              │
│  Span-based stateless parse/write — no I/O inside        │
└───────────────────┬──────────────────────────────────────┘
                    │  SpanReader / SpanWriter / primitives
┌───────────────────▼──────────────────────────────────────┐
│               ArcNET.Core                                 │
│  ref struct SpanReader(ReadOnlySpan<byte>)                │
│  ref struct SpanWriter(IBufferWriter<byte>)               │
│  readonly record struct Location, ArtId, GameObjectGuid  │
│  FrozenDictionary lookup tables                           │
│  ISpanFormattable / IUtf8SpanFormattable on all types    │
└──────────────────────────────────────────────────────────┘
```

**`GameDataLoader` dual-source API:**

```csharp
// ArcNET.GameData — GameDataLoader.cs
public sealed class GameDataLoader
{
    // Source 1: directory of extracted files (tool / CLI use)
    public Task<GameDataStore> LoadFromDirectoryAsync(
        string path,
        IProgress<float>? progress = null,
        CancellationToken ct = default);

    // Source 2: pre-loaded bytes keyed by filename (editor / unit test use)
    public Task<GameDataStore> LoadFromMemoryAsync(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files,
        IProgress<float>? progress = null,
        CancellationToken ct = default);
}

// ArcNET.GameData — GameDataSaver.cs
public sealed class GameDataSaver
{
    // Target 1: write changed files back to directory
    public Task SaveToDirectoryAsync(
        GameDataStore store,
        string path,
        IProgress<float>? progress = null,
        CancellationToken ct = default);

    // Target 2: write to memory (editor round-trip, no filesystem needed)
    public IReadOnlyDictionary<string, byte[]> SaveToMemory(GameDataStore store);
}
```

**`GameDataStore` — mutable, editor-friendly:**

```csharp
// ArcNET.GameData — GameDataStore.cs
public sealed class GameDataStore
{
    // Mutable collections — editors can add/remove/replace
    public List<IGameObject> Objects { get; }
    public List<Sector> Sectors { get; }
    public List<Monster> Monsters { get; }
    public List<NpcInstance> Npcs { get; }
    public List<UniqueInstance> Uniques { get; }
    public List<MessageEntry> Messages { get; }

    // Dirty tracking — consumers subscribe to know what changed
    public event EventHandler<GameObjectChangedEventArgs>? ObjectChanged;

    // Lookup helpers backed by FrozenDictionary (rebuilt on demand)
    public IGameObject? FindById(GameObjectGuid id);
    public IReadOnlyList<T> GetByType<T>() where T : IGameObject;

    // For editors: mark a specific object dirty
    public void MarkDirty(GameObjectGuid id);
    public IReadOnlySet<GameObjectGuid> DirtyObjects { get; }
    public void ClearDirty();
}
```

### 7. Memory-mapped access for large files (Archive)

`ArcNET.Archive` should use `MemoryMappedFile` for large `.dat` files so extraction does not require
loading the entire archive into memory:

```csharp
// ArcNET.Archive — DatArchive.cs
public sealed class DatArchive : IDisposable
{
    private readonly MemoryMappedFile _mmf;
    private readonly FrozenDictionary<string, DatEntry> _entries;

    public static DatArchive Open(string path) { ... }

    // Returns a Memory<byte> slice — no copy, backed by the mapped view
    public ReadOnlyMemory<byte> GetEntryData(string name) { ... }

    // Streams a single entry without loading others
    public Stream OpenEntry(string name) { ... }
}
```

### 8. `stackalloc` policy

- Use `stackalloc Span<byte>` for fixed-size intermediate buffers **≤ 256 bytes** (e.g., reading a struct header).
- Use `ArrayPool<byte>.Shared.Rent(size)` for larger variable-length buffers; always in a `try/finally` with `Return`.
- Never `stackalloc` inside a loop without profiling — each iteration gets a fresh stack frame allocation.

```csharp
// ✅ Safe: fixed small header
Span<byte> magic = stackalloc byte[8];
reader.ReadBytes(8).CopyTo(magic);

// ✅ Safe: large variable buffer via pool
byte[] rented = ArrayPool<byte>.Shared.Rent(entrySize);
try { ... }
finally { ArrayPool<byte>.Shared.Return(rented); }
```

### 9. `allows ref struct` generic constraint (C# 13 / .NET 9+, available in C# 14)

Use this constraint on generic helpers in `ArcNET.Core` that need to accept `SpanReader` / `SpanWriter`:

```csharp
// Enables generic parse helpers that work with ref struct cursors
public static T[] ReadArray<T, TReader>(ref TReader reader, int count)
    where TReader : allows ref struct
    where T : IBinarySerializable<TReader>
{
    var arr = new T[count];
    for (var i = 0; i < count; i++)
        arr[i] = T.Read(ref reader);
    return arr;
}
```

### 10. `ISpanFormattable` / `IUtf8SpanFormattable` on all Core types

Every primitive type implements both interfaces so they integrate with `Span<char>` formatting
(used by `string.Create`, interpolated string handlers, logging sinks) without boxing or temporary strings:

```csharp
public readonly record struct ArtId(uint Value)
    : ISpanFormattable, IUtf8SpanFormattable
{
    public bool TryFormat(Span<char> dest, out int written,
        ReadOnlySpan<char> format, IFormatProvider? provider)
        => dest.TryWrite($"0x{Value:X8}", out written);

    public bool TryFormat(Span<byte> utf8Dest, out int written,
        ReadOnlySpan<char> format, IFormatProvider? provider)
        => Utf8.TryWrite(utf8Dest, $"0x{Value:X8}", out written);

    public override string ToString() => $"0x{Value:X8}";
}
```

---

## Key architectural decisions

| Decision | Rationale |
|---|---|
| Drop reflection-based `[Order(n)]` deserialization | Fragile, slow, not AOT-friendly; replace with explicit ordered property reads in each type |
| `ref struct SpanReader` / `SpanWriter` replace `BinaryReader`/`BinaryWriter` | Zero-allocation parsing; works on any `ReadOnlySpan<byte>` source (file, memory, network) |
| `IBufferWriter<byte>` as write target | Decouples format logic from output destination; same code writes to memory, files, or pipes |
| `IFormatReader<T>` / `IFormatWriter<T>` interfaces | Symmetric read/write surface; allows consumers to implement custom formats |
| `readonly record struct` for all primitives | Stack allocation, structural equality, `with` expressions for editor mutations, no boxing |
| `ISpanFormattable` + `IUtf8SpanFormattable` on Core types | Allocation-free display in logs, UI bindings, JSON serialization |
| `FrozenDictionary`/`FrozenSet` for lookup tables | Zero allocation per lookup after startup (format registry, type dispatch, flag tables) |
| Dual-source `GameDataLoader` (directory + memory) | Editor consumers never need the filesystem; unit tests use in-memory blobs |
| Dual-target `GameDataSaver` (directory + memory) | Round-trip editing without disk; enables Blazor WASM scenarios |
| Dirty tracking on `GameDataStore` | Editors know exactly what to re-serialize; avoids full-store save on every change |
| `MemoryMappedFile` in `ArcNET.Archive` | Large DAT files never fully loaded into RAM; entry extraction is a zero-copy slice |
| `GameDataStore` (non-static, injectable) | Replaces static `GameObjectManager`; supports unit testing and multi-instance use |
| `System.Text.Json` instead of Newtonsoft.Json | Already in runtime; no extra dep; better performance; source-gen friendly |
| Remove `LibGit2Sharp` | Heavy native dep; not needed — git CLI or plain HTTP fetch is sufficient |
| No `Spectre.Console` in any library | UI-agnostic constraint; only `ArcNET.App` may reference it |
| `MinVer` per publishable library | Semver from git tags; each library tagged independently (e.g., `arcnet.core-v1.0.0`) |
| TUnit for all tests | Only test framework used (per project instructions) |
| Central Package Management | Single `Directory.Packages.props`; all `.csproj` omit `Version` |

---

## Migration mapping (old → new)

| Old | New |
|---|---|
| `ArcNET.DataTypes` (library) | Split into `ArcNET.Core` + `ArcNET.GameObjects` + `ArcNET.Formats` |
| `ArcNET.Terminal` (exe) | `ArcNET.App` (exe) |
| `ArcNET.Utilities` (library) | `ArcNET.Patch` (extracted) + `ArcNET.Archive` (new) |
| `GameObjectManager` (static) | `GameDataStore` (injectable) in `ArcNET.GameData` |
| `Parser.cs` (monolith, 650+ lines) | `GameDataLoader` in `ArcNET.GameData` |
| `Terminal.cs` (UI helpers) | `ArcNET.App` internal helpers |
| `BinaryReaderExtensions.cs` | `ArcNET.Core` extensions + symmetric write counterparts |
| `Newtonsoft.Json` | `System.Text.Json` |
| `LibGit2Sharp` | Removed |
| net5.0 | net10.0 |

---

## Phases (agent task list)

### Phase 1 — Repository root scaffold
- [ ] Delete old `ArcNET.sln`, `ArcNET.sln.DotSettings`; keep source code
- [ ] Create `global.json` (SDK 10.0.201, rollForward latestPatch)
- [ ] Create `nuget.config` (clear + nuget.org only)
- [ ] Run `dotnet new gitignore` then append `artifacts/` and `BenchmarkDotNet.Artifacts/`
- [ ] Create `.gitattributes` (LF everywhere per lib guide 1.4)
- [ ] Create `.editorconfig` (full template from lib guide 1.5 + app guide 1.5a additions)
- [ ] Create `.jscpd.json` (threshold 5)
- [ ] Create `.markdownlint.json`
- [ ] Create `.csharpierrc.json` (printWidth 120, lf)
- [ ] Create `.config/dotnet-tools.json` (CSharpier latest)
- [ ] Create `.pre-commit-config.yaml` (gitleaks + end-of-file + CSharpier hook)
- [ ] Create `codecov.yml`
- [ ] Create `LICENSE` (MIT, 2026, Bia10)
- [ ] Create `SECURITY.md`
- [ ] Create `CONTRIBUTING.md`
- [ ] Create `CHANGELOG.md`
- [ ] Create `.github/FUNDING.yml`
- [ ] Create `.github/ISSUE_TEMPLATE/bug_report.md`
- [ ] Create `.github/ISSUE_TEMPLATE/feature_request.md`
- [ ] Create `.github/PULL_REQUEST_TEMPLATE.md`
- [ ] Create `.github/dependabot.yml`
- [ ] Generate 128x128 `Icon.png` placeholder via PowerShell
- [ ] Create `README.md` (with badges, architecture table, getting-started)
- [ ] Create `docs/PublicApi.md` placeholder
- [ ] Create `ArcNET.slnx` (solution file with all projects in solution folders)

### Phase 2 — Central build infrastructure
- [ ] Create `src/Directory.Packages.props` with CPM (all package versions centrally managed)
  - CSharpier.MsBuild, MinVer, TUnit, BenchmarkDotNet, NSubstitute
  - Spectre.Console (App only)
  - System.IO.Pipelines (Core — optional high-throughput path)
  - Resolve all latest stable versions via `dotnet package search <name> --take 1`
  - Note: System.Text.Json, System.Buffers, System.Memory are in-box on net10.0 — no PackageVersion needed
- [ ] Create `src/Directory.Build.props` (Tier 1: global — all projects)
  - net10.0, C# 14, TreatWarningsAsErrors, Nullable enable, UseArtifactsOutput, TestingPlatformDotnetTestSupport
  - AllowUnsafeBlocks=true (needed for SpanReader/stackalloc patterns)
  - MinVer + CSharpier.MsBuild in every project
- [ ] Create `src/Directory.Build.targets` (minimal)
- [ ] Create `src/App/Directory.Build.props` (Tier 3: OutputType=Exe, version 0.1.0)
- [ ] Create `src/Tests/Directory.Build.props` (Tier 3: test-layer overrides, IsPackable=false)
- [ ] Create `Build.cs` task runner (build / test / format / format-check / bench / publish commands)

### Phase 3 — `ArcNET.Core` library
- [ ] Create `src/Core/ArcNET.Core/ArcNET.Core.csproj`
  - IsPackable=true, AllowUnsafeBlocks=true, MinVer tags prefix `arcnet.core-v`
  - InternalsVisibleTo ArcNET.Core.Tests
- [ ] Implement `ref struct SpanReader(ReadOnlySpan<byte> data)`
  - `ReadByte/UInt16/Int16/UInt32/Int32/Int64/Single/Double` via `BinaryPrimitives` (no allocations)
  - `ReadBytes(int count) → ReadOnlySpan<byte>`
  - `Position`, `Remaining`, `TryPeek`
  - `Slice(int length) → SpanReader` — sub-reader for bounded parsing
- [ ] Implement `ref struct SpanWriter(IBufferWriter<byte> output)`
  - `WriteByte/UInt16/Int16/UInt32/Int32/Int64/Single/Double` via `BinaryPrimitives`
  - `WriteBytes(ReadOnlySpan<byte>)`
  - Backed by `IBufferWriter<byte>` — works against `ArrayBufferWriter`, `PipeWriter`, custom targets
- [ ] Define `IBinarySerializable<TReader>` interface (with `allows ref struct` constraint):
  ```csharp
  interface IBinarySerializable<TSelf, TReader>
      where TSelf : IBinarySerializable<TSelf, TReader>
      where TReader : allows ref struct
  {
      static abstract TSelf Read(ref TReader reader);
      void Write(ref SpanWriter writer);
  }
  ```
- [ ] Rewrite all primitives as `readonly record struct` with `ISpanFormattable` + `IUtf8SpanFormattable`:
  - `ArtId(uint Value)` — format: `0x{Value:X8}`
  - `Location(short X, short Y)` — format: `({X}, {Y})`
  - `Color(byte R, byte G, byte B)` — format: `#{R:X2}{G:X2}{B:X2}`
  - `GameObjectGuid(uint Type, uint Foo0, uint Foo2, uint Guid)` — format: `{Type:X8}-{Guid:X8}`
  - `PrefixedString` — wraps `string`, implements `ISpanFormattable`
  - Each type: `static TSelf Read(ref SpanReader r)` + `void Write(ref SpanWriter w)`
- [ ] Port/expand all shared enums: `GameObjectType`, `Material`, `Category`, `ResistType`
- [ ] Implement `SpanReaderExtensions` — domain-specific helpers on `ref SpanReader`:
  - `ReadLocation`, `ReadArtId`, `ReadGameObjectGuid`, `ReadPrefixedString`, `ReadTupleArray<T>`
- [ ] Implement `SpanWriterExtensions` — symmetric write helpers on `ref SpanWriter`:
  - `WriteLocation`, `WriteArtId`, `WriteGameObjectGuid`, `WritePrefixedString`, `WriteTupleArray<T>`
- [ ] Add `FrozenDictionary`-backed `EnumLookup<TEnum>` helper for fast name→value lookups
- [ ] Define `stackalloc` budget constant: `internal const int MaxStackAllocBytes = 256`
  - All callers use `size <= MaxStackAllocBytes ? stackalloc byte[size] : ArrayPool<byte>.Shared.Rent(size)`
- [ ] Create `src/Core/ArcNET.Core.Tests/ArcNET.Core.Tests.csproj` (TUnit)
- [ ] Write round-trip tests for every primitive: `Write(value) → Read → Assert.Equal(value)`
- [ ] Write `SpanReader` boundary tests: underflow throws, slice correctness

### Phase 4 — `ArcNET.GameObjects` library
- [ ] Create `src/GameObjects/ArcNET.GameObjects/ArcNET.GameObjects.csproj`
  - IsPackable=true, → ArcNET.Core project ref
  - InternalsVisibleTo ArcNET.GameObjects.Tests, ArcNET.Formats
- [ ] Port `GameObjectHeader` with `Bitmap` field-presence logic
  - `Bitmap` stays as `BitArray` but expose a `ReadOnlySpan<bool>`-friendly accessor
  - Use `in GameObjectHeader` on all methods that only read it
- [ ] Port all flag enums from `ArcNET.DataTypes/GameObjects/Flags/`
  - Annotate multi-flag enums with `[Flags]`
  - Add `FrozenSet<TFlag>` lookup helpers where fast membership checks are needed
- [ ] Port/rewrite all 22 type classes from `ArcNET.DataTypes/GameObjects/Types/`
  - **Delete** `[Order(n)]` attribute and all reflection machinery
  - Each type: `static TSelf Read(ref SpanReader reader)` + `void Write(ref SpanWriter writer)`
  - Use primary constructors where the type is essentially a data bag
  - Small value types (e.g., `SectorTile`, `SectorLight`) → `readonly record struct`
  - Larger mutable types (game objects the editor will modify) → `sealed class` with `IGameObject`
- [ ] Port specialized classes: `Monster`, `Unique`, `Entity`, `NpcInstance`, `PcInstance`
- [ ] Port supporting types:
  - `Inventory`, `InventorySource`, `InventorySourceBuy` — keep as `List<T>` fields on owning type
  - `Script` — `readonly record struct`
  - `Overlay` — `readonly record struct`
  - `SectorLight(ushort X, ushort Y, uint Intensity)` — `readonly record struct`
  - `SectorTile(uint Data)` — `readonly record struct` with property accessors for bit fields
- [ ] Port `Sector` — `sealed class`; holds `SectorTile[]` (4096), `List<SectorLight>`, `List<IGameObject>`
- [ ] Port quest/faction/background/XP level data classes — use primary constructors + `ISpanFormattable`
- [ ] Create `src/GameObjects/ArcNET.GameObjects.Tests/` (TUnit)
- [ ] Write round-trip tests per type using raw binary fixture bytes (no filesystem required)
- [ ] Write `GameObjectHeader` bitmap round-trip tests

### Phase 5 — `ArcNET.Formats` library
- [ ] Create `src/Formats/ArcNET.Formats/ArcNET.Formats.csproj`
  - IsPackable=true, → ArcNET.Core + ArcNET.GameObjects refs
- [ ] Define `FileFormat` enum (one value per supported format)
- [ ] Define `FormatDetector` — `FrozenDictionary<string, FileFormat>` keyed by extension (OrdinalIgnoreCase)
  - `static FileFormat Detect(ReadOnlySpan<char> extension)`
- [ ] Define the format API contract — every format is a `static class` with this exact shape:
  ```csharp
  public static class FacWalkFormat          // example
  {
      // Primary API — Span-based, zero allocation
      public static FacadeWalk Parse(scoped ref SpanReader reader);
      public static void Write(in FacadeWalk value, ref SpanWriter writer);

      // Convenience overloads — thin wrappers, single allocation each
      public static FacadeWalk ParseFile(string path);
      public static FacadeWalk ParseMemory(ReadOnlyMemory<byte> memory);
      public static byte[] WriteToArray(in FacadeWalk value);
      public static void WriteToFile(in FacadeWalk value, string path);
  }
  ```
- [ ] Implement each format following the above shape:
  - `FacWalkFormat` (`FacadeWalk` model) — port existing reader; add writer
  - `MessageFormat` (`MessageFile` model, MES) — port existing reader; add writer
  - `SectorFormat` (`Sector` model, SEC) — port existing reader; add writer
  - `ProtoFormat` (`ProtoFile` model, PRO) — port stub; complete read + write
  - `MobFormat` (`MobFile` model, MOB) — implement read + write
  - `ArtFormat` (`ArtIndex` model, ART) — implement read + write
  - `JmpFormat` (`JmpFile` model, JMP) — implement read + write
  - `ScriptFormat` (`ScriptFile` model, SCR) — implement read + write
  - `DialogFormat` (`DialogFile` model, DLG) — implement read + write
  - `TerrainFormat` (`TerrainFile` model, TDF) — implement read + write
  - `MapPropertiesFormat` (`MapProperties` model, PRP) — implement read + write
- [ ] Port `TextDataReader` → rename to `TextDataFormat` following same static-class shape
  - Text formats use `ReadOnlySpan<char>` as parse input (not `ReadOnlySpan<byte>`)
- [ ] Use `stackalloc` + `ArrayPool` per the budget rule from Phase 3 for all internal scratch buffers
- [ ] Create `src/Formats/ArcNET.Formats.Tests/` (TUnit)
- [ ] Round-trip tests for FacWalk and MES using embedded binary test fixtures (no disk access in tests)
- [ ] Property-based round-trip test helper: `ParseFile → WriteToArray → parse again → deep equal`
- [ ] Stub tests (parse throws `NotImplementedException`) for formats not yet fully reversed

### Phase 6 — `ArcNET.GameData` library
- [ ] Create `src/GameData/ArcNET.GameData/ArcNET.GameData.csproj`
  - IsPackable=true, → ArcNET.Core + ArcNET.GameObjects + ArcNET.Formats refs
- [ ] Implement `GameDataStore` (replaces static `GameObjectManager`)
  - Mutable `List<T>` collections: `Objects`, `Sectors`, `Monsters`, `Npcs`, `Uniques`, `Messages`
  - Fast lookups via lazily-rebuilt `FrozenDictionary<GameObjectGuid, IGameObject>`; invalidated on mutation
  - `IGameObject? FindById(GameObjectGuid id)` — O(1) via frozen dict
  - `IReadOnlyList<T> GetByType<T>()` — filtered view, cached
  - Dirty tracking: `HashSet<GameObjectGuid> _dirty`; `MarkDirty(id)`, `DirtyObjects`, `ClearDirty()`
  - `event EventHandler<GameObjectChangedEventArgs>? ObjectChanged` — for editor bindings
  - No statics; constructor injection friendly
- [ ] Implement `GameDataLoader` (replaces monolithic `Parser.cs`)
  - **Dual source**: both overloads resolve to `IReadOnlyDictionary<string, ReadOnlyMemory<byte>>` internally
    ```
    Task<GameDataStore> LoadFromDirectoryAsync(string path, IProgress<float>?, CancellationToken)
    Task<GameDataStore> LoadFromMemoryAsync(IReadOnlyDictionary<string, ReadOnlyMemory<byte>>, IProgress<float>?, CancellationToken)
    ```
  - Internal: `FormatDetector.Detect(ext)` → dispatch to matching `*Format.Parse(ref SpanReader)`
  - Reads files in parallel where safe (`Task.WhenAll` per format group)
- [ ] Implement `GameDataSaver` (new — no equivalent existed)
  - **Dual target**:
    ```
    Task SaveToDirectoryAsync(GameDataStore store, string path, IProgress<float>?, CancellationToken)
    IReadOnlyDictionary<string, byte[]> SaveToMemory(GameDataStore store)
    ```
  - `SaveToMemory` uses `ArrayBufferWriter<byte>` per file; calls `*Format.Write(in value, ref writer)`
  - Optionally save only dirty objects: `SaveDirtyToMemory(GameDataStore store)` → only files that contain dirty GUIDs
- [ ] Add JSON export via `System.Text.Json` source generation:
  - `[JsonSerializable]` context covering `GameDataStore` graph
  - `GameDataExporter.ExportToJsonAsync(GameDataStore store, Stream output, CancellationToken)`
  - `GameDataExporter.ExportToJsonString(GameDataStore store) → string`
- [ ] Create `src/GameData/ArcNET.GameData.Tests/` (TUnit)
- [ ] Tests for `GameDataStore`: add, find by id, get by type, dirty tracking, event fires
- [ ] Tests for `GameDataLoader.LoadFromMemoryAsync` using in-memory binary fixtures (no disk)
- [ ] Tests for `GameDataSaver.SaveToMemory` → re-parse → deep equal original

### Phase 7 — `ArcNET.Archive` library
- [ ] Create `src/Archive/ArcNET.Archive/ArcNET.Archive.csproj`
  - IsPackable=true, → ArcNET.Core ref only
- [ ] Implement `DatArchive : IDisposable`
  - Backed by `MemoryMappedFile` — large archives never fully loaded into RAM
  - Entry table parsed once at open; stored as `FrozenDictionary<string, DatEntry>` (OrdinalIgnoreCase)
  - `IReadOnlyCollection<string> EntryNames`
  - `ReadOnlyMemory<byte> GetEntryData(string name)` — zero-copy slice of the mapped view
  - `Stream OpenEntry(string name)` — streaming access for callers that process entry sequentially
  - `static DatArchive Open(string path)`
- [ ] Implement `DatExtractor`
  - `Task ExtractAllAsync(DatArchive archive, string outputDir, IProgress<float>?, CancellationToken)`
  - `Task ExtractEntryAsync(DatArchive archive, string entryName, string outputPath, CancellationToken)`
  - Uses `FileStream` with `FileOptions.Asynchronous | FileOptions.WriteThrough`
- [ ] Implement `DatPacker`
  - `Task PackAsync(string inputDir, string outputPath, IProgress<float>?, CancellationToken)`
  - Writes entries sequentially; builds entry table at end; uses `ArrayBufferWriter<byte>` for header
- [ ] Create `src/Archive/ArcNET.Archive.Tests/` (TUnit)
- [ ] Tests: open known small DAT fixture, enumerate entries, extract entry, re-pack, verify byte equality

### Phase 8 — `ArcNET.Patch` library
- [ ] Create `src/Patch/ArcNET.Patch/ArcNET.Patch.csproj`
  - IsPackable=true, → ArcNET.Core ref only; no UI deps
- [ ] Port `HighResConfig`
  - Parse/write `config.ini` using `ReadOnlySpan<char>` line iteration (no `string.Split` allocations)
  - Expose as strongly typed record: `readonly record struct HighResSettings(int Width, int Height, ...)`
- [ ] Implement `PatchInstaller` / `PatchUninstaller`
  - Remove `LibGit2Sharp` entirely — replace download logic with `HttpClient.GetStreamAsync` + progress
  - Use `FileStream` with async APIs throughout; no synchronous file I/O
- [ ] Create `src/Patch/ArcNET.Patch.Tests/` (TUnit)
- [ ] Tests for `HighResConfig` parse/write round-trip using in-memory strings

### Phase 9 — `ArcNET.App` console executable
- [ ] Create `src/ArcNET.App/ArcNET.App.csproj`
  - OutputType=Exe (inherited from App/Directory.Build.props)
  - Refs: ArcNET.GameData, ArcNET.Archive, ArcNET.Patch
  - PackageRef: Spectre.Console (only this project may reference it)
- [ ] Port main menu logic from `ArcNET.Terminal/Program.cs`
  - Replace direct static `GameObjectManager` with injected `GameDataStore`
  - Replace direct `Parser.ParseExtractedData()` with `GameDataLoader.LoadAsync()`
- [ ] Port terminal UI helpers from `Terminal.cs`
- [ ] Verify end-to-end: menu → load → display → export flow

### Phase 10 — Benchmarks
- [ ] Create `benchmarks/ArcNET.Benchmarks/ArcNET.Benchmarks.csproj`
  - BenchmarkDotNet refs
  - Benchmark `FacWalkReader`, `MessageReader`, `SectorReader` read throughput
- [ ] Add `bench` command to `Build.cs`

### Phase 11 — CI pipeline
- [ ] Create `.github/workflows/dotnet.yml`
  - Jobs: `build`, `test`, `format`, `pack` (NuGet pack but not push)
  - Use the lib guide's Phase 9 workflow template
  - Matrix: ubuntu-latest, windows-latest
  - `format` job: `dotnet csharpier check .` then `dotnet format style --verify-no-changes` then `dotnet format analyzers --verify-no-changes` (never bare `dotnet format`)
  - `pack` job: `dotnet pack -c Release` for all six NuGet libraries
  - On tag push (`arcnet.*-v*`): publish to NuGet.org via `NUGET_API_KEY` secret
- [ ] Create `.github/dependabot.yml` (nuget + github-actions ecosystems)

### Phase 12 — Formatting and validation
- [ ] Run `dotnet tool restore`
- [ ] Run `dotnet csharpier format .`
- [ ] Run `dotnet format style`
- [ ] Run `dotnet format analyzers`
- [ ] Run `dotnet build -c Release` — must be zero warnings
- [ ] Run `dotnet test` — all tests pass
- [ ] Verify `ArcNET.slnx` loads cleanly in VS 2022

### Phase 13 — Repository health and README finalization
- [ ] Populate `README.md` badges (build, codecov, NuGet links for each package)
- [ ] Add architecture diagram to README
- [ ] Add per-package quick-start examples
- [ ] Update `CHANGELOG.md` with `## [Unreleased]` section
- [ ] Write initial git tag `arcnet.core-v0.1.0` etc. (or a single `v0.1.0` if unified versioning preferred)

---

## Package naming convention

NuGet packages are published under a unified prefix:

| Project | Package ID | Tag prefix |
|---|---|---|
| `ArcNET.Core` | `ArcNET.Core` | `arcnet.core-v` |
| `ArcNET.GameObjects` | `ArcNET.GameObjects` | `arcnet.gameobjects-v` |
| `ArcNET.Formats` | `ArcNET.Formats` | `arcnet.formats-v` |
| `ArcNET.GameData` | `ArcNET.GameData` | `arcnet.gamedata-v` |
| `ArcNET.Archive` | `ArcNET.Archive` | `arcnet.archive-v` |
| `ArcNET.Patch` | `ArcNET.Patch` | `arcnet.patch-v` |

All libraries version-bump together (single MinVer tag like `v0.1.0` applies to all) until there's a reason to version them independently.

---

## What NOT to port (dead code / replace)

| Old code | Decision |
|---|---|
| `[Order(n)]` attribute | Delete — replaced by explicit per-type Read/Write |
| `GameObjectReader.cs` reflection engine | Delete — replaced by explicit methods on each type |
| Static `GameObjectManager` | Delete — replaced by injectable `GameDataStore` |
| `Newtonsoft.Json` references | Remove — use `System.Text.Json` |
| `LibGit2Sharp` | Remove — too heavy; plain git CLI / HTTP |
| `GitHub.cs` in Utilities | Port concept to `ArcNET.Patch` using `HttpClient` |
| `Bia10.Utils` NuGet dep | Evaluate — replace with BCL equivalents or inline; don't depend on a personal lib in published packages |

---

## Results summary

> To be filled in by agent after all phases complete.
