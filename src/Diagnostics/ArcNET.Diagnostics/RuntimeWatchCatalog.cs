namespace ArcNET.Diagnostics;

public static class RuntimeWatchCatalog
{
    public static IReadOnlyList<RuntimeWatchHookDefinition> AllHooks => HookDefinitions;

    public static IReadOnlyList<RuntimeWatchProfileDescriptor> Profiles => s_profiles;

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

            if (ProfileDefinitionsByKey.TryGetValue(normalized, out var profile))
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
                $"Unknown watch selector '{selector}'. Use the diagnostics runtime watch presets to inspect available hooks."
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

    public static bool NeedsNameCatalog(IReadOnlyList<RuntimeWatchHookDefinition> hooks) =>
        hooks.Any(static hook => !LightweightResolverHookIds.Contains(hook.Id));

    public static bool UsesHighVolumeHooks(IReadOnlyList<RuntimeWatchHookDefinition> hooks) =>
        hooks.Any(static hook => HighVolumeHookIds.Contains(hook.Id));

    private static string Normalize(string value) => CatalogAddressResolver.NormalizeKey(value);

    private static RuntimeWatchHookDefinition Hook(RuntimeWatchHookId id) => HookDefinitionsById[id];

    private static RuntimeWatchHookDefinition Hook(
        RuntimeWatchHookId id,
        string key,
        string eventName,
        int rva,
        string area,
        string description
    ) =>
        new(
            id,
            key,
            eventName,
            rva,
            ModuleAddressFormatter.FormatModuleAddress(RuntimeOffsets.ModuleName, unchecked((uint)rva)),
            area,
            description
        );

