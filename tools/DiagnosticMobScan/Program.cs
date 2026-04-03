// Diagnostic — scan Arcanum DAT archives for:
//   (1) Proto entries inside DAT files (shadowing check)
//   (2) Container mob instances where ObjFContainerInventorySource (bit 69) is non-zero
//
// OFF Header layout:
//   [0x00] int32  version     = 0x77
//   [0x04] uint32 protoType   = 0xFFFFFFFF → this IS a proto; other → instance
//   [0x0C] int32  protoId     (numeric prototype ID, e.g. 3057)
//   [0x34] int32  ObjectType  (2 = Container, confirmed from proto file dumps)
//   PROTO:    [0x38+] 12-byte bitmap, then properties (4 bytes each)
//   INSTANCE: [0x38] int16 PropCollectionItems, [0x3A+] 12-byte bitmap, then properties
//
// ObjFContainerInventorySource = bit 69.
//
// Run from repo root:
//   dotnet run --project tools/DiagnosticMobScan -- "C:\Games\Arcanum\<dir>"
//   dotnet run --project tools/DiagnosticMobScan -- "C:\Games\Arcanum\<dir>\modules"

using System.Buffers.Binary;
using System.Collections;
using ArcNET.Archive;

const int ContainerObjectType = 2;
const int ObjectTypeOffset = 0x34;
const int ProtoBitmapOffset = 0x38;
const int InstanceBitmapOffset = 0x3A; // +2 for PropCollectionItems int16
const int BitmapLength = 12;
const int ProtoIdOffset = 0x0C;
const int ProtoTypeOffset = 0x04;
const int InvSourceBit = 69;

var gameDir = args.Length > 0 ? args[0] : @"C:\Games\Arcanum\ArcanumCleanUAPnohighres - Copy";
var extractDir = args.Length > 1 ? args[1] : null;

// ── Special mode: extract & dump Bangellian mob files ─────────────────
if (gameDir.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
{
    var datPath = gameDir;
    using var arc = DatArchive.Open(datPath);
    var bangMobs = arc
        .Entries.Where(e =>
            !e.IsDirectory
            && e.Path.Contains("Bangellian", StringComparison.OrdinalIgnoreCase)
            && e.Path.EndsWith(".mob", StringComparison.OrdinalIgnoreCase)
        )
        .ToList();
    Console.WriteLine($"Bangellian mob files in {Path.GetFileName(datPath)}: {bangMobs.Count}");
    foreach (var entry in bangMobs)
    {
        var bytes = arc.ReadEntry(entry);
        var fname = Path.GetFileName(entry.Path.Replace('\\', '/'));
        Console.WriteLine($"\n=== {fname} ({bytes.Length} bytes) ===");
        Console.WriteLine($"  Full path in DAT: {entry.Path}");
        Console.WriteLine($"  Hex dump (192 bytes):");
        Console.WriteLine(BitConverter.ToString(bytes[..Math.Min(192, bytes.Length)]));
        // Raw parse
        var ver = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0));
        var protoType = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0x04));
        var protoId = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0x0C));
        var objType = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0x34));
        Console.WriteLine($"  ver=0x{ver:X} protoType=0x{protoType:X8} protoId={protoId} objType={objType}");
        // Print bitmap at both possible offsets
        Console.WriteLine($"  Bitmap @ 0x38: {BitConverter.ToString(bytes[0x38..0x44])}");
        Console.WriteLine($"  Bitmap @ 0x3A: {BitConverter.ToString(bytes[0x3A..0x46])}");
        // PropCollectionItems (instance extra int16 at 0x38)
        var propItems = BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(0x38));
        Console.WriteLine($"  PropCollectionItems (at 0x38): {propItems}");
        if (extractDir != null)
        {
            Directory.CreateDirectory(extractDir);
            File.WriteAllBytes(Path.Combine(extractDir, fname), bytes);
            Console.WriteLine($"  Extracted to: {Path.Combine(extractDir, fname)}");
        }
    }
    return;
}

Console.WriteLine($"Scanning DAT archives in: {gameDir}\n");

var datFiles = Directory.GetFiles(gameDir, "*.dat", SearchOption.TopDirectoryOnly);
Array.Sort(datFiles);

