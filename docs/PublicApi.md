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
| `Slice(int start, int length)` | Return a sub-reader without advancing |

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
| `GameObjectGuid` | `uint Type, Foo0, Foo2, Guid` | `IsProto` bool property |
| `Location` | `short X, Y` | |
| `PrefixedString` | `string Value` | ushort-length-prefixed ASCII on wire |

---

## ArcNET.GameObjects

### `ObjectType` (enum)

`Wall | Portal | Container | Scenery | Projectile | Trap | Weapon | Ammo | Armor | Gold | Food | Scroll | Key | KeyRing | Written | Generic | Pc | Npc`

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
| `TextDataFormat` | `TextDataFile` (`Entries: IReadOnlyList<TextDataEntry>`) | `.tdf` |
| `SaveIndexFormat` | `SaveIndex` (`Root: IReadOnlyList<TfaiEntry>`) | `.svg` |
| `SaveInfoFormat` | `SaveInfo` | `.gsi` |
| `TerrainFormat` | `TerrainData` | `.ter` |
| `MapPropertiesFormat` | `MapProperties` | `.prp` |

### `TfafFormat` (static, not IFormatReader/Writer)

| Method | Description |
|---|---|
| `ExtractAll(ReadOnlySpan<byte>, string outputDir)` | Extract all entries to a directory |
| `Extract(ReadOnlySpan<byte>, string name)` → `byte[]?` | Extract a single named entry |
| `TotalPayloadSize(ReadOnlySpan<byte>)` → `long` | Sum of all entry payload sizes |
| `Pack(string inputDir, string outputPath)` | Pack directory into TFAF |

### `FileFormat` (enum) + `FileFormatExtensions` (static)

16 values: `Unknown | Message | Sector | Art | Dialog | Script | Proto | Mob | FacadeWalk | Jmp | TextData | SaveIndex | SaveInfo | Terrain | MapProperties | Tfaf`

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
| `UncompressedSize` | `int` | Size after decompression |
| `CompressedSize` | `int` | Stored size (equals `UncompressedSize` when not compressed) |
| `Offset` | `long` | Byte offset in the DAT file |
| `IsCompressed` | `bool` | |

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

### `GameDataSaver` (static)

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