    private static readonly RuntimeWatchHookDefinition[] HookDefinitions =
    [
        Hook(
            RuntimeWatchHookId.LevelRecalc,
            "level-recalc",
            "LevelRecalc",
            RuntimeOffsets.LevelRecalcRva,
            "Progression",
            "Recomputes player level-derived state after progression or stat changes."
        ),
        Hook(
            RuntimeWatchHookId.UpdateFollowerLevel,
            "update-follower-level",
            "UpdateFollowerLevel",
            RuntimeOffsets.UpdateFollowerLevelRva,
            "Progression",
            "Synchronizes follower level to the current player level."
        ),
        Hook(
            RuntimeWatchHookId.StatBaseSet,
            "stat-base-set",
            "StatBaseSet",
            RuntimeOffsets.StatBaseSetRva,
            "Progression",
            "Writes a base stat, resource, or identity field onto an object."
        ),
        Hook(
            RuntimeWatchHookId.BackgroundEducateFollowers,
            "background-educate-followers",
            "BackgroundEducateFollowers",
            RuntimeOffsets.BackgroundEducateFollowersRva,
            "Progression",
            "Applies follower education and training side effects after PC progression."
        ),
        Hook(
            RuntimeWatchHookId.BackgroundSet,
            "background-set",
            "BackgroundSet",
            RuntimeOffsets.BackgroundSetRva,
            "Progression",
            "Assigns a player background and applies its runtime state changes."
        ),
        Hook(
            RuntimeWatchHookId.BackgroundClear,
            "background-clear",
            "BackgroundClear",
            RuntimeOffsets.BackgroundClearRva,
            "Progression",
            "Removes the current player background and clears its runtime state."
        ),
        Hook(
            RuntimeWatchHookId.UiShowInvenLoot,
            "ui-show-inven-loot",
            "UiShowInvenLoot",
            RuntimeOffsets.UiShowInvenLootRva,
            "Inventory",
            "Opens the loot UI for a target container or lootable object."
        ),
        Hook(
            RuntimeWatchHookId.ItemInsert,
            "item-insert",
            "ItemInsert",
            RuntimeOffsets.ItemInsertRva,
            "Inventory",
            "Moves an item into a container, inventory, or owner slot."
        ),
        Hook(
            RuntimeWatchHookId.ItemEquipped,
            "item-equipped",
            "ItemEquipped",
            RuntimeOffsets.ItemEquippedRva,
            "Inventory",
            "Moves an item into an equipped slot on an actor."
        ),
        Hook(
            RuntimeWatchHookId.ItemForceRemove,
            "item-force-remove",
            "ItemForceRemove",
            RuntimeOffsets.ItemForceRemoveRva,
            "Inventory",
            "Removes an item from its current owner or container without going through the loot UI."
        ),
        Hook(
            RuntimeWatchHookId.ItemUnequipped,
            "item-unequipped",
            "ItemUnequipped",
            RuntimeOffsets.ItemUnequippedRva,
            "Inventory",
            "Moves an equipped item out of its worn or wielded slot."
        ),
        Hook(
            RuntimeWatchHookId.ObjectCreate,
            "object-create",
            "ObjectCreate",
            RuntimeOffsets.ObjectCreateRva,
            "Lifecycle",
            "Requests creation of a new runtime object from a prototype at a location."
        ),
        Hook(
            RuntimeWatchHookId.ObjectDestroy,
            "object-destroy",
            "ObjectDestroy",
            RuntimeOffsets.ObjectDestroyRva,
            "Lifecycle",
            "Destroys an object and enters object cleanup."
        ),
        Hook(
            RuntimeWatchHookId.ObjFieldInt32Set,
            "obj-field-int32-set",
            "ObjFieldInt32Set",
            RuntimeOffsets.ObjFieldInt32SetRva,
            "Mutation",
            "Writes one 32-bit object field through the generic object property store."
        ),
        Hook(
            RuntimeWatchHookId.ObjFieldInt64Set,
            "obj-field-int64-set",
            "ObjFieldInt64Set",
            RuntimeOffsets.ObjFieldInt64SetRva,
            "Mutation",
            "Writes one 64-bit object field such as locations and teleport destinations."
        ),
        Hook(
            RuntimeWatchHookId.ObjFieldHandleSet,
            "obj-field-handle-set",
            "ObjFieldHandleSet",
            RuntimeOffsets.ObjFieldHandleSetRva,
            "Mutation",
            "Writes one handle-backed object field such as parents, leaders, and prototype references."
        ),
        Hook(
            RuntimeWatchHookId.ObjArrayFieldInt32Set,
            "obj-arrayfield-int32-set",
            "ObjArrayFieldInt32Set",
            RuntimeOffsets.ObjArrayFieldInt32SetRva,
            "Mutation",
            "Writes one 32-bit object array element such as stats, skills, or resistances."
        ),
        Hook(
            RuntimeWatchHookId.ObjArrayFieldUInt32Set,
            "obj-arrayfield-uint32-set",
            "ObjArrayFieldUInt32Set",
            RuntimeOffsets.ObjArrayFieldUInt32SetRva,
            "Mutation",
            "Writes one unsigned 32-bit object array element such as spell-tech state, effects, or blessings."
        ),
        Hook(
            RuntimeWatchHookId.ObjArrayFieldInt64Set,
            "obj-arrayfield-int64-set",
            "ObjArrayFieldInt64Set",
            RuntimeOffsets.ObjArrayFieldInt64SetRva,
            "Mutation",
            "Writes one 64-bit object array element such as blessing timestamps and packed runtime datetimes."
        ),
        Hook(
            RuntimeWatchHookId.ObjArrayFieldObjSet,
            "obj-arrayfield-obj-set",
            "ObjArrayFieldObjSet",
            RuntimeOffsets.ObjArrayFieldObjSetRva,
            "Mutation",
            "Writes one handle-backed object array element such as followers or inventory lists."
        ),
        Hook(
            RuntimeWatchHookId.ObjArrayFieldScriptSet,
            "obj-arrayfield-script-set",
            "ObjArrayFieldScriptSet",
            RuntimeOffsets.ObjArrayFieldScriptSetRva,
            "ScriptState",
            "Writes one structured script attachment record containing script number, flags, and local counters."
        ),
        Hook(
            RuntimeWatchHookId.ObjArrayFieldPcQuestSet,
            "obj-arrayfield-pc-quest-set",
            "ObjArrayFieldPcQuestSet",
            RuntimeOffsets.ObjArrayFieldPcQuestSetRva,
            "Quest",
            "Writes one structured per-PC quest-state record containing raw state and quest timestamp."
        ),
        Hook(
            RuntimeWatchHookId.ObjArrayFieldLengthSet,
            "obj-arrayfield-length-set",
            "ObjArrayFieldLengthSet",
            RuntimeOffsets.ObjArrayFieldLengthSetRva,
            "Mutation",
            "Resizes one object array field after additions, removals, or cleanup compaction."
        ),
        Hook(
            RuntimeWatchHookId.ObjectScriptExecute,
            "object-script-execute",
            "ObjectScriptExecute",
            RuntimeOffsets.ObjectScriptExecuteRva,
            "Scripts",
            "Runs a script attachment point for AI, interaction, combat, inventory, or travel behavior."
        ),
        Hook(
            RuntimeWatchHookId.UiStartDialog,
            "ui-start-dialog",
            "UiStartDialog",
            RuntimeOffsets.UiStartDialogRva,
            "Dialog",
            "Starts a conversation between the player and an NPC."
        ),
        Hook(
            RuntimeWatchHookId.UiSpellAdd,
            "ui-spell-add",
            "UiSpellAdd",
            RuntimeOffsets.UiSpellAddRva,
            "Magic",
            "Adds one spell entry to the runtime spell UI for the owning actor."
        ),
        Hook(
            RuntimeWatchHookId.UiSpellMaintainAdd,
            "ui-spell-maintain-add",
            "UiSpellMaintainAdd",
            RuntimeOffsets.UiSpellMaintainAddRva,
            "Magic",
            "Begins tracking one maintained spell or tech effect in the runtime UI."
        ),
        Hook(
            RuntimeWatchHookId.UiSpellMaintainEnd,
            "ui-spell-maintain-end",
            "UiSpellMaintainEnd",
            RuntimeOffsets.UiSpellMaintainEndRva,
            "Magic",
            "Stops tracking one maintained spell or tech effect in the runtime UI."
        ),
        Hook(
            RuntimeWatchHookId.SpellAdd,
            "spell-add",
            "SpellAdd",
            RuntimeOffsets.SpellAddRva,
            "Magic",
            "Attempts to add one spell to an actor spellbook and adjust magick aptitude state."
        ),
        Hook(
            RuntimeWatchHookId.SpellRemove,
            "spell-remove",
            "SpellRemove",
            RuntimeOffsets.SpellRemoveRva,
            "Magic",
            "Attempts to remove one spell from an actor spellbook and refund its magick-point cost."
        ),
        Hook(
            RuntimeWatchHookId.SpellCollegeLevelSet,
            "spell-college-level-set",
            "SpellCollegeLevelSet",
            RuntimeOffsets.SpellCollegeLevelSetRva,
            "Magic",
            "Commits one spell-college level change after spellbook validation has already succeeded."
        ),
        Hook(
            RuntimeWatchHookId.TechLearnSchematic,
            "tech-learn-schematic",
            "TechLearnSchematic",
            RuntimeOffsets.TechLearnSchematicRva,
            "Progression",
            "Learns one written schematic through the native technology progression path."
        ),
        Hook(
            RuntimeWatchHookId.EffectAdd,
            "effect-add",
            "EffectAdd",
            RuntimeOffsets.EffectAddRva,
            "Effect",
            "Commits one critter effect with a specific cause and triggers derived-state recalculation."
        ),
        Hook(
            RuntimeWatchHookId.CurseAdd,
            "curse-add",
            "CurseAdd",
            RuntimeOffsets.CurseAddRva,
            "Effect",
            "Adds one curse entry to a player character through the native curse system."
        ),
        Hook(
            RuntimeWatchHookId.CurseRemove,
            "curse-remove",
            "CurseRemove",
            RuntimeOffsets.CurseRemoveRva,
            "Effect",
            "Removes one curse entry from a player character through the native curse system."
        ),
        Hook(
            RuntimeWatchHookId.BlessAdd,
            "bless-add",
            "BlessAdd",
            RuntimeOffsets.BlessAddRva,
            "Effect",
            "Adds one blessing entry to a player character through the native blessing system."
        ),
        Hook(
            RuntimeWatchHookId.BlessRemove,
            "bless-remove",
            "BlessRemove",
            RuntimeOffsets.BlessRemoveRva,
            "Effect",
            "Removes one blessing entry from a player character through the native blessing system."
        ),
        Hook(
            RuntimeWatchHookId.EffectRemoveOneTyped,
            "effect-remove-one-typed",
            "EffectRemoveOneTyped",
            RuntimeOffsets.EffectRemoveOneTypedRva,
            "Effect",
            "Requests removal of the first matching critter effect of one type."
        ),
        Hook(
            RuntimeWatchHookId.EffectRemoveAllTyped,
            "effect-remove-all-typed",
            "EffectRemoveAllTyped",
            RuntimeOffsets.EffectRemoveAllTypedRva,
            "Effect",
            "Requests removal of every matching critter effect of one type."
        ),
        Hook(
            RuntimeWatchHookId.EffectRemoveOneCausedBy,
            "effect-remove-one-caused-by",
            "EffectRemoveOneCausedBy",
            RuntimeOffsets.EffectRemoveOneCausedByRva,
            "Effect",
            "Requests removal of the first critter effect associated with one effect cause."
        ),
        Hook(
            RuntimeWatchHookId.EffectRemoveAllCausedBy,
            "effect-remove-all-caused-by",
            "EffectRemoveAllCausedBy",
            RuntimeOffsets.EffectRemoveAllCausedByRva,
            "Effect",
            "Requests removal of every critter effect associated with one effect cause."
        ),
        Hook(
            RuntimeWatchHookId.EffectRemoveInternal,
            "effect-remove-internal",
            "EffectRemoveInternal",
            RuntimeOffsets.EffectRemoveInternalRva,
            "Effect",
            "Commits removal of one critter effect slot after a typed or cause-based match has already succeeded."
        ),
        Hook(
            RuntimeWatchHookId.ReactionAdj,
            "reaction-adj",
            "ReactionAdj",
            RuntimeOffsets.ReactionAdjRva,
            "Disposition",
            "Adjusts an NPC's reaction score toward the player."
        ),
        Hook(
            RuntimeWatchHookId.ReputationAdd,
            "reputation-add",
            "ReputationAdd",
            RuntimeOffsets.ReputationAddRva,
            "Disposition",
            "Adds one reputation entry to a player character."
        ),
        Hook(
            RuntimeWatchHookId.ReputationRemove,
            "reputation-remove",
            "ReputationRemove",
            RuntimeOffsets.ReputationRemoveRva,
            "Disposition",
            "Removes one reputation entry from a player character."
        ),
        Hook(
            RuntimeWatchHookId.CritterKill,
            "critter-kill",
            "CritterKill",
            RuntimeOffsets.CritterKillRva,
            "Combat",
            "Enters critter death and kill-resolution handling."
        ),
        Hook(
            RuntimeWatchHookId.CritterGiveXp,
            "critter-give-xp",
            "CritterGiveXp",
            RuntimeOffsets.CritterGiveXpRva,
            "Progression",
            "Awards experience through the native critter progression routine."
        ),
        Hook(
            RuntimeWatchHookId.LogbookAddKill,
            "logbook-add-kill",
            "LogbookAddKill",
            RuntimeOffsets.LogbookAddKillRva,
            "Combat",
            "Records one NPC kill in the player logbook."
        ),
        Hook(
            RuntimeWatchHookId.LogbookAddInjury,
            "logbook-add-injury",
            "LogbookAddInjury",
            RuntimeOffsets.LogbookAddInjuryRva,
            "Combat",
            "Records one injury-history entry in the player logbook."
        ),
        Hook(
            RuntimeWatchHookId.ScriptGlobalVarSet,
            "script-global-var-set",
            "ScriptGlobalVarSet",
            RuntimeOffsets.ScriptGlobalVarSetRva,
            "ScriptState",
            "Writes one script-global integer variable."
        ),
        Hook(
            RuntimeWatchHookId.ScriptGlobalFlagSet,
            "script-global-flag-set",
            "ScriptGlobalFlagSet",
            RuntimeOffsets.ScriptGlobalFlagSetRva,
            "ScriptState",
            "Writes one script-global bit flag."
        ),
        Hook(
            RuntimeWatchHookId.ScriptPcVarSet,
            "script-pc-var-set",
            "ScriptPcVarSet",
            RuntimeOffsets.ScriptPcVarSetRva,
            "ScriptState",
            "Writes one per-PC script variable."
        ),
        Hook(
            RuntimeWatchHookId.ScriptPcFlagSet,
            "script-pc-flag-set",
            "ScriptPcFlagSet",
            RuntimeOffsets.ScriptPcFlagSetRva,
            "ScriptState",
            "Writes one per-PC script flag."
        ),
        Hook(
            RuntimeWatchHookId.ScriptLocalFlagSet,
            "script-local-flag-set",
            "ScriptLocalFlagSet",
            RuntimeOffsets.ScriptLocalFlagSetRva,
            "ScriptState",
            "Writes one local script-header flag on an attached object script."
        ),
        Hook(
            RuntimeWatchHookId.ScriptLocalCounterSet,
            "script-local-counter-set",
            "ScriptLocalCounterSet",
            RuntimeOffsets.ScriptLocalCounterSetRva,
            "ScriptState",
            "Writes one local script-header counter byte on an attached object script."
        ),
        Hook(
            RuntimeWatchHookId.ScriptStoryStateSet,
            "script-story-state-set",
            "ScriptStoryStateSet",
            RuntimeOffsets.ScriptStoryStateSetRva,
            "Quest",
            "Requests an advance of the global story-state marker."
        ),
        Hook(
            RuntimeWatchHookId.QuestStateSet,
            "quest-state-set",
            "QuestStateSet",
            RuntimeOffsets.QuestStateSetRva,
            "Quest",
            "Advances one player-character quest state and triggers its side effects."
        ),
        Hook(
            RuntimeWatchHookId.QuestGlobalStateSet,
            "quest-global-state-set",
            "QuestGlobalStateSet",
            RuntimeOffsets.QuestGlobalStateSetRva,
            "Quest",
            "Writes one global quest-state gate."
        ),
        Hook(
            RuntimeWatchHookId.RumorQstateSet,
            "rumor-qstate-set",
            "RumorQstateSet",
            RuntimeOffsets.RumorQstateSetRva,
            "Quest",
            "Writes one global rumor quelled-state gate."
        ),
        Hook(
            RuntimeWatchHookId.RumorKnownSet,
            "rumor-known-set",
            "RumorKnownSet",
            RuntimeOffsets.RumorKnownSetRva,
            "Quest",
            "Marks one rumor as known for a player character."
        ),
        Hook(
            RuntimeWatchHookId.TimeEventAddDelay,
            "timeevent-add-delay",
            "TimeEventAddDelay",
            RuntimeOffsets.TimeEventAddDelayRva,
            "Scheduler",
            "Schedules a runtime timeevent after a requested delay."
        ),
        Hook(
            RuntimeWatchHookId.TimeEventNotifyPcTeleported,
            "timeevent-notify-pc-teleported",
            "TimeEventNotifyPcTeleported",
            RuntimeOffsets.TimeEventNotifyPcTeleportedRva,
            "World",
            "Breaks map-bound runtime nodes while preparing a teleport or map transition."
        ),
        Hook(
            RuntimeWatchHookId.MapOpenInGame,
            "map-open-in-game",
            "MapOpenInGame",
            RuntimeOffsets.MapOpenInGameRva,
            "World",
            "Loads one in-game map and applies the surrounding transition pipeline."
        ),
        Hook(
            RuntimeWatchHookId.AreaSetKnown,
            "area-set-known",
            "AreaSetKnown",
            RuntimeOffsets.AreaSetKnownRva,
            "World",
            "Marks one world-map area as known for a player character."
        ),
        Hook(
            RuntimeWatchHookId.AreaResetLastKnownArea,
            "area-reset-last-known",
            "AreaResetLastKnownArea",
            RuntimeOffsets.AreaResetLastKnownAreaRva,
            "World",
            "Clears the last-known world-map area marker for a player character."
        ),
        Hook(
            RuntimeWatchHookId.TeleportDo,
            "teleport-do",
            "TeleportDo",
            RuntimeOffsets.TeleportDoRva,
            "World",
            "Requests a teleport or map transition using a populated teleport descriptor."
        ),
        Hook(
            RuntimeWatchHookId.WmapRndEncounterCheck,
            "wmap-rnd-encounter-check",
            "WmapRndEncounterCheck",
            RuntimeOffsets.WmapRndEncounterCheckRva,
            "World",
            "Runs the world-map random-encounter selector and spawn check."
        ),
        Hook(
            RuntimeWatchHookId.WmapUiEncounterStart,
            "wmap-ui-encounter-start",
            "WmapUiEncounterStart",
            RuntimeOffsets.WmapUiEncounterStartRva,
            "World",
            "Flags the current world-map session as an encounter and schedules follow-up cleanup."
        ),
        Hook(
            RuntimeWatchHookId.WmapLoadWorldmapInfo,
            "wmap-load-worldmap-info",
            "WmapLoadWorldmapInfo",
            RuntimeOffsets.WmapLoadWorldmapInfoRva,
            "World",
            "Rebuilds the active world-map runtime info block for the current module."
        ),
        Hook(
            RuntimeWatchHookId.CombatTurnBasedWhosTurnSet,
            "combat-turn-based-whos-turn-set",
            "CombatTurnBasedWhosTurnSet",
            RuntimeOffsets.CombatTurnBasedWhosTurnSetRva,
            "Combat",
            "Hands the active turn-based combat turn to one critter."
        ),
        Hook(
            RuntimeWatchHookId.GamelibInvalidateRect,
            "gamelib-invalidate-rect",
            "GamelibInvalidateRect",
            RuntimeOffsets.GamelibInvalidateRectRva,
            "Render",
            "Invalidates one game-library dirty rectangle before the next world render pass."
        ),
        Hook(
            RuntimeWatchHookId.GamelibDraw,
            "gamelib-draw",
            "GamelibDraw",
            RuntimeOffsets.GamelibDrawRva,
            "Render",
            "Begins one world render pass from the accumulated game-library dirty-rect queue."
        ),
        Hook(
            RuntimeWatchHookId.GamelibDrawGame,
            "gamelib-draw-game",
            "GamelibDrawGame",
            RuntimeOffsets.GamelibDrawGameRva,
            "Render",
            "Runs the main in-game world draw pipeline over one prepared draw-info packet."
        ),
        Hook(
            RuntimeWatchHookId.LightDraw,
            "light-draw",
            "LightDraw",
            RuntimeOffsets.LightDrawRva,
            "Render",
            "Executes the lighting pass for one world frame."
        ),
        Hook(
            RuntimeWatchHookId.TileDraw,
            "tile-draw",
            "TileDraw",
            RuntimeOffsets.TileDrawRva,
            "Render",
            "Executes the terrain and tile pass for one world frame."
        ),
        Hook(
            RuntimeWatchHookId.ObjectHoverDraw,
            "object-hover-draw",
            "ObjectHoverDraw",
            RuntimeOffsets.ObjectHoverDrawRva,
            "Render",
            "Executes the object-hover or highlight overlay pass inside world rendering."
        ),
        Hook(
            RuntimeWatchHookId.ObjectDraw,
            "object-draw",
            "ObjectDraw",
            RuntimeOffsets.ObjectDrawRva,
            "Render",
            "Executes the object draw pass for critters, scenery, items, and other runtime objects."
        ),
        Hook(
            RuntimeWatchHookId.RoofDraw,
            "roof-draw",
            "RoofDraw",
            RuntimeOffsets.RoofDrawRva,
            "Render",
            "Executes the roof overlay pass for one world frame."
        ),
        Hook(
            RuntimeWatchHookId.TextBubbleDraw,
            "text-bubble-draw",
            "TextBubbleDraw",
            RuntimeOffsets.TextBubbleDrawRva,
            "Render",
            "Executes the text-bubble pass for speech bubbles and diegetic labels."
        ),
        Hook(
            RuntimeWatchHookId.TextFloaterDraw,
            "text-floater-draw",
            "TextFloaterDraw",
            RuntimeOffsets.TextFloaterDrawRva,
            "Render",
            "Executes the floating-text pass for combat text and transient world labels."
        ),
        Hook(
            RuntimeWatchHookId.TextConversationDraw,
            "text-conversation-draw",
            "TextConversationDraw",
            RuntimeOffsets.TextConversationDrawRva,
            "Render",
            "Executes the conversation-text pass layered over the world frame."
        ),
        Hook(
            RuntimeWatchHookId.TigWindowDisplay,
            "tig-window-display",
            "TigWindowDisplay",
            RuntimeOffsets.TigWindowDisplayRva,
            "Presentation",
            "Begins TIG dirty-rect presentation across the current window stack."
        ),
        Hook(
            RuntimeWatchHookId.TigWindowComposeDirtyRect,
            "tig-window-compose-dirty-rect",
            "TigWindowComposeDirtyRect",
            RuntimeOffsets.TigWindowComposeDirtyRectRva,
            "Presentation",
            "Composes one dirty rectangle through the TIG window stack into a destination buffer or the primary frame."
        ),
        Hook(
            RuntimeWatchHookId.TigWindowBlitArt,
            "tig-window-blit-art",
            "TigWindowBlitArt",
            RuntimeOffsets.TigWindowBlitArtRva,
            "Presentation",
            "Blits one art resource into a TIG window using the supplied art-blit descriptor."
        ),
        Hook(
            RuntimeWatchHookId.TigWindowCopyFromVBuffer,
            "tig-window-copy-from-vbuffer",
            "TigWindowCopyFromVBuffer",
            RuntimeOffsets.TigWindowCopyFromVBufferRva,
            "Presentation",
            "Copies pixels from one video buffer into a TIG window and invalidates the affected destination rect."
        ),
        Hook(
            RuntimeWatchHookId.TigWindowInvalidateRect,
            "tig-window-invalidate-rect",
            "TigWindowInvalidateRect",
            RuntimeOffsets.TigWindowInvalidateRectRva,
            "Presentation",
            "Queues one TIG dirty rectangle for later composition and presentation."
        ),
        Hook(
            RuntimeWatchHookId.TigVideoFlip,
            "tig-video-flip",
            "TigVideoFlip",
            RuntimeOffsets.TigVideoFlipRva,
            "Presentation",
            "Uploads the current software frame to the SDL texture and presents it."
        ),
    ];

