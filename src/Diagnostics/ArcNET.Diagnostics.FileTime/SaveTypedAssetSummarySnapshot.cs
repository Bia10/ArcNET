namespace ArcNET.Diagnostics;

public sealed record SaveTypedAssetSummarySnapshot(
    int TotalFileCount,
    int RawFileCount,
    int ParseErrorCount,
    int MobCount,
    int MobileMdCount,
    int MobileMdyCount,
    int SectorCount,
    int JumpFileCount,
    int MapPropertiesCount,
    int MessageCount,
    int TownMapFogCount,
    int DataSavCount,
    int Data2SavCount,
    int ScriptCount,
    int DialogCount
);
