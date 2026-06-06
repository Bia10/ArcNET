using System.Runtime.Versioning;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal enum RuntimeWatchHookId
{
    LevelRecalc = 1,
    UpdateFollowerLevel = 2,
    StatBaseSet = 3,
    BackgroundEducateFollowers = 4,
    UiShowInvenLoot = 5,
    ItemInsert = 6,
    ItemEquipped = 7,
    ItemForceRemove = 8,
    ItemUnequipped = 9,
    ObjectDestroy = 10,
    ObjectScriptExecute = 11,
    UiStartDialog = 12,
    ReactionAdj = 13,
    CritterKill = 14,
}

[SupportedOSPlatform("windows")]
internal readonly record struct RuntimeWatchHookDefinition(
    RuntimeWatchHookId Id,
    string Key,
    string EventName,
    int Rva,
    string Site,
    string Area,
    string Description
);

[SupportedOSPlatform("windows")]
internal readonly record struct RuntimeWatchProfileDefinition(
    string Name,
    string Description,
    RuntimeWatchHookDefinition[] Hooks
);

[SupportedOSPlatform("windows")]
internal static class RuntimeWatchCatalog
{
    public static IReadOnlyList<RuntimeWatchHookDefinition> ResolveSelectors(IEnumerable<string> selectors)
    {
        var resolved = new Dictionary<RuntimeWatchHookId, RuntimeWatchHookDefinition>();
        foreach (var selector in selectors)
        {
            var normalized = Normalize(selector);
            if (normalized.Length == 0)
                continue;

            if (normalized == "all")
            {
                foreach (var definition in HookDefinitions)
                    resolved[definition.Id] = definition;

                continue;
            }

            if (ProfileDefinitions.TryGetValue(normalized, out var profile))
            {
                foreach (var definition in profile.Hooks)
                    resolved[definition.Id] = definition;

                continue;
            }

            if (HookDefinitionsByKey.TryGetValue(normalized, out var hook))
            {
                resolved[hook.Id] = hook;
                continue;
            }

            throw new InvalidOperationException(
                $"Unknown watch selector '{selector}'. Use 'ArcNET.LiveLab watch list' to inspect available hooks."
            );
        }

        return [.. resolved.Values.OrderBy(static hook => (int)hook.Id)];
    }

    public static object DescribeCatalog() =>
        new
        {
            Profiles = ProfileDefinitions
                .OrderBy(static entry => entry.Key)
                .Select(static entry => new
                {
                    entry.Value.Name,
                    entry.Value.Description,
                    Hooks = entry.Value.Hooks.Select(static hook => hook.Key).ToArray(),
                })
                .ToArray(),
            Hooks = HookDefinitions.Select(static hook => new
            {
                hook.Key,
                hook.EventName,
                hook.Site,
                hook.Area,
                hook.Description,
            }),
        };

    public static RuntimeWatchHookDefinition GetDefinition(RuntimeWatchHookId id) => HookDefinitionsById[id];

    private static string Normalize(string value)
    {
        Span<char> buffer = stackalloc char[value.Length];
        var count = 0;
        foreach (var ch in value)
        {
            if (!char.IsLetterOrDigit(ch))
                continue;

            buffer[count++] = char.ToLowerInvariant(ch);
        }

        return new string(buffer[..count]);
    }

    private static RuntimeWatchHookDefinition Hook(RuntimeWatchHookId id) => HookDefinitionsById[id];

    private static RuntimeWatchHookDefinition Hook(
        RuntimeWatchHookId id,
        string key,
        string eventName,
        int rva,
        string area,
        string description
    ) => new(id, key, eventName, rva, $"Arcanum.exe+0x{rva:X8}", area, description);

