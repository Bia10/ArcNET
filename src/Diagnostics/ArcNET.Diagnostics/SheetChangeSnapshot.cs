namespace ArcNET.Diagnostics;

public readonly record struct SheetChangeSnapshot(string Category, string Name, int Before, int After, string? Detail);
