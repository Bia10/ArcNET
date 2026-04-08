using ArcNET.Core;
using ArcNET.Editor;
using ArcNET.Formats;
using ArcNET.GameObjects;

const string saveDir = @"C:\Games\Arcanum\ArcanumCleanUAPnohighres - Copy\modules\Arcanum\Save";

static void Cmp(string label, byte[] a, byte[] b)
{
    if (a.AsSpan().SequenceEqual(b))
    {
        Console.WriteLine($"  {label}: IDENTICAL ({a.Length}B)");
        return;
    }
    int d = 0;
    for (var i = 0; i < Math.Min(a.Length, b.Length); i++)
        if (a[i] != b[i])
            d++;
    Console.WriteLine($"  {label}: DIFFERS a={a.Length}B b={b.Length}B diffs={d}");
}

// Load Slot0013
Console.WriteLine("=== Loading Slot0013 (test10) ===");
var gsi170path = Directory.GetFiles(saveDir, "Slot0013test10.gsi").First();
var src = SaveGameLoader.Load(
    gsi170path,
    Path.Combine(saveDir, "Slot0013.tfai"),
    Path.Combine(saveDir, "Slot0013.tfaf")
);
var tfaf170 = File.ReadAllBytes(Path.Combine(saveDir, "Slot0013.tfaf"));
Console.WriteLine($"  {src.Info.LeaderName} lvl={src.Info.LeaderLevel}  TFAF={tfaf170.Length}B");
Console.WriteLine($"  MobileMdys={src.MobileMdys.Count}  ParseErrors={src.ParseErrors.Count}");
foreach (var (ek, ev) in src.ParseErrors.Where(e => e.Key.Contains("mobile.mdy", StringComparison.OrdinalIgnoreCase)))
    Console.WriteLine($"  ParseError mobile.mdy [{ek}]: {ev[..Math.Min(150, ev.Length)]}");

// Find all PC instances
var allPcs = new List<(string path, MobileMdFile md, int idx, MobileMdRecord rec, MobData data)>();
foreach (var (path, md) in src.MobileMds)
    for (var i = 0; i < md.Records.Count; i++)
        if (md.Records[i].Data?.Header.GameObjectType == ObjectType.Pc)
            allPcs.Add((path, md, i, md.Records[i], md.Records[i].Data!));
Console.WriteLine($"  PC instances in mobile.md: {allPcs.Count}");

// Per-map PC breakdown: decoded vs null
{
    var pcByMap = new Dictionary<string, (int decoded, int nullPc)>();
    foreach (var (path, md) in src.MobileMds)
    foreach (var rec in md.Records)
    {
        bool isTypePc =
            rec.Data?.Header.GameObjectType == ObjectType.Pc
            || (rec.Data is null && rec.RawMobBytes.Length > 28 && rec.RawMobBytes[24] == 15);
        if (!isTypePc)
            continue;
        pcByMap.TryGetValue(path, out var cur);
        pcByMap[path] = rec.Data is not null ? (cur.decoded + 1, cur.nullPc) : (cur.decoded, cur.nullPc + 1);
    }
    foreach (var (mapPath, (decoded, nullPc)) in pcByMap.OrderBy(x => x.Key))
        Console.WriteLine(
            $"  map={System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(mapPath))}  decoded={decoded}  null={nullPc}"
        );
}

// Print current gold from first PC instance
if (allPcs.Count > 0)
{
    var firstPcRec = allPcs[0].rec;
    var firstPcData = allPcs[0].data;
    var goldProp = firstPcData.Properties.FirstOrDefault(p => p.Field == ObjectField.ObjFCritterGold);
    if (goldProp is null)
        Console.WriteLine("  Current gold: PROPERTY ABSENT");
    else
        Console.WriteLine($"  Current gold: {goldProp.GetInt32()} (rawBytes={goldProp.RawBytes.Length}B)");

    // Bitmap bit count vs decoded prop count
    var bitmapSetBits = firstPcData.Header.Bitmap.Sum(b => int.PopCount(b));
    Console.WriteLine($"  Bitmap set bits: {bitmapSetBits}  Decoded props: {firstPcData.Properties.Count}");
    Console.WriteLine($"  TailBytes: {(firstPcRec.TailBytes?.Length.ToString() ?? "null")}B");
    Console.WriteLine($"  ParseNote: {firstPcRec.ParseNote ?? "(none)"}");

    // Print all property fields on first PC
    Console.WriteLine(
        $"  PC props ({firstPcData.Properties.Count}): "
            + string.Join(
                ", ",
                firstPcData.Properties.Select(p => $"{p.Field}(idx={(int)p.Field},note={p.ParseNote ?? ""})")
            )
    );
    // Hex dump of TailBytes
    if (firstPcRec.TailBytes is { } tb)
        Console.WriteLine($"  TailBytes hex: {Convert.ToHexString(tb)}");
    // Bitmap bytes
    Console.WriteLine($"  Bitmap hex: {Convert.ToHexString(firstPcData.Header.Bitmap)}");
    Console.WriteLine($"  PropCollItems header: {firstPcData.Header.PropCollectionItems}");
    Console.WriteLine(
        $"  ProtoId.OidType: {firstPcData.Header.ProtoId.OidType}  IsProto={firstPcData.Header.IsPrototype}"
    );
    Console.WriteLine(
        $"  RawMobBytes[0..5] (protoId start): {Convert.ToHexString(firstPcRec.RawMobBytes[..Math.Min(6, firstPcRec.RawMobBytes.Length)])}"
    );
    // Full rawMobBytes
    Console.WriteLine($"  RawMobBytes length: {firstPcRec.RawMobBytes.Length}B");
    // Annotated hex dump of first 55 bytes
    if (firstPcRec.RawMobBytes.Length >= 55)
    {
        var r = firstPcRec.RawMobBytes;
        Console.WriteLine($"    [0..23]  protoId:       {Convert.ToHexString(r[..24])}");
        Console.WriteLine($"    [24..27] objectType:    {Convert.ToHexString(r[24..28])}");
        Console.WriteLine($"    [28..29] propCollItems: {Convert.ToHexString(r[28..30])}");
        Console.WriteLine($"    [30..49] bitmap:        {Convert.ToHexString(r[30..50])}");
        Console.WriteLine($"    [50..54] first5B props: {Convert.ToHexString(r[50..55])}");
    }

    // Check all 147 decoded PC records for gold (bit 78) or other interesting fields
    Console.WriteLine($"\n  Checking all {allPcs.Count} decoded PC records for gold (bit78) and inventory (bit84):");
    int pcWithGold = 0,
        pcWithInv = 0,
        pcWithStats = 0;
    foreach (var (_, _, _, _, pdata) in allPcs)
    {
        var bmp = pdata.Header.Bitmap;
        bool hasBit78 = bmp.Length > 9 && (bmp[9] & (1 << (78 - 72))) != 0;
        bool hasBit84 = bmp.Length > 10 && (bmp[10] & (1 << (84 - 80))) != 0;
        bool hasBit66 = bmp.Length > 8 && (bmp[8] & (1 << (66 - 64))) != 0; // CritterStatBaseIdx
        if (hasBit78)
            pcWithGold++;
        if (hasBit84)
            pcWithInv++;
        if (hasBit66)
            pcWithStats++;
    }
    Console.WriteLine(
        $"    HasGold(bit78): {pcWithGold}  HasInventory(bit84): {pcWithInv}  HasStats(bit66): {pcWithStats}"
    );

    // Decoded property raw bytes for current map's first PC
    Console.WriteLine($"\n  Decoded property raw values for PC[0] on current map:");
    foreach (var prop in firstPcData.Properties)
    {
        var rawHex = Convert.ToHexString(prop.RawBytes);
        string valStr = prop.ParseNote is not null ? $"<FAILED>" : rawHex;
        Console.WriteLine($"    Field={prop.Field}(bit={(int)prop.Field}) raw={valStr}");
    }
}

// Scan null PC records (compact parse failed) — may hold full player state
Console.WriteLine("\n=== Null PC records (parse failed, rawMobBytes dump) ===");
var nullPcRecs = new List<(string path, MobileMdRecord rec)>();
foreach (var (path, md) in src.MobileMds)
foreach (var rec in md.Records)
    if (rec.Data is null && rec.RawMobBytes.Length > 28 && rec.RawMobBytes[24] == 15)
        nullPcRecs.Add((path, rec));
Console.WriteLine($"  Total null Pc records: {nullPcRecs.Count}");

// Show first null PC from current map
var firstNullPc =
    nullPcRecs.FirstOrDefault(x => x.path.Contains("Arcanum1-024-fixed")).rec
    ?? (nullPcRecs.Count > 0 ? nullPcRecs[0].rec : null);
if (firstNullPc is not null)
{
    Console.WriteLine($"  ParseNote: {firstNullPc.ParseNote}");
    Console.WriteLine($"  RawMobBytes length: {firstNullPc.RawMobBytes.Length}B");
    Console.WriteLine(
        $"  RawMobBytes[0..80]: {Convert.ToHexString(firstNullPc.RawMobBytes[..Math.Min(80, firstNullPc.RawMobBytes.Length)])}"
    );
    if (firstNullPc.RawMobBytes.Length >= 50)
    {
        var nullBmp = firstNullPc.RawMobBytes.AsSpan(30, 20).ToArray();
        Console.WriteLine($"  BitmapAt[30..49]: {Convert.ToHexString(nullBmp)}");
        int nullBits = nullBmp.Sum(b => int.PopCount(b));
        // bit 78: byte 9 (78-64=14/8=1... wait: 78 within bitmap: byte=78/8=9, bit=78%8=6
        // bitmapByte[9] bit6
        bool bit78 = (nullBmp[9] & (1 << 6)) != 0;
        bool bit84 = (nullBmp[10] & (1 << 4)) != 0; // 84/8=10, 84%8=4
        bool bit66 = (nullBmp[2] & (1 << 2)) != 0; // 66/8=8... wait bitmap starts at rawMobBytes[30] → bitmap[0]=rawMobBytes[30], bitmap[8]=rawMobBytes[38]. bit66: 66/8=8(within full object bitmap but within 20B bitmap starting from 0-based: bit66 → bitmap byte index 66/8=8, bit position 66%8=2 → nullBmp[8] bit2
        bool bit66v2 = (nullBmp[8] & (1 << 2)) != 0; // corrected
        Console.WriteLine(
            $"  Bitmap set bits: {nullBits}  bit78(gold)={bit78}  bit84(inv)={bit84}  bit66(stats)={bit66v2}"
        );
    }
}

// Find PC standalone .mob files too
var pcMobFiles = src.Mobiles.Where(kvp => kvp.Value.Header.GameObjectType == ObjectType.Pc).ToList();
Console.WriteLine($"  PC .mob files: {pcMobFiles.Count}");
foreach (var (path, mob) in pcMobFiles)
{
    var g = mob.Properties.FirstOrDefault(p => p.Field == ObjectField.ObjFCritterGold);
    var goldStr = g is null ? "absent" : g.GetInt32().ToString();
    Console.WriteLine($"    {path}  gold={goldStr}");
}

if (allPcs.Count == 0 && pcMobFiles.Count == 0)
{
    Console.WriteLine("ERROR: No PC found");
    return;
}

// Find ObjectType.Gold records in ALL mobile.md files AND print all type counts
Console.WriteLine("\n=== Scanning ALL mobile.md record types ===");
var typeCounts = new Dictionary<int, int>();
var goldRecords = new List<(string path, MobileMdRecord rec)>();
foreach (var (path, md) in src.MobileMds)
{
    foreach (var rec in md.Records)
    {
        var typeVal = rec.Data is not null ? (int)rec.Data.Header.GameObjectType : -1;
        typeCounts.TryGetValue(typeVal, out var cnt);
        typeCounts[typeVal] = cnt + 1;
        if (rec.Data?.Header.GameObjectType == ObjectType.Gold)
            goldRecords.Add((path, rec));
    }
}
foreach (var (typeVal, count) in typeCounts.OrderBy(x => x.Key))
{
    var typeName = typeVal >= 0 ? ((ObjectType)typeVal).ToString() : "null";
    Console.WriteLine($"  type={typeVal} ({typeName}): {count} records");
}
Console.WriteLine($"  ObjectType.Gold records: {goldRecords.Count}");

// Check null records: sample rawMobBytes[24] to test compact hypothesis
Console.WriteLine("\n=== Null record rawMobBytes[24] distribution ===");
var nullByte24 = new Dictionary<int, int>();
foreach (var (path, md) in src.MobileMds)
foreach (var rec in md.Records)
    if (rec.Data is null && rec.RawMobBytes.Length > 28)
    {
        var b = rec.RawMobBytes[24];
        nullByte24.TryGetValue(b, out var n);
        nullByte24[b] = n + 1;
    }
foreach (var (b, cnt) in nullByte24.OrderBy(x => x.Key).Take(20))
{
    var typeName = b <= 17 ? ((ObjectType)b).ToString() : "?";
    Console.WriteLine($"  byte24={b} ({typeName}): {cnt}  (matches compact type?)");
}

Dictionary<string, MobileMdFile> BuildUpdated(Func<MobData, MobData> fn)
{
    var result = new Dictionary<string, MobileMdFile>(StringComparer.OrdinalIgnoreCase);
    foreach (var path in allPcs.Select(x => x.path).Distinct())
    {
        var (_, md, _, _, _) = allPcs.First(x => x.path == path);
        var recs = new List<MobileMdRecord>(md.Records);
        foreach (var (p, _, i, rec, existingPc) in allPcs.Where(x => x.path == path))
        {
            recs[i] = new MobileMdRecord
            {
                MapObjectId = rec.MapObjectId,
                Version = rec.Version,
                RawMobBytes = rec.RawMobBytes,
                Data = fn(existingPc),
                TailBytes = rec.TailBytes,
                IsCompact = rec.IsCompact,
            };
        }
        result[path] = new MobileMdFile { Records = recs };
    }
    return result;
}

// Determine which test to run from arg (default=0)
// 0=roundtrip, 1=gold, 2=gold+stats, 3=rawbytes-field80-to-9999
var testMode = args.Length > 0 ? int.Parse(args[0]) : 0;
Console.WriteLine($"  testMode={testMode}  (0=roundtrip 1=gold 2=gold+stats 3=field80-raw-patch)");

var gsiOut = Path.Combine(saveDir, "Slot0171ARCNET_TEST.gsi");
var tfaiOut = Path.Combine(saveDir, "Slot0171.tfai");
var tfafOut = Path.Combine(saveDir, "Slot0171.tfaf");

