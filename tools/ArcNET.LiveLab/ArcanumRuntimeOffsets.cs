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

    public static ReadOnlySpan<byte> CharacterSheetSubstructureOriginal => [0x8B, 0x4F, 0x50, 0x8D, 0x14, 0x81];

    public static ReadOnlySpan<byte> CharacterSheetPropertyOriginal => [0x8B, 0x4E, 0x50, 0x8D, 0x14, 0x81];
}
