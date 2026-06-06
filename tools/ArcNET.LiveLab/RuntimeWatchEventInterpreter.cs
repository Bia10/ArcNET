using System.Runtime.Versioning;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal static class RuntimeWatchEventInterpreter
{
    public static bool IsNoise(RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent) =>
        capturedEvent.Definition.Id == RuntimeWatchHookId.ObjectScriptExecute
        && IsNoiseAttachment(AttachmentPoint(capturedEvent));

    public static string EventClass(RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent) =>
        IsNoise(capturedEvent) ? "Noise" : "Signal";

    public static string HookArea(RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent) =>
        capturedEvent.Definition.Area;

    public static string HookDescription(RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent) =>
        capturedEvent.Definition.Description;

    public static string SemanticEvent(RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent) =>
        capturedEvent.Definition.Id switch
        {
            RuntimeWatchHookId.LevelRecalc => "PlayerLevelStateRecomputed",
            RuntimeWatchHookId.UpdateFollowerLevel => "FollowerLevelSynchronized",
            RuntimeWatchHookId.StatBaseSet => "BaseStatWritten",
            RuntimeWatchHookId.BackgroundEducateFollowers => "FollowerEducationApplied",
            RuntimeWatchHookId.UiShowInvenLoot => "LootWindowOpened",
            RuntimeWatchHookId.ItemInsert => "ItemInserted",
            RuntimeWatchHookId.ItemEquipped => "ItemEquipped",
            RuntimeWatchHookId.ItemForceRemove => "ItemRemoved",
            RuntimeWatchHookId.ItemUnequipped => "ItemUnequipped",
            RuntimeWatchHookId.ObjectDestroy => "ObjectDestroyed",
            RuntimeWatchHookId.ObjectScriptExecute => ScriptSemanticEvent(AttachmentPoint(capturedEvent)),
            RuntimeWatchHookId.UiStartDialog => "ConversationStarted",
            RuntimeWatchHookId.ReactionAdj => ReactionSemanticEvent(IntValue(capturedEvent, 4)),
            RuntimeWatchHookId.CritterKill => "CritterDeathHandled",
            _ => capturedEvent.Definition.EventName,
        };

    public static string Category(RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent) =>
        capturedEvent.Definition.Id switch
        {
            RuntimeWatchHookId.LevelRecalc => "Progression",
            RuntimeWatchHookId.UpdateFollowerLevel => "Progression",
            RuntimeWatchHookId.StatBaseSet => "Progression",
            RuntimeWatchHookId.BackgroundEducateFollowers => "Progression",
            RuntimeWatchHookId.UiShowInvenLoot => "Inventory",
            RuntimeWatchHookId.ItemInsert => "Inventory",
            RuntimeWatchHookId.ItemEquipped => "Inventory",
            RuntimeWatchHookId.ItemForceRemove => "Inventory",
            RuntimeWatchHookId.ItemUnequipped => "Inventory",
            RuntimeWatchHookId.ObjectDestroy => "Lifecycle",
            RuntimeWatchHookId.ObjectScriptExecute => ScriptCategory(AttachmentPoint(capturedEvent)),
            RuntimeWatchHookId.UiStartDialog => "Dialog",
            RuntimeWatchHookId.ReactionAdj => "Disposition",
            RuntimeWatchHookId.CritterKill => "Combat",
            _ => "Runtime",
        };

    public static string PrimaryRole(RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent) =>
        capturedEvent.Definition.Id switch
        {
            RuntimeWatchHookId.LevelRecalc => "PlayerCharacter",
            RuntimeWatchHookId.UpdateFollowerLevel => "Follower",
            RuntimeWatchHookId.StatBaseSet => "Object",
            RuntimeWatchHookId.BackgroundEducateFollowers => "PlayerCharacter",
            RuntimeWatchHookId.UiShowInvenLoot => "PlayerCharacter",
            RuntimeWatchHookId.ItemInsert => "Item",
            RuntimeWatchHookId.ItemEquipped => "Item",
            RuntimeWatchHookId.ItemForceRemove => "Item",
            RuntimeWatchHookId.ItemUnequipped => "Item",
            RuntimeWatchHookId.ObjectDestroy => "Object",
            RuntimeWatchHookId.ObjectScriptExecute => ScriptTriggererRole(AttachmentPoint(capturedEvent)),
            RuntimeWatchHookId.UiStartDialog => "PlayerCharacter",
            RuntimeWatchHookId.ReactionAdj => "Npc",
            RuntimeWatchHookId.CritterKill => "Critter",
            _ => "Subject",
        };

    public static string SecondaryRole(RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent) =>
        capturedEvent.Definition.Id switch
        {
            RuntimeWatchHookId.LevelRecalc => "None",
            RuntimeWatchHookId.UpdateFollowerLevel => "PlayerLevelReference",
            RuntimeWatchHookId.StatBaseSet => "WrittenField",
            RuntimeWatchHookId.BackgroundEducateFollowers => "FollowerGroup",
            RuntimeWatchHookId.UiShowInvenLoot => "LootTarget",
            RuntimeWatchHookId.ItemInsert => InventoryLocationContext(IntValue(capturedEvent, 4)) == "Equipment"
                ? "WearerOrOwner"
                : "ContainerOrOwner",
            RuntimeWatchHookId.ItemEquipped => "WearerOrOwner",
            RuntimeWatchHookId.ItemForceRemove => "FormerOwnerOrContainer",
            RuntimeWatchHookId.ItemUnequipped => "FormerWearerOrOwner",
            RuntimeWatchHookId.ObjectDestroy => "DestroyedObject",
            RuntimeWatchHookId.ObjectScriptExecute => ScriptAttacheeRole(AttachmentPoint(capturedEvent)),
            RuntimeWatchHookId.UiStartDialog => "Npc",
            RuntimeWatchHookId.ReactionAdj => "PlayerCharacter",
            RuntimeWatchHookId.CritterKill => "VictimOrDyingCritter",
            _ => "RelatedObject",
        };

    public static string ExtraRole(RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent) =>
        capturedEvent.Definition.Id switch
        {
            RuntimeWatchHookId.ObjectScriptExecute => ScriptExtraRole(AttachmentPoint(capturedEvent)),
            _ => "None",
        };

    public static string Summary(
        RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent,
        RuntimeWatchObjectResolver? resolver = null
    )
    {
        var stack = capturedEvent.StackDwords;
        return capturedEvent.Definition.Id switch
        {
            RuntimeWatchHookId.LevelRecalc
                => $"{DescribeHandle(resolver, in stack, 0, "Player Character")} started recomputing level-derived state.",
            RuntimeWatchHookId.UpdateFollowerLevel
                => $"{DescribeHandle(resolver, in stack, 0, "Follower")} synchronized from player level {IntValue(capturedEvent, 2)} to {IntValue(capturedEvent, 3)}.",
            RuntimeWatchHookId.StatBaseSet
                => $"{StatName(IntValue(capturedEvent, 2))} on {DescribeHandle(resolver, in stack, 0)} was written to {IntValue(capturedEvent, 3)}.",
            RuntimeWatchHookId.BackgroundEducateFollowers
                => $"{DescribeHandle(resolver, in stack, 0, "Player Character")} triggered follower education side effects.",
            RuntimeWatchHookId.UiShowInvenLoot
                => $"{DescribeHandle(resolver, in stack, 0, "Player Character")} opened the loot window for {DescribeHandle(resolver, in stack, 2, "Loot Target")}.",
            RuntimeWatchHookId.ItemInsert
                => $"{DescribeHandle(resolver, in stack, 0, "Item")} moved into {DescribeHandle(resolver, in stack, 2, "Container or Owner")} at {InventoryLocationName(IntValue(capturedEvent, 4))}.",
            RuntimeWatchHookId.ItemEquipped
                => $"{DescribeHandle(resolver, in stack, 2, "Wearer or Owner")} equipped {DescribeHandle(resolver, in stack, 0, "Item")} in {InventoryLocationName(IntValue(capturedEvent, 4))}.",
            RuntimeWatchHookId.ItemForceRemove
                => $"{DescribeHandle(resolver, in stack, 0, "Item")} was forcibly removed from {DescribeHandle(resolver, in stack, 2, "Former Owner or Container")}.",
            RuntimeWatchHookId.ItemUnequipped
                => $"{DescribeHandle(resolver, in stack, 2, "Former Wearer or Owner")} unequipped {DescribeHandle(resolver, in stack, 0, "Item")} from {InventoryLocationName(IntValue(capturedEvent, 4))}.",
            RuntimeWatchHookId.ObjectDestroy
                => $"{DescribeHandle(resolver, in stack, 0)} entered destroy cleanup.",
            RuntimeWatchHookId.ObjectScriptExecute
                => ScriptSummary(capturedEvent, resolver),
            RuntimeWatchHookId.UiStartDialog
                => $"{DescribeHandle(resolver, in stack, 0, "Player Character")} started dialog {IntValue(capturedEvent, 6)} with {DescribeHandle(resolver, in stack, 2, "NPC")} using script {IntValue(capturedEvent, 4)} line {IntValue(capturedEvent, 5)}.",
            RuntimeWatchHookId.ReactionAdj
                => $"{DescribeHandle(resolver, in stack, 0, "NPC")} reaction toward {DescribeHandle(resolver, in stack, 2, "Player Character")} {ReactionDirectionText(IntValue(capturedEvent, 4))} by {Math.Abs(IntValue(capturedEvent, 4))}.",
            RuntimeWatchHookId.CritterKill
                => $"{DescribeHandle(resolver, in stack, 0, "Critter")} entered death and kill-resolution handling.",
            _ => $"{capturedEvent.Definition.EventName} captured.",
        };
    }

    public static string Signature(
        RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent,
        RuntimeWatchObjectResolver? resolver = null
    )
    {
        var stack = capturedEvent.StackDwords;
        return capturedEvent.Definition.Id switch
        {
            RuntimeWatchHookId.LevelRecalc
                => $"LevelRecalc {DescribeHandleCompact(resolver, in stack, 0, "Player Character")}",
            RuntimeWatchHookId.UpdateFollowerLevel
                => $"FollowerLevelSync {DescribeHandleCompact(resolver, in stack, 0, "Follower")} {IntValue(capturedEvent, 2)}->{IntValue(capturedEvent, 3)}",
            RuntimeWatchHookId.StatBaseSet
                => $"StatBaseSet {StatName(IntValue(capturedEvent, 2))} {DescribeHandleCompact(resolver, in stack, 0)}={IntValue(capturedEvent, 3)}",
            RuntimeWatchHookId.BackgroundEducateFollowers
                => $"FollowerEducationPass {DescribeHandleCompact(resolver, in stack, 0, "Player Character")}",
            RuntimeWatchHookId.UiShowInvenLoot
                => $"LootUiOpened {DescribeHandleCompact(resolver, in stack, 0, "Player Character")}->{DescribeHandleCompact(resolver, in stack, 2, "Loot Target")}",
            RuntimeWatchHookId.ItemInsert
                => $"ItemInserted {DescribeHandleCompact(resolver, in stack, 0, "Item")}->{DescribeHandleCompact(resolver, in stack, 2, "Container or Owner")} @{InventoryLocationName(IntValue(capturedEvent, 4))}",
            RuntimeWatchHookId.ItemEquipped
                => $"ItemEquipped {DescribeHandleCompact(resolver, in stack, 0, "Item")}->{DescribeHandleCompact(resolver, in stack, 2, "Wearer or Owner")} @{InventoryLocationName(IntValue(capturedEvent, 4))}",
            RuntimeWatchHookId.ItemForceRemove
                => $"ItemRemoved {DescribeHandleCompact(resolver, in stack, 0, "Item")}<-{DescribeHandleCompact(resolver, in stack, 2, "Former Owner or Container")}",
            RuntimeWatchHookId.ItemUnequipped
                => $"ItemUnequipped {DescribeHandleCompact(resolver, in stack, 0, "Item")}<-{DescribeHandleCompact(resolver, in stack, 2, "Former Wearer or Owner")} @{InventoryLocationName(IntValue(capturedEvent, 4))}",
            RuntimeWatchHookId.ObjectDestroy
                => $"ObjectDestroyed {DescribeHandleCompact(resolver, in stack, 0)}",
            RuntimeWatchHookId.ObjectScriptExecute
                => ScriptSignature(capturedEvent, resolver),
            RuntimeWatchHookId.UiStartDialog
                => $"DialogStarted {DescribeHandleCompact(resolver, in stack, 0, "Player Character")}->{DescribeHandleCompact(resolver, in stack, 2, "NPC")} dialog {IntValue(capturedEvent, 6)}",
            RuntimeWatchHookId.ReactionAdj
                => $"{ReactionDirection(IntValue(capturedEvent, 4))} {DescribeHandleCompact(resolver, in stack, 0, "NPC")}->{DescribeHandleCompact(resolver, in stack, 2, "Player Character")}",
            RuntimeWatchHookId.CritterKill
                => $"CritterKilled {DescribeHandleCompact(resolver, in stack, 0, "Critter")}",
            _ => SemanticEvent(capturedEvent),
        };
    }

    public static string StatName(int stat) => stat >= 0 && stat < StatNames.Length ? StatNames[stat] : $"Stat[{stat}]";

    public static string StatGroup(int stat) =>
        stat switch
        {
            >= 0 and <= 7 => "Attribute",
            >= 8 and <= 16 => "DerivedStat",
            17 or 18 or 20 or 21 or 22 or 23 => "ProgressionOrResource",
            19 => "Alignment",
            24 => "Condition",
            >= 25 and <= 27 => "Identity",
            _ => "UnknownStatGroup",
        };

    public static string AttachmentPointName(int attachmentPoint) =>
        attachmentPoint >= 0 && attachmentPoint < AttachmentPointNames.Length
            ? AttachmentPointNames[attachmentPoint]
            : $"AttachmentPoint[{attachmentPoint}]";

    public static string InventoryLocationName(int inventoryLocation) =>
        InventoryLocationNames.TryGetValue(inventoryLocation, out var name)
            ? name
            : $"InventoryLocation[{inventoryLocation}]";

    public static string InventoryLocationContext(int inventoryLocation) =>
        inventoryLocation is >= 1000 and <= 1008 ? "Equipment" : "ContainerOrGeneralInventory";

    public static string ReactionDirection(int delta) =>
        delta switch
        {
            > 0 => "DispositionImproved",
            < 0 => "DispositionWorsened",
            _ => "DispositionUnchanged",
        };

    public static int AttachmentPoint(RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent) =>
        IntValue(capturedEvent, 6);

    public static int IntValue(RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent, int index) =>
        unchecked((int)capturedEvent.StackDwords.Get(index));

    public static ulong HandleValue(in RuntimeWatchSession.RuntimeWatchStackCapture stack, int lowIndex) =>
        ((ulong)stack.Get(lowIndex + 1) << 32) | stack.Get(lowIndex);

    public static string HandleHex(in RuntimeWatchSession.RuntimeWatchStackCapture stack, int lowIndex) =>
        FormatHandle(HandleValue(in stack, lowIndex));

    public static string FormatHandle(ulong handle) => handle == 0 ? "null" : $"0x{handle:X16}";

    public static string ReactionDirectionText(int delta) =>
        delta switch
        {
            > 0 => "improved",
            < 0 => "worsened",
            _ => "held steady",
        };

    private static string ReactionSemanticEvent(int delta) =>
        delta switch
        {
            > 0 => "DispositionImproved",
            < 0 => "DispositionWorsened",
            _ => "DispositionChecked",
        };

    private static string ScriptSemanticEvent(int attachmentPoint) =>
        attachmentPoint switch
        {
            0 => "ExamineInteraction",
            1 => "UseInteraction",
            2 => "DestroyScript",
            3 => "UnlockAttempt",
            4 => "PickupAttempt",
            5 => "DropItem",
            7 => "Hit",
            8 => "Miss",
            9 => "DialogScript",
            10 => "SpawnInit",
            12 => "DyingScript",
            13 => "EnterCombat",
            14 => "ExitCombat",
            15 => "StartCombat",
            16 => "EndCombat",
            17 => "BuyObject",
            19 => "ActorHeartbeat",
            20 => "LeaderKilling",
            21 => "ContainerInsert",
            22 => "AggroCheck",
            23 => "TakingDamage",
            24 => "WieldOn",
            25 => "WieldOff",
            26 => "CritterHits",
            27 => "SectorEnter",
            28 => "ContainerRemove",
            32 => "Transfer",
            34 => "CriticalHit",
            35 => "CriticalMiss",
            _ => AttachmentPointName(attachmentPoint),
        };

    private static string ScriptCategory(int attachmentPoint) =>
        attachmentPoint switch
        {
            0 or 1 or 3 or 4 => "Interaction",
            5 or 21 or 28 or 32 => "Inventory",
            2 or 10 or 12 => "Lifecycle",
            9 => "Dialog",
            7 or 8 or 13 or 14 or 15 or 16 or 20 or 23 or 26 or 34 or 35 => "Combat",
            17 => "Commerce",
            22 => "Ai",
            19 => "Ai",
            24 or 25 => "Equipment",
            27 => "Travel",
            _ => "Script",
        };

    private static string ScriptTriggererRole(int attachmentPoint) =>
        attachmentPoint switch
        {
            0 or 1 or 3 or 4 or 5 => "Actor",
            7 or 8 or 13 or 15 or 16 or 23 or 34 or 35 => "AttackerOrSource",
            10 => "SpawnerOrSelf",
            14 => "OpponentOrSource",
            19 => "HeartbeatOwner",
            20 => "LeaderOrWitness",
            21 => "ContainerOrOwner",
            22 => "AggroSource",
            24 or 25 => "WearerOrOwner",
            26 => "Victim",
            27 => "Traveler",
            28 => "ContainerOrOwner",
            32 => "SourceOwner",
            _ => "Triggerer",
        };

    private static string ScriptAttacheeRole(int attachmentPoint) =>
        attachmentPoint switch
        {
            0 => "ExaminedObject",
            1 => "UsedObject",
            3 => "UnlockTarget",
            4 => "PickupTarget",
            5 => "DroppedItem",
            7 or 8 or 13 or 14 or 15 or 16 or 23 or 34 or 35 => "Target",
            10 => "SpawnedOrInitializedObject",
            19 => "HeartbeatTarget",
            20 => "KilledLeaderOrWitnessTarget",
            21 => "InsertedItem",
            22 => "AggroTarget",
            24 or 25 => "WieldedItem",
            26 => "Attacker",
            27 => "SectorObject",
            28 => "RemovedItem",
            32 => "DestinationOwner",
            _ => "Attachee",
        };

    private static string ScriptExtraRole(int attachmentPoint) =>
        attachmentPoint switch
        {
            1 => "UseSource",
            7 or 8 or 23 or 34 or 35 => "WeaponOrDamageSource",
            32 => "TransferredItem",
            _ => "None",
        };

    private static bool IsNoiseAttachment(int attachmentPoint) => attachmentPoint is 19 or 22;

    private static string ScriptSummary(
        RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent,
        RuntimeWatchObjectResolver? resolver
    )
    {
        var stack = capturedEvent.StackDwords;
        var triggerer = DescribeHandle(resolver, in stack, 0, "Triggerer");
        var attachee = DescribeHandle(resolver, in stack, 2, "Attachee");
        var extra = DescribeHandle(resolver, in stack, 4, "Extra Object");
        var attachmentPoint = AttachmentPoint(capturedEvent);
        var line = IntValue(capturedEvent, 7);

        return attachmentPoint switch
        {
            1 => $"{triggerer} used {attachee}{DescribeExtra(extra, triggerer)}.",
            3 => $"{triggerer} attempted to unlock or open {attachee}.",
            4 => $"{triggerer} attempted to pick up {attachee}.",
            5 => $"{triggerer} dropped item {attachee}.",
            7 => $"{triggerer} landed a hit on {attachee} using {extra}.",
            8 => $"{triggerer} missed {attachee} with {extra}.",
            10 => $"{attachee} ran first-heartbeat initialization.",
            13 => $"{triggerer} entered combat with {attachee}.",
            14 => $"{attachee} exited combat with {triggerer}.",
            15 => $"{triggerer} started combat against {attachee}.",
            16 => $"{triggerer} ended combat against {attachee}.",
            19 => triggerer == attachee
                ? $"{triggerer} ran a heartbeat tick on itself."
                : $"{triggerer} ran a heartbeat tick against {attachee}.",
            20 => $"{triggerer} processed leader-killing for {attachee}.",
            21 => $"{triggerer} inserted or accepted item {attachee}.",
            22 => $"{triggerer} evaluated attack-on-sight rules against {attachee}.",
            23 => $"{triggerer} processed taking-damage for {attachee} using {extra}.",
            24 => $"{triggerer} wielded {attachee}.",
            25 => $"{triggerer} unwielded {attachee}.",
            26 => $"{triggerer} registered a critter-hits event against {attachee}.",
            27 => $"{triggerer} entered the sector for {attachee}.",
            28 => $"{triggerer} removed or released item {attachee}.",
            32 => $"{triggerer} transferred {extra} to {attachee}.",
            34 => $"{triggerer} landed a critical hit on {attachee} using {extra}.",
            35 => $"{triggerer} critically missed {attachee} with {extra}.",
            _ => $"{ScriptSemanticEvent(attachmentPoint)}: triggerer {triggerer}, attachee {attachee}, extra {extra}, line {line}.",
        };
    }

    private static string ScriptSignature(
        RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent,
        RuntimeWatchObjectResolver? resolver
    )
    {
        var stack = capturedEvent.StackDwords;
        var semanticEvent = ScriptSemanticEvent(AttachmentPoint(capturedEvent));
        var triggerer = DescribeHandleCompact(resolver, in stack, 0, "Triggerer");
        var attachee = DescribeHandleCompact(resolver, in stack, 2, "Attachee");
        var extra = DescribeHandleCompact(resolver, in stack, 4, "Extra Object");

        return AttachmentPoint(capturedEvent) switch
        {
            21 or 28 or 24 or 25 => $"{semanticEvent} {triggerer}->{attachee}",
            32 => $"{semanticEvent} {triggerer}->{attachee} item {extra}",
            _ => $"{semanticEvent} {triggerer}->{attachee}",
        };
    }

    private static string DescribeExtra(string extra, string triggerer) =>
        extra is "null" or "" || extra == triggerer ? string.Empty : $" using {extra}";

    private static string DescribeHandle(
        RuntimeWatchObjectResolver? resolver,
        in RuntimeWatchSession.RuntimeWatchStackCapture stack,
        int lowIndex,
        string fallbackName = "Object"
    )
    {
        if (resolver is null)
            return HandleValue(in stack, lowIndex) == 0 ? "null" : $"{fallbackName} {HandleHex(in stack, lowIndex)}";

        return resolver.ResolveHandle(HandleValue(in stack, lowIndex)).SummaryLabel(fallbackName);
    }

    private static string DescribeHandleCompact(
        RuntimeWatchObjectResolver? resolver,
        in RuntimeWatchSession.RuntimeWatchStackCapture stack,
        int lowIndex,
        string fallbackName = "Object"
    )
    {
        if (resolver is null)
            return HandleHex(in stack, lowIndex);

        return resolver.ResolveHandle(HandleValue(in stack, lowIndex)).SignatureLabel(fallbackName);
    }

    private static readonly string[] StatNames =
    [
        "Strength",
        "Dexterity",
        "Constitution",
        "Beauty",
        "Intelligence",
        "Perception",
        "Willpower",
        "Charisma",
        "CarryWeight",
        "DamageBonus",
        "AcAdjustment",
        "Speed",
        "HealRate",
        "PoisonRecovery",
        "ReactionModifier",
        "MaxFollowers",
        "MagickTechAptitude",
        "Level",
        "ExperiencePoints",
        "Alignment",
        "FatePoints",
        "UnspentPoints",
        "MagickPoints",
        "TechPoints",
        "PoisonLevel",
        "Age",
        "Gender",
        "Race",
    ];

    private static readonly string[] AttachmentPointNames =
    [
        "Examine",
        "Use",
        "Destroy",
        "Unlock",
        "Get",
        "Drop",
        "Throw",
        "Hit",
        "Miss",
        "Dialog",
        "FirstHeartbeat",
        "CatchingThiefPc",
        "Dying",
        "EnterCombat",
        "ExitCombat",
        "StartCombat",
        "EndCombat",
        "BuyObject",
        "Resurrect",
        "Heartbeat",
        "LeaderKilling",
        "InsertItem",
        "WillKos",
        "TakingDamage",
        "WieldOn",
        "WieldOff",
        "CritterHits",
        "NewSector",
        "RemoveItem",
        "LeaderSleeping",
        "Bust",
        "DialogOverride",
        "Transfer",
        "CaughtThief",
        "CriticalHit",
        "CriticalMiss",
    ];

    private static readonly Dictionary<int, string> InventoryLocationNames = new()
    {
        [1000] = "Helmet",
        [1001] = "RingLeft",
        [1002] = "RingRight",
        [1003] = "Medallion",
        [1004] = "Weapon",
        [1005] = "Shield",
        [1006] = "Armor",
        [1007] = "Gauntlet",
        [1008] = "Boots",
    };
}
