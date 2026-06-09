namespace ArcNET.Diagnostics.Contracts;

public sealed record class RuntimeCapabilityReport(
    RuntimeSupportLevel SupportLevel,
    DiagnosticsCapability Capabilities,
    IReadOnlyList<string> Warnings
);
