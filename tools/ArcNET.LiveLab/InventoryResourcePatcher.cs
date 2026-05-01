using System.Runtime.Versioning;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal static class InventoryResourcePatcher
{
    private static readonly nint[] GoldAddresses =
    [
        (nint)0x10888ABC,
        (nint)0x108EFD34,
        (nint)0x108EFD74,
        (nint)0x113B4BB4,
    ];

    private static readonly nint[] ArrowsAddresses =
    [
        (nint)0x1087ACDC,
        (nint)0x1088437C,
        (nint)0x108AE584,
        (nint)0x108E92F4,
        (nint)0x108E9374,
        (nint)0x108E93B4,
        (nint)0x108EC9C8,
    ];

    private static readonly nint[] BulletsAddresses =
    [
        (nint)0x108887A0,
        (nint)0x10888890,
        (nint)0x108ED200,
        (nint)0x108EBB74,
        (nint)0x108EFFA0,
        (nint)0x108E2250,
        (nint)0x108E27F8,
        (nint)0x108E3698,
        (nint)0x108E3C30,
        (nint)0x108EC5E8,
        (nint)0x108EF1B8,
        (nint)0x108F1630,
    ];

    private static readonly int[] HighHeapHeader = [-950191472, 144998066, 2518, 2, 40, 50];

    public static object[] PatchResources(ProcessMemory memory, int gold, int arrows, int bullets)
    {
        var writes = new List<object>();
        writes.AddRange(Int32RuntimeScanner.PatchAddresses(memory, gold, GoldAddresses));
        writes.AddRange(Int32RuntimeScanner.PatchAddresses(memory, arrows, ArrowsAddresses));
        writes.AddRange(Int32RuntimeScanner.PatchAddresses(memory, bullets, BulletsAddresses));

        writes.AddRange(
            Int32RuntimeScanner.PatchRecordFieldInRange(
                memory,
                (nint)0x11284000,
                65536,
                10,
                7,
                memory.ReadInt32((nint)0x11284814),
                bullets,
                HighHeapHeader
            )
        );
        writes.AddRange(
            Int32RuntimeScanner.PatchRecordFieldInRange(
                memory,
                (nint)0x11284000,
                65536,
                10,
                9,
                memory.ReadInt32((nint)0x112849AC),
                bullets,
                HighHeapHeader
            )
        );
        writes.AddRange(
            Int32RuntimeScanner.PatchRecordFieldInRange(
                memory,
                (nint)0x11284000,
                65536,
                10,
                7,
                memory.ReadInt32((nint)0x1128EFDC),
                arrows,
                HighHeapHeader
            )
        );
        writes.AddRange(
            Int32RuntimeScanner.PatchRecordFieldInRange(
                memory,
                (nint)0x11284000,
                65536,
                10,
                8,
                memory.ReadInt32((nint)0x112849A8),
                arrows,
                HighHeapHeader
            )
        );
        writes.AddRange(
            Int32RuntimeScanner.PatchRecordFieldInRange(
                memory,
                (nint)0x11390000,
                262144,
                8,
                0,
                memory.ReadInt32((nint)0x1139B054),
                gold,
                [memory.ReadInt32((nint)0x1139B054), 288948984, -1, 5, 0, 0]
            )
        );
        writes.AddRange(
            Int32RuntimeScanner.PatchRecordFieldInRange(
                memory,
                (nint)0x11390000,
                262144,
                7,
                0,
                memory.ReadInt32((nint)0x11393B84),
                gold,
                [memory.ReadInt32((nint)0x11393B84), -1050854762, 146112180, 285227596, 171003200, 0]
            )
        );

        return writes.ToArray();
    }
}
