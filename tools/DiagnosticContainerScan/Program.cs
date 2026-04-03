// Diagnostic — scan Arcanum container protos for Fancy Chest and wrong inventory source.
// Uses the actual OFF (Object File Format) header layout from ArcanumResearchDoc.md.
//
// OFF Header layout (56 bytes, offsets 0x00–0x37):
//   [0x00] int32  version       = 0x77
//   [0x04] int16  subtype       = 0xFFFF (proto) | 0x0001 (instance)
//   [0x06] 6 B    handle_part1  (in-memory, zero on disk)
//   [0x0C] int32  prototype_id
//   [0x10] 12 B   handle_part2  (in-memory, zero on disk)
//   [0x1C] int64  guid_type
//   [0x24] 16 B   guid
//   [0x34] int32  object_type   (2 = Container in Arcanum)
//   [0x38+]       12-byte bitmap for Container
//   property data follows bitmap (4 bytes each, in bit-index order)
//
// ObjectField bits (from ArcNET ObjectField.cs):
//   ObjFName=21, ObjFDescription=22, ObjFAid=23
//   ObjFContainerFlags=64, ObjFContainerLockDifficulty=65, ObjFContainerKeyId=66,
//   ObjFContainerInventoryNum=67, ObjFContainerInventoryListIdx=68, ObjFContainerInventorySource=69
//   ObjFContainerNotifyNpc=70
//
// Run from repo root:
//   dotnet run --project tools/DiagnosticContainerScan

using System.Buffers.Binary;
using System.Collections;
using System.Text;
using ArcNET.Archive;

const string ProtoDir = @"C:\Games\Arcanum\ArcanumUAPclean\data\proto";
const int ContainerObjectType = 2; // Arcanum: Portal=1, Container=2, Scenery=3 …
const int BitmapLength = 12;
const int HeaderSize = 0x38; // 56 bytes
const int BitCount = BitmapLength * 8;

const int ObjFNameBit = 21;
const int ObjFDescriptionBit = 22;
const int ObjFAidBit = 23;
const int ObjFContainerInventorySourceBit = 69;

// Returns all set bits and their corresponding int32 property values, plus the byte offset
// of ObjFContainerInventorySource within the file (for RawAtOffset patches).
static (Dictionary<int, int> fields, int invSourceOffset) ReadAllProps(byte[] data)
{
    var bitmap = new BitArray(data[HeaderSize..(HeaderSize + BitmapLength)]);
    int cursor = HeaderSize + BitmapLength;
    var fields = new Dictionary<int, int>();
    int invSourceOffset = -1;

    for (var bit = 0; bit < BitCount; bit++)
    {
        if (!bitmap[bit])
            continue;
        if (cursor + 4 > data.Length)
            break;
        var v = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(cursor));
        fields[bit] = v;
        if (bit == ObjFContainerInventorySourceBit)
            invSourceOffset = cursor;
        cursor += 4;
    }
    return (fields, invSourceOffset);
}

static bool IsValidContainerProto(byte[] data, out int objType)
{
    objType = 0;
    if (data.Length < HeaderSize + BitmapLength)
        return false;
    var version = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0x00));
    var subtype = BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(0x04));
    objType = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0x34));
    return version == 0x77 && subtype == unchecked((short)0xFFFF) && objType == ContainerObjectType;
}

static int ProtoIdFromFilename(string file)
{
    var name = Path.GetFileNameWithoutExtension(Path.GetFileName(file));
    return int.TryParse(name[..6], out var id) ? id : -1;
}

var files = Directory.GetFiles(ProtoDir, "*Container.pro");
Array.Sort(files);
Console.WriteLine($"Scanning {files.Length} container protos in:\n  {ProtoDir}\n");

// ── Pass 1: Protos with wrong InvSource — full field dump ──────────────
Console.WriteLine("=== Protos with wrong ObjFContainerInventorySource (full field dump) ===\n");
foreach (var file in files)
{
    try
    {
        var data = File.ReadAllBytes(file);
        if (!IsValidContainerProto(data, out _))
            continue;
        var (fields, invSourceOffset) = ReadAllProps(data);
        if (!fields.TryGetValue(ObjFContainerInventorySourceBit, out var invSource) || invSource == 0)
            continue;

        var name = fields.TryGetValue(ObjFNameBit, out var nv) ? nv.ToString() : "-";
        var desc = fields.TryGetValue(ObjFDescriptionBit, out var dv) ? dv.ToString() : "-";
        var aid = fields.TryGetValue(ObjFAidBit, out var av) ? av.ToString() : "-";

        Console.WriteLine($"{Path.GetFileName(file)}  (proto id={ProtoIdFromFilename(file)})");
        Console.WriteLine(
            $"  ObjFName(21)={name}  ObjFDesc(22)={desc}  ObjFAid(23)={aid}  InvSource={invSource}  InvSrcOffset=0x{invSourceOffset:X}"
        );
        Console.Write("  All set bits: ");
        foreach (var (bit, val) in fields.OrderBy(x => x.Key))
            Console.Write($"[{bit}]={val} ");
        Console.WriteLine();
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{Path.GetFileName(file)}  ERROR: {ex.Message}");
    }
}

