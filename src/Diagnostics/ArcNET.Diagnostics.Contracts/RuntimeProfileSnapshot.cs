namespace ArcNET.Diagnostics.Contracts;

public sealed record class RuntimeProfileSnapshot(
    string? Id,
    string DisplayName,
    RuntimeKind RuntimeKind,
    RuntimeSupportLevel SupportLevel,
    bool SupportsCatalogRvas,
    string Notes,
    string? ModuleSha256,
    string? HashError
);
