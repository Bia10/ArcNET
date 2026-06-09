using System.Collections.Concurrent;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public static class CatalogAddressResolver
{
    public const string ForceSignatureFallbackEnvironmentVariable = "ARCNET_DIAGNOSTICS_FORCE_CATALOG_SIGNATURES";
    public const string LegacyForceSignatureFallbackEnvironmentVariable = "ARCNET_LIVELAB_FORCE_CATALOG_SIGNATURES";

    public static bool ShouldForceSignatureFallback()
    {
        var value =
            Environment.GetEnvironmentVariable(ForceSignatureFallbackEnvironmentVariable)
            ?? Environment.GetEnvironmentVariable(LegacyForceSignatureFallbackEnvironmentVariable);
        return value is not null
            && (
                value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("on", StringComparison.OrdinalIgnoreCase)
            );
    }

    public static CatalogAddressResolution Resolve(CatalogAddressResolveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Key);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Operation);

        var normalizedKey = NormalizeKey(request.Key);
        if (
            !request.ForceSignatureFallback
            && request.RuntimeProfile.SupportsCatalogRvas
            && request.PreferredRva > 0
            && request.PreferredRva < request.ModuleSize
        )
        {
            return new CatalogAddressResolution(unchecked((uint)request.PreferredRva), "catalog-rva");
        }

        if (!request.SignaturesByNormalizedKey.TryGetValue(normalizedKey, out var signaturePattern))
        {
            var reason = request.ForceSignatureFallback
                ? $"Forced signature resolution is enabled, but no signature fallback is registered for '{request.Key}'."
                : $"No signature fallback is registered for '{request.Key}'.";
            throw new InvalidOperationException(
                $"{request.Operation} could not resolve '{request.Key}' inside {request.ModuleFileName}. {reason}"
            );
        }

        var cacheKey = $"{request.AddressCacheKey}:{normalizedKey}";
        if (s_addressCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var pattern = BytePattern.Parse(signaturePattern);
        var matches = pattern.FindMatches(request.ModuleBytes);
        if (matches.Length == 0)
        {
            throw new InvalidOperationException(
                $"{request.Operation} could not resolve '{request.Key}' inside {request.ModuleFileName}. "
                    + $"Signature fallback '{pattern.NormalizedText}' did not match."
            );
        }

        if (matches.Length > 1)
        {
            var sample = string.Join(
                ", ",
                matches
                    .Take(5)
                    .Select(index =>
                        ModuleAddressFormatter.FormatModuleAddress(request.ModuleFileName, unchecked((uint)index))
                    )
            );
            throw new InvalidOperationException(
                $"{request.Operation} could not resolve '{request.Key}' inside {request.ModuleFileName}. "
                    + $"Signature fallback '{pattern.NormalizedText}' matched {matches.Length} location(s): {sample}."
            );
        }

        var resolved = new CatalogAddressResolution(unchecked((uint)matches[0]), "catalog-signature");
        s_addressCache[cacheKey] = resolved;
        return resolved;
    }

    public static string NormalizeKey(string value)
    {
        Span<char> buffer = stackalloc char[value.Length];
        var count = 0;
        foreach (var ch in value)
        {
            if (!char.IsLetterOrDigit(ch))
                continue;

            buffer[count++] = char.ToLowerInvariant(ch);
        }

        return new string(buffer[..count]);
    }

    private static readonly ConcurrentDictionary<string, CatalogAddressResolution> s_addressCache = new(
        StringComparer.OrdinalIgnoreCase
    );
}
