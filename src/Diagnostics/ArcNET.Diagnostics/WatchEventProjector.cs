namespace ArcNET.Diagnostics;

internal static class WatchEventProjector
{
    public static WatchEventProjection Project(RuntimeWatchCapturedEvent capturedEvent)
    {
        var semanticEvent = SemanticEvent(capturedEvent);
        var summary = Summary(capturedEvent);
        var signature = Signature(capturedEvent);
        var candidateHandles = CandidateHandles(capturedEvent);
        return new WatchEventProjection(
            semanticEvent,
            signature,
            summary,
            SuggestedHandleHex(capturedEvent, candidateHandles),
            candidateHandles
        );
    }

    private static string SemanticEvent(RuntimeWatchCapturedEvent capturedEvent) =>
        capturedEvent.Definition.Id switch
        {
            RuntimeWatchHookId.LevelRecalc => "PlayerLevelStateRecomputed",
            RuntimeWatchHookId.UpdateFollowerLevel => "FollowerLevelSynchronized",
            RuntimeWatchHookId.ItemInsert => "ItemInserted",
            RuntimeWatchHookId.ItemEquipped => "ItemEquipped",
            RuntimeWatchHookId.ItemForceRemove => "ItemRemoved",
            RuntimeWatchHookId.ItemUnequipped => "ItemUnequipped",
            RuntimeWatchHookId.ObjectCreate => "ObjectCreationRequested",
            RuntimeWatchHookId.ObjectDestroy => "ObjectDestroyed",
            RuntimeWatchHookId.ObjFieldInt32Set => "ObjectInt32FieldWritten",
            RuntimeWatchHookId.ObjFieldInt64Set => "ObjectInt64FieldWritten",
            RuntimeWatchHookId.ObjFieldHandleSet => "ObjectHandleFieldWritten",
            RuntimeWatchHookId.ObjArrayFieldInt32Set => "ObjectInt32ArrayElementWritten",
            RuntimeWatchHookId.ObjArrayFieldUInt32Set => "ObjectUInt32ArrayElementWritten",
            RuntimeWatchHookId.ObjArrayFieldInt64Set => "ObjectInt64ArrayElementWritten",
            RuntimeWatchHookId.ObjArrayFieldObjSet => "ObjectHandleArrayElementWritten",
            RuntimeWatchHookId.UiStartDialog => "ConversationStarted",
            RuntimeWatchHookId.ReactionAdj => ReactionSemanticEvent(IntValue(capturedEvent, 4)),
            RuntimeWatchHookId.CritterKill => "CritterDeathHandled",
            RuntimeWatchHookId.CritterGiveXp => "ExperienceAwarded",
            RuntimeWatchHookId.QuestStateSet => "QuestStateAdvanceRequested",
            RuntimeWatchHookId.TeleportDo => "TeleportRequested",
            _ => capturedEvent.Definition.EventName,
        };

