using ArcNET.Formats;

namespace ArcNET.Diagnostics;

public static class RuntimeWatchValueCatalog
{
    public const int QuestBotchedModifier = 0x100;

    public static string FormatPackedLocation(ulong rawValue)
    {
        var tileX = unchecked((int)(rawValue & uint.MaxValue));
        var tileY = unchecked((int)(rawValue >> 32));
        return $"({tileX}, {tileY})";
    }

    public static string FormatGameDateTime(ulong rawValue)
    {
        var days = unchecked((uint)(rawValue & uint.MaxValue));
        var milliseconds = unchecked((uint)(rawValue >> 32));
        var hours = milliseconds / 3_600_000;
        var minutes = (milliseconds % 3_600_000) / 60_000;
        var seconds = (milliseconds % 60_000) / 1_000;
        var millisecondsRemainder = milliseconds % 1_000;
        return $"day {days} {hours:00}:{minutes:00}:{seconds:00}.{millisecondsRemainder:000}";
    }

    public static string ViewTypeName(int viewType) =>
        viewType switch
        {
            0 => "Isometric",
            1 => "TopDown",
            _ => $"ViewType[{viewType}]",
        };

    public static string QuestStateName(int state) =>
        state switch
        {
            0 => "Unknown",
            1 => "Mentioned",
            2 => "Accepted",
            3 => "Achieved",
            4 => "Completed",
            5 => "OtherCompleted",
            6 => "Botched",
            _ => $"QuestState[{state}]",
        };

    public static int QuestBaseState(int rawState) => rawState & ~QuestBotchedModifier;

    public static bool QuestHasBotchedModifier(int rawState) => (rawState & QuestBotchedModifier) != 0;

    public static string QuestPcStateName(int rawState)
    {
        var state = QuestBaseState(rawState);
        return QuestHasBotchedModifier(rawState) && state != 6
            ? $"{QuestStateName(state)} [Botched]"
            : QuestStateName(state);
    }

    public static string QuestStateVerb(int state) =>
        state switch
        {
            1 => "mentioned",
            2 => "accepted",
            3 => "achieved",
            4 => "completed",
            5 => "completed elsewhere",
            6 => "botched",
            _ => $"set to {QuestStateName(state)}",
        };

    public static string ScriptFlagsText(uint flags)
    {
        var knownFlags = unchecked((ushort)(flags & ushort.MaxValue));
        var text = Enum.Format(typeof(ScriptFlags), (ScriptFlags)knownFlags, "F");
        var unknownMask = flags & 0xFFFF0000u;
        return unknownMask == 0 ? $"{text} (0x{flags:X8})" : $"{text}, 0x{unknownMask:X8} (0x{flags:X8})";
    }

    public static string ScriptLocalFlagName(int flag)
    {
        var value = 1u << flag;
        return Enum.IsDefined(typeof(ScriptFlags), unchecked((ushort)value))
            ? ((ScriptFlags)unchecked((ushort)value)).ToString()
            : $"ScriptFlagBit[{flag}]";
    }

    public static string EffectCauseName(int cause) =>
        cause switch
        {
            0 => "Race",
            1 => "Background",
            2 => "Class",
            3 => "Bless",
            4 => "Curse",
            5 => "Item",
            6 => "Spell",
            7 => "Injury",
            8 => "Tech",
            9 => "Gender",
            _ => $"EffectCause[{cause}]",
        };

    public static string FallbackEffectName(int effectId) =>
        effectId switch
        {
            50 => "Scarring",
            156 => "Polymorph",
            158 => "Encumbrance",
            330 => "Female",
            >= 64 and < 75 => $"RaceSpecific[{effectId - 64}]",
            >= 75 and < 156 => $"ClassSpecific[{effectId - 75}]",
            _ => $"Effect {effectId}",
        };

