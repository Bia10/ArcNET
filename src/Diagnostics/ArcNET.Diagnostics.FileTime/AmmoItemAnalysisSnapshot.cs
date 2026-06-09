namespace ArcNET.Diagnostics;

public sealed record AmmoItemAnalysisSnapshot(int? Quantity, int? AmmoType) : MobItemSpecificAnalysisSnapshot;
