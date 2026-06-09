using ArcNET.Diagnostics;

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

        SaveSlotLoadSnapshot slotA;
        SaveSlotLoadSnapshot slotB;
        try
        {
            slotA = SaveSlotLoadService.Load(saveDir, args[0]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Cannot load {stemA}: {ex.Message}");
            return Task.CompletedTask;
        }

        try
        {
            slotB = SaveSlotLoadService.Load(saveDir, args[1]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Cannot load {stemB}: {ex.Message}");
            return Task.CompletedTask;
        }

        Console.WriteLine(
            $"\n=== Mode 11: Binary diff  {stemA} ({slotA.Save.Info.LeaderName} lv={slotA.Save.Info.LeaderLevel})  vs  {stemB} ({slotB.Save.Info.LeaderName} lv={slotB.Save.Info.LeaderLevel}) ==="
        );

        var diffs = SaveBinaryDiffService.CompareInnerFiles(slotA.Save.Files, slotB.Save.Files);
        Console.WriteLine(
            $"  Files: {diffs.TotalFiles} total  {diffs.ChangedFileCount} changed  {diffs.IdenticalFileCount} identical"
        );

        var playerA = SavePlayerCharacterResolver.Resolve(slotA.Save)?.Record;
        var playerB = SavePlayerCharacterResolver.Resolve(slotB.Save)?.Record;

        foreach (var diff in diffs.Files)
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

            PrintHexDiff(SaveBinaryDiffService.CreatePreview(diff.Regions, maxRegions: 10));

            if (
                diff.Path.EndsWith("mobile.mdy", StringComparison.OrdinalIgnoreCase)
                && playerA is not null
                && playerB is not null
            )
            {
                Console.WriteLine(
                    $"\n  SAR diff - player v2 record ({playerA.RawBytes.Length}B -> {playerB.RawBytes.Length}B):"
                );
                PrintSarDiff(CharacterSarDiffService.Compare(playerA.RawBytes, playerB.RawBytes));
            }
        }

        return Task.CompletedTask;
    }

    private static void PrintHexDiff(SaveBinaryDiffPreviewSnapshot preview)
    {
        foreach (var region in preview.Regions)
        {
            Console.WriteLine($"  @0x{region.Offset:X5}  ({region.Length}B, {region.ChangedByteCount} changed)");
            foreach (var row in region.Rows)
            {
                Console.WriteLine(
                    $"  {row.AbsoluteOffset:X5}:  {row.BeforeHex} {row.BeforeAscii}  │  {row.AfterHex} {row.AfterAscii}"
                );
            }
        }

        if (preview.OmittedRegionCount > 0)
            Console.WriteLine($"  ... ({preview.OmittedRegionCount} more regions)");
    }

    private static void PrintSarDiff(CharacterSarDiffSnapshot diff)
    {
        if (!diff.HasChanges)
        {
            Console.WriteLine("    (no SAR changes)");
            return;
        }

        foreach (var entry in diff.Entries)
        {
            var indexLabel = entry.OccurrenceCount > 1 ? $"[{entry.OccurrenceIndex}]" : string.Empty;
            var annotationSuffix = !string.IsNullOrEmpty(entry.Annotation) ? $"  {entry.Annotation}" : string.Empty;

            switch (entry.Kind)
            {
                case CharacterSarDiffKind.ElementValuesChanged:
                    Console.WriteLine(
                        $"    ~ {entry.Fingerprint}{indexLabel}{annotationSuffix}  ({entry.ChangedElements.Count} elements changed)"
                    );
                    foreach (var element in entry.ChangedElements)
                        Console.WriteLine(
                            $"        [{element.Index:D2}] {element.BeforeValue, 10} -> {element.AfterValue, 10}"
                        );
                    break;
                case CharacterSarDiffKind.SummaryChanged:
                    Console.WriteLine(
                        $"    ~ {entry.Fingerprint}{indexLabel}{annotationSuffix}  eCnt={entry.BeforeElementCount}->{entry.AfterElementCount}  {entry.BeforeValueSummary} -> {entry.AfterValueSummary}"
                    );
                    break;
                case CharacterSarDiffKind.Added:
                    Console.WriteLine(
                        $"    + {entry.Fingerprint}{indexLabel}{annotationSuffix}  {entry.AfterValueSummary}"
                    );
                    break;
                case CharacterSarDiffKind.Removed:
                    Console.WriteLine(
                        $"    - {entry.Fingerprint}{indexLabel}{annotationSuffix}  {entry.BeforeValueSummary}"
                    );
                    break;
            }
        }
    }
}
