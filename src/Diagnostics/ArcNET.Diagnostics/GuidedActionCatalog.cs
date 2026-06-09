namespace ArcNET.Diagnostics;

public static class GuidedActionCatalog
{
    public static IReadOnlyList<GuidedActionDescriptor> Actions => s_actions;

    public static bool TryGetDescriptor(string key, out GuidedActionDescriptor descriptor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return s_actionsByKey.TryGetValue(key, out descriptor!);
    }

    public static GuidedActionDescriptor GetDescriptor(string key) =>
        TryGetDescriptor(key, out var descriptor)
            ? descriptor
            : throw new InvalidOperationException($"Unknown guided debugger action '{key}'.");

    private static readonly GuidedActionDescriptor[] s_actions =
    [
        new(
            "teleport_traveler",
            "Teleport Traveler",
            "Moves one traveler to a tile X/Y target with optional map-id and flags, without asking the user to build the engine teleport payload by hand.",
            "teleport_do"
        ),
    ];

    private static readonly Dictionary<string, GuidedActionDescriptor> s_actionsByKey = s_actions.ToDictionary(
        static action => action.Key,
        static action => action,
        StringComparer.OrdinalIgnoreCase
    );
}