if (testMode == 0)
{
    Console.WriteLine("\n=== Slot0171: raw round-trip (byte-identical check) ===");
    SaveGameWriter.Save(src, gsiOut, tfaiOut, tfafOut);
    Cmp("TFAF vs Slot0013", File.ReadAllBytes(tfafOut), tfaf170);
}
else if (testMode == 1)
{
    Console.WriteLine("\n=== Slot0171: gold=99999 ===");
    var updatedMobs =
        pcMobFiles.Count > 0
            ? pcMobFiles.ToDictionary(
                kvp => kvp.Key,
                kvp => new CharacterBuilder(kvp.Value).WithGold(99999).Build(),
                StringComparer.OrdinalIgnoreCase
            )
            : null;
    SaveGameWriter.Save(
        src,
        gsiOut,
        tfaiOut,
        tfafOut,
        new SaveGameUpdates
        {
            UpdatedMobiles = updatedMobs,
            UpdatedMobileMds = BuildUpdated(pc => new CharacterBuilder(pc).WithGold(99999).Build()),
        }
    );
    Console.WriteLine(
        $"  TFAF={new FileInfo(tfafOut).Length}B  delta={new FileInfo(tfafOut).Length - tfaf170.Length}B"
    );
    // Verify: re-load Slot0171 and confirm gold is present and correct.
    var verify171 = SaveGameLoader.Load(gsiOut, tfaiOut, tfafOut);
    int goldPcCount = 0;
    int firstGoldValue = -1;
    foreach (var (vPath, vMd) in verify171.MobileMds)
    foreach (var vRec in vMd.Records)
    {
        var gp = vRec.Data?.Properties.FirstOrDefault(p => p.Field == ObjectField.ObjFCritterGold);
        if (gp is null)
            continue;
        goldPcCount++;
        if (firstGoldValue < 0)
            firstGoldValue = gp.GetInt32();
    }
    Console.WriteLine($"  VERIFY: PC records with gold in Slot0171: {goldPcCount}  first value={firstGoldValue}");
}
else if (testMode == 2)
{
    Console.WriteLine("\n=== Slot0171: gold=99999 + stats=20 ===");
    SaveGameWriter.Save(
        src,
        gsiOut,
        tfaiOut,
        tfafOut,
        new SaveGameUpdates
        {
            UpdatedMobileMds = BuildUpdated(pc =>
                new CharacterBuilder(pc).WithGold(99999).WithBaseStats([20, 20, 20, 20, 20, 20]).Build()
            ),
        }
    );
    Console.WriteLine(
        $"  TFAF={new FileInfo(tfafOut).Length}B  delta={new FileInfo(tfafOut).Length - tfaf170.Length}B"
    );
}
else if (testMode == 3)
{
    // Directly patch field 80's raw 4 bytes in all PC compact records without going through CharacterBuilder.
    // Field 80 (ObjFItemSpell4 / CritterBullets) is already in the bitmap (bit 80 set).
    // Its data occupies exactly 4 bytes somewhere in the encoded property stream.
    // If changing these 4 bytes shows in-game (bullets/ammo count changes), write path is confirmed.
    Console.WriteLine("\n=== Slot0171: raw-patch field 80 to 9999 ===");
    Console.WriteLine(
        $"  Field 80 current value: {allPcs[0].data.Properties.First(p => p.Field == (ObjectField)80).GetInt32()}"
    );
    var patched = BuildUpdated(pc =>
    {
        var newProps = pc
            .Properties.Select(p =>
                p.Field == (ObjectField)80 ? ObjectPropertyFactory.ForInt32((ObjectField)80, 9999) : p
            )
            .ToList();
        return new MobData { Header = pc.Header, Properties = newProps.AsReadOnly() };
    });
    SaveGameWriter.Save(src, gsiOut, tfaiOut, tfafOut, new SaveGameUpdates { UpdatedMobileMds = patched });
    Cmp("TFAF vs Slot0013", File.ReadAllBytes(tfafOut), tfaf170);
    Console.WriteLine($"  delta={new FileInfo(tfafOut).Length - tfaf170.Length}B");
}
else if (testMode == 4)
{
    // Mode 4: raw-bytes gold patch. Directly insert gold=99999 into rawMobBytes by:
    // 1. Setting bit 78 in the bitmap (rawMobBytes[39] |= 0x40)
    // 2. Inserting 4B LE-int32 value at the property offset for bit 78 (after bit30+bit41)
    // This preserves ALL other data (gap bits, TailBytes, bitmap bits beyond 88) exactly as-is.
    // Properties decoded for bit30=4B, bit41=17B → bit78 goes at rawMobBytes[50+21]=71
    Console.WriteLine("\n=== Slot0171: raw-bytes gold=99999 insert at rawMobBytes offset 71 ===");
    var goldPatchedMds = new Dictionary<string, MobileMdFile>(StringComparer.OrdinalIgnoreCase);
    foreach (var kv in src.MobileMds)
        goldPatchedMds[kv.Key] = kv.Value; // start with unmodified copies

    int patchedCount = 0;
    var rawPatchedMds = new Dictionary<string, MobileMdFile>(StringComparer.OrdinalIgnoreCase);
    foreach (var (mdPath, md) in src.MobileMds)
    {
        var recs = new List<MobileMdRecord>(md.Records);
        bool modified = false;
        for (int ri = 0; ri < recs.Count; ri++)
        {
            var r = recs[ri];
            // Only patch compact Pc records that match our known 102B structure
            if (!r.IsCompact || r.Data?.Header.GameObjectType != ObjectType.Pc)
                continue;
            if (r.RawMobBytes.Length < 75)
                continue; // safety: need at least 75B

            // Check bit 78 not already set
            var bmpByte9 = r.RawMobBytes[39]; // bitmap[9] = rawMobBytes[30+9]
            if ((bmpByte9 & 0x40) != 0)
            {
                continue;
            } // already has gold

            // Build new rawMobBytes: insert 4B at offset 71
            var oldRaw = r.RawMobBytes;
            var newRaw = new byte[oldRaw.Length + 4];
            oldRaw.AsSpan(0, 71).CopyTo(newRaw); // copy bytes 0..70
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(newRaw.AsSpan(71), 99999); // gold
            oldRaw.AsSpan(71).CopyTo(newRaw.AsSpan(75)); // copy bytes 71..end
            // Set bit 78 in bitmap (newRaw[39])
            newRaw[39] |= 0x40;

            recs[ri] = new MobileMdRecord
            {
                MapObjectId = r.MapObjectId,
                Version = r.Version,
                RawMobBytes = newRaw,
                Data = null, // write verbatim (raw bytes)
                TailBytes = null,
                IsCompact = false, // use rawMobBytes path in writer
                ParseNote = null,
            };
            patchedCount++;
            modified = true;
        }
        rawPatchedMds[mdPath] = modified ? new MobileMdFile { Records = recs } : md;
    }
    Console.WriteLine($"  Patched {patchedCount} PC records");
    SaveGameWriter.Save(src, gsiOut, tfaiOut, tfafOut, new SaveGameUpdates { UpdatedMobileMds = rawPatchedMds });
    Cmp("TFAF vs Slot0013", File.ReadAllBytes(tfafOut), tfaf170);
    Console.WriteLine($"  delta={new FileInfo(tfafOut).Length - tfaf170.Length}B");
}
else if (testMode == 5)
{
    // Mode 5: like mode 4 (raw-bytes gold insert) but ALSO sets propCollItems from 0 to 19.
    // Tests the hypothesis that propCollItems=0 causes the game to read 0 properties (all defaults).
    // If gold shows after loading this save, propCollItems=0 was the root cause.
    Console.WriteLine("\n=== Slot0171: raw-bytes gold=99999 + propCollItems=19 ===");
    var rawPatchedMds5 = new Dictionary<string, MobileMdFile>(StringComparer.OrdinalIgnoreCase);
    foreach (var kv in src.MobileMds)
        rawPatchedMds5[kv.Key] = kv.Value;

    int patchedCount5 = 0;
    foreach (var (mdPath, md) in src.MobileMds)
    {
        var recs = new List<MobileMdRecord>(md.Records);
        bool modified = false;
        for (int ri = 0; ri < recs.Count; ri++)
        {
            var r = recs[ri];
            if (!r.IsCompact || r.Data?.Header.GameObjectType != ObjectType.Pc)
                continue;
            if (r.RawMobBytes.Length < 75)
                continue;

            // Count bits in the original bitmap to determine new propCollItems.
            var origBitmap = r.RawMobBytes[30..50]; // bitmap at rawMobBytes[30..49]
            int origBitCount = origBitmap.Sum(b => int.PopCount(b));
            // After inserting gold (bit78), bitmap gains 1 bit.
            int newBitCount = origBitCount + 1;

            // Check bit 78 not already set.
            var bmpByte9 = r.RawMobBytes[39]; // bitmap[9] = bits 72-79
            if ((bmpByte9 & 0x40) != 0)
                continue;

            // Build new rawMobBytes: insert 4B gold at offset 71, set bit78, set propCollItems.
            var oldRaw = r.RawMobBytes;
            var newRaw = new byte[oldRaw.Length + 4];
            oldRaw.AsSpan(0, 71).CopyTo(newRaw);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(newRaw.AsSpan(71), 99999);
            oldRaw.AsSpan(71).CopyTo(newRaw.AsSpan(75));

            // Set bit 78 in bitmap.
            newRaw[39] |= 0x40;

            // Set propCollItems at rawMobBytes[28..29] to newBitCount.
            System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(newRaw.AsSpan(28), (short)newBitCount);

            recs[ri] = new MobileMdRecord
            {
                MapObjectId = r.MapObjectId,
                Version = r.Version,
                RawMobBytes = newRaw,
                Data = null,
                TailBytes = null,
                IsCompact = false,
                ParseNote = null,
            };
            patchedCount5++;
            modified = true;
        }
        rawPatchedMds5[mdPath] = modified ? new MobileMdFile { Records = recs } : md;
    }
    Console.WriteLine($"  Patched {patchedCount5} PC records (gold + propCollItems)");
    SaveGameWriter.Save(src, gsiOut, tfaiOut, tfafOut, new SaveGameUpdates { UpdatedMobileMds = rawPatchedMds5 });
    Cmp("TFAF vs Slot0013", File.ReadAllBytes(tfafOut), tfaf170);
    Console.WriteLine($"  delta={new FileInfo(tfafOut).Length - tfaf170.Length}B");
}
else if (testMode == 6)
{
    // Mode 6: raw-patch data.sav — search every inner file for 41074 (0x52 0xA0 0x00 0x00) and
    // replace each occurrence with 99999 (0x9F 0x86 0x01 0x00).  No structure awareness needed;
    // the file sizes stay identical so the TFAI index is reused as-is.
    Console.WriteLine("\n=== Slot0171: raw-patch gold 41074→99999 across all inner files ===");
    var needle = new byte[] { 0x52, 0xA0, 0x00, 0x00 };
    var patch = new byte[] { 0x9F, 0x86, 0x01, 0x00 };
    var patchedFiles = new Dictionary<string, byte[]>(src.Files, StringComparer.OrdinalIgnoreCase);
    int totalPatches = 0;
    foreach (var key in patchedFiles.Keys.ToList())
    {
        var src6 = patchedFiles[key];
        byte[]? modified = null;
        for (var si = 0; si <= src6.Length - 4; si++)
        {
            if (
                src6[si] == needle[0]
                && src6[si + 1] == needle[1]
                && src6[si + 2] == needle[2]
                && src6[si + 3] == needle[3]
            )
            {
                modified ??= (byte[])src6.Clone();
                patch.CopyTo(modified.AsSpan(si));
                Console.WriteLine($"  Patched {key}  offset={si}");
                totalPatches++;
            }
        }
        if (modified is not null)
            patchedFiles[key] = modified;
    }
    Console.WriteLine($"  Total patches: {totalPatches}");
    // Sizes unchanged → reuse original index directly.
    File.WriteAllBytes(gsiOut, SaveInfoFormat.WriteToArray(src.Info));
    File.WriteAllBytes(tfaiOut, SaveIndexFormat.WriteToArray(src.Index));
    File.WriteAllBytes(tfafOut, TfafFormat.Pack(src.Index, patchedFiles));
    Cmp("TFAF vs Slot0013", File.ReadAllBytes(tfafOut), tfaf170);
    Console.WriteLine($"  delta={new FileInfo(tfafOut).Length - tfaf170.Length}B");
}

Console.WriteLine($"  Written: {gsiOut}");
Console.WriteLine("  Run with arg 0=roundtrip  1=gold  2=gold+stats  3=field80-raw-patch");

// Dump all inner file paths with sizes
Console.WriteLine("\n=== Inner files in TFAF ===");
foreach (var (path, bytes) in src.Files.OrderBy(x => x.Key))
    Console.WriteLine($"  {bytes.Length, 8:N0}B  {path}");

// Hex dump data.sav header
if (src.Files.TryGetValue("data.sav", out var dataSav))
{
    Console.WriteLine($"\n=== data.sav first 64 bytes + last 8 ===");
    Console.WriteLine($"  head: {Convert.ToHexString(dataSav[..Math.Min(64, dataSav.Length)])}");
    Console.WriteLine($"  tail: {Convert.ToHexString(dataSav[^8..])}");
}

// Parse all mobile.des files (object name overrides)
Console.WriteLine("\n=== mobile.des files and content (first found with current PC map) ===");
var currentMapMd = allPcs.Count > 0 ? allPcs[0].path : null; // e.g. "maps/Arcanum1-024-fixed/mobile.md"
var currentMapBase = currentMapMd is not null ? Path.GetDirectoryName(currentMapMd)?.Replace('\\', '/') : null;
Console.WriteLine($"  Current map base: {currentMapBase}");
var desKey = currentMapBase is not null ? currentMapBase + "/mobile.des" : null;
if (desKey is not null && src.Files.TryGetValue(desKey, out var desBytes))
{
    Console.WriteLine($"  mobile.des size: {desBytes.Length}B");
    Console.WriteLine($"  mobile.des first 128B: {Convert.ToHexString(desBytes[..Math.Min(128, desBytes.Length)])}");
}

// Scan mobile.mdy for current map — stores dynamically spawned objects (items carried, etc.)
var mdyKey = currentMapBase is not null ? currentMapBase + "/mobile.mdy" : null;
Console.WriteLine($"\n=== mobile.mdy for current map ===");
if (mdyKey is not null && src.MobileMdys.TryGetValue(mdyKey, out var curMdy))
{
    Console.WriteLine($"  Records: {curMdy.Records.Count}");
    var mdyTypeCounts = new Dictionary<string, int>();
    foreach (var mob in curMdy.Records)
    {
        var key = mob.IsMob ? mob.Mob!.Header.GameObjectType.ToString() : "V2Character";
        mdyTypeCounts.TryGetValue(key, out var c);
        mdyTypeCounts[key] = c + 1;
    }
    foreach (var (t, c) in mdyTypeCounts.OrderBy(x => x.Key))
        Console.WriteLine($"    type={t}: {c}");

    // Gold-type items
    var goldItems = curMdy
        .Records.Where(m => m.IsMob && m.Mob!.Header.GameObjectType == ObjectType.Gold)
        .Select(m => m.Mob!)
        .ToList();
    Console.WriteLine($"  Gold items: {goldItems.Count}");
    foreach (var g in goldItems.Take(5))
    {
        var qty = g.Properties.FirstOrDefault(p => (int)p.Field == 97);
        Console.WriteLine(
            $"    proto={g.Header.ProtoId}  qty={qty?.GetInt32() ?? -1}  oidType={g.Header.ProtoId.OidType}"
        );
    }

    // Items with ItemParent (bit 65) set — owned by someone
    var ownedItems = curMdy
        .Records.Where(m =>
            m.IsMob && m.Mob!.Header.Bitmap.Length > 8 && (m.Mob!.Header.Bitmap[8] & (1 << (65 - 64))) != 0
        )
        .Select(m => m.Mob!)
        .ToList();
    Console.WriteLine($"  Items with ItemParent: {ownedItems.Count}");
    foreach (var item in ownedItems.Take(10))
    {
        var parentProp = item.Properties.FirstOrDefault(p => (int)p.Field == 65);
        Console.WriteLine(
            $"    type={item.Header.GameObjectType}  proto={item.Header.ProtoId}  parentRaw={Convert.ToHexString(parentProp?.RawBytes ?? [])}"
        );
    }
}
else
    Console.WriteLine($"  Not found: {mdyKey}");