// ── Pass 2: Summary table with InvSrcOffset for RawAtOffset patches ────
Console.WriteLine("\n=== Summary — protos where InvSource != 0 (for patch JSON) ===\n");
Console.WriteLine($"{"File", -42} {"ProtoId", 8} {"InvSource", 10} {"InvSrcOffset", 14}  proto path");
Console.WriteLine(new string('-', 108));
foreach (var file in files)
{
    try
    {
        var data = File.ReadAllBytes(file);
        if (!IsValidContainerProto(data, out _))
            continue;
        var (fields, invSourceOffset) = ReadAllProps(data);
        if (!fields.TryGetValue(ObjFContainerInventorySourceBit, out var invSource) || invSource == 0)
            continue;
        var protoId = ProtoIdFromFilename(file);
        Console.WriteLine(
            $"{Path.GetFileName(file), -42} {protoId, 8} {invSource, 10} 0x{invSourceOffset:X4} ({invSourceOffset, 6})  data/proto/{Path.GetFileName(file)}"
        );
    }
    catch
    { /* skip */
    }
}

Console.WriteLine("\nDone.");

// ── Module dat scan ────────────────────────────────────────────────────

const string ModuleDat = @"C:\Games\Arcanum\ArcanumClean\modules\Arcanum.dat";
Console.WriteLine($"\n\n=== Scanning module dat for container protos ===\n  {ModuleDat}\n");

using var archive = DatArchive.Open(ModuleDat);
var allEntries = archive.Entries.Select(e => e.Path).OrderBy(p => p).ToList();
Console.WriteLine($"Total entries in module dat: {allEntries.Count}");

// List all proto entries and any entries whose path contains "bangellian" or "deeps"
var protoEntries = allEntries
    .Where(p =>
        p.Contains("proto", StringComparison.OrdinalIgnoreCase)
        || p.Contains("bangellian", StringComparison.OrdinalIgnoreCase)
        || p.Contains("deeps", StringComparison.OrdinalIgnoreCase)
    )
    .ToList();

Console.WriteLine($"\nProto/Bangellian/Deeps entries ({protoEntries.Count}):");
foreach (var e in protoEntries)
    Console.WriteLine($"  {e}");

// ── Scan mob files in Cave of the Bangellian Scourge for container objects ──
// Each .mob file is an object instance (OFF subtype=0x0001).
// Offset 0x0C = int32 prototype_id — tells us which proto this instance uses.
// Offset 0x34 = int32 object_type  — 2 = Container.

const int ContainerType = 2;
const int MobHeaderPrototypeIdOffset = 0x0C;
const int MobHeaderObjectTypeOffset = 0x34;
const int MobHeaderSize = 0x38;
const int MobBitmapLength = 12;
const int FaultyProtoMin = 3055;
const int FaultyProtoMax = 3075;

var bangellianMobs = allEntries
    .Where(p =>
        p.Contains("bangellian", StringComparison.OrdinalIgnoreCase)
        && p.EndsWith(".mob", StringComparison.OrdinalIgnoreCase)
    )
    .ToList();

Console.WriteLine($"\n=== Mob objects in Cave of the Bangellian Scourge ({bangellianMobs.Count} files) ===\n");
Console.WriteLine($"{"MobFile", -60} {"ProtoId", 8} {"ObjType", 9} {"IsFaulty", 10}");
Console.WriteLine(new string('-', 92));

foreach (var mobPath in bangellianMobs.OrderBy(p => p))
{
    try
    {
        var entry = archive.FindEntry(mobPath);
        if (entry is null)
            continue;
        var mobData = archive.ReadEntry(entry);
        if (mobData.Length < MobHeaderSize + MobBitmapLength)
            continue;

        var protoId = BinaryPrimitives.ReadInt32LittleEndian(mobData.AsSpan(MobHeaderPrototypeIdOffset, 4));
        var objType = BinaryPrimitives.ReadInt32LittleEndian(mobData.AsSpan(MobHeaderObjectTypeOffset, 4));
        var isFaulty = objType == ContainerType && protoId >= FaultyProtoMin && protoId <= FaultyProtoMax;
        var marker = isFaulty ? " <-- BANGELLIAN CHEST CANDIDATE" : "";

        Console.WriteLine($"{Path.GetFileName(mobPath), -60} {protoId, 8} {objType, 9} {isFaulty, 10}{marker}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{Path.GetFileName(mobPath), -60} ERROR: {ex.Message}");
    }
}

// List all map/sector directories
var mapEntries = allEntries.Where(p => p.Contains("maps", StringComparison.OrdinalIgnoreCase)).ToList();
Console.WriteLine($"\nMap entries ({mapEntries.Count} total) — first 30:");
foreach (var e in mapEntries.Take(30))
    Console.WriteLine($"  {e}");
if (mapEntries.Count > 30)
    Console.WriteLine($"  ... and {mapEntries.Count - 30} more");
