using System.Globalization;
using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

public sealed class GuidedActionService(IGuidedActionBackend backend)
{
    [SupportedOSPlatform("windows")]
    public static GuidedActionService Default { get; } = new(new FunctionCallBackend());

    public GuidedActionSnapshot Execute(GuidedActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Session.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions))
        {
            return CreateDormantSnapshot(
                request.ActionKey,
                "Guided action unavailable",
                "Attach to a validated runtime before executing guided game actions."
            );
        }

        if (!GuidedActionCatalog.TryGetDescriptor(request.ActionKey, out var descriptor))
        {
            return CreateDormantSnapshot(
                request.ActionKey,
                "Unknown guided action",
                $"Action '{request.ActionKey}' is not registered in the guided debugger action catalog."
            );
        }

        var function = FunctionCatalog.GetDefinition(descriptor.FunctionKey);
        if (!TryParseTimeout(request.TimeoutMillisecondsText, out var timeout, out var timeoutError))
            return CreateDormantSnapshot(descriptor.Key, "Invalid timeout", timeoutError, descriptor, function);

        try
        {
            return descriptor.Key switch
            {
                "teleport_traveler" => ExecuteTeleport(request, descriptor, function, timeout),
                _ => CreateDormantSnapshot(
                    descriptor.Key,
                    "Unsupported guided action",
                    $"The app does not yet know how to execute '{descriptor.DisplayName}'.",
                    descriptor,
                    function
                ),
            };
        }
        catch (Exception ex)
        {
            return CreateDormantSnapshot(descriptor.Key, "Guided action failed", ex.Message, descriptor, function);
        }
    }

    private GuidedActionSnapshot ExecuteTeleport(
        GuidedActionRequest request,
        GuidedActionDescriptor descriptor,
        FunctionDefinition function,
        TimeSpan timeout
    )
    {
        var travelerHandle = ResolveHandleToken(
            backend,
            request.Session.ProcessId,
            request.TravelerToken,
            "teleport traveler"
        );
        var tileX = ParseInt32(request.TileXText, "Teleport X");
        var tileY = ParseInt32(request.TileYText, "Teleport Y");
        var mapId = string.IsNullOrWhiteSpace(request.MapIdText) ? -1 : ParseInt32(request.MapIdText, "Map id");
        var flags = string.IsNullOrWhiteSpace(request.FlagsText)
            ? 0u
            : ParseUInt32(request.FlagsText, "Teleport flags");
        var execution = backend.ExecuteTeleport(
            request.Session.ProcessId,
            request.Session.RuntimeProfile,
            travelerHandle,
            tileX,
            tileY,
            mapId,
            flags,
            timeout
        );

        return new GuidedActionSnapshot(
            DateTimeOffset.UtcNow,
            IsAvailable: true,
            "Action completed",
            $"Teleported {FormatHandleToken(request.TravelerToken)} to tile ({tileX}, {tileY}) on {(mapId < 0 ? "the current map" : $"map {mapId}")} with flags 0x{flags:X8}.",
            descriptor.Key,
            descriptor.DisplayName,
            function.Key,
            function.Site,
            $"{execution.DispatcherMode} · {execution.DispatcherSite}",
            $"{execution.TargetAddressText} · {function.SuggestedCleanup}",
            $"EAX {FormatUInt32Result(execution.ResultEax)} · EDX {FormatUInt32Result(execution.ResultEdx)}"
        );
    }

    private static GuidedActionSnapshot CreateDormantSnapshot(
        string actionKey,
        string status,
        string summary,
        GuidedActionDescriptor? descriptor = null,
        FunctionDefinition function = default
    ) =>
        new(
            DateTimeOffset.UtcNow,
            IsAvailable: false,
            status,
            summary,
            actionKey,
            descriptor?.DisplayName ?? "Guided action",
            descriptor?.FunctionKey ?? function.Key,
            function.Site,
            "Dispatcher result unavailable.",
            "Target address and cleanup details will appear here after a live action.",
            "EAX and EDX values will appear here after a live action."
        );

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

    private static ulong ResolveHandleToken(IGuidedActionBackend backend, int processId, string? token, string label)
    {
        if (string.IsNullOrWhiteSpace(token) || IsPlayerToken(token))
        {
            var resolution = backend.LocatePlayers(processId);
            if (resolution.AutoResolvedHandle.HasValue)
                return resolution.AutoResolvedHandle.Value;

            throw new InvalidOperationException($"{label}: {resolution.Summary}");
        }

        var trimmed = token.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return ulong.Parse(trimmed[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return ulong.Parse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    private static int ParseInt32(string? token, string label)
    {
        if (!int.TryParse(token?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            throw new InvalidOperationException($"{label} must be a signed 32-bit integer value.");

        return value;
    }

    private static uint ParseUInt32(string token, string label)
    {
        var trimmed = token.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (uint.TryParse(trimmed[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue))
                return hexValue;

            throw new InvalidOperationException($"{label}: '{token}' is not a valid 32-bit hexadecimal value.");
        }

        if (uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return value;

        throw new InvalidOperationException($"{label}: '{token}' is not a valid unsigned 32-bit integer value.");
    }

    private static bool IsPlayerToken(string token)
    {
        var normalized = token.Trim();
        return normalized.Equals("player", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("pc", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("auto", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("self", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("current", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatHandleToken(string? token) =>
        string.IsNullOrWhiteSpace(token) || IsPlayerToken(token) ? "the active player" : token.Trim();

    private static string FormatUInt32Result(uint value) =>
        $"0x{value:X8} ({unchecked((int)value).ToString(CultureInfo.InvariantCulture)})";
}
