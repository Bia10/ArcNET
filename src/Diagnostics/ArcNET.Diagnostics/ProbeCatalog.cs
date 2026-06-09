using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public static class ProbeCatalog
{
    public static IReadOnlyList<ProbeProfile> Profiles => s_profiles;

    public static string[] ExpandSelectors(IEnumerable<string> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        var results = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token))
                continue;

            if (TryGetProfile(token, out var profile))
            {
                foreach (var selector in profile.Selectors)
                {
                    if (seen.Add(selector))
                        results.Add(selector);
                }

                continue;
            }

            if (seen.Add(token))
                results.Add(token);
        }

        return [.. results];
    }

    public static bool TryGetProfile(string key, out ProbeProfile profile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (s_profilesByKey.TryGetValue(key, out var resolvedProfile))
        {
            profile = resolvedProfile;
            return true;
        }

        profile = null!;
        return false;
    }

    private static readonly ProbeProfile[] s_profiles =
    [
        new(
            "session-core",
            "Session Core",
            "High-signal coverage for a whole play session without scheduler spam or low-level mutation churn.",
            ["session-core"]
        ),
        new(
            "progression-core",
            "Progression Core",
            "Level recalculation, follower education, stat writes, and adjacent character-growth seams.",
            ["progression"]
        ),
        new(
            "inventory-core",
            "Inventory Core",
            "Loot windows, insert/remove/equip flows, and adjacent inventory-facing state changes.",
            ["inventory"]
        ),
        new(
            "world-core",
            "World Core",
            "Map opens, teleports, arrival notifications, and nearby creation events without scheduler flood.",
            ["teleport-do", "map-open-in-game", "timeevent-notify-pc-teleported", "object-create"]
        ),
        new(
            "ui-core",
            "UI Core",
            "Debugger-friendly UI seams for inventory, dialog, and spellbook activity.",
            ["ui-show-inven-loot", "ui-start-dialog", "ui-spell-add", "ui-spell-maintain-add", "ui-spell-maintain-end"]
        ),
        new(
            "combat-core",
            "Combat Core",
            "Turn ownership, combat state, and other tactical flow transitions.",
            ["combat"]
        ),
        new(
            "render-core",
            "Render Core",
            "High-level world-draw and window-stack heartbeat without the full render/presentation firehose.",
            ["render-core"]
        ),
        new(
            "object-create-only",
            "Object Create Only",
            "Single high-signal lifecycle seam for object creation requests.",
            ["object-create-only"]
        ),
        new("movement", "Movement", "Travel transitions plus low-level packed location writes.", ["movement"]),
        new("appearance", "Appearance", "Creation events plus low-level art/color appearance churn.", ["appearance"]),
        new(
            "all-live",
            "All Live",
            "Maximum live-session coverage, suitable for rigorous debugger-style monitoring.",
            ["all"]
        ),
    ];

    private static readonly Dictionary<string, ProbeProfile> s_profilesByKey = s_profiles.ToDictionary(
        static profile => profile.Key,
        static profile => profile,
        StringComparer.OrdinalIgnoreCase
    );
}
