namespace ArcNET.Diagnostics;

public sealed record WeaponItemAnalysisSnapshot(
    int? DamageLower,
    int? DamageUpper,
    int? MagicDamageBonus,
    int? Speed,
    int? MagicSpeedBonus,
    int? Range,
    int? BonusToHit,
    int? MagicHitBonus,
    int? MinStrength
) : MobItemSpecificAnalysisSnapshot;
