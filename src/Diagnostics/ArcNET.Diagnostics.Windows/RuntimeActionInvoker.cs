using System.Buffers.Binary;
using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.GameObjects.Metadata;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public static class RuntimeActionInvoker
{
    public static bool TryReadCurrentMapId(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        TimeSpan timeout,
        out int currentMapId
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        try
        {
            using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
            currentMapId = InvokeInt32(memory, dispatcher, s_mapCurrentMap, [], timeout);
            return true;
        }
        catch
        {
            currentMapId = default;
            return false;
        }
    }

    public static RuntimeActionInvocationResult Teleport(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong travelerHandle,
        int tileX,
        int tileY,
        int mapId,
        uint flags,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        return Teleport(memory, dispatcher, travelerHandle, tileX, tileY, mapId, flags, timeout);
    }

    public static WorldMapDiscoveryExecutionResult DiscoverAllWorldMapLocations(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong travelerHandle,
        IReadOnlyList<WorldMapLocationDescriptor> locations,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);
        ArgumentNullException.ThrowIfNull(locations);

        var distinctLocations = locations
            .Where(static location => location.AreaId > 0)
            .GroupBy(static location => location.AreaId)
            .Select(static group => group.First())
            .ToArray();
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var startMapId = InvokeInt32(memory, dispatcher, s_mapByType, [MapTypeStartMap], timeout);
        var currentMapId = InvokeInt32(memory, dispatcher, s_mapCurrentMap, [], timeout);
        var travelerLocation = InvokeUInt64(
            memory,
            dispatcher,
            s_objectFieldInt64Getter,
            [ToLow32(travelerHandle), ToHigh32(travelerHandle), unchecked((uint)s_locationFieldId)],
            timeout
        );
        var townMapId = InvokeInt32(
            memory,
            dispatcher,
            s_townmapGet,
            [ToLow32(CreateSectorId(travelerLocation)), ToHigh32(CreateSectorId(travelerLocation))],
            timeout
        );
        var isTravelerOnWorldMap = currentMapId == startMapId && townMapId == TownMapNone;

        foreach (var location in distinctLocations)
            _ = Invoke(
                memory,
                dispatcher,
                s_areaSetKnown,
                [ToLow32(travelerHandle), ToHigh32(travelerHandle), unchecked((uint)location.AreaId)],
                timeout
            );

        var visitedLocationCount = 0;
        if (isTravelerOnWorldMap)
        {
            foreach (
                var location in distinctLocations.Where(static location => location.WorldX != 0 || location.WorldY != 0)
            )
            {
                _ = Teleport(
                    memory,
                    dispatcher,
                    travelerHandle,
                    location.WorldX,
                    location.WorldY,
                    startMapId,
                    TeleportRenderLockFlag,
                    timeout
                );
                visitedLocationCount++;
            }
        }

        var reloadResult = Invoke(memory, dispatcher, s_worldMapInfoReload, [], timeout);
        var executionDetailText = isTravelerOnWorldMap
            ? $"area_set_known x{distinctLocations.Length} · teleport_do x{visitedLocationCount} · wmap_load_worldmap_info x1"
            : $"area_set_known x{distinctLocations.Length} · wmap_load_worldmap_info x1";
        var resultText =
            $"Map {currentMapId} · start map {startMapId} · town map {townMapId} · EAX {FormatUInt32Result(reloadResult.ResultEax)} · EDX {FormatUInt32Result(reloadResult.ResultEdx)}";
        return new WorldMapDiscoveryExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            executionDetailText,
            resultText,
            distinctLocations.Length,
            visitedLocationCount,
            isTravelerOnWorldMap,
            currentMapId,
            startMapId,
            townMapId
        );
    }

    public static InventoryCreateExecutionResult CreateInventoryItem(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong ownerHandle,
        ulong prototypeHandle,
        int inventoryLocation,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var ownerLocation = InvokeUInt64(
            memory,
            dispatcher,
            s_objectFieldInt64Getter,
            [ToLow32(ownerHandle), ToHigh32(ownerHandle), unchecked((uint)s_locationFieldId)],
            timeout
        );

        using var createdItemBuffer = new RemoteAllocation(memory, sizeof(ulong));
        memory.WriteBytes(createdItemBuffer.Address, new byte[sizeof(ulong)]);
        var createResult = Invoke(
            memory,
            dispatcher,
            s_objectCreate,
            [
                ToLow32(prototypeHandle),
                ToHigh32(prototypeHandle),
                ToLow32(ownerLocation),
                ToHigh32(ownerLocation),
                createdItemBuffer.Address32,
            ],
            timeout
        );
        var createdItemHandle = BinaryPrimitives.ReadUInt64LittleEndian(
            memory.ReadBytes(createdItemBuffer.Address, sizeof(ulong))
        );
        if (!RuntimeSemanticCatalog.LooksLikeObjectHandle(createdItemHandle))
        {
            throw new InvalidOperationException(
                "object_create did not publish a live item handle. Verify the prototype handle and inventory owner."
            );
        }

        var insertResult = Invoke(
            memory,
            dispatcher,
            s_itemInsert,
            [
                ToLow32(createdItemHandle),
                ToHigh32(createdItemHandle),
                ToLow32(ownerHandle),
                ToHigh32(ownerHandle),
                unchecked((uint)inventoryLocation),
            ],
            timeout
        );
        return new InventoryCreateExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"object_create @ {s_objectCreate.Site} · item_insert @ {s_itemInsert.Site}",
            $"Created {RuntimeSemanticCatalog.FormatHandle(createdItemHandle)} · object_create EAX {FormatUInt32Result(createResult.ResultEax)} · item_insert EAX {FormatUInt32Result(insertResult.ResultEax)}",
            createdItemHandle
        );
    }

    public static InventoryDestroyExecutionResult DestroyInventoryItem(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong itemHandle,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var parentHandle = InvokeUInt64(
            memory,
            dispatcher,
            s_objectFieldHandleGetter,
            [ToLow32(itemHandle), ToHigh32(itemHandle), unchecked((uint)s_itemParentFieldId)],
            timeout
        );
        NativeInvocationResult? removeResult = null;
        if (RuntimeSemanticCatalog.LooksLikeObjectHandle(parentHandle))
        {
            removeResult = Invoke(
                memory,
                dispatcher,
                s_itemForceRemove,
                [ToLow32(itemHandle), ToHigh32(itemHandle), ToLow32(parentHandle), ToHigh32(parentHandle)],
                timeout
            );
        }

        _ = Invoke(memory, dispatcher, s_objectDestroy, [ToLow32(itemHandle), ToHigh32(itemHandle)], timeout);
        return new InventoryDestroyExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            removeResult is null
                ? $"obj_field_handle_get @ {s_objectFieldHandleGetter.Site} · object_destroy @ {s_objectDestroy.Site}"
                : $"obj_field_handle_get @ {s_objectFieldHandleGetter.Site} · item_force_remove @ {s_itemForceRemove.Site} · object_destroy @ {s_objectDestroy.Site}",
            removeResult is null
                ? $"Destroyed {RuntimeSemanticCatalog.FormatHandle(itemHandle)} with no live parent handle."
                : $"Removed {RuntimeSemanticCatalog.FormatHandle(itemHandle)} from {RuntimeSemanticCatalog.FormatHandle(parentHandle)} and destroyed it.",
            parentHandle
        );
    }

    public static SheetMutationExecutionResult SetSheetStat(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int statId,
        int value,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        var statName = RuntimeSemanticCatalog.StatName(statId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var currentValue = InvokeInt32(
            memory,
            dispatcher,
            s_statBaseGetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)statId)],
            timeout
        );
        if (currentValue == value)
        {
            return new SheetMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"stat_base_get @ {s_statBaseGetter.Site}",
                $"{statName} is already {value.ToString()} on {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                NoMutation: true
            );
        }

        var result = Invoke(
            memory,
            dispatcher,
            s_statBaseSetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)statId), unchecked((uint)value)],
            timeout
        );
        var updatedValue = InvokeInt32(
            memory,
            dispatcher,
            s_statBaseGetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)statId)],
            timeout
        );
        if (updatedValue != value)
        {
            throw new InvalidOperationException(
                $"Failed to persist {statName} = {value.ToString()} on {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        return new SheetMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"stat_base_get @ {s_statBaseGetter.Site} · stat_base_set @ {s_statBaseSetter.Site}",
            $"{statName} {currentValue.ToString()} -> {updatedValue.ToString()} · EAX {FormatUInt32Result(result.ResultEax)}"
        );
    }

    public static SheetMutationExecutionResult SetSheetResistance(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int resistanceId,
        int value,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        var resistanceName = CharacterSheetMetadata.ResistanceName(resistanceId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var currentValue = InvokeInt32(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Getter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)s_resistanceFieldId), unchecked((uint)resistanceId)],
            timeout
        );
        if (currentValue == value)
        {
            return new SheetMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site}",
                $"{resistanceName} resistance is already {value.ToString()} on {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                NoMutation: true
            );
        }

        _ = Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Setter,
            [
                ToLow32(handle),
                ToHigh32(handle),
                unchecked((uint)s_resistanceFieldId),
                unchecked((uint)resistanceId),
                unchecked((uint)value),
            ],
            timeout
        );
        var updatedValue = InvokeInt32(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Getter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)s_resistanceFieldId), unchecked((uint)resistanceId)],
            timeout
        );
        if (updatedValue != value)
        {
            throw new InvalidOperationException(
                $"Failed to persist {resistanceName} resistance = {value.ToString()} on {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        return new SheetMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site} · obj_array_field_int32_set @ {s_objectArrayFieldInt32Setter.Site}",
            $"{resistanceName} resistance {currentValue.ToString()} -> {updatedValue.ToString()}"
        );
    }

    public static SheetMutationExecutionResult SetBasicSkill(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int skillId,
        int points,
        int? training,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        return SetEncodedSkill(
            memory,
            runtimeProfile,
            handle,
            s_basicSkillFieldId,
            skillId,
            points,
            training,
            SkillPointsStatId,
            CharacterSheetMetadata.BasicSkillName(skillId),
            timeout
        );
    }

    public static SheetMutationExecutionResult SetTechSkill(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int skillId,
        int points,
        int? training,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        return SetEncodedSkill(
            memory,
            runtimeProfile,
            handle,
            s_techSkillFieldId,
            skillId,
            points,
            training,
            TechPointsStatId,
            CharacterSheetMetadata.TechSkillName(skillId),
            timeout
        );
    }

    public static SheetMutationExecutionResult SetSheetSpellCollegeLevel(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int collegeId,
        int level,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        var result = SetSpellCollegeLevel(memory, runtimeProfile, handle, collegeId, level, timeout);
        return new SheetMutationExecutionResult(
            result.DispatcherMode,
            result.DispatcherSite,
            result.ExecutionDetailText,
            result.ResultText,
            result.NoMutation
        );
    }

    public static SheetMutationExecutionResult SetSpellMastery(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int masteryCollegeId,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var currentValue = InvokeInt32(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Getter,
            [
                ToLow32(handle),
                ToHigh32(handle),
                unchecked((uint)s_spellTechFieldId),
                unchecked((uint)SpellTechCatalog.SpellCollegeCount),
            ],
            timeout
        );
        if (currentValue == masteryCollegeId)
        {
            return new SheetMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site}",
                $"Spell Mastery is already {FormatSpellMastery(masteryCollegeId)} on {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                NoMutation: true
            );
        }

        _ = Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldUInt32Setter,
            [
                ToLow32(handle),
                ToHigh32(handle),
                unchecked((uint)s_spellTechFieldId),
                unchecked((uint)SpellTechCatalog.SpellCollegeCount),
                unchecked((uint)masteryCollegeId),
            ],
            timeout
        );
        var updatedValue = InvokeInt32(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Getter,
            [
                ToLow32(handle),
                ToHigh32(handle),
                unchecked((uint)s_spellTechFieldId),
                unchecked((uint)SpellTechCatalog.SpellCollegeCount),
            ],
            timeout
        );
        if (updatedValue != masteryCollegeId)
        {
            throw new InvalidOperationException(
                $"Failed to persist Spell Mastery = {FormatSpellMastery(masteryCollegeId)} on {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        return new SheetMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site} · obj_array_field_uint32_set @ {s_objectArrayFieldUInt32Setter.Site}",
            $"Spell Mastery {FormatSpellMastery(currentValue)} -> {FormatSpellMastery(updatedValue)}"
        );
    }

    public static SheetMutationExecutionResult SetSheetTechDisciplineLevel(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int disciplineId,
        int level,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        var result = SetTechDisciplineLevel(memory, runtimeProfile, handle, disciplineId, level, timeout);
        return new SheetMutationExecutionResult(
            result.DispatcherMode,
            result.DispatcherSite,
            result.ExecutionDetailText,
            result.ResultText,
            result.NoMutation
        );
    }

    public static MobileMutationExecutionResult SetMobileStat(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int statId,
        int value,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var result = Invoke(
            memory,
            dispatcher,
            s_statBaseSetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)statId), unchecked((uint)value)],
            timeout
        );
        return new MobileMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"stat_base_set @ {s_statBaseSetter.Site}",
            $"Set {RuntimeSemanticCatalog.StatName(statId)} on {RuntimeSemanticCatalog.FormatHandle(handle)} to {value.ToString()} · EAX {FormatUInt32Result(result.ResultEax)}",
            handle
        );
    }

    public static MobileMutationExecutionResult KillMobile(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var result = Invoke(memory, dispatcher, s_critterKill, [ToLow32(handle), ToHigh32(handle)], timeout);
        return new MobileMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"critter_kill @ {s_critterKill.Site}",
            $"Triggered critter_kill for {RuntimeSemanticCatalog.FormatHandle(handle)} · EAX {FormatUInt32Result(result.ResultEax)}",
            handle
        );
    }

    public static MobileMutationExecutionResult DespawnMobile(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var result = Invoke(memory, dispatcher, s_objectDestroy, [ToLow32(handle), ToHigh32(handle)], timeout);
        return new MobileMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"object_destroy @ {s_objectDestroy.Site}",
            $"Destroyed {RuntimeSemanticCatalog.FormatHandle(handle)} · EAX {FormatUInt32Result(result.ResultEax)}",
            handle
        );
    }

    public static MobileMutationExecutionResult SpawnMobile(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong prototypeHandle,
        ulong anchorHandle,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var anchorLocation = InvokeUInt64(
            memory,
            dispatcher,
            s_objectFieldInt64Getter,
            [ToLow32(anchorHandle), ToHigh32(anchorHandle), unchecked((uint)s_locationFieldId)],
            timeout
        );

        using var createdMobileBuffer = new RemoteAllocation(memory, sizeof(ulong));
        memory.WriteBytes(createdMobileBuffer.Address, new byte[sizeof(ulong)]);
        var createResult = Invoke(
            memory,
            dispatcher,
            s_objectCreate,
            [
                ToLow32(prototypeHandle),
                ToHigh32(prototypeHandle),
                ToLow32(anchorLocation),
                ToHigh32(anchorLocation),
                createdMobileBuffer.Address32,
            ],
            timeout
        );
        var createdMobileHandle = BinaryPrimitives.ReadUInt64LittleEndian(
            memory.ReadBytes(createdMobileBuffer.Address, sizeof(ulong))
        );
        if (!RuntimeSemanticCatalog.LooksLikeObjectHandle(createdMobileHandle))
        {
            throw new InvalidOperationException(
                "object_create did not publish a live mobile handle. Verify the prototype handle and spawn anchor."
            );
        }

        return new MobileMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"obj_field_int64_get @ {s_objectFieldInt64Getter.Site} · object_create @ {s_objectCreate.Site}",
            $"Created {RuntimeSemanticCatalog.FormatHandle(createdMobileHandle)} at {RuntimeSemanticCatalog.FormatHandle(anchorHandle)} · object_create EAX {FormatUInt32Result(createResult.ResultEax)}",
            createdMobileHandle
        );
    }

    public static SpellTechMutationExecutionResult AddSpell(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int spellId,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        var collegeId = spellId / SpellTechCatalog.SpellMaxLevel;
        var spellLevel = (spellId % SpellTechCatalog.SpellMaxLevel) + 1;
        var collegeName = SpellTechCatalog.SpellCollegeName(collegeId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var currentLevel = InvokeInt32(
            memory,
            dispatcher,
            s_spellCollegeLevelGetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)collegeId)],
            timeout
        );
        if (currentLevel >= spellLevel)
        {
            return new SpellTechMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"spell_college_level_get @ {s_spellCollegeLevelGetter.Site}",
                $"{collegeName} already covers rank {spellLevel.ToString()} for {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                NoMutation: true
            );
        }

        var beforeMagickPoints = InvokeInt32(
            memory,
            dispatcher,
            s_statBaseGetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)MagickPointsStatId)],
            timeout
        );
        var result = Invoke(
            memory,
            dispatcher,
            s_spellAdd,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)spellId), 1u],
            timeout
        );
        var updatedLevel = InvokeInt32(
            memory,
            dispatcher,
            s_spellCollegeLevelGetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)collegeId)],
            timeout
        );
        var afterMagickPoints = InvokeInt32(
            memory,
            dispatcher,
            s_statBaseGetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)MagickPointsStatId)],
            timeout
        );
        if (updatedLevel < spellLevel)
        {
            throw new InvalidOperationException(
                $"spell_add did not raise {collegeName} to rank {spellLevel.ToString()} for {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        return new SpellTechMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"spell_college_level_get @ {s_spellCollegeLevelGetter.Site} · stat_base_get @ {s_statBaseGetter.Site} · spell_add @ {s_spellAdd.Site}",
            $"{collegeName} {currentLevel.ToString()} -> {updatedLevel.ToString()} · magick points {beforeMagickPoints.ToString()} -> {afterMagickPoints.ToString()} · EAX {FormatUInt32Result(result.ResultEax)}"
        );
    }

    public static SpellTechMutationExecutionResult GrantSchematic(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int schematicId,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var currentLength = Math.Max(
            0,
            InvokeInt32(
                memory,
                dispatcher,
                s_objectArrayFieldLengthGetter,
                [ToLow32(handle), ToHigh32(handle), unchecked((uint)s_pcSchematicsFoundFieldId)],
                timeout
            )
        );
        var scanCount = Math.Min(currentLength, MaxSchematicScanCount);
        for (var index = 0; index < scanCount; index++)
        {
            var existingValue = InvokeInt32(
                memory,
                dispatcher,
                s_objectArrayFieldInt32Getter,
                [
                    ToLow32(handle),
                    ToHigh32(handle),
                    unchecked((uint)s_pcSchematicsFoundFieldId),
                    unchecked((uint)index),
                ],
                timeout
            );
            if (existingValue == schematicId)
            {
                return new SpellTechMutationExecutionResult(
                    dispatcher.ModeDescription,
                    dispatcher.SiteDescription,
                    $"obj_array_field_length_get @ {s_objectArrayFieldLengthGetter.Site} · obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site}",
                    $"Schematic {schematicId.ToString()} is already stored at slot {index.ToString()} on {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                    NoMutation: true,
                    RelatedIndex: index
                );
            }
        }

        var targetSlot = currentLength;
        _ = Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldUInt32Setter,
            [
                ToLow32(handle),
                ToHigh32(handle),
                unchecked((uint)s_pcSchematicsFoundFieldId),
                unchecked((uint)targetSlot),
                unchecked((uint)schematicId),
            ],
            timeout
        );
        var storedValue = InvokeInt32(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Getter,
            [
                ToLow32(handle),
                ToHigh32(handle),
                unchecked((uint)s_pcSchematicsFoundFieldId),
                unchecked((uint)targetSlot),
            ],
            timeout
        );
        if (storedValue != schematicId)
        {
            throw new InvalidOperationException(
                $"Failed to store schematic {schematicId.ToString()} on {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        var updatedLength = Math.Max(
            0,
            InvokeInt32(
                memory,
                dispatcher,
                s_objectArrayFieldLengthGetter,
                [ToLow32(handle), ToHigh32(handle), unchecked((uint)s_pcSchematicsFoundFieldId)],
                timeout
            )
        );
        return new SpellTechMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"obj_array_field_length_get @ {s_objectArrayFieldLengthGetter.Site} · obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site} · obj_array_field_uint32_set @ {s_objectArrayFieldUInt32Setter.Site}",
            $"Stored schematic {schematicId.ToString()} in slot {targetSlot.ToString()} on {RuntimeSemanticCatalog.FormatHandle(handle)} · length {currentLength.ToString()} -> {updatedLength.ToString()}",
            RelatedIndex: targetSlot
        );
    }

    public static SpellTechMutationExecutionResult RemoveSchematic(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int schematicId,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var currentLength = Math.Max(
            0,
            InvokeInt32(
                memory,
                dispatcher,
                s_objectArrayFieldLengthGetter,
                [ToLow32(handle), ToHigh32(handle), unchecked((uint)s_pcSchematicsFoundFieldId)],
                timeout
            )
        );
        var scanCount = Math.Min(currentLength, MaxSchematicScanCount);
        var schematicIndex = -1;
        for (var index = 0; index < scanCount; index++)
        {
            var existingValue = InvokeInt32(
                memory,
                dispatcher,
                s_objectArrayFieldInt32Getter,
                [
                    ToLow32(handle),
                    ToHigh32(handle),
                    unchecked((uint)s_pcSchematicsFoundFieldId),
                    unchecked((uint)index),
                ],
                timeout
            );
            if (existingValue != schematicId)
                continue;

            schematicIndex = index;
            break;
        }

        if (schematicIndex < 0)
        {
            return new SpellTechMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"obj_array_field_length_get @ {s_objectArrayFieldLengthGetter.Site} · obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site}",
                $"Schematic {schematicId.ToString()} is not currently stored on {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                NoMutation: true
            );
        }

        for (var index = schematicIndex + 1; index < currentLength; index++)
        {
            var shiftedValue = InvokeInt32(
                memory,
                dispatcher,
                s_objectArrayFieldInt32Getter,
                [
                    ToLow32(handle),
                    ToHigh32(handle),
                    unchecked((uint)s_pcSchematicsFoundFieldId),
                    unchecked((uint)index),
                ],
                timeout
            );
            _ = Invoke(
                memory,
                dispatcher,
                s_objectArrayFieldUInt32Setter,
                [
                    ToLow32(handle),
                    ToHigh32(handle),
                    unchecked((uint)s_pcSchematicsFoundFieldId),
                    unchecked((uint)(index - 1)),
                    unchecked((uint)shiftedValue),
                ],
                timeout
            );
        }

        _ = Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldLengthSetter,
            [
                ToLow32(handle),
                ToHigh32(handle),
                unchecked((uint)s_pcSchematicsFoundFieldId),
                unchecked((uint)(currentLength - 1)),
            ],
            timeout
        );
        var updatedLength = Math.Max(
            0,
            InvokeInt32(
                memory,
                dispatcher,
                s_objectArrayFieldLengthGetter,
                [ToLow32(handle), ToHigh32(handle), unchecked((uint)s_pcSchematicsFoundFieldId)],
                timeout
            )
        );
        var updatedScanCount = Math.Min(updatedLength, MaxSchematicScanCount);
        for (var index = 0; index < updatedScanCount; index++)
        {
            var existingValue = InvokeInt32(
                memory,
                dispatcher,
                s_objectArrayFieldInt32Getter,
                [
                    ToLow32(handle),
                    ToHigh32(handle),
                    unchecked((uint)s_pcSchematicsFoundFieldId),
                    unchecked((uint)index),
                ],
                timeout
            );
            if (existingValue == schematicId)
            {
                throw new InvalidOperationException(
                    $"Failed to remove schematic {schematicId.ToString()} from {RuntimeSemanticCatalog.FormatHandle(handle)}."
                );
            }
        }

        if (updatedLength != currentLength - 1)
        {
            throw new InvalidOperationException(
                $"Failed to shrink the schematic list after removing {schematicId.ToString()} from {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        return new SpellTechMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"obj_array_field_length_get @ {s_objectArrayFieldLengthGetter.Site} · obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site} · obj_array_field_uint32_set @ {s_objectArrayFieldUInt32Setter.Site} · obj_array_field_length_set @ {s_objectArrayFieldLengthSetter.Site}",
            $"Removed schematic {schematicId.ToString()} from slot {schematicIndex.ToString()} on {RuntimeSemanticCatalog.FormatHandle(handle)} · length {currentLength.ToString()} -> {updatedLength.ToString()}",
            RelatedIndex: schematicIndex
        );
    }

    public static SpellTechMutationExecutionResult SetSpellCollegeLevel(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int collegeId,
        int level,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        var collegeName = SpellTechCatalog.SpellCollegeName(collegeId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var currentLevel = InvokeInt32(
            memory,
            dispatcher,
            s_spellCollegeLevelGetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)collegeId)],
            timeout
        );
        if (currentLevel == level)
        {
            return new SpellTechMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"spell_college_level_get @ {s_spellCollegeLevelGetter.Site}",
                $"{collegeName} is already rank {level.ToString()} on {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                NoMutation: true
            );
        }

        var result = Invoke(
            memory,
            dispatcher,
            s_spellCollegeLevelSetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)collegeId), unchecked((uint)level)],
            timeout
        );
        var updatedLevel = InvokeInt32(
            memory,
            dispatcher,
            s_spellCollegeLevelGetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)collegeId)],
            timeout
        );
        if (updatedLevel != level)
        {
            throw new InvalidOperationException(
                $"spell_college_level_set did not persist {collegeName} rank {level.ToString()} on {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        return new SpellTechMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"spell_college_level_get @ {s_spellCollegeLevelGetter.Site} · spell_college_level_set @ {s_spellCollegeLevelSetter.Site}",
            $"{collegeName} {currentLevel.ToString()} -> {updatedLevel.ToString()} · EAX {FormatUInt32Result(result.ResultEax)}"
        );
    }

    public static SpellTechMutationExecutionResult SetTechDisciplineLevel(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int disciplineId,
        int level,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        var disciplineName = SpellTechCatalog.TechDisciplineName(disciplineId);
        var fieldIndex = SpellTechCatalog.TechDisciplineBaseIndex + disciplineId;
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var currentLevel = InvokeInt32(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Getter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)s_spellTechFieldId), unchecked((uint)fieldIndex)],
            timeout
        );
        if (currentLevel == level)
        {
            return new SpellTechMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site}",
                $"{disciplineName} is already degree {level.ToString()} on {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                NoMutation: true
            );
        }

        _ = Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldUInt32Setter,
            [
                ToLow32(handle),
                ToHigh32(handle),
                unchecked((uint)s_spellTechFieldId),
                unchecked((uint)fieldIndex),
                unchecked((uint)level),
            ],
            timeout
        );
        var updatedLevel = InvokeInt32(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Getter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)s_spellTechFieldId), unchecked((uint)fieldIndex)],
            timeout
        );
        if (updatedLevel != level)
        {
            throw new InvalidOperationException(
                $"Failed to persist {disciplineName} degree {level.ToString()} on {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        return new SpellTechMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site} · obj_array_field_uint32_set @ {s_objectArrayFieldUInt32Setter.Site}",
            $"{disciplineName} {currentLevel.ToString()} -> {updatedLevel.ToString()}"
        );
    }

    public static SpellTechMutationExecutionResult SetTechSkillPoints(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int skillId,
        int points,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        var skillName = SpellTechCatalog.TechSkillName(skillId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var currentRaw = InvokeInt32(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Getter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)s_techSkillFieldId), unchecked((uint)skillId)],
            timeout
        );
        var currentPoints = currentRaw & SpellTechPointMask;
        var currentTechPoints = InvokeInt32(
            memory,
            dispatcher,
            s_statBaseGetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)TechPointsStatId)],
            timeout
        );
        if (currentPoints == points)
        {
            return new SpellTechMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site}",
                $"{skillName} already has {points.ToString()} point(s) on {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                NoMutation: true
            );
        }

        var updatedRaw = (currentRaw & ~SpellTechPointMask) | points;
        _ = Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Setter,
            [
                ToLow32(handle),
                ToHigh32(handle),
                unchecked((uint)s_techSkillFieldId),
                unchecked((uint)skillId),
                unchecked((uint)updatedRaw),
            ],
            timeout
        );
        var confirmedRaw = InvokeInt32(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Getter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)s_techSkillFieldId), unchecked((uint)skillId)],
            timeout
        );
        var confirmedPoints = confirmedRaw & SpellTechPointMask;
        if (confirmedPoints != points)
        {
            throw new InvalidOperationException(
                $"Failed to persist {skillName} points {points.ToString()} on {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        var updatedTechPoints = currentTechPoints + points - currentPoints;
        _ = Invoke(
            memory,
            dispatcher,
            s_statBaseSetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)TechPointsStatId), unchecked((uint)updatedTechPoints)],
            timeout
        );
        var confirmedTechPoints = InvokeInt32(
            memory,
            dispatcher,
            s_statBaseGetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)TechPointsStatId)],
            timeout
        );
        if (confirmedTechPoints != updatedTechPoints)
        {
            throw new InvalidOperationException(
                $"Failed to adjust tech points after writing {skillName} on {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        return new SpellTechMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            $"obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site} · obj_array_field_int32_set @ {s_objectArrayFieldInt32Setter.Site} · stat_base_get @ {s_statBaseGetter.Site} · stat_base_set @ {s_statBaseSetter.Site}",
            $"{skillName} {currentPoints.ToString()} -> {confirmedPoints.ToString()} · tech points {currentTechPoints.ToString()} -> {confirmedTechPoints.ToString()}"
        );
    }

    private static RuntimeActionInvocationResult Teleport(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        ulong travelerHandle,
        int tileX,
        int tileY,
        int mapId,
        uint flags,
        TimeSpan timeout
    )
    {
        var function = FunctionCatalog.GetDefinition("teleport_do");
        using var teleportBuffer = CreateTeleportBuffer(memory, travelerHandle, tileX, tileY, mapId, flags);
        var targetAddress = memory.ResolveRva(function.Rva);
        var result = dispatcher.Invoke(
            memory.ToUInt32Address(targetAddress),
            function.SuggestedCleanup,
            0,
            0,
            [teleportBuffer.Address32],
            timeout
        );

        return new RuntimeActionInvocationResult(
            function.Key,
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            ProcessMemory.FormatAddress(targetAddress),
            result.ResultEax,
            result.ResultEdx,
            result.State.ToString()
        );
    }

    private static RemoteAllocation CreateTeleportBuffer(
        ProcessMemory memory,
        ulong travelerHandle,
        int tileX,
        int tileY,
        int mapId,
        uint flags
    )
    {
        var teleportData = new byte[0x5C];
        BinaryPrimitives.WriteUInt32LittleEndian(teleportData.AsSpan(0x00), flags);
        BinaryPrimitives.WriteUInt64LittleEndian(teleportData.AsSpan(0x08), travelerHandle);
        BinaryPrimitives.WriteUInt64LittleEndian(teleportData.AsSpan(0x10), PackLocation(tileX, tileY));
        BinaryPrimitives.WriteInt32LittleEndian(teleportData.AsSpan(0x18), mapId);

        var teleportBuffer = new RemoteAllocation(memory, teleportData.Length);
        memory.WriteBytes(teleportBuffer.Address, teleportData);
        return teleportBuffer;
    }

    private static NativeInvocationResult Invoke(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        FunctionDefinition function,
        IReadOnlyList<uint> stackArguments,
        TimeSpan timeout
    ) => NativeInvoker.Invoke(dispatcher, memory, function.Key, stackArguments, timeout);

    private static int InvokeInt32(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        FunctionDefinition function,
        IReadOnlyList<uint> stackArguments,
        TimeSpan timeout
    ) => unchecked((int)Invoke(memory, dispatcher, function, stackArguments, timeout).ResultEax);

    private static ulong InvokeUInt64(
        ProcessMemory memory,
        RuntimeCallDispatcher dispatcher,
        FunctionDefinition function,
        IReadOnlyList<uint> stackArguments,
        TimeSpan timeout
    )
    {
        var invocation = Invoke(memory, dispatcher, function, stackArguments, timeout);
        return invocation.ResultEax | ((ulong)invocation.ResultEdx << 32);
    }

    private static ulong PackLocation(int x, int y) => (uint)x | ((ulong)(uint)y << 32);

    private static ulong CreateSectorId(ulong location)
    {
        var x = (uint)location >> 6;
        var y = (uint)(location >> 32) >> 6;
        return x | ((ulong)y << 26);
    }

    private static SheetMutationExecutionResult SetEncodedSkill(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int fieldId,
        int skillId,
        int points,
        int? training,
        int pointStatId,
        string skillName,
        TimeSpan timeout
    )
    {
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var currentRaw = InvokeInt32(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Getter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)fieldId), unchecked((uint)skillId)],
            timeout
        );
        var currentPoints = currentRaw & SpellTechPointMask;
        var currentTraining = (currentRaw >> SkillTrainingShift) & SkillTrainingMaskValue;
        var updatedTraining = training ?? currentTraining;
        var updatedRaw =
            (currentRaw & ~SkillEncodedMask)
            | (points & SpellTechPointMask)
            | ((updatedTraining & SkillTrainingMaskValue) << SkillTrainingShift);

        if (updatedRaw == currentRaw)
        {
            return new SheetMutationExecutionResult(
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                $"obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site}",
                $"{skillName} already has {points.ToString()} point(s) and {CharacterSheetMetadata.TrainingName(updatedTraining)} training on {RuntimeSemanticCatalog.FormatHandle(handle)}.",
                NoMutation: true
            );
        }

        var currentPointStat = InvokeInt32(
            memory,
            dispatcher,
            s_statBaseGetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)pointStatId)],
            timeout
        );
        _ = Invoke(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Setter,
            [
                ToLow32(handle),
                ToHigh32(handle),
                unchecked((uint)fieldId),
                unchecked((uint)skillId),
                unchecked((uint)updatedRaw),
            ],
            timeout
        );
        var confirmedRaw = InvokeInt32(
            memory,
            dispatcher,
            s_objectArrayFieldInt32Getter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)fieldId), unchecked((uint)skillId)],
            timeout
        );
        var confirmedPoints = confirmedRaw & SpellTechPointMask;
        var confirmedTraining = (confirmedRaw >> SkillTrainingShift) & SkillTrainingMaskValue;
        if (confirmedPoints != points || confirmedTraining != updatedTraining)
        {
            throw new InvalidOperationException(
                $"Failed to persist {skillName} = {points.ToString()} with {CharacterSheetMetadata.TrainingName(updatedTraining)} training on {RuntimeSemanticCatalog.FormatHandle(handle)}."
            );
        }

        var updatedPointStat = currentPointStat + points - currentPoints;
        var confirmedPointStat = currentPointStat;
        if (updatedPointStat != currentPointStat)
        {
            _ = Invoke(
                memory,
                dispatcher,
                s_statBaseSetter,
                [ToLow32(handle), ToHigh32(handle), unchecked((uint)pointStatId), unchecked((uint)updatedPointStat)],
                timeout
            );
            confirmedPointStat = InvokeInt32(
                memory,
                dispatcher,
                s_statBaseGetter,
                [ToLow32(handle), ToHigh32(handle), unchecked((uint)pointStatId)],
                timeout
            );
            if (confirmedPointStat != updatedPointStat)
            {
                throw new InvalidOperationException(
                    $"Failed to adjust {RuntimeSemanticCatalog.StatName(pointStatId)} after writing {skillName} on {RuntimeSemanticCatalog.FormatHandle(handle)}."
                );
            }
        }

        var executionDetailText =
            updatedPointStat == currentPointStat
                ? $"obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site} · obj_array_field_int32_set @ {s_objectArrayFieldInt32Setter.Site} · stat_base_get @ {s_statBaseGetter.Site}"
                : $"obj_array_field_int32_get @ {s_objectArrayFieldInt32Getter.Site} · obj_array_field_int32_set @ {s_objectArrayFieldInt32Setter.Site} · stat_base_get @ {s_statBaseGetter.Site} · stat_base_set @ {s_statBaseSetter.Site}";
        return new SheetMutationExecutionResult(
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            executionDetailText,
            $"{skillName} {currentPoints.ToString()} ({CharacterSheetMetadata.TrainingName(currentTraining)}) -> {confirmedPoints.ToString()} ({CharacterSheetMetadata.TrainingName(confirmedTraining)}) · {RuntimeSemanticCatalog.StatName(pointStatId)} {currentPointStat.ToString()} -> {confirmedPointStat.ToString()}"
        );
    }

    private static string FormatSpellMastery(int masteryCollegeId) =>
        masteryCollegeId is >= 0 and < SpellTechCatalog.SpellCollegeCount
            ? CharacterSheetMetadata.SpellCollegeName(masteryCollegeId)
            : "None";

    private static uint ToLow32(ulong value) => unchecked((uint)(value & uint.MaxValue));

    private static uint ToHigh32(ulong value) => unchecked((uint)(value >> 32));

    private static string FormatUInt32Result(uint value) => $"0x{value:X8} ({unchecked((int)value)})";

    private static int ResolveFieldId(string rawName)
    {
        if (ObjectFieldCatalog.TryGetFieldId(rawName, out var fieldId))
            return fieldId;

        throw new InvalidOperationException($"Unable to resolve runtime object field '{rawName}'.");
    }

    private sealed class RemoteAllocation(ProcessMemory memory, int byteLength) : IDisposable
    {
        public nint Address { get; } = memory.AllocateWritable(byteLength);

        public uint Address32 => memory.ToUInt32Address(Address);

        public void Dispose() => memory.TryFree(Address);
    }

    private static readonly FunctionDefinition s_objectFieldInt64Getter = FunctionCatalog.GetDefinition(
        "obj_field_int64_get"
    );
    private static readonly FunctionDefinition s_objectFieldHandleGetter = FunctionCatalog.GetDefinition(
        "obj_field_handle_get"
    );
    private static readonly FunctionDefinition s_objectArrayFieldInt32Getter = FunctionCatalog.GetDefinition(
        "obj_array_field_int32_get"
    );
    private static readonly FunctionDefinition s_objectArrayFieldInt32Setter = FunctionCatalog.GetDefinition(
        "obj_array_field_int32_set"
    );
    private static readonly FunctionDefinition s_objectArrayFieldUInt32Setter = FunctionCatalog.GetDefinition(
        "obj_array_field_uint32_set"
    );
    private static readonly FunctionDefinition s_objectArrayFieldLengthGetter = FunctionCatalog.GetDefinition(
        "obj_array_field_length_get"
    );
    private static readonly FunctionDefinition s_objectArrayFieldLengthSetter = FunctionCatalog.GetDefinition(
        "obj_array_field_length_set"
    );
    private static readonly FunctionDefinition s_statBaseGetter = FunctionCatalog.GetDefinition("stat_base_get");
    private static readonly FunctionDefinition s_statBaseSetter = FunctionCatalog.GetDefinition("stat_base_set");
    private static readonly FunctionDefinition s_critterKill = FunctionCatalog.GetDefinition("critter_kill");
    private static readonly FunctionDefinition s_objectCreate = FunctionCatalog.GetDefinition("object_create");
    private static readonly FunctionDefinition s_objectDestroy = FunctionCatalog.GetDefinition("object_destroy");
    private static readonly FunctionDefinition s_itemInsert = FunctionCatalog.GetDefinition("item_insert");
    private static readonly FunctionDefinition s_itemForceRemove = FunctionCatalog.GetDefinition("item_force_remove");
    private static readonly FunctionDefinition s_spellAdd = FunctionCatalog.GetDefinition("spell_add");
    private static readonly FunctionDefinition s_spellCollegeLevelGetter = FunctionCatalog.GetDefinition(
        "spell_college_level_get"
    );
    private static readonly FunctionDefinition s_spellCollegeLevelSetter = FunctionCatalog.GetDefinition(
        "spell_college_level_set"
    );
    private static readonly FunctionDefinition s_areaSetKnown = FunctionCatalog.GetDefinition("area_set_known");
    private static readonly FunctionDefinition s_worldMapInfoReload = FunctionCatalog.GetDefinition(
        "wmap_load_worldmap_info"
    );
    private static readonly FunctionDefinition s_mapCurrentMap = FunctionCatalog.GetDefinition("map_current_map");
    private static readonly FunctionDefinition s_mapByType = FunctionCatalog.GetDefinition("map_by_type");
    private static readonly FunctionDefinition s_townmapGet = FunctionCatalog.GetDefinition("townmap_get");
    private static readonly int s_locationFieldId = ResolveFieldId("OBJ_F_LOCATION");
    private static readonly int s_itemParentFieldId = ResolveFieldId("OBJ_F_ITEM_PARENT");
    private static readonly int s_pcSchematicsFoundFieldId = ResolveFieldId("OBJ_F_PC_SCHEMATICS_FOUND_IDX");
    private static readonly int s_resistanceFieldId = ResolveFieldId("OBJ_F_RESISTANCE_IDX");
    private static readonly int s_basicSkillFieldId = ResolveFieldId("OBJ_F_CRITTER_BASIC_SKILL_IDX");
    private static readonly int s_spellTechFieldId = ResolveFieldId("OBJ_F_CRITTER_SPELL_TECH_IDX");
    private static readonly int s_techSkillFieldId = ResolveFieldId("OBJ_F_CRITTER_TECH_SKILL_IDX");
    private const int MapTypeStartMap = 1;
    private const int TownMapNone = 0;
    private const int MagickPointsStatId = 22;
    private const int SkillPointsStatId = 21;
    private const int TechPointsStatId = 23;
    private const int SpellTechPointMask = SpellTechCatalog.TechSkillPointMask;
    private const int SkillTrainingShift = 6;
    private const int SkillTrainingMaskValue = 3;
    private const int SkillEncodedMask = 0xFF;
    private const int MaxSchematicScanCount = 256;
    private const uint TeleportRenderLockFlag = 0x0020;
}
