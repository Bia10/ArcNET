using ArcNET.Formats;

namespace ArcNET.Diagnostics;

public readonly record struct SaveGlobalFileSnapshot(
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