    private static string Signature(RuntimeWatchCapturedEvent capturedEvent) =>
        capturedEvent.Definition.Id switch
        {
            RuntimeWatchHookId.LevelRecalc => $"LevelRecalc {HandleLabel(capturedEvent, 0, "PC")}",
            RuntimeWatchHookId.UpdateFollowerLevel =>
                $"FollowerLevelSync {HandleLabel(capturedEvent, 0, "Follower")} {IntValue(capturedEvent, 2)}->{IntValue(capturedEvent, 3)}",
            RuntimeWatchHookId.ItemInsert =>
                $"ItemInserted {HandleLabel(capturedEvent, 0, "Item")}->{HandleLabel(capturedEvent, 2, "Container")} @{RuntimeSemanticCatalog.InventoryLocationName(IntValue(capturedEvent, 4))}",
            RuntimeWatchHookId.ItemEquipped =>
                $"ItemEquipped {HandleLabel(capturedEvent, 0, "Item")}->{HandleLabel(capturedEvent, 2, "Owner")} @{RuntimeSemanticCatalog.InventoryLocationName(IntValue(capturedEvent, 4))}",
            RuntimeWatchHookId.ObjectCreate =>
                $"ObjectCreate {HandleLabel(capturedEvent, 0, "Prototype")} @{RuntimeWatchValueCatalog.FormatPackedLocation(HandleValue(capturedEvent.StackDwords, 2))}",
            RuntimeWatchHookId.ObjectDestroy => $"ObjectDestroyed {HandleLabel(capturedEvent, 0, "Object")}",
            RuntimeWatchHookId.ObjFieldInt32Set =>
                $"Field32 {HandleLabel(capturedEvent, 0, "Object")} {ObjectFieldCatalog.DisplayName(IntValue(capturedEvent, 2))}={IntValue(capturedEvent, 3)}",
            RuntimeWatchHookId.ObjFieldInt64Set =>
                $"Field64 {HandleLabel(capturedEvent, 0, "Object")} {ObjectFieldCatalog.DisplayName(IntValue(capturedEvent, 2))}={FormatUInt64(HandleValue(capturedEvent.StackDwords, 3))}",
            RuntimeWatchHookId.ObjFieldHandleSet =>
                $"FieldHandle {HandleLabel(capturedEvent, 0, "Object")} {ObjectFieldCatalog.DisplayName(IntValue(capturedEvent, 2))}={HandleLabel(capturedEvent, 3, "Target")}",
            RuntimeWatchHookId.ObjArrayFieldInt32Set =>
                $"Array32 {HandleLabel(capturedEvent, 0, "Object")} {ArrayFieldLabel(capturedEvent)}={IntValue(capturedEvent, 4)}",
            RuntimeWatchHookId.ObjArrayFieldUInt32Set =>
                $"ArrayU32 {HandleLabel(capturedEvent, 0, "Object")} {ArrayFieldLabel(capturedEvent)}={UIntValue(capturedEvent, 4)}",
            RuntimeWatchHookId.ObjArrayFieldInt64Set =>
                $"Array64 {HandleLabel(capturedEvent, 0, "Object")} {ArrayFieldLabel(capturedEvent)}={FormatUInt64(HandleValue(capturedEvent.StackDwords, 4))}",
            RuntimeWatchHookId.ObjArrayFieldObjSet =>
                $"ArrayHandle {HandleLabel(capturedEvent, 0, "Object")} {ArrayFieldLabel(capturedEvent)}={HandleLabel(capturedEvent, 4, "Value")}",
            RuntimeWatchHookId.UiStartDialog =>
                $"DialogStarted {HandleLabel(capturedEvent, 0, "PC")}->{HandleLabel(capturedEvent, 2, "NPC")} dialog {IntValue(capturedEvent, 6)}",
            RuntimeWatchHookId.ReactionAdj =>
                $"Disposition {HandleLabel(capturedEvent, 0, "NPC")}->{HandleLabel(capturedEvent, 2, "PC")} {ReactionDirectionText(IntValue(capturedEvent, 4))} {Math.Abs(IntValue(capturedEvent, 4))}",
            RuntimeWatchHookId.CritterKill => $"CritterKilled {HandleLabel(capturedEvent, 0, "Critter")}",
            RuntimeWatchHookId.CritterGiveXp =>
                $"ExperienceAwarded {HandleLabel(capturedEvent, 0, "PC")} +{IntValue(capturedEvent, 2)}",
            RuntimeWatchHookId.TeleportDo => $"TeleportRequested @{FormatUInt32(capturedEvent.StackDwords.Get(0))}",
            _ => $"{capturedEvent.Definition.EventName} {capturedEvent.Definition.Key}",
        };

