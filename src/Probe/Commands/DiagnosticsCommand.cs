using ArcNET.Core;
using ArcNET.Formats;
using ArcNET.GameObjects;
using Bia.ValueBuffers;
using Probe;

namespace Probe.Commands;

internal sealed class DiagnosticsCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var ctx = SharedProbeContext.Load(saveDir, slot4);

        Console.WriteLine($"\n=== Mode 12: Diagnostics - {ctx.SlotStem} ===");

        Console.WriteLine("\n  Inner files:");
        foreach (var (path, bytes) in ctx.Save.Files.OrderBy(entry => entry.Key))
            Console.WriteLine($"    {bytes.Length, 8:N0}B  {path}");

        Console.WriteLine($"\n  MobileMdy files: {ctx.Save.MobileMdys.Count}");
        foreach (var (mdyPath, mdyFile) in ctx.Save.MobileMdys.OrderBy(entry => entry.Key))
        {
            var v2Count = mdyFile.Records.Count(record => record.IsCharacter);
            var mobCount = mdyFile.Records.Count(record => record.IsMob);
            Console.WriteLine($"    {mdyPath}  records={mdyFile.Records.Count}  v2chars={v2Count}  mobs={mobCount}");
        }

        if (ctx.AllPcs.Count > 0)
        {
            Console.WriteLine($"\n  PC instances: {ctx.AllPcs.Count}");
            var firstPc = ctx.AllPcs[0].data;
            Console.WriteLine(
                $"  PC[0]: bitmap_bits={firstPc.Header.Bitmap.Sum(b => int.PopCount(b))}  props={firstPc.Properties.Count}  propCollItems={firstPc.Header.PropCollectionItems}"
            );
            var goldProp = firstPc.Properties.FirstOrDefault(p => p.Field == ObjectField.ObjFCritterGold);
            Console.WriteLine($"  PC[0] gold: {(goldProp is null ? "absent" : goldProp.GetInt32().ToString())}");
            Console.WriteLine($"  PC[0] bitmap: {ValueBufferText.FormatHex(firstPc.Header.Bitmap)}");
            Console.WriteLine(
                $"  PC[0] props: {ValueBufferText.JoinFormatted(firstPc.Properties, ", ", new PropertyBitFormatter())}"
            );
        }

        Console.WriteLine("\n  mobile.md record types across all maps:");
        var typeCounts = new Dictionary<int, int>();
        foreach (var (_, md) in ctx.Save.MobileMds)
        {
            foreach (var record in md.Records)
            {
                var typeVal = record.Data is not null ? (int)record.Data.Header.GameObjectType : -1;
                typeCounts.TryGetValue(typeVal, out var count);
                typeCounts[typeVal] = count + 1;
            }
        }

        foreach (var (typeVal, count) in typeCounts.OrderBy(entry => entry.Key))
        {
            var typeName = typeVal >= 0 ? ((ObjectType)typeVal).ToString() : "null/unparsed";
            Console.WriteLine($"    type={typeVal} ({typeName}): {count}");
        }

        var playerChar = SarUtils.FindPlayerRecord(ctx.Save);
        if (playerChar is not null)
        {
            Console.WriteLine(
                $"\n  Player v2 record: {playerChar.RawBytes.Length}B  Name={playerChar.Name ?? "(none)"}  lv={playerChar.Stats[17]}"
            );
            var sars = SarUtils.ParseSars(playerChar.RawBytes);
            Console.WriteLine(
                $"  SARs: {sars.Count} total, {sars.Count(sar => !sar.IsFiller)} non-filler, {sars.Count(sar => sar.IsFiller)} filler"
            );
        }
        else
        {
            Console.WriteLine("\n  Player v2 record: not found");
        }

        return Task.CompletedTask;
    }

    private readonly struct PropertyBitFormatter : IValueStringBuilderFormatter<ObjectProperty>
    {
        public void Append(ref ValueStringBuilder builder, ObjectProperty value)
        {
            builder.Append(value.Field);
            builder.Append("(bit=");
            builder.Append((int)value.Field);
            builder.Append(')');
        }
    }
}
