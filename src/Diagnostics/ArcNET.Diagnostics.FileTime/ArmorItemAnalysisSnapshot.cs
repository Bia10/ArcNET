namespace ArcNET.Diagnostics;

public sealed record ArmorItemAnalysisSnapshot(
    int? ArmorClassAdjustment,
    int? MagicArmorClassAdjustment,
    int? SilentMoveAdjustment
) : MobItemSpecificAnalysisSnapshot;
