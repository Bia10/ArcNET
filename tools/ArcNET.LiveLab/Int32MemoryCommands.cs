using System.Runtime.Versioning;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal static class Int32MemoryCommands
{
    public static int RunReadInt32(ProcessMemory memory, string[] args)
    {
        if (args.Length < 1)
            throw new InvalidOperationException("Usage: ArcNET.LiveLab read-int32 <address|module+rva>");

        var address = LiveLabCli.ParseAddress(memory, args[0]);
        var value = memory.ReadInt32(address);
        Console.WriteLine($"{ProcessMemory.FormatAddress(address)} = {value} (0x{unchecked((uint)value):X8})");
        return 0;
    }

    public static int RunDumpInt32s(ProcessMemory memory, string[] args)
    {
        if (args.Length < 2)
            throw new InvalidOperationException("Usage: ArcNET.LiveLab dump-int32s <address|module+rva> <count>");

        var start = LiveLabCli.ParseAddress(memory, args[0]);
        var count = LiveLabCli.ParseInt32(args[1]);
        if (count < 1)
            throw new InvalidOperationException("count must be at least 1.");

        var values = Enumerable
            .Range(0, count)
            .Select(index =>
            {
                var offset = index * sizeof(int);
                var address = start + offset;
                var value = memory.ReadInt32(address);
                return new
                {
                    Index = index,
                    Offset = offset,
                    Address = ProcessMemory.FormatAddress(address),
                    Value = value,
                    Hex = $"0x{unchecked((uint)value):X8}",
                };
            })
            .ToArray();

        LiveLabCli.WriteJson(
            new
            {
                Start = ProcessMemory.FormatAddress(start),
                Count = count,
                Values = values,
            }
        );
        return 0;
    }

    public static int RunWriteInt32(ProcessMemory memory, string[] args)
    {
        if (args.Length < 2)
            throw new InvalidOperationException("Usage: ArcNET.LiveLab write-int32 <address|module+rva> <value>");

        var address = LiveLabCli.ParseAddress(memory, args[0]);
        var value = LiveLabCli.ParseInt32(args[1]);
        var before = memory.ReadInt32(address);
        memory.WriteInt32(address, value);
        var after = memory.ReadInt32(address);
        Console.WriteLine($"{ProcessMemory.FormatAddress(address)}: {before} -> {after}");
        return 0;
    }

    public static int RunWriteInt32Many(ProcessMemory memory, string[] args)
    {
        if (args.Length < 2)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab write-int32-many <value> <address|module+rva> [address|module+rva] ..."
            );
        }

        var value = LiveLabCli.ParseInt32(args[0]);
        var writes = args[1..]
            .Select(text => LiveLabCli.ParseAddress(memory, text))
            .Select(address =>
            {
                var before = memory.ReadInt32(address);
                memory.WriteInt32(address, value);
                var after = memory.ReadInt32(address);
                return new
                {
                    Address = ProcessMemory.FormatAddress(address),
                    Before = before,
                    After = after,
                };
            })
            .ToArray();

        LiveLabCli.WriteJson(new { Value = value, Writes = writes });
        return 0;
    }
}
