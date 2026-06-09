using System.Buffers.Binary;
using ArcNET.Editor;
using ArcNET.Formats;
using ArcNET.GameObjects;
using Probe;

namespace Probe.Commands;

internal sealed class RawGoldCollCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var ctx = SharedProbeContext.Load(saveDir, slot4);

        Console.WriteLine("\n=== Mode 5: raw-bytes gold=99999 + propCollItems update ===");
        var patchedCount = 0;
        var rawPatched = new Dictionary<string, MobileMdFile>(StringComparer.OrdinalIgnoreCase);
        foreach (var (mdPath, md) in ctx.Save.MobileMds)
        {
            var records = new List<MobileMdRecord>((IEnumerable<MobileMdRecord>)md.Records);
            var modified = false;
            for (var recordIndex = 0; recordIndex < records.Count; recordIndex++)
            {
                var record = records[recordIndex];
                if (
                    !record.IsCompact
                    || record.Data?.Header.GameObjectType != ObjectType.Pc
                    || record.RawMobBytes.Length < 75
                )
                    continue;
                if ((record.RawMobBytes[39] & 0x40) != 0)
                    continue;

                var origBitmap = record.RawMobBytes[30..50];
                var newBitCount = origBitmap.Sum(b => int.PopCount(b)) + 1;
                var oldRaw = record.RawMobBytes;
                var newRaw = new byte[oldRaw.Length + 4];
                oldRaw.AsSpan(0, 71).CopyTo(newRaw);
                BinaryPrimitives.WriteInt32LittleEndian(newRaw.AsSpan(71), 99999);
                oldRaw.AsSpan(71).CopyTo(newRaw.AsSpan(75));
                newRaw[39] |= 0x40;
                BinaryPrimitives.WriteInt16LittleEndian(newRaw.AsSpan(28), (short)newBitCount);
                records[recordIndex] = new MobileMdRecord
                {
                    MapObjectId = record.MapObjectId,
                    Version = record.Version,
                    RawMobBytes = newRaw,
                    Data = null,
                    TailBytes = null,
                    IsCompact = false,
                };
                patchedCount++;
                modified = true;
            }

            rawPatched[mdPath] = modified ? new MobileMdFile { Records = records } : md;
        }

        Console.WriteLine($"  Patched {patchedCount} PC records");
        var (gsiOut, tfaiOut, tfafOut) = SharedProbeContext.GetLegacyOutputPaths(saveDir);
        ArcNET.Editor.SaveGameWriter.Save(
            ctx.Save,
            gsiOut,
            tfaiOut,
            tfafOut,
            new SaveGameUpdates { UpdatedMobileMds = rawPatched }
        );
        SharedProbeContext.CompareBytes($"TFAF vs {ctx.SlotStem}", File.ReadAllBytes(tfafOut), ctx.TfafBytes);
        Console.WriteLine($"  delta={new FileInfo(tfafOut).Length - ctx.TfafBytes.Length}B");

        return Task.CompletedTask;
    }
}
