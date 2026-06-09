namespace ArcNET.Diagnostics;

public readonly record struct ObjectFieldDescriptor(
    int FieldId,
    string RawName,
    string DisplayName,
    string CollectionName,
    bool IsNoise
);