// ── Pass 0: Proto entries in DAT archives ────────────────────────────────
Console.WriteLine("=== Proto entries inside DAT archives ===");
bool anyProtos = false;
foreach (var datPath in datFiles)
{
    try
    {
        using var archive = DatArchive.Open(datPath);
        var protoEntries = archive
            .Entries.Where(e => !e.IsDirectory && e.Path.Contains("proto", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (protoEntries.Count > 0)
        {
            anyProtos = true;
            Console.WriteLine($"  {Path.GetFileName(datPath)}:");
            foreach (var e in protoEntries)
                Console.WriteLine($"    {e.Path}  ({e.UncompressedSize} B)");
        }
    }
    catch
    { /* skip non-DAT */
    }
}
if (!anyProtos)
    Console.WriteLine("  (none)");
Console.WriteLine();

// ── Pass 1: Container mob instances with InvSource != 0 ──────────────────
Console.WriteLine("=== Container mob instances: InvSource scan ===");

var hits = new List<(string datFile, string virtualPath, int protoId, int invSource, int fileOffset)>();

foreach (var datPath in datFiles)
{
    Console.Write($"  {Path.GetFileName(datPath)} ... ");
    int mobCount = 0;
    int containerCount = 0;
    int hitCount = 0;

    try
    {
        using var archive = DatArchive.Open(datPath);
        foreach (var entry in archive.Entries)
        {
            if (entry.IsDirectory)
                continue;
            if (!entry.Path.EndsWith(".mob", StringComparison.OrdinalIgnoreCase))
                continue;
            mobCount++;

            byte[] bytes;
            try
            {
                bytes = archive.ReadEntry(entry);
            }
            catch
            {
                continue;
            }

            if (bytes.Length < InstanceBitmapOffset + BitmapLength)
                continue;

            var version = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0x00));
            if (version != 0x77)
                continue;

            var objType = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(ObjectTypeOffset));
            if (objType != ContainerObjectType)
                continue;

            containerCount++;

            // Determine proto vs instance by the type field at 0x04
            var protoTypeField = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(ProtoTypeOffset));
            bool isProto = protoTypeField == 0xFFFFFFFF;

            int bitmapStart = isProto ? ProtoBitmapOffset : InstanceBitmapOffset;
            var protoId = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(ProtoIdOffset));

            if (bytes.Length < bitmapStart + BitmapLength)
                continue;

            var bitmap = new BitArray(bytes[bitmapStart..(bitmapStart + BitmapLength)]);

            // Always report: print path + set bits for all containers
            Console.WriteLine();
            Console.Write(
                $"    {entry.Path}  proto={protoId}  isProto={isProto}  bitmapBit69={(bitmap.Length > 69 ? bitmap[69] : false)}"
            );

            if (!bitmap[InvSourceBit])
            {
                Console.WriteLine("  (bit69 not set – inherits from proto)");
                continue;
            }

            // Walk to bit 69
            int cursor = bitmapStart + BitmapLength;
            for (int bit = 0; bit < InvSourceBit; bit++)
            {
                if (bitmap[bit])
                    cursor += 4;
            }

            if (cursor + 4 > bytes.Length)
            {
                Console.WriteLine("  (out of bounds reading InvSource)");
                continue;
            }

            var invSource = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(cursor));
            Console.Write($"  InvSource={invSource} offset={cursor}");

            if (invSource != 0)
            {
                Console.Write("  *** NEEDS PATCH ***");
                hitCount++;
                hits.Add((datPath, entry.Path, protoId, invSource, cursor));
            }
            Console.WriteLine();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
        continue;
    }

    Console.WriteLine($"  → {hitCount} hit(s) / {containerCount} containers / {mobCount} mob files");
}

Console.WriteLine();
if (hits.Count == 0)
{
    Console.WriteLine("No container mob instances found with InvSource != 0.");
}
else
{
    Console.WriteLine($"=== {hits.Count} container mob files with non-zero InvSource ===");
    Console.WriteLine($"{"DAT", -28} {"ProtoId", 8} {"InvSource", 12} {"Offset", 12}  VirtualPath");
    Console.WriteLine(new string('-', 110));
    foreach (var (datFile, vpath, protoId, invSource, offset) in hits)
    {
        var eb = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(eb, invSource);
        Console.WriteLine(
            $"{Path.GetFileName(datFile), -28} {protoId, 8} {invSource, 12} 0x{offset:X6}({offset, 6})  {vpath}"
        );
    }
}
