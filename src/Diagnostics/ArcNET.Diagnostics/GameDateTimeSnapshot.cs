namespace ArcNET.Diagnostics;

public readonly record struct GameDateTimeSnapshot(uint Days, uint Milliseconds)
{
    public long SortKey => ((long)Days << 32) | Milliseconds;
}
