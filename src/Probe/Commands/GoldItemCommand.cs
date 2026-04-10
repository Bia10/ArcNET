using System.Buffers.Binary;
using ArcNET.GameObjects;
using Probe;

namespace Probe.Commands;

internal sealed class GoldItemCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var ctx = SharedProbeContext.Load(saveDir, slot4);

        Console.WriteLine("\n=== Mode 8: Gold item v2 SAR dump ===");
        Console.WriteLine(
            $"  Save: {ctx.SlotStem}  LeaderName={ctx.Save.Info.LeaderName}  Level={ctx.Save.Info.LeaderLevel}"
        );

        var pcRawBytes = new List<byte[]>();
        foreach (var (_, mdyFile) in ctx.Save.MobileMdys)
        {
            foreach (var record in mdyFile.Records.Where(record => record.IsCharacter))
                pcRawBytes.Add(record.Character!.RawBytes);
        }

        foreach (var (mdyPath, mdyFile) in ctx.Save.MobileMdys.OrderBy(entry => entry.Key))
        {
            var goldRecords = mdyFile
                .Records.Where(record => record.IsMob && record.Mob!.Header.GameObjectType == ObjectType.Gold)
                .Select(record => record.Mob!)
                .ToList();
            if (goldRecords.Count == 0)
                continue;

            Console.WriteLine($"\n  {mdyPath}: {goldRecords.Count} gold items");
            foreach (var goldRecord in goldRecords)
            {
                var oidBytes = new byte[24];
                BinaryPrimitives.WriteInt16LittleEndian(oidBytes.AsSpan(0, 2), goldRecord.Header.ObjectId.OidType);
                BinaryPrimitives.WriteInt16LittleEndian(oidBytes.AsSpan(2, 2), goldRecord.Header.ObjectId.Padding2);
                BinaryPrimitives.WriteInt32LittleEndian(oidBytes.AsSpan(4, 4), goldRecord.Header.ObjectId.Padding4);
                goldRecord.Header.ObjectId.Id.TryWriteBytes(oidBytes.AsSpan(8, 16));

                var foundInPc = pcRawBytes.Any(pc =>
                {
                    for (var i = 0; i <= pc.Length - 24; i++)
                    {
                        var match = true;
                        for (var j = 0; j < 24; j++)
                        {
                            if (pc[i + j] != oidBytes[j])
                            {
                                match = false;
                                break;
                            }
                        }

                        if (match)
                            return true;
                    }

                    return false;
                });

                var quantityProp = goldRecord.Properties.FirstOrDefault(p =>
                    (int)(byte)p.Field == 97 && p.RawBytes.Length == 4
                );
                var quantity = quantityProp is not null
                    ? BinaryPrimitives.ReadInt32LittleEndian(quantityProp.RawBytes)
                    : -1;
                var hasParent = goldRecord.Properties.Any(p => (int)(byte)p.Field == 65);
                var posProps = goldRecord
                    .Properties.Where(p => p.RawBytes.Length == 4)
                    .Select(p => ((int)(byte)p.Field, BinaryPrimitives.ReadInt32LittleEndian(p.RawBytes)))
                    .Where(x => x.Item2 > 0)
                    .ToList();

                var marker = foundInPc ? "  *** PC GOLD ***" : string.Empty;
                var parentFlag = hasParent ? " [in-inventory]" : string.Empty;
                Console.WriteLine(
                    $"    oid={Convert.ToHexString(oidBytes)}  qty(bit97)={quantity}{parentFlag}  props=[{string.Join(",", posProps.Select(x => $"b{x.Item1}={x.Item2}"))}]{marker}"
                );
            }
        }

        return Task.CompletedTask;
    }
}
