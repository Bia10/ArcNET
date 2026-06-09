using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.GameObjects;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public static class LivePlayerLocator
{
    public static LivePlayerLocatorResult Locate(IProcessMemory memory)
    {
        ArgumentNullException.ThrowIfNull(memory);

        var candidates = ScanPcCandidates(memory);
        var livePlayerCandidates = candidates
            .Where(static candidate => candidate.CandidateKind == CandidateKindLiveInstance)
            .ToArray();
        var prototypeTemplates = candidates
            .Where(static candidate => candidate.CandidateKind == CandidateKindPrototypeTemplate)
            .ToArray();
        ulong? autoResolvedHandle = livePlayerCandidates.Length == 1 ? livePlayerCandidates[0].Handle : null;
        var resolutionSource =
            autoResolvedHandle.HasValue ? "SingleLivePcInstance"
            : livePlayerCandidates.Length > 1 ? "AmbiguousLivePcInstances"
            : prototypeTemplates.Length > 0 ? "OnlyPrototypePcEntries"
            : "NoLivePcCandidates";

        List<string> notes =
        [
            "Player resolution currently prefers the live object pool over transient UI seams.",
            "Object GUID/OID data is usually more stable across launches than runtime addresses or live handles.",
        ];

        if (prototypeTemplates.Length > 0)
        {
            notes.Add(
                "PC prototype templates are reported separately from live player instances so they do not make the main-player answer ambiguous."
            );
        }

        if (livePlayerCandidates.Length > 1)
            notes.Add("Multiple live PC instances were found, so the locator intentionally refuses to guess.");

        return new LivePlayerLocatorResult(
            autoResolvedHandle,
            resolutionSource,
            BuildHumanSummary(livePlayerCandidates, prototypeTemplates),
            livePlayerCandidates,
            prototypeTemplates,
            [.. notes]
        );
    }

    private static LivePlayerCandidate[] ScanPcCandidates(IProcessMemory memory)
    {
        var objPoolElementByteSize = TryReadObjPoolElementByteSize(memory);
        if (objPoolElementByteSize <= RuntimeOffsets.ObjPoolEntryHeaderByteSize)
            return [];

        var bucketTable = memory.ReadPointer32(memory.ResolveRva(RuntimeOffsets.ObjPoolBucketsRva));
        if (bucketTable == 0)
            return [];

        List<LivePlayerCandidate> candidates = [];
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
            if (!TryScanBucket(memory, bucketIndex, bucketAddress, objPoolElementByteSize, headerBytes, candidates))
                break;
        }

        return [.. candidates.OrderBy(static candidate => candidate.Handle)];
    }

    private static bool TryScanBucket(
        IProcessMemory memory,
        int bucketIndex,
        nint bucketAddress,
        int objPoolElementByteSize,
        byte[] headerBytes,
        List<LivePlayerCandidate> candidates
    )
    {
        for (var slotIndex = 0; slotIndex < RuntimeOffsets.ObjPoolBucketSize; slotIndex++)
        {
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

            var objectType = BinaryPrimitives.ReadInt32LittleEndian(headerBytes.AsSpan(ObjectTypeOffset, sizeof(int)));
            if (objectType != (int)ObjectType.Pc)
                continue;

            var prototypeHandle = BinaryPrimitives.ReadUInt64LittleEndian(
                headerBytes.AsSpan(PrototypeHandleOffset, sizeof(ulong))
            );
            var objectOidType = BinaryPrimitives.ReadInt16LittleEndian(
                headerBytes.AsSpan(ObjectOidOffset, sizeof(short))
            );
            var prototypeOidType = BinaryPrimitives.ReadInt16LittleEndian(
                headerBytes.AsSpan(PrototypeOidOffset, sizeof(short))
            );
            var handleSequence = (entryHeader >> 8) & (uint)RuntimeOffsets.ObjHandleSequenceMask;
            var handleIndex = checked(bucketIndex * RuntimeOffsets.ObjPoolBucketSize + slotIndex);
            var handle = ComposeHandle(handleIndex, handleSequence);
            candidates.Add(
                DescribeHandle(memory, handle, ClassifyCandidateKind(prototypeHandle, objectOidType, prototypeOidType))
            );
        }

        return true;
    }

    private static LivePlayerCandidate DescribeHandle(IProcessMemory memory, ulong handle, string candidateKind)
    {
        var runtimeIdentity = LiveObjectInspector.Inspect(memory, handle);
        var header = runtimeIdentity.Header;
        var protoNumber =
            candidateKind == CandidateKindPrototypeTemplate
                ? header?.ObjectId.ProtoNumber
                : header?.PrototypeId.ProtoNumber ?? header?.ObjectId.ProtoNumber;

        return new LivePlayerCandidate(
            handle,
            runtimeIdentity.HandleHex,
            candidateKind,
            BuildDisplayValue(
                runtimeIdentity.HandleHex,
                candidateKind,
                header?.ObjectId.Label,
                header?.PrototypeId.Label,
                protoNumber
            ),
            runtimeIdentity.ResolutionSource,
            header?.ObjectTypeName,
            header?.ObjectId.Label,
            header?.PrototypeId.Label,
            protoNumber,
            header?.PrototypeHandle
        );
    }

    private static string BuildDisplayValue(
        string handleHex,
        string candidateKind,
        string? objectIdLabel,
        string? prototypeIdLabel,
        int? protoNumber
    )
    {
        var objectText = !string.IsNullOrWhiteSpace(objectIdLabel) ? objectIdLabel : "pc-object";
        var protoText =
            !string.IsNullOrWhiteSpace(prototypeIdLabel) ? prototypeIdLabel
            : protoNumber.HasValue ? $"proto#{protoNumber.Value}"
            : "unknown-proto";

        return candidateKind switch
        {
            CandidateKindLiveInstance => $"Live PC instance {objectText} from {protoText} ({handleHex})",
            CandidateKindPrototypeTemplate => $"PC prototype template {objectText} ({handleHex})",
            _ => $"PC object {objectText} ({handleHex})",
        };
    }

    private static string BuildHumanSummary(
        IReadOnlyList<LivePlayerCandidate> livePlayerCandidates,
        IReadOnlyList<LivePlayerCandidate> prototypeTemplates
    )
    {
        if (livePlayerCandidates.Count == 1)
        {
            return $"Your likely live player is {livePlayerCandidates[0].DisplayValue}. I also found {prototypeTemplates.Count} PC prototype template entr{(prototypeTemplates.Count == 1 ? "y" : "ies")} and ignored them for auto-selection.";
        }

        if (livePlayerCandidates.Count > 1)
        {
            return $"I found {livePlayerCandidates.Count} live PC instances, so I cannot safely say which one you control. I also found {prototypeTemplates.Count} PC prototype template entr{(prototypeTemplates.Count == 1 ? "y" : "ies")}.";
        }

        if (prototypeTemplates.Count > 0)
            return "I only found PC prototype templates, not a live player instance.";

        return "I did not find any PC objects in the live object pool.";
    }

    private static string ClassifyCandidateKind(ulong prototypeHandle, short objectOidType, short prototypeOidType)
    {
        if (prototypeHandle != 0)
            return CandidateKindLiveInstance;

        if (prototypeOidType <= 0 || objectOidType == prototypeOidType)
            return CandidateKindPrototypeTemplate;

        return CandidateKindUnknownPc;
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

    private const string CandidateKindLiveInstance = "LiveInstance";
    private const string CandidateKindPrototypeTemplate = "PrototypeTemplate";
    private const string CandidateKindUnknownPc = "UnknownPc";
    private const byte StatusHandle = (byte)'H';
    private const int ConsecutiveNullBucketBreakCount = 32;
    private const int MaxBucketScanCount = 4096;
    private const int ObjectHeaderSize = 0x40;
    private const int ObjectTypeOffset = 0x00;
    private const int ObjectOidOffset = 0x08;
    private const int PrototypeOidOffset = 0x20;
    private const int PrototypeHandleOffset = 0x38;
}
