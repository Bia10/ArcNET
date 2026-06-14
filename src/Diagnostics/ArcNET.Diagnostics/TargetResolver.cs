using System.Globalization;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public static class TargetResolver
{
    public static ResolvedTarget Resolve(
        IHandleBackend backend,
        AttachedSessionSnapshot session,
        string token,
        string subject
    )
    {
        var usedAutoToken = IsPlayerToken(token);
        var resolution = usedAutoToken ? backend.LocatePlayers(session.ProcessId) : default(LivePlayerLocatorResult);
        var handle = ResolveHandleToken(token, subject, resolution);
        var identity = backend.InspectHandle(session.ProcessId, handle);
        List<string> notes = [];
        if (usedAutoToken && !string.IsNullOrWhiteSpace(resolution.Summary))
            notes.Add(resolution.Summary);

        if (!identity.HasHeader)
            notes.Add(
                $"Target inspection resolved through {identity.ResolutionSource} without a decoded object header."
            );

        return new(
            handle,
            string.IsNullOrWhiteSpace(identity.HandleHex)
                ? RuntimeSemanticCatalog.FormatHandle(handle)
                : identity.HandleHex,
            FormatTargetText(handle, identity),
            [.. notes]
        );
    }

    public static bool IsPlayerToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return true;

        return token.Trim() switch
        {
            var candidate when candidate.Equals("player", StringComparison.OrdinalIgnoreCase) => true,
            var candidate when candidate.Equals("pc", StringComparison.OrdinalIgnoreCase) => true,
            var candidate when candidate.Equals("auto", StringComparison.OrdinalIgnoreCase) => true,
            var candidate when candidate.Equals("self", StringComparison.OrdinalIgnoreCase) => true,
            var candidate when candidate.Equals("current", StringComparison.OrdinalIgnoreCase) => true,
            _ => false,
        };
    }

    public static ulong ParseUInt64(string token)
    {
        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return Convert.ToUInt64(token[2..], 16);

        return ulong.Parse(token, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    public static uint ToLow32(ulong value) => unchecked((uint)(value & uint.MaxValue));

    public static uint ToHigh32(ulong value) => unchecked((uint)(value >> 32));

    private static ulong ResolveHandleToken(string token, string subject, LivePlayerLocatorResult resolution)
    {
        if (string.IsNullOrWhiteSpace(token) || IsPlayerToken(token))
        {
            if (resolution.AutoResolvedHandle.HasValue)
                return resolution.AutoResolvedHandle.Value;

            throw new InvalidOperationException($"{subject}: {resolution.Summary}");
        }

        return ParseUInt64(token);
    }

    private static string FormatTargetText(ulong handle, LiveObjectIdentity identity)
    {
        if (!identity.HasHeader)
            return RuntimeSemanticCatalog.FormatHandle(handle);

        var header = identity.Header!.Value;
        var objectType = string.IsNullOrWhiteSpace(header.ObjectTypeName) ? "Object" : header.ObjectTypeName;
        var objectId = string.IsNullOrWhiteSpace(header.ObjectId.Label) ? identity.HandleHex : header.ObjectId.Label;
        return string.IsNullOrWhiteSpace(header.PrototypeId.Label)
            ? $"{objectType} {objectId}"
            : $"{objectType} {objectId} from {header.PrototypeId.Label}";
    }
}
