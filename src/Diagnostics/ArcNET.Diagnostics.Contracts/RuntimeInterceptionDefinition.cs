namespace ArcNET.Diagnostics;

public readonly record struct RuntimeInterceptionDefinition(
    string Key,
    uint Address,
    uint? Rva,
    string Site,
    int StackCaptureDwordCount,
    RuntimeInterceptionMutation Mutation
);
