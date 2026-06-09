using System.Globalization;
using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

public sealed class FunctionCallService(IFunctionCallBackend backend)
{
    [SupportedOSPlatform("windows")]
    public static FunctionCallService Default { get; } = new(new FunctionCallBackend());

    public FunctionCallSnapshot Invoke(FunctionCallRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Session.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions))
        {
            return CreateDormantSnapshot(
                "Function call unavailable",
                "This session does not currently expose live function-invocation capability, so native calls stay disabled."
            );
        }

        if (!TryResolveTarget(request.TargetText, out var target, out var targetError))
            return CreateDormantSnapshot("Unknown function target", targetError);

        if (!TryParseTimeout(request.TimeoutMillisecondsText, out var timeout, out var timeoutError))
            return CreateDormantSnapshot("Invalid timeout", timeoutError);

        try
        {
            var parser = new InvocationArgumentParser(backend, request.Session.ProcessId);
            var ecxValue = parser.ParseScalar(request.EcxValueText, "ECX");
            var edxValue = parser.ParseScalar(request.EdxValueText, "EDX");
            var stackArguments = parser.ParseStackArguments(request.StackArgumentsText);
            var cleanupMode = request.UseSuggestedCleanup
                ? target.SuggestedCleanupMode ?? request.OverrideCleanupMode
                : request.OverrideCleanupMode;
            var execution = backend.InvokeCall(
                request.Session.ProcessId,
                target.Rva,
                request.Session.RuntimeProfile,
                cleanupMode,
                ecxValue,
                edxValue,
                [.. stackArguments.Select(static argument => argument.Value)],
                timeout
            );

            return new FunctionCallSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                "Call completed",
                CreateSuccessSummary(target, execution, stackArguments.Count, cleanupMode, request.UseSuggestedCleanup),
                target.Key,
                target.Site,
                CreateCleanupText(cleanupMode, request.UseSuggestedCleanup, target.SuggestedCleanupMode),
                $"{execution.DispatcherMode} · {execution.DispatcherSite}",
                execution.TargetAddressText,
                FormatUInt32Result(execution.ResultEax),
                FormatUInt32Result(execution.ResultEdx),
                [.. stackArguments.Select(CreateArgumentSnapshot)]
            );
        }
        catch (Exception ex)
        {
            return CreateDormantSnapshot("Function call failed", ex.Message, target.Key, target.Site);
        }
    }

    private static FunctionCallSnapshot CreateDormantSnapshot(
        string status,
        string summary,
        string targetKey = "",
        string targetSite = ""
    ) =>
        new(
            DateTimeOffset.UtcNow,
            IsAvailable: false,
            status,
            summary,
            targetKey,
            targetSite,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            []
        );

    private static bool TryResolveTarget(string? targetText, out ResolvedTarget target, out string error)
    {
        if (string.IsNullOrWhiteSpace(targetText))
        {
            target = default;
            error = "Choose one known function or enter a raw module RVA before invoking a live call.";
            return false;
        }

        var trimmed = targetText.Trim();
        if (FunctionCatalog.TryGetDefinition(trimmed, out var function))
        {
            target = new ResolvedTarget(
                function.Key,
                function.Site,
                function.Rva,
                function.SuggestedCleanup,
                function.Example
            );
            error = string.Empty;
            return true;
        }

        if (TryParseRva(trimmed, out var rva))
        {
            target = new ResolvedTarget(
                $"raw_rva_0x{rva:X8}",
                CodeCatalog.FormatModuleAddress(unchecked((uint)rva)),
                rva,
                SuggestedCleanupMode: null,
                Example: null
            );
            error = string.Empty;
            return true;
        }

        target = default;
        error =
            $"Unknown function target '{trimmed}'. Use a known function key like 'ui_start_dialog' or a raw RVA like '0x000609E0'.";
        return false;
    }

    private static bool TryParseRva(string token, out int rva)
    {
        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (uint.TryParse(token[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
            {
                rva = unchecked((int)hex);
                return true;
            }

            rva = 0;
            return false;
        }

        if (token.StartsWith("rva:", StringComparison.OrdinalIgnoreCase))
            return TryParseRva(token[4..], out rva);

        if (
            int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var decimalRva)
            && decimalRva >= 0
        )
        {
            rva = decimalRva;
            return true;
        }

        rva = 0;
        return false;
    }

    private static bool TryParseTimeout(string? timeoutText, out TimeSpan timeout, out string error)
    {
        if (string.IsNullOrWhiteSpace(timeoutText))
        {
            timeout = TimeSpan.FromSeconds(1);
            error = string.Empty;
            return true;
        }

        if (
            int.TryParse(timeoutText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds)
            && milliseconds > 0
        )
        {
            timeout = TimeSpan.FromMilliseconds(milliseconds);
            error = string.Empty;
            return true;
        }

        timeout = default;
        error = $"Timeout '{timeoutText}' is not a valid positive millisecond value.";
        return false;
    }

    private static string CreateSuccessSummary(
        ResolvedTarget target,
        FunctionCallExecutionResult execution,
        int argumentCount,
        StackCleanupMode cleanupMode,
        bool usedSuggestedCleanup
    )
    {
        var cleanupSource =
            usedSuggestedCleanup && target.SuggestedCleanupMode.HasValue ? "suggested cleanup" : "manual cleanup";
        return $"Called {target.Key} with {argumentCount} stack dword(s) using {cleanupMode} ({cleanupSource}). Dispatcher completed in {execution.DispatcherMode} mode.";
    }

    private static string CreateCleanupText(
        StackCleanupMode cleanupMode,
        bool usedSuggestedCleanup,
        StackCleanupMode? suggestedCleanupMode
    )
    {
        var source = usedSuggestedCleanup && suggestedCleanupMode.HasValue ? "Suggested" : "Manual";
        return $"{source} {cleanupMode}";
    }

    private static FunctionCallArgumentSnapshot CreateArgumentSnapshot(ParsedArgument argument) =>
        new(argument.Index, FormatUInt32Result(argument.Value), argument.SourceText);

    private static string FormatUInt32Result(uint value) =>
        $"0x{value:X8} ({unchecked((int)value).ToString(CultureInfo.InvariantCulture)})";

    private readonly record struct ResolvedTarget(
        string Key,
        string Site,
        int Rva,
        StackCleanupMode? SuggestedCleanupMode,
        string? Example
    );

    private readonly record struct ParsedArgument(int Index, uint Value, string SourceText);

    private sealed class InvocationArgumentParser(IFunctionCallBackend backend, int processId)
    {
        private readonly Lazy<LivePlayerLocatorResult> _playerResolution = new(() => backend.LocatePlayers(processId));

        public uint ParseScalar(string? token, string label)
        {
            if (string.IsNullOrWhiteSpace(token))
                return 0;

            var trimmed = token.Trim();
            if (TryParseMacro(trimmed, "handle_low", out var lowHandleToken))
                return ToLow32(ResolveHandle(lowHandleToken, label));

            if (TryParseMacro(trimmed, "handle_high", out var highHandleToken))
                return ToHigh32(ResolveHandle(highHandleToken, label));

            if (TryParseMacro(trimmed, "handle_hi", out var hiHandleToken))
                return ToHigh32(ResolveHandle(hiHandleToken, label));

            if (TryParseMacro(trimmed, "handle", out _))
            {
                throw new InvalidOperationException(
                    $"{label} expects one 32-bit value. Use handle_low(...) or handle_high(...) for split object-handle arguments."
                );
            }

            return ParseUInt32(trimmed, label);
        }

        public IReadOnlyList<ParsedArgument> ParseStackArguments(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return [];

            List<ParsedArgument> arguments = [];
            var tokens = text.Split(
                [',', ';', '\r', '\n', '\t', ' '],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
            foreach (var token in tokens)
            {
                if (TryParseMacro(token, "handle", out var handleToken))
                {
                    var handle = ResolveHandle(handleToken, "stack argument");
                    arguments.Add(new(arguments.Count, ToLow32(handle), $"{token} [low]"));
                    arguments.Add(new(arguments.Count, ToHigh32(handle), $"{token} [high]"));
                    continue;
                }

                arguments.Add(new(arguments.Count, ParseScalar(token, $"stack argument {arguments.Count}"), token));
            }

            return arguments;
        }

        private ulong ResolveHandle(string token, string label)
        {
            var trimmed = token.Trim();
            if (IsPlayerToken(trimmed))
            {
                var resolution = _playerResolution.Value;
                if (resolution.AutoResolvedHandle.HasValue)
                    return resolution.AutoResolvedHandle.Value;

                throw new InvalidOperationException($"{label}: {resolution.Summary}");
            }

            if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return ulong.Parse(trimmed[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            return ulong.Parse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        private static uint ParseUInt32(string token, string label)
        {
            if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (uint.TryParse(token[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue))
                    return hexValue;

                throw new InvalidOperationException($"{label}: '{token}' is not a valid 32-bit hexadecimal value.");
            }

            if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var signedValue))
            {
                if (signedValue is >= int.MinValue and <= int.MaxValue)
                    return unchecked((uint)(int)signedValue);

                if (signedValue >= 0 && signedValue <= uint.MaxValue)
                    return (uint)signedValue;
            }

            throw new InvalidOperationException($"{label}: '{token}' is not a valid 32-bit integer value.");
        }

        private static bool TryParseMacro(string token, string macroName, out string innerToken)
        {
            var prefix = $"{macroName}(";
            if (token.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && token.EndsWith(')'))
            {
                innerToken = token[prefix.Length..^1].Trim();
                return innerToken.Length > 0;
            }

            innerToken = string.Empty;
            return false;
        }

        private static bool IsPlayerToken(string token) =>
            token.Equals("player", StringComparison.OrdinalIgnoreCase)
            || token.Equals("pc", StringComparison.OrdinalIgnoreCase)
            || token.Equals("auto", StringComparison.OrdinalIgnoreCase)
            || token.Equals("self", StringComparison.OrdinalIgnoreCase)
            || token.Equals("current", StringComparison.OrdinalIgnoreCase);

        private static uint ToLow32(ulong value) => unchecked((uint)(value & uint.MaxValue));

        private static uint ToHigh32(ulong value) => unchecked((uint)(value >> 32));
    }
}
