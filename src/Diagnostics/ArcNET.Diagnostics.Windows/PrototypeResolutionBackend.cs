using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.Versioning;
using ArcNET.Core.Primitives;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.GameData.Workspace;
using ArcNET.GameObjects;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class PrototypeResolutionBackend : IPrototypeResolutionBackend
{
    public LivePlayerLocatorResult LocatePlayers(int processId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live prototype resolution currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LivePlayerLocator.Locate(memory);
    }

    public async Task<IReadOnlyList<PrototypePaletteEntry>> LoadPaletteAsync(string workspacePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        return WorkspaceGameDataCatalogProjector.ToPrototypePaletteEntries(
            await WorkspaceGameDataCatalogLoader.LoadFromModulePathAsync(workspacePath).ConfigureAwait(false)
        );
    }

    public async Task<IReadOnlyList<StaticObjectCatalogEntry>> LoadStaticObjectCatalogAsync(string workspacePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        return WorkspaceGameDataCatalogProjector.ToStaticObjectEntries(
            await WorkspaceGameDataCatalogLoader.LoadFromModulePathAsync(workspacePath).ConfigureAwait(false)
        );
    }

    public LiveObjectIdentity InspectHandle(int processId, ulong handle)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live prototype resolution currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LiveObjectInspector.Inspect(memory, handle);
    }

    public PrototypeHandleResolutionResult ResolvePrototypeHandle(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        int protoNumber
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live prototype resolution currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        if (TryResolvePrototypeHandleViaEngine(memory, runtimeProfile, protoNumber, out var handle, out var source))
            return new(true, handle, source);

        if (TryResolvePrototypeHandleFromPool(memory, protoNumber, out handle, out source))
            return new(true, handle, source);

        return new(false, 0, source);
    }

    private static bool TryResolvePrototypeHandleViaEngine(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        int protoNumber,
        out ulong handle,
        out string resolutionSource
    )
    {
        handle = 0;
        resolutionSource = "PrototypeLookupFunctionMiss";
        if (TryResolvePrototypeHandleViaEngine(memory, runtimeProfile, protoNumber, out handle))
        {
            resolutionSource = "PrototypeLookupFunction";
            return true;
        }

        if (
            protoNumber > 20
            && TryResolvePrototypeHandleViaEngine(memory, runtimeProfile, protoNumber - 20, out handle)
        )
        {
            resolutionSource = "PrototypeLookupFunctionMinus20";
            return true;
        }

        return false;
    }

    private static bool TryResolvePrototypeHandleViaEngine(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        int engineBasicPrototype,
        out ulong handle
    )
    {
        handle = 0;
        if (!runtimeProfile.SupportsCatalogRvas)
            return false;

        try
        {
            var function = FunctionCatalog.GetDefinition("prototype_handle_by_proto_number");
            using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
            var result = dispatcher.Invoke(
                memory.ToUInt32Address(memory.ResolveRva(function.Rva)),
                function.SuggestedCleanup,
                0,
                0,
                [unchecked((uint)engineBasicPrototype)],
                TimeSpan.FromSeconds(2)
            );
            if (result.State != RuntimeCallDispatcher.DispatcherState.Completed)
                return false;

            handle = ComposeWideResult(result.ResultEax, result.ResultEdx);
            if (!RuntimeSemanticCatalog.LooksLikeObjectHandle(handle))
            {
                handle = 0;
                return false;
            }

            return true;
        }
        catch (InvalidOperationException)
        {
            handle = 0;
            return false;
        }
    }

    private static bool TryResolvePrototypeHandleFromPool(
        ProcessMemory memory,
        int protoNumber,
        out ulong handle,
        out string resolutionSource
    )
    {
        handle = 0;
        resolutionSource = "PoolLookupMiss";

        var objPoolElementByteSize = TryReadObjPoolElementByteSize(memory);
        if (objPoolElementByteSize <= RuntimeOffsets.ObjPoolEntryHeaderByteSize)
            return false;

        var bucketTable = memory.ReadPointer32(memory.ResolveRva(RuntimeOffsets.ObjPoolBucketsRva));
        if (bucketTable == 0 || !memory.TryGetReadableRegion(bucketTable, out var region))
            return false;

        var bucketCount = checked((int)((ulong)region.Size / sizeof(uint)));
        var consecutiveNullBuckets = 0;
        var sawBucket = false;
        ulong fallbackHandle = 0;
        for (var bucketIndex = 0; bucketIndex < bucketCount; bucketIndex++)
        {
            nint bucketAddress;
            try
            {
                bucketAddress = memory.ReadPointer32(bucketTable + bucketIndex * sizeof(uint));
            }
            catch
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
                TryScanBucket(
                    memory,
                    bucketIndex,
                    bucketAddress,
                    objPoolElementByteSize,
                    protoNumber,
                    ref fallbackHandle,
                    out handle,
                    out resolutionSource
                )
            )
            {
                return true;
            }
        }

        if (fallbackHandle == 0)
            return false;

        handle = fallbackHandle;
        resolutionSource = "InstancePrototypeHandle";
        return true;
    }

    private static bool TryScanBucket(
        ProcessMemory memory,
        int bucketIndex,
        nint bucketAddress,
        int objPoolElementByteSize,
        int protoNumber,
        ref ulong fallbackHandle,
        out ulong handle,
        out string resolutionSource
    )
    {
        handle = 0;
        resolutionSource = "PoolLookupMiss";
        var headerBytes = new byte[ObjectHeaderSize];
        for (var slotIndex = 0; slotIndex < RuntimeOffsets.ObjPoolBucketSize; slotIndex++)
        {
            var entryAddress = bucketAddress + slotIndex * objPoolElementByteSize;
            uint entryHeader;
            try
            {
                entryHeader = unchecked((uint)memory.ReadInt32(entryAddress));
            }
            catch
            {
                return false;
            }

            if ((byte)(entryHeader & 0xFF) != StatusHandle)
                continue;

            try
            {
                memory.ReadBytes(entryAddress + RuntimeOffsets.ObjPoolEntryHeaderByteSize, headerBytes);
            }
            catch
            {
                continue;
            }

            var handleSequence = (entryHeader >> 8) & (uint)RuntimeOffsets.ObjHandleSequenceMask;
            var handleIndex = checked(bucketIndex * RuntimeOffsets.ObjPoolBucketSize + slotIndex);
            var candidateHandle = ComposeHandle(handleIndex, handleSequence);
            var oidType = BinaryPrimitives.ReadInt16LittleEndian(headerBytes.AsSpan(ObjectOidOffset, sizeof(short)));
            var prototypeOidType = BinaryPrimitives.ReadInt16LittleEndian(
                headerBytes.AsSpan(PrototypeOidOffset, sizeof(short))
            );
            var oidProtoNumber =
                oidType == GameObjectGuid.OidTypeA
                    ? BinaryPrimitives.ReadInt32LittleEndian(
                        headerBytes.AsSpan(ObjectOidProtoNumberOffset, sizeof(int))
                    )
                    : (int?)null;
            var prototypeOidProtoNumber =
                prototypeOidType == GameObjectGuid.OidTypeA
                    ? BinaryPrimitives.ReadInt32LittleEndian(
                        headerBytes.AsSpan(PrototypeOidProtoNumberOffset, sizeof(int))
                    )
                    : (int?)null;
            var prototypeHandle = BinaryPrimitives.ReadUInt64LittleEndian(
                headerBytes.AsSpan(PrototypeHandleOffset, sizeof(ulong))
            );

            if (oidProtoNumber == protoNumber)
            {
                handle = candidateHandle;
                resolutionSource = "PrototypePoolEntry";
                return true;
            }

            if (prototypeOidProtoNumber == protoNumber && prototypeHandle != 0 && fallbackHandle == 0)
                fallbackHandle = prototypeHandle;
        }

        return false;
    }

    private static int TryReadObjPoolElementByteSize(ProcessMemory memory)
    {
        try
        {
            return memory.ReadInt32(memory.ResolveRva(RuntimeOffsets.ObjPoolElementByteSizeRva));
        }
        catch
        {
            return 0;
        }
    }

    private static ulong ComposeHandle(int index, uint sequence) =>
        ((ulong)(uint)index << RuntimeOffsets.ObjHandleIndexShift)
        | ((ulong)sequence << RuntimeOffsets.ObjHandleSequenceShift)
        | RuntimeOffsets.ObjHandleMarkerValue;

    private static ulong ComposeWideResult(uint eax, uint edx) => eax | ((ulong)edx << 32);

    private const byte StatusHandle = (byte)'H';
    private const int ConsecutiveNullBucketBreakCount = 32;
    private const int ObjectHeaderSize = 0x40;
    private const int ObjectOidOffset = 0x08;
    private const int ObjectOidProtoNumberOffset = 0x10;
    private const int PrototypeOidOffset = 0x20;
    private const int PrototypeOidProtoNumberOffset = 0x28;
    private const int PrototypeHandleOffset = 0x38;
}
