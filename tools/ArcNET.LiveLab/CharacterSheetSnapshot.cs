namespace ArcNET.LiveLab;

internal sealed class CharacterSheetSnapshot
{
    public required DateTimeOffset CapturedAtUtc { get; init; }

    public required int ProcessId { get; init; }

    public required string ProcessName { get; init; }

    public required string ModuleBase { get; init; }

    public required string ModulePath { get; init; }

    public required uint CurrentCharacterSheetId { get; init; }

    public required int ActionPoints { get; init; }

    public required CharacterSheetPointerSnapshot Pointers { get; init; }

    public required List<RuntimeFieldValueSnapshot> MainStats { get; init; }

    public required List<RuntimeFieldValueSnapshot> BasicSkills { get; init; }

    public required List<RuntimeFieldValueSnapshot> TechSkills { get; init; }

    public required List<RuntimeFieldValueSnapshot> SpellAndTech { get; init; }

    public required CharacterSheetPropertySnapshot Properties { get; init; }

    public required CharacterSheetHookTelemetrySnapshot HookTelemetry { get; init; }

    public required List<string> MissingCaptures { get; init; }
}

internal sealed class CharacterSheetPointerSnapshot
{
    public required string Character { get; init; }

    public required string MainStats { get; init; }

    public required string BasicSkills { get; init; }

    public required string TechSkills { get; init; }

    public required string SpellAndTech { get; init; }
}

internal sealed class RuntimeFieldValueSnapshot
{
    public required string Name { get; init; }

    public required int Offset { get; init; }

    public required string Address { get; init; }

    public required int Value { get; init; }
}

internal sealed class RuntimeScalarSnapshot
{
    public required string Address { get; init; }

    public required int Value { get; init; }
}

internal sealed class FlagsSnapshot
{
    public required string Address { get; init; }

    public required uint Value { get; init; }

    public required string Bits { get; init; }
}

internal sealed class NameSnapshot
{
    public required string PropertyAddress { get; init; }

    public required string StringPointer { get; init; }

    public required string Value { get; init; }
}

internal sealed class CharacterSheetPropertySnapshot
{
    public RuntimeScalarSnapshot? HpBonus { get; init; }

    public RuntimeScalarSnapshot? HpLoss { get; init; }

    public RuntimeScalarSnapshot? MpBonus { get; init; }

    public RuntimeScalarSnapshot? MpLoss { get; init; }

    public FlagsSnapshot? Flags { get; init; }

    public NameSnapshot? Name { get; init; }
}

internal sealed class CharacterSheetHookTelemetrySnapshot
{
    public required uint SeenKnownSubstructureMask { get; init; }

    public required uint LastSubstructureIndex { get; init; }

    public required uint LastSubstructureId { get; init; }

    public required string LastSubstructurePointer { get; init; }

    public required uint LastPropertyIndex { get; init; }

    public required uint LastPropertyId { get; init; }

    public required string LastPropertyAddress { get; init; }
}

internal readonly record struct CapturedPointers(
    nint Character,
    nint MainStats,
    nint BasicSkills,
    nint TechSkills,
    nint SpellAndTech,
    nint HpLoss,
    nint HpBonus,
    nint MpLoss,
    nint MpBonus,
    nint Name,
    nint Flags,
    uint SeenKnownSubstructureMask,
    uint LastSubstructureIndex,
    uint LastSubstructureId,
    nint LastSubstructurePointer,
    uint LastPropertyIndex,
    uint LastPropertyId,
    nint LastPropertyAddress
)
{
    public bool HasAnyCapture =>
        Character != 0 || MainStats != 0 || BasicSkills != 0 || TechSkills != 0 || SpellAndTech != 0;
}

internal readonly record struct ResolvedIntField(string Name, nint Address, int Value);
