namespace ArcNET.Editor.Runtime;

/// <summary>Describes one named INT32 field in a live runtime structure.</summary>
public readonly record struct RuntimeFieldDescriptor(string Name, int Offset);