// Raw inspection of mobile.mdy — find what stops the parser
if (mdyKey is not null && src.Files.TryGetValue(mdyKey, out var mdyRaw))
{
    Console.WriteLine($"\n=== mobile.mdy raw inspection ===");
    Console.WriteLine($"  File size: {mdyRaw.Length}B");
    // Show first 64 bytes
    Console.WriteLine($"  First 64B: {Convert.ToHexString(mdyRaw[..Math.Min(64, mdyRaw.Length)])}");
    // Parse first record manually to find where it ends, then show next 16 bytes
    try
    {
        var rawReader = new ArcNET.Core.SpanReader(mdyRaw);
        var firstMob = ArcNET.Formats.MobFormat.Parse(ref rawReader);
        var posAfterFirst = mdyRaw.Length - rawReader.Remaining;
        Console.WriteLine($"  First record size: {posAfterFirst}B  version=0x{firstMob.Header.Version:X2}");
        if (rawReader.Remaining >= 16)
        {
            var nextBytes = mdyRaw.AsSpan(posAfterFirst, Math.Min(16, rawReader.Remaining)).ToArray();
            Console.WriteLine($"  Bytes after first record: {Convert.ToHexString(nextBytes)}");
            var nextVer = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(nextBytes);
            Console.WriteLine($"  Next version dword: 0x{nextVer:X8}");
        }

        // Walk records one by one to find where parsing stops
        var walkReader = new ArcNET.Core.SpanReader(mdyRaw);
        int walkPos = 0,
            walkRecCount = 0;
        while (walkReader.Remaining >= 4)
        {
            if (unchecked((uint)walkReader.PeekInt32At(0)) == 0xFFFFFFFF)
            {
                walkReader.Skip(4);
                walkPos += 4;
                continue;
            }
            var v2span = walkReader.RemainingSpan;
            // Check v2 magic: 02 00 00 00 0F 00 00 00 00 00 00 00
            bool isV2Magic =
                v2span.Length >= 12
                && v2span[0] == 0x02
                && v2span[1] == 0
                && v2span[2] == 0
                && v2span[3] == 0
                && v2span[4] == 0x0F
                && v2span[5] == 0
                && v2span[6] == 0
                && v2span[7] == 0
                && v2span[8] == 0
                && v2span[9] == 0
                && v2span[10] == 0
                && v2span[11] == 0;
            if (isV2Magic)
            {
                Console.WriteLine($"  Walk: v2 magic at pos=0x{walkPos:X4}  records={walkRecCount}");
                break;
            }
            var verDword = walkReader.PeekInt32At(0);
            if (verDword != 0x08 && verDword != 0x77)
            {
                Console.WriteLine(
                    $"  Walk: BREAK at pos=0x{walkPos:X4}  ver=0x{verDword:X8}  records={walkRecCount}  nextBytes={Convert.ToHexString(v2span[..Math.Min(16, v2span.Length)].ToArray())}"
                );
                break;
            }
            try
            {
                ArcNET.Formats.MobFormat.Parse(ref walkReader);
                walkRecCount++;
                walkPos = mdyRaw.Length - walkReader.Remaining;
            }
            catch (Exception)
            {
                Console.WriteLine($"  Walk: parse fail at pos=0x{walkPos:X4}");
                break;
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Parse error: {ex.Message}");
    }
}

// Also search all mobile.md files across all maps for inventory-related CritterInventoryListIdx
// Bit 84 (CritterInventoryListIdx) — look for any Pc records that DO have it
Console.WriteLine("\n=== PC records with inventory bit (bit 84) set ===");
var invRecords = allPcs
    .Where(x => (x.data.Header.Bitmap.Length > 10 && (x.data.Header.Bitmap[10] & (1 << (84 - 80))) != 0))
    .ToList();
Console.WriteLine($"  Found: {invRecords.Count} PC records with inv bit");
if (invRecords.Count == 0)
    Console.WriteLine("  (inventory OID list not in any compact PC record)");

// Scan .dif files in current map — full MobData records for objects that differ from static world
Console.WriteLine("\n=== Scanning .dif files in current map for PC objects ===");
var difFiles = src
    .Files.Where(kvp =>
        kvp.Key.StartsWith(currentMapBase ?? "\0", StringComparison.OrdinalIgnoreCase)
        && kvp.Key.EndsWith(".dif", StringComparison.OrdinalIgnoreCase)
    )
    .OrderBy(kvp => kvp.Key)
    .ToList();
Console.WriteLine($"  .dif count: {difFiles.Count}");
int difPcCount = 0;
int difTotalObjects = 0;
foreach (var (difPath, difBytes) in difFiles)
{
    try
    {
        var reader = new ArcNET.Core.SpanReader(difBytes);
        // SEC-style: objects are sequential MobFormat records, count in last 4 bytes
        // Try reading object count from end, then parse that many MobFormat records
        if (difBytes.Length < 4)
            continue;
        var objCount = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
            difBytes.AsSpan(difBytes.Length - 4)
        );
        if (objCount <= 0 || objCount > 10000)
            continue; // sanity
        difTotalObjects += objCount;
        for (int oi = 0; oi < objCount; oi++)
        {
            try
            {
                var mob = ArcNET.Formats.MobFormat.Parse(ref reader);
                if (mob.Header.GameObjectType == ObjectType.Pc)
                {
                    difPcCount++;
                    var gold78 = mob.Properties.FirstOrDefault(p => p.Field == ObjectField.ObjFCritterGold);
                    var inv84 = mob.Properties.FirstOrDefault(p => p.Field == ObjectField.ObjFCritterInventoryListIdx);
                    Console.WriteLine(
                        $"    DIF PC: file={System.IO.Path.GetFileName(difPath)}  bits={mob.Header.Bitmap.Length * 8}  props={mob.Properties.Count}  gold={gold78?.GetInt32() ?? -1}  inv={inv84 != null}  bitmap={Convert.ToHexString(mob.Header.Bitmap)}"
                    );
                }
            }
            catch (Exception)
            {
                break;
            }
        }
    }
    catch (Exception) { }
}
Console.WriteLine($"  DIF total Pc found: {difPcCount}  (total objects across all difs: {difTotalObjects})");

// Also scan .dif files for inventory items (ItemParent = bit 65 set)
Console.WriteLine("\n=== Scanning ALL .dif files for items with ItemParent (bit65) or Gold type ===");
var allDifFiles = src.Files.Where(kvp => kvp.Key.EndsWith(".dif", StringComparison.OrdinalIgnoreCase)).ToList();
Console.WriteLine($"  Total .dif files: {allDifFiles.Count}");
int difItemsWithParent = 0,
    difGoldTotal = 0;
foreach (var (dp, db) in allDifFiles)
{
    if (db.Length < 4)
        continue;
    var objCnt = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(db.AsSpan(db.Length - 4));
    if (objCnt <= 0 || objCnt > 10000)
        continue;
    var dr = new ArcNET.Core.SpanReader(db);
    for (int oi = 0; oi < objCnt; oi++)
    {
        try
        {
            var mob = ArcNET.Formats.MobFormat.Parse(ref dr);
            bool hasParent = mob.Header.Bitmap.Length > 8 && (mob.Header.Bitmap[8] & (1 << 1)) != 0;
            bool isGold = mob.Header.GameObjectType == ObjectType.Gold;
            if (hasParent)
            {
                difItemsWithParent++;
                if (difItemsWithParent <= 5)
                    Console.WriteLine($"    ItemParent: type={mob.Header.GameObjectType}  proto={mob.Header.ProtoId}");
            }
            if (isGold)
                difGoldTotal++;
        }
        catch (Exception)
        {
            break;
        }
    }
}
Console.WriteLine($"  Items with ItemParent across all difs: {difItemsWithParent}  Gold objects: {difGoldTotal}");

// ── Search all inner files for gold=41074 in multiple encodings ──────────────────────────────────
Console.WriteLine("\n=== Search inner files for gold=41074 (multiple encodings) ===");

// Varint encoding of 41074: 41074 = 0xA052
//   7-bit groups LSB-first: bits 0-6 = 0x52 | 0x80 = 0xD2, bits 7-13 = 0x40 | 0x80 = 0xC0, bits 14+ = 0x02
byte[] goldVarint = [0xD2, 0xC0, 0x02]; // protobuf-style LEB128 varint for 41074
byte[] goldLeI32 = [0x52, 0xA0, 0x00, 0x00]; // LE int32
byte[] goldBeI32 = [0x00, 0x00, 0xA0, 0x52]; // BE int32

int goldSearchHits = 0;

static bool MatchAt(byte[] data, int idx, byte[] pattern)
{
    if (idx + pattern.Length > data.Length)
        return false;
    for (int k = 0; k < pattern.Length; k++)
        if (data[idx + k] != pattern[k])
            return false;
    return true;
}

static string ObjFName(int bit) =>
    bit switch
    {
        0 => "CurrentAid",
        1 => "Location",
        2 => "OffsetX",
        3 => "OffsetY",
        4 => "Shadow",
        5 => "OverlayFore",
        6 => "OverlayBack",
        7 => "Underlay",
        8 => "BlitFlags",
        9 => "BlitColor",
        10 => "BlitAlpha",
        11 => "BlitScale",
        12 => "LightFlags",
        13 => "LightAid",
        14 => "LightColor",
        15 => "OverlayLightFlags",
        16 => "OverlayLightAid",
        17 => "OverlayLightColor",
        18 => "Flags",
        19 => "SpellFlags",
        20 => "BlockingMask",
        21 => "Name",
        22 => "Description",
        23 => "Aid",
        24 => "DestroyedAid",
        25 => "Ac",
        26 => "HpPts",
        27 => "HpAdj",
        28 => "HpDamage",
        29 => "Material",
        30 => "ResistanceIdx",
        31 => "ScriptsIdx",
        32 => "SoundEffect",
        33 => "Category",
        34 => "Rotation",
        35 => "PadI64As1",
        36 => "SpeedRun",
        37 => "SpeedWalk",
        38 => "PadFloat1",
        39 => "Radius",
        40 => "Height",
        41 => "Conditions",
        42 => "ConditionArg0",
        43 => "PermanentMods",
        64 => "CritterFlags",
        65 => "CritterFlags2",
        66 => "CritterStatBaseIdx",
        67 => "CritterBasicSkillIdx",
        68 => "CritterTechSkillIdx",
        69 => "CritterSpellTechIdx",
        70 => "CritterFatiguePts",
        71 => "CritterFatigueAdj",
        72 => "CritterFatigueDamage",
        73 => "CritterCritHitChart",
        74 => "CritterEffectsIdx",
        75 => "CritterEffectCauseIdx",
        76 => "CritterFleeingFrom",
        77 => "CritterPortrait",
        78 => "CritterGold",
        79 => "CritterArrows",
        80 => "CritterBullets",
        81 => "CritterPowerCells",
        82 => "CritterFuel",
        83 => "CritterInventoryNum",
        84 => "CritterInventoryListIdx",
        85 => "CritterInventorySource",
        86 => "CritterDescriptionUnknown",
        87 => "CritterFollowerIdx",
        88 => "CritterTeleportDest",
        89 => "CritterTeleportMap",
        128 => "PcFlags",
        129 => "PcFlagsFate",
        130 => "PcReputationIdx",
        145 => "PcPlayerName",
        146 => "PcBankMoney",
        _ => $"bit{bit}",
    };

foreach (var (sfPath, sfBytes) in src.Files.OrderBy(x => x.Key))
{
    for (var si = 0; si <= sfBytes.Length - 3; si++)
    {
        string? enc = null;
        if (si + 3 < sfBytes.Length && MatchAt(sfBytes, si, goldLeI32))
            enc = "LE-int32";
        else if (si + 3 < sfBytes.Length && MatchAt(sfBytes, si, goldBeI32))
            enc = "BE-int32";
        else if (MatchAt(sfBytes, si, goldVarint))
            enc = "varint";

        if (enc is not null)
        {
            var ctxStart = Math.Max(0, si - 4);
            var ctxEnd = Math.Min(sfBytes.Length, si + 12);
            Console.WriteLine(
                $"  FOUND ({enc}): {sfPath}  offset={si}  ctx={Convert.ToHexString(sfBytes[ctxStart..ctxEnd])}"
            );
            goldSearchHits++;
        }
    }
}
if (goldSearchHits == 0)
    Console.WriteLine("  (41074 not found in any encoding across all inner files)");
else
    Console.WriteLine($"  Total hits: {goldSearchHits}");

// ── Dump data.sav in full hex for manual inspection ───────────────────────────────────────────────
if (src.Files.TryGetValue("data.sav", out var dataSavFull))
{
    Console.WriteLine($"\n=== data.sav full hex ({dataSavFull.Length}B) ===");
    const int rowSize = 32;
    for (int row = 0; row < dataSavFull.Length; row += rowSize)
    {
        var chunk = dataSavFull.AsSpan(row, Math.Min(rowSize, dataSavFull.Length - row));
        Console.WriteLine($"  {row:X4}: {Convert.ToHexString(chunk)}");
    }
}

// ── Dump data2.sav in full hex ────────────────────────────────────────────────────────────────────
if (src.Files.TryGetValue("data2.sav", out var data2SavFull))
{
    Console.WriteLine($"\n=== data2.sav full hex ({data2SavFull.Length}B) ===");
    for (int row = 0; row < data2SavFull.Length; row += 32)
    {
        var chunk = data2SavFull.AsSpan(row, Math.Min(32, data2SavFull.Length - row));
        Console.WriteLine($"  {row:X4}: {Convert.ToHexString(chunk)}");
    }
}

// ── Dump data.sav around known level=6 offset (0xA882) ─────────────────────────────────────────────────────
Console.WriteLine("\n=== data.sav dump around level-44 hits (0xA882 region, ±128B) ===");
if (src.Files.TryGetValue("data.sav", out var dsAroundLvl))
{
    // Dump [-128..+128] around each of the 4 known level=6 offsets
    foreach (int lvlOffset in new[] { 0x2E8B, 0x42EB, 0x453F, 0xA882 })
    {
        if (lvlOffset >= dsAroundLvl.Length)
            continue;
        int dumpStart = Math.Max(0, lvlOffset - 128);
        int dumpLen = Math.Min(256, dsAroundLvl.Length - dumpStart);
        Console.WriteLine(
            $"\n  -- Level-44 hit at 0x{lvlOffset:X4} (dump 0x{dumpStart:X4}..0x{dumpStart + dumpLen:X4}) --"
        );
        for (int row = dumpStart; row < dumpStart + dumpLen; row += 16)
        {
            var chunkLen = Math.Min(16, dumpStart + dumpLen - row);
            var mark = (row <= lvlOffset && lvlOffset < row + 16) ? " ←LVL44" : "";
            Console.WriteLine($"    {row:X4}: {Convert.ToHexString(dsAroundLvl.AsSpan(row, chunkLen))}{mark}");
        }
    }
}

// ── Dump mobile.mdy bytes at 0x74 in detail (new-format records) ──────────────────────────────────────────
Console.WriteLine("\n=== mobile.mdy: detailed dump starting at 0x74 (after FFs) ===");
var mainMdyKeyEarly = currentMapBase is not null ? currentMapBase + "/mobile.mdy" : null;
if (mainMdyKeyEarly is not null && src.Files.TryGetValue(mainMdyKeyEarly, out var mdyDumpBytes))
{
    const int mdyDumpStart = 0x74;
    int mdyDumpLen = Math.Min(256, mdyDumpBytes.Length - mdyDumpStart);
    for (int row = mdyDumpStart; row < mdyDumpStart + mdyDumpLen; row += 16)
    {
        var chunkLen = Math.Min(16, mdyDumpStart + mdyDumpLen - row);
        Console.WriteLine($"  {row:X4}: {Convert.ToHexString(mdyDumpBytes.AsSpan(row, chunkLen))}");
    }
    // Also find ALL occurrences of 0F000000 and dump ±16B around each
    Console.WriteLine("\n  All 0F000000 occurrences in mobile.mdy (first 10, with context ±24B):");
    int pcOccurrenceCount = 0;
    for (int si = 0; si + 4 <= mdyDumpBytes.Length && pcOccurrenceCount < 10; si++)
    {
        if (
            mdyDumpBytes[si] == 0x0F
            && mdyDumpBytes[si + 1] == 0x00
            && mdyDumpBytes[si + 2] == 0x00
            && mdyDumpBytes[si + 3] == 0x00
        )
        {
            int ctxStart = Math.Max(0, si - 24);
            int ctxEnd = Math.Min(mdyDumpBytes.Length, si + 28);
            Console.WriteLine($"    offset=0x{si:X5}  ctx={Convert.ToHexString(mdyDumpBytes[ctxStart..ctxEnd])}");
            pcOccurrenceCount++;
        }
    }

    // Scan mobile.mdy for the 02000000 0F000000 records and print record boundaries + size
    Console.WriteLine("\n  Scanning mobile.mdy for 02000000-0F000000 PC records (4B-aligned):");
    var pcRecordOffsets = new List<int>();
    for (int si = 0; si + 8 <= mdyDumpBytes.Length; si += 4)
    {
        var dw0 = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(mdyDumpBytes.AsSpan(si, 4));
        var dw1 = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(mdyDumpBytes.AsSpan(si + 4, 4));
        if (dw0 == 0x00000002 && dw1 == 0x0000000F)
            pcRecordOffsets.Add(si);
    }
    Console.WriteLine($"    Total 02000000+0F000000 hits: {pcRecordOffsets.Count}");
    for (int ri = 0; ri < Math.Min(pcRecordOffsets.Count, 20); ri++)
    {
        int recOff = pcRecordOffsets[ri];
        int nextOff = ri + 1 < pcRecordOffsets.Count ? pcRecordOffsets[ri + 1] : mdyDumpBytes.Length;
        int recSpan = nextOff - recOff;
        // Dump first 64B of each record
        int recDumpLen = Math.Min(64, mdyDumpBytes.Length - recOff);
        Console.WriteLine(
            $"    rec[{ri}] @ 0x{recOff:X5}  span={recSpan}B  first64={Convert.ToHexString(mdyDumpBytes.AsSpan(recOff, recDumpLen))}"
        );
    }

    // For the FIRST 02000000+0F000000 record: dump ALL 568B (or until next)
    // searching for level=6 (0x06000000), gold, stats, etc.
    if (pcRecordOffsets.Count > 0)
    {
        int firstRecOff = pcRecordOffsets[0];
        int firstRecEnd2 = pcRecordOffsets.Count > 1 ? pcRecordOffsets[1] : mdyDumpBytes.Length;
        int firstRecBytes = firstRecEnd2 - firstRecOff;
        Console.WriteLine($"\n  First PC record dump (offset=0x{firstRecOff:X5} len={firstRecBytes}B):");
        for (int row = firstRecOff; row < firstRecEnd2 && row < firstRecOff + 800; row += 16)
        {
            var chunkLen = Math.Min(16, firstRecEnd2 - row);
            Console.WriteLine($"    {row:X5}: {Convert.ToHexString(mdyDumpBytes.AsSpan(row, chunkLen))}");
        }
    }
}

// ── Dump data.sav 0x8000..0xCFFF to find full PC record region ───────────────────────────────────────────────
Console.WriteLine("\n=== data.sav dump 0x8000..0xCFFF (PC record region) ===");
if (src.Files.TryGetValue("data.sav", out var dsRegion))
{
    // Look for the 0x2005 pattern (flags) which was found at both data.sav 0xA88E and mdy 0xC4
    byte[] sig2005 = [0x05, 0x20, 0x00, 0x00];
    Console.WriteLine("  All 0x00002005 pattern occurrences in data.sav (at 4B-aligned):");
    for (int si = 0; si + 4 <= dsRegion.Length; si += 4)
    {
        if (MatchAt(dsRegion, si, sig2005))
        {
            int ctxStart2 = Math.Max(0, si - 16);
            int ctxEnd2 = Math.Min(dsRegion.Length, si + 16);
            Console.WriteLine($"    offset=0x{si:X4}  ctx={Convert.ToHexString(dsRegion[ctxStart2..ctxEnd2])}");
        }
    }

    // Dump 0x9000..0xB000 (256B around where the PC GUID is)
    Console.WriteLine("\n  data.sav dump 0x9000..0xB000:");
    for (int row = 0x9000; row < Math.Min(0xB000, dsRegion.Length); row += 32)
    {
        var chunkLen = Math.Min(32, dsRegion.Length - row);
        Console.WriteLine($"    {row:X4}: {Convert.ToHexString(dsRegion.AsSpan(row, chunkLen))}");
    }
}
Console.WriteLine("\n=== Scan data.sav for mob-record version magic patterns ===");
if (src.Files.TryGetValue("data.sav", out var dsScanBytes))
{
    Console.WriteLine($"  data.sav size: {dsScanBytes.Length}B");
    // Find all positions where 0x08000000 or 0x77000000 appear at 4-byte aligned positions.
    var mobMagicHits = new List<int>();
    for (int si = 0; si + 3 < dsScanBytes.Length; si += 4)
    {
        var dw = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(dsScanBytes.AsSpan(si, 4));
        if (dw == 0x08000000 || dw == 0x77000000)
            mobMagicHits.Add(si);
    }
    Console.WriteLine($"  Version-magic hits (0x08 or 0x77 at 4B-aligned): {mobMagicHits.Count}");
    // Show first 10 hits with context
    foreach (var hitPos in mobMagicHits.Take(10))
    {
        var ctxEnd = Math.Min(dsScanBytes.Length, hitPos + 56);
        Console.WriteLine(
            $"    offset={hitPos:X4}: {Convert.ToHexString(dsScanBytes.AsSpan(hitPos, ctxEnd - hitPos))}"
        );
    }

    // Try parsing data.sav as a mob sequence from offset 0
    Console.WriteLine("\n  Attempting MobFormat.Parse from data.sav[0]:");
    try
    {
        var dsReader = new ArcNET.Core.SpanReader(dsScanBytes);
        var mob0 = ArcNET.Formats.MobFormat.Parse(ref dsReader);
        Console.WriteLine(
            $"    Parsed OK: type={mob0.Header.GameObjectType}  props={mob0.Properties.Count}  remaining={dsReader.Remaining}B"
        );
        Console.WriteLine($"    Bitmap: {Convert.ToHexString(mob0.Header.Bitmap)}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"    Parse failed: {ex.GetType().Name}: {ex.Message}");
    }
}

// ── Search ALL inner files for level=6 (0x06000000) and other known values ────────────────────────────────
Console.WriteLine("\n=== Search inner files for level=6 (0x06000000) ===");
var level44 = new byte[] { 0x06, 0x00, 0x00, 0x00 };

// Also search for typical Arcanum max stats (20 = 0x14) array patterns
// and ObjectType.Pc marker (0x0F 0x00 0x00 0x00)
var pcTypeMark = new byte[] { 0x0F, 0x00, 0x00, 0x00 };
int lv44Hits = 0,
    pcHits = 0;
foreach (var (sfPath, sfBytes) in src.Files.OrderBy(x => x.Key))
{
    for (var si = 0; si <= sfBytes.Length - 4; si++)
    {
        if (MatchAt(sfBytes, si, level44))
        {
            var ctxStart = Math.Max(0, si - 8);
            var ctxEnd = Math.Min(sfBytes.Length, si + 16);
            Console.WriteLine(
                $"  LVL44 FOUND: {sfPath}  offset={si:X4}  ctx={Convert.ToHexString(sfBytes[ctxStart..ctxEnd])}"
            );
            lv44Hits++;
            if (lv44Hits >= 20)
                break;
        }
    }
    if (lv44Hits >= 20)
        break;
}
if (lv44Hits == 0)
    Console.WriteLine("  level=6 not found");

Console.WriteLine("\n=== Search inner files for ObjectType.Pc marker (0x0F000000) ===");
foreach (var (sfPath, sfBytes) in src.Files.OrderBy(x => x.Key))
{
    for (var si = 0; si + 4 <= sfBytes.Length; si += 4) // 4B aligned only
    {
        if (MatchAt(sfBytes, si, pcTypeMark))
        {
            var ctxStart = Math.Max(0, si - 48);
            var ctxEnd = Math.Min(sfBytes.Length, si + 8);
            Console.WriteLine(
                $"  PC-type FOUND (4B-aligned): {sfPath}  offset={si:X4}  ctx={Convert.ToHexString(sfBytes[ctxStart..ctxEnd])}"
            );
            pcHits++;
        }
    }
}
Console.WriteLine($"  Total ObjectType.Pc (4B-aligned) hits: {pcHits}");

// ── Quick MobileMdy record-count diagnostic ──────────────────────────────────────────────────────────────────
Console.WriteLine("\n=== MobileMdy parsed records per file ===");
foreach (var (mdyPath, mdyFile) in src.MobileMdys.OrderBy(x => x.Key))
{
    var mobs = mdyFile.Records.Count(r => r.IsMob);
    var chars = mdyFile.Records.Count(r => r.IsCharacter);
    Console.WriteLine($"  {mdyPath}: total={mdyFile.Records.Count}  mobs={mobs}  chars={chars}");
}

// ── Scan mobile.mdy for all maps for ObjectType.Pc ──────────────────────────────────────────────────────────
Console.WriteLine("\n=== All mobile.mdy files: PC scan ===");
int mdyPcTotal = 0;
foreach (var (mdyPath, mdyFile) in src.MobileMdys.OrderBy(x => x.Key))
{
    var mdyPcs = mdyFile
        .Records.Where(m => m.IsMob && m.Mob!.Header.GameObjectType == ObjectType.Pc)
        .Select(m => m.Mob!)
        .ToList();
    var mdyV2Pcs = mdyFile.Records.Where(m => m.IsCharacter).ToList();
    if (mdyPcs.Count > 0 || mdyV2Pcs.Count > 0)
    {
        Console.WriteLine($"  {mdyPath}: {mdyFile.Records.Count} records  PC={mdyPcs.Count}  V2PC={mdyV2Pcs.Count}");
        foreach (var p in mdyPcs)
        {
            var gold = p.Properties.FirstOrDefault(pr => pr.Field == ObjectField.ObjFCritterGold);
            var stats = p.Properties.FirstOrDefault(pr => pr.Field == ObjectField.ObjFCritterStatBaseIdx);
            Console.WriteLine(
                $"    PC: proto={p.Header.ProtoId}  gold={gold?.GetInt32() ?? -1}  stats={stats != null}  propCount={p.Properties.Count}  bitmapHex={Convert.ToHexString(p.Header.Bitmap)}"
            );
        }
        foreach (var v in mdyV2Pcs)
        {
            Console.WriteLine(
                $"    V2PC: LVL={v.Character!.Stats[17]}  Race={v.Character.Stats[27]}  hasAll={v.Character.HasCompleteData}"
            );
        }
    }
    mdyPcTotal += mdyPcs.Count + mdyV2Pcs.Count;
}
Console.WriteLine($"  Total PC objects in mobile.mdy files: {mdyPcTotal}");

// ── Deep scan mobile.mdy raw bytes for ANY occurrence of ObjectType.Pc (0x0F000000) ───────────────────────────
Console.WriteLine("\n=== Raw scan: ALL mobile.mdy files for 0x0F000000 at 4B-aligned positions ===");
foreach (
    var (mdyPath, mdyRawBytes) in src
        .Files.Where(kv => kv.Key.EndsWith("mobile.mdy", StringComparison.OrdinalIgnoreCase))
        .OrderBy(x => x.Key)
)
{
    int pcFoundCount = 0;
    for (int si = 0; si + 4 <= mdyRawBytes.Length; si += 4)
    {
        if (MatchAt(mdyRawBytes, si, pcTypeMark))
        {
            var ctxStart = Math.Max(0, si - 52);
            var ctxEnd = Math.Min(mdyRawBytes.Length, si + 8);
            Console.WriteLine(
                $"  {System.IO.Path.GetDirectoryName(mdyPath)}: offset={si:X5}  ctx={Convert.ToHexString(mdyRawBytes[ctxStart..ctxEnd])}"
            );
            pcFoundCount++;
            if (pcFoundCount >= 5)
                break;
        }
    }
}

// ── Inspect main map mobile.mdy raw at various offsets beyond first record ─────────────────────────────────────
Console.WriteLine("\n=== Inspect main-map mobile.mdy at multiple offsets ===");
var mainMdyKey = currentMapBase is not null ? currentMapBase + "/mobile.mdy" : null;
if (mainMdyKey is not null && src.Files.TryGetValue(mainMdyKey, out var mainMdyRaw))
{
    Console.WriteLine($"  Main map mobile.mdy size: {mainMdyRaw.Length}B");

    // Find the end of the first record by trying to count bytes consumed.
    int firstRecEnd = -1;
    try
    {
        var trialReader = new ArcNET.Core.SpanReader(mainMdyRaw);
        ArcNET.Formats.MobFormat.Parse(ref trialReader);
        firstRecEnd = mainMdyRaw.Length - trialReader.Remaining;
    }
    catch (Exception) { }
    Console.WriteLine($"  First record ends at byte: {firstRecEnd}");

    // Count consecutive 0xFF bytes after first record
    int ffStart = firstRecEnd >= 0 ? firstRecEnd : 0;
    int ffCount = 0;
    while (ffStart + ffCount < mainMdyRaw.Length && mainMdyRaw[ffStart + ffCount] == 0xFF)
        ffCount++;
    Console.WriteLine($"  Consecutive 0xFF bytes after first record: {ffCount}  (next byte at {ffStart + ffCount:X5})");

    int afterFf = ffStart + ffCount;
    if (afterFf < mainMdyRaw.Length)
    {
        var nextCtx = mainMdyRaw.AsSpan(afterFf, Math.Min(64, mainMdyRaw.Length - afterFf)).ToArray();
        Console.WriteLine($"  Bytes after 0xFF block: {Convert.ToHexString(nextCtx)}");
        var nextVer32 = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(nextCtx);
        Console.WriteLine($"  Next DWORD after FFs: 0x{nextVer32:X8}  (as oct/int={nextVer32})");

        // Try to parse from afterFf as mob records
        Console.WriteLine($"  Try parsing from offset {afterFf:X5}:");
        try
        {
            var afterFfReader = new ArcNET.Core.SpanReader(mainMdyRaw.AsSpan(afterFf));
            var mobAfterFf = ArcNET.Formats.MobFormat.Parse(ref afterFfReader);
            Console.WriteLine(
                $"    OK: type={mobAfterFf.Header.GameObjectType}  props={mobAfterFf.Properties.Count}  bitmap={Convert.ToHexString(mobAfterFf.Header.Bitmap)}"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    Failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // Sample bytes at 10% intervals through the file
    Console.WriteLine("  Sampling bytes at 10% intervals:");
    for (int pct = 10; pct <= 90; pct += 10)
    {
        int sampleOffset = (mainMdyRaw.Length * pct) / 100;
        var sample = mainMdyRaw.AsSpan(sampleOffset, Math.Min(16, mainMdyRaw.Length - sampleOffset)).ToArray();
        Console.WriteLine($"    {pct}% ({sampleOffset:X5}): {Convert.ToHexString(sample)}");
    }
}

// ══════════════════════════════════════════════════════════════════════════════════════════════════════════
// TARGETED MOBILE.MDY SEARCH: Find level=6 and OBJ_F_CRITTER_STAT_BASE_IDX array across ALL MDY FILES
// OBJ_F_CRITTER_STAT_BASE_IDX = INT32_ARRAY[28]:
//   idx 0=STR 1=DEX 2=CON 3=BEAUTY 4=INT 5=PERC 6=WILL 7=CHA
//   idx 8=CARRY_WEIGHT 9=DAMAGE_BONUS 10=AC_ADJ 11=SPEED 12=HEAL_RATE 13=POISON_RECOV
//   idx 14=REACTION_MOD 15=MAX_FOLLOWERS 16=MAGIC_TECH_APT 17=LEVEL 18=XP
//   idx 19=ALIGNMENT 20=FATE_POINTS 21=UNSPENT_POINTS 22=MAGIC_POINTS 23=TECH_POINTS
//   idx 24=POISON_LVL 25=AGE 26=GENDER 27=RACE
// In wire format: [elementSize=4][elementCount=28][stat0][stat1]...[stat27] = 120B total
// level=6 appears at byte SAR[8 + 17×4] = SAR[76] from start of the SAR field
// ══════════════════════════════════════════════════════════════════════════════════════════════════════════
Console.WriteLine("\n=== TARGETED: Search ALL mobile.mdy files for level=6 (0x06000000) ===");
var lv44Pattern = new byte[] { 0x06, 0x00, 0x00, 0x00 };
int mdyLv44Total = 0;
foreach (
    var (mdyFilePath, mdyFileBytes) in src
        .Files.Where(kv => kv.Key.EndsWith("mobile.mdy", StringComparison.OrdinalIgnoreCase))
        .OrderBy(x => x.Key)
)
{
    for (int si = 0; si + 4 <= mdyFileBytes.Length; si++)
    {
        if (!MatchAt(mdyFileBytes, si, lv44Pattern))
            continue;

        // Dump 160B around the hit: 80B before = ~2 stat array entries back, 80B after = next stats
        int dumpStart = Math.Max(0, si - 80);
        int dumpEnd = Math.Min(mdyFileBytes.Length, si + 80);
        Console.WriteLine($"\n  LVL44 HIT: {System.IO.Path.GetDirectoryName(mdyFilePath)} @ 0x{si:X5}");
        // Print 16B rows
        for (int row = dumpStart; row < dumpEnd; row += 16)
        {
            var chunkLen = Math.Min(16, dumpEnd - row);
            var mark = (row <= si && si < row + 16) ? " ←LVL44" : "";
            Console.WriteLine($"    {row:X5}: {Convert.ToHexString(mdyFileBytes.AsSpan(row, chunkLen))}{mark}");
        }
        mdyLv44Total++;
        if (mdyLv44Total >= 15)
            break;
    }
    if (mdyLv44Total >= 15)
        break;
}
Console.WriteLine($"  Total level=6 hits across all mobile.mdy files: {mdyLv44Total}");

// Search for the SAR-encoded stat array pattern:
// elementSize=4 (0x04000000), elementCount=28 (0x1C000000)
// This would appear at the START of the OBJ_F_CRITTER_STAT_BASE_IDX SAR field
Console.WriteLine("\n=== TARGETED: Search mobile.mdy files for stat-array SAR header (04000000 1C000000) ===");
var sarStatHeader = new byte[] { 0x04, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00 };
int sarStatTotal = 0;
foreach (
    var (mdyFilePath2, mdyFileBytes2) in src
        .Files.Where(kv => kv.Key.EndsWith("mobile.mdy", StringComparison.OrdinalIgnoreCase))
        .OrderBy(x => x.Key)
)
{
    for (int si = 0; si + 8 <= mdyFileBytes2.Length; si++)
    {
        if (!MatchAt(mdyFileBytes2, si, sarStatHeader))
            continue;

        // Dump stat array: 8B SAR header + 4B bitsetId + 28×4B = 124B
        int dumpEnd = Math.Min(mdyFileBytes2.Length, si + 128);
        var statBytes = mdyFileBytes2.AsSpan(si, dumpEnd - si).ToArray();

        Console.WriteLine($"\n  SAR-STAT HIT: {System.IO.Path.GetDirectoryName(mdyFilePath2)} @ 0x{si:X5}");
        if (statBytes.Length >= 8)
        {
            // Read SAR elementSize + elementCount
            var elemSz = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(statBytes);
            var elemCnt = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(statBytes.AsSpan(4));
            Console.WriteLine($"    SAR: elementSize={elemSz}  elementCount={elemCnt}");
            if (elemSz == 4 && elemCnt == 28 && statBytes.Length >= 12 + 28 * 4)
            {
                string[] statNames =
                [
                    "STR",
                    "DEX",
                    "CON",
                    "BEAUT",
                    "INT",
                    "PERC",
                    "WILL",
                    "CHA",
                    "CARRY_WT",
                    "DMG_BONUS",
                    "AC_ADJ",
                    "SPEED",
                    "HEAL_RATE",
                    "POISON_REC",
                    "REACT_MOD",
                    "MAX_FOLL",
                    "MAGIC_TECH_APT",
                    "LEVEL",
                    "XP_TOTAL",
                    "ALIGNMENT",
                    "FATE_PTS",
                    "UNSPENT_PTS",
                    "MAGIC_PTS",
                    "TECH_PTS",
                    "POISON_LVL",
                    "AGE",
                    "GENDER",
                    "RACE",
                ];
                for (int i = 0; i < 28 && i < statNames.Length; i++)
                {
                    // +12 = skip elemSize(4) + elemCount(4) + bitsetId(4)
                    int val = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                        statBytes.AsSpan(12 + i * 4)
                    );
                    Console.WriteLine($"      [{i:D2}] {statNames[i], -20} = {val}");
                }
            }
        }
        sarStatTotal++;
        if (sarStatTotal >= 10)
            break;
    }
    if (sarStatTotal >= 10)
        break;
}
Console.WriteLine($"  Total stat-SAR headers found: {sarStatTotal}");

// Search for PC's gold: OBJ_F_CRITTER_GOLD is a HANDLE to an OBJ_TYPE_GOLD item.
// The Gold item is in mobile.mdy with type=8 (Gold) and has OBJ_F_GOLD_QUANTITY.
// Gold items in standard mdy will have version=0x77 or 0x08, type=8 (ObjectType.Gold).
Console.WriteLine("\n=== Gold items in mobile.mdy (type=8=ObjectType.Gold) ===");
foreach (var (mdyPath3, mdyFile3) in src.MobileMdys.OrderBy(x => x.Key))
{
    var goldRecs = mdyFile3
        .Records.Where(m => m.IsMob && m.Mob!.Header.GameObjectType == ObjectType.Gold)
        .Select(m => m.Mob!)
        .ToList();
    if (goldRecs.Count == 0)
        continue;
    Console.WriteLine($"  {System.IO.Path.GetDirectoryName(mdyPath3)}: {goldRecs.Count} gold items");
    foreach (var g in goldRecs.Take(5))
    {
        // Show all 4-byte (int32-like) props with raw values
        var int32Props = g
            .Properties.Where(p => p.RawBytes.Length == 4)
            .Select(p => $"{p.Field}={System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(p.RawBytes)}");
        Console.WriteLine(
            $"    proto={g.Header.ProtoId}  bitmap={Convert.ToHexString(g.Header.Bitmap)}  props={string.Join(",", int32Props)}"
        );
    }
}

// ============================================================
// DEEP DIVE: Find Player's v2 PC record (level=6) in main map
// V2 records start with: 02000000 0F000000 00000000
// Within each v2 record, scan forward for SAR stat array header (04000000 1C000000)
// then read stat[17]=LEVEL. For level==44, dump SAR arrays after stats.
// ============================================================
Console.WriteLine("\n=== DEEP DIVE: Player's v2 PC record (level=6) in main map ===");
{
    uint goldHandleId = 0; // Captured from sz=8x1 gold handle field

    // Use src.Files (forward-slash keys) directly — same as the SAR search above
    var mainMdyKvp = src
        .Files.Where(kv =>
            kv.Key.EndsWith("mobile.mdy", StringComparison.OrdinalIgnoreCase)
            && kv.Key.Contains("Arcanum1-024-fixed", StringComparison.OrdinalIgnoreCase)
        )
        .FirstOrDefault();
    string? mainMdyInnerPath = mainMdyKvp.Key;

    if (mainMdyInnerPath == null)
    {
        Console.WriteLine("  [main map mobile.mdy not found in src.Files]");
    }
    else
    {
        var deepRaw = mainMdyKvp.Value;
        Console.WriteLine($"  size={deepRaw.Length}B");
        byte[] v2Magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        byte[] sarStat8 = [0x04, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00];

        int v2Count = 0;
        for (int vi = 0; vi + v2Magic.Length <= deepRaw.Length; vi++)
        {
            if (!MatchAt(deepRaw, vi, v2Magic))
                continue;
            v2Count++;

            int statOff = -1;
            for (int si2 = vi + 12; si2 + 9 <= Math.Min(deepRaw.Length, vi + 4096); si2++)
            {
                if (deepRaw[si2] != 0x01)
                    continue;
                if (!MatchAt(deepRaw, si2 + 1, sarStat8))
                    continue;
                if (si2 + 1 + 8 + 4 + 28 * 4 > deepRaw.Length)
                    continue;
                statOff = si2;
                break;
            }
            if (statOff < 0)
                continue;

            int levelOff = statOff + 1 + 4 + 4 + 4 + 17 * 4;
            if (levelOff + 4 > deepRaw.Length)
                continue;
            int lvl = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(levelOff, 4));
            if (lvl != 6)
                continue;
            // Only process Dark Elf (Race=8 at stat index 27)
            int raceOff = statOff + 1 + 4 + 4 + 4 + 27 * 4;
            if (raceOff + 4 > deepRaw.Length)
                continue;
            int race = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(raceOff, 4));
            if (race != 8)
                continue;

            Console.WriteLine($"\n  *** Level-6 v2 record at 0x{vi:X5}  stat-SAR at 0x{statOff:X5}");

            string[] snD =
            [
                "STR",
                "DEX",
                "CON",
                "BEAUT",
                "INT",
                "PERC",
                "WILL",
                "CHA",
                "CARRY_WT",
                "DMG_BONUS",
                "AC_ADJ",
                "SPEED",
                "HEAL_RATE",
                "POISON_REC",
                "REACT_MOD",
                "MAX_FOLL",
                "MAGIC_TECH_APT",
                "LEVEL",
                "XP_TOTAL",
                "ALIGNMENT",
                "FATE_PTS",
                "UNSPENT_PTS",
                "MAGIC_PTS",
                "TECH_PTS",
                "POISON_LVL",
                "AGE",
                "GENDER",
                "RACE",
            ];
            for (int i = 0; i < 28; i++)
            {
                int sval = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                    deepRaw.AsSpan(statOff + 1 + 4 + 4 + 4 + i * 4, 4)
                );
                if (i % 7 == 0)
                    Console.Write("\n  Stats: ");
                Console.Write($"{snD[i]}={sval}  ");
            }
            Console.WriteLine();

            // === Pre-stat SAR scan (fields before the stat array) ===
            Console.WriteLine($"\n  Pre-stat SAR fields (record+12 → stat-SAR):");
            for (int si_pre = vi + 12; si_pre + 13 < statOff; si_pre++)
            {
                if (deepRaw[si_pre] != 0x01)
                    continue;
                if (si_pre + 13 > deepRaw.Length)
                    break;
                int eS_p = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(si_pre + 1, 4));
                int eC_p = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(si_pre + 5, 4));
                if (eS_p is not (1 or 2 or 4 or 8 or 16) || eC_p is < 1 or > 512)
                    continue;
                int dLen_p = eS_p * eC_p;
                if (si_pre + 13 + dLen_p + 4 > deepRaw.Length)
                    continue;
                int bsId_p = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                    deepRaw.AsSpan(si_pre + 9, 4)
                );
                int bcOff_p = si_pre + 13 + dLen_p;
                int bc_p =
                    bcOff_p + 4 <= deepRaw.Length
                        ? System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(bcOff_p, 4))
                        : 0;
                if (bc_p > 256)
                    continue; // sanity check
                var dpSpan = deepRaw.AsSpan(si_pre + 13, dLen_p);
                string preLbl,
                    preVals;
                if (eS_p == 4 && eC_p <= 20)
                {
                    var pvals = new int[eC_p];
                    for (int k = 0; k < eC_p; k++)
                        pvals[k] = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(dpSpan.Slice(k * 4, 4));
                    preLbl = $"INT32[{eC_p}]";
                    preVals = string.Join(",", pvals);
                }
                else if (eS_p == 8 && eC_p == 1)
                {
                    int v0 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(dpSpan[..4]);
                    uint v1 = (uint)System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(dpSpan[4..]);
                    preLbl = v0 == 8 ? "GOLD_HANDLE" : $"HANDLE(sz8)";
                    preVals = $"v0={v0} v1=0x{v1:X8}";
                }
                else
                {
                    preLbl = $"sz={eS_p}×{eC_p}";
                    preVals = Convert.ToHexString(dpSpan[..Math.Min(32, dLen_p)]);
                }
                // Decode which ObjF bits are set in the bitset
                var bsBytes2 = deepRaw.AsSpan(bcOff_p + 4, bc_p * 4);
                var setBits2 = new System.Collections.Generic.List<int>();
                for (int bw = 0; bw < bc_p; bw++)
                {
                    uint word2 = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(
                        bsBytes2.Slice(bw * 4, 4)
                    );
                    for (int bb = 0; bb < 32; bb++)
                        if ((word2 & (1u << bb)) != 0)
                            setBits2.Add(bw * 32 + bb);
                }
                string bitsetHex2 = Convert.ToHexString(bsBytes2);
                string bitNames2 = string.Join(",", setBits2.Select(b => $"bit{b}={ObjFName(b)}"));
                Console.WriteLine(
                    $"    0x{si_pre:X5}: [{preLbl}] bsId=0x{bsId_p:X4} bsCnt={bc_p}  {preVals}  |bits: {bitNames2}| hex={bitsetHex2}"
                );
                si_pre = bcOff_p + 4 + bc_p * 4 - 1;
            }

            int afterStatData = statOff + 1 + 4 + 4 + 4 + 28 * 4;
            if (afterStatData + 4 > deepRaw.Length)
                continue;
            int bsCnt = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(afterStatData, 4));
            int scanFrom = afterStatData + 4 + bsCnt * 4;
            Console.WriteLine($"  Stat SAR bsCnt={bsCnt}  scanFrom=0x{scanFrom:X5}");

            // Hex dump 512B after stat SAR (extended to find gold handle)
            int dLen = Math.Min(512, deepRaw.Length - scanFrom);
            Console.WriteLine($"  Hex dump [{scanFrom:X5}..{scanFrom + dLen - 1:X5}]:");
            for (int di = 0; di < dLen; di += 16)
            {
                int le = Math.Min(di + 16, dLen);
                Console.Write($"    {scanFrom + di:X5}: ");
                for (int bi = di; bi < le; bi++)
                    Console.Write($"{deepRaw[scanFrom + bi]:X2}");
                Console.WriteLine();
            }

            // Scan for SAR arrays in next 4096B (extended to catch gold handle + inventory)
            Console.WriteLine($"\n  SAR arrays after stats (next 4KB, incl. large arrays):");
            int sarEnd2 = Math.Min(deepRaw.Length, scanFrom + 4096);
            // Track last confirmed SAR end so we can hex-dump any gap
            int lastSarEnd2 = scanFrom;
            for (int si3 = scanFrom; si3 + 13 <= sarEnd2; si3++)
            {
                if (deepRaw[si3] != 0x01)
                    continue;
                if (si3 + 13 > deepRaw.Length)
                    break;
                int eSize = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(si3 + 1, 4));
                int eCnt = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(si3 + 5, 4));
                // Allow large element counts (up to 65536) for global variables etc.
                if (eSize is not (1 or 2 or 4 or 8 or 16 or 24) || eCnt is < 1 or > 65536)
                    continue;
                long dataLen2L = (long)eSize * eCnt;
                if (si3 + 13 + dataLen2L + 4 > deepRaw.Length || dataLen2L > 524288)
                    continue; // skip unreasonably large
                int dataLen2 = (int)dataLen2L;
                int bsId2 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(si3 + 9, 4));
                var dSpan = deepRaw.AsSpan(si3 + 13, dataLen2);
                int bcOff2 = si3 + 13 + dataLen2;
                int bc2 =
                    bcOff2 + 4 <= deepRaw.Length
                        ? System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(bcOff2, 4))
                        : 0;

                // Decode known fields by successive SAR position after stat array
                string fieldLabel;
                string sarVals2;
                if (eSize == 4 && eCnt == 12)
                {
                    // OBJ_F_CRITTER_BASIC_SKILL_IDX (bit 67)
                    string[] basicSkillNames =
                    [
                        "BOW",
                        "DODGE",
                        "MELEE",
                        "THROWING",
                        "BACKSTAB",
                        "PICK_POCKET",
                        "PROWLING",
                        "SPOT_TRAP",
                        "GAMBLING",
                        "HAGGLE",
                        "HEAL",
                        "PERSUASION",
                    ];
                    fieldLabel = "BASIC_SKILL_IDX";
                    var parts = new System.Text.StringBuilder();
                    for (int k = 0; k < eCnt; k++)
                    {
                        int v = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(dSpan.Slice(k * 4, 4));
                        if (v != 0)
                            parts.Append($"{basicSkillNames[k]}={v} ");
                    }
                    sarVals2 = parts.Length > 0 ? parts.ToString() : "(all untrained)";
                }
                else if (eSize == 4 && eCnt == 4)
                {
                    // OBJ_F_CRITTER_TECH_SKILL_IDX (bit 68)
                    string[] techSkillNames = ["REPAIR", "FIREARMS", "PICK_LOCKS", "DISARM_TRAPS"];
                    fieldLabel = "TECH_SKILL_IDX";
                    var parts = new System.Text.StringBuilder();
                    for (int k = 0; k < eCnt; k++)
                    {
                        int v = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(dSpan.Slice(k * 4, 4));
                        if (v != 0)
                            parts.Append($"{techSkillNames[k]}={v} ");
                    }
                    sarVals2 = parts.Length > 0 ? parts.ToString() : "(all untrained)";
                }
                else if (eSize == 4 && eCnt == 25)
                {
                    // OBJ_F_CRITTER_SPELL_TECH_IDX (bit 69)
                    // 0-15=spell colleges, 16=mastery, 17-24=tech disciplines
                    string[] spellColleges =
                    [
                        "CONVEYANCE",
                        "DIVINATION",
                        "AIR",
                        "EARTH",
                        "FIRE",
                        "WATER",
                        "FORCE",
                        "MENTAL",
                        "META",
                        "MORPH",
                        "NATURE",
                        "NECRO_BLACK",
                        "NECRO_WHITE",
                        "PHANTASM",
                        "SUMMONING",
                        "TEMPORAL",
                        "MASTERY",
                    ];
                    string[] techDisciplines =
                    [
                        "HERBOLOGY",
                        "CHEMISTRY",
                        "ELECTRIC",
                        "EXPLOSIVES",
                        "GUN",
                        "MECHANICAL",
                        "SMITHY",
                        "THERAPEUTICS",
                    ];
                    fieldLabel = "SPELL_TECH_IDX";
                    var parts = new System.Text.StringBuilder();
                    for (int k = 0; k < 17; k++)
                    {
                        int v = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(dSpan.Slice(k * 4, 4));
                        if (v != 0)
                            parts.Append($"{spellColleges[k]}={v} ");
                    }
                    for (int k = 0; k < 8; k++)
                    {
                        int v = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                            dSpan.Slice((17 + k) * 4, 4)
                        );
                        if (v != 0)
                            parts.Append($"{techDisciplines[k]}={v} ");
                    }
                    sarVals2 = parts.Length > 0 ? parts.ToString() : "(none)";
                }
                else if (eSize == 16)
                {
                    // OBJ_F_PC_QUEST_IDX or similar — decode each 16B element
                    fieldLabel = $"sz16-ARRAY[{eCnt}] (quests?)";
                    var sb16 = new System.Text.StringBuilder();
                    for (int k = 0; k < Math.Min(eCnt, 12); k++)
                    {
                        int q0 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(dSpan.Slice(k * 16, 4));
                        int q1 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                            dSpan.Slice(k * 16 + 4, 4)
                        );
                        int q2 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                            dSpan.Slice(k * 16 + 8, 4)
                        );
                        int q3 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                            dSpan.Slice(k * 16 + 12, 4)
                        );
                        sb16.Append($"\n          [{k}] id={q0} h=0x{q1:X8} flags={q2} extra={q3}");
                    }
                    if (eCnt > 12)
                        sb16.Append($"\n          ... ({eCnt - 12} more)");
                    sarVals2 = sb16.ToString();
                }
                else if (eSize == 8)
                {
                    // 8-byte handles (gold or inventory)
                    if (eCnt == 1)
                    {
                        int v0 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(dSpan[..4]);
                        uint v1 = (uint)System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(dSpan[4..]);
                        if (v0 == 8) // ObjType.Gold
                        {
                            fieldLabel = "GOLD_HANDLE";
                            sarVals2 = $"ObjType=Gold handleId=0x{v1:X8}";
                            goldHandleId = v1;
                        }
                        else
                        {
                            fieldLabel = "HANDLE(sz=8×1)";
                            sarVals2 = $"v0={v0} v1=0x{v1:X8}";
                        }
                    }
                    else
                    {
                        // Inventory handles — h0 is a compact handle OID-type tag (NOT GameObjectType enum);
                        // h1 is the 32-bit compact OID of the referenced object.
                        // Known: h0=0 → generic world-object ref; h0=11 → key/key-ring object.
                        fieldLabel = $"HANDLES(sz=8×{eCnt})";
                        var sbh = new System.Text.StringBuilder();
                        static string HandleOidType(int t) =>
                            t switch
                            {
                                0 => "ObjRef",
                                11 => "KeyRef",
                                _ => $"htype{t}",
                            };
                        for (int k = 0; k < Math.Min(eCnt, 16); k++)
                        {
                            int h0 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                                dSpan.Slice(k * 8, 4)
                            );
                            uint h1 = (uint)
                                System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(dSpan.Slice(k * 8 + 4, 4));
                            sbh.Append($"\n          [{k}] {HandleOidType(h0)} oid=0x{h1:X8}");
                        }
                        sarVals2 = sbh.ToString();
                    }
                }
                else if (eSize == 24)
                {
                    // 24-byte ObjectID handles (full GUID handles)
                    fieldLabel = $"HANDLES24(sz=24×{eCnt})";
                    var sbh24 = new System.Text.StringBuilder();
                    for (int k = 0; k < Math.Min(eCnt, 8); k++)
                        sbh24.Append($"\n          [{k}] {Convert.ToHexString(dSpan.Slice(k * 24, 24))}");
                    if (eCnt > 8)
                        sbh24.Append($"\n          ... ({eCnt - 8} more)");
                    sarVals2 = sbh24.ToString();
                }
                else if (eSize == 4 && eCnt <= 64)
                {
                    fieldLabel = $"INT32_ARRAY[{eCnt}]";
                    var vals = new int[eCnt];
                    for (int k = 0; k < eCnt; k++)
                        vals[k] = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(dSpan.Slice(k * 4, 4));
                    sarVals2 = string.Join(",", vals);
                }
                else if (eSize == 4 && eCnt > 64)
                {
                    // Large int32 SAR (e.g. PcGlobalVariables)
                    fieldLabel = $"LARGE_INT32_ARRAY[{eCnt}]";
                    // Show first 8 and last 4 non-zero values
                    var nonzero = new System.Collections.Generic.List<string>();
                    for (int k = 0; k < eCnt && nonzero.Count < 16; k++)
                    {
                        int v = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(dSpan.Slice(k * 4, 4));
                        if (v != 0)
                            nonzero.Add($"[{k}]={v}");
                    }
                    sarVals2 = nonzero.Count > 0 ? string.Join(",", nonzero) : "(all zero)";
                }
                else
                {
                    fieldLabel = $"sz={eSize}×{eCnt}";
                    sarVals2 = Convert.ToHexString(dSpan.Slice(0, Math.Min(48, dataLen2)));
                }

                // Show gap between last SAR end and this SAR start (if > 0)
                if (si3 > lastSarEnd2)
                {
                    int gapLen3 = si3 - lastSarEnd2;
                    Console.Write($"    [GAP {lastSarEnd2:X5}..{si3 - 1:X5}: {gapLen3}B] ");
                    // Hex dump up to 64 bytes of the gap
                    int showGap = Math.Min(gapLen3, 64);
                    Console.WriteLine(Convert.ToHexString(deepRaw.AsSpan(lastSarEnd2, showGap)));
                }

                // Decode inner bitset for this post-stat SAR
                string postBitNames = "";
                if (bc2 > 0 && bcOff2 + 4 + bc2 * 4 <= deepRaw.Length)
                {
                    var bsBytes3 = deepRaw.AsSpan(bcOff2 + 4, bc2 * 4);
                    var setBits3 = new System.Collections.Generic.List<int>();
                    for (int bw3 = 0; bw3 < bc2; bw3++)
                    {
                        uint word3 = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(
                            bsBytes3.Slice(bw3 * 4, 4)
                        );
                        for (int bb3 = 0; bb3 < 32; bb3++)
                            if ((word3 & (1u << bb3)) != 0)
                                setBits3.Add(bw3 * 32 + bb3);
                    }
                    postBitNames = " |bits: " + string.Join(",", setBits3.Select(b => $"{ObjFName(b)}")) + "|";
                }
                Console.WriteLine(
                    $"    0x{si3:X5}: [{fieldLabel}] bsId=0x{bsId2:X4} bsCnt={bc2}  {sarVals2}{postBitNames}"
                );
                int thisSarEnd3 = bcOff2 + 4 + bc2 * 4;
                lastSarEnd2 = thisSarEnd3;
                si3 = thisSarEnd3 - 1;
            }
            break; // Only show first level=6 character hit
        }

        // === Non-critter v2 records (gold/item objects) ===
        Console.WriteLine($"\n=== Non-critter v2 records in main map (potential gold/items) ===");
        byte[] sarStatSig = [0x04, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00];
        byte[] goldProtoBytes = [0x74, 0x23, 0x00, 0x00];
        for (int vi2 = 0; vi2 + v2Magic.Length <= deepRaw.Length; vi2++)
        {
            if (!MatchAt(deepRaw, vi2, v2Magic))
                continue;
            // Check if this v2 record has a stat SAR (critter) — skip if so
            bool hasStat = false;
            for (int si5 = vi2 + 12; si5 + 9 <= Math.Min(deepRaw.Length, vi2 + 4096); si5++)
                if (deepRaw[si5] == 0x01 && MatchAt(deepRaw, si5 + 1, sarStatSig))
                {
                    hasStat = true;
                    break;
                }
            if (hasStat)
                continue;
            // Non-critter v2 record — check for gold proto nearby
            bool hasGoldProto = false;
            for (int gp = vi2; gp + 4 <= Math.Min(deepRaw.Length, vi2 + 64); gp++)
                if (MatchAt(deepRaw, gp, goldProtoBytes))
                {
                    hasGoldProto = true;
                    break;
                }
            // Scan for INT32[1] or INT32[2] SARs in first 512B
            var intSars = new System.Collections.Generic.List<(int offset, int val)>();
            for (int si5 = vi2 + 12; si5 + 17 <= Math.Min(deepRaw.Length, vi2 + 512); si5++)
            {
                if (deepRaw[si5] != 0x01)
                    continue;
                int eS5 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(si5 + 1, 4));
                int eC5 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(si5 + 5, 4));
                if (eS5 != 4 || eC5 != 1 || si5 + 17 > deepRaw.Length)
                    continue;
                int v5 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(si5 + 13, 4));
                intSars.Add((si5, v5));
            }
            if (!hasGoldProto && intSars.Count == 0)
                continue;
            Console.Write($"  v2 at 0x{vi2:X5}");
            if (hasGoldProto)
                Console.Write(" [GOLD-PROTO=9076]");
            foreach (var (off, val) in intSars)
                Console.Write($"  INT32@0x{off:X5}={val}");
            Console.WriteLine();
            if (hasGoldProto)
            {
                // Dump 256B of this v2 record and all its SARs
                int dEnd = Math.Min(deepRaw.Length, vi2 + 256);
                for (int di = vi2; di < dEnd; di += 16)
                {
                    int le3 = Math.Min(di + 16, dEnd);
                    Console.Write($"    {di:X5}: ");
                    for (int bi = di; bi < le3; bi++)
                        Console.Write($"{deepRaw[bi]:X2}");
                    Console.WriteLine();
                }
                // Scan for ALL SARs in the record
                for (int si5 = vi2 + 12; si5 + 13 <= dEnd; si5++)
                {
                    if (deepRaw[si5] != 0x01)
                        continue;
                    if (si5 + 13 > deepRaw.Length)
                        break;
                    int eS5 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(si5 + 1, 4));
                    int eC5 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(si5 + 5, 4));
                    if (eS5 is not (1 or 2 or 4 or 8 or 16) || eC5 is < 1 or > 128)
                        continue;
                    int dL5 = eS5 * eC5;
                    if (si5 + 13 + dL5 + 4 > deepRaw.Length)
                        continue;
                    int bsId5 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                        deepRaw.AsSpan(si5 + 9, 4)
                    );
                    var d5 = deepRaw.AsSpan(si5 + 13, dL5);
                    int bcOff5 = si5 + 13 + dL5;
                    int bc5 =
                        bcOff5 + 4 <= deepRaw.Length
                            ? System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(bcOff5, 4))
                            : 0;
                    if (bc5 > 256)
                        continue;
                    if (eS5 == 4)
                    {
                        var vals5 = new int[eC5];
                        for (int k = 0; k < eC5; k++)
                            vals5[k] = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(d5.Slice(k * 4, 4));
                        Console.WriteLine(
                            $"    SAR 0x{si5:X5}: INT32[{eC5}] bsId=0x{bsId5:X4} bsCnt={bc5} = [{string.Join(",", vals5)}]"
                        );
                    }
                    else
                    {
                        Console.WriteLine(
                            $"    SAR 0x{si5:X5}: sz={eS5}×{eC5} bsId=0x{bsId5:X4} bsCnt={bc5} = {Convert.ToHexString(d5[..Math.Min(32, dL5)])}"
                        );
                    }
                    si5 = bcOff5 + 4 + bc5 * 4 - 1;
                }
            }
        }

        Console.WriteLine($"\n  Total v2 records scanned: {v2Count}");

        // === Gold Object Search ===
        if (goldHandleId != 0)
        {
            Console.WriteLine($"\n=== Gold Object Search (handleId=0x{goldHandleId:X8}) ===");
            byte[] goldHandleBytes = BitConverter.GetBytes(goldHandleId); // LE bytes of the handle
            // Search for proto=9076 (0x2374) gold objects and look for the matching handle bytes nearby
            // Gold proto in bytes (LE): 74 23 00 00
            byte[] goldProto = [0x74, 0x23, 0x00, 0x00];
            byte[] goldHandleSearch = [goldHandleBytes[0], goldHandleBytes[1], goldHandleBytes[2], goldHandleBytes[3]];
            for (int gi = 0; gi + 4 <= deepRaw.Length; gi++)
            {
                // Search for the gold handle bytes sequence in the file
                if (deepRaw[gi] != goldHandleSearch[0])
                    continue;
                if (!MatchAt(deepRaw, gi, goldHandleSearch))
                    continue;
                // Found the handle bytes — look around for INT32 gold quantity SAR
                // Try to find SAR: presence=01, elemSize=4, elemCount=1, then data
                int start = Math.Max(0, gi - 64);
                int end = Math.Min(deepRaw.Length, gi + 64);
                Console.WriteLine($"  Handle bytes found at 0x{gi:X5} (context 0x{start:X5}..0x{end:X5}):");
                for (int di = start; di < end; di += 16)
                {
                    int le2 = Math.Min(di + 16, end);
                    Console.Write($"    {di:X5}: ");
                    for (int bi = di; bi < le2; bi++)
                        Console.Write($"{deepRaw[bi]:X2}");
                    Console.WriteLine();
                }
                // Scan nearby for INT32[1] SAR (gold qty)
                for (int si4 = start; si4 + 13 <= end; si4++)
                {
                    if (deepRaw[si4] != 0x01)
                        continue;
                    if (si4 + 13 > deepRaw.Length)
                        break;
                    int eS4 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(si4 + 1, 4));
                    int eC4 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(si4 + 5, 4));
                    if (eS4 != 4 || eC4 != 1)
                        continue;
                    int qty = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(deepRaw.AsSpan(si4 + 13, 4));
                    Console.WriteLine($"  Candidate qty SAR at 0x{si4:X5}: INT32[1]={qty}");
                }
                break; // Only first hit
            }
            if (goldHandleId != 0)
            {
                // Dump all gold objects (proto=9076) in main map mobile.mdy to find ArciMagus's gold
                Console.WriteLine($"\n  Dumping all gold-proto objects in main map mobile.mdy:");
                foreach (
                    var kv in src.Files.Where(f =>
                        f.Key.EndsWith("mobile.mdy", StringComparison.OrdinalIgnoreCase)
                        && f.Key.Contains("Arcanum1-024-fixed", StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    var raw = kv.Value;
                    int goldProtoFound = 0;
                    for (int gi2 = 0; gi2 + 4 <= raw.Length; gi2++)
                    {
                        if (!MatchAt(raw, gi2, goldProto))
                            continue;
                        goldProtoFound++;
                        int dumpStart = Math.Max(0, gi2 - 50);
                        int dumpEnd = Math.Min(raw.Length, gi2 + 200);
                        Console.WriteLine($"\n  Gold#={goldProtoFound} at 0x{gi2:X5}:");
                        // Print hex dump
                        for (int di = dumpStart; di < dumpEnd; di += 16)
                        {
                            int le2 = Math.Min(di + 16, dumpEnd);
                            Console.Write($"    {di:X5}: ");
                            for (int bi = di; bi < le2; bi++)
                                Console.Write($"{raw[bi]:X2}");
                            Console.WriteLine();
                        }
                        // Also scan for INT32[1] SAR in the region after proto (gold qty)
                        for (int si4 = gi2; si4 + 17 <= dumpEnd; si4++)
                        {
                            if (raw[si4] != 0x01)
                                continue;
                            int eS4 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                                raw.AsSpan(si4 + 1, 4)
                            );
                            int eC4 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                                raw.AsSpan(si4 + 5, 4)
                            );
                            if (eS4 == 4 && eC4 == 1 && si4 + 17 <= raw.Length)
                            {
                                int qty = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                                    raw.AsSpan(si4 + 13, 4)
                                );
                                Console.WriteLine($"    ^^^^ INT32[1]={qty} SAR at 0x{si4:X5}");
                            }
                        }
                    }
                }
            }
        }
    } // end mainMdyInnerPath != null else
} // end DEEP DIVE block

