namespace ArcNET.Diagnostics;

public sealed record class CrashDumpCaptureSnapshot(CrashDumpWriteSnapshot Dump, CrashDumpAnalysisSnapshot Analysis);
