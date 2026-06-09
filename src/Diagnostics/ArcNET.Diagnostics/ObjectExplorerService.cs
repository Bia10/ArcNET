using System.Globalization;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public static class ObjectExplorerService
{
    public static ObjectExplorerSnapshot Create(ObjectExplorerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var capabilities = DiagnosticsCapabilityPolicy.Create(request.RuntimeProfile, request.HasModuleSymbols);
        if (!capabilities.Capabilities.HasFlag(DiagnosticsCapability.ReadStructuredState))
        {
            return new ObjectExplorerSnapshot(capabilities, [], [], CreateUnavailableNotes(capabilities));
        }

        var allGroups = BuildGroups();
        IReadOnlyList<ObjectFieldGroupDescriptor> recommendedGroups =
        [
            .. s_recommendedGroupKeys.Select(key => allGroups.First(group => group.Key == key)),
        ];
        var notes = CreateNotes(capabilities, allGroups);
        return new ObjectExplorerSnapshot(capabilities, recommendedGroups, allGroups, notes);
    }

    private static IReadOnlyList<ObjectFieldGroupDescriptor> BuildGroups()
    {
        Dictionary<string, List<ObjectFieldDescriptor>> fieldsByGroup = new(StringComparer.OrdinalIgnoreCase)
        {
            ["core"] = [],
        };

        var currentGroup = "core";
        foreach (var field in ObjectFieldCatalog.Fields)
        {
            if (field.RawName is "OBJ_F_BEGIN" or "OBJ_F_TOTAL_NORMAL" or "OBJ_F_MAX")
                continue;

            if (field.RawName.EndsWith("_BEGIN", StringComparison.Ordinal))
            {
                currentGroup = ToGroupKey(field.RawName);
                fieldsByGroup.TryAdd(currentGroup, []);
                continue;
            }

            if (field.RawName.EndsWith("_END", StringComparison.Ordinal))
            {
                currentGroup = "core";
                continue;
            }

            fieldsByGroup.TryAdd(currentGroup, []);
            fieldsByGroup[currentGroup].Add(field);
        }

        return
        [
            .. fieldsByGroup
                .Where(static entry => entry.Value.Count > 0)
                .Select(CreateGroupDescriptor)
                .OrderBy(static group => GroupSortOrder(group.Key))
                .ThenBy(static group => group.DisplayName, StringComparer.OrdinalIgnoreCase),
        ];
    }

    private static ObjectFieldGroupDescriptor CreateGroupDescriptor(
        KeyValuePair<string, List<ObjectFieldDescriptor>> entry
    ) =>
        new(
            entry.Key,
            DisplayName(entry.Key),
            GroupDescription(entry.Key),
            [.. entry.Value],
            entry.Value.Count(static field => field.IsNoise)
        );

    private static string ToGroupKey(string rawName)
    {
        const string beginSuffix = "_BEGIN";
        const string prefix = "OBJ_F_";

        var trimmed = rawName.StartsWith(prefix, StringComparison.Ordinal) ? rawName[prefix.Length..] : rawName;
        trimmed = trimmed.EndsWith(beginSuffix, StringComparison.Ordinal) ? trimmed[..^beginSuffix.Length] : trimmed;
        return trimmed.Replace('_', '-').ToLowerInvariant();
    }

    private static string DisplayName(string key) =>
        string.Join(
            ' ',
            key.Split('-', StringSplitOptions.RemoveEmptyEntries)
                .Select(static token => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(token))
        );

    private static string GroupDescription(string key) =>
        key switch
        {
            "core" => "Base object identity, art, flags, location, and other shared runtime fields.",
            "container" => "Lock state, inventory lists, and ownership metadata for containers and loot surfaces.",
            "item" => "Item-level weight, worth, spell, and inventory-slot metadata.",
            "weapon" => "Combat stats, damage ranges, ammo usage, and critical-effect metadata for weapons.",
            "armor" => "Armor class and resistance adjustments for equippable protection items.",
            "critter" => "Actor stats, skills, resources, inventory, effects, and teleport state.",
            "pc" => "Player-specific progression, quest, reputation, blessing, curse, and schematic state.",
            "npc" => "NPC AI, faction, reaction, and encounter state.",
            "transient" =>
                "Render/transient runtime caches and internal handles that are usually noisy or engine-owned.",
            _ => $"{DisplayName(key)} object fields.",
        };

    private static int GroupSortOrder(string key) =>
        Array.IndexOf(s_groupSortOrder, key) is var index and >= 0 ? index : int.MaxValue;

    private static IReadOnlyList<string> CreateNotes(
        RuntimeCapabilityReport capabilities,
        IReadOnlyList<ObjectFieldGroupDescriptor> groups
    )
    {
        List<string> notes = [.. capabilities.Warnings];
        notes.Add(
            "Object explorer groups are metadata-first and prepare the future live inspector to bind against stable field vocabulary."
        );

        if (groups.Any(static group => group.NoiseFieldCount > 0))
        {
            notes.Add(
                "Transient/render groups contain noisy fields that should stay collapsed or advanced-only in the default UI."
            );
        }

        notes.Add(
            "The next extraction step is wiring these shared field catalogs into the live object resolver and timeline semantic projections."
        );
        return notes;
    }

    private static IReadOnlyList<string> CreateUnavailableNotes(RuntimeCapabilityReport capabilities)
    {
        List<string> notes = [.. capabilities.Warnings];
        notes.Add("Object exploration stays unavailable until the runtime can safely decode structured state.");
        return notes;
    }

    private static readonly string[] s_recommendedGroupKeys = ["core", "container", "item", "critter", "pc", "npc"];

    private static readonly string[] s_groupSortOrder =
    [
        "core",
        "wall",
        "portal",
        "container",
        "scenery",
        "projectile",
        "item",
        "weapon",
        "ammo",
        "armor",
        "gold",
        "food",
        "scroll",
        "key",
        "key-ring",
        "written",
        "generic",
        "critter",
        "pc",
        "npc",
        "trap",
        "transient",
    ];
}
