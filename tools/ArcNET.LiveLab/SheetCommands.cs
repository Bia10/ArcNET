using System.Runtime.Versioning;
using ArcNET.Editor.Runtime;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal static class SheetCommands
{
    private static readonly string DirectObjectSupportedFieldNames = string.Join(
        ", ",
        CharacterSheetRuntimeLayout
            .MainStatsFields.Concat(CharacterSheetRuntimeLayout.BasicSkillsFields)
            .Select(static descriptor => descriptor.Name)
            .OrderBy(name => name)
    );
    private static readonly string ObjectSupportedFieldNames = string.Join(
        ", ",
        CharacterSheetRuntimeLayout
            .MainStatsFields.Concat(CharacterSheetRuntimeLayout.BasicSkillsFields)
            .Concat(CharacterSheetRuntimeLayout.TechSkillsFields)
            .Concat(CharacterSheetRuntimeLayout.SpellAndTechFields)
            .Select(static descriptor => descriptor.Name)
            .OrderBy(name => name)
    );

    public static int RunSheet(string[] args, Func<Func<ProcessMemory, int>, int> runWithProcess)
    {
        if (args.Length == 0)
            throw new InvalidOperationException("Usage: ArcNET.LiveLab sheet <snapshot|write>");

        var subcommand = args[0].ToLowerInvariant();
        return runWithProcess(memory =>
            subcommand switch
            {
                "snapshot" => RunSheetSnapshot(memory, args[1..]),
                "write" => RunSheetWrite(memory, args[1..]),
                _ => throw new InvalidOperationException("Usage: ArcNET.LiveLab sheet <snapshot|write> ..."),
            }
        );
    }

    private static int RunSheetSnapshot(ProcessMemory memory, string[] args)
    {
        var timeout = LiveLabCli.ParseTimeout(args);
        Console.Error.WriteLine(
            $"[livelab] Installing CE-derived character-sheet hooks. Open the character sheet and click the stats / skills / tech / spells pages within {timeout.TotalSeconds:0.#}s."
        );

        using var hooks = CharacterSheetHookSession.Install(memory);
        var pointers = hooks.WaitForCapture(timeout);
        if (!pointers.HasAnyCapture)
            throw new InvalidOperationException("No character-sheet pointers were captured before timeout.");

        var snapshot = CharacterSheetCapture.Create(memory, pointers);
        LiveLabCli.WriteJson(snapshot);
        return 0;
    }

    private static int RunSheetWrite(ProcessMemory memory, string[] args)
    {
        var parsed = ParseWriteArguments(memory, args);
        if (parsed.FieldName.Length < 1)
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab sheet write <field> <value> [--timeout-ms <ms>] [--mode <ui|object>] [--character <address|Arcanum.exe+rva>]"
            );

        var fieldName = parsed.FieldName;
        var value = parsed.Value;
        var timeout = parsed.Timeout;
        var mode = parsed.Mode;
        var characterAddress = parsed.CharacterAddress;

        if (mode == "object" && characterAddress != 0)
        {
            if (
                CharacterSheetCapture.TryResolveObjectIntField(memory, characterAddress, fieldName, out var directField)
            )
            {
                WriteField(memory, directField, value, "object");
                return 0;
            }

            Console.Error.WriteLine(
                "[livelab] Direct character-object resolve only covers main stats + basic skills. Capturing active substructures for full precision."
            );
        }

        if (mode == "object")
        {
            Console.Error.WriteLine(
                $"[livelab] Installing CE-derived character-sheet hooks. Open the character sheet to capture the active character within {timeout.TotalSeconds:0.#}s."
            );
        }
        else
        {
            Console.Error.WriteLine(
                $"[livelab] Installing CE-derived character-sheet hooks. Open the relevant character-sheet page for '{fieldName}' within {timeout.TotalSeconds:0.#}s."
            );
        }

        using var hooks = CharacterSheetHookSession.Install(memory);
        var pointers = hooks.WaitForCapture(timeout);

        if (mode == "object")
        {
            var effectiveCharacter = characterAddress == 0 ? pointers.Character : characterAddress;
            if (
                effectiveCharacter == 0
                || !CharacterSheetCapture.TryResolveObjectIntField(
                    memory,
                    effectiveCharacter,
                    fieldName,
                    pointers.TechSkills,
                    pointers.SpellAndTech,
                    out var objectField
                )
            )
            {
                throw new InvalidOperationException(
                    characterAddress == 0
                        ? $"Unable to resolve field '{fieldName}' in object mode. Open the character sheet to capture all substructures, and use one of: {ObjectSupportedFieldNames}."
                        : $"Unable to resolve field '{fieldName}' in object mode. Direct character-object writes currently support only main stats/basic skills ({DirectObjectSupportedFieldNames}). For tech/spell fields, open the character sheet to capture substructures and use one of: {ObjectSupportedFieldNames}."
                );
            }

            WriteField(memory, objectField, value, "object");
            return 0;
        }

        if (!CharacterSheetCapture.TryResolveIntField(memory, pointers, fieldName, out var field))
        {
            throw new InvalidOperationException(
                $"Unable to resolve field '{fieldName}' in ui mode. Open the matching character-sheet page and try again."
            );
        }

        WriteField(memory, field, value, "ui");
        return 0;
    }

    private static (
        string FieldName,
        int Value,
        TimeSpan Timeout,
        string Mode,
        nint CharacterAddress
    ) ParseWriteArguments(ProcessMemory memory, string[] args)
    {
        var positional = new List<string>();
        var mode = "ui";
        var timeout = TimeSpan.FromSeconds(15);
        nint characterAddress = 0;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--timeout-ms", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                    throw new InvalidOperationException("Missing value for --timeout-ms.");

                timeout = TimeSpan.FromMilliseconds(LiveLabCli.ParseInt32(args[i + 1]));
                i++;
                continue;
            }

            if (args[i].Equals("--mode", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                    throw new InvalidOperationException("Missing value for --mode.");

                var value = args[i + 1];
                if (
                    !value.Equals("ui", StringComparison.OrdinalIgnoreCase)
                    && !value.Equals("object", StringComparison.OrdinalIgnoreCase)
                )
                {
                    throw new InvalidOperationException("Invalid mode. Supported values are: ui, object.");
                }

                mode = value.ToLowerInvariant();
                i++;
                continue;
            }

            if (args[i].Equals("--character", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                    throw new InvalidOperationException("Missing value for --character.");

                characterAddress = LiveLabCli.ParseAddress(memory, args[i + 1]);
                i++;
                continue;
            }

            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Unknown option '{args[i]}'.");
            }

            positional.Add(args[i]);
        }

        if (positional.Count < 2)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab sheet write <field> <value> [--timeout-ms <ms>] [--mode <ui|object>] [--character <address|Arcanum.exe+rva>]"
            );
        }

        return (positional[0], LiveLabCli.ParseInt32(positional[1]), timeout, mode, characterAddress);
    }

    private static void WriteField(ProcessMemory memory, ResolvedIntField field, int value, string mode)
    {
        var before = field.Value;
        memory.WriteInt32(field.Address, value);
        var after = memory.ReadInt32(field.Address);

        LiveLabCli.WriteJson(
            new
            {
                Field = field.Name,
                Mode = mode,
                Address = ProcessMemory.FormatAddress(field.Address),
                Before = before,
                After = after,
            }
        );
    }
}