// ─────────────────────────────────────────────────────────────────────────────
// HP / FATIGUE DAMAGE HYPOTHESIS SCAN
// bsId=0x4046 (INT32[4], all zeros at full health) likely covers ObjF fields
// {Ac(25), HpPts(26), HpAdj(27), HpDamage(28)} → element [3] = HP damage taken.
// bsId=0x423E (INT32[4], all zeros at full health) likely covers fatigue fields
// {CritterFatiguePts(70), CritterFatigueAdj(71), CritterFatigueDamage(72), ?}.
// At full health all four values are 0 → field absent-by-zero but SAR is present.
// To REDUCE HP: set bsId=0x4046[3] to a positive value (e.g. 20 → HP shows 45-20=25).
// To REDUCE FATIGUE: set bsId=0x423E[3] similarly.
//
// This section also scans ALL v2 critter records in the save for any rare bsId
// that only appears in some records (suggesting it holds non-default state like HP damage).
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n\n═══════════════════════════════════════════════════════════");
Console.WriteLine("=== HP/FATIGUE HYPOTHESIS — bsId element layout ===");
Console.WriteLine("  bsId=0x4046: [AcBonus(0), HpPtsBonus(1), HpAdj(2), HpDamage(3)]");
Console.WriteLine("  bsId=0x423E: [FatiguePts(0), FatigueAdj(1), FatigueDmg(2), ?(3)]");
Console.WriteLine("  → To reduce PC HP by 20: set bsId=0x4046[3]=20 via WithHpDamage([0,0,0,20])");
Console.WriteLine("  → Previous test set [1]=30 (HpPtsBonus = +30 max HP) — HP did NOT drop, correct!");

