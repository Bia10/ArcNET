namespace ArcNET.Diagnostics;

public readonly record struct RuntimeWatchProfileDescriptor(
    string Key,
    string Description,
    IReadOnlyList<RuntimeWatchHookDefinition> Hooks
);