    private static readonly RuntimeWatchHookDefinition[] HookDefinitions =
    [
        Hook(
            RuntimeWatchHookId.LevelRecalc,
            "level-recalc",
            "LevelRecalc",
            ArcanumRuntimeOffsets.LevelRecalcRva,
            "Progression",
            "Recomputes player level-derived state after progression or stat changes."
        ),
        Hook(
            RuntimeWatchHookId.UpdateFollowerLevel,
            "update-follower-level",
            "UpdateFollowerLevel",
            ArcanumRuntimeOffsets.UpdateFollowerLevelRva,
            "Progression",
            "Synchronizes follower level to the current player level."
        ),
        Hook(
            RuntimeWatchHookId.StatBaseSet,
            "stat-base-set",
            "StatBaseSet",
            ArcanumRuntimeOffsets.StatBaseSetRva,
            "Progression",
            "Writes a base stat, resource, or identity field onto an object."
        ),
        Hook(
            RuntimeWatchHookId.BackgroundEducateFollowers,
            "background-educate-followers",
            "BackgroundEducateFollowers",
            ArcanumRuntimeOffsets.BackgroundEducateFollowersRva,
            "Progression",
            "Applies follower education and training side effects after PC progression."
        ),
        Hook(
            RuntimeWatchHookId.UiShowInvenLoot,
            "ui-show-inven-loot",
            "UiShowInvenLoot",
            ArcanumRuntimeOffsets.UiShowInvenLootRva,
            "Inventory",
            "Opens the loot UI for a target container or lootable object."
        ),
        Hook(
            RuntimeWatchHookId.ItemInsert,
            "item-insert",
            "ItemInsert",
            ArcanumRuntimeOffsets.ItemInsertRva,
            "Inventory",
            "Moves an item into a container, inventory, or owner slot."
        ),
        Hook(
            RuntimeWatchHookId.ItemEquipped,
            "item-equipped",
            "ItemEquipped",
            ArcanumRuntimeOffsets.ItemEquippedRva,
            "Inventory",
            "Moves an item into an equipped slot on an actor."
        ),
        Hook(
            RuntimeWatchHookId.ItemForceRemove,
            "item-force-remove",
            "ItemForceRemove",
            ArcanumRuntimeOffsets.ItemForceRemoveRva,
            "Inventory",
            "Removes an item from its current owner or container without going through the loot UI."
        ),
        Hook(
            RuntimeWatchHookId.ItemUnequipped,
            "item-unequipped",
            "ItemUnequipped",
            ArcanumRuntimeOffsets.ItemUnequippedRva,
            "Inventory",
            "Moves an equipped item out of its worn or wielded slot."
        ),
        Hook(
            RuntimeWatchHookId.ObjectDestroy,
            "object-destroy",
            "ObjectDestroy",
            ArcanumRuntimeOffsets.ObjectDestroyRva,
            "Lifecycle",
            "Destroys an object and enters object cleanup."
        ),
        Hook(
            RuntimeWatchHookId.ObjectScriptExecute,
            "object-script-execute",
            "ObjectScriptExecute",
            ArcanumRuntimeOffsets.ObjectScriptExecuteRva,
            "Scripts",
            "Runs a script attachment point for AI, interaction, combat, inventory, or travel behavior."
        ),
        Hook(
            RuntimeWatchHookId.UiStartDialog,
            "ui-start-dialog",
            "UiStartDialog",
            ArcanumRuntimeOffsets.UiStartDialogRva,
            "Dialog",
            "Starts a conversation between the player and an NPC."
        ),
        Hook(
            RuntimeWatchHookId.ReactionAdj,
            "reaction-adj",
            "ReactionAdj",
            ArcanumRuntimeOffsets.ReactionAdjRva,
            "Disposition",
            "Adjusts an NPC's reaction score toward the player."
        ),
        Hook(
            RuntimeWatchHookId.CritterKill,
            "critter-kill",
            "CritterKill",
            ArcanumRuntimeOffsets.CritterKillRva,
            "Combat",
            "Enters critter death and kill-resolution handling."
        ),
    ];

    private static readonly Dictionary<RuntimeWatchHookId, RuntimeWatchHookDefinition> HookDefinitionsById =
        HookDefinitions.ToDictionary(static hook => hook.Id);

    private static readonly Dictionary<string, RuntimeWatchHookDefinition> HookDefinitionsByKey =
        HookDefinitions.ToDictionary(static hook => Normalize(hook.Key));

