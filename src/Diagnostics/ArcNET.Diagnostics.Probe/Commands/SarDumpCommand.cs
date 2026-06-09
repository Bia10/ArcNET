using ArcNET.Diagnostics;

namespace Probe.Commands;

internal sealed class SarDumpCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var ctx = SharedProbeContext.Load(saveDir, slot4);
        var snapshot = SaveCharacterSarDumpService.Create(ctx.Save);

        Console.WriteLine("\n=== Mode 7: Full SAR dump of v2 PC records ===");
        Console.WriteLine($"  Save: {ctx.SlotStem}  LeaderName={snapshot.LeaderName}  Level={snapshot.LeaderLevel}");

        foreach (var record in snapshot.Records)
        {
            Console.WriteLine(
                $"\n--- V2 character in {record.SourcePath}: HasAll={record.HasCompleteData}  Stats[17]={record.Level} (level)  RawBytes={record.RawBytesLength}B ---"
            );

            Console.WriteLine($"\n  V2 record: {record.SourcePath}  ({record.RawBytesLength}B)");
            var printed = 0;
            foreach (var sar in record.Sars)
            {
                if (sar.IsFiller)
                    continue;

                Console.WriteLine(
                    $"    [0x{sar.BitsetId:X4}] eSize={sar.ElementSize, 2} eCnt={sar.ElementCount, 3} bcCnt={sar.BitsetWordCount, 3}  {sar.Annotation}"
                );
                Console.WriteLine($"             val={sar.ValuePreview}");
                printed++;
            }

            Console.WriteLine($"  Total SAR entries (non-filler): {printed}  total={record.Sars.Count}");

            if (record.Name is { } name)
                Console.WriteLine($"  Name: \"{name}\"");
            Console.WriteLine(
                $"  Gold={record.Gold}  Arrows={record.Arrows}  Bullets={record.Bullets}  PowerCells={record.PowerCells}  TotalKills={record.TotalKills}"
            );
            Console.WriteLine($"  PortraitIndex={record.PortraitIndex}  MaxFollowers={record.MaxFollowers}");
            Console.WriteLine($"  HP damage={record.HpDamage}  Fatigue damage={record.FatigueDamage}");
            Console.WriteLine(
                $"  QuestCount={record.QuestCount}  QuestDataRaw={(record.QuestDataRawBytesLength is null ? "null" : $"{record.QuestDataRawBytesLength}B")}"
            );
            if (record.QuestBitsetWordCount is not null)
                Console.WriteLine($"  Quest slot IDs: {CharacterSarDiagnostics.FormatSlotList(record.QuestSlotIds)}");
            if (record.Reputation.Count > 0)
                Console.WriteLine($"  Reputation[19]: {CharacterSarDiagnostics.FormatInt32List(record.Reputation)}");
            if (record.Blessings.Count > 0)
                Console.WriteLine(
                    $"  Blessings[{record.Blessings.Count}]: {CharacterSarDiagnostics.FormatInt32List(record.Blessings)}  (PcBlessingIdx)"
                );
            if (record.Curses.Count > 0)
                Console.WriteLine(
                    $"  Curses[{record.Curses.Count}]: {CharacterSarDiagnostics.FormatInt32List(record.Curses)}  (PcCurseIdx)"
                );
            if (record.Schematics.Count > 0)
                Console.WriteLine(
                    $"  Schematics[{record.Schematics.Count}]: {CharacterSarDiagnostics.FormatInt32List(record.Schematics)}  (PcSchematicsFoundIdx)"
                );
        }

        return Task.CompletedTask;
    }
}
