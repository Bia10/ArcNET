using System.Buffers.Binary;

namespace ArcNET.Diagnostics;

public static class CharacterSarDiffService
{
    public static CharacterSarDiffSnapshot Compare(byte[] rawA, byte[] rawB) =>
        Compare(CharacterSarDiagnostics.Parse(rawA), CharacterSarDiagnostics.Parse(rawB), rawA, rawB);

    public static CharacterSarDiffSnapshot Compare(
        IReadOnlyList<CharacterSarEntrySnapshot> sarsA,
        IReadOnlyList<CharacterSarEntrySnapshot> sarsB,
        byte[] rawA,
        byte[] rawB
    )
    {
        var groupA = sarsA
            .Where(static sar => !sar.IsFiller)
            .GroupBy(static sar => sar.Fingerprint)
            .ToDictionary(static group => group.Key, static group => group.ToList());
        var groupB = sarsB
            .Where(static sar => !sar.IsFiller)
            .GroupBy(static sar => sar.Fingerprint)
            .ToDictionary(static group => group.Key, static group => group.ToList());

        var allFingerprints = new HashSet<string>(groupA.Keys);
        allFingerprints.UnionWith(groupB.Keys);

        List<CharacterSarDiffEntrySnapshot> entries = [];
        foreach (var fingerprint in allFingerprints.OrderBy(static fingerprint => fingerprint))
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
                var annotation = CharacterSarDiagnostics.AnnotateSarValue(sarB);

                if (sarA.ElementSize == 4 && sarA.ElementCount == sarB.ElementCount)
                {
                    var changed = CompareInt32Elements(sarA, sarB, rawA, rawB);
                    if (changed.Count > 0)
                    {
                        entries.Add(
                            new CharacterSarDiffEntrySnapshot(
                                CharacterSarDiffKind.ElementValuesChanged,
                                fingerprint,
                                index,
                                commonCount,
                                annotation,
                                sarA.ElementCount,
                                sarB.ElementCount,
                                sarA.ValueSummary,
                                sarB.ValueSummary,
                                changed
                            )
                        );
                    }
                }
                else if (sarA.ElementCount != sarB.ElementCount || sarA.ValueSummary != sarB.ValueSummary)
                {
                    entries.Add(
                        new CharacterSarDiffEntrySnapshot(
                            CharacterSarDiffKind.SummaryChanged,
                            fingerprint,
                            index,
                            commonCount,
                            annotation,
                            sarA.ElementCount,
                            sarB.ElementCount,
                            sarA.ValueSummary,
                            sarB.ValueSummary,
                            []
                        )
                    );
                }
            }

            for (var index = commonCount; index < countB; index++)
            {
                var sar = listB![index];
                entries.Add(
                    new CharacterSarDiffEntrySnapshot(
                        CharacterSarDiffKind.Added,
                        fingerprint,
                        index,
                        countB,
                        CharacterSarDiagnostics.AnnotateSarValue(sar),
                        null,
                        sar.ElementCount,
                        null,
                        sar.ValueSummary,
                        []
                    )
                );
            }

            for (var index = commonCount; index < countA; index++)
            {
                var sar = listA![index];
                entries.Add(
                    new CharacterSarDiffEntrySnapshot(
                        CharacterSarDiffKind.Removed,
                        fingerprint,
                        index,
                        countA,
                        CharacterSarDiagnostics.AnnotateSarValue(sar),
                        sar.ElementCount,
                        null,
                        sar.ValueSummary,
                        null,
                        []
                    )
                );
            }
        }

        return new CharacterSarDiffSnapshot(entries);
    }

    private static IReadOnlyList<CharacterSarElementValueDiffSnapshot> CompareInt32Elements(
        CharacterSarEntrySnapshot entryA,
        CharacterSarEntrySnapshot entryB,
        byte[] rawA,
        byte[] rawB
    )
    {
        if (entryA.ElementSize != 4 || entryB.ElementSize != 4 || entryA.ElementCount != entryB.ElementCount)
            return [];

        List<CharacterSarElementValueDiffSnapshot> changed = [];
        for (var index = 0; index < entryA.ElementCount; index++)
        {
            var valueA = BinaryPrimitives.ReadInt32LittleEndian(rawA.AsSpan(entryA.DataOffset + index * 4, 4));
            var valueB = BinaryPrimitives.ReadInt32LittleEndian(rawB.AsSpan(entryB.DataOffset + index * 4, 4));
            if (valueA != valueB)
                changed.Add(new CharacterSarElementValueDiffSnapshot(index, valueA, valueB));
        }

        return changed;
    }
}
