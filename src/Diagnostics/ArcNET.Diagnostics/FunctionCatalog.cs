using System.Reflection;

namespace ArcNET.Diagnostics;

public static class FunctionCatalog
{
    public static IReadOnlyList<DispatcherCandidateDefinition> DispatcherCandidates => s_dispatcherCandidates;

    public static IReadOnlyList<FunctionDefinition> KnownFunctions => s_functions;

    public static IReadOnlyList<string> KnownFunctionKeys => s_knownFunctionKeys;

    public static bool HasKnownFunction(string token)
    {
        var normalized = CatalogAddressResolver.NormalizeKey(token);
        return normalized.Length != 0 && s_functionsByKey.ContainsKey(normalized);
    }

    public static bool TryGetDefinition(string token, out FunctionDefinition definition)
    {
        var normalized = CatalogAddressResolver.NormalizeKey(token);
        if (normalized.Length != 0 && s_functionsByKey.TryGetValue(normalized, out var resolved))
        {
            definition = resolved;
            return true;
        }

        definition = default;
        return false;
    }

    public static FunctionDefinition GetDefinition(string token)
    {
        if (TryGetDefinition(token, out var definition))
            return definition;

        throw new InvalidOperationException($"Unknown debugger function '{token}'.");
    }

    private static FunctionDefinition[] BuildFunctions() =>
        [
            .. typeof(RuntimeOffsets)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(static field =>
                    field.IsLiteral
                    && field.FieldType == typeof(int)
                    && field.Name.EndsWith("Rva", StringComparison.Ordinal)
                    && !s_excludedFieldNames.Contains(field.Name)
                )
                .Select(CreateFunction)
                .OrderBy(static function => function.Rva),
        ];

    private static FunctionDefinition CreateFunction(FieldInfo field)
    {
        var key = FormatKey(field.Name[..^3]);
        var rva = (int)field.GetRawConstantValue()!;
        var summary = s_summaries.TryGetValue(key, out var text)
            ? text
            : $"Calls {field.Name[..^3]} by module-relative address.";
        var example = s_examples.TryGetValue(key, out var value) ? value : null;
        var suggestedCleanup = s_stdCallKeys.Contains(key) ? StackCleanupMode.StdCall : StackCleanupMode.Cdecl;
        return new FunctionDefinition(
            key,
            rva,
            ModuleAddressFormatter.FormatModuleAddress(RuntimeOffsets.ModuleName, unchecked((uint)rva)),
            summary,
            suggestedCleanup,
            example
        );
    }

    private static string FormatKey(string rawName)
    {
        if (rawName.Length == 0)
            return string.Empty;

        Span<char> buffer = stackalloc char[(rawName.Length * 2) - 1];
        var count = 0;
        for (var index = 0; index < rawName.Length; index++)
        {
            var current = rawName[index];
            var previous = index > 0 ? rawName[index - 1] : '\0';
            var next = index + 1 < rawName.Length ? rawName[index + 1] : '\0';
            if (
                index > 0
                && char.IsUpper(current)
                && (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && char.IsLower(next)))
            )
            {
                buffer[count++] = '_';
            }

            buffer[count++] = char.ToLowerInvariant(current);
        }

