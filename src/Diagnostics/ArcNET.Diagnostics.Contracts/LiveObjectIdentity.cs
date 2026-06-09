namespace ArcNET.Diagnostics.Contracts;

public readonly record struct LiveObjectIdentity(
    string HandleHex,
    bool LooksLikeHandle,
    string ResolutionSource,
    int? PoolIndex,
    int? BucketIndex,
    int? SlotIndex,
    string? EntryAddress,
    string? ObjectAddress,
    byte? Status,
    uint? Sequence,
    uint? ExpectedSequence,
    LiveObjectHeader? Header
)
{
    public bool HasHeader => Header is not null;
}
