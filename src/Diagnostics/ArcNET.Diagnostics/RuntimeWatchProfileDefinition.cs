namespace ArcNET.Diagnostics;

internal readonly record struct RuntimeWatchProfileDefinition(
    string Name,
    string Description,
    RuntimeWatchHookDefinition[] Hooks
);
