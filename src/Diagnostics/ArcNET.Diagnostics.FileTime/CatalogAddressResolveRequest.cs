using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class CatalogAddressResolveRequest(
    string Key,
    int PreferredRva,
    string Operation,
    string ModuleFileName,
    int ModuleSize,
    byte[] ModuleBytes,
    RuntimeProfileSnapshot RuntimeProfile,
    bool ForceSignatureFallback,
    IReadOnlyDictionary<string, string> SignaturesByNormalizedKey,
    string AddressCacheKey
);