    private static string Summary(RuntimeWatchCapturedEvent capturedEvent) =>
        capturedEvent.Definition.Id switch
        {
            RuntimeWatchHookId.LevelRecalc =>
                $"{HandleLabel(capturedEvent, 0, "Player Character")} started recomputing level-derived state.",
            RuntimeWatchHookId.UpdateFollowerLevel =>
                $"{HandleLabel(capturedEvent, 0, "Follower")} synchronized from player level {IntValue(capturedEvent, 2)} to {IntValue(capturedEvent, 3)}.",
            RuntimeWatchHookId.ItemInsert =>
                $"{HandleLabel(capturedEvent, 0, "Item")} moved into {HandleLabel(capturedEvent, 2, "Container or Owner")} at {RuntimeSemanticCatalog.InventoryLocationName(IntValue(capturedEvent, 4))}.",
            RuntimeWatchHookId.ItemEquipped =>
                $"{HandleLabel(capturedEvent, 2, "Wearer or Owner")} equipped {HandleLabel(capturedEvent, 0, "Item")} in {RuntimeSemanticCatalog.InventoryLocationName(IntValue(capturedEvent, 4))}.",
            RuntimeWatchHookId.ItemForceRemove =>
                $"{HandleLabel(capturedEvent, 0, "Item")} was forcibly removed from {HandleLabel(capturedEvent, 2, "Former Owner or Container")}.",
            RuntimeWatchHookId.ItemUnequipped =>
                $"{HandleLabel(capturedEvent, 2, "Former Wearer or Owner")} unequipped {HandleLabel(capturedEvent, 0, "Item")} from {RuntimeSemanticCatalog.InventoryLocationName(IntValue(capturedEvent, 4))}.",
            RuntimeWatchHookId.ObjectCreate =>
                $"Requested creation of {HandleLabel(capturedEvent, 0, "Prototype")} at {RuntimeWatchValueCatalog.FormatPackedLocation(HandleValue(capturedEvent.StackDwords, 2))}.",
            RuntimeWatchHookId.ObjectDestroy => $"{HandleLabel(capturedEvent, 0, "Object")} entered destroy cleanup.",
            RuntimeWatchHookId.ObjFieldInt32Set =>
                $"{HandleLabel(capturedEvent, 0, "Object")} wrote {ObjectFieldCatalog.DisplayName(IntValue(capturedEvent, 2))} = {IntValue(capturedEvent, 3)}.",
            RuntimeWatchHookId.ObjFieldInt64Set =>
                $"{HandleLabel(capturedEvent, 0, "Object")} wrote {ObjectFieldCatalog.DisplayName(IntValue(capturedEvent, 2))} = {FormatUInt64(HandleValue(capturedEvent.StackDwords, 3))}.",
            RuntimeWatchHookId.ObjFieldHandleSet =>
                $"{HandleLabel(capturedEvent, 0, "Object")} wrote {ObjectFieldCatalog.DisplayName(IntValue(capturedEvent, 2))} = {HandleLabel(capturedEvent, 3, "Target")}.",
            RuntimeWatchHookId.ObjArrayFieldInt32Set =>
                $"{HandleLabel(capturedEvent, 0, "Object")} wrote {ArrayFieldLabel(capturedEvent)} = {IntValue(capturedEvent, 4)}.",
            RuntimeWatchHookId.ObjArrayFieldUInt32Set =>
                $"{HandleLabel(capturedEvent, 0, "Object")} wrote {ArrayFieldLabel(capturedEvent)} = {UIntValue(capturedEvent, 4)}.",
            RuntimeWatchHookId.ObjArrayFieldInt64Set =>
                $"{HandleLabel(capturedEvent, 0, "Object")} wrote {ArrayFieldLabel(capturedEvent)} = {FormatUInt64(HandleValue(capturedEvent.StackDwords, 4))}.",
            RuntimeWatchHookId.ObjArrayFieldObjSet =>
                $"{HandleLabel(capturedEvent, 0, "Object")} wrote {ArrayFieldLabel(capturedEvent)} = {HandleLabel(capturedEvent, 4, "Value")}.",
            RuntimeWatchHookId.UiStartDialog =>
                $"{HandleLabel(capturedEvent, 0, "Player Character")} started dialog {IntValue(capturedEvent, 6)} with {HandleLabel(capturedEvent, 2, "NPC")}.",
            RuntimeWatchHookId.ReactionAdj =>
                $"{HandleLabel(capturedEvent, 0, "NPC")} reaction toward {HandleLabel(capturedEvent, 2, "Player Character")} {ReactionDirectionText(IntValue(capturedEvent, 4))} by {Math.Abs(IntValue(capturedEvent, 4))}.",
            RuntimeWatchHookId.CritterKill =>
                $"{HandleLabel(capturedEvent, 0, "Critter")} entered death and kill-resolution handling.",
            RuntimeWatchHookId.CritterGiveXp =>
                $"{HandleLabel(capturedEvent, 0, "Player Character")} was awarded {IntValue(capturedEvent, 2)} XP.",
            RuntimeWatchHookId.QuestStateSet =>
                $"Quest {IntValue(capturedEvent, 2)} advanced to {RuntimeWatchValueCatalog.QuestStateName(IntValue(capturedEvent, 3))}.",
            RuntimeWatchHookId.TeleportDo =>
                $"Teleport requested via descriptor {FormatUInt32(capturedEvent.StackDwords.Get(0))}.",
            _ => $"{capturedEvent.Definition.Area} · {capturedEvent.Definition.Description}",
        };

    private static IReadOnlyList<string> CandidateHandles(RuntimeWatchCapturedEvent capturedEvent)
    {
        List<string> candidateHandles = [];
        AddRoleHandleCandidate(candidateHandles, capturedEvent, 0);
        AddRoleHandleCandidate(candidateHandles, capturedEvent, 2);
        AddRoleHandleCandidate(candidateHandles, capturedEvent, 3);
        AddRoleHandleCandidate(candidateHandles, capturedEvent, 4);

        for (var index = 0; index < 7; index++)
        {
            var candidate = HandleValue(capturedEvent.StackDwords, index);
            if (!RuntimeSemanticCatalog.LooksLikeObjectHandle(candidate))
                continue;

            candidateHandles.Add(RuntimeSemanticCatalog.FormatHandle(candidate));
        }

        return [.. candidateHandles.Distinct(StringComparer.Ordinal)];
    }

