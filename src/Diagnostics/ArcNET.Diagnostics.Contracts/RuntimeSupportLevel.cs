namespace ArcNET.Diagnostics.Contracts;

public enum RuntimeSupportLevel : byte
{
    Unsupported = 0,
    Exploratory = 1,
    SymbolAssisted = 2,
    Validated = 3,
}