    private static readonly Dictionary<RuntimeWatchHookId, RuntimeWatchHookDefinition> HookDefinitionsById =
        HookDefinitions.ToDictionary(static hook => hook.Id);

    private static readonly Dictionary<string, RuntimeWatchHookDefinition> HookDefinitionsByKey =
        HookDefinitions.ToDictionary(static hook => Normalize(hook.Key));

    private static readonly HashSet<RuntimeWatchHookId> HighVolumeHookIds = new()
    {
        RuntimeWatchHookId.ObjFieldInt32Set,
        RuntimeWatchHookId.ObjFieldInt64Set,
        RuntimeWatchHookId.ObjFieldHandleSet,
        RuntimeWatchHookId.ObjArrayFieldInt32Set,
        RuntimeWatchHookId.ObjArrayFieldUInt32Set,
        RuntimeWatchHookId.ObjArrayFieldInt64Set,
        RuntimeWatchHookId.ObjArrayFieldObjSet,
        RuntimeWatchHookId.ObjArrayFieldLengthSet,
        RuntimeWatchHookId.GamelibInvalidateRect,
        RuntimeWatchHookId.GamelibDraw,
        RuntimeWatchHookId.GamelibDrawGame,
        RuntimeWatchHookId.LightDraw,
        RuntimeWatchHookId.TileDraw,
        RuntimeWatchHookId.ObjectHoverDraw,
        RuntimeWatchHookId.ObjectDraw,
        RuntimeWatchHookId.RoofDraw,
        RuntimeWatchHookId.TextBubbleDraw,
        RuntimeWatchHookId.TextFloaterDraw,
        RuntimeWatchHookId.TextConversationDraw,
        RuntimeWatchHookId.TigWindowDisplay,
        RuntimeWatchHookId.TigWindowComposeDirtyRect,
        RuntimeWatchHookId.TigWindowBlitArt,
        RuntimeWatchHookId.TigWindowCopyFromVBuffer,
        RuntimeWatchHookId.TigWindowInvalidateRect,
        RuntimeWatchHookId.TigVideoFlip,
    };

