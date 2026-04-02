# ArcNET — Example Catalogue

Comprehensive, copy-paste-ready examples for every ArcNET library.
All code targets `net10.0` / C# 14.

---

## Table of Contents

- [ArcNET.Formats](#arcnetformats)
  - [Parse a `.mes` message file](#parse-a-mes-message-file)
  - [Round-trip a message file](#round-trip-a-message-file)
  - [Parse a sector file (`.sec`)](#parse-a-sector-file-sec)
  - [Parse an ART sprite file](#parse-an-art-sprite-file)
  - [Parse a prototype file (`.pro`)](#parse-a-prototype-file-pro)
  - [Parse a mob file (`.mob`)](#parse-a-mob-file-mob)
  - [Parse a dialog file (`.dlg`)](#parse-a-dialog-file-dlg)
  - [Parse a script file (`.scr`)](#parse-a-script-file-scr)
  - [Parse a FacWalk walk mesh](#parse-a-facwalk-walk-mesh)
  - [Parse a JMP jump table](#parse-a-jmp-jump-table)
  - [Parse a TextData file (`.tdf`)](#parse-a-textdata-file-tdf)
  - [Parse a save index (TFAI / `.svg`)](#parse-a-save-index-tfai--svg)
  - [Parse a save-info file (`.gsi`)](#parse-a-save-info-file-gsi)
  - [Parse a terrain file](#parse-a-terrain-file)
  - [Parse map properties](#parse-map-properties)
  - [Discover files by format](#discover-files-by-format)
- [ArcNET.Archive](#arcnetarchive)
  - [Open and enumerate a DAT archive](#open-and-enumerate-a-dat-archive)
  - [Extract a single entry](#extract-a-single-entry)
  - [Extract all entries](#extract-all-entries)
  - [Read an entry without extracting](#read-an-entry-without-extracting)
  - [Pack a directory into a DAT archive](#pack-a-directory-into-a-dat-archive)
  - [Extract using TFAF sub-archive](#extract-using-tfaf-sub-archive)
- [ArcNET.GameObjects](#arcnetgameobjects)
  - [Read a game object from raw bytes](#read-a-game-object-from-raw-bytes)
  - [Read a game-object header only](#read-a-game-object-header-only)
  - [Use GameObjectStore](#use-gameobjectstore)
- [ArcNET.GameData](#arcnetgamedata)
  - [Load all messages from a directory](#load-all-messages-from-a-directory)
  - [Load from in-memory buffers (editor / test)](#load-from-in-memory-buffers-editor--test)
  - [Save messages back to disk](#save-messages-back-to-disk)
  - [Dirty tracking and the ObjectChanged event](#dirty-tracking-and-the-objectchanged-event)
  - [Export to JSON](#export-to-json)
- [ArcNET.Patch](#arcnetpatch)
  - [Install the HighRes patch](#install-the-highres-patch)
  - [Uninstall the HighRes patch](#uninstall-the-highres-patch)
  - [Read or modify HighRes config](#read-or-modify-highres-config)
- [ArcNET.Core](#arcnetcore)
  - [Low-level SpanReader / SpanWriter](#low-level-spanreader--spanwriter)
  - [Primitive types round-trip](#primitive-types-round-trip)
  - [EnumLookup — frozen name maps](#enumlookup--frozen-name-maps)

---

## ArcNET.Formats

### Parse a `.mes` message file

```csharp
using ArcNET.Formats;

// From disk — one allocation (File.ReadAllBytes internally)
MesFile mesFile = MessageFormat.ParseFile("arcanum/mes/game.mes");

foreach (MessageEntry entry in mesFile.Entries)
{
    Console.WriteLine($"[{entry.Index}] {entry.Text}");

    // Optional sound-effect token (present only in 3-field entries)
    if (entry.SoundId is string snd)
        Console.WriteLine($"  sound: {snd}");
}

// From a buffer you already own — zero extra allocations
ReadOnlyMemory<byte> buf = await File.ReadAllBytesAsync("game.mes");
mesFile = MessageFormat.ParseMemory(buf);
```

### Round-trip a message file

```csharp
using ArcNET.Formats;

MesFile original = MessageFormat.ParseFile("game.mes");

// Serialize back to bytes (same format as on disk)
byte[] bytes = MessageFormat.WriteToArray(in original);

// Write directly to a file
MessageFormat.WriteToFile(in original, "game_out.mes");

// Parse again — must produce identical entries
MesFile copy = MessageFormat.ParseMemory(bytes);
Debug.Assert(copy.Entries.Count == original.Entries.Count);
```

### Parse a sector file (`.sec`)

```csharp
using ArcNET.Formats;

Sector sector = SectorFormat.ParseFile("maps/map_001_001.sec");

Console.WriteLine($"Tiles: {sector.Tiles.Count}");
Console.WriteLine($"Lights: {sector.Lights.Count}");
Console.WriteLine($"Objects: {sector.Objects.Count}");

if (sector.HasRoofs)
    Console.WriteLine($"Roofs: {sector.Roofs.Count}");

// Access block-mask bit for tile (x, y)
bool blocked = sector.BlockMask[sector.GetSectorLoc(3, 7)];
```

### Parse an ART sprite file

```csharp
using ArcNET.Formats;

ArtFile art = ArtFormat.ParseFile("art/critters/barbarian.ART");

Console.WriteLine($"Rotations: {art.EffectiveRotationCount}");
Console.WriteLine($"Frames per rotation: {art.FrameCount}");
Console.WriteLine($"Frame rate: {art.FrameRate}");

// Access pixel data for rotation 0, frame 0
ArtFrame frame = art.Frames[0][0];
Console.WriteLine($"Frame size: {frame.Header.Width}×{frame.Header.Height}");
byte[] pixels = frame.Pixels; // RLE-decoded, palette-indexed
```

### Parse a prototype file (`.pro`)

```csharp
using ArcNET.Formats;

ProtoData proto = ProtoFormat.ParseFile("proto/items/weapon.pro");

Console.WriteLine($"Is prototype: {proto.Header.IsPrototype}");  // always true for .pro
Console.WriteLine($"Object type: {proto.Header.GameObjectType}");
Console.WriteLine($"Property count: {proto.Properties.Count}");
```

### Parse a mob file (`.mob`)

```csharp
using ArcNET.Formats;

MobData mob = MobFormat.ParseFile("maps/instances/npc_001.mob");

Console.WriteLine($"Object type: {mob.Header.GameObjectType}");
Console.WriteLine($"Object GUID: {mob.Header.ObjectId}");
Console.WriteLine($"Proto GUID: {mob.Header.ProtoId}");
Console.WriteLine($"Properties: {mob.Properties.Count}");
```

### Parse a dialog file (`.dlg`)

```csharp
using ArcNET.Formats;

DlgFile dlg = DialogFormat.ParseFile("dlg/townguard_001.dlg");

foreach (DialogEntry entry in dlg.Entries)
{
    Console.WriteLine($"[{entry.Num}] {entry.Text}");
    if (!string.IsNullOrEmpty(entry.Conditions))
        Console.WriteLine($"  condition: {entry.Conditions}");
}
```

### Parse a script file (`.scr`)

```csharp
using ArcNET.Formats;

ScrFile scr = ScriptFormat.ParseFile("scr/combat_guard.scr");

Console.WriteLine($"Description: {scr.Description}");
Console.WriteLine($"Script entries: {scr.Entries.Count}");

foreach (ScriptConditionData cond in scr.Entries)
    Console.WriteLine($"  condition type {cond.Type}, action type {cond.Action?.Type}");
```

### Parse a FacWalk walk mesh

```csharp
using ArcNET.Formats;

FacadeWalk walk = FacWalkFormat.ParseFile("art/walls/facwalk.wall_001");

Console.WriteLine($"Terrain: {walk.Header.Terrain}");
Console.WriteLine($"Size: {walk.Header.Width}×{walk.Header.Height}");

foreach (FacWalkEntry cell in walk.Entries)
    Console.WriteLine($"  ({cell.X},{cell.Y}) walkable={cell.Walkable}");
```

### Parse a JMP jump table

```csharp
using ArcNET.Formats;

JmpFile jmp = JmpFormat.ParseFile("maps/map_001.jmp");

foreach (JumpEntry jump in jmp.Jumps)
{
    Console.WriteLine($"Flags: {jump.Flags}");
    Console.WriteLine($"Source: {jump.SourceLoc} → Map {jump.DestinationMapId} @ {jump.DestinationLoc}");
}
```

### Parse a TextData file (`.tdf`)

```csharp
using ArcNET.Formats;

TextDataFile tdf = TextDataFormat.ParseFile("rules/chargenrules.tdf");

foreach (TextDataEntry entry in tdf.Entries)
    Console.WriteLine($"{entry.Key} = {entry.Value}");

// Convenient key lookup
if (tdf.Entries.FirstOrDefault(e => e.Key == "MaxLevel") is { } maxLevel)
    Console.WriteLine($"Max level: {maxLevel.Value}");
```

### Parse a save index (TFAI / `.svg`)

```csharp
using ArcNET.Formats;

SaveIndex index = SaveIndexFormat.ParseFile("save/slot_001.svg");

// TFAI tree: files and directories
void PrintTree(IReadOnlyList<TfaiEntry> entries, int depth = 0)
{
    foreach (TfaiEntry entry in entries)
    {
        var indent = new string(' ', depth * 2);
        if (entry is TfaiFileEntry file)
            Console.WriteLine($"{indent}[file] {file.Name} ({file.Size} bytes)");
        else if (entry is TfaiDirectoryEntry dir)
        {
            Console.WriteLine($"{indent}[dir]  {dir.Name}/");
            PrintTree(dir.Children, depth + 1);
        }
    }
}

PrintTree(index.Root);
```

### Parse a save-info file (`.gsi`)

```csharp
using ArcNET.Formats;

SaveInfo info = SaveInfoFormat.ParseFile("save/slot_001.gsi");

Console.WriteLine($"Module:  {info.ModuleName}");
Console.WriteLine($"Leader:  {info.LeaderName}  (level {info.LeaderLevel})");
Console.WriteLine($"Map ID:  {info.MapId}");
Console.WriteLine($"Day:     {info.GameTimeDays}");
Console.WriteLine($"Story:   {info.StoryState}");
```

### Parse a terrain file

```csharp
using ArcNET.Formats;

TerrainData terrain = TerrainFormat.ParseFile("maps/terrain_001.ter");

Console.WriteLine($"Version: {terrain.Version}");
Console.WriteLine($"Base terrain: {terrain.BaseTerrainType}");
Console.WriteLine($"Size: {terrain.Width}×{terrain.Height}  compressed={terrain.Compressed}");
Console.WriteLine($"Tile count: {terrain.Tiles.Count}");
```

### Parse map properties

```csharp
using ArcNET.Formats;

MapProperties mp = MapPropertiesFormat.ParseFile("maps/map_001.prp");

Console.WriteLine($"Art ID:  {mp.ArtId}");
Console.WriteLine($"Limit X: {mp.LimitX}");
Console.WriteLine($"Limit Y: {mp.LimitY}");
```

### Discover files by format

```csharp
using ArcNET.Formats;
using ArcNET.GameData;

IReadOnlyDictionary<FileFormat, IReadOnlyList<string>> discovered =
    GameDataLoader.DiscoverFiles("extracted/");

foreach ((FileFormat format, IReadOnlyList<string> paths) in discovered)
{
    if (paths.Count > 0)
        Console.WriteLine($"{format}: {paths.Count} files");
}
```

---

## ArcNET.Archive

### Open and enumerate a DAT archive

```csharp
using ArcNET.Archive;

// DatArchive holds a MemoryMappedFile — dispose when done
using DatArchive archive = DatArchive.Open("arcanum.dat");

Console.WriteLine($"Total entries: {archive.Entries.Count}");

foreach (ArchiveEntry entry in archive.Entries)
    Console.WriteLine($"{entry.Path}  {entry.UncompressedSize:N0} bytes  compressed={entry.IsCompressed}");
```

### Extract a single entry

```csharp
using ArcNET.Archive;

using DatArchive archive = DatArchive.Open("arcanum.dat");

// Extract one file — creates subdirectories as needed
await DatExtractor.ExtractEntryAsync(archive, "art/critters/barbarian.ART", outputDir: "extracted/");
```

### Extract all entries

```csharp
using ArcNET.Archive;

using DatArchive archive = DatArchive.Open("arcanum.dat");

var progress = new Progress<float>(p => Console.Write($"\rExtracting {p:P0}   "));
await DatExtractor.ExtractAllAsync(archive, outputDir: "extracted/", progress: progress);

Console.WriteLine("\nDone.");
```

### Read an entry without extracting

```csharp
using ArcNET.Archive;
using ArcNET.Formats;

using DatArchive archive = DatArchive.Open("arcanum.dat");

// Zero-copy MMF read — no intermediate file on disk
ReadOnlyMemory<byte> data = archive.GetEntryData("mes/game.mes");

MesFile mesFile = MessageFormat.ParseMemory(data);
Console.WriteLine($"Loaded {mesFile.Entries.Count} messages directly from DAT");

// Or open as a streaming view
using Stream stream = archive.OpenEntry("mes/game.mes");
// ... use stream ...
```

### Pack a directory into a DAT archive

```csharp
using ArcNET.Archive;

var progress = new Progress<float>(p => Console.Write($"\rPacking {p:P0}   "));
await DatPacker.PackAsync(inputDir: "extracted/", outputPath: "output.dat", progress: progress);

Console.WriteLine("\nPacked.");
```

### Extract using TFAF sub-archive

```csharp
using ArcNET.Formats;

// TFAF is a lightweight sub-archive used inside .dat entries (e.g., save files)
ReadOnlyMemory<byte> tfafData = File.ReadAllBytes("save/slot_001.sav");

// List all contained files
long totalBytes = TfafFormat.TotalPayloadSize(tfafData.Span);
Console.WriteLine($"TFAF payload: {totalBytes:N0} bytes");

// Extract all entries to a directory
TfafFormat.ExtractAll(tfafData.Span, outputDir: "save_extracted/");

// Extract a single named entry
byte[]? entry = TfafFormat.Extract(tfafData.Span, "party.gam");
if (entry is not null)
    Console.WriteLine($"Extracted party.gam — {entry.Length} bytes");
```

---

## ArcNET.GameObjects

### Read a game object from raw bytes

```csharp
using ArcNET.Core;
using ArcNET.GameObjects;

byte[] raw = File.ReadAllBytes("proto/items/weapon_001.pro");
var reader = new SpanReader(raw);

// Dispatches by ObjectType — returns the concrete sub-type
IGameObject obj = GameObject.Read(ref reader);

Console.WriteLine($"Type:    {obj.Header.GameObjectType}");
Console.WriteLine($"GUID:    {obj.ObjectId}");
Console.WriteLine($"ProtoId: {obj.ProtoId}");
Console.WriteLine($"Proto:   {obj.IsPrototype}");
```

### Read a game-object header only

```csharp
using ArcNET.Core;
using ArcNET.GameObjects;

byte[] raw = File.ReadAllBytes("proto/items/weapon_001.pro");
var reader = new SpanReader(raw);

GameObjectHeader header = GameObjectHeader.Read(ref reader);
Console.WriteLine($"Version:   {header.Version}");
Console.WriteLine($"Type:      {header.GameObjectType}");
Console.WriteLine($"Bitmap:    0x{header.Bitmap:X16}");
Console.WriteLine($"Fields:    {header.PropCollectionItems}");
```

### Use GameObjectStore

```csharp
using ArcNET.GameObjects;

var store = new GameObjectStore();

// Add many headers (e.g., after loading from files)
foreach (GameObjectHeader header in LoadHeaders())
    store.Add(header);

// Enumerate all
foreach (GameObjectHeader h in store.Headers)
    Console.WriteLine($"{h.ObjectId}  {h.GameObjectType}");

store.Clear();

static IEnumerable<GameObjectHeader> LoadHeaders() => [];
```

---

## ArcNET.GameData

> **Status (Preview):** `GameDataLoader` currently wires only `.mes` message files.
> Object loading from `.mob` / `.pro` and other formats is in progress.
> The `GameDataStore` design (dirty tracking, GUID index) is complete and stable.

### Load all messages from a directory

```csharp
using ArcNET.GameData;

// All .mes files in the tree are merged into a single store
GameDataStore store = await GameDataLoader.LoadFromDirectoryAsync(
    "extracted/",
    progress: new Progress<float>(p => Console.Write($"\rLoading {p:P0}   ")));

Console.WriteLine($"Messages loaded: {store.Messages.Count}");
```

### Load from in-memory buffers (editor / test)

```csharp
using ArcNET.GameData;

// No filesystem access — suitable for editors and unit tests
var blobs = new Dictionary<string, ReadOnlyMemory<byte>>
{
    ["game.mes"]    = File.ReadAllBytes("game.mes"),
    ["items.mes"]   = File.ReadAllBytes("items.mes"),
};

GameDataStore store = await GameDataLoader.LoadFromMemoryAsync(blobs);
Console.WriteLine($"Messages: {store.Messages.Count}");
```

### Save messages back to disk

```csharp
using ArcNET.GameData;

GameDataStore store = await GameDataLoader.LoadFromDirectoryAsync("extracted/");

// Single-file save (all messages → one .mes file)
GameDataSaver.SaveMessagesToFile(store, "output/game.mes");

// Or as a byte array — no filesystem needed
byte[] bytes = GameDataSaver.SaveMessagesToMemory(store);

// Or use the directory-save overload (creates output/game.mes automatically)
await GameDataSaver.SaveToDirectoryAsync(store, "output/");

// Or serialize to a virtual filename map
IReadOnlyDictionary<string, byte[]> files = GameDataSaver.SaveToMemory(store);
foreach ((string name, byte[] data) in files)
    Console.WriteLine($"{name}: {data.Length} bytes");
```

### Dirty tracking and the ObjectChanged event

```csharp
using ArcNET.Core.Primitives;
using ArcNET.GameData;
using ArcNET.GameObjects;

var store = new GameDataStore();

// Subscribe before loading so no events are missed
store.ObjectChanged += (_, guid) => Console.WriteLine($"Changed: {guid}");

// After loading ... mark an object dirty to trigger the event and dirty-set
var guid = new GameObjectGuid(/*...*/);
store.MarkDirty(in guid);

Console.WriteLine($"Dirty count: {store.DirtyObjects.Count}");

// Find an object by GUID in O(1)
GameObjectHeader? header = store.FindByGuid(in guid);

// Persist only dirty objects, then reset dirty state
await GameDataSaver.SaveToDirectoryAsync(store, "output/");
store.ClearDirty();
```

### Export to JSON

```csharp
using ArcNET.GameData;

GameDataStore store = await GameDataLoader.LoadFromDirectoryAsync("extracted/");

// Full store → JSON string (System.Text.Json source-generated, AOT-compatible)
string json = GameDataExporter.ExportToJson(store);
Console.WriteLine(json[..200]);

// Or write directly to a file
await GameDataExporter.ExportToJsonFileAsync(store, "output/gamedata.json");
```

---

## ArcNET.Patch

### Install the HighRes patch

```csharp
using ArcNET.Patch;

var progress = new Progress<float>(p => Console.Write($"\rInstalling {p:P0}   "));

await PatchInstaller.InstallAsync(
    gameDir: @"C:\Games\Arcanum",
    progress: progress);

Console.WriteLine("\nHighRes patch installed.");
```

### Uninstall the HighRes patch

```csharp
using ArcNET.Patch;

if (PatchUninstaller.IsPatchInstalled(@"C:\Games\Arcanum"))
{
    await PatchUninstaller.UninstallAsync(gameDir: @"C:\Games\Arcanum");
    Console.WriteLine("Patch removed.");
}
else
    Console.WriteLine("Patch is not installed.");
```

### Read or modify HighRes config

```csharp
using ArcNET.Patch;

HighResConfig config = HighResConfig.ParseFile(@"C:\Games\Arcanum\hires.ini");

Console.WriteLine($"Resolution: {config.Width}×{config.Height}");
Console.WriteLine($"Windowed:   {config.Windowed}");
Console.WriteLine($"Renderer:   {config.Renderer}");
```

---

## ArcNET.Core

### Low-level SpanReader / SpanWriter

```csharp
using ArcNET.Core;

// Read
byte[] data = File.ReadAllBytes("some.bin");
var reader = new SpanReader(data);

byte   b  = reader.ReadByte();
short  s  = reader.ReadInt16();
int    i  = reader.ReadInt32();
float  f  = reader.ReadSingle();
ReadOnlySpan<byte> chunk = reader.ReadBytes(16);

Console.WriteLine($"Remaining: {reader.Remaining}");

// Write to a pooled buffer
var buf = new System.Buffers.ArrayBufferWriter<byte>();
var writer = new SpanWriter(buf);

writer.WriteByte(0xFF);
writer.WriteInt32(42);
writer.WriteBytes([1, 2, 3, 4]);

byte[] result = buf.WrittenSpan.ToArray();
```

### Primitive types round-trip

```csharp
using ArcNET.Core;
using ArcNET.Core.Primitives;

// ArtId
var artId = new ArtId(0x00_01_00_FF);
Console.WriteLine(artId.ToString());         // formatted

// Location
var loc = new Location(X: 10, Y: 20);

// Color (RGB — ReadRgba strips the alpha channel)
var buf = new byte[] { 255, 128, 0, 255 };  // RGBA
var reader = new SpanReader(buf);
Color color = Color.ReadRgba(ref reader);
Console.WriteLine($"#{color.R:X2}{color.G:X2}{color.B:X2}");

// GameObjectGuid
var guid = new GameObjectGuid(Type: 0, Foo0: 0, Foo2: 0, Guid: 1234);
Console.WriteLine($"IsProto: {guid.IsProto}");

// PrefixedString (ushort-length-prefixed ASCII)
using var ms = new MemoryStream();
var sw = new SpanWriter(ms);
// write via extension method
```

### EnumLookup — frozen name maps

```csharp
using ArcNET.Core;

// O(1) frozen lookup for any enum — no reflection at call site
if (EnumLookup<DayOfWeek>.TryGetByName("Monday", out DayOfWeek day))
    Console.WriteLine($"Got: {day}");

string name = EnumLookup<DayOfWeek>.GetName(DayOfWeek.Friday);
Console.WriteLine(name);  // "Friday"
```
