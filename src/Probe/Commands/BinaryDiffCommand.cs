using System.Buffers.Binary;
using ArcNET.Editor;
using Probe;

namespace Probe.Commands;

internal sealed class BinaryDiffCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("  Usage: probe 11 <slotA> <slotB>");
            return Task.CompletedTask;
        }

        var stemA = "Slot" + args[0].PadLeft(4, '0');
        var stemB = "Slot" + args[1].PadLeft(4, '0');

        LoadedSave saveA;
        LoadedSave saveB;
        try
        {
            var gsiA =
                Directory.GetFiles(saveDir, stemA + "*.gsi").FirstOrDefault() ?? throw new FileNotFoundException(stemA);
            saveA = SaveGameLoader.Load(
                gsiA,
                Path.Combine(saveDir, stemA + ".tfai"),
                Path.Combine(saveDir, stemA + ".tfaf")
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Cannot load {stemA}: {ex.Message}");
            return Task.CompletedTask;
        }

        try
        {
            var gsiB =
                Directory.GetFiles(saveDir, stemB + "*.gsi").FirstOrDefault() ?? throw new FileNotFoundException(stemB);
            saveB = SaveGameLoader.Load(
                gsiB,
                Path.Combine(saveDir, stemB + ".tfai"),
                Path.Combine(saveDir, stemB + ".tfaf")
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Cannot load {stemB}: {ex.Message}");
            return Task.CompletedTask;
        }

        Console.WriteLine(
            $"\n=== Mode 11: Binary diff  {stemA} ({saveA.Info.LeaderName} lv={saveA.Info.LeaderLevel})  vs  {stemB} ({saveB.Info.LeaderName} lv={saveB.Info.LeaderLevel}) ==="
        );

        var diffs = BinaryDiff.CompareInnerFiles(saveA.Files, saveB.Files);
        var totalFiles = saveA.Files.Keys.Union(saveB.Files.Keys, StringComparer.OrdinalIgnoreCase).Count();
        var identicalFiles = totalFiles - diffs.Count;
        Console.WriteLine($"  Files: {totalFiles} total  {diffs.Count} changed  {identicalFiles} identical");

        var playerA = SarUtils.FindPlayerRecord(saveA);
        var playerB = SarUtils.FindPlayerRecord(saveB);

        foreach (var diff in diffs)
        {
            if (diff.OnlyInA)
            {
                Console.WriteLine($"\n  [ONLY IN A] {diff.Path}  ({diff.SizeA}B)");
                continue;
            }
            if (diff.OnlyInB)
            {
                Console.WriteLine($"\n  [ONLY IN B] {diff.Path}  ({diff.SizeB}B)");
                continue;
            }

            Console.WriteLine(
                $"\n  [CHANGED] {diff.Path}  {diff.SizeA}B->{diff.SizeB}B  regions={diff.Regions.Count}  changed_bytes={diff.ChangedByteCount}"
            );

            BinaryDiff.PrintHexDiff(diff.Regions, maxRegions: 10);

            if (
                diff.Path.EndsWith("mobile.mdy", StringComparison.OrdinalIgnoreCase)
                && playerA is not null
                && playerB is not null
            )
            {
                Console.WriteLine(
                    $"\n  SAR diff - player v2 record ({playerA.RawBytes.Length}B -> {playerB.RawBytes.Length}B):"
                );
                DiffV2Sars(
                    SarUtils.ParseSars(playerA.RawBytes),
                    SarUtils.ParseSars(playerB.RawBytes),
                    playerA.RawBytes,
                    playerB.RawBytes
                );
            }
        }

        return Task.CompletedTask;

        static void DiffV2Sars(List<SarEntry> sarsA, List<SarEntry> sarsB, byte[] rawA, byte[] rawB)
        {
            var groupA = sarsA
                .Where(sar => !sar.IsFiller)
                .GroupBy(sar => sar.Fingerprint)
                .ToDictionary(group => group.Key, group => group.ToList());
            var groupB = sarsB
                .Where(sar => !sar.IsFiller)
                .GroupBy(sar => sar.Fingerprint)
                .ToDictionary(group => group.Key, group => group.ToList());

            var allFingerprints = new HashSet<string>(groupA.Keys);
            allFingerprints.UnionWith(groupB.Keys);

            var anyChange = false;
            foreach (var fingerprint in allFingerprints.OrderBy(x => x))
            {
                groupA.TryGetValue(fingerprint, out var listA);
                groupB.TryGetValue(fingerprint, out var listB);
                var countA = listA?.Count ?? 0;
                var countB = listB?.Count ?? 0;
                var commonCount = Math.Min(countA, countB);

                for (var index = 0; index < commonCount; index++)
                {
                    var sarA = listA![index];
                    var sarB = listB![index];
                    var indexLabel = commonCount > 1 ? $"[{index}]" : string.Empty;
                    var annotation = SarUtils.AnnotateSarValue(sarB);
                    var annotationSuffix = !string.IsNullOrEmpty(annotation) ? $"  {annotation}" : string.Empty;

                    if (sarA.ESize == 4 && sarA.ECnt == sarB.ECnt)
                    {
                        var changed = new List<(int Idx, int VA, int VB)>();
                        for (var valueIndex = 0; valueIndex < sarA.ECnt; valueIndex++)
                        {
                            var valueA = BinaryPrimitives.ReadInt32LittleEndian(
                                rawA.AsSpan(sarA.Offset + 13 + valueIndex * 4, 4)
                            );
                            var valueB = BinaryPrimitives.ReadInt32LittleEndian(
                                rawB.AsSpan(sarB.Offset + 13 + valueIndex * 4, 4)
                            );
                            if (valueA != valueB)
                                changed.Add((valueIndex, valueA, valueB));
                        }

                        if (changed.Count > 0)
                        {
                            anyChange = true;
                            Console.WriteLine(
                                $"    ~ {fingerprint}{indexLabel}{annotationSuffix}  ({changed.Count} elements changed)"
                            );
                            foreach (var (valueIndex, valueA, valueB) in changed)
                                Console.WriteLine($"        [{valueIndex:D2}] {valueA, 10} -> {valueB, 10}");
                        }
                    }
                    else if (sarA.ECnt != sarB.ECnt || sarA.ValueSummary != sarB.ValueSummary)
                    {
                        anyChange = true;
                        Console.WriteLine(
                            $"    ~ {fingerprint}{indexLabel}{annotationSuffix}  eCnt={sarA.ECnt}->{sarB.ECnt}  {sarA.ValueSummary} -> {sarB.ValueSummary}"
                        );
                    }
                }

                for (var index = commonCount; index < countB; index++)
                {
                    anyChange = true;
                    var indexLabel = countB > 1 ? $"[{index}]" : string.Empty;
                    var annotation = SarUtils.AnnotateSarValue(listB![index]);
                    var annotationSuffix = !string.IsNullOrEmpty(annotation) ? $"  {annotation}" : string.Empty;
                    Console.WriteLine(
                        $"    + {fingerprint}{indexLabel}{annotationSuffix}  {listB![index].ValueSummary}"
                    );
                }

                for (var index = commonCount; index < countA; index++)
                {
                    anyChange = true;
                    var indexLabel = countA > 1 ? $"[{index}]" : string.Empty;
                    var annotation = SarUtils.AnnotateSarValue(listA![index]);
                    var annotationSuffix = !string.IsNullOrEmpty(annotation) ? $"  {annotation}" : string.Empty;
                    Console.WriteLine(
                        $"    - {fingerprint}{indexLabel}{annotationSuffix}  {listA![index].ValueSummary}"
                    );
                }
            }

            if (!anyChange)
                Console.WriteLine("    (no SAR changes)");
        }
    }
}