    public static RuntimeWatchTimeEventTypeDescriptor TimeEventDescriptor(int type) =>
        type >= 0 && type < TimeEventDescriptors.Length
            ? TimeEventDescriptors[type]
            : new RuntimeWatchTimeEventTypeDescriptor(
                $"TimeEventType[{type}]",
                "UnknownTimeType",
                Saveable: false,
                RuntimeWatchTimeEventParamKind.None,
                RuntimeWatchTimeEventParamKind.None,
                RuntimeWatchTimeEventParamKind.None,
                RuntimeWatchTimeEventParamKind.None
            );

    public static bool IsNoiseTimeEventType(int type) =>
        type
            is 2 // Bkg Anim
                or 3 // Fidget Anim
                or 10 // AI
                or 12 // TB Combat
                or 13 // Ambient Lighting
                or 16 // Clock
                or 18 // MainMenu
                or 19 // Light
                or 20 // Multiplayer
                or 30 // MP Ctrl UI
                or 31; // UI

    public static string MagicTechActionName(int action) =>
        action switch
        {
            0 => "Begin",
            1 => "Maintain",
            2 => "End",
            3 => "Callback",
            4 => "EndCallback",
            _ => $"MagicTechAction[{action}]",
        };

    public static string MagicTechRunFlagsText(uint flags) =>
        JoinFlagNames(
            flags,
            [
                (0x0001u, "Active"),
                (0x0002u, "Free"),
                (0x0004u, "Flag0x04"),
                (0x0008u, "Reflected"),
                (0x0010u, "Unresistable"),
                (0x0020u, "Flag0x20"),
                (0x0040u, "Flag0x40"),
                (0x80000000u, "Dispelled"),
            ]
        );

    public static string TeleportFlagsText(uint flags) =>
        JoinFlagNames(
            flags,
            [
                (0x0001u, "Movie1"),
                (0x0002u, "FadeOut"),
                (0x0004u, "FadeIn"),
                (0x0008u, "TimeAdvance"),
                (0x0010u, "Sound"),
                (0x0020u, "RenderLock"),
                (0x0040u, "Movie2"),
                (0x0100u, "SkipFollowers"),
            ]
        );

    public static string WindowFlagsText(uint flags) =>
        JoinFlagNames(
            flags,
            [
                (0x0001u, "Transparent"),
                (0x0002u, "MessageFilter"),
                (0x0004u, "Modal"),
                (0x0008u, "AlwaysOnTop"),
                (0x0010u, "VideoMemory"),
                (0x0020u, "Hidden"),
                (0x0040u, "RenderTarget"),
                (0x0080u, "AlwaysOnBottom"),
            ]
        );

    public static string ArtBlitFlagsText(uint flags) =>
        JoinFlagNames(
            flags,
            [
                (0x00000001u, "FlipX"),
                (0x00000002u, "FlipY"),
                (0x00000004u, "PaletteOriginal"),
                (0x00000008u, "PaletteOverride"),
                (0x00000010u, "BlendAdd"),
                (0x00000020u, "BlendSub"),
                (0x00000040u, "BlendMul"),
                (0x00000080u, "BlendAlphaAvg"),
                (0x00000100u, "BlendAlphaConst"),
                (0x00000200u, "BlendAlphaSrc"),
                (0x00000400u, "BlendAlphaLerpX"),
                (0x00000800u, "BlendAlphaLerpY"),
                (0x00001000u, "BlendAlphaLerpBoth"),
                (0x00002000u, "BlendColorConst"),
                (0x00004000u, "BlendColorArray"),
                (0x00008000u, "BlendAlphaStippleS"),
                (0x00010000u, "BlendAlphaStippleD"),
                (0x00020000u, "BlendColorLerp"),
                (0x01000000u, "ScratchValid"),
            ]
        );

    public static string ArtTypeName(int artType) =>
        artType switch
        {
            0 => "Tile",
            1 => "Wall",
            2 => "Critter",
            3 => "Portal",
            4 => "Scenery",
            5 => "Interface",
            6 => "Item",
            7 => "Container",
            8 => "Misc",
            9 => "Light",
            10 => "Roof",
            11 => "Facade",
            12 => "Monster",
            13 => "UniqueNpc",
            14 => "EyeCandy",
            _ => $"ArtType[{artType}]",
        };

