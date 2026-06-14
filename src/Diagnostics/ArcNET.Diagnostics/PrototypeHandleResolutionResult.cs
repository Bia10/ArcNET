namespace ArcNET.Diagnostics;

public readonly record struct PrototypeHandleResolutionResult(bool Success, ulong Handle, string ResolutionSource);
