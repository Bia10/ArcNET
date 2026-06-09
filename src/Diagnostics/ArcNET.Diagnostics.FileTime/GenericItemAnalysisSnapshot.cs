namespace ArcNET.Diagnostics;

public sealed record GenericItemAnalysisSnapshot(int? UsageBonus, int? UsesRemaining) : MobItemSpecificAnalysisSnapshot;
