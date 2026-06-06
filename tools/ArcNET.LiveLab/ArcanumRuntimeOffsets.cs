namespace ArcNET.LiveLab;

/// <summary>
/// CE-derived module-relative anchors for the current Arcanum runtime research harness.
/// These are specific to the game build targeted by the supplied Cheat Engine table.
/// </summary>
internal static class ArcanumRuntimeOffsets
{
    public const string ProcessName = "Arcanum";
    public const string ModuleName = "Arcanum.exe";

    public const int ActionPointsRva = 0x001FC234;
    public const int CurrentCharacterSheetIdRva = 0x0024E010;

    public const int CharacterSheetSubstructureHookRva = 0x00008C46;
    public const int CharacterSheetPropertyHookRva = 0x00008AC7;

    public const int LevelRecalcRva = 0x000A69C0;
    public const int UpdateFollowerLevelRva = 0x000A6CB0;
    public const int StatBaseSetRva = 0x000B0980;
    public const int BackgroundEducateFollowersRva = 0x000C2950;
    public const int UiShowInvenLootRva = 0x000602D0;
    public const int UiStartDialogRva = 0x000609E0;
    public const int CritterKillRva = 0x0005D900;
    public const int ItemInsertRva = 0x00066640;
    public const int ItemEquippedRva = 0x000677B0;
    public const int ItemForceRemoveRva = 0x00067860;
    public const int ItemUnequippedRva = 0x00067CB0;
    public const int ObjectDestroyRva = 0x0003CCA0;
    public const int ObjectScriptExecuteRva = 0x00041980;
    public const int ReactionAdjRva = 0x000C0DE0;

    public const int ObjPoolBucketsRva = 0x002036BC;
    public const int ObjPoolElementByteSizeRva = 0x002036E4;

    public const int ObjPoolBucketSize = 0x2000;
    public const int ObjPoolEntryHeaderByteSize = 0x0004;
    public const int ObjHandleIndexShift = 29;
    public const int ObjHandleSequenceShift = 3;
    public const ulong ObjHandleSequenceMask = 0x007FFFFF;
    public const ulong ObjHandleMarkerMask = 0x00000007;
    public const ulong ObjHandleMarkerValue = 0x00000002;
}
