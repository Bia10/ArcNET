using System.Runtime.Versioning;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal static class ProcessStatusCommands
{
    public static int RunStatus(ProcessMemory memory)
    {
        Console.WriteLine($"Process: {memory.ProcessName} ({memory.ProcessId})");
        Console.WriteLine($"Module:  {ProcessMemory.FormatAddress(memory.ModuleBase)}  {memory.ModulePath}");
        Console.WriteLine(
            $"OpenedCharacterId: 0x{memory.ReadUInt32(memory.ResolveRva(ArcanumRuntimeOffsets.CurrentCharacterSheetIdRva)):X8}"
        );
        Console.WriteLine(
            $"ActionPoints: {memory.ReadInt32(memory.ResolveRva(ArcanumRuntimeOffsets.ActionPointsRva))}"
        );
        return 0;
    }

    public static int RunActionPoints(ProcessMemory memory, string[] args)
    {
        var address = memory.ResolveRva(ArcanumRuntimeOffsets.ActionPointsRva);
        if (args.Length == 0 || args[0].Equals("get", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"ActionPoints: {memory.ReadInt32(address)}");
            return 0;
        }

        if (args[0].Equals("set", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 2)
                throw new InvalidOperationException("Usage: ArcNET.LiveLab ap set <value>");

            var value = LiveLabCli.ParseInt32(args[1]);
            var before = memory.ReadInt32(address);
            memory.WriteInt32(address, value);
            var after = memory.ReadInt32(address);
            Console.WriteLine($"ActionPoints: {before} -> {after}");
            return 0;
        }

        throw new InvalidOperationException("Usage: ArcNET.LiveLab ap [get|set <value>]");
    }
}
