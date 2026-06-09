using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public static class DiagnosticsCapabilityPolicy
{
    public static RuntimeCapabilityReport Create(RuntimeProfileSnapshot profile, bool hasModuleSymbols = false)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var capabilities =
            DiagnosticsCapability.ReadMemory
            | DiagnosticsCapability.CaptureDump
            | DiagnosticsCapability.ResolveRuntimeProfile;
        List<string> warnings = [];

        if (hasModuleSymbols)
            capabilities |= DiagnosticsCapability.LoadModuleSymbols;

        var supportLevel = profile.SupportLevel;
        switch (profile.SupportLevel)
        {
            case RuntimeSupportLevel.Validated:
                capabilities |=
                    DiagnosticsCapability.ReadStructuredState
                    | DiagnosticsCapability.WatchHooks
                    | DiagnosticsCapability.DecodeObjectLayout
                    | DiagnosticsCapability.InvokeFunctions
                    | DiagnosticsCapability.InterceptFunctions
                    | DiagnosticsCapability.MutateRuntime;
                break;

            case RuntimeSupportLevel.Exploratory
                when hasModuleSymbols && profile.RuntimeKind == RuntimeKind.CommunityEdition:
                supportLevel = RuntimeSupportLevel.SymbolAssisted;
                capabilities |= DiagnosticsCapability.InvokeFunctions | DiagnosticsCapability.InterceptFunctions;
                warnings.Add(
                    "Module symbols can unlock named-function research workflows even without a validated fixed-RVA profile."
                );
                break;
        }

        if (!string.IsNullOrWhiteSpace(profile.Notes))
            warnings.Add(profile.Notes);

        if (!string.IsNullOrWhiteSpace(profile.HashError))
            warnings.Add($"Module hash could not be computed: {profile.HashError}");

        return new RuntimeCapabilityReport(
            supportLevel,
            capabilities,
            warnings.Count == 0 ? [] : [.. warnings.Distinct(StringComparer.Ordinal)]
        );
    }
}
