using Probe;

namespace Probe.Commands;

internal sealed class SarDumpCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var ctx = SharedProbeContext.Load(saveDir, slot4);

        Console.WriteLine("\n=== Mode 7: Full SAR dump of v2 PC records ===");
        Console.WriteLine(
            $"  Save: {ctx.SlotStem}  LeaderName={ctx.Save.Info.LeaderName}  Level={ctx.Save.Info.LeaderLevel}"
        );

        static void DumpV2RecordSars(byte[] rawBytes, string mapPath)
        {
            Console.WriteLine($"\n  V2 record: {mapPath}  ({rawBytes.Length}B)");
            var sars = SarUtils.ParseSars(rawBytes);
            var printed = 0;
            foreach (var sar in sars)
            {
                if (sar.IsFiller)
                    continue;

                var knownBsId = SarUtils.AnnotateBsId(sar.BsId);
                var tag = string.IsNullOrEmpty(knownBsId) ? SarUtils.AnnotateFingerprint(sar.Fingerprint) : knownBsId;
                if (string.IsNullOrEmpty(tag))
                    tag = "UNKNOWN";

                Console.WriteLine(
                    $"    [0x{sar.BsId:X4}] eSize={sar.ESize, 2} eCnt={sar.ECnt, 3} bcCnt={sar.BCnt, 3}  {tag}"
                );
                Console.WriteLine(
                    $"             val={SarUtils.FormatElements(rawBytes, sar.Offset + 13, sar.ESize, sar.ECnt)}"
                );
                printed++;
            }

            Console.WriteLine($"  Total SAR entries (non-filler): {printed}  total={sars.Count}");
        }

        foreach (var (mdyPath, mdyFile) in ctx.Save.MobileMdys.OrderBy(entry => entry.Key))
        {
            var v2Records = mdyFile.Records.Where(record => record.IsCharacter).ToList();
            if (v2Records.Count == 0)
                continue;

            foreach (var record in v2Records)
            {
                var character = record.Character!;
                Console.WriteLine(
                    $"\n--- V2 character in {mdyPath}: HasAll={character.HasCompleteData}  Stats[17]={character.Stats[17]} (level)  RawBytes={character.RawBytes.Length}B ---"
                );
                DumpV2RecordSars(character.RawBytes, mdyPath);
                if (character.Name is { } name)
                    Console.WriteLine($"  Name: \"{name}\"");
                Console.WriteLine(
                    $"  Gold={character.Gold}  Arrows={character.Arrows}  Bullets={character.Bullets}  PowerCells={character.PowerCells}  TotalKills={character.TotalKills}"
                );
                Console.WriteLine($"  PortraitIndex={character.PortraitIndex}  MaxFollowers={character.MaxFollowers}");
                Console.WriteLine($"  HP damage={character.HpDamage}  Fatigue damage={character.FatigueDamage}");
                Console.WriteLine(
                    $"  QuestCount={character.QuestCount}  QuestDataRaw={(character.QuestDataRaw is null ? "null" : $"{character.QuestDataRaw.Length}B")}"
                );
                if (character.QuestBitsetRaw is { } questBits)
                {
                    var activeSlots = new List<int>();
                    for (var wordIndex = 0; wordIndex < questBits.Length; wordIndex++)
                    {
                        for (var bitIndex = 0; bitIndex < 32; bitIndex++)
                        {
                            if ((questBits[wordIndex] & (1 << bitIndex)) != 0)
                                activeSlots.Add(wordIndex * 32 + bitIndex);
                        }
                    }

                    Console.WriteLine($"  Quest slot IDs: [{string.Join(",", activeSlots)}]");
                }

                if (character.ReputationRaw is { } reputation)
                    Console.WriteLine($"  Reputation[19]: [{string.Join(",", reputation)}]");
                if (character.BlessingRaw is { } blessing)
                {
                    Console.WriteLine(
                        $"  Blessings[{character.BlessingProtoElementCount}]: [{string.Join(",", blessing)}]  (PcBlessingIdx)"
                    );
                }

                if (character.CurseRaw is { } curse)
                {
                    Console.WriteLine(
                        $"  Curses[{character.CurseProtoElementCount}]: [{string.Join(",", curse)}]  (PcCurseIdx)"
                    );
                }

                if (character.SchematicsRaw is { } schematics)
                {
                    Console.WriteLine(
                        $"  Schematics[{character.SchematicsElementCount}]: [{string.Join(",", schematics)}]  (PcSchematicsFoundIdx)"
                    );
                }
            }
        }

        return Task.CompletedTask;
    }
}