    private static readonly HashSet<RuntimeWatchHookId> LightweightResolverHookIds = new()
    {
        RuntimeWatchHookId.GamelibInvalidateRect,
        RuntimeWatchHookId.GamelibDraw,
        RuntimeWatchHookId.GamelibDrawGame,
        RuntimeWatchHookId.LightDraw,
        RuntimeWatchHookId.TileDraw,
        RuntimeWatchHookId.ObjectHoverDraw,
        RuntimeWatchHookId.ObjectDraw,
        RuntimeWatchHookId.RoofDraw,
        RuntimeWatchHookId.TextBubbleDraw,
        RuntimeWatchHookId.TextFloaterDraw,
        RuntimeWatchHookId.TextConversationDraw,
        RuntimeWatchHookId.TigWindowDisplay,
        RuntimeWatchHookId.TigWindowComposeDirtyRect,
        RuntimeWatchHookId.TigWindowBlitArt,
        RuntimeWatchHookId.TigWindowCopyFromVBuffer,
        RuntimeWatchHookId.TigWindowInvalidateRect,
        RuntimeWatchHookId.TigVideoFlip,
    };

    private static readonly RuntimeWatchHookDefinition[] BroadSessionHooks =
    [
        .. HookDefinitions.Where(hook =>
            !HighVolumeHookIds.Contains(hook.Id) && hook.Id != RuntimeWatchHookId.TimeEventAddDelay
        ),
    ];

