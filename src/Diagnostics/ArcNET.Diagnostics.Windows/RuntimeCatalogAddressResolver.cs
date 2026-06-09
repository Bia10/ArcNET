using System.Collections.Concurrent;

namespace ArcNET.Diagnostics.Windows;

public static class RuntimeCatalogAddressResolver
{
    public const string ForceSignatureFallbackEnvironmentVariable =
        CatalogAddressResolver.ForceSignatureFallbackEnvironmentVariable;

    public static bool ShouldForceSignatureFallback() => CatalogAddressResolver.ShouldForceSignatureFallback();

    public static ResolvedCatalogAddress Resolve(ProcessMemory memory, string key, int preferredRva, string operation)
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        var runtimeProfile = RuntimeProfileService.Resolve(
            memory.ProcessName,
            memory.ProcessId,
            memory.ModulePath,
            memory.ModuleBase,
            memory.ModuleSize
        );
        var forceSignatureFallback = ShouldForceSignatureFallback();
        var resolution = CatalogAddressResolver.Resolve(
            new CatalogAddressResolveRequest(
                Key: key,
                PreferredRva: preferredRva,
                Operation: operation,
                ModuleFileName: Path.GetFileName(memory.ModulePath),
                ModuleSize: memory.ModuleSize,
                ModuleBytes: ShouldReadModuleBytes(
                    runtimeProfile,
                    forceSignatureFallback,
                    preferredRva,
                    memory.ModuleSize
                )
                    ? GetCachedModuleBytes(memory)
                    : [],
                RuntimeProfile: runtimeProfile,
                ForceSignatureFallback: forceSignatureFallback,
                SignaturesByNormalizedKey: CatalogSignatureCatalog.SignaturesByNormalizedKey,
                AddressCacheKey: $"{memory.ProcessId}:{memory.ModulePath}:{memory.ModuleSize}"
            )
        );

        return new ResolvedCatalogAddress(
            memory.ToUInt32Address(memory.ResolveRva(unchecked((int)resolution.Rva))),
            resolution.Rva,
            CodeCatalog.FormatModuleAddress(Path.GetFileName(memory.ModulePath), resolution.Rva),
            resolution.Resolution
        );
    }

    public static IReadOnlyList<RuntimeWatchHookDefinition> BindHooks(
        ProcessMemory memory,
        IReadOnlyList<RuntimeWatchHookDefinition> hooks,
        string operation
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(hooks);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        if (hooks.Count == 0)
            return [];

        List<RuntimeWatchHookDefinition> boundHooks = new(hooks.Count);
        foreach (var hook in hooks)
        {
            var resolved = Resolve(memory, hook.Key, hook.Rva, $"{operation} hook '{hook.Key}'");
            if (!resolved.Rva.HasValue)
                throw new InvalidOperationException($"{operation} hook '{hook.Key}' did not resolve to a module RVA.");

            boundHooks.Add(hook with { Rva = checked((int)resolved.Rva.Value), Site = resolved.Site });
        }

        return [.. boundHooks];
    }

    private static bool ShouldReadModuleBytes(
        ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot runtimeProfile,
        bool forceSignatureFallback,
        int preferredRva,
        int moduleSize
    ) =>
        forceSignatureFallback
        || !runtimeProfile.SupportsCatalogRvas
        || preferredRva <= 0
        || preferredRva >= moduleSize;

    private static byte[] GetCachedModuleBytes(ProcessMemory memory)
    {
        var cacheKey = $"{memory.ProcessId}:{memory.ModulePath}:{memory.ModuleSize}";
        return s_moduleBytesByCacheKey.GetOrAdd(cacheKey, _ => memory.ReadModuleBytes());
    }

    private static readonly ConcurrentDictionary<string, byte[]> s_moduleBytesByCacheKey = new(
        StringComparer.OrdinalIgnoreCase
    );
}