        return new string(buffer[..count]);
    }

    private static readonly HashSet<string> s_excludedFieldNames =
    [
        nameof(RuntimeOffsets.ActionPointsRva),
        nameof(RuntimeOffsets.CurrentCharacterSheetIdRva),
        nameof(RuntimeOffsets.ObjPoolBucketsRva),
        nameof(RuntimeOffsets.ObjPoolElementByteSizeRva),
        nameof(RuntimeOffsets.MagicTechRunInfoPointerRva),
        nameof(RuntimeOffsets.QuestGlobalStateGetRva),
    ];

    private static readonly Dictionary<string, string> s_summaries = new(StringComparer.OrdinalIgnoreCase)
    {
        ["level_recalc"] = "Recomputes level-derived player state after stat or progression changes.",
        ["critter_give_xp"] =
            "Awards XP through the engine's native progression path, including difficulty adjustment and local-PC UI refresh.",
        ["stat_base_set"] =
            "Writes one base stat or resource field to an object and triggers downstream recalculation paths.",
        ["stat_base_get"] = "Reads one critter base stat through the engine's native stat accessor.",
        ["ui_show_inven_loot"] = "Opens the loot window between a PC and a loot target.",
        ["ui_start_dialog"] = "Starts a conversation between a PC and an NPC using script coordinates.",
        ["spell_add"] = "Attempts to add one spell to the target actor spellbook.",
        ["spell_remove"] = "Attempts to remove one spell from the target actor spellbook.",
        ["spell_college_level_set"] = "Writes one spell-college mastery level for the target actor.",
        ["tech_learn_schematic"] =
            "Consumes one written schematic object through the engine's native tech-learning path.",
        ["prototype_handle_by_proto_number"] =
            "Resolves one basic prototype number into the engine's canonical prototype handle.",
        ["prototype_handle_by_object_type"] = "Resolves the default prototype handle for one object type.",
        ["obj_array_field_int32_set"] =
            "Writes one signed element in a runtime object array field such as skills or reputations.",
        ["obj_array_field_uint32_set"] =
            "Writes one unsigned element in a runtime object array field such as discovered schematics.",
        ["quest_state_get"] = "Reads one per-PC quest state entry through the engine's native quest resolver.",
        ["script_story_state_set"] = "Writes the global story-state marker used by scripts and progression gates.",
        ["script_story_state_get"] =
            "Reads the current global story-state marker used by scripts and progression gates.",
        ["quest_state_set"] = "Writes one per-PC quest state entry.",
        ["quest_global_state_get"] = "Reads the global state for one quest.",
        ["quest_global_state_set"] = "Writes the global state for one quest.",
        ["reaction_adj"] = "Adjusts one NPC's reaction toward one player character by a signed delta.",
        ["spell_college_level_get"] = "Reads one spell-college mastery level for the target actor.",
        ["wmap_rnd_encounter_check"] =
            "Runs the world-map random-encounter selector/spawn check using the current world-map globals.",
        ["wmap_ui_encounter_start"] =
            "Flags the current world-map session as an encounter and schedules encounter cleanup.",
        ["wmap_load_worldmap_info"] = "Rebuilds the current world-map UI info cache for the active module.",
        ["teleport_do"] =
            "Low-level teleport entrypoint that expects one prepared destination payload. Prefer the guided teleport action when you have X/Y/map inputs.",
        ["combat_turn_based_whos_turn_set"] = "Transfers turn-based ownership to a specific combatant.",
    };

    private static readonly Dictionary<string, string> s_examples = new(StringComparer.OrdinalIgnoreCase)
    {
        ["level_recalc"] = "Function call: level-recalc 0x0000000201234567",
        ["critter_give_xp"] = "Function call: award-xp player 500",
        ["stat_base_set"] = "Function call: stat-base-set 0x0000000201234567 strength 12",
        ["stat_base_get"] = "Function call: stat-base-get player",
        ["ui_show_inven_loot"] = "Function call: loot-window 0x0000000201234567 0x00000002089ABCDE",
        ["ui_start_dialog"] = "Function call: start-dialog 0x0000000201234567 0x00000002089ABCDE 0 0 0",
        ["spell_college_level_set"] = "Function call: spell-college-set player fire 1",
        ["tech_learn_schematic"] = "Function call: learn-schematic-object player 0x00000002089ABCDE",
        ["prototype_handle_by_proto_number"] = "Function call: create-object 14001 480 512",
        ["obj_array_field_int32_set"] = "Function call: skill-set player haggle 1",
        ["obj_array_field_uint32_set"] = "Function call: schematic-set player 0 10079",
        ["quest_state_get"] = "Function call: quest player 42",
        ["script_story_state_set"] = "Function call: story-state-set 12",
        ["script_story_state_get"] = "Function call: story-state",
        ["quest_state_set"] = "Function call: quest-set player 42 accepted",
        ["quest_global_state_get"] = "Function call: quest-global 42",
        ["quest_global_state_set"] = "Function call: quest-global-set 42 completed",
        ["reaction_adj"] = "Function call: reaction-adjust 0x00000002089ABCDE player 10",
        ["spell_college_level_get"] = "Function call: spell-college fire",
        ["teleport_do"] =
            "Prefer the guided 'Teleport Traveler' action in ArcanumDebugger for traveler, X, Y, map, and flags inputs.",
        ["wmap_rnd_encounter_check"] = "Function call: random-encounter-check",
        ["wmap_ui_encounter_start"] = "Function call: worldmap-encounter-start",
        ["wmap_load_worldmap_info"] = "Function call: worldmap-load-info",
    };

    private static readonly HashSet<string> s_stdCallKeys = [];

    private static readonly DispatcherCandidateDefinition[] s_dispatcherCandidates =
    [
        new(
            "tig_window_display",
            RuntimeOffsets.TigWindowDisplayRva,
            CodeCatalog.FormatModuleAddress((uint)RuntimeOffsets.TigWindowDisplayRva)
        ),
        new(
            "gamelib_draw",
            RuntimeOffsets.GamelibDrawRva,
            CodeCatalog.FormatModuleAddress((uint)RuntimeOffsets.GamelibDrawRva)
        ),
        new(
            "tig_video_flip",
            RuntimeOffsets.TigVideoFlipRva,
            CodeCatalog.FormatModuleAddress((uint)RuntimeOffsets.TigVideoFlipRva)
        ),
    ];

    private static readonly FunctionDefinition[] s_functions = BuildFunctions();

    private static readonly string[] s_knownFunctionKeys = [.. s_functions.Select(static function => function.Key)];

    private static readonly Dictionary<string, FunctionDefinition> s_functionsByKey = s_functions.ToDictionary(
        static function => CatalogAddressResolver.NormalizeKey(function.Key),
        static function => function,
        StringComparer.OrdinalIgnoreCase
    );
}