    private static readonly Dictionary<string, RuntimeWatchProfileDefinition> ProfileDefinitions = new()
    {
        ["containers"] = new RuntimeWatchProfileDefinition(
            "containers",
            "Loot UI, inventory transfers, equip changes, and destroy events around containers and held items.",
            [
                Hook(RuntimeWatchHookId.UiShowInvenLoot),
                Hook(RuntimeWatchHookId.ItemInsert),
                Hook(RuntimeWatchHookId.ItemForceRemove),
                Hook(RuntimeWatchHookId.ItemEquipped),
                Hook(RuntimeWatchHookId.ItemUnequipped),
                Hook(RuntimeWatchHookId.ObjectDestroy),
            ]
        ),
        ["inventory"] = new RuntimeWatchProfileDefinition(
            "inventory",
            "Tracks item movement into inventories, equipment slots, and loot surfaces.",
            [
                Hook(RuntimeWatchHookId.UiShowInvenLoot),
                Hook(RuntimeWatchHookId.ItemInsert),
                Hook(RuntimeWatchHookId.ItemForceRemove),
                Hook(RuntimeWatchHookId.ItemEquipped),
                Hook(RuntimeWatchHookId.ItemUnequipped),
                Hook(RuntimeWatchHookId.ObjectDestroy),
            ]
        ),
        ["critters"] = new RuntimeWatchProfileDefinition(
            "critters",
            "Tracks script activity, reaction shifts, kill flow, and destroy cleanup for creatures and actors.",
            [
                Hook(RuntimeWatchHookId.ObjectScriptExecute),
                Hook(RuntimeWatchHookId.ReactionAdj),
                Hook(RuntimeWatchHookId.CritterKill),
                Hook(RuntimeWatchHookId.ObjectDestroy),
            ]
        ),
        ["levelup"] = new RuntimeWatchProfileDefinition(
            "levelup",
            "Tracks level recomputation, follower sync, stat writes, and follower education side effects.",
            [
                Hook(RuntimeWatchHookId.LevelRecalc),
                Hook(RuntimeWatchHookId.UpdateFollowerLevel),
                Hook(RuntimeWatchHookId.StatBaseSet),
                Hook(RuntimeWatchHookId.BackgroundEducateFollowers),
            ]
        ),
        ["npcs"] = new RuntimeWatchProfileDefinition(
            "npcs",
            "Tracks NPC script activity, dialog start, disposition changes, kill flow, and destroy cleanup.",
            [
                Hook(RuntimeWatchHookId.ObjectScriptExecute),
                Hook(RuntimeWatchHookId.UiStartDialog),
                Hook(RuntimeWatchHookId.ReactionAdj),
                Hook(RuntimeWatchHookId.CritterKill),
                Hook(RuntimeWatchHookId.ObjectDestroy),
            ]
        ),
        ["progression"] = new RuntimeWatchProfileDefinition(
            "progression",
            "Tracks player and follower progression recalculation and the stat writes that accompany it.",
            [
                Hook(RuntimeWatchHookId.LevelRecalc),
                Hook(RuntimeWatchHookId.UpdateFollowerLevel),
                Hook(RuntimeWatchHookId.StatBaseSet),
                Hook(RuntimeWatchHookId.BackgroundEducateFollowers),
            ]
        ),
        ["actors"] = new RuntimeWatchProfileDefinition(
            "actors",
            "Broad actor-facing probe for scripts, dialog, disposition changes, deaths, and destruction.",
            [
                Hook(RuntimeWatchHookId.ObjectScriptExecute),
                Hook(RuntimeWatchHookId.UiStartDialog),
                Hook(RuntimeWatchHookId.ReactionAdj),
                Hook(RuntimeWatchHookId.CritterKill),
                Hook(RuntimeWatchHookId.ObjectDestroy),
            ]
        ),
        ["scripts"] = new RuntimeWatchProfileDefinition(
            "scripts",
            "Raw script attachment execution across AI, interaction, combat, inventory, and travel.",
            [Hook(RuntimeWatchHookId.ObjectScriptExecute)]
        ),
    };
}
