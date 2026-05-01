using System.Runtime.Versioning;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("ArcNET.LiveLab is Windows-only.");
            return 1;
        }

        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage();
            return 0;
        }

        try
        {
            return await RunAsync(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static async Task<int> RunAsync(string[] args)
    {
        var command = args[0].ToLowerInvariant();
        return command switch
        {
            "status" => RunWithProcess(ProcessStatusCommands.RunStatus),
            "ap" => RunWithProcess(memory => ProcessStatusCommands.RunActionPoints(memory, args[1..])),
            "read-int32" => RunWithProcess(memory => Int32MemoryCommands.RunReadInt32(memory, args[1..])),
            "dump-int32s" => RunWithProcess(memory => Int32MemoryCommands.RunDumpInt32s(memory, args[1..])),
            "dump-pointer-graph" => RuntimeInspectionCommands.RunDumpPointerGraph(args[1..], RunWithProcess),
            "write-int32" => RunWithProcess(memory => Int32MemoryCommands.RunWriteInt32(memory, args[1..])),
            "write-int32-many" => RunWithProcess(memory => Int32MemoryCommands.RunWriteInt32Many(memory, args[1..])),
            "find-int32-sequence" => RuntimeInspectionCommands.RunFindInt32Sequence(args[1..], RunWithProcess),
            "find-int32-sequence-global" => RuntimeInspectionCommands.RunFindInt32SequenceGlobal(
                args[1..],
                RunWithProcess
            ),
            "scan-int32-records" => RuntimeInspectionCommands.RunScanInt32Records(args[1..], RunWithProcess),
            "patch-int32-record-field" => RuntimeInspectionCommands.RunPatchInt32RecordField(args[1..], RunWithProcess),
            "inventory" => InventoryCommands.RunInventory(args[1..], RunWithProcess),
            "sheet" => SheetCommands.RunSheet(args[1..], RunWithProcess),
            _ => throw new InvalidOperationException($"Unknown command '{args[0]}'. Use 'help' for usage."),
        };
    }

    private static int RunWithProcess(Func<ProcessMemory, int> action)
    {
        using var memory = ProcessMemory.Attach(ArcanumRuntimeOffsets.ProcessName);
        return action(memory);
    }

    private static bool IsHelp(string arg) => arg is "help" or "--help" or "-h" or "/?";

    private static void PrintUsage()
    {
        Console.WriteLine("ArcNET.LiveLab - live Arcanum runtime research harness");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  ArcNET.LiveLab status");
        Console.WriteLine("  ArcNET.LiveLab ap [get|set <value>]");
        Console.WriteLine("  ArcNET.LiveLab read-int32 <address|Arcanum.exe+rva>");
        Console.WriteLine("  ArcNET.LiveLab dump-int32s <address|Arcanum.exe+rva> <count>");
        Console.WriteLine("  ArcNET.LiveLab dump-pointer-graph <address|Arcanum.exe+rva> <depth> [int-count]");
        Console.WriteLine("  ArcNET.LiveLab write-int32 <address|Arcanum.exe+rva> <value>");
        Console.WriteLine(
            "  ArcNET.LiveLab write-int32-many <value> <address|Arcanum.exe+rva> [address|Arcanum.exe+rva] ..."
        );
        Console.WriteLine(
            "  ArcNET.LiveLab find-int32-sequence <start-address|Arcanum.exe+rva> <byte-count> <value1> [value2] ..."
        );
        Console.WriteLine("  ArcNET.LiveLab find-int32-sequence-global <value1> [value2] ...");
        Console.WriteLine(
            "  ArcNET.LiveLab scan-int32-records <start-address|Arcanum.exe+rva> <byte-count> <record-int-count> <header1> [header2] ..."
        );
        Console.WriteLine(
            "  ArcNET.LiveLab patch-int32-record-field <start-address|Arcanum.exe+rva> <byte-count> <record-int-count> <field-index> <expected-value> <new-value> <header1> [header2] ..."
        );
        Console.WriteLine(
            "  ArcNET.LiveLab inventory snapshot [--timeout-ms <ms>] [--aggregate-root <address|Arcanum.exe+rva>]"
        );
        Console.WriteLine("  ArcNET.LiveLab inventory resources set <gold> <arrows> <bullets>");
        Console.WriteLine("  ArcNET.LiveLab sheet snapshot [--timeout-ms <ms>]");
        Console.WriteLine("  ArcNET.LiveLab sheet write <field> <value> [--timeout-ms <ms>]");
        Console.WriteLine();
        Console.WriteLine("Notes:");
        Console.WriteLine("  - sheet commands install ephemeral CE-derived hooks and remove them before exit.");
        Console.WriteLine(
            "  - sheet snapshot/write expects the Arcanum character sheet to be open while the tool is waiting."
        );
        Console.WriteLine(
            "  - field names are case-insensitive and ignore punctuation, e.g. Strength, hp-loss, GunSmithy."
        );
    }
}