    private static string? SuggestedHandleHex(
        RuntimeWatchCapturedEvent capturedEvent,
        IReadOnlyList<string> candidateHandles
    )
    {
        foreach (var lowIndex in SuggestedHandleIndexes(capturedEvent.Definition.Id))
        {
            if (TryFormatHandle(capturedEvent.StackDwords, lowIndex, out var handleHex))
                return handleHex;
        }

        return candidateHandles.FirstOrDefault();
    }

    private static IEnumerable<int> SuggestedHandleIndexes(RuntimeWatchHookId hookId) =>
        hookId switch
        {
            RuntimeWatchHookId.ItemInsert
            or RuntimeWatchHookId.ItemEquipped
            or RuntimeWatchHookId.ItemForceRemove
            or RuntimeWatchHookId.ItemUnequipped
            or RuntimeWatchHookId.ObjectDestroy
            or RuntimeWatchHookId.CritterKill
            or RuntimeWatchHookId.CritterGiveXp
            or RuntimeWatchHookId.LevelRecalc
            or RuntimeWatchHookId.UpdateFollowerLevel
            or RuntimeWatchHookId.ObjFieldInt32Set
            or RuntimeWatchHookId.ObjFieldInt64Set
            or RuntimeWatchHookId.ObjFieldHandleSet
            or RuntimeWatchHookId.ObjArrayFieldInt32Set
            or RuntimeWatchHookId.ObjArrayFieldUInt32Set
            or RuntimeWatchHookId.ObjArrayFieldInt64Set
            or RuntimeWatchHookId.ObjArrayFieldObjSet
            or RuntimeWatchHookId.ReactionAdj
            or RuntimeWatchHookId.UiStartDialog => [0, 2, 4],
            RuntimeWatchHookId.ObjectCreate => [0],
            _ => [0, 2, 4],
        };

    private static string ArrayFieldLabel(RuntimeWatchCapturedEvent capturedEvent)
    {
        var fieldId = IntValue(capturedEvent, 2);
        var index = IntValue(capturedEvent, 3);
        return $"{ObjectFieldCatalog.CollectionName(fieldId)}[{ObjectFieldCatalog.ArrayElementName(fieldId, index)}]";
    }

    private static string HandleLabel(RuntimeWatchCapturedEvent capturedEvent, int lowIndex, string fallbackLabel) =>
        TryFormatHandle(capturedEvent.StackDwords, lowIndex, out var handleHex) ? handleHex : fallbackLabel;

    private static bool TryFormatHandle(RuntimeWatchStackCapture stack, int lowIndex, out string handleHex)
    {
        if (lowIndex < 0 || lowIndex + 1 >= 8)
        {
            handleHex = string.Empty;
            return false;
        }

        var handle = HandleValue(stack, lowIndex);
        if (!RuntimeSemanticCatalog.LooksLikeObjectHandle(handle))
        {
            handleHex = string.Empty;
            return false;
        }

        handleHex = RuntimeSemanticCatalog.FormatHandle(handle);
        return true;
    }

    private static void AddRoleHandleCandidate(
        List<string> candidateHandles,
        RuntimeWatchCapturedEvent capturedEvent,
        int lowIndex
    )
    {
        if (TryFormatHandle(capturedEvent.StackDwords, lowIndex, out var handleHex))
            candidateHandles.Add(handleHex);
    }

    private static int IntValue(RuntimeWatchCapturedEvent capturedEvent, int index) =>
        unchecked((int)capturedEvent.StackDwords.Get(index));

    private static uint UIntValue(RuntimeWatchCapturedEvent capturedEvent, int index) =>
        capturedEvent.StackDwords.Get(index);

    private static ulong HandleValue(RuntimeWatchStackCapture stack, int lowIndex) =>
        ((ulong)stack.Get(lowIndex + 1) << 32) | stack.Get(lowIndex);

    private static string FormatUInt32(uint value) => $"0x{value:X8}";

    private static string FormatUInt64(ulong value) => $"0x{value:X16}";

    private static string ReactionSemanticEvent(int delta) =>
        delta switch
        {
            > 0 => "DispositionImproved",
            < 0 => "DispositionWorsened",
            _ => "DispositionChecked",
        };

    private static string ReactionDirectionText(int delta) =>
        delta switch
        {
            > 0 => "improved",
            < 0 => "worsened",
            _ => "held steady",
        };
}