// Scan ALL v2 critter records in main map for bsId=0x4046, bsId=0x423E and any
// NEW bsIds not seen in the fully-healed PC (which would indicate non-default state).
Console.WriteLine("\n=== Scanning ALL v2 critter records for HP-related SARs ===");
{
    var knownBsIds = new System.Collections.Generic.HashSet<int>
    {
        0x4DA2,
        0x4DA3,
        0x4DA4,
        0x4D6A,
        0x4D69,
        0x4050,
        0x4047,
        0x4046,
        0x423D,
        0x423E,
        0x4299,
        0x43C3,
        0x4A07,
        0x4A08,
        0x49FC,
        0x49FD,
        0x49FE,
        0x49FF,
        0x4A00,
        0x4B13,
        0x4D77,
        0x4D6F,
        0x4D68,
        0x404F,
    };
    byte[] v2Magic2 = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
    byte[] statSig2 = [0x04, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00]; // INT32[28]
    int novelBsIdCount = 0;
    int recordsScanned = 0;

    foreach (
        var (fPath, fBytes) in src.Files.Where(f => f.Key.EndsWith("mobile.mdy", StringComparison.OrdinalIgnoreCase))
    )
    {
        for (int vi = 0; vi + v2Magic2.Length <= fBytes.Length; vi++)
        {
            if (!MatchAt(fBytes, vi, v2Magic2))
                continue;
            // Only process critter v2 records (those that have ST SAR)
            bool isCreature = false;
            for (int si2 = vi + 12; si2 + 9 <= Math.Min(fBytes.Length, vi + 4096); si2++)
                if (fBytes[si2] == 0x01 && MatchAt(fBytes, si2 + 1, statSig2))
                {
                    isCreature = true;
                    break;
                }
            if (!isCreature)
                continue;
            recordsScanned++;

            // Scan SARs from start of v2 record
            int scanLim = Math.Min(fBytes.Length, vi + 8192);
            for (int si2 = vi + 12; si2 + 13 <= scanLim; si2++)
            {
                if (fBytes[si2] != 0x01)
                    continue;
                if (si2 + 13 > fBytes.Length)
                    break;
                int eS2 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(fBytes.AsSpan(si2 + 1, 4));
                int eC2 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(fBytes.AsSpan(si2 + 5, 4));
                if (eS2 != 4 || eC2 is < 1 or > 64)
                    continue;
                long dL2 = eS2 * eC2;
                if (si2 + 13 + dL2 + 4 > fBytes.Length)
                    continue;
                int bId2 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(fBytes.AsSpan(si2 + 9, 4));
                int bcoff2 = (int)(si2 + 13 + dL2);
                int bc2b = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(fBytes.AsSpan(bcoff2, 4));
                if (bc2b is < 0 or > 32)
                    continue;
                int sarEnd2 = bcoff2 + 4 + bc2b * 4;

                // Check bsId=0x4046 specifically — show non-zero values
                if (bId2 == 0x4046 && eC2 == 4)
                {
                    int[] v4 = new int[4];
                    for (int k = 0; k < 4; k++)
                        v4[k] = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                            fBytes.AsSpan(si2 + 13 + k * 4, 4)
                        );
                    if (v4[0] != 0 || v4[1] != 0 || v4[2] != 0 || v4[3] != 0)
                        Console.WriteLine(
                            $"  bsId=0x4046 NON-ZERO at {System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(fPath))} off=0x{si2:X5}: [{string.Join(",", v4)}]  →[3]=HpDamage candidate"
                        );
                }
                // Check bsId=0x423E — show non-zero values
                if (bId2 == 0x423E && eC2 == 4)
                {
                    int[] v4e = new int[4];
                    for (int k = 0; k < 4; k++)
                        v4e[k] = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                            fBytes.AsSpan(si2 + 13 + k * 4, 4)
                        );
                    if (v4e[0] != 0 || v4e[1] != 0 || v4e[2] != 0 || v4e[3] != 0)
                        Console.WriteLine(
                            $"  bsId=0x423E NON-ZERO at {System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(fPath))} off=0x{si2:X5}: [{string.Join(",", v4e)}]  →fatigue candidate"
                        );
                }
                // Report novel bsIds (not in known set) with non-zero values
                if (!knownBsIds.Contains(bId2) && eS2 == 4 && eC2 <= 16)
                {
                    int[] vN = new int[eC2];
                    for (int k = 0; k < eC2; k++)
                        vN[k] = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                            fBytes.AsSpan(si2 + 13 + k * 4, 4)
                        );
                    if (vN.Any(x => x != 0 && x != -1))
                    {
                        novelBsIdCount++;
                        if (novelBsIdCount <= 20)
                            Console.WriteLine(
                                $"  NOVEL bsId=0x{bId2:X4} INT32[{eC2}] at {System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(fPath))} off=0x{si2:X5}: [{string.Join(",", vN)}]"
                            );
                    }
                }
                si2 = sarEnd2 - 1;
            }
        }
    }
    Console.WriteLine($"  Records scanned: {recordsScanned}  Novel bsIds with non-zero: {novelBsIdCount}");
}

