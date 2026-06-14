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
        new(
            "discover_world_map_locations",
            "Discover World Map Locations",
            "Loads ArcNET's world-area catalog, marks every known area through area_set_known, and if the traveler is already standing on the world map, walks each anchor with teleport_do before refreshing world-map info.",
            "area_set_known"
        ),
    ];

    private static readonly Dictionary<string, GuidedActionDescriptor> s_actionsByKey = s_actions.ToDictionary(
        static action => action.Key,
        static action => action,
        StringComparer.OrdinalIgnoreCase
    );
}
