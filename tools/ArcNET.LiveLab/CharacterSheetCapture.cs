using System.Runtime.Versioning;
using ArcNET.Editor.Runtime;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal static class CharacterSheetCapture
{
    public static CharacterSheetSnapshot Create(ProcessMemory memory, CapturedPointers pointers)
    {
        var missing = new List<string>();

        var mainStats = ReadGroup(
            memory,
            pointers.MainStats,
            CharacterSheetRuntimeLayout.MainStatsFields,
            "main-stats",
            missing
        );
        var basicSkills = ReadGroup(
            memory,
            pointers.BasicSkills,
            CharacterSheetRuntimeLayout.BasicSkillsFields,
            "basic-skills",
            missing
        );
        var techSkills = ReadGroup(
            memory,
            pointers.TechSkills,
            CharacterSheetRuntimeLayout.TechSkillsFields,
            "tech-skills",
            missing
        );
        var spellAndTech = ReadGroup(
            memory,
            pointers.SpellAndTech,
            CharacterSheetRuntimeLayout.SpellAndTechFields,
            "spell-tech",
            missing
        );

        return new CharacterSheetSnapshot
        {
            CapturedAtUtc = DateTimeOffset.UtcNow,
            ProcessId = memory.ProcessId,
            ProcessName = memory.ProcessName,
            ModuleBase = ProcessMemory.FormatAddress(memory.ModuleBase),
            ModulePath = memory.ModulePath,
            CurrentCharacterSheetId = memory.ReadUInt32(
                memory.ResolveRva(ArcanumRuntimeOffsets.CurrentCharacterSheetIdRva)
            ),
            ActionPoints = memory.ReadInt32(memory.ResolveRva(ArcanumRuntimeOffsets.ActionPointsRva)),
            Pointers = new CharacterSheetPointerSnapshot
            {
                Character = ProcessMemory.FormatAddress(pointers.Character),
                MainStats = ProcessMemory.FormatAddress(pointers.MainStats),
                BasicSkills = ProcessMemory.FormatAddress(pointers.BasicSkills),
                TechSkills = ProcessMemory.FormatAddress(pointers.TechSkills),
                SpellAndTech = ProcessMemory.FormatAddress(pointers.SpellAndTech),
            },
            MainStats = mainStats,
            BasicSkills = basicSkills,
            TechSkills = techSkills,
            SpellAndTech = spellAndTech,
            Properties = new CharacterSheetPropertySnapshot
            {
                HpBonus = ReadScalar(memory, pointers.HpBonus, "hp-bonus", missing),
                HpLoss = ReadScalar(memory, pointers.HpLoss, "hp-loss", missing),
                MpBonus = ReadScalar(memory, pointers.MpBonus, "mp-bonus", missing),
                MpLoss = ReadScalar(memory, pointers.MpLoss, "mp-loss", missing),
                Flags = ReadFlags(memory, pointers.Flags, missing),
                Name = ReadName(memory, pointers.Name, missing),
            },
            HookTelemetry = new CharacterSheetHookTelemetrySnapshot
            {
                SeenKnownSubstructureMask = pointers.SeenKnownSubstructureMask,
                LastSubstructureIndex = pointers.LastSubstructureIndex,
                LastSubstructureId = pointers.LastSubstructureId,
                LastSubstructurePointer = ProcessMemory.FormatAddress(pointers.LastSubstructurePointer),
                LastPropertyIndex = pointers.LastPropertyIndex,
                LastPropertyId = pointers.LastPropertyId,
                LastPropertyAddress = ProcessMemory.FormatAddress(pointers.LastPropertyAddress),
            },
            MissingCaptures = missing,
        };
    }

    public static bool TryResolveIntField(
        ProcessMemory memory,
        CapturedPointers pointers,
        string fieldName,
        out ResolvedIntField field
    )
    {
        foreach (var descriptor in CharacterSheetRuntimeLayout.MainStatsFields)
        {
            if (Matches(descriptor.Name, fieldName) && pointers.MainStats != 0)
            {
                var address = pointers.MainStats + descriptor.Offset;
                field = new ResolvedIntField(descriptor.Name, address, memory.ReadInt32(address));
                return true;
            }
        }

        foreach (var descriptor in CharacterSheetRuntimeLayout.BasicSkillsFields)
        {
            if (Matches(descriptor.Name, fieldName) && pointers.BasicSkills != 0)
            {
                var address = pointers.BasicSkills + descriptor.Offset;
                field = new ResolvedIntField(descriptor.Name, address, memory.ReadInt32(address));
                return true;
            }
        }

        foreach (var descriptor in CharacterSheetRuntimeLayout.TechSkillsFields)
        {
            if (Matches(descriptor.Name, fieldName) && pointers.TechSkills != 0)
            {
                var address = pointers.TechSkills + descriptor.Offset;
                field = new ResolvedIntField(descriptor.Name, address, memory.ReadInt32(address));
                return true;
            }
        }

        foreach (var descriptor in CharacterSheetRuntimeLayout.SpellAndTechFields)
        {
            if (Matches(descriptor.Name, fieldName) && pointers.SpellAndTech != 0)
            {
                var address = pointers.SpellAndTech + descriptor.Offset;
                field = new ResolvedIntField(descriptor.Name, address, memory.ReadInt32(address));
                return true;
            }
        }

        if (Matches("HpBonus", fieldName) && pointers.HpBonus != 0)
        {
            field = new ResolvedIntField("HpBonus", pointers.HpBonus, memory.ReadInt32(pointers.HpBonus));
            return true;
        }

        if (Matches("HpLoss", fieldName) && pointers.HpLoss != 0)
        {
            field = new ResolvedIntField("HpLoss", pointers.HpLoss, memory.ReadInt32(pointers.HpLoss));
            return true;
        }

        if (Matches("MpBonus", fieldName) && pointers.MpBonus != 0)
        {
            field = new ResolvedIntField("MpBonus", pointers.MpBonus, memory.ReadInt32(pointers.MpBonus));
            return true;
        }

        if (Matches("MpLoss", fieldName) && pointers.MpLoss != 0)
        {
            field = new ResolvedIntField("MpLoss", pointers.MpLoss, memory.ReadInt32(pointers.MpLoss));
            return true;
        }

        if (Matches("Flags", fieldName) && pointers.Flags != 0)
        {
            field = new ResolvedIntField("Flags", pointers.Flags, unchecked((int)memory.ReadUInt32(pointers.Flags)));
            return true;
        }

        field = default;
        return false;
    }

    private static List<RuntimeFieldValueSnapshot> ReadGroup(
        ProcessMemory memory,
        nint baseAddress,
        IReadOnlyList<RuntimeFieldDescriptor> descriptors,
        string captureName,
        ICollection<string> missing
    )
    {
        if (baseAddress == 0)
        {
            missing.Add(captureName);
            return [];
        }

        var values = new List<RuntimeFieldValueSnapshot>(descriptors.Count);
        foreach (var descriptor in descriptors)
        {
            var address = baseAddress + descriptor.Offset;
            values.Add(
                new RuntimeFieldValueSnapshot
                {
                    Name = descriptor.Name,
                    Offset = descriptor.Offset,
                    Address = ProcessMemory.FormatAddress(address),
                    Value = memory.ReadInt32(address),
                }
            );
        }

        return values;
    }

    private static RuntimeScalarSnapshot? ReadScalar(
        ProcessMemory memory,
        nint address,
        string captureName,
        ICollection<string> missing
    )
    {
        if (address == 0)
        {
            missing.Add(captureName);
            return null;
        }

        return new RuntimeScalarSnapshot
        {
            Address = ProcessMemory.FormatAddress(address),
            Value = memory.ReadInt32(address),
        };
    }

    private static FlagsSnapshot? ReadFlags(ProcessMemory memory, nint address, ICollection<string> missing)
    {
        if (address == 0)
        {
            missing.Add("flags");
            return null;
        }

        var value = memory.ReadUInt32(address);
        return new FlagsSnapshot
        {
            Address = ProcessMemory.FormatAddress(address),
            Value = value,
            Bits = Convert.ToString(value, 2).PadLeft(32, '0'),
        };
    }

    private static NameSnapshot? ReadName(ProcessMemory memory, nint address, ICollection<string> missing)
    {
        if (address == 0)
        {
            missing.Add("name");
            return null;
        }

        var stringPointer = memory.ReadPointer32(address);
        var value = stringPointer == 0 ? string.Empty : memory.ReadAsciiZ(stringPointer);
        return new NameSnapshot
        {
            PropertyAddress = ProcessMemory.FormatAddress(address),
            StringPointer = ProcessMemory.FormatAddress(stringPointer),
            Value = value,
        };
    }

    private static bool Matches(string candidate, string input) => Normalize(candidate) == Normalize(input);

    private static string Normalize(string value)
    {
        Span<char> buffer = stackalloc char[value.Length];
        var count = 0;
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
                buffer[count++] = char.ToUpperInvariant(ch);
        }

        return new string(buffer[..count]);
    }
}