// ─────────────────────────────────────────────────────────────────────────────
// SCREENSHOT REFERENCE MATCHER
// All values extracted by reading the test10 (Slot0013) screenshots for ArciMagus.
// The program scans every SAR field found in the PC v2 record and reports which
// parsed values match which screenshot reference values — so unknown fields can be
// identified automatically.
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n\n═══════════════════════════════════════════════════════════");
Console.WriteLine("  SCREENSHOT REFERENCE MATCHER — ArciMagus / test10 / Slot0013");
Console.WriteLine("═══════════════════════════════════════════════════════════");

// ── Reference table (field name → expected value) ────────────────────────────
// Stored values = what is in the binary (before any racial modifier display math).
// Where stored ≠ displayed, both are listed.
var refValues = new System.Collections.Generic.Dictionary<string, int>
{
    // ── Character identity ─────────────────────────────────────────────────
    ["Level"] = 6,
    ["UnspentPoints"] = 0,
    ["XpTotal"] = 15582, // "XP Next: 4718" means 15582+4718=20300 needed for lvl7
    ["XpToNextLvl"] = 4718, // derived, probably not stored directly
    ["Alignment"] = 55, // 0=evil 200=good; displayed as meter ~-10 scale
    ["Age"] = 20, // stored ×10 = displayed 200
    ["Gender"] = 1, // 1=male
    ["Race"] = 8, // Dark Elf
    ["FatePts"] = 1,
    ["MagickPts"] = 4,
    ["TechPts"] = 0,
    // ── Primary stats (STORED, base = before racial bonus) ─────────────────
    ["STR"] = 8, // displayed 8  (no racial modifier)
    ["DEX"] = 8, // displayed 9  (+1 Dark Elf; stored=8)
    ["CON"] = 8, // displayed 8
    ["BEAUT"] = 8, // displayed 8
    ["INT"] = 11, // displayed 12 (+1 Dark Elf; stored=11)
    ["PERC"] = 8, // displayed 7  (-1 Dark Elf; stored=8)
    ["WILL"] = 12, // displayed 13 (+1 Dark Elf; stored=12)
    ["CHA"] = 8, // displayed 8
    // ── Primary stats (DISPLAYED, after racial) ────────────────────────────
    ["STR_disp"] = 8,
    ["DEX_disp"] = 9,
    ["CON_disp"] = 8,
    ["BEAUT_disp"] = 8,
    ["INT_disp"] = 12,
    ["PERC_disp"] = 7,
    ["WILL_disp"] = 13,
    ["CHA_disp"] = 8,
    // ── Derived stats ──────────────────────────────────────────────────────
    ["CarryWeight"] = 4000,
    ["DamageBonus"] = -1,
    ["AcAdjustment"] = -1,
    ["Speed"] = 7,
    ["HealRate"] = 3,
    ["PoisonRecovery"] = 8,
    ["ReactionModifier"] = -2,
    ["MaxFollowers"] = 2,
    // ── Resistances ───────────────────────────────────────────────────────
    ["ResDamage"] = 8,
    ["ResMagick"] = 0,
    ["ResFire"] = 0,
    ["ResPoison"] = 20,
    ["ResElectrical"] = 0,
    // ── HP & Fatigue (two "45" circle gauges on char screen) ──────────────
    // We do NOT know which binary value these come from yet — they're the targets to find.
    ["GaugeCircle_Orange"] = 45, // likely Fatigue or HP current
    ["GaugeCircle_Blue"] = 45, // likely Magick points or HP max (same value, coincidence?)
    ["AlignmentMeter"] = -10, // displayed on the meter (55 stored → -10 displayed?)
    // ── Spells / disciplines ───────────────────────────────────────────────
    ["NECRO_BLACK"] = 3,
    ["TEMPORAL"] = 1,
    ["MASTERY"] = -1,
    // ── Gold & economy ────────────────────────────────────────────────────
    ["Gold"] = 471, // displayed in inventory top-right
    // ── Inventory metadata ────────────────────────────────────────────────
    ["TotalWeight"] = 938, // "938 stone" shown on inventory screen
    ["EncumbranceMax"] = 1200, // "light (1200)"
    // ── Kill log ──────────────────────────────────────────────────────────-
    ["TotalKills"] = 59,
    // ── Ammo ──────────────────────────────────────────────────────────────
    ["Arrows"] = 15, // bsId=0x4D68[8] — user-confirmed (not Silver)
};

Console.WriteLine($"\nReference table: {refValues.Count} known values");
Console.WriteLine("\nValues by magnitude (to aid matching):");
foreach (var kvp in refValues.OrderBy(x => x.Value))
    Console.WriteLine($"  {kvp.Value, 8}  ← {kvp.Key}");

