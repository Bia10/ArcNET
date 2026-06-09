using System.Globalization;
using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class SheetBackend : ISheetBackend
{
    public LivePlayerLocatorResult LocatePlayers(int processId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live sheet diagnostics currently require Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LivePlayerLocator.Locate(memory);
    }

    public LiveObjectIdentity InspectHandle(int processId, ulong handle)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live sheet diagnostics currently require Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LiveObjectInspector.Inspect(memory, handle);
    }

    public SheetDataSnapshot ReadSheetData(int processId, RuntimeProfileSnapshot runtimeProfile, ulong handle)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live sheet diagnostics currently require Windows.");

        using var memory = ProcessMemory.Attach(processId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        return new SheetDataSnapshot(
            [
                .. Enumerable
                    .Range(0, PrimaryStatCount)
                    .Select(index => new SheetScalarSnapshot(
                        index,
                        RuntimeSemanticCatalog.StatName(index),
                        ReadStatValue(dispatcher, memory, handle, index)
                    )),
            ],
            [
                .. ProgressionStatIds.Select(statId => new SheetScalarSnapshot(
                    statId,
                    RuntimeSemanticCatalog.StatName(statId),
                    ReadStatValue(dispatcher, memory, handle, statId)
                )),
            ],
            [
                .. Enumerable
                    .Range(0, DerivedStatLabels.Length)
                    .Select(index => new SheetScalarSnapshot(
                        DerivedStatBaseIndex + index,
                        DerivedStatLabels[index],
                        ReadStatValue(dispatcher, memory, handle, DerivedStatBaseIndex + index)
                    )),
            ],
            [
                .. Enumerable
                    .Range(0, ResistanceLabels.Length)
                    .Select(index => new SheetScalarSnapshot(
                        index,
                        ResistanceLabels[index],
                        ReadResistanceValue(dispatcher, memory, handle, index)
                    )),
            ],
            [
                .. Enumerable
                    .Range(0, BasicSkillLabels.Length)
                    .Select(index =>
                    {
                        var encoded = ReadArrayInt32Value(dispatcher, memory, handle, s_basicSkillFieldId, index);
                        var training = (encoded >> 6) & 3;
                        return new SheetSkillSnapshot(
                            index,
                            BasicSkillLabels[index],
                            encoded & 63,
                            training,
                            ObjectValueFormatter.FormatSkillTraining(training),
                            encoded
                        );
                    }),
            ],
            [
                .. Enumerable
                    .Range(0, TechSkillLabels.Length)
                    .Select(index =>
                    {
                        var encoded = ReadArrayInt32Value(dispatcher, memory, handle, s_techSkillFieldId, index);
                        var training = (encoded >> 6) & 3;
                        return new SheetSkillSnapshot(
                            index,
                            TechSkillLabels[index],
                            encoded & 63,
                            training,
                            ObjectValueFormatter.FormatSkillTraining(training),
                            encoded
                        );
                    }),
            ],
            [
                .. Enumerable
                    .Range(0, SpellCollegeCount)
                    .Select(index => new SheetScalarSnapshot(
                        index,
                        ObjectFieldCatalog.SpellCollegeName(index),
                        ReadArrayInt32Value(dispatcher, memory, handle, s_spellTechFieldId, index)
                    )),
            ],
            new SheetScalarSnapshot(
                SpellCollegeCount,
                "Spell Mastery",
                ReadArrayInt32Value(dispatcher, memory, handle, s_spellTechFieldId, SpellCollegeCount)
            ),
            [
                .. Enumerable
                    .Range(0, TechDisciplineLabels.Length)
                    .Select(index => new SheetScalarSnapshot(
                        index,
                        TechDisciplineLabels[index],
                        ReadArrayInt32Value(
                            dispatcher,
                            memory,
                            handle,
                            s_spellTechFieldId,
                            SpellCollegeCount + 1 + index
                        )
                    )),
            ]
        );
    }

    private static int ReadStatValue(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        ulong handle,
        int statId
    ) =>
        dispatcher.InvokeInt32(
            s_statBaseGetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)statId)],
            ReadTimeout
        );

    private static int ReadResistanceValue(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        ulong handle,
        int resistanceId
    ) =>
        dispatcher.InvokeInt32(
            s_objectResistanceGetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)resistanceId), 1u],
            ReadTimeout
        );

    private static int ReadArrayInt32Value(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        ulong handle,
        int fieldId,
        int index
    ) =>
        dispatcher.InvokeInt32(
            s_objectArrayFieldGetter,
            [ToLow32(handle), ToHigh32(handle), unchecked((uint)fieldId), unchecked((uint)index)],
            ReadTimeout
        );

    private static int ResolveFieldId(string rawName)
    {
        if (ObjectFieldCatalog.TryGetFieldId(rawName, out var fieldId))
            return fieldId;

        throw new InvalidOperationException($"Unable to resolve runtime object field '{rawName}'.");
    }

    private static uint ToLow32(ulong value) => unchecked((uint)(value & uint.MaxValue));

    private static uint ToHigh32(ulong value) => unchecked((uint)(value >> 32));

    private static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(1);
    private const int PrimaryStatCount = 8;
    private const int DerivedStatBaseIndex = 8;
    private const int SpellCollegeCount = 16;
    private static readonly int[] ProgressionStatIds = [17, 18, 19, 20, 21, 22, 23];
    private static readonly FunctionDefinition s_objectArrayFieldGetter = FunctionCatalog.GetDefinition(
        "obj_array_field_int32_get"
    );
    private static readonly FunctionDefinition s_objectResistanceGetter = FunctionCatalog.GetDefinition(
        "object_get_resistance"
    );
    private static readonly FunctionDefinition s_statBaseGetter = FunctionCatalog.GetDefinition("stat_base_get");
    private static readonly int s_basicSkillFieldId = ResolveFieldId("OBJ_F_CRITTER_BASIC_SKILL_IDX");
    private static readonly int s_techSkillFieldId = ResolveFieldId("OBJ_F_CRITTER_TECH_SKILL_IDX");
    private static readonly int s_spellTechFieldId = ResolveFieldId("OBJ_F_CRITTER_SPELL_TECH_IDX");

    private static readonly string[] BasicSkillLabels =
    [
        "Bow",
        "Dodge",
        "Melee",
        "Throw",
        "Backstab",
        "Pick Pocket",
        "Prowling",
        "Spot Trap",
        "Gambling",
        "Haggle",
        "Heal",
        "Persuasion",
    ];

    private static readonly string[] TechSkillLabels = ["Repair", "Firearms", "Pick Locks", "Disarm Traps"];

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

    private static readonly string[] ResistanceLabels = ["Normal", "Fire", "Electrical", "Poison", "Magic"];

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
}
