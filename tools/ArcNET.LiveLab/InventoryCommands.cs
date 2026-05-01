using System.Runtime.Versioning;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal static class InventoryCommands
{
    public static int RunInventory(string[] args, Func<Func<ProcessMemory, int>, int> runWithProcess)
    {
        if (args.Length == 0)
            throw new InvalidOperationException("Usage: ArcNET.LiveLab inventory <snapshot|resources>");

        var subcommand = args[0].ToLowerInvariant();
        return runWithProcess(memory =>
            subcommand switch
            {
                "snapshot" => RunInventorySnapshot(memory, args[1..]),
                "resources" => RunInventoryResources(memory, args[1..]),
                _ => throw new InvalidOperationException("Usage: ArcNET.LiveLab inventory <snapshot|resources> ..."),
            }
        );
    }

    private static int RunInventoryResources(ProcessMemory memory, string[] args)
    {
        if (args.Length == 0)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab inventory resources set <gold> <arrows> <bullets>"
            );
        }

        var subcommand = args[0].ToLowerInvariant();
        return subcommand switch
        {
            "set" => RunInventoryResourcesSet(memory, args[1..]),
            _ => throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab inventory resources set <gold> <arrows> <bullets>"
            ),
        };
    }

    private static int RunInventoryResourcesSet(ProcessMemory memory, string[] args)
    {
        if (args.Length < 3)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab inventory resources set <gold> <arrows> <bullets>"
            );
        }

        var gold = LiveLabCli.ParseInt32(args[0]);
        var arrows = LiveLabCli.ParseInt32(args[1]);
        var bullets = LiveLabCli.ParseInt32(args[2]);
        var writes = InventoryResourcePatcher.PatchResources(memory, gold, arrows, bullets);

        LiveLabCli.WriteJson(
            new
            {
                Gold = gold,
                Arrows = arrows,
                Bullets = bullets,
                WriteCount = writes.Length,
                Writes = writes,
            }
        );

        return 0;
    }

    private static int RunInventorySnapshot(ProcessMemory memory, string[] args)
    {
        var aggregateRootOverride = LiveLabCli.ParseOptionalAddressArgument(memory, args, "--aggregate-root");
        if (aggregateRootOverride != 0)
        {
            var aggregateSnapshot = InventoryPageCapture.CreateFromAggregateRoot(memory, aggregateRootOverride);
            LiveLabCli.WriteJson(aggregateSnapshot);
            return 0;
        }

        var timeout = LiveLabCli.ParseTimeout(args);
        Console.Error.WriteLine(
            $"[livelab] Installing CE-derived character-sheet hooks. Open the character sheet and click the stats page within {timeout.TotalSeconds:0.#}s to capture the current character for inventory analysis."
        );

        using var hooks = CharacterSheetHookSession.Install(memory);
        var pointers = WaitForCharacterCapture(hooks, timeout);
        if (pointers.Character == 0)
            throw new InvalidOperationException("No character pointer was captured before timeout.");

        var snapshot = InventoryPageCapture.Create(memory, pointers);
        LiveLabCli.WriteJson(snapshot);
        return 0;
    }

    private static CapturedPointers WaitForCharacterCapture(CharacterSheetHookSession hooks, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        var last = hooks.ReadPointers();
        while (DateTime.UtcNow < deadline)
        {
            last = hooks.ReadPointers();
            if (last.Character != 0)
                return last;

            Thread.Sleep(100);
        }

        return last;
    }
}