    private static readonly RuntimeWatchHookDefinition[] SessionCoreHooks =
    [
        Hook(RuntimeWatchHookId.LevelRecalc),
        Hook(RuntimeWatchHookId.UpdateFollowerLevel),
        Hook(RuntimeWatchHookId.CritterGiveXp),
        Hook(RuntimeWatchHookId.StatBaseSet),
        Hook(RuntimeWatchHookId.BackgroundEducateFollowers),
        Hook(RuntimeWatchHookId.BackgroundSet),
        Hook(RuntimeWatchHookId.BackgroundClear),
        Hook(RuntimeWatchHookId.UiShowInvenLoot),
        Hook(RuntimeWatchHookId.ItemInsert),
        Hook(RuntimeWatchHookId.ItemEquipped),
        Hook(RuntimeWatchHookId.ItemForceRemove),
        Hook(RuntimeWatchHookId.ItemUnequipped),
        Hook(RuntimeWatchHookId.ObjectCreate),
        Hook(RuntimeWatchHookId.ObjectDestroy),
        Hook(RuntimeWatchHookId.UiStartDialog),
        Hook(RuntimeWatchHookId.UiSpellAdd),
        Hook(RuntimeWatchHookId.UiSpellMaintainAdd),
        Hook(RuntimeWatchHookId.UiSpellMaintainEnd),
        Hook(RuntimeWatchHookId.SpellAdd),
        Hook(RuntimeWatchHookId.SpellRemove),
        Hook(RuntimeWatchHookId.SpellCollegeLevelSet),
        Hook(RuntimeWatchHookId.TechLearnSchematic),
        Hook(RuntimeWatchHookId.EffectAdd),
        Hook(RuntimeWatchHookId.EffectRemoveInternal),
        Hook(RuntimeWatchHookId.QuestStateSet),
        Hook(RuntimeWatchHookId.QuestGlobalStateSet),
        Hook(RuntimeWatchHookId.RumorKnownSet),
        Hook(RuntimeWatchHookId.MapOpenInGame),
        Hook(RuntimeWatchHookId.AreaSetKnown),
        Hook(RuntimeWatchHookId.TeleportDo),
        Hook(RuntimeWatchHookId.CombatTurnBasedWhosTurnSet),
    ];

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
                Hook(RuntimeWatchHookId.ObjFieldHandleSet),
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
                Hook(RuntimeWatchHookId.ObjFieldHandleSet),
                Hook(RuntimeWatchHookId.ObjectDestroy),
            ]
        ),
        ["objects"] = new RuntimeWatchProfileDefinition(
            "objects",
            "Object lifecycle plus low-level scalar and array mutation coverage, including handles, locations, and list-backed state.",
            [
                Hook(RuntimeWatchHookId.ObjectCreate),
                Hook(RuntimeWatchHookId.ObjectDestroy),
                Hook(RuntimeWatchHookId.ObjFieldInt32Set),
                Hook(RuntimeWatchHookId.ObjFieldInt64Set),
                Hook(RuntimeWatchHookId.ObjFieldHandleSet),
                Hook(RuntimeWatchHookId.ObjArrayFieldInt32Set),
                Hook(RuntimeWatchHookId.ObjArrayFieldUInt32Set),
                Hook(RuntimeWatchHookId.ObjArrayFieldInt64Set),
                Hook(RuntimeWatchHookId.ObjArrayFieldObjSet),
                Hook(RuntimeWatchHookId.ObjArrayFieldScriptSet),
                Hook(RuntimeWatchHookId.ObjArrayFieldPcQuestSet),
                Hook(RuntimeWatchHookId.ObjArrayFieldLengthSet),
            ]
        ),
        ["object-create-only"] = new RuntimeWatchProfileDefinition(
            "object-create-only",
            "Single high-signal lifecycle seam for new runtime object creation requests.",
            [Hook(RuntimeWatchHookId.ObjectCreate)]
        ),
        ["movement"] = new RuntimeWatchProfileDefinition(
            "movement",
            "Higher-level travel transitions plus low-level packed location writes without the broader object mutation firehose.",
            [
                Hook(RuntimeWatchHookId.TeleportDo),
                Hook(RuntimeWatchHookId.MapOpenInGame),
                Hook(RuntimeWatchHookId.TimeEventNotifyPcTeleported),
                Hook(RuntimeWatchHookId.ObjFieldInt64Set),
            ]
        ),
        ["appearance"] = new RuntimeWatchProfileDefinition(
            "appearance",
            "Object creation plus low-level int32 appearance churn such as art, color, and related visual state.",
            [Hook(RuntimeWatchHookId.ObjectCreate), Hook(RuntimeWatchHookId.ObjFieldInt32Set)]
        ),
        ["arrays"] = new RuntimeWatchProfileDefinition(
            "arrays",
            "Raw and structured array writes across stats, skills, effects, followers, script attachments, quest state, and timestamp-backed arrays.",
            [
                Hook(RuntimeWatchHookId.ObjArrayFieldInt32Set),
                Hook(RuntimeWatchHookId.ObjArrayFieldUInt32Set),
                Hook(RuntimeWatchHookId.ObjArrayFieldInt64Set),
                Hook(RuntimeWatchHookId.ObjArrayFieldObjSet),
                Hook(RuntimeWatchHookId.ObjArrayFieldScriptSet),
                Hook(RuntimeWatchHookId.ObjArrayFieldPcQuestSet),
                Hook(RuntimeWatchHookId.ObjArrayFieldLengthSet),
            ]
        ),
        ["mutations"] = new RuntimeWatchProfileDefinition(
            "mutations",
            "Raw and structured object writes across scalar fields, array elements, script attachments, quest records, and list resizing operations.",
            [
                Hook(RuntimeWatchHookId.ObjFieldInt32Set),
                Hook(RuntimeWatchHookId.ObjFieldInt64Set),
                Hook(RuntimeWatchHookId.ObjFieldHandleSet),
                Hook(RuntimeWatchHookId.ObjArrayFieldInt32Set),
                Hook(RuntimeWatchHookId.ObjArrayFieldUInt32Set),
                Hook(RuntimeWatchHookId.ObjArrayFieldInt64Set),
                Hook(RuntimeWatchHookId.ObjArrayFieldObjSet),
                Hook(RuntimeWatchHookId.ObjArrayFieldScriptSet),
                Hook(RuntimeWatchHookId.ObjArrayFieldPcQuestSet),
                Hook(RuntimeWatchHookId.ObjArrayFieldLengthSet),
            ]
        ),
        ["attachments"] = new RuntimeWatchProfileDefinition(
            "attachments",
            "Script attachment writes plus local attachment flags and counters on runtime object scripts.",
            [
                Hook(RuntimeWatchHookId.ObjArrayFieldScriptSet),
                Hook(RuntimeWatchHookId.ScriptLocalFlagSet),
                Hook(RuntimeWatchHookId.ScriptLocalCounterSet),
            ]
        ),
        ["critters"] = new RuntimeWatchProfileDefinition(
            "critters",
            "Tracks script activity, reaction shifts, kill flow, and destroy cleanup for creatures and actors.",
            [
                Hook(RuntimeWatchHookId.ObjectCreate),
                Hook(RuntimeWatchHookId.ObjectScriptExecute),
                Hook(RuntimeWatchHookId.ReactionAdj),
                Hook(RuntimeWatchHookId.CritterKill),
                Hook(RuntimeWatchHookId.CritterGiveXp),
                Hook(RuntimeWatchHookId.LogbookAddKill),
                Hook(RuntimeWatchHookId.LogbookAddInjury),
                Hook(RuntimeWatchHookId.ObjectDestroy),
            ]
        ),
        ["levelup"] = new RuntimeWatchProfileDefinition(
            "levelup",
            "Tracks level recomputation, follower sync, stat writes, and follower education side effects.",
            [
                Hook(RuntimeWatchHookId.LevelRecalc),
                Hook(RuntimeWatchHookId.UpdateFollowerLevel),
                Hook(RuntimeWatchHookId.CritterGiveXp),
                Hook(RuntimeWatchHookId.StatBaseSet),
                Hook(RuntimeWatchHookId.BackgroundEducateFollowers),
            ]
        ),
        ["npcs"] = new RuntimeWatchProfileDefinition(
            "npcs",
            "Tracks NPC script activity, dialog start, disposition changes, kill flow, and destroy cleanup.",
            [
                Hook(RuntimeWatchHookId.ObjectCreate),
                Hook(RuntimeWatchHookId.ObjectScriptExecute),
                Hook(RuntimeWatchHookId.UiStartDialog),
                Hook(RuntimeWatchHookId.ReactionAdj),
                Hook(RuntimeWatchHookId.CritterKill),
                Hook(RuntimeWatchHookId.ObjectDestroy),
            ]
        ),
        ["progression"] = new RuntimeWatchProfileDefinition(
            "progression",
            "Tracks XP awards, player and follower progression recalculation, background changes, technology learning, and the stat writes that accompany them.",
            [
                Hook(RuntimeWatchHookId.LevelRecalc),
                Hook(RuntimeWatchHookId.UpdateFollowerLevel),
                Hook(RuntimeWatchHookId.CritterGiveXp),
                Hook(RuntimeWatchHookId.StatBaseSet),
                Hook(RuntimeWatchHookId.BackgroundEducateFollowers),
                Hook(RuntimeWatchHookId.BackgroundSet),
                Hook(RuntimeWatchHookId.BackgroundClear),
                Hook(RuntimeWatchHookId.TechLearnSchematic),
            ]
        ),
        ["actors"] = new RuntimeWatchProfileDefinition(
            "actors",
            "Broad actor-facing probe for scripts, dialog, disposition changes, deaths, and destruction.",
            [
                Hook(RuntimeWatchHookId.ObjectCreate),
                Hook(RuntimeWatchHookId.ObjectScriptExecute),
                Hook(RuntimeWatchHookId.UiStartDialog),
                Hook(RuntimeWatchHookId.ReactionAdj),
                Hook(RuntimeWatchHookId.CritterKill),
                Hook(RuntimeWatchHookId.ObjectDestroy),
            ]
        ),
        ["magic"] = new RuntimeWatchProfileDefinition(
            "magic",
            "Spellbook requests, committed college-level changes, maintained-effect lifecycle, blessing/curse changes, tech schematic learning, and adjacent scheduler activity around magical or technological state.",
            [
                Hook(RuntimeWatchHookId.SpellAdd),
                Hook(RuntimeWatchHookId.SpellRemove),
                Hook(RuntimeWatchHookId.SpellCollegeLevelSet),
                Hook(RuntimeWatchHookId.TechLearnSchematic),
                Hook(RuntimeWatchHookId.UiSpellAdd),
                Hook(RuntimeWatchHookId.UiSpellMaintainAdd),
                Hook(RuntimeWatchHookId.UiSpellMaintainEnd),
                Hook(RuntimeWatchHookId.BlessAdd),
                Hook(RuntimeWatchHookId.BlessRemove),
                Hook(RuntimeWatchHookId.CurseAdd),
                Hook(RuntimeWatchHookId.CurseRemove),
                Hook(RuntimeWatchHookId.TimeEventAddDelay),
            ]
        ),
        ["session-core"] = new RuntimeWatchProfileDefinition(
            "session-core",
            "Human-first live session pack for the strongest progression, UI, lifecycle, world, and combat signals without scheduler spam or low-level mutation churn.",
            [.. SessionCoreHooks]
        ),
        ["effects"] = new RuntimeWatchProfileDefinition(
            "effects",
            "Committed effect adds plus request and commit-side removal telemetry with decoded effect causes and matched effect identifiers.",
            [
                Hook(RuntimeWatchHookId.EffectAdd),
                Hook(RuntimeWatchHookId.EffectRemoveOneTyped),
                Hook(RuntimeWatchHookId.EffectRemoveAllTyped),
                Hook(RuntimeWatchHookId.EffectRemoveOneCausedBy),
                Hook(RuntimeWatchHookId.EffectRemoveAllCausedBy),
                Hook(RuntimeWatchHookId.EffectRemoveInternal),
            ]
        ),
        ["scripts"] = new RuntimeWatchProfileDefinition(
            "scripts",
            "Raw script attachment execution plus structured attachment writes and local attachment state changes.",
            [
                Hook(RuntimeWatchHookId.ObjectScriptExecute),
                Hook(RuntimeWatchHookId.ObjArrayFieldScriptSet),
                Hook(RuntimeWatchHookId.ScriptLocalFlagSet),
                Hook(RuntimeWatchHookId.ScriptLocalCounterSet),
            ]
        ),
        ["state"] = new RuntimeWatchProfileDefinition(
            "state",
            "Script variables, attachment state, quest records, rumor state, reputation, world-map discovery, quest state, and story-state mutations.",
            [
                Hook(RuntimeWatchHookId.ScriptGlobalVarSet),
                Hook(RuntimeWatchHookId.ScriptGlobalFlagSet),
                Hook(RuntimeWatchHookId.ScriptPcVarSet),
                Hook(RuntimeWatchHookId.ScriptPcFlagSet),
                Hook(RuntimeWatchHookId.ObjArrayFieldScriptSet),
                Hook(RuntimeWatchHookId.ScriptLocalFlagSet),
                Hook(RuntimeWatchHookId.ScriptLocalCounterSet),
                Hook(RuntimeWatchHookId.ScriptStoryStateSet),
                Hook(RuntimeWatchHookId.ObjArrayFieldPcQuestSet),
                Hook(RuntimeWatchHookId.QuestStateSet),
                Hook(RuntimeWatchHookId.QuestGlobalStateSet),
                Hook(RuntimeWatchHookId.ReputationAdd),
                Hook(RuntimeWatchHookId.ReputationRemove),
                Hook(RuntimeWatchHookId.RumorQstateSet),
                Hook(RuntimeWatchHookId.RumorKnownSet),
                Hook(RuntimeWatchHookId.AreaSetKnown),
                Hook(RuntimeWatchHookId.AreaResetLastKnownArea),
            ]
        ),
        ["quests"] = new RuntimeWatchProfileDefinition(
            "quests",
            "Quest, rumor, and story-state changes, including committed per-PC quest records and global gates.",
            [
                Hook(RuntimeWatchHookId.ScriptStoryStateSet),
                Hook(RuntimeWatchHookId.ObjArrayFieldPcQuestSet),
                Hook(RuntimeWatchHookId.QuestStateSet),
                Hook(RuntimeWatchHookId.QuestGlobalStateSet),
                Hook(RuntimeWatchHookId.RumorQstateSet),
                Hook(RuntimeWatchHookId.RumorKnownSet),
            ]
        ),
        ["world"] = new RuntimeWatchProfileDefinition(
            "world",
            "Map opens, world-map discovery, random encounters, teleport preparation, and runtime event scheduling around travel and transitions.",
            [
                Hook(RuntimeWatchHookId.TeleportDo),
                Hook(RuntimeWatchHookId.MapOpenInGame),
                Hook(RuntimeWatchHookId.AreaSetKnown),
                Hook(RuntimeWatchHookId.AreaResetLastKnownArea),
                Hook(RuntimeWatchHookId.WmapRndEncounterCheck),
                Hook(RuntimeWatchHookId.WmapUiEncounterStart),
                Hook(RuntimeWatchHookId.WmapLoadWorldmapInfo),
                Hook(RuntimeWatchHookId.TimeEventNotifyPcTeleported),
                Hook(RuntimeWatchHookId.TimeEventAddDelay),
            ]
        ),
        ["travel"] = new RuntimeWatchProfileDefinition(
            "travel",
            "Teleport requests, map opens, world-map encounter flow, and scheduler activity around movement between maps and sectors.",
            [
                Hook(RuntimeWatchHookId.TeleportDo),
                Hook(RuntimeWatchHookId.MapOpenInGame),
                Hook(RuntimeWatchHookId.WmapRndEncounterCheck),
                Hook(RuntimeWatchHookId.WmapUiEncounterStart),
                Hook(RuntimeWatchHookId.WmapLoadWorldmapInfo),
                Hook(RuntimeWatchHookId.TimeEventNotifyPcTeleported),
                Hook(RuntimeWatchHookId.TimeEventAddDelay),
            ]
        ),
        ["combat"] = new RuntimeWatchProfileDefinition(
            "combat",
            "Turn handoff, script combat callbacks, reaction changes, kills, and destroy cleanup.",
            [
                Hook(RuntimeWatchHookId.CombatTurnBasedWhosTurnSet),
                Hook(RuntimeWatchHookId.ObjectScriptExecute),
                Hook(RuntimeWatchHookId.ReactionAdj),
                Hook(RuntimeWatchHookId.CritterKill),
                Hook(RuntimeWatchHookId.LogbookAddKill),
                Hook(RuntimeWatchHookId.LogbookAddInjury),
                Hook(RuntimeWatchHookId.ObjectDestroy),
            ]
        ),
        ["render"] = new RuntimeWatchProfileDefinition(
            "render",
            "World-frame invalidation, draw entry, pass ordering, dirty-rect composition, and final presentation across the TIG window system.",
            [
                Hook(RuntimeWatchHookId.GamelibInvalidateRect),
                Hook(RuntimeWatchHookId.GamelibDraw),
                Hook(RuntimeWatchHookId.GamelibDrawGame),
                Hook(RuntimeWatchHookId.LightDraw),
                Hook(RuntimeWatchHookId.TileDraw),
                Hook(RuntimeWatchHookId.ObjectHoverDraw),
                Hook(RuntimeWatchHookId.ObjectDraw),
                Hook(RuntimeWatchHookId.RoofDraw),
                Hook(RuntimeWatchHookId.TextBubbleDraw),
                Hook(RuntimeWatchHookId.TextFloaterDraw),
                Hook(RuntimeWatchHookId.TextConversationDraw),
                Hook(RuntimeWatchHookId.TigWindowDisplay),
                Hook(RuntimeWatchHookId.TigWindowComposeDirtyRect),
                Hook(RuntimeWatchHookId.TigWindowInvalidateRect),
                Hook(RuntimeWatchHookId.TigVideoFlip),
            ]
        ),
        ["render-core"] = new RuntimeWatchProfileDefinition(
            "render-core",
            "High-level render heartbeat using the world draw entry and TIG window-stack display start without the full render firehose.",
            [Hook(RuntimeWatchHookId.GamelibDraw), Hook(RuntimeWatchHookId.TigWindowDisplay)]
        ),
        ["presentation"] = new RuntimeWatchProfileDefinition(
            "presentation",
            "TIG dirty-rect queueing, composition, window-stack display, and the final present step.",
            [
                Hook(RuntimeWatchHookId.TigWindowDisplay),
                Hook(RuntimeWatchHookId.TigWindowComposeDirtyRect),
                Hook(RuntimeWatchHookId.TigWindowInvalidateRect),
                Hook(RuntimeWatchHookId.TigVideoFlip),
            ]
        ),
        ["blits"] = new RuntimeWatchProfileDefinition(
            "blits",
            "High-volume pixel transfer coverage for art blits and video-buffer copies into TIG windows.",
            [Hook(RuntimeWatchHookId.TigWindowBlitArt), Hook(RuntimeWatchHookId.TigWindowCopyFromVBuffer)]
        ),
        ["session"] = new RuntimeWatchProfileDefinition(
            "session",
            "Debugger-style broad session coverage across world transitions, quest/script state, combat, dialog, progression, inventory, spellbook flow, and effect lifecycle without the highest-volume raw field churn or scheduler spam.",
            [.. BroadSessionHooks]
        ),
        ["debugger"] = new RuntimeWatchProfileDefinition(
            "debugger",
            "Alias for the broad session debugger profile without scheduler spam.",
            [.. BroadSessionHooks]
        ),
    };

    private static readonly Dictionary<string, RuntimeWatchProfileDefinition> ProfileDefinitionsByKey =
        ProfileDefinitions.ToDictionary(static entry => Normalize(entry.Key), static entry => entry.Value);

    private static readonly RuntimeWatchProfileDescriptor[] s_profiles =
    [
        .. ProfileDefinitions
            .OrderBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static entry => new RuntimeWatchProfileDescriptor(
                entry.Key,
                entry.Value.Description,
                entry.Value.Hooks
            )),
    ];
}
