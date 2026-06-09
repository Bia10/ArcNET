using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.Versioning;
using ArcNET.Core.Primitives;
using ArcNET.Diagnostics.Contracts;
using ArcNET.GameObjects;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public static class LiveObjectInspector
{
    public static LiveObjectIdentity Inspect(IProcessMemory memory, ulong handle)
    {
        ArgumentNullException.ThrowIfNull(memory);

        var handleHex = RuntimeSemanticCatalog.FormatHandle(handle);
        if (!RuntimeSemanticCatalog.LooksLikeObjectHandle(handle))
            return new LiveObjectIdentity(
                handleHex,
                false,
                "NotHandleShaped",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null
            );

        try
        {
            var objPoolElementByteSize = TryReadObjPoolElementByteSize(memory);
            if (objPoolElementByteSize <= RuntimeOffsets.ObjPoolEntryHeaderByteSize)
                return new LiveObjectIdentity(
                    handleHex,
                    true,
                    "ObjPoolUnavailable",
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                );

            var index = checked((int)(handle >> RuntimeOffsets.ObjHandleIndexShift));
            var bucketIndex = index / RuntimeOffsets.ObjPoolBucketSize;
            var slotIndex = index % RuntimeOffsets.ObjPoolBucketSize;
            var bucketTable = memory.ReadPointer32(memory.ResolveRva(RuntimeOffsets.ObjPoolBucketsRva));
            if (bucketTable == 0)
            {
                return new LiveObjectIdentity(
                    handleHex,
                    true,
                    "BucketTableUnavailable",
                    index,
                    bucketIndex,
                    slotIndex,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                );
            }

            var bucketAddress = memory.ReadPointer32(bucketTable + bucketIndex * sizeof(uint));
            if (bucketAddress == 0)
            {
                return new LiveObjectIdentity(
                    handleHex,
                    true,
                    "BucketUnavailable",
                    index,
                    bucketIndex,
                    slotIndex,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                );
            }

            var entryAddress = bucketAddress + slotIndex * objPoolElementByteSize;
            var entryHeader = unchecked((uint)memory.ReadInt32(entryAddress));
            var status = (byte)(entryHeader & byte.MaxValue);
            var sequence = (entryHeader >> 8) & (uint)RuntimeOffsets.ObjHandleSequenceMask;
            var expectedSequence = (uint)(
                (handle >> RuntimeOffsets.ObjHandleSequenceShift) & RuntimeOffsets.ObjHandleSequenceMask
            );
            if (status != StatusHandle || sequence != expectedSequence)
            {
                return new LiveObjectIdentity(
                    handleHex,
                    true,
                    "PoolEntryMismatch",
                    index,
                    bucketIndex,
                    slotIndex,
                    FormatAddress(entryAddress),
                    null,
                    status,
                    sequence,
                    expectedSequence,
                    null
                );
            }

            var objectAddress = entryAddress + RuntimeOffsets.ObjPoolEntryHeaderByteSize;
            var header = new byte[ObjectHeaderSize];
            memory.ReadBytes(objectAddress, header);
            var objectTypeRaw = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(ObjectTypeOffset, sizeof(int)));
            var objectType = (ObjectType)objectTypeRaw;
            var objectTypeName = Enum.IsDefined(objectType) ? objectType.ToString() : null;
            var objectId = ReadOid(header.AsSpan(ObjectOidOffset, ObjectIdSize));
            var prototypeId = ReadOid(header.AsSpan(PrototypeOidOffset, ObjectIdSize));
            var prototypeHandle = BinaryPrimitives.ReadUInt64LittleEndian(
                header.AsSpan(PrototypeHandleOffset, sizeof(ulong))
            );

            return new LiveObjectIdentity(
                handleHex,
                true,
                "PoolEntry",
                index,
                bucketIndex,
                slotIndex,
                FormatAddress(entryAddress),
                FormatAddress(objectAddress),
                status,
                sequence,
                expectedSequence,
                new LiveObjectHeader(
                    objectTypeRaw,
                    objectTypeName,
                    objectId,
                    prototypeId,
                    RuntimeSemanticCatalog.FormatHandle(prototypeHandle)
                )
            );
        }
        catch (Win32Exception ex)
        {
            return new LiveObjectIdentity(
                handleHex,
                true,
                $"ReadFailed:{ex.NativeErrorCode}",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null
            );
        }
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

    private static LiveOid ReadOid(ReadOnlySpan<byte> span)
    {
        var oidType = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(0x00, sizeof(short)));
        var padding2 = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(0x02, sizeof(short)));
        var padding4 = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(0x04, sizeof(int)));
        var guid = new Guid(span.Slice(0x08, 16));
        var value = new GameObjectGuid(oidType, padding2, padding4, guid);
        return new LiveOid(oidType, value.GetProtoNumber(), guid.ToString(), value.ToLabel(), value.ToString());
    }

    private static string FormatAddress(nint address)
    {
        var value = (ulong)(long)address;
        return value <= uint.MaxValue ? $"0x{value:X8}" : $"0x{value:X16}";
    }

    private const byte StatusHandle = (byte)'H';
    private const int ObjectHeaderSize = 0x40;
    private const int ObjectIdSize = 0x18;
    private const int ObjectTypeOffset = 0x00;
    private const int ObjectOidOffset = 0x08;
    private const int PrototypeOidOffset = 0x20;
    private const int PrototypeHandleOffset = 0x38;
}