    public static string ArtIdSummary(int artId)
    {
        var artType = ArtTypeFromId(artId);
        var artTypeName = ArtTypeName(artType);
        return TryGetArtNumber(artId, out var artNumber)
            ? $"{artTypeName}#{artNumber} (0x{unchecked((uint)artId):X8})"
            : $"{artTypeName} (0x{unchecked((uint)artId):X8})";
    }

    private static string JoinFlagNames(uint flags, (uint Mask, string Name)[] descriptors)
    {
        if (flags == 0)
            return "None";

        List<string> names = [];
        uint unknown = flags;
        foreach (var (mask, name) in descriptors)
        {
            if ((flags & mask) == 0)
                continue;

            names.Add(name);
            unknown &= ~mask;
        }

        if (unknown != 0)
            names.Add($"0x{unknown:X8}");

        return string.Join(", ", names);
    }

    private static int ArtTypeFromId(int artId) => (int)(unchecked((uint)artId) >> ArtIdTypeShift);

    private static bool TryGetArtNumber(int artId, out int artNumber)
    {
        artNumber = 0;
        switch (ArtTypeFromId(artId))
        {
            case 0:
            case 1:
            case 2:
            case 12:
                return false;
            case 13:
                artNumber = (int)((unchecked((uint)artId) >> UniqueNpcIdNumShift) & (UniqueNpcIdMaxNum - 1));
                return true;
            case 5:
                artNumber = (int)((unchecked((uint)artId) >> InterfaceIdNumShift) & (InterfaceIdMaxNum - 1));
                return true;
            case 6:
                artNumber = (int)((unchecked((uint)artId) >> 17) & 0x7FF);
                return true;
            default:
                artNumber = (int)((unchecked((uint)artId) >> ArtIdNumShift) & (ArtIdMaxNum - 1));
                return true;
        }
    }

    private const int ArtIdTypeShift = 28;
    private const int ArtIdMaxNum = 512;
    private const int ArtIdNumShift = 19;
    private const int InterfaceIdMaxNum = 4096;
    private const int InterfaceIdNumShift = 16;
    private const int UniqueNpcIdMaxNum = 1024;
    private const int UniqueNpcIdNumShift = 20;

    private static readonly RuntimeWatchTimeEventTypeDescriptor[] TimeEventDescriptors =
    [
        new(
            "Debug",
            "RealTime",
            Saveable: false,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Anim",
            "Animations",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "BkgAnim",
            "RealTime",
            Saveable: false,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "FidgetAnim",
            "RealTime",
            Saveable: false,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Script",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.Object,
            RuntimeWatchTimeEventParamKind.Object
        ),
        new(
            "MagicTech",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Poison",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.Object,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Resting",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Fatigue",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.Object,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Aging",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "AI",
            "GameTime",
            Saveable: false,
            RuntimeWatchTimeEventParamKind.Object,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Combat",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "TBCombat",
            "RealTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "AmbientLighting",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "WorldMap",
            "RealTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Sleeping",
            "RealTime",
            Saveable: false,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Clock",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "NpcWaitHere",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.Object,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "MainMenu",
            "RealTime",
            Saveable: false,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Light",
            "Animations",
            Saveable: false,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Multiplayer",
            "RealTime",
            Saveable: false,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Lock",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.Object,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "NpcRespawn",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.Object,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "RechargeMagicItem",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.Object,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "DecayDeadBody",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.Object,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "ItemDecay",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.Object,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "CombatFocusWipe",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.Object,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Newspapers",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Traps",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.Location,
            RuntimeWatchTimeEventParamKind.Object,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Fade",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.Float,
            RuntimeWatchTimeEventParamKind.Integer
        ),
        new(
            "MpCtrlUi",
            "RealTime",
            Saveable: false,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Ui",
            "RealTime",
            Saveable: false,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.Integer,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "Teleported",
            "GameTime",
            Saveable: false,
            RuntimeWatchTimeEventParamKind.Object,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "SceneryRespawn",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.Object,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
        new(
            "RandomEncounter",
            "GameTime",
            Saveable: true,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None,
            RuntimeWatchTimeEventParamKind.None
        ),
    ];
}
