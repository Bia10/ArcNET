using System.Runtime.Versioning;
using ArcNET.Diagnostics;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
internal static class NativeInvoker
{
    public static NativeInvocationResult Invoke(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        string functionKey,
        IReadOnlyList<uint> stackArguments,
        TimeSpan timeout
    )
    {
        var function = FunctionCatalog.GetDefinition(functionKey);
        var result = dispatcher.Invoke(
            memory.ToUInt32Address(memory.ResolveRva(function.Rva)),
            function.SuggestedCleanup,
            0,
            0,
            stackArguments,
            timeout
        );

        return new(
            new NativeReadSnapshot(
                function.Key,
                function.Site,
                function.Summary,
                dispatcher.ModeDescription,
                dispatcher.SiteDescription,
                result.State.ToString(),
                unchecked((int)result.ResultEax),
                FormatUInt32Result(result.ResultEax),
                FormatUInt32Result(result.ResultEdx)
            ),
            result.ResultEax,
            result.ResultEdx
        );
    }

    public static string? ReadAsciiPointerResult(
        RuntimeCallDispatcher dispatcher,
        ProcessMemory memory,
        string functionKey,
        uint argument,
        TimeSpan timeout,
        int maxLength = 4096
    )
    {
        var invocation = Invoke(dispatcher, memory, functionKey, [argument], timeout);
        if (invocation.ResultEax == 0)
            return null;

        return memory.ReadAsciiZ((nint)(long)invocation.ResultEax, maxLength);
    }

    private static string FormatUInt32Result(uint value) => $"0x{value:X8} ({unchecked((int)value)})";
}
