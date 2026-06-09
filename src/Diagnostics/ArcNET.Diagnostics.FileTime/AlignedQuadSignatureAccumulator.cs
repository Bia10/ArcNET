namespace ArcNET.Diagnostics;

internal sealed class AlignedQuadSignatureAccumulator(AlignedQuadSignature signature, int row, int a)
{
    public AlignedQuadSignature Signature { get; } = signature;

    public int Count { get; private set; } = 1;

    public int FirstRow { get; } = row;

    public int LastRow { get; private set; } = row;

    public int FirstA { get; } = a;

    public int LastA { get; private set; } = a;

    public int LongestRunLength { get; private set; }

    public int LongestRunStart { get; private set; } = row;

    public int LongestRunFirstA { get; private set; } = a;

    public int LongestRunLastA { get; private set; } = a;

    public void AddRow(int nextRow, int nextA)
    {
        Count++;
        LastRow = nextRow;
        LastA = nextA;
    }

    public void RecordRun(int startRow, int length, int firstRunA, int lastRunA)
    {
        if (length <= LongestRunLength)
            return;

        LongestRunLength = length;
        LongestRunStart = startRow;
        LongestRunFirstA = firstRunA;
        LongestRunLastA = lastRunA;
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
