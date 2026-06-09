using ArcNET.Diagnostics;

namespace ArcNET.Diagnostics.Windows;

internal readonly record struct NativeInvocationResult(NativeReadSnapshot Snapshot, uint ResultEax, uint ResultEdx);
