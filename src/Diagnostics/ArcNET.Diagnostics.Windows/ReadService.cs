using System.Globalization;
using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

public sealed class ReadService(IReadBackend backend)
{
    [SupportedOSPlatform("windows")]
    public static ReadService Default { get; } = new(new ReadBackend());

    public ReadSnapshot Read(ReadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (
            !request.Session.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions)
            || !request.Session.RuntimeProfile.SupportsCatalogRvas
        )
        {
            return CreateUnavailableSnapshot(
                request,
                "Read unavailable",
                "Getter-backed debugger reads require a validated runtime profile with live function invocation support."
            );
        }

        try
        {
            return Normalize(request.AdapterKey) switch
            {
                "storystate" => ReadStoryState(request),
                "quest" => ReadQuest(request),
                "reputation" => ReadReputation(request),
                "schematic" => ReadSchematic(request),
                "blessing" => ReadBlessing(request),
                "curse" => ReadCurse(request),
                "spellcollege" => ReadSpellCollege(request),
                "skill" => ReadSkill(request),
                "stat" => ReadStat(request),
                "derivedstat" => ReadDerivedStat(request),
                "resistance" => ReadResistance(request),
                "techdiscipline" => ReadTechDiscipline(request),
                "field" => ReadField(request),
                "objfield" => ReadObjectField(request),
                "effectcount" => ReadEffectCount(request),
                "scriptglobalvar" => ReadScriptGlobalVar(request),
                "scriptglobalflag" => ReadScriptGlobalFlag(request),
                "scriptpcvar" => ReadScriptPcVar(request),
                "scriptpcflag" => ReadScriptPcFlag(request),
                "scriptlocalflag" => ReadScriptLocalFlag(request),
                "scriptlocalcounter" => ReadScriptLocalCounter(request),
                "areaknown" => ReadAreaKnown(request),
                "lastknownarea" => ReadLastKnownArea(request),
                "rumorknown" => ReadRumorKnown(request),
                "rumorquelled" => ReadRumorQuelled(request),
                "questglobal" => CreateUnavailableSnapshot(
                    request,
                    "Quest-global read disabled",
                    "Quest-global getter extraction remains disabled until the native getter is revalidated against live runtimes."
                ),
                "sheet" or "sheetscan" or "sheetdiff" => CreateUnavailableSnapshot(
                    request,
                    "Read adapter moved",
                    "Sheet diagnostics now live in SheetService so sheet, sheet-scan, and sheet-diff snapshots can stay strongly typed."
                ),
                "scriptattachment" => CreateUnavailableSnapshot(
                    request,
                    "Read adapter moved",
                    "Script attachment diagnostics now live in ScriptAttachmentService so attachment snapshots can stay strongly typed."
                ),
                "logbook" => CreateUnavailableSnapshot(
                    request,
                    "Read adapter moved",
                    "Logbook diagnostics now live in LogbookService so journal pages can stay strongly typed and UI-agnostic."
                ),
                _ => CreateUnavailableSnapshot(
                    request,
                    "Unknown read adapter",
                    $"Read adapter '{request.AdapterKey}' is not registered in ArcNET.Diagnostics."
                ),
            };
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableSnapshot(request, "Invalid read request", ex.Message);
        }
        catch (Exception ex)
        {
            return CreateUnavailableSnapshot(request, "Read failed", ex.Message);
        }
    }

    private ReadSnapshot ReadStoryState(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 0, 0, "story-state");
        var nativeRead = InvokeInt32(request, "script_story_state_get", []);
        return CreateSuccessSnapshot(
            request,
            summary: "Read the current global story-state marker through the native script-story getter.",
            target: null,
            nativeRead,
            [Value("story_state", "Story State", nativeRead.Int32Value)]
        );
    }

    private ReadSnapshot ReadQuest(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 2, 2, "quest <pc-handle|player|auto> <quest-id>");
        var target = ResolveTarget(request, request.Arguments[0], "quest target");
        var questId = ParseInt32(request.Arguments[1]);
        var nativeRead = InvokeInt32(
            request,
            "quest_state_get",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)questId)]
        );

        return CreateSuccessSnapshot(
            request,
            $"Read quest {questId} for {target.TargetText}.",
            target,
            nativeRead,
            [
                Value("quest_id", "Quest Id", questId),
                Value("quest_state", "Quest State", RuntimeWatchValueCatalog.QuestStateName(nativeRead.Int32Value)),
                Value("raw_state", "Raw State", nativeRead.Int32Value),
            ]
        );
    }

    private ReadSnapshot ReadReputation(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 2, 2, "reputation <object-handle|player|auto> <slot-index>");
        var fieldId = ResolveFieldIdOrThrow("OBJ_F_PC_REPUTATION_IDX");
        var target = ResolveTarget(request, request.Arguments[0], "reputation target");
        var slotIndex = ParseInt32(request.Arguments[1]);
        var nativeRead = InvokeInt32(
            request,
            "obj_array_field_int32_get",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)fieldId), unchecked((uint)slotIndex)]
        );

        return CreateSuccessSnapshot(
            request,
            $"Read reputation slot {slotIndex} for {target.TargetText}.",
            target,
            nativeRead,
            CreateArrayFieldValues(fieldId, slotIndex, nativeRead.Int32Value, unsigned: false)
        );
    }

    private ReadSnapshot ReadSchematic(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 2, 2, "schematic <object-handle|player|auto> <slot-index>");
        var fieldId = ResolveFieldIdOrThrow("OBJ_F_PC_SCHEMATICS_FOUND_IDX");
        var target = ResolveTarget(request, request.Arguments[0], "schematic target");
        var slotIndex = ParseInt32(request.Arguments[1]);
        var nativeRead = InvokeInt32(
            request,
            "obj_array_field_int32_get",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)fieldId), unchecked((uint)slotIndex)]
        );
        var schematicId = unchecked((uint)nativeRead.Int32Value);

        return CreateSuccessSnapshot(
            request,
            $"Read schematic slot {slotIndex} for {target.TargetText}.",
            target,
            nativeRead,
            [
                .. CreateArrayFieldValues(fieldId, slotIndex, nativeRead.Int32Value, unsigned: true),
                Value("schematic_id", "Schematic Id", schematicId),
            ]
        );
    }

    private ReadSnapshot ReadBlessing(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 2, 2, "blessing <object-handle|player|auto> <slot-index>");
        var fieldId = ResolveFieldIdOrThrow("OBJ_F_PC_BLESSING_IDX");
        var target = ResolveTarget(request, request.Arguments[0], "blessing target");
        var slotIndex = ParseInt32(request.Arguments[1]);
        var nativeRead = InvokeInt32(
            request,
            "obj_array_field_int32_get",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)fieldId), unchecked((uint)slotIndex)]
        );
        var blessingId = unchecked((uint)nativeRead.Int32Value);

        return CreateSuccessSnapshot(
            request,
            $"Read blessing slot {slotIndex} for {target.TargetText}.",
            target,
            nativeRead,
            [
                .. CreateArrayFieldValues(fieldId, slotIndex, nativeRead.Int32Value, unsigned: true),
                Value("blessing_id", "Blessing Id", blessingId),
            ]
        );
    }

    private ReadSnapshot ReadCurse(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 2, 2, "curse <object-handle|player|auto> <slot-index>");
        var fieldId = ResolveFieldIdOrThrow("OBJ_F_PC_CURSE_IDX");
        var target = ResolveTarget(request, request.Arguments[0], "curse target");
        var slotIndex = ParseInt32(request.Arguments[1]);
        var nativeRead = InvokeInt32(
            request,
            "obj_array_field_int32_get",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)fieldId), unchecked((uint)slotIndex)]
        );
        var curseId = unchecked((uint)nativeRead.Int32Value);

        return CreateSuccessSnapshot(
            request,
            $"Read curse slot {slotIndex} for {target.TargetText}.",
            target,
            nativeRead,
            [
                .. CreateArrayFieldValues(fieldId, slotIndex, nativeRead.Int32Value, unsigned: true),
                Value("curse_id", "Curse Id", curseId),
            ]
        );
    }

    private ReadSnapshot ReadSpellCollege(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 2, 2, "spell-college <object-handle|player|auto> <college-id|name>");
        var target = ResolveTarget(request, request.Arguments[0], "spell-college target");
        var collegeId = ParseSpellCollegeId(request.Arguments[1]);
        var nativeRead = InvokeInt32(
            request,
            "spell_college_level_get",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)collegeId)]
        );

        return CreateSuccessSnapshot(
            request,
            $"Read spell-college level for {ObjectFieldCatalog.SpellCollegeName(collegeId)} on {target.TargetText}.",
            target,
            nativeRead,
            [
                Value("college_id", "College Id", collegeId),
                Value("college_name", "College", ObjectFieldCatalog.SpellCollegeName(collegeId)),
                Value("level", "Level", nativeRead.Int32Value),
            ]
        );
    }

    private ReadSnapshot ReadSkill(ReadRequest request)
    {
        RequireArgumentCount(
            request.Arguments,
            2,
            2,
            "skill <object-handle|player|auto> <skill-name|basic:index|tech:index>"
        );
        var target = ResolveTarget(request, request.Arguments[0], "skill target");
        var skill = ParseSkillReference(request.Arguments[1]);
        var fieldId = ResolveFieldIdOrThrow(skill.FieldRawName);
        var nativeRead = InvokeInt32(
            request,
            "obj_array_field_int32_get",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)fieldId), unchecked((uint)skill.Index)]
        );
        var level = nativeRead.Int32Value & 63;
        var training = (nativeRead.Int32Value >> 6) & 3;

        return CreateSuccessSnapshot(
            request,
            $"Read skill slot {skill.Index} for {target.TargetText}.",
            target,
            nativeRead,
            [
                .. CreateArrayFieldValues(fieldId, skill.Index, nativeRead.Int32Value, unsigned: false),
                Value("level", "Level", level),
                Value("training", "Training", training),
                Value("training_name", "Training Name", ObjectValueFormatter.FormatSkillTraining(training)),
            ]
        );
    }

    private ReadSnapshot ReadStat(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 2, 2, "stat <object-handle|player|auto> <stat-id|name>");
        var target = ResolveTarget(request, request.Arguments[0], "stat target");
        var statId = ParseStatId(request.Arguments[1]);
        var nativeRead = InvokeInt32(
            request,
            "stat_base_get",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)statId)]
        );

        return CreateSuccessSnapshot(
            request,
            $"Read stat {RuntimeSemanticCatalog.StatName(statId)} for {target.TargetText}.",
            target,
            nativeRead,
            [
                Value("stat_id", "Stat Id", statId),
                Value("stat_name", "Stat", RuntimeSemanticCatalog.StatName(statId)),
                Value("value", "Value", nativeRead.Int32Value),
            ]
        );
    }

    private ReadSnapshot ReadDerivedStat(ReadRequest request)
    {
        RequireArgumentCount(
            request.Arguments,
            2,
            2,
            "derived-stat <object-handle|player|auto> <derived-stat-id|name>"
        );
        var target = ResolveTarget(request, request.Arguments[0], "derived-stat target");
        var derivedStatId = ParseDerivedStatId(request.Arguments[1]);
        var nativeRead = InvokeInt32(
            request,
            "stat_base_get",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)derivedStatId)]
        );

        return CreateSuccessSnapshot(
            request,
            $"Read derived stat {DerivedStatLabels[derivedStatId - DerivedStatBaseIndex]} for {target.TargetText}.",
            target,
            nativeRead,
            [
                Value("derived_stat_id", "Derived Stat Id", derivedStatId),
                Value("derived_stat_name", "Derived Stat", DerivedStatLabels[derivedStatId - DerivedStatBaseIndex]),
                Value("value", "Value", nativeRead.Int32Value),
            ]
        );
    }

    private ReadSnapshot ReadResistance(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 2, 2, "resistance <object-handle|player|auto> <resistance-id|name>");
        var target = ResolveTarget(request, request.Arguments[0], "resistance target");
        var resistanceId = ParseResistanceId(request.Arguments[1]);
        var nativeRead = InvokeInt32(
            request,
            "object_get_resistance",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)resistanceId), 1u]
        );

        return CreateSuccessSnapshot(
            request,
            $"Read {ObjectFieldCatalog.ResistanceName(resistanceId)} resistance for {target.TargetText}.",
            target,
            nativeRead,
            [
                Value("resistance_id", "Resistance Id", resistanceId),
                Value("resistance_name", "Resistance", ObjectFieldCatalog.ResistanceName(resistanceId)),
                Value("identified_aware", "Identified Aware", "Yes"),
                Value("value", "Value", nativeRead.Int32Value),
            ]
        );
    }

    private ReadSnapshot ReadTechDiscipline(ReadRequest request)
    {
        RequireArgumentCount(
            request.Arguments,
            2,
            2,
            "tech-discipline <object-handle|player|auto> <discipline-id|name>"
        );
        var fieldId = ResolveFieldIdOrThrow("OBJ_F_CRITTER_SPELL_TECH_IDX");
        var target = ResolveTarget(request, request.Arguments[0], "tech-discipline target");
        var disciplineId = ParseTechDisciplineId(request.Arguments[1]);
        var slotIndex = SpellCollegeCount + 1 + disciplineId;
        var nativeRead = InvokeInt32(
            request,
            "obj_array_field_int32_get",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)fieldId), unchecked((uint)slotIndex)]
        );

        return CreateSuccessSnapshot(
            request,
            $"Read tech discipline {TechDisciplineLabels[disciplineId]} for {target.TargetText}.",
            target,
            nativeRead,
            [
                .. CreateArrayFieldValues(fieldId, slotIndex, nativeRead.Int32Value, unsigned: false),
                Value("discipline_id", "Discipline Id", disciplineId),
                Value("discipline_name", "Discipline", TechDisciplineLabels[disciplineId]),
                Value("degree", "Degree", nativeRead.Int32Value),
            ]
        );
    }

    private ReadSnapshot ReadField(ReadRequest request)
    {
        RequireArgumentCount(
            request.Arguments,
            3,
            4,
            "field <object-handle|player|auto> <field-id|raw-name> <index> [signed|unsigned]"
        );
        var target = ResolveTarget(request, request.Arguments[0], "field target");
        var field = ResolveFieldReference(
            request.Arguments[1],
            request.Arguments[2],
            request.Arguments.Count == 4 ? request.Arguments[3] : null
        );
        var nativeRead = InvokeInt32(
            request,
            "obj_array_field_int32_get",
            [
                ToLow32(target.Handle),
                ToHigh32(target.Handle),
                unchecked((uint)field.FieldId),
                unchecked((uint)field.Index),
            ]
        );

        return CreateSuccessSnapshot(
            request,
            $"Read array field {field.FieldRawName}[{field.Index}] for {target.TargetText}.",
            target,
            nativeRead,
            [
                .. CreateArrayFieldValues(field.FieldId, field.Index, nativeRead.Int32Value, field.Unsigned),
                Value("storage", "Storage", field.Unsigned ? "array-uint32" : "array-int32"),
            ]
        );
    }

    private ReadSnapshot ReadObjectField(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 2, 2, "obj-field <object-handle|player|auto> <field-id|raw-name>");
        var target = ResolveTarget(request, request.Arguments[0], "obj-field target");
        var fieldId = TryParseFieldId(request.Arguments[1], out var parsedFieldId)
            ? parsedFieldId
            : ResolveFieldIdOrThrow(request.Arguments[1]);
        var nativeRead = InvokeInt32(
            request,
            "obj_field_int32_get",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)fieldId)]
        );

        return CreateSuccessSnapshot(
            request,
            $"Read object field {ObjectFieldCatalog.RawName(fieldId)} for {target.TargetText}.",
            target,
            nativeRead,
            [
                Value("field_id", "Field Id", fieldId),
                Value("field_raw_name", "Field", ObjectFieldCatalog.RawName(fieldId)),
                Value("field_name", "Field Name", ObjectFieldCatalog.DisplayName(fieldId)),
                Value("value", "Value", nativeRead.Int32Value),
                Value(
                    "value_text",
                    "Value Text",
                    ObjectValueFormatter.FormatFieldInt32(fieldId, nativeRead.Int32Value)
                ),
            ]
        );
    }

    private ReadSnapshot ReadEffectCount(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 2, 2, "effect-count <object-handle|player|auto> <effect-id>");
        var target = ResolveTarget(request, request.Arguments[0], "effect-count target");
        var effectId = ParseInt32(request.Arguments[1]);
        var nativeRead = InvokeInt32(
            request,
            "effect_count_effects_of_type",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)effectId)]
        );

        return CreateSuccessSnapshot(
            request,
            $"Counted effect {effectId} on {target.TargetText}.",
            target,
            nativeRead,
            [
                Value("effect_id", "Effect Id", effectId),
                Value("effect_name", "Effect", RuntimeWatchValueCatalog.FallbackEffectName(effectId)),
                Value("count", "Count", nativeRead.Int32Value),
            ]
        );
    }

    private ReadSnapshot ReadScriptGlobalVar(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 1, 1, "script-global-var <index>");
        var index = ParseInt32(request.Arguments[0]);
        var nativeRead = InvokeInt32(request, "script_global_var_get", [unchecked((uint)index)]);

        return CreateSuccessSnapshot(
            request,
            $"Read script global variable {index}.",
            target: null,
            nativeRead,
            [
                Value("index", "Index", index),
                Value("value", "Value", nativeRead.Int32Value),
                Value("unsigned_value", "Unsigned Value", unchecked((uint)nativeRead.Int32Value)),
            ]
        );
    }

    private ReadSnapshot ReadScriptGlobalFlag(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 1, 1, "script-global-flag <index>");
        var index = ParseInt32(request.Arguments[0]);
        var nativeRead = InvokeInt32(request, "script_global_flag_get", [unchecked((uint)index)]);

        return CreateSuccessSnapshot(
            request,
            $"Read script global flag {index}.",
            target: null,
            nativeRead,
            [
                Value("index", "Index", index),
                Value("enabled", "Enabled", FormatBoolean(nativeRead.Int32Value != 0)),
                Value("value", "Raw Value", nativeRead.Int32Value),
            ]
        );
    }

    private ReadSnapshot ReadScriptPcVar(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 2, 2, "script-pc-var <pc-handle|player|auto> <index>");
        var fieldId = ResolveFieldIdOrThrow("OBJ_F_PC_GLOBAL_VARIABLES");
        var target = ResolveTarget(request, request.Arguments[0], "script-pc-var target");
        var index = ParseInt32(request.Arguments[1]);
        var nativeRead = InvokeInt32(
            request,
            "script_pc_var_get",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)index)]
        );

        return CreateSuccessSnapshot(
            request,
            $"Read script PC variable {index} for {target.TargetText}.",
            target,
            nativeRead,
            [
                Value("field_id", "Field Id", fieldId),
                Value("field_raw_name", "Field", ObjectFieldCatalog.RawName(fieldId)),
                Value("slot_index", "Slot Index", index),
                Value("slot_name", "Slot", ObjectFieldCatalog.ArrayElementName(fieldId, index)),
                Value("value", "Value", nativeRead.Int32Value),
                Value("unsigned_value", "Unsigned Value", unchecked((uint)nativeRead.Int32Value)),
            ]
        );
    }

    private ReadSnapshot ReadScriptPcFlag(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 2, 2, "script-pc-flag <pc-handle|player|auto> <index>");
        var fieldId = ResolveFieldIdOrThrow("OBJ_F_PC_GLOBAL_FLAGS");
        var target = ResolveTarget(request, request.Arguments[0], "script-pc-flag target");
        var index = ParseInt32(request.Arguments[1]);
        var nativeRead = InvokeInt32(
            request,
            "script_pc_flag_get",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)index)]
        );

        return CreateSuccessSnapshot(
            request,
            $"Read script PC flag {index} for {target.TargetText}.",
            target,
            nativeRead,
            [
                Value("field_id", "Field Id", fieldId),
                Value("field_raw_name", "Field", ObjectFieldCatalog.RawName(fieldId)),
                Value("bit_index", "Bit Index", index),
                Value("bucket_index", "Bucket Index", index / 32),
                Value("enabled", "Enabled", FormatBoolean(nativeRead.Int32Value != 0)),
                Value("value", "Raw Value", nativeRead.Int32Value),
            ]
        );
    }

    private ReadSnapshot ReadScriptLocalFlag(ReadRequest request)
    {
        RequireArgumentCount(
            request.Arguments,
            3,
            3,
            "script-local-flag <object-handle|player|auto> <attachment-point|index> <flag>"
        );
        var target = ResolveTarget(request, request.Arguments[0], "script-local-flag target");
        var attachmentPoint = ParseAttachmentPointId(request.Arguments[1]);
        var flagIndex = ParseInt32(request.Arguments[2]);
        var nativeRead = InvokeInt32(
            request,
            "script_local_flag_get",
            [
                ToLow32(target.Handle),
                ToHigh32(target.Handle),
                unchecked((uint)attachmentPoint),
                unchecked((uint)flagIndex),
            ]
        );

        return CreateSuccessSnapshot(
            request,
            $"Read script local flag {flagIndex} on {target.TargetText}.",
            target,
            nativeRead,
            [
                Value("attachment_point", "Attachment Point", attachmentPoint),
                Value(
                    "attachment_point_name",
                    "Attachment Point Name",
                    RuntimeSemanticCatalog.AttachmentPointName(attachmentPoint)
                ),
                Value("flag_index", "Flag Index", flagIndex),
                Value("enabled", "Enabled", FormatBoolean(nativeRead.Int32Value != 0)),
                Value("value", "Raw Value", nativeRead.Int32Value),
            ]
        );
    }

    private ReadSnapshot ReadScriptLocalCounter(ReadRequest request)
    {
        RequireArgumentCount(
            request.Arguments,
            3,
            3,
            "script-local-counter <object-handle|player|auto> <attachment-point|index> <counter>"
        );
        var target = ResolveTarget(request, request.Arguments[0], "script-local-counter target");
        var attachmentPoint = ParseAttachmentPointId(request.Arguments[1]);
        var counterIndex = ParseInt32(request.Arguments[2]);
        var nativeRead = InvokeInt32(
            request,
            "script_local_counter_get",
            [
                ToLow32(target.Handle),
                ToHigh32(target.Handle),
                unchecked((uint)attachmentPoint),
                unchecked((uint)counterIndex),
            ]
        );

        return CreateSuccessSnapshot(
            request,
            $"Read script local counter {counterIndex} on {target.TargetText}.",
            target,
            nativeRead,
            [
                Value("attachment_point", "Attachment Point", attachmentPoint),
                Value(
                    "attachment_point_name",
                    "Attachment Point Name",
                    RuntimeSemanticCatalog.AttachmentPointName(attachmentPoint)
                ),
                Value("counter_index", "Counter Index", counterIndex),
                Value("value", "Value", nativeRead.Int32Value),
            ]
        );
    }

    private ReadSnapshot ReadAreaKnown(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 2, 2, "area-known <pc-handle|player|auto> <area-id>");
        var target = ResolveTarget(request, request.Arguments[0], "area-known target");
        var areaId = ParseInt32(request.Arguments[1]);
        var nativeRead = InvokeInt32(
            request,
            "area_is_known",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)areaId)]
        );

        return CreateSuccessSnapshot(
            request,
            $"Read area-known flag for area {areaId} on {target.TargetText}.",
            target,
            nativeRead,
            [
                Value("area_id", "Area Id", areaId),
                Value("known", "Known", FormatBoolean(nativeRead.Int32Value != 0)),
                Value("value", "Raw Value", nativeRead.Int32Value),
            ]
        );
    }

    private ReadSnapshot ReadLastKnownArea(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 1, 1, "last-known-area <pc-handle|player|auto>");
        var target = ResolveTarget(request, request.Arguments[0], "last-known-area target");
        var nativeRead = InvokeInt32(
            request,
            "area_get_last_known_area",
            [ToLow32(target.Handle), ToHigh32(target.Handle)]
        );

        return CreateSuccessSnapshot(
            request,
            $"Read last-known area for {target.TargetText}.",
            target,
            nativeRead,
            [Value("area_id", "Area Id", nativeRead.Int32Value)]
        );
    }

    private ReadSnapshot ReadRumorKnown(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 2, 2, "rumor-known <pc-handle|player|auto> <rumor-id>");
        var target = ResolveTarget(request, request.Arguments[0], "rumor-known target");
        var rumorId = ParseInt32(request.Arguments[1]);
        var nativeRead = InvokeInt32(
            request,
            "rumor_known_get",
            [ToLow32(target.Handle), ToHigh32(target.Handle), unchecked((uint)rumorId)]
        );

        return CreateSuccessSnapshot(
            request,
            $"Read rumor-known flag for rumor {rumorId} on {target.TargetText}.",
            target,
            nativeRead,
            [
                Value("rumor_id", "Rumor Id", rumorId),
                Value("known", "Known", FormatBoolean(nativeRead.Int32Value != 0)),
                Value("value", "Raw Value", nativeRead.Int32Value),
            ]
        );
    }

    private ReadSnapshot ReadRumorQuelled(ReadRequest request)
    {
        RequireArgumentCount(request.Arguments, 1, 1, "rumor-quelled <rumor-id>");
        var rumorId = ParseInt32(request.Arguments[0]);
        var nativeRead = InvokeInt32(request, "rumor_qstate_get", [unchecked((uint)rumorId)]);

        return CreateSuccessSnapshot(
            request,
            $"Read rumor-quelled flag for rumor {rumorId}.",
            target: null,
            nativeRead,
            [
                Value("rumor_id", "Rumor Id", rumorId),
                Value("quelled", "Quelled", FormatBoolean(nativeRead.Int32Value != 0)),
                Value("value", "Raw Value", nativeRead.Int32Value),
            ]
        );
    }

    private NativeReadSnapshot InvokeInt32(
        ReadRequest request,
        string functionKey,
        IReadOnlyList<uint> stackArguments
    ) =>
        backend.InvokeInt32(
            request.Session.ProcessId,
            request.Session.RuntimeProfile,
            functionKey,
            stackArguments,
            ReadTimeout
        );

    private ResolvedTarget ResolveTarget(ReadRequest request, string token, string subject) =>
        TargetResolver.Resolve(backend, request.Session, token, subject);

    private static IReadOnlyList<ReadValueSnapshot> CreateArrayFieldValues(
        int fieldId,
        int index,
        int value,
        bool unsigned
    ) =>
        [
            Value("field_id", "Field Id", fieldId),
            Value("field_raw_name", "Field", ObjectFieldCatalog.RawName(fieldId)),
            Value("field_name", "Field Name", ObjectFieldCatalog.CollectionName(fieldId)),
            Value("slot_index", "Slot Index", index),
            Value("slot_name", "Slot", ObjectFieldCatalog.ArrayElementName(fieldId, index)),
            unsigned ? Value("value", "Value", unchecked((uint)value)) : Value("value", "Value", value),
            Value(
                "value_text",
                "Value Text",
                unsigned
                    ? ObjectValueFormatter.FormatArrayUInt32(fieldId, index, unchecked((uint)value))
                    : ObjectValueFormatter.FormatArrayInt32(fieldId, index, value)
            ),
        ];

    private static ReadSnapshot CreateSuccessSnapshot(
        ReadRequest request,
        string summary,
        ResolvedTarget? target,
        NativeReadSnapshot nativeRead,
        IReadOnlyList<ReadValueSnapshot> values
    )
    {
        var notes = target?.Notes ?? [];
        return new ReadSnapshot(
            DateTimeOffset.UtcNow,
            IsAvailable: true,
            "Read completed",
            summary,
            request.AdapterKey,
            [.. request.Arguments],
            target?.HandleText,
            target?.TargetText,
            values,
            nativeRead,
            notes
        );
    }

    private static ReadSnapshot CreateUnavailableSnapshot(ReadRequest request, string status, string summary) =>
        new(
            DateTimeOffset.UtcNow,
            IsAvailable: false,
            status,
            summary,
            request.AdapterKey,
            [.. request.Arguments],
            TargetHandleText: null,
            TargetText: null,
            Values: [],
            NativeRead: null,
            Notes: []
        );

    private static ReadValueSnapshot Value(string key, string label, string valueText) => new(key, label, valueText);

    private static ReadValueSnapshot Value(string key, string label, int value) =>
        Value(key, label, value.ToString(CultureInfo.InvariantCulture));

    private static ReadValueSnapshot Value(string key, string label, uint value) =>
        Value(key, label, value.ToString(CultureInfo.InvariantCulture));

    private static string FormatBoolean(bool value) => value ? "Yes" : "No";

    private static void RequireArgumentCount(IReadOnlyList<string> arguments, int minimum, int maximum, string usage)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.Count >= minimum && arguments.Count <= maximum)
            return;

        throw new InvalidOperationException($"Usage: {usage}");
    }

    private static int ParseSpellCollegeId(string value)
    {
        var normalized = Normalize(value);
        for (var index = 0; index < SpellCollegeCount; index++)
        {
            if (Normalize(ObjectFieldCatalog.SpellCollegeName(index)) == normalized)
                return index;
        }

        return ParseInt32(value);
    }

    private static int ParseStatId(string value)
    {
        var normalized = Normalize(value);
        if (StatAliases.TryGetValue(normalized, out var aliasedStat))
            return aliasedStat;

        for (var index = 0; index < RuntimeStatCount; index++)
        {
            if (Normalize(RuntimeSemanticCatalog.StatName(index)) == normalized)
                return index;
        }

        return ParseInt32(value);
    }

    private static int ParseDerivedStatId(string value)
    {
        var normalized = Normalize(value);
        if (DerivedStatAliases.TryGetValue(normalized, out var aliasedStat))
            return aliasedStat;

        for (var index = 0; index < DerivedStatLabels.Length; index++)
        {
            if (Normalize(DerivedStatLabels[index]) == normalized)
                return DerivedStatBaseIndex + index;
        }

        return ParseInt32(value);
    }

    private static int ParseResistanceId(string value)
    {
        var normalized = Normalize(value);
        if (ResistanceAliases.TryGetValue(normalized, out var aliasedResistance))
            return aliasedResistance;

        for (var index = 0; index < ResistanceCount; index++)
        {
            if (Normalize(ObjectFieldCatalog.ResistanceName(index)) == normalized)
                return index;
        }

        return ParseInt32(value);
    }

    private static int ParseTechDisciplineId(string value)
    {
        var normalized = Normalize(value);
        if (TechDisciplineAliases.TryGetValue(normalized, out var aliasedDiscipline))
            return aliasedDiscipline;

        for (var index = 0; index < TechDisciplineLabels.Length; index++)
        {
            if (Normalize(TechDisciplineLabels[index]) == normalized)
                return index;
        }

        return ParseInt32(value);
    }

    private static int ParseAttachmentPointId(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericAttachmentPoint))
            return numericAttachmentPoint;

        var normalized = Normalize(value);
        for (var index = 0; index < MaxAttachmentPointScan; index++)
        {
            if (Normalize(RuntimeSemanticCatalog.AttachmentPointName(index)) == normalized)
                return index;
        }

        throw new InvalidOperationException(
            $"Unknown attachment point '{value}'. Example values: dialog, heartbeat, use, first-heartbeat, 9."
        );
    }

    private static DebuggerArrayFieldReference ResolveFieldReference(
        string fieldToken,
        string indexToken,
        string? storageToken
    )
    {
        var fieldId = TryParseFieldId(fieldToken, out var parsedFieldId)
            ? parsedFieldId
            : ResolveFieldIdOrThrow(fieldToken);
        return new DebuggerArrayFieldReference(
            fieldId,
            ObjectFieldCatalog.RawName(fieldId),
            ParseInt32(indexToken),
            ParseArrayFieldUnsigned(storageToken)
        );
    }

    private static DebuggerReadSkillReference ParseSkillReference(string value)
    {
        var normalized = Normalize(value);
        if (normalized.StartsWith("basic", StringComparison.Ordinal))
            return new DebuggerReadSkillReference("OBJ_F_CRITTER_BASIC_SKILL_IDX", ParseIndexedSuffix(value, "basic"));

        if (normalized.StartsWith("tech", StringComparison.Ordinal))
            return new DebuggerReadSkillReference("OBJ_F_CRITTER_TECH_SKILL_IDX", ParseIndexedSuffix(value, "tech"));

        if (BasicSkillAliases.TryGetValue(normalized, out var basicIndex))
            return new DebuggerReadSkillReference("OBJ_F_CRITTER_BASIC_SKILL_IDX", basicIndex);

        if (TechSkillAliases.TryGetValue(normalized, out var techIndex))
            return new DebuggerReadSkillReference("OBJ_F_CRITTER_TECH_SKILL_IDX", techIndex);

        throw new InvalidOperationException(
            $"Unknown skill '{value}'. Examples: haggle, persuasion, repair, firearms, basic:9, tech:2."
        );
    }

    private static int ParseIndexedSuffix(string value, string prefix)
    {
        var parts = value.Split(
            [':', '[', ']'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        if (parts.Length >= 2 && Normalize(parts[0]) == prefix)
            return ParseInt32(parts[1]);

        throw new InvalidOperationException(
            $"Expected '{prefix}:<index>' or '{prefix}[<index>]' but received '{value}'."
        );
    }

    private static int ResolveFieldIdOrThrow(string rawFieldName)
    {
        if (ObjectFieldCatalog.TryGetFieldId(rawFieldName, out var fieldId))
            return fieldId;

        throw new InvalidOperationException($"Unable to resolve runtime object field '{rawFieldName}'.");
    }

    private static bool TryParseFieldId(string token, out int fieldId)
    {
        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (uint.TryParse(token[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue))
            {
                fieldId = unchecked((int)hexValue);
                return true;
            }

            fieldId = 0;
            return false;
        }

        return int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out fieldId);
    }

    private static bool ParseArrayFieldUnsigned(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        return Normalize(token) switch
        {
            "signed" or "int32" or "i32" => false,
            "unsigned" or "uint32" or "u32" => true,
            _ => throw new InvalidOperationException("Field storage must be signed/int32 or unsigned/uint32."),
        };
    }

    private static uint ToLow32(ulong value) => unchecked((uint)(value & uint.MaxValue));

    private static uint ToHigh32(ulong value) => unchecked((uint)(value >> 32));

    private static int ParseInt32(string token)
    {
        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return unchecked((int)Convert.ToUInt32(token[2..], 16));

        return int.Parse(token, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

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

    private static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(1);
    private const int SpellCollegeCount = 16;
    private const int RuntimeStatCount = 28;
    private const int DerivedStatBaseIndex = 8;
    private const int ResistanceCount = 5;
    private const int MaxAttachmentPointScan = 128;

    private static readonly string[] DerivedStatLabels =
    [
        "CarryWeight",
        "DamageBonus",
        "AcAdjustment",
        "Speed",
        "HealRate",
        "PoisonRecovery",
        "ReactionModifier",
        "MaxFollowers",
        "MagickTechAptitude",
    ];

    private static readonly string[] TechDisciplineLabels =
    [
        "Herbology",
        "Chemistry",
        "Electric",
        "Explosives",
        "Gun Smithy",
        "Mechanical",
        "Smithy",
        "Therapeutics",
    ];

    private static readonly Dictionary<string, int> BasicSkillAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bow"] = 0,
        ["dodge"] = 1,
        ["melee"] = 2,
        ["throw"] = 3,
        ["backstab"] = 4,
        ["pickpocket"] = 5,
        ["pickpocketing"] = 5,
        ["prowling"] = 6,
        ["prowl"] = 6,
        ["spottrap"] = 7,
        ["spottraps"] = 7,
        ["gambling"] = 8,
        ["gamble"] = 8,
        ["haggle"] = 9,
        ["haggling"] = 9,
        ["heal"] = 10,
        ["persuasion"] = 11,
        ["persuade"] = 11,
    };

    private static readonly Dictionary<string, int> TechSkillAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["repair"] = 0,
        ["firearms"] = 1,
        ["firearm"] = 1,
        ["picklocks"] = 2,
        ["picklock"] = 2,
        ["disarmtraps"] = 3,
        ["disarmtrap"] = 3,
    };

    private static readonly Dictionary<string, int> StatAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["level"] = 17,
        ["xp"] = 18,
        ["experience"] = 18,
        ["experiencepoints"] = 18,
        ["alignment"] = 19,
        ["align"] = 19,
        ["fate"] = 20,
        ["skillpoints"] = 21,
        ["unspentpoints"] = 21,
        ["unspent"] = 21,
        ["mp"] = 22,
        ["magick"] = 22,
        ["magickpoints"] = 22,
        ["tp"] = 23,
        ["tech"] = 23,
        ["techpoints"] = 23,
        ["ac"] = 10,
    };

    private static readonly Dictionary<string, int> DerivedStatAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["carryweight"] = 8,
        ["carry"] = 8,
        ["damagebonus"] = 9,
        ["dmgbonus"] = 9,
        ["acadjustment"] = 10,
        ["acadj"] = 10,
        ["speed"] = 11,
        ["healrate"] = 12,
        ["poisonrecovery"] = 13,
        ["reactionmodifier"] = 14,
        ["reactionmod"] = 14,
        ["maxfollowers"] = 15,
        ["followers"] = 15,
        ["magicktechaptitude"] = 16,
        ["magickaptitude"] = 16,
        ["techaptitude"] = 16,
    };

    private static readonly Dictionary<string, int> ResistanceAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["normalresistance"] = 0,
        ["physicalresistance"] = 0,
        ["fireresistance"] = 1,
        ["electricalresistance"] = 2,
        ["electricresistance"] = 2,
        ["poisonresistance"] = 3,
        ["magicresistance"] = 4,
    };

    private static readonly Dictionary<string, int> TechDisciplineAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["herbology"] = 0,
        ["chemistry"] = 1,
        ["electric"] = 2,
        ["explosives"] = 3,
        ["gunsmithy"] = 4,
        ["gunsmith"] = 4,
        ["mechanical"] = 5,
        ["smithy"] = 6,
        ["therapeutics"] = 7,
        ["therapeutic"] = 7,
    };

    private readonly record struct DebuggerReadSkillReference(string FieldRawName, int Index);

    private readonly record struct DebuggerArrayFieldReference(
        int FieldId,
        string FieldRawName,
        int Index,
        bool Unsigned
    );
}
