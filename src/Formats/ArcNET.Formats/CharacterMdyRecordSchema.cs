namespace ArcNET.Formats;

internal static class CharacterMdyRecordSchema
{
    public static ReadOnlySpan<byte> V2Magic =>
        [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

    public static ReadOnlySpan<byte> StatSig => [0x04, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00];
    public static ReadOnlySpan<byte> BasicSkillSig => [0x04, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00];
    public static ReadOnlySpan<byte> TechSkillSig => [0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00];
    public static ReadOnlySpan<byte> SpellTechSig => [0x04, 0x00, 0x00, 0x00, 0x19, 0x00, 0x00, 0x00];
    public static ReadOnlySpan<byte> PositionAiSig =>
        [0x04, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0xA3, 0x4D, 0x00, 0x00];
    public static ReadOnlySpan<byte> HpDamageSig =>
        [0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x46, 0x40, 0x00, 0x00];
    public static ReadOnlySpan<byte> FatigueSig =>
        [0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x3E, 0x42, 0x00, 0x00];

    public const int SarHeaderSize = 13;
    public const int MaxScanDistance = 4096;
    public const int ExtendedScanLimit = 32768;
    public const int MaxSarBitsetWords = 256;
    public const int MaxSarElementCount = 512;
    public const int MaxRumorEntries = 2048;
    public const int MaxBlessingPairEntries = 49;
    public const int MaxNameLength = 64;
    public const int NameSearchLimit = 512;

    public const int GoldAmountBsId = 0x4B13;
    public const int GoldHandleBsId = 0x4D77;
    public const int EffectsBsId = 0x49FC;
    public const int EffectCausesBsId = 0x49FD;
    public const int GameStatsBsId = 0x4D68;
    public const int GameStatsElementCount = 11;
    public const int GameStatsTotalKillsIndex = 0;
    public const int GameStatsArrowsIndex = 8;
    public const int GameStatsBulletsIndex = 11;
    public const int GameStatsPowerCellsIndex = 12;
    public const int PortraitBsId = 0x4DA4;
    public const int PortraitElementCount = 3;
    public const int PortraitMaxFollowersElement = 0;
    public const int PortraitIndexElement = 1;
    public const int QuestSarElementSize = 16;
    public const int QuestSarBitsetWords = 37;
    public const int ReputationSarElementCount = 19;
    public const int ReputationSarBitsetWords = 3;
    public const int BlessingTsElementSize = 8;
    public const int RumorsSarElementSize = 8;
    public const int RumorsSarBitsetWords = 39;

    public static bool IsLikelyGenericSar(int elementSize, int elementCount) =>
        elementSize is 1 or 2 or 4 or 8 or 16 && elementCount is >= 1 and <= MaxSarElementCount;

    public static bool IsPrintableAscii(byte value) => value is >= 0x20 and <= 0x7E;
}
