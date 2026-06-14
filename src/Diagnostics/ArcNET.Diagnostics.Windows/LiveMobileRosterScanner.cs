using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.Versioning;
using ArcNET.Diagnostics.Contracts;
using ArcNET.GameObjects;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public static class LiveMobileRosterScanner
{
    public static IReadOnlyList<LiveObjectIdentity> Scan(IProcessMemory memory, int maxEntries)
    {
        ArgumentNullException.ThrowIfNull(memory);

        var cappedMaxEntries = maxEntries <= 0 ? DefaultMaxEntries : maxEntries;
        var objPoolElementByteSize = TryReadObjPoolElementByteSize(memory);
        if (objPoolElementByteSize <= RuntimeOffsets.ObjPoolEntryHeaderByteSize)
            return [];

        var bucketTable = memory.ReadPointer32(memory.ResolveRva(RuntimeOffsets.ObjPoolBucketsRva));
        if (bucketTable == 0)
            return [];

        List<LiveObjectIdentity> mobiles = [];
        var headerBytes = new byte[ObjectHeaderSize];
        var sawBucket = false;
        var consecutiveNullBuckets = 0;
        for (var bucketIndex = 0; bucketIndex < MaxBucketScanCount; bucketIndex++)
        {
            nint bucketAddress;
            try
            {
                bucketAddress = memory.ReadPointer32(bucketTable + bucketIndex * sizeof(uint));
            }
            catch (Win32Exception)
            {
                break;
            }

            if (bucketAddress == 0)
            {
                if (sawBucket && ++consecutiveNullBuckets >= ConsecutiveNullBucketBreakCount)
                    break;

                continue;
            }

            sawBucket = true;
            consecutiveNullBuckets = 0;
            if (
                !TryScanBucket(
                    memory,
                    bucketIndex,
                    bucketAddress,
                    objPoolElementByteSize,
                    headerBytes,
                    cappedMaxEntries,
                    mobiles
                )
            )
            {
                break;
            }

            if (mobiles.Count >= cappedMaxEntries)
                break;
        }

        return
        [
            .. mobiles
                .OrderBy(static identity => identity.Header?.ObjectTypeName)
                .ThenBy(static identity => identity.Header?.PrototypeId.ProtoNumber ?? int.MaxValue)
                .ThenBy(static identity => identity.HandleHex, StringComparer.Ordinal),
        ];
    }

    private static bool TryScanBucket(
        IProcessMemory memory,
        int bucketIndex,
        nint bucketAddress,
        int objPoolElementByteSize,
        byte[] headerBytes,
        int maxEntries,
        List<LiveObjectIdentity> mobiles
    )
    {
        for (var slotIndex = 0; slotIndex < RuntimeOffsets.ObjPoolBucketSize; slotIndex++)
        {
            if (mobiles.Count >= maxEntries)
                return true;

            var entryAddress = bucketAddress + slotIndex * objPoolElementByteSize;
            uint entryHeader;
            try
            {
                entryHeader = unchecked((uint)memory.ReadInt32(entryAddress));
            }
            catch (Win32Exception)
            {
                return false;
            }

            if ((byte)(entryHeader & byte.MaxValue) != StatusHandle)
                continue;

            try
            {
                memory.ReadBytes(entryAddress + RuntimeOffsets.ObjPoolEntryHeaderByteSize, headerBytes);
            }
            catch (Win32Exception)
            {
                continue;
            }

            var objectType = (ObjectType)BinaryPrimitives.ReadInt32LittleEndian(headerBytes.AsSpan(ObjectTypeOffset));
            if (objectType is not ObjectType.Pc and not ObjectType.Npc)
                continue;

            var prototypeHandle = BinaryPrimitives.ReadUInt64LittleEndian(
                headerBytes.AsSpan(PrototypeHandleOffset, sizeof(ulong))
            );
            if (!RuntimeSemanticCatalog.LooksLikeObjectHandle(prototypeHandle))
                continue;

            var handleSequence = (entryHeader >> 8) & (uint)RuntimeOffsets.ObjHandleSequenceMask;
            var handleIndex = checked(bucketIndex * RuntimeOffsets.ObjPoolBucketSize + slotIndex);
            var handle = ComposeHandle(handleIndex, handleSequence);
            var identity = LiveObjectInspector.Inspect(memory, handle);
            if (identity.HasHeader)
                mobiles.Add(identity);
        }

        return true;
    }

    private static int TryReadObjPoolElementByteSize(IProcessMemory memory)
    {
        try
        {
            return memory.ReadInt32(memory.ResolveRva(RuntimeOffsets.ObjPoolElementByteSizeRva));
        }
        catch (Win32Exception)
        {
            return 0;
        }
    }

    private static ulong ComposeHandle(int index, uint sequence) =>
        ((ulong)(uint)index << RuntimeOffsets.ObjHandleIndexShift)
        | ((ulong)sequence << RuntimeOffsets.ObjHandleSequenceShift)
        | RuntimeOffsets.ObjHandleMarkerValue;

    private const byte StatusHandle = (byte)'H';
    private const int ConsecutiveNullBucketBreakCount = 32;
    private const int MaxBucketScanCount = 4096;
    private const int DefaultMaxEntries = 128;
    private const int ObjectHeaderSize = 0x40;
    private const int ObjectTypeOffset = 0x00;
    private const int PrototypeHandleOffset = 0x38;
}
