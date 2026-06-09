namespace ArcNET.Diagnostics.Windows;

public readonly record struct PrototypeHandleResolutionResult(bool Success, ulong Handle, string ResolutionSource);
