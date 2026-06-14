using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public static class RuntimeProfileService
{
    public static RuntimeFingerprint ReadFingerprint(
        string processName,
        int processId,
        string modulePath,
        nint moduleBase,
        int moduleSize
    ) => RuntimeFingerprintReader.Create(processName, processId, modulePath, moduleBase, moduleSize);

    public static RuntimeProfileSnapshot Resolve(
        string processName,
        int processId,
        string modulePath,
        nint moduleBase,
        int moduleSize
    )
    {
        var fingerprint = ReadFingerprint(processName, processId, modulePath, moduleBase, moduleSize);
        _ = RuntimeProfileMatcher.TryComputeModuleSha256(modulePath, out var moduleSha256, out var hashError);
        return RuntimeProfileMatcher.Match(fingerprint, moduleSha256, hashError);
    }

    public static RuntimeProfileSnapshot RequireCatalogSupport(
        string processName,
        int processId,
        string modulePath,
        nint moduleBase,
        int moduleSize,
        string operation
    )
    {
        var runtimeProfile = Resolve(processName, processId, modulePath, moduleBase, moduleSize);
        if (runtimeProfile.SupportsCatalogRvas)
            return runtimeProfile;

        var details = runtimeProfile.ModuleSha256 is { Length: > 0 }
            ? $" Current module SHA256 is {runtimeProfile.ModuleSha256}."
            : string.Empty;
        throw new InvalidOperationException(
            $"{operation} requires a validated runtime profile before reading fixed RVAs. {runtimeProfile.Notes}{details}"
        );
    }
}
