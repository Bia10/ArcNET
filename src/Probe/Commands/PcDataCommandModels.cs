using ArcNET.Formats;

namespace Probe.Commands;

internal static class PcDataCommandModels
{
    internal readonly record struct SavFileSnapshot(
        byte[] Bytes,
        int Header0,
        int Header1,
        int TotalInts,
        int TrailingBytes,
        int NonZeroCount,
        int BeefCafeCount,
        int MinusOneCount,
        SaveIdPairTableSnapshot? SaveIdPairs,
        AlignedQuadSummary? QuadSummary,
        Data2SavFile? Data2Sav
    );

    internal readonly record struct AlignedQuadSignature(int B, int C, int D);

    internal readonly record struct AlignedQuadSummary(
        int StartInt,
        int QuadCount,
        int RemainderInts,
        int DistinctSignatures,
        int SectionCount,
        int ZeroSectionCount,
        int LongestZeroSectionStart,
        int LongestZeroSectionLength,
        int FrontMatterRowCount,
        int FrontMatterSectionCount,
        IReadOnlyList<AlignedQuadRunSummary> FrontMatterRuns,
        int TailRowStart,
        int TailRowCount,
        int TailSectionCount,
        IReadOnlyList<AlignedQuadRunSummary> TailRuns,
        IReadOnlyList<AlignedQuadRunSummary> LeadingRuns,
        IReadOnlyList<AlignedQuadRunSummary> TrailingRuns,
        IReadOnlyList<AlignedQuadSignatureSummary> TopSignatures,
        IReadOnlyList<AlignedQuadRunSummary> TopRuns
    );

    internal readonly record struct AlignedQuadSignatureSummary(
        AlignedQuadSignature Signature,
        int Count,
        int FirstRow,
        int LastRow,
        int FirstA,
        int LastA,
        int LongestRunLength,
        int LongestRunStart,
        int LongestRunFirstA,
        int LongestRunLastA
    );

    internal readonly record struct AlignedQuadRunSummary(
        int StartRow,
        int Length,
        AlignedQuadSignature Signature,
        int FirstA,
        int LastA
    );

    internal readonly record struct PlayerStateSnapshot(
        int QuestCount,
        int RumorsCount,
        int Blessings,
        int Curses,
        int Schematics,
        IReadOnlyDictionary<int, int>? Reputation
    );

    internal readonly record struct ContiguousIntWindow(
        int StartInt,
        int RemovedInts,
        int AddedInts,
        int CommonSuffixInts
    );

    internal readonly record struct WindowPattern(int StartInt, int RemovedInts, int AddedInts);

    internal readonly record struct WindowTraceSpec(int StartInt, int Width);

    internal readonly record struct FrontMatterFamilyKey(int RowCount, int SectionCount, string Sequence);

    internal readonly record struct TailFamilyKey(int RowCount, int SectionCount, string Sequence);

    internal readonly record struct Data2RegionFamilyKey(int IntCount, string Sequence, string Preview);

    internal readonly record struct SaveIdPairTableSnapshot(
        int StartInt,
        int PairCount,
        int EndInt,
        int FirstId,
        int LastId,
        int NonZeroPairs,
        int MaxValue,
        IReadOnlyDictionary<int, int> Values
    );

    internal readonly record struct TownMapFogFileSnapshot(byte[] Bytes, int RevealedTiles);

    internal readonly record struct TownMapFogSnapshot(
        int FileCount,
        int RevealedTiles,
        IReadOnlyDictionary<string, TownMapFogFileSnapshot> Files
    );

    internal readonly record struct PcDataSlotSnapshot(
        int Slot,
        string SlotStem,
        string LeaderName,
        int LeaderLevel,
        IReadOnlyDictionary<string, SavFileSnapshot> Files,
        PlayerStateSnapshot? Player,
        TownMapFogSnapshot TownMapFogs
    );

    internal sealed class AlignedQuadSignatureAccumulator
    {
        public AlignedQuadSignatureAccumulator(AlignedQuadSignature signature, int row, int a)
        {
            Signature = signature;
            Count = 1;
            FirstRow = row;
            LastRow = row;
            FirstA = a;
            LastA = a;
            LongestRunLength = 0;
            LongestRunStart = row;
            LongestRunFirstA = a;
            LongestRunLastA = a;
        }

        public AlignedQuadSignature Signature { get; }

        public int Count { get; private set; }

        public int FirstRow { get; }

        public int LastRow { get; private set; }

        public int FirstA { get; }

        public int LastA { get; private set; }

        public int LongestRunLength { get; private set; }

        public int LongestRunStart { get; private set; }

        public int LongestRunFirstA { get; private set; }

        public int LongestRunLastA { get; private set; }

        public void AddRow(int row, int a)
        {
            Count++;
            LastRow = row;
            LastA = a;
        }

        public void RecordRun(int startRow, int length, int firstA, int lastA)
        {
            if (length <= LongestRunLength)
                return;

            LongestRunLength = length;
            LongestRunStart = startRow;
            LongestRunFirstA = firstA;
            LongestRunLastA = lastA;
        }

        public AlignedQuadSignatureSummary ToSummary() =>
            new(
                Signature,
                Count,
                FirstRow,
                LastRow,
                FirstA,
                LastA,
                LongestRunLength,
                LongestRunStart,
                LongestRunFirstA,
                LongestRunLastA
            );
    }
}