// ── Now scan the PC v2 record raw bytes and match every INT32 value ───────────
Console.WriteLine("\n─── Scanning PC v2 SAR data for matches ─────────────────────────────────");
{
    var mainMdyKvp2 = src
        .Files.Where(kv =>
            kv.Key.EndsWith("mobile.mdy", StringComparison.OrdinalIgnoreCase)
            && kv.Key.Contains("Arcanum1-024-fixed", StringComparison.OrdinalIgnoreCase)
        )
        .FirstOrDefault();

    if (mainMdyKvp2.Key == null)
    {
        Console.WriteLine("  [mobile.mdy not found]");
    }
    else
    {
        var raw = mainMdyKvp2.Value;
        byte[] v2Mag = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        byte[] statS = [0x04, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00];

        // Find the Level=6, Race=8 v2 record
        int pcV2Start = -1;
        for (int i = 0; i + v2Mag.Length <= raw.Length && pcV2Start < 0; i++)
        {
            if (!MatchAt(raw, i, v2Mag))
                continue;
            // find stat SAR
            for (int s = i + 12; s + 13 <= Math.Min(raw.Length, i + 4096); s++)
            {
                if (raw[s] != 0x01 || !MatchAt(raw, s + 1, statS))
                    continue;
                int lvlOff = s + 1 + 8 + 4 + 17 * 4;
                int raceOff = s + 1 + 8 + 4 + 27 * 4;
                if (lvlOff + 4 > raw.Length || raceOff + 4 > raw.Length)
                    break;
                int lvl = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(lvlOff, 4));
                int race = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(raceOff, 4));
                if (lvl == 6 && race == 8)
                {
                    pcV2Start = i;
                    break;
                }
                break;
            }
        }

        if (pcV2Start < 0)
        {
            Console.WriteLine("  [PC v2 record not found]");
        }
        else
        {
            Console.WriteLine($"  PC v2 record at 0x{pcV2Start:X5} — scanning all INT32 values...");

            // Build a reverse lookup: value → list of reference names
            var refByValue = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<string>>();
            foreach (var kv in refValues)
            {
                if (!refByValue.TryGetValue(kv.Value, out var lst))
                {
                    lst = [];
                    refByValue[kv.Value] = lst;
                }
                lst.Add(kv.Key);
            }

            // Walk every candidate SAR within 8KB of the v2 record start
            int scanEnd = Math.Min(raw.Length, pcV2Start + 8192);
            int sarPos = pcV2Start + 12;
            int sarIdx = 0;
            var matchLog = new System.Collections.Generic.List<string>();
            var allSarInts = new System.Collections.Generic.List<(
                int offset,
                int bsId,
                int eCnt,
                int elemIdx,
                int value
            )>();

            while (sarPos + 13 <= scanEnd)
            {
                if (raw[sarPos] != 0x01)
                {
                    sarPos++;
                    continue;
                }
                int eSize = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(sarPos + 1, 4));
                int eCnt = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(sarPos + 5, 4));
                if (eSize != 4 || eCnt is < 1 or > 65536)
                {
                    sarPos++;
                    continue;
                }
                long dataLen = (long)eSize * eCnt;
                if (sarPos + 13 + dataLen + 4 > raw.Length || dataLen > 524288)
                {
                    sarPos++;
                    continue;
                }
                int bsId = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(sarPos + 9, 4));
                int bcOff = sarPos + 13 + (int)dataLen;
                int bc = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(bcOff, 4));
                if (bc is < 0 or > 256)
                {
                    sarPos++;
                    continue;
                }
                int sarEnd3 = bcOff + 4 + bc * 4;
                if (sarEnd3 > scanEnd)
                    break;

                // Extract all int32 elements
                for (int k = 0; k < (int)Math.Min(eCnt, 65536); k++)
                {
                    int val = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                        raw.AsSpan(sarPos + 13 + k * 4, 4)
                    );
                    allSarInts.Add((sarPos, bsId, (int)eCnt, k, val));
                    if (refByValue.TryGetValue(val, out var names))
                    {
                        // Skip zero and -1 — fill values everywhere, useless for identification
                        if (val == 0 || val == -1)
                            continue;
                        string sarLabel = $"bsId=0x{bsId:X4}[{k}/{eCnt}]@0x{sarPos:X5}";
                        foreach (var name in names)
                            matchLog.Add($"  MATCH  {val, 8}  {sarLabel}  ← {name}");
                    }
                }

                sarIdx++;
                sarPos = sarEnd3;
            }

            Console.WriteLine($"  Scanned {sarIdx} SARs, {allSarInts.Count} INT32 elements total.");
            Console.WriteLine($"\n  ── Matches ({matchLog.Count} hits) ──────────────────────────────────");
            if (matchLog.Count == 0)
                Console.WriteLine("  (no matches)");
            else
                foreach (var m in matchLog)
                    Console.WriteLine(m);

            // Also report values from the reference that were NOT matched
            Console.WriteLine($"\n  ── Unmatched reference values ───────────────────────────────────");
            var matchedNames = new System.Collections.Generic.HashSet<string>();
            foreach (var (offset, bsId, cnt, idx, val) in allSarInts)
                if (refByValue.TryGetValue(val, out var names))
                    foreach (var n in names)
                        matchedNames.Add(n);
            foreach (var kv in refValues.Where(kv => !matchedNames.Contains(kv.Key)))
                Console.WriteLine($"  NOT FOUND  {kv.Value, 8}  ← {kv.Key}");
        }
    }
}

