using ArcNET.Diagnostics;

namespace ArcNET.Diagnostics;

public sealed record class ObjectFieldGroupDescriptor(
    string Key,
    string DisplayName,
    string Description,
    IReadOnlyList<ObjectFieldDescriptor> Fields,
    int NoiseFieldCount
);
