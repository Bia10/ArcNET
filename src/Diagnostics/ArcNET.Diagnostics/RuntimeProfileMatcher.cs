using System.Collections.Concurrent;
using System.Security.Cryptography;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public static class RuntimeProfileMatcher
{
    public static RuntimeProfileSnapshot Match(
        RuntimeFingerprint fingerprint,
        string? moduleSha256,
        string? hashError = null
    )
    {
        ArgumentNullException.ThrowIfNull(fingerprint);

        var runtimeKind =
            fingerprint.RuntimeKind != RuntimeKind.Unknown
                ? fingerprint.RuntimeKind
                : ClassifyRuntimeKind(fingerprint.ModuleFileName, fingerprint.ProcessName);
        foreach (var profile in s_profiles)
        {
            if (!profile.IsMatch(fingerprint, moduleSha256))
                continue;

            return new RuntimeProfileSnapshot(
                profile.Id,
                profile.DisplayName,
                runtimeKind,
                RuntimeSupportLevel.Validated,
                SupportsCatalogRvas: true,
                profile.Notes,
                moduleSha256,
                hashError
            );
        }

        return runtimeKind switch
        {
            RuntimeKind.CommunityEdition => new RuntimeProfileSnapshot(
                Id: null,
                DisplayName: "Community Edition executable (unvalidated profile)",
                RuntimeKind: runtimeKind,
                SupportLevel: RuntimeSupportLevel.Exploratory,
                SupportsCatalogRvas: false,
                Notes: moduleSha256 is null
                    ? "The module hash could not be computed, so fixed-RVA debugger features should remain disabled."
                    : "No validated Community Edition profile is registered yet, so fixed-RVA debugger features should remain disabled unless a safer symbol-backed workflow is available.",
                ModuleSha256: moduleSha256,
                HashError: hashError
            ),
            RuntimeKind.Classic => new RuntimeProfileSnapshot(
                Id: null,
                DisplayName: "Classic executable (unvalidated profile)",
                RuntimeKind: runtimeKind,
                SupportLevel: RuntimeSupportLevel.Exploratory,
                SupportsCatalogRvas: false,
                Notes: moduleSha256 is null
                    ? "The module hash could not be computed, so fixed-RVA debugger features should remain disabled."
                    : "This executable does not match any validated runtime profile, so fixed-RVA debugger features should remain disabled.",
                ModuleSha256: moduleSha256,
                HashError: hashError
            ),
            _ => new RuntimeProfileSnapshot(
                Id: null,
                DisplayName: "Unsupported executable",
                RuntimeKind: RuntimeKind.Unknown,
                SupportLevel: RuntimeSupportLevel.Unsupported,
                SupportsCatalogRvas: false,
                Notes: "The attached process is not a recognized Arcanum runtime.",
                ModuleSha256: moduleSha256,
                HashError: hashError
            ),
        };
    }

    public static bool TryComputeModuleSha256(string modulePath, out string? moduleSha256, out string? error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modulePath);

        if (s_hashCache.TryGetValue(modulePath, out var cachedHash))
        {
            moduleSha256 = cachedHash;
            error = null;
            return true;
        }

        try
        {
            using var stream = File.OpenRead(modulePath);
            moduleSha256 = Convert.ToHexString(SHA256.HashData(stream));
            s_hashCache[modulePath] = moduleSha256;
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            moduleSha256 = null;
            error = ex.Message;
            return false;
        }
    }

    public static RuntimeKind ClassifyRuntimeKind(string moduleFileName, string processName)
    {
        var normalizedModuleName = NormalizeModuleOrProcessName(moduleFileName);
        var normalizedProcessName = NormalizeModuleOrProcessName(processName);
        return (normalizedModuleName, normalizedProcessName) switch
        {
            ("arcanum", _) or (_, "arcanum") => RuntimeKind.Classic,
            ("arcanum-ce", _) or (_, "arcanum-ce") => RuntimeKind.CommunityEdition,
            _ => RuntimeKind.Unknown,
        };
    }

    private static string NormalizeModuleOrProcessName(string value) =>
        Path.GetFileNameWithoutExtension(value.Trim()).ToLowerInvariant();

    private static readonly ConcurrentDictionary<string, string> s_hashCache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly RuntimeProfileDefinition[] s_profiles =
    [
        new(
            "classic-uap-2021-05-28",
            "Classic/UAP Arcanum.exe (validated 2021-05-28 build)",
            RuntimeKind.Classic,
            "Arcanum.exe",
            3_538_944,
            2_048_000,
            "D7A16B8C29141E6E834ED2647506059BC482F7AE63EB2CB5E6F1761358FD038F",
            "This build matches the validated fixed-RVA table currently used by diagnostics."
        ),
    ];

    private readonly record struct RuntimeProfileDefinition(
        string Id,
        string DisplayName,
        RuntimeKind RuntimeKind,
        string ModuleFileName,
        int ModuleSize,
        long ModuleFileSize,
        string ModuleSha256,
        string Notes
    )
    {
        public bool IsMatch(RuntimeFingerprint fingerprint, string? moduleSha256) =>
            moduleSha256 is not null
            && fingerprint.RuntimeKind == RuntimeKind
            && fingerprint.ModuleFileName.Equals(ModuleFileName, StringComparison.OrdinalIgnoreCase)
            && fingerprint.ModuleSize == ModuleSize
            && fingerprint.ModuleFileSize == ModuleFileSize
            && moduleSha256.Equals(ModuleSha256, StringComparison.OrdinalIgnoreCase);
    }
}
