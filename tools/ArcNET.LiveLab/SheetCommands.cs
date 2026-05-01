using System.Runtime.Versioning;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal static class SheetCommands
{
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
        if (args.Length < 2)
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab sheet write <field> <value> [--timeout-ms <ms>]"
            );

        var fieldName = args[0];
        var value = LiveLabCli.ParseInt32(args[1]);
        var timeout = LiveLabCli.ParseTimeout(args[2..]);

        Console.Error.WriteLine(
            $"[livelab] Installing CE-derived character-sheet hooks. Open the relevant character-sheet page for '{fieldName}' within {timeout.TotalSeconds:0.#}s."
        );

        using var hooks = CharacterSheetHookSession.Install(memory);
        var pointers = hooks.WaitForCapture(timeout);
        if (!CharacterSheetCapture.TryResolveIntField(memory, pointers, fieldName, out var field))
        {
            throw new InvalidOperationException(
                $"Unable to resolve field '{fieldName}'. Open the matching character-sheet page and try again."
            );
        }

        var before = field.Value;
        memory.WriteInt32(field.Address, value);
        var after = memory.ReadInt32(field.Address);

        LiveLabCli.WriteJson(
            new
            {
                Field = field.Name,
                Address = ProcessMemory.FormatAddress(field.Address),
                Before = before,
                After = after,
            }
        );

        return 0;
    }
}
