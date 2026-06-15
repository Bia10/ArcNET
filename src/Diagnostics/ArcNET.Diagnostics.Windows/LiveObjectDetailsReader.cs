using System.Globalization;
using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.GameObjects;
using ArcNET.GameObjects.Metadata;
using ArcNET.GameObjects.Runtime;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class LiveObjectDetailsReader(ProcessMemory memory, RuntimeProfileSnapshot runtimeProfile) : IDisposable
{
    private readonly ProcessMemory _memory = memory;
    private readonly RuntimeProfileSnapshot _runtimeProfile = runtimeProfile;
    private RuntimeCallDispatcher? _dispatcher;
    private bool _dispatcherUnavailable;

    public LiveObjectInspection Inspect(ulong handle)
    {
        var identity = LiveObjectInspector.Inspect(_memory, handle);
        if (!identity.HasHeader || !TryGetDispatcher(out var dispatcher))
            return new LiveObjectInspection(identity, []);

        List<LiveObjectDetail> details = [];
        AppendCommonDetails(details, dispatcher, handle);

        if (!Enum.IsDefined((ObjectType)identity.Header!.Value.ObjectTypeRaw))
            return new LiveObjectInspection(identity, [.. details]);

        switch ((ObjectType)identity.Header.Value.ObjectTypeRaw)
        {
            case ObjectType.Pc:
                // Keep the experimental aggregate path dormant until it is validated against live classic/UAP runtimes.
                var directSheet = EnableExperimentalDirectCharacterAggregates
                    ? TryReadDirectCharacterSheet(identity)
                    : null;
                AppendCritterDetails(details, dispatcher, handle, directSheet);
                AppendPcDetails(details, dispatcher, handle);
                break;

            case ObjectType.Npc:
                AppendCritterDetails(details, dispatcher, handle, directSheet: null);
                AppendNpcDetails(details, dispatcher, handle);
                break;

            case ObjectType.Container:
                AppendContainerDetails(details, dispatcher, handle);
                break;

            case ObjectType.Portal:
                AppendPortalDetails(details, dispatcher, handle);
                break;

            case ObjectType.Projectile:
                AppendFieldDetail(details, dispatcher, handle, s_projectileHitLocFieldId);
                break;

            case ObjectType.Trap:
                AppendFieldDetail(details, dispatcher, handle, s_trapDifficultyFieldId);
                break;

            default:
                if (IsItemObjectType((ObjectType)identity.Header.Value.ObjectTypeRaw))
                    AppendItemDetails(details, dispatcher, handle, (ObjectType)identity.Header.Value.ObjectTypeRaw);
                break;
        }

        return new LiveObjectInspection(identity, [.. details]);
    }

    public void Dispose() => _dispatcher?.Dispose();

    private void AppendCommonDetails(List<LiveObjectDetail> details, RuntimeCallDispatcher dispatcher, ulong handle)
    {
        AppendFieldDetail(details, dispatcher, handle, s_currentAidFieldId);
        AppendFieldDetail(details, dispatcher, handle, s_armorClassFieldId);

        var hitPoints = AppendFieldDetail(details, dispatcher, handle, s_hitPointsFieldId);
        var hitPointDamage = AppendFieldDetail(details, dispatcher, handle, s_hitPointDamageFieldId);
        if (hitPoints is { } maxHitPoints && hitPointDamage is { } damage)
        {
            details.Add(
                new LiveObjectDetail(
                    "health_remaining",
                    "Health Remaining",
                    (maxHitPoints - damage).ToString(CultureInfo.InvariantCulture),
                    "Computed"
                )
            );
        }
    }

    private void AppendCritterDetails(
        List<LiveObjectDetail> details,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        DirectCharacterSheetSnapshot? directSheet
    )
    {
        if (directSheet is { } snapshot)
        {
            AppendDirectMainStats(details, snapshot.MainStats);
            AppendDirectProgression(details, dispatcher, handle, snapshot.MainStats);
        }
        else
        {
            AppendStatRange(details, dispatcher, handle, 0, PrimaryStatCount);
            AppendStatRange(details, dispatcher, handle, ProgressionStatStartIndex, ProgressionStatCount);
        }

        AppendStatRange(details, dispatcher, handle, DerivedStatStartIndex, DerivedStatCount);

        for (var resistanceId = 0; resistanceId < ResistanceCount; resistanceId++)
            AppendResistanceDetail(details, dispatcher, handle, resistanceId);

        if (directSheet is { } directSkillSnapshot)
            AppendDirectBasicSkills(details, directSkillSnapshot.BasicSkills);
        else
        {
            for (var skillIndex = 0; skillIndex < BasicSkillCount; skillIndex++)
                AppendSkillDetail(details, dispatcher, handle, s_basicSkillFieldId, skillIndex, isTechSkill: false);
        }

        for (var skillIndex = 0; skillIndex < TechSkillCount; skillIndex++)
            AppendSkillDetail(details, dispatcher, handle, s_techSkillFieldId, skillIndex, isTechSkill: true);

        for (var collegeIndex = 0; collegeIndex < SpellCollegeCount; collegeIndex++)
            AppendSpellCollegeDetail(details, dispatcher, handle, collegeIndex);

        AppendSpellMasteryDetail(details, dispatcher, handle);

        for (var disciplineIndex = 0; disciplineIndex < TechDisciplineCount; disciplineIndex++)
            AppendTechDisciplineDetail(details, dispatcher, handle, disciplineIndex);

        var fatiguePoints = AppendFieldDetail(details, dispatcher, handle, s_fatiguePointsFieldId);
        var fatigueDamage = AppendFieldDetail(details, dispatcher, handle, s_fatigueDamageFieldId);
        if (fatiguePoints is { } maxFatigue && fatigueDamage is { } currentFatigueDamage)
        {
            details.Add(
                new LiveObjectDetail(
                    "fatigue_remaining",
                    "Fatigue Remaining",
                    (maxFatigue - currentFatigueDamage).ToString(CultureInfo.InvariantCulture),
                    "Computed"
                )
            );
        }

        var inventoryCount = AppendFieldDetail(details, dispatcher, handle, s_critterInventoryCountFieldId);
        if (inventoryCount is > 0)
            AppendInventoryHandles(details, dispatcher, handle, s_critterInventoryListFieldId, inventoryCount.Value);
    }

    private void AppendPcDetails(List<LiveObjectDetail> details, RuntimeCallDispatcher dispatcher, ulong handle)
    {
        AppendFieldDetail(details, dispatcher, handle, s_pcBankMoneyFieldId);
        AppendFieldDetail(details, dispatcher, handle, s_pcPartyIdFieldId);
    }

    private void AppendNpcDetails(List<LiveObjectDetail> details, RuntimeCallDispatcher dispatcher, ulong handle)
    {
        AppendFieldDetail(details, dispatcher, handle, s_npcExperienceWorthFieldId);
        AppendFieldDetail(details, dispatcher, handle, s_npcExperiencePoolFieldId);
        AppendFieldDetail(details, dispatcher, handle, s_npcReactionBaseFieldId);
        AppendFieldDetail(details, dispatcher, handle, s_npcFactionFieldId);
    }

    private void AppendContainerDetails(List<LiveObjectDetail> details, RuntimeCallDispatcher dispatcher, ulong handle)
    {
        var inventoryCount = AppendFieldDetail(details, dispatcher, handle, s_containerInventoryCountFieldId);
        if (inventoryCount is > 0)
            AppendInventoryHandles(details, dispatcher, handle, s_containerInventoryListFieldId, inventoryCount.Value);
        AppendFieldDetail(details, dispatcher, handle, s_containerLockDifficultyFieldId);
        AppendFieldDetail(details, dispatcher, handle, s_containerKeyIdFieldId);
    }

    private void AppendPortalDetails(List<LiveObjectDetail> details, RuntimeCallDispatcher dispatcher, ulong handle)
    {
        AppendFieldDetail(details, dispatcher, handle, s_portalLockDifficultyFieldId);
        AppendFieldDetail(details, dispatcher, handle, s_portalKeyIdFieldId);
        AppendFieldDetail(details, dispatcher, handle, s_portalNotifyNpcFieldId);
    }

    private void AppendItemDetails(
        List<LiveObjectDetail> details,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        ObjectType objectType
    )
    {
        AppendFieldDetail(details, dispatcher, handle, s_itemWeightFieldId);
        AppendFieldDetail(details, dispatcher, handle, s_itemWorthFieldId);
        AppendFieldDetail(details, dispatcher, handle, s_itemManaStoreFieldId);
        AppendHandleFieldDetail(details, dispatcher, handle, s_itemParentFieldId);
        AppendFieldDetail(details, dispatcher, handle, s_itemInventoryLocationFieldId);
        AppendFieldDetail(details, dispatcher, handle, s_itemMagicTechComplexityFieldId);

        switch (objectType)
        {
            case ObjectType.Weapon:
                AppendFieldDetail(details, dispatcher, handle, s_weaponBonusToHitFieldId);
                AppendFieldDetail(details, dispatcher, handle, s_weaponSpeedFactorFieldId);
                AppendFieldDetail(details, dispatcher, handle, s_weaponRangeFieldId);
                AppendFieldDetail(details, dispatcher, handle, s_weaponMinStrengthFieldId);
                AppendFieldDetail(details, dispatcher, handle, s_weaponAmmoConsumptionFieldId);
                break;

            case ObjectType.Ammo:
                AppendFieldDetail(details, dispatcher, handle, s_ammoQuantityFieldId);
                AppendFieldDetail(details, dispatcher, handle, s_ammoTypeFieldId);
                break;

            case ObjectType.Armor:
                AppendFieldDetail(details, dispatcher, handle, s_armorAcAdjustmentFieldId);
                AppendFieldDetail(details, dispatcher, handle, s_armorMagicAcAdjustmentFieldId);
                AppendFieldDetail(details, dispatcher, handle, s_armorSilentMoveAdjustmentFieldId);
                AppendFieldDetail(details, dispatcher, handle, s_armorUnarmedDamageFieldId);
                break;

            case ObjectType.Gold:
                AppendFieldDetail(details, dispatcher, handle, s_goldQuantityFieldId);
                break;

            case ObjectType.Key:
                AppendFieldDetail(details, dispatcher, handle, s_keyIdFieldId);
                break;

            case ObjectType.Written:
                AppendFieldDetail(details, dispatcher, handle, s_writtenSubtypeFieldId);
                AppendFieldDetail(details, dispatcher, handle, s_writtenTextStartLineFieldId);
                AppendFieldDetail(details, dispatcher, handle, s_writtenTextEndLineFieldId);
                break;

            case ObjectType.Generic:
                AppendFieldDetail(details, dispatcher, handle, s_genericUsageBonusFieldId);
                AppendFieldDetail(details, dispatcher, handle, s_genericUsageCountRemainingFieldId);
                break;
        }
    }

    private int? AppendFieldDetail(
        List<LiveObjectDetail> details,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int fieldId
    )
    {
        if (!TryReadObjectField(dispatcher, handle, fieldId, out var value))
            return null;

        details.Add(
            new LiveObjectDetail(
                ObjectFieldCatalog.RawName(fieldId).ToLowerInvariant(),
                ObjectFieldCatalog.DisplayName(fieldId),
                RuntimeObjectValueFormatter.FormatFieldInt32(fieldId, value),
                "obj_field_int32_get"
            )
        );
        return value;
    }

    private void AppendStatRange(
        List<LiveObjectDetail> details,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int startStatId,
        int count
    )
    {
        for (var statId = startStatId; statId < startStatId + count; statId++)
            AppendStatDetail(details, dispatcher, handle, statId);
    }

    private void AppendStatDetail(
        List<LiveObjectDetail> details,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int statId
    )
    {
        if (!TryReadStat(dispatcher, handle, statId, out var value))
            return;

        details.Add(
            new LiveObjectDetail(
                $"stat_{statId}",
                RuntimeSemanticCatalog.StatName(statId),
                value.ToString(CultureInfo.InvariantCulture),
                "stat_base_get"
            )
        );
    }

    private void AppendHandleFieldDetail(
        List<LiveObjectDetail> details,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int fieldId
    )
    {
        if (!TryReadObjectHandle(dispatcher, handle, fieldId, out var value))
            return;

        details.Add(
            new LiveObjectDetail(
                ObjectFieldCatalog.RawName(fieldId).ToLowerInvariant(),
                ObjectFieldCatalog.DisplayName(fieldId),
                RuntimeSemanticCatalog.FormatHandle(value),
                "obj_field_handle_get"
            )
        );
    }

    private void AppendInventoryHandles(
        List<LiveObjectDetail> details,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int fieldId,
        int count
    )
    {
        for (var index = 0; index < count; index++)
        {
            if (!TryReadArrayHandle(dispatcher, handle, fieldId, index, out var value))
                continue;

            details.Add(
                new LiveObjectDetail(
                    $"{ObjectFieldCatalog.RawName(fieldId).ToLowerInvariant()}_{index.ToString(CultureInfo.InvariantCulture)}",
                    ObjectFieldCatalog.ArrayElementName(fieldId, index),
                    RuntimeSemanticCatalog.FormatHandle(value),
                    "obj_array_field_handle_get"
                )
            );
        }
    }

    private static void AppendDirectMainStats(
        List<LiveObjectDetail> details,
        IReadOnlyList<DirectCharacterSheetField> fields
    )
    {
        foreach (var field in fields)
        {
            if (field.StatId is not int statId || statId is < 0 or >= PrimaryStatCount)
                continue;

            details.Add(
                new LiveObjectDetail(
                    $"stat_{statId}",
                    field.Label,
                    field.Value.ToString(CultureInfo.InvariantCulture),
                    "character_base_aggregate"
                )
            );
        }
    }

    private void AppendDirectProgression(
        List<LiveObjectDetail> details,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        IReadOnlyList<DirectCharacterSheetField> fields
    )
    {
        foreach (var field in fields)
        {
            if (field.StatId is not int statId || statId < ProgressionStatStartIndex || statId > UnspentPointsStatIndex)
                continue;

            details.Add(
                new LiveObjectDetail(
                    $"stat_{statId}",
                    field.Label,
                    field.Value.ToString(CultureInfo.InvariantCulture),
                    "character_base_aggregate"
                )
            );
        }

        for (
            var statId = UnspentPointsStatIndex + 1;
            statId < ProgressionStatStartIndex + ProgressionStatCount;
            statId++
        )
            AppendStatDetail(details, dispatcher, handle, statId);
    }

    private static void AppendDirectBasicSkills(
        List<LiveObjectDetail> details,
        IReadOnlyList<DirectCharacterSheetField> fields
    )
    {
        foreach (var field in fields)
        {
            if (field.SkillIndex is not int skillIndex)
                continue;

            var level = field.Value & 63;
            var training = (field.Value >> 6) & 3;
            details.Add(
                new LiveObjectDetail(
                    $"basic_skill_{skillIndex}",
                    field.Label,
                    $"{level.ToString(CultureInfo.InvariantCulture)} ({CharacterSheetMetadata.TrainingName(training)})",
                    "character_base_aggregate"
                )
            );
        }
    }

    private void AppendResistanceDetail(
        List<LiveObjectDetail> details,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int resistanceId
    )
    {
        if (!TryReadResistance(dispatcher, handle, resistanceId, out var value))
            return;

        details.Add(
            new LiveObjectDetail(
                $"resistance_{resistanceId}",
                $"Resistance / {CharacterSheetMetadata.ResistanceName(resistanceId)}",
                value.ToString(CultureInfo.InvariantCulture),
                "object_get_resistance"
            )
        );
    }

    private void AppendSkillDetail(
        List<LiveObjectDetail> details,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int fieldId,
        int index,
        bool isTechSkill
    )
    {
        if (!TryReadArrayInt32(dispatcher, handle, fieldId, index, out var encodedValue))
            return;

        var skillName = isTechSkill
            ? CharacterSheetMetadata.TechSkillName(index)
            : CharacterSheetMetadata.BasicSkillName(index);
        var category = isTechSkill ? "Tech Skill" : "Basic Skill";
        var level = encodedValue & 63;
        var training = (encodedValue >> 6) & 3;
        details.Add(
            new LiveObjectDetail(
                $"{(isTechSkill ? "tech" : "basic")}_skill_{index}",
                $"{category} / {skillName}",
                $"{level.ToString(CultureInfo.InvariantCulture)} ({CharacterSheetMetadata.TrainingName(training)})",
                "obj_array_field_int32_get"
            )
        );
    }

    private void AppendSpellCollegeDetail(
        List<LiveObjectDetail> details,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int collegeIndex
    )
    {
        if (!TryReadArrayInt32(dispatcher, handle, s_spellTechFieldId, collegeIndex, out var value))
            return;

        details.Add(
            new LiveObjectDetail(
                $"spell_college_{collegeIndex}",
                $"Spell College / {CharacterSheetMetadata.SpellCollegeName(collegeIndex)}",
                value.ToString(CultureInfo.InvariantCulture),
                "obj_array_field_int32_get"
            )
        );
    }

    private void AppendSpellMasteryDetail(
        List<LiveObjectDetail> details,
        RuntimeCallDispatcher dispatcher,
        ulong handle
    )
    {
        if (!TryReadArrayInt32(dispatcher, handle, s_spellTechFieldId, SpellCollegeCount, out var masteryValue))
            return;

        var masteryLabel = masteryValue is >= 0 and < SpellCollegeCount
            ? CharacterSheetMetadata.SpellCollegeName(masteryValue)
            : "None";
        details.Add(new LiveObjectDetail("spell_mastery", "Spell Mastery", masteryLabel, "obj_array_field_int32_get"));
    }

    private void AppendTechDisciplineDetail(
        List<LiveObjectDetail> details,
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int disciplineIndex
    )
    {
        var fieldIndex = SpellCollegeCount + 1 + disciplineIndex;
        if (!TryReadArrayInt32(dispatcher, handle, s_spellTechFieldId, fieldIndex, out var value))
            return;

        details.Add(
            new LiveObjectDetail(
                $"tech_discipline_{disciplineIndex}",
                ObjectFieldCatalog.ArrayElementName(s_spellTechFieldId, fieldIndex),
                value.ToString(CultureInfo.InvariantCulture),
                "obj_array_field_int32_get"
            )
        );
    }

    private bool TryReadObjectField(RuntimeCallDispatcher dispatcher, ulong handle, int fieldId, out int value)
    {
        try
        {
            value = dispatcher.InvokeInt32(
                s_objectFieldGetter,
                [ToLow32(handle), ToHigh32(handle), unchecked((uint)fieldId)],
                s_readTimeout
            );
            return true;
        }
        catch
        {
            value = 0;
            return false;
        }
    }

    private bool TryReadStat(RuntimeCallDispatcher dispatcher, ulong handle, int statId, out int value)
    {
        try
        {
            value = dispatcher.InvokeInt32(
                s_statBaseGetter,
                [ToLow32(handle), ToHigh32(handle), unchecked((uint)statId)],
                s_readTimeout
            );
            return true;
        }
        catch
        {
            value = 0;
            return false;
        }
    }

    private bool TryReadResistance(RuntimeCallDispatcher dispatcher, ulong handle, int resistanceId, out int value)
    {
        try
        {
            value = dispatcher.InvokeInt32(
                s_objectResistanceGetter,
                [ToLow32(handle), ToHigh32(handle), unchecked((uint)resistanceId), 1u],
                s_readTimeout
            );
            return true;
        }
        catch
        {
            value = 0;
            return false;
        }
    }

    private bool TryReadArrayInt32(
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int fieldId,
        int index,
        out int value
    )
    {
        try
        {
            value = dispatcher.InvokeInt32(
                s_objectArrayFieldGetter,
                [ToLow32(handle), ToHigh32(handle), unchecked((uint)fieldId), unchecked((uint)index)],
                s_readTimeout
            );
            return true;
        }
        catch
        {
            value = 0;
            return false;
        }
    }

    private bool TryReadObjectHandle(RuntimeCallDispatcher dispatcher, ulong handle, int fieldId, out ulong value)
    {
        try
        {
            var result = dispatcher.Invoke(
                _memory.ToUInt32Address(_memory.ResolveRva(s_objectHandleFieldGetter.Rva)),
                s_objectHandleFieldGetter.SuggestedCleanup,
                0,
                0,
                [ToLow32(handle), ToHigh32(handle), unchecked((uint)fieldId)],
                s_readTimeout
            );
            value = result.ResultEax | ((ulong)result.ResultEdx << 32);
            return true;
        }
        catch
        {
            value = 0;
            return false;
        }
    }

    private bool TryReadArrayHandle(
        RuntimeCallDispatcher dispatcher,
        ulong handle,
        int fieldId,
        int index,
        out ulong value
    )
    {
        try
        {
            var result = dispatcher.Invoke(
                _memory.ToUInt32Address(_memory.ResolveRva(s_objectArrayHandleFieldGetter.Rva)),
                s_objectArrayHandleFieldGetter.SuggestedCleanup,
                0,
                0,
                [ToLow32(handle), ToHigh32(handle), unchecked((uint)fieldId), unchecked((uint)index)],
                s_readTimeout
            );
            value = result.ResultEax | ((ulong)result.ResultEdx << 32);
            return true;
        }
        catch
        {
            value = 0;
            return false;
        }
    }

    private bool TryGetDispatcher(out RuntimeCallDispatcher dispatcher)
    {
        if (_dispatcher is not null)
        {
            dispatcher = _dispatcher;
            return true;
        }

        if (_dispatcherUnavailable || !_runtimeProfile.SupportsCatalogRvas)
        {
            dispatcher = null!;
            return false;
        }

        try
        {
            _dispatcher = RuntimeCallDispatcher.Install(_memory, _runtimeProfile);
            dispatcher = _dispatcher;
            return true;
        }
        catch
        {
            _dispatcherUnavailable = true;
            dispatcher = null!;
            return false;
        }
    }

    private DirectCharacterSheetSnapshot? TryReadDirectCharacterSheet(LiveObjectIdentity identity)
    {
        if (!TryParseFormattedAddress(identity.ObjectAddress, out var characterAddress))
            return null;

        try
        {
            var aggregateRoot = _memory.ReadPointer32(characterAddress + CharacterAggregateOffset);
            if (aggregateRoot == 0)
                return null;

            var mainStatsAddress = _memory.ReadPointer32(aggregateRoot + AggregateMainStatsOffset);
            var basicSkillsAddress = _memory.ReadPointer32(aggregateRoot + AggregateBasicSkillsOffset);
            if (mainStatsAddress == 0 && basicSkillsAddress == 0)
                return null;

            return new DirectCharacterSheetSnapshot(
                ReadDirectFields(
                    mainStatsAddress,
                    CharacterSheetRuntimeLayout.MainStatsFields,
                    isBasicSkillGroup: false
                ),
                ReadDirectFields(
                    basicSkillsAddress,
                    CharacterSheetRuntimeLayout.BasicSkillsFields,
                    isBasicSkillGroup: true
                )
            );
        }
        catch
        {
            return null;
        }
    }

    private IReadOnlyList<DirectCharacterSheetField> ReadDirectFields(
        nint baseAddress,
        IReadOnlyList<RuntimeFieldDescriptor> descriptors,
        bool isBasicSkillGroup
    )
    {
        if (baseAddress == 0)
            return [];

        List<DirectCharacterSheetField> fields = [];
        foreach (var descriptor in descriptors)
        {
            var address = baseAddress + descriptor.Offset;
            var value = _memory.ReadInt32(address);
            fields.Add(
                new DirectCharacterSheetField(
                    descriptor.Name,
                    CreateDirectFieldLabel(descriptor.Name, isBasicSkillGroup),
                    value,
                    ResolveDirectStatId(descriptor.Name),
                    isBasicSkillGroup ? ResolveBasicSkillIndex(descriptor.Name) : null
                )
            );
        }

        return fields;
    }

    private static string CreateDirectFieldLabel(string name, bool isBasicSkillGroup) =>
        isBasicSkillGroup ? $"Basic Skill / {HumanizeName(name)}" : HumanizeName(name);

    private static string HumanizeName(string value)
    {
        if (value.Equals("FatePoints", StringComparison.Ordinal))
            return "Fate Points";

        if (value.Equals("SkillPoints", StringComparison.Ordinal))
            return "Unspent Points";

        if (value.Equals("Experience", StringComparison.Ordinal))
            return "Experience Points";

        return string.Concat(
            value.Select(
                (ch, index) =>
                    index > 0 && char.IsUpper(ch) && char.IsLower(value[index - 1]) ? $" {ch}" : ch.ToString()
            )
        );
    }

    private static int? ResolveDirectStatId(string name) =>
        name switch
        {
            "Strength" => 0,
            "Dexterity" => 1,
            "Constitution" => 2,
            "Beauty" => 3,
            "Intelligence" => 4,
            "Perception" => 5,
            "Willpower" => 6,
            "Charisma" => 7,
            "Level" => 17,
            "Experience" => 18,
            "Alignment" => 19,
            "FatePoints" => 20,
            "SkillPoints" => 21,
            _ => null,
        };

    private static int? ResolveBasicSkillIndex(string name)
    {
        for (var index = 0; index < BasicSkillCount; index++)
        {
            if (
                string.Equals(
                    CharacterSheetMetadata.BasicSkillName(index).Replace(" ", string.Empty),
                    name,
                    StringComparison.OrdinalIgnoreCase
                )
            )
                return index;
        }

        return null;
    }

    private static bool TryParseFormattedAddress(string? formattedAddress, out nint address)
    {
        if (string.IsNullOrWhiteSpace(formattedAddress))
        {
            address = 0;
            return false;
        }

        var trimmed = formattedAddress.Trim();
        ulong rawValue;
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (!ulong.TryParse(trimmed[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rawValue))
            {
                address = 0;
                return false;
            }
        }
        else if (!ulong.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out rawValue))
        {
            address = 0;
            return false;
        }

        address = (nint)(long)rawValue;
        return true;
    }

    private static bool IsItemObjectType(ObjectType objectType) =>
        objectType is >= ObjectType.Weapon and <= ObjectType.Generic;

    private static uint ToLow32(ulong value) => unchecked((uint)(value & uint.MaxValue));

    private static uint ToHigh32(ulong value) => unchecked((uint)(value >> 32));

    private static int ResolveFieldId(string rawName)
    {
        if (ObjectFieldCatalog.TryGetFieldId(rawName, out var fieldId))
            return fieldId;

        throw new InvalidOperationException($"Unable to resolve runtime object field '{rawName}'.");
    }

    private static readonly TimeSpan s_readTimeout = TimeSpan.FromSeconds(1);
    private static readonly FunctionDefinition s_objectFieldGetter = FunctionCatalog.GetDefinition(
        "obj_field_int32_get"
    );
    private static readonly FunctionDefinition s_objectHandleFieldGetter = FunctionCatalog.GetDefinition(
        "obj_field_handle_get"
    );
    private static readonly FunctionDefinition s_objectArrayFieldGetter = FunctionCatalog.GetDefinition(
        "obj_array_field_int32_get"
    );
    private static readonly FunctionDefinition s_objectArrayHandleFieldGetter = FunctionCatalog.GetDefinition(
        "obj_array_field_handle_get"
    );
    private static readonly FunctionDefinition s_objectResistanceGetter = FunctionCatalog.GetDefinition(
        "object_get_resistance"
    );
    private static readonly FunctionDefinition s_statBaseGetter = FunctionCatalog.GetDefinition("stat_base_get");
    private static readonly int s_currentAidFieldId = ResolveFieldId("OBJ_F_CURRENT_AID");
    private static readonly int s_armorClassFieldId = ResolveFieldId("OBJ_F_AC");
    private static readonly int s_hitPointsFieldId = ResolveFieldId("OBJ_F_HP_PTS");
    private static readonly int s_hitPointDamageFieldId = ResolveFieldId("OBJ_F_HP_DAMAGE");
    private static readonly int s_fatiguePointsFieldId = ResolveFieldId("OBJ_F_CRITTER_FATIGUE_PTS");
    private static readonly int s_fatigueDamageFieldId = ResolveFieldId("OBJ_F_CRITTER_FATIGUE_DAMAGE");
    private static readonly int s_basicSkillFieldId = ResolveFieldId("OBJ_F_CRITTER_BASIC_SKILL_IDX");
    private static readonly int s_techSkillFieldId = ResolveFieldId("OBJ_F_CRITTER_TECH_SKILL_IDX");
    private static readonly int s_spellTechFieldId = ResolveFieldId("OBJ_F_CRITTER_SPELL_TECH_IDX");
    private static readonly int s_critterInventoryCountFieldId = ResolveFieldId("OBJ_F_CRITTER_INVENTORY_NUM");
    private static readonly int s_critterInventoryListFieldId = ResolveFieldId("OBJ_F_CRITTER_INVENTORY_LIST_IDX");
    private static readonly int s_pcBankMoneyFieldId = ResolveFieldId("OBJ_F_PC_BANK_MONEY");
    private static readonly int s_pcPartyIdFieldId = ResolveFieldId("OBJ_F_PC_PARTY_ID");
    private static readonly int s_npcExperienceWorthFieldId = ResolveFieldId("OBJ_F_NPC_EXPERIENCE_WORTH");
    private static readonly int s_npcExperiencePoolFieldId = ResolveFieldId("OBJ_F_NPC_EXPERIENCE_POOL");
    private static readonly int s_npcReactionBaseFieldId = ResolveFieldId("OBJ_F_NPC_REACTION_BASE");
    private static readonly int s_npcFactionFieldId = ResolveFieldId("OBJ_F_NPC_FACTION");
    private static readonly int s_containerInventoryCountFieldId = ResolveFieldId("OBJ_F_CONTAINER_INVENTORY_NUM");
    private static readonly int s_containerInventoryListFieldId = ResolveFieldId("OBJ_F_CONTAINER_INVENTORY_LIST_IDX");
    private static readonly int s_containerLockDifficultyFieldId = ResolveFieldId("OBJ_F_CONTAINER_LOCK_DIFFICULTY");
    private static readonly int s_containerKeyIdFieldId = ResolveFieldId("OBJ_F_CONTAINER_KEY_ID");
    private static readonly int s_portalLockDifficultyFieldId = ResolveFieldId("OBJ_F_PORTAL_LOCK_DIFFICULTY");
    private static readonly int s_portalKeyIdFieldId = ResolveFieldId("OBJ_F_PORTAL_KEY_ID");
    private static readonly int s_portalNotifyNpcFieldId = ResolveFieldId("OBJ_F_PORTAL_NOTIFY_NPC");
    private static readonly int s_projectileHitLocFieldId = ResolveFieldId("OBJ_F_PROJECTILE_HIT_LOC");
    private static readonly int s_trapDifficultyFieldId = ResolveFieldId("OBJ_F_TRAP_DIFFICULTY");
    private static readonly int s_itemWeightFieldId = ResolveFieldId("OBJ_F_ITEM_WEIGHT");
    private static readonly int s_itemWorthFieldId = ResolveFieldId("OBJ_F_ITEM_WORTH");
    private static readonly int s_itemManaStoreFieldId = ResolveFieldId("OBJ_F_ITEM_MANA_STORE");
    private static readonly int s_itemParentFieldId = ResolveFieldId("OBJ_F_ITEM_PARENT");
    private static readonly int s_itemInventoryLocationFieldId = ResolveFieldId("OBJ_F_ITEM_INV_LOCATION");
    private static readonly int s_itemMagicTechComplexityFieldId = ResolveFieldId("OBJ_F_ITEM_MAGIC_TECH_COMPLEXITY");
    private static readonly int s_weaponBonusToHitFieldId = ResolveFieldId("OBJ_F_WEAPON_BONUS_TO_HIT");
    private static readonly int s_weaponSpeedFactorFieldId = ResolveFieldId("OBJ_F_WEAPON_SPEED_FACTOR");
    private static readonly int s_weaponRangeFieldId = ResolveFieldId("OBJ_F_WEAPON_RANGE");
    private static readonly int s_weaponMinStrengthFieldId = ResolveFieldId("OBJ_F_WEAPON_MIN_STRENGTH");
    private static readonly int s_weaponAmmoConsumptionFieldId = ResolveFieldId("OBJ_F_WEAPON_AMMO_CONSUMPTION");
    private static readonly int s_ammoQuantityFieldId = ResolveFieldId("OBJ_F_AMMO_QUANTITY");
    private static readonly int s_ammoTypeFieldId = ResolveFieldId("OBJ_F_AMMO_TYPE");
    private static readonly int s_armorAcAdjustmentFieldId = ResolveFieldId("OBJ_F_ARMOR_AC_ADJ");
    private static readonly int s_armorMagicAcAdjustmentFieldId = ResolveFieldId("OBJ_F_ARMOR_MAGIC_AC_ADJ");
    private static readonly int s_armorSilentMoveAdjustmentFieldId = ResolveFieldId("OBJ_F_ARMOR_SILENT_MOVE_ADJ");
    private static readonly int s_armorUnarmedDamageFieldId = ResolveFieldId("OBJ_F_ARMOR_UNARMED_BONUS_DAMAGE");
    private static readonly int s_goldQuantityFieldId = ResolveFieldId("OBJ_F_GOLD_QUANTITY");
    private static readonly int s_keyIdFieldId = ResolveFieldId("OBJ_F_KEY_KEY_ID");
    private static readonly int s_writtenSubtypeFieldId = ResolveFieldId("OBJ_F_WRITTEN_SUBTYPE");
    private static readonly int s_writtenTextStartLineFieldId = ResolveFieldId("OBJ_F_WRITTEN_TEXT_START_LINE");
    private static readonly int s_writtenTextEndLineFieldId = ResolveFieldId("OBJ_F_WRITTEN_TEXT_END_LINE");
    private static readonly int s_genericUsageBonusFieldId = ResolveFieldId("OBJ_F_GENERIC_USAGE_BONUS");
    private static readonly int s_genericUsageCountRemainingFieldId = ResolveFieldId(
        "OBJ_F_GENERIC_USAGE_COUNT_REMAINING"
    );
    private const int CharacterAggregateOffset = 0x50;
    private const int AggregateMainStatsOffset = 0x2C;
    private const int AggregateBasicSkillsOffset = 0x30;
    private const bool EnableExperimentalDirectCharacterAggregates = false;
    private const int PrimaryStatCount = 8;
    private const int DerivedStatStartIndex = 8;
    private const int DerivedStatCount = 9;
    private const int ProgressionStatStartIndex = 17;
    private const int ProgressionStatCount = 7;
    private const int UnspentPointsStatIndex = 21;
    private const int ResistanceCount = 5;
    private const int BasicSkillCount = 12;
    private const int TechSkillCount = 4;
    private const int SpellCollegeCount = 16;
    private const int TechDisciplineCount = 8;

    private readonly record struct DirectCharacterSheetSnapshot(
        IReadOnlyList<DirectCharacterSheetField> MainStats,
        IReadOnlyList<DirectCharacterSheetField> BasicSkills
    );

    private readonly record struct DirectCharacterSheetField(
        string Name,
        string Label,
        int Value,
        int? StatId,
        int? SkillIndex
    );
}
