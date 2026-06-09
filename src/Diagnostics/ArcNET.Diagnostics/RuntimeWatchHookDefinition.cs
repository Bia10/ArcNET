namespace ArcNET.Diagnostics;

public readonly record struct RuntimeWatchHookDefinition(
    RuntimeWatchHookId Id,
    string Key,
    string EventName,
    int Rva,
    string Site,
    string Area,
    string Description
);