// ============================================================
// SAVE GAME EDITOR ROUND-TRIP TEST
// ============================================================
Console.WriteLine("\n=== SaveGameEditor round-trip test ===");
{
    var editor = new ArcNET.Editor.SaveGameEditor(src);

    if (!editor.TryFindCharacter(c => c.Level == 6 && c.Race == 8, out var pc, out var pcMdyPath))
    {
        Console.WriteLine("  [level=6 race=8 v2 record not found]");
    }
    else
    {
        Console.WriteLine(
            $"  Before: STR={pc.Strength} DEX={pc.Dexterity} INT={pc.Intelligence} WILL={pc.Willpower} LVL={pc.Level} XP={pc.ExperiencePoints} Align={pc.Alignment}"
        );
        Console.WriteLine($"  Before skills: BOW={pc.SkillBow} MELEE={pc.SkillMelee} HEAL={pc.SkillHeal}");
        Console.WriteLine(
            $"  Before spells: NECRO_BLACK={pc.SpellNecroBlack} TEMPORAL={pc.SpellTemporal} MASTERY={pc.SpellMastery}"
        );
        Console.WriteLine(
            $"  Before gold: {pc.Gold}  HasCompleteData={pc.HasCompleteData}  GoldDataOffset={(pc.Gold >= 0 ? "set" : "absent")}"
        );

        // Bump XP by 1 and bump gold by 100 to verify the round-trip
        var updated = pc.ToBuilder().WithExperiencePoints(pc.ExperiencePoints + 1).WithGold(pc.Gold + 100).Build();
        editor.WithCharacter(pcMdyPath, c => c.Level == 6 && c.Race == 8, updated);

        // Read back from pending MobileMdyFile without writing to disk
        var pendingMdy = editor.GetPendingMobileMdy(pcMdyPath);
        var pendingChar = pendingMdy?.Characters.FirstOrDefault(c => c.Stats[17] == 6 && c.Stats[27] == 8);
        if (pendingChar is not null)
        {
            var readBack = ArcNET.Editor.CharacterRecord.From(pendingChar);
            Console.WriteLine(
                $"  After:  XP={readBack.ExperiencePoints} (expected {pc.ExperiencePoints + 1})  PASS={readBack.ExperiencePoints == pc.ExperiencePoints + 1}"
            );
            Console.WriteLine(
                $"  After gold: {readBack.Gold} (expected {pc.Gold + 100})  PASS={readBack.Gold == pc.Gold + 100}"
            );
        }

        Console.WriteLine("  Round-trip confirmed. No disk write performed.");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// SECTION: Full bsId=0x4D68 element dump (11-element game-stats SAR)
// Confirmed: [0]=TotalKills=59, [8]=Arrows=15.
// Dump all 11 values so we can identify the remaining 9 elements.
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n\n═══════════════════════════════════════════════════════════");
Console.WriteLine("=== bsId=0x4D68 full element dump (all v2 critter records) ===");
{
    byte[] v2MagicD = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
    byte[] statSigD = [0x04, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00]; // INT32[28]
    const int GameStatsBsId = 0x4D68;
    const int GameStatsElemCount = 11;
    string[] gameStatsNames =
    [
        "TotalKills",
        "elem1",
        "elem2",
        "elem3",
        "elem4",
        "elem5",
        "elem6",
        "elem7",
        "Arrows",
        "elem9",
        "elem10",
    ];

    foreach (
        var (fPath, fBytes) in src
            .Files.Where(f => f.Key.EndsWith("mobile.mdy", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Key)
    )
    {
        for (int vi = 0; vi + v2MagicD.Length <= fBytes.Length; vi++)
        {
            if (!MatchAt(fBytes, vi, v2MagicD))
                continue;

            // Only critter v2 records (stat SAR present)
            bool isCritter = false;
            int levelVal = -1,
                raceVal = -1;
            for (int si = vi + 12; si + 13 <= Math.Min(fBytes.Length, vi + 4096); si++)
            {
                if (fBytes[si] != 0x01 || !MatchAt(fBytes, si + 1, statSigD))
                    continue;
                isCritter = true;
                int lvlOff = si + 1 + 8 + 4 + 17 * 4;
                int raceOff = si + 1 + 8 + 4 + 27 * 4;
                if (lvlOff + 4 <= fBytes.Length)
                    levelVal = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(fBytes.AsSpan(lvlOff, 4));
                if (raceOff + 4 <= fBytes.Length)
                    raceVal = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(fBytes.AsSpan(raceOff, 4));
                break;
            }
            if (!isCritter)
                continue;

            // Scan for bsId=0x4D68
            int scanLim = Math.Min(fBytes.Length, vi + 16384);
            for (int si = vi + 12; si + 13 <= scanLim; si++)
            {
                if (fBytes[si] != 0x01)
                    continue;
                int eS = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(fBytes.AsSpan(si + 1, 4));
                int eC = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(fBytes.AsSpan(si + 5, 4));
                if (eS != 4 || eC != GameStatsElemCount)
                    continue;
                int bId = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(fBytes.AsSpan(si + 9, 4));
                if (bId != GameStatsBsId)
                    continue;

                // Found! Dump all elements
                var vals = new int[GameStatsElemCount];
                for (int k = 0; k < GameStatsElemCount; k++)
                    vals[k] = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                        fBytes.AsSpan(si + 13 + k * 4, 4)
                    );

                Console.WriteLine(
                    $"\n  v2 record @ 0x{vi:X5}  map={System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(fPath))}  LVL={levelVal}  Race={raceVal}"
                );
                for (int k = 0; k < GameStatsElemCount; k++)
                    Console.WriteLine($"    [{k:D2}] {gameStatsNames[k], -12} = {vals[k]}");
            }
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// SECTION: data.sav full SAR scan
// Attempt to parse data.sav as a sequence of SAR packets to identify what
// character/game state it contains.
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n\n═══════════════════════════════════════════════════════════");
Console.WriteLine("=== data.sav full SAR scan ===");
if (src.Files.TryGetValue("data.sav", out var dataSavBytes))
{
    Console.WriteLine($"  data.sav size: {dataSavBytes.Length}B");
    Console.WriteLine($"  First 32B: {Convert.ToHexString(dataSavBytes[..Math.Min(32, dataSavBytes.Length)])}");

    // ── Try parsing data.sav as a flat mob record sequence ──────────────────
    Console.WriteLine("\n  Attempting MobFormat.Parse (mob sequence) from offset 0...");
    {
        int mobCount = 0;
        var dsReader = new ArcNET.Core.SpanReader(dataSavBytes);
        while (dsReader.Remaining >= 4)
        {
            int ver = dsReader.PeekInt32At(0);
            if (ver != 0x08 && ver != 0x77)
                break;
            try
            {
                var mob = ArcNET.Formats.MobFormat.Parse(ref dsReader);
                mobCount++;
                var g = mob.Properties.FirstOrDefault(p => p.Field == ObjectField.ObjFCritterGold);
                Console.WriteLine(
                    $"    Mob[{mobCount}] type={mob.Header.GameObjectType}  props={mob.Properties.Count}  gold={g?.GetInt32() ?? -1}  bitmap={Convert.ToHexString(mob.Header.Bitmap)}"
                );
                if (mobCount >= 10)
                    break;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"    Parse fail at offset 0x{dataSavBytes.Length - dsReader.Remaining:X4}: {ex.GetType().Name}"
                );
                break;
            }
        }
        Console.WriteLine($"    Parsed {mobCount} mob records  remaining={dsReader.Remaining}B");
    }

    // ── Try parsing data.sav as v2 SAR records ──────────────────────────────
    Console.WriteLine("\n  Scanning data.sav for v2-style SAR packets...");
    {
        int sarCount = 0;
        var refValsD = new Dictionary<int, string>
        {
            [6] = "Level",
            [15582] = "XpTotal",
            [55] = "Alignment",
            [8] = "STR/DEX/CON etc",
            [11] = "INT",
            [12] = "WILL",
            [20] = "Age",
            [1] = "Gender/FatePts",
            [8] = "Race",
            [59] = "TotalKills",
            [15] = "Arrows",
            [67] = "Gold",
            [3] = "NECRO_BLACK/HealRate",
            [1] = "TEMPORAL",
        };
        for (int si = 0; si + 13 <= dataSavBytes.Length; si++)
        {
            if (dataSavBytes[si] != 0x01)
                continue;
            if (si + 13 > dataSavBytes.Length)
                break;
            int eS = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(dataSavBytes.AsSpan(si + 1, 4));
            int eC = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(dataSavBytes.AsSpan(si + 5, 4));
            if (eS is not (1 or 2 or 4 or 8 or 16) || eC is < 1 or > 8192)
                continue;
            long dL = (long)eS * eC;
            if (si + 13 + dL + 4 > dataSavBytes.Length || dL > 65536)
                continue;
            int bsId = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(dataSavBytes.AsSpan(si + 9, 4));
            int bcOff = (int)(si + 13 + dL);
            int bc = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(dataSavBytes.AsSpan(bcOff, 4));
            if (bc is < 0 or > 128)
                continue;
            int sarEnd4 = bcOff + 4 + bc * 4;
            if (sarEnd4 > dataSavBytes.Length)
                continue;

            // Decode values
            string valStr;
            if (eS == 4 && eC <= 32)
            {
                var vals = new int[eC];
                for (int k = 0; k < eC; k++)
                    vals[k] = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                        dataSavBytes.AsSpan(si + 13 + k * 4, 4)
                    );
                // Check if any value matches reference values
                var matches = vals.Select((v, k) => refValsD.TryGetValue(v, out var name) ? $"[{k}]={v}({name})" : null)
                    .Where(m => m != null)
                    .ToList();
                valStr =
                    string.Join(",", vals) + (matches.Count > 0 ? $"  ← possible: {string.Join(",", matches)}" : "");
            }
            else if (eS == 4 && eC > 32)
            {
                // Large array — show non-zero count
                int nonZero = 0;
                for (int k = 0; k < eC; k++)
                    if (
                        System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                            dataSavBytes.AsSpan(si + 13 + k * 4, 4)
                        ) != 0
                    )
                        nonZero++;
                valStr = $"INT32[{eC}] nonZero={nonZero}";
            }
            else
            {
                valStr = $"sz={eS}×{eC}";
            }

            Console.WriteLine($"  0x{si:X4}: bsId=0x{bsId:X4} sz={eS} cnt={eC} bsCnt={bc}  {valStr}");
            sarCount++;
            si = sarEnd4 - 1;
            if (sarCount >= 80)
            {
                Console.WriteLine("  (truncated at 80 SARs)");
                break;
            }
        }
        Console.WriteLine($"  Total SAR packets found: {sarCount}");
    }
}
else
{
    Console.WriteLine("  data.sav not found in inner files");
}

// ─────────────────────────────────────────────────────────────────────────────
// SECTION: data2.sav full SAR scan
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n=== data2.sav full SAR scan ===");
if (src.Files.TryGetValue("data2.sav", out var data2SavBytes))
{
    Console.WriteLine($"  data2.sav size: {data2SavBytes.Length}B");
    Console.WriteLine($"  First 32B: {Convert.ToHexString(data2SavBytes[..Math.Min(32, data2SavBytes.Length)])}");
    int sarCount2 = 0;
    for (int si = 0; si + 13 <= data2SavBytes.Length; si++)
    {
        if (data2SavBytes[si] != 0x01)
            continue;
        int eS = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(data2SavBytes.AsSpan(si + 1, 4));
        int eC = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(data2SavBytes.AsSpan(si + 5, 4));
        if (eS is not (1 or 2 or 4 or 8 or 16) || eC is < 1 or > 8192)
            continue;
        long dL = (long)eS * eC;
        if (si + 13 + dL + 4 > data2SavBytes.Length || dL > 65536)
            continue;
        int bsId = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(data2SavBytes.AsSpan(si + 9, 4));
        int bcOff2 = (int)(si + 13 + dL);
        int bc2 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(data2SavBytes.AsSpan(bcOff2, 4));
        if (bc2 is < 0 or > 128)
            continue;
        int sarEnd5 = bcOff2 + 4 + bc2 * 4;
        if (sarEnd5 > data2SavBytes.Length)
            continue;
        string v2Str =
            eS == 4 && eC <= 16
                ? string.Join(
                    ",",
                    Enumerable
                        .Range(0, eC)
                        .Select(k =>
                            System
                                .Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                                    data2SavBytes.AsSpan(si + 13 + k * 4, 4)
                                )
                                .ToString()
                        )
                )
                : $"sz={eS}×{eC}";
        Console.WriteLine($"  0x{si:X4}: bsId=0x{bsId:X4} sz={eS} cnt={eC} bsCnt={bc2}  {v2Str}");
        sarCount2++;
        si = sarEnd5 - 1;
        if (sarCount2 >= 40)
        {
            Console.WriteLine("  (truncated)");
            break;
        }
    }
    Console.WriteLine($"  Total SAR packets: {sarCount2}");
}
else
{
    Console.WriteLine("  data2.sav not found");
}

// ─────────────────────────────────────────────────────────────────────────────
// SECTION: PC Name string search
// Search for the PC name "ArciMagus" (and shorter variants) as raw ASCII bytes
// in the main-map mobile.mdy to locate the OBJ_F_PC_PLAYER_NAME string SAR.
// Also search for it encoded as UTF-16 LE (common in .NET / Windows formats).
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n\n═══════════════════════════════════════════════════════════");
Console.WriteLine("=== PC Name string search (OBJ_F_PC_PLAYER_NAME) ===");
{
    var mainMdyForName = src
        .Files.Where(kv =>
            kv.Key.EndsWith("mobile.mdy", StringComparison.OrdinalIgnoreCase)
            && kv.Key.Contains("Arcanum1-024-fixed", StringComparison.OrdinalIgnoreCase)
        )
        .FirstOrDefault();

    if (mainMdyForName.Key == null)
    {
        Console.WriteLine("  [main-map mobile.mdy not found]");
    }
    else
    {
        var raw = mainMdyForName.Value;
        byte[] nameAscii = System.Text.Encoding.ASCII.GetBytes("ArciMagus");
        byte[] nameUtf16 = System.Text.Encoding.Unicode.GetBytes("ArciMagus");
        byte[] nameShort = System.Text.Encoding.ASCII.GetBytes("Arci");

        static int FindBytes(byte[] haystack, byte[] needle, int from = 0)
        {
            for (int i = from; i + needle.Length <= haystack.Length; i++)
            {
                bool ok = true;
                for (int j = 0; j < needle.Length && ok; j++)
                    ok = haystack[i + j] == needle[j];
                if (ok)
                    return i;
            }
            return -1;
        }

        int hitAscii = FindBytes(raw, nameAscii);
        int hitUtf16 = FindBytes(raw, nameUtf16);
        int hitShort = FindBytes(raw, nameShort);

        if (hitAscii >= 0)
        {
            Console.WriteLine($"  ASCII 'ArciMagus' found at 0x{hitAscii:X5}");
            // Dump 64B around the hit
            int dumpStart = Math.Max(0, hitAscii - 32);
            int dumpEnd = Math.Min(raw.Length, hitAscii + 64);
            for (int di = dumpStart; di < dumpEnd; di += 16)
            {
                int le = Math.Min(di + 16, dumpEnd);
                Console.Write($"    {di:X5}: ");
                for (int bi = di; bi < le; bi++)
                    Console.Write($"{raw[bi]:X2} ");
                Console.Write("  |");
                for (int bi = di; bi < le; bi++)
                    Console.Write(raw[bi] >= 32 && raw[bi] < 127 ? (char)raw[bi] : '.');
                Console.WriteLine("|");
            }
            // Check if there's a SAR header before it (length-prefixed string SAR)
            if (hitAscii >= 13)
            {
                // Look for presence=01 + elemSz=1 + elemCnt=[name_length] in the 32B before the string
                for (int si = Math.Max(0, hitAscii - 20); si < hitAscii; si++)
                {
                    if (raw[si] != 0x01 || si + 13 > raw.Length)
                        continue;
                    int eS = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(si + 1, 4));
                    int eC = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(si + 5, 4));
                    int bId = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(si + 9, 4));
                    if (eS == 1 && eC >= 4 && eC <= 64 && si + 13 + eC <= raw.Length)
                        Console.WriteLine(
                            $"  Candidate string SAR at 0x{si:X5}: sz=1×{eC} bsId=0x{bId:X4}  text=[{System.Text.Encoding.ASCII.GetString(raw, si + 13, eC)}]"
                        );
                }
            }
        }
        else
        {
            Console.WriteLine("  ASCII 'ArciMagus' NOT found in main-map mobile.mdy");
        }

        if (hitUtf16 >= 0)
        {
            Console.WriteLine($"  UTF-16 'ArciMagus' found at 0x{hitUtf16:X5}");
            int dumpStart = Math.Max(0, hitUtf16 - 16);
            int dumpEnd = Math.Min(raw.Length, hitUtf16 + 32);
            for (int di = dumpStart; di < dumpEnd; di += 16)
            {
                int le = Math.Min(di + 16, dumpEnd);
                Console.Write($"    {di:X5}: ");
                for (int bi = di; bi < le; bi++)
                    Console.Write($"{raw[bi]:X2} ");
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine("  UTF-16 'ArciMagus' NOT found in main-map mobile.mdy");
        }

        // Also scan all inner files for the name
        Console.WriteLine("\n  Scanning ALL inner files for 'ArciMagus' (ASCII):");
        foreach (var (fKey, fVal) in src.Files.OrderBy(x => x.Key))
        {
            int hit = FindBytes(fVal, nameAscii);
            if (hit >= 0)
                Console.WriteLine($"    {fKey} @ 0x{hit:X5}");
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// SECTION: PC-exclusive SARs — bsIds present in PC record but absent in ALL NPCs
// This isolates SARs that store PC-only data: name, bank money, reputation,
// portrait, PC flags, global variables.
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n\n═══════════════════════════════════════════════════════════");
Console.WriteLine("=== PC-exclusive bsIds (not found in NPC v2 records) ===");
{
    byte[] v2MagicX = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
    byte[] statSigX = [0x04, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00];
    const int PcLevel = 6;
    const int PcRace = 8;

    var pcBsIds = new System.Collections.Generic.Dictionary<int, (int eCnt, int eSize, int[] sampleVals)>();
    var npcBsIds = new System.Collections.Generic.HashSet<int>();

    foreach (
        var (fPath, fBytes) in src.Files.Where(f => f.Key.EndsWith("mobile.mdy", StringComparison.OrdinalIgnoreCase))
    )
    {
        for (int vi = 0; vi + v2MagicX.Length <= fBytes.Length; vi++)
        {
            if (!MatchAt(fBytes, vi, v2MagicX))
                continue;

            // Find stat SAR and read level/race
            int levelV = -1,
                raceV = -1;
            bool hasStat = false;
            for (int si = vi + 12; si + 13 <= Math.Min(fBytes.Length, vi + 4096); si++)
            {
                if (fBytes[si] != 0x01 || !MatchAt(fBytes, si + 1, statSigX))
                    continue;
                hasStat = true;
                int lvlOff = si + 1 + 8 + 4 + 17 * 4;
                int raceOff = si + 1 + 8 + 4 + 27 * 4;
                if (lvlOff + 4 <= fBytes.Length)
                    levelV = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(fBytes.AsSpan(lvlOff, 4));
                if (raceOff + 4 <= fBytes.Length)
                    raceV = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(fBytes.AsSpan(raceOff, 4));
                break;
            }
            if (!hasStat)
                continue;

            bool isPC = (levelV == PcLevel && raceV == PcRace);

            // Scan all SARs in this record
            int scanLimX = Math.Min(fBytes.Length, vi + 32768);
            for (int si = vi + 12; si + 13 <= scanLimX; si++)
            {
                if (fBytes[si] != 0x01)
                    continue;
                int eS = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(fBytes.AsSpan(si + 1, 4));
                int eC = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(fBytes.AsSpan(si + 5, 4));
                if (eS is not (1 or 2 or 4 or 8 or 16) || eC is < 1 or > 8192)
                    continue;
                long dL = (long)eS * eC;
                if (si + 13 + dL + 4 > fBytes.Length || dL > 131072)
                    continue;
                int bId = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(fBytes.AsSpan(si + 9, 4));
                int bcOff = (int)(si + 13 + dL);
                int bc = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(fBytes.AsSpan(bcOff, 4));
                if (bc is < 0 or > 512)
                    continue;
                int sarEnd = bcOff + 4 + bc * 4;
                if (sarEnd > scanLimX)
                    break;

                if (isPC)
                {
                    if (!pcBsIds.ContainsKey(bId))
                    {
                        // Capture sample values (up to 16 ints)
                        var sampleCount = Math.Min(eC, 16);
                        var sample = new int[sampleCount];
                        if (eS == 4)
                            for (int k = 0; k < sampleCount; k++)
                                sample[k] = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                                    fBytes.AsSpan(si + 13 + k * 4, 4)
                                );
                        pcBsIds[bId] = (eC, eS, sample);
                    }
                }
                else
                {
                    npcBsIds.Add(bId);
                }

                si = sarEnd - 1;
            }
        }
    }

    var exclusivePcBsIds = pcBsIds.Where(kv => !npcBsIds.Contains(kv.Key)).OrderBy(kv => kv.Key).ToList();
    Console.WriteLine($"  PC bsIds total: {pcBsIds.Count}  NPC bsIds total: {npcBsIds.Count}");
    Console.WriteLine($"  PC-EXCLUSIVE bsIds: {exclusivePcBsIds.Count}");
    foreach (var (bId, (eCnt, eSize, sample)) in exclusivePcBsIds)
    {
        string valStr =
            eSize == 4 && sample.Length > 0
                ? string.Join(",", sample) + (eCnt > 16 ? $"... ({eCnt} total)" : "")
                : $"sz={eSize}×{eCnt}";
        Console.WriteLine($"  bsId=0x{bId:X4}  sz={eSize}×{eCnt}  vals=[{valStr}]");
    }

    // Also show ALL PC bsIds not seen so far in CharacterMdyRecord
    var knownInCode = new System.Collections.Generic.HashSet<int>
    {
        0x4DA3,
        0x4046,
        0x423E,
        0x4DA5,
        0x43C3,
        0x4A07,
        0x4A08,
        0x4B13,
        0x4D68,
    };
    var unknownPcBsIds = pcBsIds.Where(kv => !knownInCode.Contains(kv.Key)).OrderBy(kv => kv.Key).ToList();
    Console.WriteLine($"\n  PC bsIds NOT yet parsed in CharacterMdyRecord ({unknownPcBsIds.Count}):");
    foreach (var (bId, (eCnt, eSize, sample)) in unknownPcBsIds)
    {
        string valStr =
            eSize == 4 && sample.Length > 0
                ? string.Join(",", sample) + (eCnt > 16 ? $"... ({eCnt} total)" : "")
                : $"sz={eSize}×{eCnt}";
        bool exclusivePC = !npcBsIds.Contains(bId);
        Console.WriteLine($"  bsId=0x{bId:X4}  sz={eSize}×{eCnt}  vals=[{valStr}]  PC-only={exclusivePC}");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// SECTION: bsId=0x4D68 cross-reference with reference values
// All 11 elements cross-checked against known PC stats.
// Elements [1],[2],[3],[4],[5],[6],[7],[9],[10] still unknown.
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n\n═══════════════════════════════════════════════════════════");
Console.WriteLine("=== bsId=0x4D68 cross-reference (known PC values) ===");
Console.WriteLine("  Known:   [0]=TotalKills=59  [8]=Arrows=15");
Console.WriteLine("  Unknown: [1],[2],[3],[4],[5],[6],[7],[9],[10]");
Console.WriteLine("  Possible ammo: Bullets(OBJ_F_CRITTER_BULLETS=bit80), PowerCells(bit81), Fuel(bit82)");
{
    var mainMdyCr = src
        .Files.Where(kv =>
            kv.Key.EndsWith("mobile.mdy", StringComparison.OrdinalIgnoreCase)
            && kv.Key.Contains("Arcanum1-024-fixed", StringComparison.OrdinalIgnoreCase)
        )
        .FirstOrDefault();

    if (mainMdyCr.Key != null)
    {
        var raw = mainMdyCr.Value;
        byte[] v2MagC = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        byte[] statSC = [0x04, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00];

        for (int vi = 0; vi + v2MagC.Length <= raw.Length; vi++)
        {
            if (!MatchAt(raw, vi, v2MagC))
                continue;
            bool found = false;
            for (int si = vi + 12; si + 13 <= Math.Min(raw.Length, vi + 4096); si++)
            {
                if (raw[si] != 0x01 || !MatchAt(raw, si + 1, statSC))
                    continue;
                int lvl = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                    raw.AsSpan(si + 1 + 8 + 4 + 17 * 4, 4)
                );
                int race = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                    raw.AsSpan(si + 1 + 8 + 4 + 27 * 4, 4)
                );
                if (lvl != 6 || race != 8)
                    break;
                found = true;
                break;
            }
            if (!found)
                continue;

            // Find bsId=0x4D68 with 11 elements
            int scanLimC = Math.Min(raw.Length, vi + 32768);
            for (int si = vi + 12; si + 13 <= scanLimC; si++)
            {
                if (raw[si] != 0x01)
                    continue;
                int eS = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(si + 1, 4));
                int eC = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(si + 5, 4));
                int bId = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(si + 9, 4));
                if (bId != 0x4D68 || eS != 4 || eC != 11)
                {
                    si++;
                    continue;
                }
                Console.WriteLine("  bsId=0x4D68 [11 INT32 elements]:");
                string[] labels =
                [
                    "TotalKills",
                    "?elem1",
                    "?elem2",
                    "?elem3",
                    "?elem4",
                    "?elem5",
                    "?elem6",
                    "?elem7",
                    "Arrows",
                    "?elem9(Bullets?)",
                    "?elem10(PwrCell?)",
                ];
                for (int k = 0; k < 11; k++)
                {
                    int v = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                        raw.AsSpan(si + 13 + k * 4, 4)
                    );
                    Console.WriteLine($"    [{k}] {labels[k], -20} = {v, 8}  (0x{(uint)v:X8})");
                }
                // Also show the bitset entries for this SAR
                int bcOff = si + 13 + 11 * 4;
                int bc = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(bcOff, 4));
                Console.WriteLine($"  bsCnt={bc}");
                if (bc > 0 && bcOff + 4 + bc * 4 <= raw.Length)
                {
                    var bsWords = new System.Collections.Generic.List<int>();
                    for (int k = 0; k < bc; k++)
                        bsWords.Add(
                            System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                                raw.AsSpan(bcOff + 4 + k * 4, 4)
                            )
                        );
                    Console.WriteLine($"  Inner bitset words: {string.Join(",", bsWords.Select(w => $"0x{w:X8}"))}");
                    // Decode bit positions
                    var setBits = new System.Collections.Generic.List<int>();
                    for (int bw = 0; bw < bc; bw++)
                    {
                        uint word = (uint)bsWords[bw];
                        for (int bb = 0; bb < 32; bb++)
                            if ((word & (1u << bb)) != 0)
                                setBits.Add(bw * 32 + bb);
                    }
                    Console.WriteLine(
                        $"  Bit positions set: {string.Join(",", setBits)} → ObjectField bits: {string.Join(",", setBits.Select(b => ObjFName(b)))}"
                    );
                }
                break;
            }
            break;
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// SECTION: Bank money, reputation, portrait search
// Scan PC v2 record for PC-specific single-INT32 SARs that might hold bank money,
// reputation index, or portrait index. These are expected in PC-only bsIds.
// Cross-reference with known Arcanum values:
//   Bank money: OBJ_F_PC_BANK_MONEY (bit 146)
//   Reputation:  OBJ_F_PC_REPUTATION_IDX (bit 130)
//   Portrait:    OBJ_F_PC_PORTRAIT (bit 132) — typically a small integer index
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n\n═══════════════════════════════════════════════════════════");
Console.WriteLine("=== Bank money / reputation / portrait SAR scan ===");
Console.WriteLine("  Strategy: find all INT32[1] SARs in the PC v2 record and list them.");
Console.WriteLine("  Bank money is likely the only large integer not yet assigned.");
Console.WriteLine("  Portrait and reputation are small integers (0–50 range).");
{
    var mainMdyBr = src
        .Files.Where(kv =>
            kv.Key.EndsWith("mobile.mdy", StringComparison.OrdinalIgnoreCase)
            && kv.Key.Contains("Arcanum1-024-fixed", StringComparison.OrdinalIgnoreCase)
        )
        .FirstOrDefault();

    if (mainMdyBr.Key != null)
    {
        var raw = mainMdyBr.Value;
        byte[] v2MagB = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        byte[] statSB = [0x04, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00];

        int pcStart = -1;
        for (int vi = 0; vi + v2MagB.Length <= raw.Length && pcStart < 0; vi++)
        {
            if (!MatchAt(raw, vi, v2MagB))
                continue;
            for (int si = vi + 12; si + 13 <= Math.Min(raw.Length, vi + 4096); si++)
            {
                if (raw[si] != 0x01 || !MatchAt(raw, si + 1, statSB))
                    continue;
                int lvl = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                    raw.AsSpan(si + 1 + 8 + 4 + 17 * 4, 4)
                );
                int race = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                    raw.AsSpan(si + 1 + 8 + 4 + 27 * 4, 4)
                );
                if (lvl == 6 && race == 8)
                {
                    pcStart = vi;
                    break;
                }
                break;
            }
        }

        if (pcStart < 0)
        {
            Console.WriteLine("  [PC v2 record not found]");
        }
        else
        {
            Console.WriteLine($"  PC v2 record at 0x{pcStart:X5}");
            Console.WriteLine("  All INT32[1] SARs (single-value, potential bank/rep/portrait):");

            int scanLimB = Math.Min(raw.Length, pcStart + 32768);
            int si2 = pcStart + 12;
            while (si2 + 13 <= scanLimB)
            {
                if (raw[si2] != 0x01)
                {
                    si2++;
                    continue;
                }
                int eS = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(si2 + 1, 4));
                int eC = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(si2 + 5, 4));
                if (eS is not (1 or 2 or 4 or 8 or 16) || eC is < 1 or > 8192)
                {
                    si2++;
                    continue;
                }
                long dL = (long)eS * eC;
                if (si2 + 13 + dL + 4 > raw.Length || dL > 65536)
                {
                    si2++;
                    continue;
                }
                int bId = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(si2 + 9, 4));
                int bcOff = (int)(si2 + 13 + dL);
                int bc = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(bcOff, 4));
                if (bc is < 0 or > 512)
                {
                    si2++;
                    continue;
                }
                int sarEnd = bcOff + 4 + bc * 4;
                if (sarEnd > scanLimB)
                    break;

                // Emit INT32[1] and INT32[2] SARs with their values
                if (eS == 4 && eC <= 4)
                {
                    var vals = new int[eC];
                    for (int k = 0; k < eC; k++)
                        vals[k] = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                            raw.AsSpan(si2 + 13 + k * 4, 4)
                        );

                    // Decode bitset to get ObjectField bit numbers
                    string bitsStr = "";
                    if (bc > 0 && bcOff + 4 + bc * 4 <= raw.Length)
                    {
                        var setBits2 = new System.Collections.Generic.List<int>();
                        for (int bw = 0; bw < bc; bw++)
                        {
                            uint word = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(
                                raw.AsSpan(bcOff + 4 + bw * 4, 4)
                            );
                            for (int bb = 0; bb < 32; bb++)
                                if ((word & (1u << bb)) != 0)
                                    setBits2.Add(bw * 32 + bb);
                        }
                        bitsStr = " bits=" + string.Join(",", setBits2.Select(b => $"{b}({ObjFName(b)})"));
                    }
                    Console.WriteLine(
                        $"    0x{si2:X5}: bsId=0x{bId:X4} INT32[{eC}]=[{string.Join(",", vals)}]{bitsStr}"
                    );
                }

                si2 = sarEnd;
            }

            // Also specifically look for large INT32[1] (bank money is likely > 0)
            Console.WriteLine("\n  Large single-INT32 values (potential bank money, > 1000):");
            si2 = pcStart + 12;
            while (si2 + 13 <= scanLimB)
            {
                if (raw[si2] != 0x01)
                {
                    si2++;
                    continue;
                }
                int eS = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(si2 + 1, 4));
                int eC = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(si2 + 5, 4));
                if (eS is not (1 or 2 or 4 or 8 or 16) || eC is < 1 or > 8192)
                {
                    si2++;
                    continue;
                }
                long dL = (long)eS * eC;
                if (si2 + 13 + dL + 4 > raw.Length || dL > 65536)
                {
                    si2++;
                    continue;
                }
                int bId = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(si2 + 9, 4));
                int bcOff = (int)(si2 + 13 + dL);
                int bc = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(bcOff, 4));
                if (bc is < 0 or > 512)
                {
                    si2++;
                    continue;
                }
                int sarEnd = bcOff + 4 + bc * 4;
                if (sarEnd > scanLimB)
                    break;

                if (eS == 4 && eC == 1)
                {
                    int v = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(si2 + 13, 4));
                    if (v > 1000 && v < 10_000_000)
                        Console.WriteLine($"    0x{si2:X5}: bsId=0x{bId:X4} INT32[1]={v}  ← large value");
                }
                si2 = sarEnd;
            }
        }
    }
}
