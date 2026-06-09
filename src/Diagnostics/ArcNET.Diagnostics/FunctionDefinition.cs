namespace ArcNET.Diagnostics;

public readonly record struct FunctionDefinition(
    string Key,
    int Rva,
    string Site,
    string Summary,
    StackCleanupMode SuggestedCleanup,
    string? Example
);
