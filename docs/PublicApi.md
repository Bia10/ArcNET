# Public API Reference

All packages target `net10.0` and are **NativeAOT and trim compatible**.
Versioning is via [MinVer](https://github.com/adamralph/minver) from git tags.

## NativeAOT compatibility

Every library is built with `IsAotCompatible=true`, which enables the IL2xxx/IL3xxx analyzers at build time.
These guarantees hold across all packages:

| Pattern | Status | Note |
|---|---|---|
| `SpanReader` / `SpanWriter` | ✅ Safe | Stack-allocated `ref struct`, zero heap, no reflection |
| Primitives (`ArtId`, `Location`, …) | ✅ Safe | `readonly record struct`, static abstract interface members |
| All format parsers (`MessageFormat`, `SectorFormat`, …) | ✅ Safe | Static methods, span-based, no reflection |
| `FrozenDictionary` lookups | ✅ Safe | BCL type, AOT-supported |
| `Enum.GetValues<T>()` | ✅ Safe | Concrete generic, resolved at compile time |
| `GameDataExporter` JSON | ✅ Safe | Uses `[JsonSerializable]` source generation — **not** `JsonSerializer.Serialize<T>(obj)` with reflection |
| `MemoryMappedFile` (`DatArchive`) | ✅ Safe | BCL type, no reflection |
| LINQ (`.Select().ToList()`) | ✅ Safe | BCL, no reflection in .NET 10 |

**What this means for consumers:** you can publish any ArcNET-based application with `PublishAot=true` without needing `rd.xml` files, `[DynamicDependency]` attributes, or suppress-trim annotations.

---

## ArcNET.Core

### `SpanReader` (ref struct)

Stack-allocated forward reader over a `ReadOnlySpan<byte>`.

| Member | Description |
|---|---|
| `int Position` | Current read offset |
| `int Remaining` | Bytes left to read |
| `ReadByte()` | Read one `byte` |
| `ReadInt16()` / `ReadUInt16()` | Read 2-byte little-endian integer |
| `ReadInt32()` / `ReadUInt32()` | Read 4-byte little-endian integer |
| `ReadInt64()` / `ReadUInt64()` | Read 8-byte little-endian integer |
| `ReadSingle()` | Read IEEE 754 `float` |
| `ReadDouble()` | Read IEEE 754 `double` |
| `ReadBytes(int count)` | Return a span slice; advance position |
| `Skip(int count)` | Advance position without reading |
| `TryPeek(out byte)` | Non-advancing peek at the current byte |
| `PeekInt32At(int offset)` | Non-advancing read of `int32` at `Position + offset` |
| `Slice(int length)` | Return a sub-reader over the next `length` bytes; advances position |

### `SpanWriter` (ref struct)

Stack-allocated forward writer over an `IBufferWriter<byte>`.

| Member | Description |
|---|---|
| `WriteByte(byte)` | Write one byte |
| `WriteInt16(short)` / `WriteUInt16(ushort)` | Write 2-byte little-endian |
| `WriteInt32(int)` / `WriteUInt32(uint)` | Write 4-byte little-endian |
| `WriteInt64(long)` / `WriteUInt64(ulong)` | Write 8-byte little-endian |
| `WriteSingle(float)` | Write IEEE 754 float |
| `WriteDouble(double)` | Write IEEE 754 double |
| `WriteBytes(ReadOnlySpan<byte>)` | Copy bytes |

### `SpanReaderExtensions` (static)

Domain-specific reads that delegate to `SpanReader`.

| Method | Returns |
|---|---|
| `ReadLocation(ref SpanReader)` | `Location` |
| `ReadArtId(ref SpanReader)` | `ArtId` |
| `ReadGameObjectGuid(ref SpanReader)` | `GameObjectGuid` |
| `ReadPrefixedString(ref SpanReader)` | `PrefixedString` |
| `ReadArray<T>(ref SpanReader, ReadElement<T>, int)` | `T[]` |

### `SpanWriterExtensions` (static)

| Method | Description |
|---|---|
| `WriteLocation(ref SpanWriter, Location)` | |
| `WriteArtId(ref SpanWriter, ArtId)` | |
| `WriteGameObjectGuid(ref SpanWriter, GameObjectGuid)` | |
| `WritePrefixedString(ref SpanWriter, PrefixedString)` | |
| `WriteArray<T>(ref SpanWriter, T[], WriteElement<T>)` | |

### `IBinarySerializable<TSelf, TReader>` (interface)

```csharp
public interface IBinarySerializable<TSelf, TReader>
{
    static abstract TSelf Read(ref TReader reader);
    void Write(ref SpanWriter writer);
}
```

Implemented by all primitive types and format data types.

### `EnumLookup<TEnum>` (static)

Frozen, zero-reflection enum name ↔ value maps, built once at startup.

| Member | Description |
|---|---|
| `FrozenDictionary<string, TEnum> ByName` | Name → value |
| `FrozenDictionary<TEnum, string> ToName` | Value → name |
| `bool TryGetByName(string, out TEnum)` | |
| `string GetName(TEnum)` | |

### Primitives

All primitives are `readonly record struct` implementing `ISpanFormattable`, `IUtf8SpanFormattable`, and `IBinarySerializable`.

| Type | Fields | Notes |
|---|---|---|
| `ArtId` | `uint Value` | |
| `Color` | `byte R, G, B` | `ReadRgba` strips alpha channel |
| `GameObjectGuid` | `short OidType, short Padding2, int Padding4, Guid Id` (24 bytes) | |
| `Location` | `short X, Y` | |
| `PrefixedString` | `string Value` | ushort-length-prefixed ASCII on wire |

---

## ArcNET.GameObjects

### `ObjectType` (enum)

`Wall=0 | Portal | Container | Scenery | Projectile | Weapon | Ammo | Armor | Gold | Food | Scroll | Key | KeyRing | Written | Generic | Pc | Npc | Trap=17`

### `ObjectField` (enum)

35+ named bit-field indices: `ObjFLocation`, `ObjFName`, `ObjFFlags`, `ObjFHitPoints`, `ObjFUnknown`, `ObjFConditionModifier`, `ObjFAiPacket`, `ObjFLevel`, `ObjFInventory`, `ObjFTimer`, `ObjFObjectScript`, `ObjFArtNum`, `ObjFAmbientLight`, `ObjFLightRadius`, `ObjFLightColor`, `ObjFScriptId`, `ObjFRunOnce`, `ObjFBroken`, `ObjFQuantityField`, `ObjFHidden`, `ObjFPlayerInitiated`, etc.

### `GameObjectHeader` (sealed)

| Member | Type | Description |
|---|---|---|
| `Version` | `int` | File format version |
| `ProtoId` | `GameObjectGuid` | Prototype reference |
| `ObjectId` | `GameObjectGuid` | Unique instance GUID |
| `GameObjectType` | `ObjectType` | Dispatch type |
| `PropCollectionItems` | `int` | Field count in the bitmap |
| `Bitmap` | `ulong` | Bit-presence field map |
| `IsPrototype` | `bool` | True when parsed from a `.pro` file |

### `GameObject` (sealed) : `IGameObject`

| Member | Description |
|---|---|
| `static Read(ref SpanReader)` | Dispatches by `ObjectType` to the concrete sub-type |
| `Header` | `GameObjectHeader` |
| `Common` | `ObjectCommon` |
| `ObjectId` / `ProtoId` | Forwarded from header |
| `IsPrototype` | Forwarded from header |

### Concrete object types (all in `ArcNET.GameObjects.Types`)

`ObjectWall`, `ObjectPortal`, `ObjectContainer`, `ObjectScenery`, `ObjectProjectile`, `ObjectTrap`, `ObjectWeapon`, `ObjectAmmo`, `ObjectArmor`, `ObjectGold`, `ObjectFood`, `ObjectScroll`, `ObjectKey`, `ObjectKeyRing`, `ObjectWritten`, `ObjectGeneric`, `ObjectCritter`, `ObjectPc`, `ObjectNpc`, `ObjectUnknown`

Each exposes typed data properties and an ordered `Read` / `Write` pair.

### `GameObjectStore` (sealed)

| Member | Description |
|---|---|
| `IReadOnlyList<GameObjectHeader> Headers` | All stored headers |
| `Add(GameObjectHeader)` | Append a header |
| `Clear()` | Remove all headers |

---

## ArcNET.Formats

### Format interfaces

```csharp
public interface IFormatReader<T>
{
    static abstract T Parse(scoped ref SpanReader reader);
    static abstract T ParseMemory(ReadOnlyMemory<byte> memory);
    static abstract T ParseFile(string path);
}

public interface IFormatWriter<T>
{
    static abstract void Write(in T value, ref SpanWriter writer);
    static abstract byte[] WriteToArray(in T value);
    static abstract void WriteToFile(in T value, string path);
}
```

### Format classes and their data types

| Format class | Data type | File ext / pattern |
|---|---|---|
| `MessageFormat` | `MesFile` (`Entries: IReadOnlyList<MessageEntry>`) | `.mes` |
| `SectorFormat` | `Sector` | `.sec` |
| `ArtFormat` | `ArtFile` | `.ART` |
| `DialogFormat` | `DlgFile` (`Entries: IReadOnlyList<DialogEntry>`) | `.dlg` |
| `ScriptFormat` | `ScrFile` | `.scr` |
| `ProtoFormat` | `ProtoData` (`Header`, `Properties`) | `.pro` |
| `MobFormat` | `MobData` (`Header`, `Properties`) | `.mob` |
| `FacWalkFormat` | `FacadeWalk` (`Header`, `Entries`) | `facwalk.*` |
| `JmpFormat` | `JmpFile` (`Jumps: IReadOnlyList<JumpEntry>`) | `.jmp` |
| `TextDataFormat` | `TextDataFile` (`Entries: IReadOnlyList<TextDataEntry>`) | (no fixed ext; not in `FileFormat` lookup) |
| `SaveIndexFormat` | `SaveIndex` (`Root: IReadOnlyList<TfaiEntry>`) | `.tfai` |
| `SaveInfoFormat` | `SaveInfo` | `.gsi` |
| `TerrainFormat` | `TerrainData` | `.tdf` |
| `MapPropertiesFormat` | `MapProperties` | `.prp` |

### `TfafFormat` (static, not IFormatReader/Writer)

A `SaveIndex` (from `SaveIndexFormat.ParseFile`) is required to map virtual paths to byte offsets in the TFAF blob.

| Method | Description |
|---|---|
| `ExtractAll(SaveIndex, ReadOnlyMemory<byte>)` → `IReadOnlyDictionary<string, byte[]>` | Extract all entries to a virtual-path → bytes map |
| `Extract(SaveIndex, ReadOnlyMemory<byte>, string virtualPath)` → `byte[]` | Extract a single named entry |
| `TotalPayloadSize(SaveIndex)` → `int` | Sum of all entry payload sizes |
| `Pack(SaveIndex, IReadOnlyDictionary<string, byte[]>)` → `byte[]` | Rebuild a TFAF blob from a virtual-path → bytes map |

### `FileFormat` (enum) + `FileFormatExtensions` (static)

16 values: `Unknown | Sector | Proto | Message | Mob | Art | Jmp | Script | Dialog | Terrain | MapProperties | FacadeWalk | DataArchive | SaveInfo | SaveIndex | SaveData`

| Method | Description |
|---|---|
| `FileFormatExtensions.FromExtension(string)` | Extension string → `FileFormat` (FrozenDictionary-backed, O(1)) |
| `FileFormatExtensions.FromPath(string)` | Full path → `FileFormat` |

---

## ArcNET.Archive

### `ArchiveEntry` (sealed)

| Property | Type | Description |
|---|---|---|
| `Path` | `string` | Virtual path inside the archive |
| `Flags` | `DatEntryFlags` | Entry flags: `Plain=0x001`, `Compressed=0x002`, `Directory=0x400` |
| `UncompressedSize` | `int` | Size after decompression |
| `CompressedSize` | `int` | Stored size (equals `UncompressedSize` when not compressed) |
| `Offset` | `int` | Byte offset in the DAT file |
| `IsCompressed` | `bool` | |
| `IsDirectory` | `bool` | True for directory pseudo-entries; skip when enumerating files |

### `DatArchive` (sealed, `IDisposable`)

Backed by `MemoryMappedFile` — no full load into RAM.

| Member | Description |
|---|---|
| `static Open(string path)` → `DatArchive` | Open and index a DAT file |
| `IReadOnlyCollection<ArchiveEntry> Entries` | All entries (backed by FrozenDictionary) |
| `FindEntry(string virtualPath)` → `ArchiveEntry?` | O(1) case-insensitive lookup |
| `GetEntryData(string name)` → `ReadOnlyMemory<byte>` | Copy-read a single entry |
| `OpenEntry(string name)` → `Stream` | Streaming MMF view |

### `DatExtractor` (static)

| Method | Description |
|---|---|
| `ExtractAllAsync(DatArchive, string outputDir, IProgress<float>?, CancellationToken)` → `Task` | |
| `ExtractEntryAsync(DatArchive, string entryName, string outputDir, CancellationToken)` → `Task` | |

### `DatPacker` (static)

| Method | Description |
|---|---|
| `PackAsync(string inputDir, string outputPath, IProgress<float>?, CancellationToken)` → `Task` | |

---

## ArcNET.GameData

> **Status:** `GameDataLoader` wires `FileFormat.Message`, `FileFormat.Sector`, `FileFormat.Proto`, and `FileFormat.Mob`.
> Other formats (Dialog, Script, Art, …) are discovered but not yet dispatched into the store.

### `GameDataLoader` (static)

| Method | Description |
|---|---|
| `DiscoverFiles(string dirPath)` → `IReadOnlyDictionary<FileFormat, IReadOnlyList<string>>` | Recursively scan a directory, grouped by format |
| `LoadMessages(string dirPath)` → `IReadOnlyDictionary<int, string>` | Merge all `.mes` files → index-to-text map |
| `LoadFromDirectoryAsync(string, IProgress<float>?, CancellationToken)` → `Task<GameDataStore>` | Load all discoverable game data from disk |
| `LoadFromMemoryAsync(IReadOnlyDictionary<string,ReadOnlyMemory<byte>>, IProgress<float>?, CancellationToken)` → `Task<GameDataStore>` | Load from pre-loaded byte buffers (no filesystem) |

### `GameDataStore` (sealed)

| Member | Description |
|---|---|
| `IReadOnlyList<GameObjectHeader> Objects` | All loaded object headers |
| `IReadOnlyList<MessageEntry> Messages` | All loaded message entries (index + optional sound ID + text) |
| `IReadOnlyList<Sector> Sectors` | All loaded sector data |
| `IReadOnlyList<ProtoData> Protos` | All loaded prototype data |
| `IReadOnlyList<MobData> Mobs` | All loaded mobile object data |
| `IReadOnlyDictionary<string, IReadOnlyList<MessageEntry>> MessagesBySource` | Messages grouped by source file path (key = original file path) |
| `IReadOnlyDictionary<string, IReadOnlyList<Sector>> SectorsBySource` | Sectors grouped by source file path |
| `IReadOnlyDictionary<string, IReadOnlyList<ProtoData>> ProtosBySource` | Protos grouped by source file path |
| `IReadOnlyDictionary<string, IReadOnlyList<MobData>> MobsBySource` | Mobs grouped by source file path |
| `IReadOnlySet<GameObjectGuid> DirtyObjects` | GUIDs marked dirty since last `ClearDirty` |
| `event ObjectChanged` (`EventHandler<GameObjectGuid>`) | Raised on `AddObject` or `MarkDirty` |
| `AddObject(GameObjectHeader)` | Append; invalidates GUID index |
| `AddMessage(MessageEntry)` | Append a fully-parsed message entry |
| `AddSector(Sector)` | Append a parsed sector |
| `AddProto(ProtoData)` | Append a parsed prototype |
| `AddMob(MobData)` | Append a parsed mobile object |
| `FindByGuid(in GameObjectGuid)` → `GameObjectHeader?` | O(1) lookup via lazy FrozenDictionary |
| `MarkDirty(in GameObjectGuid)` | Add to dirty set; raise event |
| `ClearDirty()` | Reset dirty set |
| `Clear()` | Remove all data (objects, messages, sectors, protos, mobs, dirty set) |

### `GameDataSaver` (sealed class)

| Method | Description |
|---|---|
| `SaveMessagesToFile(GameDataStore, string outputPath)` | Write all messages to a single `.mes` file, preserving original indices and sound IDs |
| `SaveMessagesToMemory(GameDataStore)` → `byte[]` | Serialize all messages to bytes |
| `SaveSectorsToDirectory(GameDataStore, string outputDir)` | Write each sector as `sector_NNNNNN.sec` under `outputDir` |
| `SaveProtosToDirectory(GameDataStore, string outputDir)` | Write each prototype as `proto_NNNNNN.pro` under `outputDir` |
| `SaveMobsToDirectory(GameDataStore, string outputDir)` | Write each mob as `mob_NNNNNN.mob` under `outputDir` |
| `SaveToDirectoryAsync(GameDataStore, string outputDir, IProgress<float>?, CancellationToken)` → `Task` | Write all data types to `outputDir` with progress reporting |
| `SaveToMemory(GameDataStore)` → `IReadOnlyDictionary<string, byte[]>` | Serialize all data types to a virtual filename map |

### `GameDataExporter` (static)

All JSON serialization uses `[JsonSerializable]` source generation — no reflection, fully AOT-safe.

| Method | Description |
|---|---|
| `ExportToJson(GameDataStore)` → `string` | Serialize the full store to a JSON string |
| `ExportToJsonFileAsync(GameDataStore, string, CancellationToken)` → `Task` | Write JSON to a file |

**DTOs exported:**

| DTO | Fields |
|---|---|
| `GameDataExportDto` | `Objects`, `Messages`, `Sectors`, `Protos`, `Mobs` |
| `GameObjectHeaderDto` | `Version`, `Type`, `ObjectId`, `ProtoId`, `IsPrototype` |
| `MessageEntryDto` | `Index`, `SoundId?`, `Text` |
| `SectorDto` | `LightCount`, `TileCount`, `HasRoofs`, `TileScriptCount`, `ObjectCount` |
| `ProtoDto` | `Version`, `Type`, `ObjectId`, `ProtoId`, `PropertyCount` |
| `MobDto` | `Version`, `Type`, `ObjectId`, `ProtoId`, `PropertyCount` |

---

## ArcNET.Patch

### `HighResConfig` (sealed)

Parsed from `hires.ini`. Key properties:

`Width`, `Height`, `BitDepth`, `Windowed`, `Renderer`, `DoubleBuffer`, `DDrawWrapper`, `DxWrapper`, `ShowFPS`, `ScrollFPS`, `ScrollDist`, `PreloadLimit`, `BroadcastLimit`, `Logos`, `Intro`, `DialogFont`, `LogbookFont`, `MenuPosition`, `MainMenuArt`, `Borders`, `Language`

| Method | Description |
|---|---|
| `static ParseFile(string path)` → `HighResConfig` | Parse an INI configuration file |

### `GitHubReleaseClient` (static)

| Method | Description |
|---|---|
| `GetLatestHighResPatchReleaseAsync(CancellationToken)` → `Task<GitHubRelease?>` | Query the GitHub Releases API |
| `DownloadFileAsync(string url, string destinationPath, CancellationToken)` → `Task` | Download a release asset |

### `PatchInstaller` (static)

| Method | Description |
|---|---|
| `InstallAsync(string gameDir, IProgress<float>?, CancellationToken)` → `Task` | Download and apply the latest HighRes patch |

### `PatchUninstaller` (static)

| Method | Description |
|---|---|
| `UninstallAsync(string gameDir, CancellationToken)` → `Task` | Remove patch files |
| `IsPatchInstalled(string gameDir)` → `bool` | Check whether patch files are present |

---

## ArcNET.Dumpers

Human-readable text dumpers for all parsed game data formats. All dumpers are `static` classes exposing `Dump(T)` → `string`.

| Dumper | Input type |
|---|---|
| `ArtDumper` | `ArtFile` |
| `DialogDumper` | `DlgFile` |
| `FacWalkDumper` | `FacadeWalk` |
| `ItemDumper` | (game-dir resolver helpers) |
| `JmpDumper` | `JmpFile` |
| `MapPropertiesDumper` | `MapProperties` |
| `MessageDumper` | `MesFile` |
| `MobDumper` | `MobData` |
| `ProtoDumper` | `ProtoData` |
| `SaveIndexDumper` | `SaveIndex` |
| `SaveInfoDumper` | `SaveInfo` |
| `ScriptDumper` | `ScrFile` |
| `SectorDumper` | `Sector` |
| `TerrainDumper` | `TerrainData` |

---

## ArcNET.BinaryPatch

JSON-driven binary patching for bug fixes and mod authoring — apply, revert, and verify field-level PRO/MOB mutations and raw byte patches with backup/restore support.

### `IBinaryPatch` (interface)

| Member | Description |
|---|---|
| `string Id` | Unique patch identifier |
| `string Description` | Human-readable description |
| `PatchTarget Target` | File to patch (`RelativePath`, `Format`, optional `SourceDatPath` + `DatEntryPath`) |
| `string PatchSummary` | Short diagnostic label |
| `bool NeedsApply(ReadOnlyMemory<byte>)` | True when the patch has not yet been applied |
| `byte[] Apply(ReadOnlyMemory<byte>)` | Return the patched bytes |

### `BinaryPatchSet` (sealed class)

| Member | Description |
|---|---|
| `string Name` | Set display name |
| `string Version` | Semantic version string |
| `IReadOnlyList<IBinaryPatch> Patches` | All patches in this set |

### `BinaryPatcher` (static)

| Method | Description |
|---|---|
| `Apply(BinaryPatchSet, string gameDir, PatchOptions?)` → `IReadOnlyList<PatchResult>` | Apply all patches; creates `.bak` backups by default |
| `Revert(BinaryPatchSet, string gameDir)` → `IReadOnlyList<PatchResult>` | Restore `.bak` backups |
| `Verify(BinaryPatchSet, string gameDir)` → `IReadOnlyList<PatchVerifyResult>` | Read-only check of which patches still need applying |

### `PatchDiscovery` (static)

| Member | Description |
|---|---|
| `string DefaultPatchesDir` | Directory next to the executable scanned by `LoadAll` |
| `LoadAll(string?, Action<string, Exception>?)` → `IReadOnlyList<BinaryPatchSet>` | Scan a directory for JSON patch descriptors |

### `JsonPatchLoader` (static)

| Method | Description |
|---|---|
| `LoadFile(string path)` → `BinaryPatchSet` | Load a patch set from a JSON file |
| `LoadEmbedded(Assembly, string resourceName)` → `BinaryPatchSet` | Load from an embedded assembly resource |

### `PatchStateStore` (static)

| Method | Description |
|---|---|
| `RecordApply(string gameDir, BinaryPatchSet)` → `PatchState` | Persist an applied-patch record (`.arcnet-patches.json`) |
| `RecordRevert(string gameDir, BinaryPatchSet)` | Remove a patch record |
| `IsRecorded(string gameDir, BinaryPatchSet)` → `bool` | Check whether a patch set was recorded as applied |

### Concrete patch types

| Type | Description |
|---|---|
| `ProtoFieldPatch` | Field-level mutation on a `.pro` file (e.g. `SetInt32`, `SetFloat`) |
| `MobFieldPatch` | Field-level mutation on a `.mob` file |
| `RawBinaryPatch` | Raw byte patch at a known offset (`AtOffset`) |

