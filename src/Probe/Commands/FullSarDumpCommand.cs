using System.Buffers.Binary;
using ArcNET.Editor;
using Probe;

namespace Probe.Commands;

internal sealed class FullSarDumpCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var targetSlotArg = args.Length >= 1 ? args[0] : "0013";
        int? bsCntFilter = args.Length >= 2 && int.TryParse(args[1], out var parsedBsCnt) ? parsedBsCnt : null;

        var targetStem = "Slot" + targetSlotArg.PadLeft(4, '0');
        var targetGsi =
            Directory.GetFiles(saveDir, targetStem + "*.gsi").FirstOrDefault()
            ?? throw new FileNotFoundException($"No GSI for {targetStem}");
        var targetSave = SaveGameLoader.Load(
            targetGsi,
            Path.Combine(saveDir, targetStem + ".tfai"),
            Path.Combine(saveDir, targetStem + ".tfaf")
        );

        Console.WriteLine(
            $"\n=== Mode 10: Full SAR dump - {targetStem}  {targetSave.Info.LeaderName} lv={targetSave.Info.LeaderLevel} ==="
        );
        if (bsCntFilter.HasValue)
            Console.WriteLine($"  Filter: bsCnt={bsCntFilter} only");

        var player = SarUtils.FindPlayerRecord(targetSave);
        if (player is null)
        {
            Console.WriteLine("  [No player v2 record found]");
            return Task.CompletedTask;
        }

        var rawBytes = player.RawBytes;
        Console.WriteLine($"  Player: lv={player.Stats[17]} race={player.Stats[27]} RawBytes={rawBytes.Length}B");

        var allSars = SarUtils.ParseSars(rawBytes);
        var sarIndex = 0;
        foreach (var sar in allSars)
        {
            if (!bsCntFilter.HasValue && sar.IsFiller)
                continue;
            if (bsCntFilter.HasValue && sar.BCnt != bsCntFilter.Value)
                continue;

            sarIndex++;
            var fingerprint = sar.Fingerprint;
            var annotation = SarUtils.AnnotateBsId(sar.BsId);
            if (string.IsNullOrEmpty(annotation))
                annotation = SarUtils.AnnotateFingerprint(fingerprint);

            Console.WriteLine(
                $"\n  SAR#{sarIndex} @ 0x{sar.Offset:X5}: bsId=0x{sar.BsId:X4}  eSize={sar.ESize} eCnt={sar.ECnt} bsCnt={sar.BCnt}  fp={fingerprint}  {annotation}"
            );

            if (sar.ESize == 4)
            {
                const int perRow = 16;
                for (var row = 0; row < sar.ECnt; row += perRow)
                {
                    var end = Math.Min(row + perRow, sar.ECnt);
                    Console.Write($"    [{row:D3}..{end - 1:D3}]: ");
                    for (var index = row; index < end; index++)
                    {
                        Console.Write(
                            $"{BinaryPrimitives.ReadInt32LittleEndian(rawBytes.AsSpan(sar.Offset + 13 + index * 4, 4)), 8} "
                        );
                    }

                    Console.WriteLine();
                }
            }
            else if (sar.ESize == 1)
            {
                var strBytes = rawBytes.AsSpan(sar.Offset + 13, sar.ECnt).ToArray();
                Console.WriteLine($"    hex: {Convert.ToHexString(strBytes)}");
                Console.WriteLine(
                    $"    txt: {System.Text.Encoding.ASCII.GetString(strBytes.Where(b => b >= 32 && b < 127).ToArray())}"
                );
            }
            else
            {
                for (var index = 0; index < Math.Min(sar.ECnt, 20); index++)
                {
                    var elem = rawBytes.AsSpan(sar.Offset + 13 + index * sar.ESize, sar.ESize).ToArray();
                    Console.WriteLine($"    [{index:D3}]: {Convert.ToHexString(elem)}");
                }

                if (sar.ECnt > 20)
                    Console.WriteLine($"    ... ({sar.ECnt - 20} more elements)");
            }

            if (sar.BCnt > 0)
            {
                var bcOff = sar.Offset + 13 + sar.ESize * sar.ECnt;
                var setBits = new List<int>();
                for (var wordIndex = 0; wordIndex < sar.BCnt; wordIndex++)
                {
                    var word = BinaryPrimitives.ReadUInt32LittleEndian(rawBytes.AsSpan(bcOff + 4 + wordIndex * 4, 4));
                    for (var bitIndex = 0; bitIndex < 32; bitIndex++)
                    {
                        if ((word & (1u << bitIndex)) != 0)
                            setBits.Add(wordIndex * 32 + bitIndex);
                    }
                }

                if (setBits.Count > 0)
                    Console.WriteLine($"    bitset slots: [{string.Join(",", setBits)}]");
            }
        }

        Console.WriteLine(
            $"\n  Total SARs printed: {sarIndex}"
                + (bsCntFilter.HasValue ? $" (bsCnt={bsCntFilter} filter)" : " (non-filler)")
        );
        return Task.CompletedTask;
    }
}
