namespace ArcNET.Diagnostics.Contracts;

public readonly record struct LiveObjectInspection(LiveObjectIdentity Identity, IReadOnlyList<LiveObjectDetail> Details)
{
    public bool HasDetails => Details.Count > 0;
}
