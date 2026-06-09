using ArcNET.Diagnostics;

namespace Probe.Commands;

internal sealed class FullSarDumpCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var targetSlotArg = args.Length >= 1 ? args[0] : "0013";
        int? bitsetWordCountFilter =
            args.Length >= 2 && int.TryParse(args[1], out var parsedBsCnt) ? parsedBsCnt : null;

        var targetStem = "Slot" + targetSlotArg.PadLeft(4, '0');
        var targetSave = SaveSlotLoadService.Load(saveDir, targetSlotArg).Save;

        Console.WriteLine(
            $"\n=== Mode 10: Full SAR dump - {targetStem}  {targetSave.Info.LeaderName} lv={targetSave.Info.LeaderLevel} ==="
        );
        if (bitsetWordCountFilter.HasValue)
            Console.WriteLine($"  Filter: bsCnt={bitsetWordCountFilter} only");

        var player = SavePlayerCharacterResolver.Resolve(targetSave)?.Record;
        if (player is null)
        {
            Console.WriteLine("  [No player v2 record found]");
            return Task.CompletedTask;
        }

        var rawBytes = player.RawBytes;
        Console.WriteLine($"  Player: lv={player.Stats[17]} race={player.Stats[27]} RawBytes={rawBytes.Length}B");

        var dump = CharacterSarFullDumpService.Create(rawBytes, bitsetWordCountFilter);
        var sarIndex = 0;
        foreach (var sar in dump.Entries)
        {
            sarIndex++;
            Console.WriteLine(
                $"\n  SAR#{sarIndex} @ 0x{sar.Offset:X5}: bsId=0x{sar.BitsetId:X4}  eSize={sar.ElementSize} eCnt={sar.ElementCount} bsCnt={sar.BitsetWordCount}  fp={sar.Fingerprint}  {sar.Annotation}"
            );

            if (sar.ElementSize == 4)
            {
                foreach (var row in sar.Int32Rows)
                {
                    var end = row.StartIndex + row.Values.Count;
                    Console.Write($"    [{row.StartIndex:D3}..{end - 1:D3}]: ");
                    foreach (var value in row.Values)
                    {
                        Console.Write($"{value, 8} ");
                    }

                    Console.WriteLine();
                }
            }
            else if (sar.ElementSize == 1)
            {
                Console.WriteLine($"    hex: {sar.ByteHex}");
                Console.WriteLine($"    txt: {sar.ByteAscii}");
            }
            else
            {
                foreach (var element in sar.ElementHexes)
                {
                    Console.WriteLine($"    [{element.Index:D3}]: {element.Hex}");
                }

                if (sar.OmittedElementCount > 0)
                    Console.WriteLine($"    ... ({sar.OmittedElementCount} more elements)");
            }

            if (sar.BitSlots.Count > 0)
                Console.WriteLine(
                    $"    bitset slots: {CharacterSarDiagnostics.FormatSlotList(sar.BitSlots, int.MaxValue)}"
                );
        }

        Console.WriteLine(
            $"\n  Total SARs printed: {sarIndex}"
                + (bitsetWordCountFilter.HasValue ? $" (bsCnt={bitsetWordCountFilter} filter)" : " (non-filler)")
        );
        return Task.CompletedTask;
    }
}
