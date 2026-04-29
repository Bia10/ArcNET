# ArcNET — Example Catalogue

Comprehensive, copy-paste-ready examples for every ArcNET library.
All code targets `net10.0` / C# 14.

> **NativeAOT compatible.** Every library is built with `IsAotCompatible=true`.
> All examples run unmodified with `PublishAot=true` — no `rd.xml`, no `[DynamicDependency]`.
> JSON serialization uses `[JsonSerializable]` source generation (see [ArcNET.GameData](#arcnetgamedata)).

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
- [ArcNET.Dumpers](#arcnetdumpers)
  - [Dump a mob file](#dump-a-mob-file)
  - [Dump a prototype file](#dump-a-prototype-file)
  - [Dump a sector file](#dump-a-sector-file)
  - [Dump a message file](#dump-a-message-file)
- [ArcNET.BinaryPatch](#arcnetbinarypatch)
  - [Create a patch set (code)](#create-a-patch-set-code)
  - [Apply, revert, and verify patches](#apply-revert-and-verify-patches)
  - [Load patches from JSON](#load-patches-from-json)
  - [Discover patches at runtime](#discover-patches-at-runtime)
  - [Track patch state](#track-patch-state)
- [ArcNET.Editor](#arcneteditor)
    - [Open a combined editor workspace](#open-a-combined-editor-workspace)
    - [Load a save slot](#load-a-save-slot)
    - [Edit the player character](#edit-the-player-character)
    - [Edit save metadata](#edit-save-metadata)
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

Console.WriteLine($"Tiles: {sector.Tiles.Length}");       // uint[4096]
Console.WriteLine($"Lights: {sector.Lights.Count}");
Console.WriteLine($"Objects: {sector.Objects.Count}");

if (sector.HasRoofs)
    Console.WriteLine($"Roofs: {sector.Roofs!.Length}");  // uint[256]

// Check whether tile (3, 7) is blocked via the 128-uint bitmask
// Each uint covers 32 tiles; bit index = y * 64 + x
int tileIndex = 7 * 64 + 3;
bool blocked = (sector.BlockMask[tileIndex / 32] & (1u << (tileIndex % 32))) != 0;
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

TerrainData terrain = TerrainFormat.ParseFile("data/terrain/outdoors.tdf");

Console.WriteLine($"Version: {terrain.Version}");
Console.WriteLine($"Base terrain: {terrain.BaseTerrainType}");
Console.WriteLine($"Size: {terrain.Width}×{terrain.Height}  compressed={terrain.Compressed}");
Console.WriteLine($"Tile count: {terrain.Tiles.Length}");  // ushort[]
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

// TFAF is a sub-archive used for save files; it requires a SaveIndex to map virtual paths.
SaveIndex index = SaveIndexFormat.ParseFile("save/slot_001.tfai");
ReadOnlyMemory<byte> tfafBlob = File.ReadAllBytes("save/slot_001.tfaf");

// Sum of all payload bytes
int totalBytes = TfafFormat.TotalPayloadSize(index);
Console.WriteLine($"TFAF payload: {totalBytes:N0} bytes");

// Extract all entries to a virtual-path → bytes map
IReadOnlyDictionary<string, byte[]> all = TfafFormat.ExtractAll(index, tfafBlob);

// Extract a single named entry
byte[] entry = TfafFormat.Extract(index, tfafBlob, "party.gam");
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

> **NativeAOT note:** All JSON serialization in this package uses `[JsonSerializable]` source generation.
> No reflection is used at any call site. Safe for `PublishAot=true` without extra annotations.

> **Status:** `GameDataLoader` wires `FileFormat.Message`, `FileFormat.Sector`, `FileFormat.Proto`, and `FileFormat.Mob`.
> Other formats (Dialog, Script, Art, …) are discovered by `DiscoverFiles` but not yet dispatched into the store.

### Load all game data from a directory

```csharp
using ArcNET.GameData;

// Messages, sectors, protos, and mobs are all loaded concurrently
GameDataStore store = await GameDataLoader.LoadFromDirectoryAsync(
    "extracted/",
    progress: new Progress<float>(p => Console.Write($"\rLoading {p:P0}   ")));

Console.WriteLine($"Messages : {store.Messages.Count}");
Console.WriteLine($"Sectors  : {store.Sectors.Count}");
Console.WriteLine($"Protos   : {store.Protos.Count}");
Console.WriteLine($"Mobs     : {store.Mobs.Count}");
```

### Access message entries (index, sound ID, text)

```csharp
using ArcNET.GameData;
using ArcNET.Formats;  // MessageEntry

GameDataStore store = await GameDataLoader.LoadFromDirectoryAsync("extracted/");

foreach (MessageEntry msg in store.Messages)
{
    // msg.Index is the original .mes index number (preserved on round-trip)
    // msg.SoundId is null when not present in the source file
    // msg.Text is the display string
    Console.WriteLine($"[{msg.Index}] ({msg.SoundId ?? "—"}) {msg.Text}");
}
```

### Load from in-memory buffers (editor / test)

```csharp
using ArcNET.GameData;

// No filesystem access — suitable for editors and unit tests
// Keys can be any filename; format is inferred from the extension
var blobs = new Dictionary<string, ReadOnlyMemory<byte>>
{
    ["game.mes"]     = File.ReadAllBytes("game.mes"),
    ["items.mes"]    = File.ReadAllBytes("items.mes"),
    ["map_001.sec"]  = File.ReadAllBytes("map_001.sec"),
    ["critter.pro"]  = File.ReadAllBytes("critter.pro"),
};

GameDataStore store = await GameDataLoader.LoadFromMemoryAsync(blobs);

Console.WriteLine($"Messages: {store.Messages.Count}");
Console.WriteLine($"Sectors : {store.Sectors.Count}");
Console.WriteLine($"Protos  : {store.Protos.Count}");
```

### Save all data back to disk

```csharp
using ArcNET.GameData;

GameDataStore store = await GameDataLoader.LoadFromDirectoryAsync("extracted/");

// Save every data type into output/ in one call
await GameDataSaver.SaveToDirectoryAsync(store, "output/");

// Or save individual types
GameDataSaver.SaveMessagesToFile(store, "output/game.mes");      // preserves original indices
GameDataSaver.SaveSectorsToDirectory(store, "output/sectors/");  // sector_000000.sec, …
GameDataSaver.SaveProtosToDirectory(store, "output/protos/");   // proto_000000.pro, …
GameDataSaver.SaveMobsToDirectory(store, "output/mobs/");       // mob_000000.mob, …
```

### Round-trip to an in-memory virtual filesystem

```csharp
using ArcNET.GameData;

GameDataStore store = await GameDataLoader.LoadFromDirectoryAsync("extracted/");

// Serialize to a virtual filename → bytes map (no filesystem writes)
IReadOnlyDictionary<string, byte[]> files = GameDataSaver.SaveToMemory(store);

foreach ((string name, byte[] data) in files)
    Console.WriteLine($"{name}: {data.Length} bytes");

// Round-trip: load the virtual files back
GameDataStore restored = await GameDataLoader.LoadFromMemoryAsync(
    files.ToDictionary(kv => kv.Key, kv => (ReadOnlyMemory<byte>)kv.Value));
```

### Dirty tracking and the ObjectChanged event

```csharp
using ArcNET.Core.Primitives;
using ArcNET.GameData;
using ArcNET.GameObjects;

var store = new GameDataStore();

// Subscribe before loading so no events are missed
store.ObjectChanged += (_, guid) => Console.WriteLine($"Changed: {guid}");

// After loading … mark an object dirty to trigger the event and dirty-set
var guid = new GameObjectGuid(/*...*/);
store.MarkDirty(in guid);

Console.WriteLine($"Dirty count: {store.DirtyObjects.Count}");

// Find an object by GUID in O(1) via lazy FrozenDictionary
GameObjectHeader? header = store.FindByGuid(in guid);

// Persist, then reset dirty state
await GameDataSaver.SaveToDirectoryAsync(store, "output/");
store.ClearDirty();
```

### Export to JSON (AOT-safe)

```csharp
using ArcNET.GameData;

GameDataStore store = await GameDataLoader.LoadFromDirectoryAsync("extracted/");

// Full store → JSON string
// Uses [JsonSerializable] source generation — no reflection, safe for PublishAot=true
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

## ArcNET.Dumpers

### Dump a mob file

```csharp
using ArcNET.Dumpers;
using ArcNET.Formats;

byte[] bytes = File.ReadAllBytes("arcanum/data/mobile/00001234.mob");
MobData mob = MobFormat.ParseMemory(bytes);

string text = MobDumper.Dump(mob);
Console.WriteLine(text);
// Header, every present field name, byte sizes, decoded scalars,
// inventory / array fields expanded with per-element detail.
```

### Dump a prototype file

```csharp
using ArcNET.Dumpers;
using ArcNET.Formats;

byte[] bytes = File.ReadAllBytes("arcanum/data/proto/containers/00000025.pro");
ProtoData proto = ProtoFormat.ParseMemory(bytes);

string text = ProtoDumper.Dump(proto);
Console.WriteLine(text);
```

### Dump a sector file

```csharp
using ArcNET.Dumpers;
using ArcNET.Formats;

byte[] bytes = File.ReadAllBytes("arcanum/data/maps/a_map/sector0001.sec");
Sector sector = SectorFormat.ParseMemory(bytes);

string text = SectorDumper.Dump(sector);
Console.WriteLine(text);
```

### Dump a message file

```csharp
using ArcNET.Dumpers;
using ArcNET.Formats;

byte[] bytes = File.ReadAllBytes("arcanum/mes/game.mes");
MesFile mes = MessageFormat.ParseMemory(bytes);

string text = MessageDumper.Dump(mes);
Console.WriteLine(text);
```

> All dumpers follow the same pattern: `XxxDumper.Dump(parsedData)` → `string`.
> Available dumpers: `MobDumper`, `ProtoDumper`, `SectorDumper`, `ArtDumper`,
> `DialogDumper`, `ScriptDumper`, `MessageDumper`, `JmpDumper`, `MapPropertiesDumper`,
> `TerrainDumper`, `SaveIndexDumper`, `SaveInfoDumper`, `FacWalkDumper`, `ItemDumper`.

---

## ArcNET.BinaryPatch

### Create a patch set (code)

```csharp
using ArcNET.BinaryPatch;
using ArcNET.BinaryPatch.Patches;
using ArcNET.GameObjects;

// Fix an int32 field inside a .pro file
var fixChest = ProtoFieldPatch.SetInt32(
    id: "fix-bangellian-chest",
    description: "Reset container inventory source to -1",
    relativePath: "data/proto/containers/00000025.pro",
    field: ObjectField.ObjFContainerInventorySource,
    expectedValue: 0,
    newValue: -1
);

// Raw byte patch at a known offset (e.g. EXE or opaque format)
var exePatch = RawBinaryPatch.AtOffset(
    id: "disable-intro-movie",
    description: "NOP the intro movie call",
    relativePath: "arcanum.exe",
    offset: 0x1A2B3C,
    expectedBytes: [0xE8, 0x12, 0x34, 0x56],
    newBytes:      [0x90, 0x90, 0x90, 0x90]
);

// Group patches into a named, versioned set
var patchSet = new BinaryPatchSet
{
    Name = "ArcNET Vanilla Bug Fixes",
    Version = "1.0.0",
    Patches = [fixChest, exePatch],
};
```

### Apply, revert, and verify patches

```csharp
using ArcNET.BinaryPatch;

string gameDir = @"C:\Games\Arcanum";

// Apply — creates .bak backups by default
IReadOnlyList<PatchResult> results = BinaryPatcher.Apply(patchSet, gameDir);

foreach (var r in results)
    Console.WriteLine($"{r.PatchId}: {r.Status} {r.Reason}");

// Dry-run — check what would happen without writing
var dryResults = BinaryPatcher.Apply(
    patchSet, gameDir,
    new PatchOptions { DryRun = true }
);

// Verify — read-only check of which patches still need applying
IReadOnlyList<PatchVerifyResult> verify = BinaryPatcher.Verify(patchSet, gameDir);

foreach (var v in verify)
    Console.WriteLine($"{v.PatchId}: NeedsApply={v.NeedsApply}, FileExists={v.FileExists}");

// Revert — restore .bak backups
IReadOnlyList<PatchResult> reverted = BinaryPatcher.Revert(patchSet, gameDir);
```

### Load patches from JSON

```csharp
using ArcNET.BinaryPatch.Json;

// From a file on disk
BinaryPatchSet patchSet = JsonPatchLoader.LoadFile("patches/vanilla-fixes.json");

// From an embedded resource
BinaryPatchSet embedded = JsonPatchLoader.LoadEmbedded(
    typeof(Program).Assembly,
    "ArcNET.App.patches.vanilla-fixes.json"
);

// Apply the loaded set
var results = BinaryPatcher.Apply(patchSet, @"C:\Games\Arcanum");
```

### Discover patches at runtime

```csharp
using ArcNET.BinaryPatch;

// Scan the patches/ directory next to the executable
IReadOnlyList<BinaryPatchSet> allSets = PatchDiscovery.LoadAll(
    onError: (file, ex) => Console.Error.WriteLine($"Skipped {file}: {ex.Message}")
);

// Or scan a custom directory
IReadOnlyList<BinaryPatchSet> custom = PatchDiscovery.LoadAll("mods/patches");

foreach (var set in allSets)
    Console.WriteLine($"Found: {set.Name} v{set.Version} ({set.Patches.Count} patches)");
```

### Track patch state

```csharp
using ArcNET.BinaryPatch;
using ArcNET.BinaryPatch.State;

string gameDir = @"C:\Games\Arcanum";

// Record a successful apply
PatchState state = PatchStateStore.RecordApply(gameDir, patchSet);
// Writes .arcnet-patches.json in the game directory

// Check if a patch set is recorded
bool isApplied = PatchStateStore.IsRecorded(gameDir, patchSet);

// Record a revert (removes the entry; deletes file when empty)
PatchStateStore.RecordRevert(gameDir, patchSet);
```

---

## ArcNET.Editor

### Open a combined editor workspace

```csharp
using ArcNET.Editor;

EditorWorkspace workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(
    gameDir: @"C:\Games\Arcanum",
    new EditorWorkspaceLoadOptions
    {
        SaveFolder = @"C:\Games\Arcanum\modules\Arcanum\Save",
        SaveSlotName = "Slot0001",
    });

Console.WriteLine($"Messages: {workspace.GameData.Messages.Count}");
Console.WriteLine($"Scripts : {workspace.GameData.Scripts.Count}");
Console.WriteLine($"Dialogs : {workspace.GameData.Dialogs.Count}");
Console.WriteLine($"Assets  : {workspace.Assets.Count}");
Console.WriteLine($"Has save: {workspace.HasSaveLoaded}");
Console.WriteLine($"Install : {workspace.InstallationType}");
Console.WriteLine($"Skipped assets: {workspace.LoadReport.SkippedAssets.Count}");
Console.WriteLine($"Validation findings: {workspace.Validation.Issues.Count}");
Console.WriteLine(
    $"Validation warnings: {workspace.Validation.Issues.Count(issue => issue.Severity == EditorWorkspaceValidationSeverity.Warning)}");

// Validation currently covers missing proto/script definitions, install-aware
// proto display-name entries, broken dialog response targets, and unknown
// script attachment slots.

for (var i = 0; i < Math.Min(5, workspace.Validation.Issues.Count); i++)
    Console.WriteLine(workspace.Validation.Issues[i]);

var gameMes = workspace.Assets.Find("mes/game.mes");
if (gameMes is not null)
{
    Console.WriteLine($"game.mes source kind : {gameMes.SourceKind}");
    Console.WriteLine($"game.mes source path : {gameMes.SourcePath}");
}

var proto6051 = workspace.Index.FindProtoDefinition(6051);
if (proto6051 is not null)
    Console.WriteLine($"Proto 6051: {proto6051.AssetPath}");

var proto6051Refs = workspace.Index.FindProtoReferences(6051);
Console.WriteLine($"Proto 6051 reference assets: {proto6051Refs.Count}");

var msg10Assets = workspace.Index.FindMessageAssets(10);
Console.WriteLine($"Message 10 appears in {msg10Assets.Count} asset(s)");

if (workspace.Index.MapNames.Count > 0)
{
    var firstMap = workspace.Index.MapNames[0];
    Console.WriteLine($"First map: {firstMap}");
    Console.WriteLine($"First map asset count: {workspace.Index.FindMapAssets(firstMap).Count}");
}

var script1Defs = workspace.Index.FindScriptDefinitions(1);
Console.WriteLine($"Script 1 definition assets: {script1Defs.Count}");

var script1Details = workspace.Index.FindScriptDetails(1);
foreach (var script in script1Details)
{
    Console.WriteLine(
        $"Script {script.ScriptId}: {script.Asset.AssetPath} => {script.ActiveAttachmentCount} active attachment(s) [{string.Join(", ", script.ActiveAttachmentPoints)}]"
    );
}

var dialog1Defs = workspace.Index.FindDialogDefinitions(1);
Console.WriteLine($"Dialog 1 definition assets: {dialog1Defs.Count}");

var dialog1Details = workspace.Index.FindDialogDetails(1);
foreach (var dialog in dialog1Details)
{
    Console.WriteLine(
        $"Dialog {dialog.DialogId}: {dialog.Asset.AssetPath} => {dialog.EntryCount} entries, {dialog.ControlEntryCount} control entries, {dialog.MissingResponseTargetNumbers.Count} missing positive target(s)"
    );
}

var script1Refs = workspace.Index.FindScriptReferences(1);
Console.WriteLine($"Script 1 reference assets: {script1Refs.Count}");

var artRefSample = workspace.Index.FindArtReferences(0x00112233);
Console.WriteLine($"Art 0x00112233 reference assets: {artRefSample.Count}");

if (workspace.HasSaveLoaded)
{
    var editor = workspace.CreateSaveEditor();
    Console.WriteLine($"Leader: {editor.GetCurrentSaveInfo().LeaderName}");
}

// Loose or extracted content still works when you want to bypass install DATs.
EditorWorkspace looseWorkspace = await EditorWorkspaceLoader.LoadAsync(
    contentDirectory: @"C:\ArcanumExtracted",
    new EditorWorkspaceLoadOptions { GameDirectory = @"C:\Games\Arcanum" });
```

The same workspace report is available from the repo CLI:

```shell
dotnet run --project src/App/ArcNET.App/ArcNET.App.csproj -c Release -- editor validate "C:\Games\Arcanum" --severity warning --top 20
```

### Load a save slot

```csharp
using ArcNET.Editor;

LoadedSave save = SaveGameLoader.Load(
    @"C:\Games\Arcanum\modules\Arcanum\Save",
    "Slot0001");

Console.WriteLine($"Leader     : {save.Info.LeaderName} (lv {save.Info.LeaderLevel})");
Console.WriteLine($"mobile.mdy : {save.MobileMdys.Count}");
Console.WriteLine($"ParseErrors: {save.ParseErrors.Count}");
```

### Edit the player character

```csharp
using ArcNET.Editor;

LoadedSave save = SaveGameLoader.Load(
    @"C:\Games\Arcanum\modules\Arcanum\Save",
    "Slot0001");

var editor = new SaveGameEditor(save)
    .WithPlayerCharacter(pc => pc.ToBuilder()
        .WithLevel(pc.Level + 1)
        .WithSkillPersuasion(pc.SkillPersuasion + 1)
        .WithName("Roberta")
        .Build());

// Writes Slot0001_EDITED.gsi/.tfai/.tfaf.
// LeaderName / LeaderLevel / LeaderPortraitId in the .gsi are synchronized
// from the edited player record automatically.
editor.Save(@"C:\Games\Arcanum\modules\Arcanum\Save", "Slot0001_EDITED");
```

### Edit save metadata

```csharp
using ArcNET.Editor;
using ArcNET.Formats;

LoadedSave save = SaveGameLoader.Load(
    @"C:\Games\Arcanum\modules\Arcanum\Save",
    "Slot0001");

var editor = new SaveGameEditor(save)
    .WithSaveInfo(info => info.With(
        displayName: "Bridge Run",
        gameTimeDays: 42,
        gameTimeMs: 18_000_000,
        leaderTileX: 480,
        leaderTileY: 512));

SaveInfo pending = editor.GetCurrentSaveInfo();
Console.WriteLine($"{pending.DisplayName} @ ({pending.LeaderTileX}, {pending.LeaderTileY})");

editor.Save(@"C:\Games\Arcanum\modules\Arcanum\Save", "Slot0001_RENAMED");
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

// Skip bytes without reading
reader.Skip(8);  // advance 8 bytes

// Peek ahead without moving position
int nextInt = reader.PeekInt32At(0);   // read int32 at current position
int later   = reader.PeekInt32At(12);  // read int32 12 bytes ahead

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

// GameObjectGuid — 24-byte ObjectID (int16 + int16 + int32 + Guid)
var guid = new GameObjectGuid(OidType: 2, Padding2: 0, Padding4: 0, Id: Guid.NewGuid());
Console.WriteLine($"IsProto: {guid.IsProto}");   // false — OidType != -1
Console.WriteLine(guid.ToString());               // OID(2):xxxxxxxx-xxxx-...

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
