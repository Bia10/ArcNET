using System.Globalization;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal static class LiveLabCli
{
    public static nint ParseAddress(ProcessMemory memory, string text)
    {
        var trimmed = text.Trim();
        var plusIndex = trimmed.IndexOf('+');
        if (plusIndex >= 0)
        {
            var moduleName = trimmed[..plusIndex];
            if (
                !moduleName.Equals(ArcanumRuntimeOffsets.ModuleName, StringComparison.OrdinalIgnoreCase)
                && !moduleName.Equals(ArcanumRuntimeOffsets.ProcessName, StringComparison.OrdinalIgnoreCase)
            )
            {
                throw new InvalidOperationException(
                    $"Unsupported module '{moduleName}'. Expected {ArcanumRuntimeOffsets.ModuleName}."
                );
            }

            return memory.ResolveRva(ParseInt32(trimmed[(plusIndex + 1)..]));
        }

        return (nint)(long)ParseUInt32(trimmed);
    }

    public static TimeSpan ParseTimeout(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--timeout-ms", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                    throw new InvalidOperationException("Missing value for --timeout-ms.");

                return TimeSpan.FromMilliseconds(ParseInt32(args[i + 1]));
            }
        }

        return TimeSpan.FromSeconds(15);
    }

    public static nint ParseOptionalAddressArgument(ProcessMemory memory, string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                    throw new InvalidOperationException($"Missing value for {name}.");

                return ParseAddress(memory, args[i + 1]);
            }
        }

        return 0;
    }

    public static int ParseInt32(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return unchecked((int)ParseUInt32(trimmed));

        return int.Parse(trimmed, CultureInfo.InvariantCulture);
    }

    public static uint ParseUInt32(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return uint.Parse(trimmed[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return uint.Parse(trimmed, CultureInfo.InvariantCulture);
    }

    public static void WriteJson<T>(T value)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        Console.WriteLine(JsonSerializer.Serialize(value, options));
    }
}
