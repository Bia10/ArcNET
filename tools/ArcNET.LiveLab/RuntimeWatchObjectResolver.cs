using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.Versioning;
using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.Editor;
using ArcNET.GameObjects;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal sealed class RuntimeWatchObjectResolver : IDisposable
{
    private readonly ProcessMemory _memory;
    private readonly EditorWorkspace? _workspace;
    private readonly Dictionary<ulong, ResolvedObject> _objectsByHandle = [];
    private readonly Dictionary<int, ResolvedProto> _protosByNumber = [];
    private readonly byte[] _objectHeaderBuffer = new byte[ObjectHeaderSize];
    private readonly int _objPoolElementByteSize;

    private RuntimeWatchObjectResolver(
        ProcessMemory memory,
        EditorWorkspace? workspace,
        string gameDirectory,
        string statusText,
        bool namesEnabled
    )
    {
        _memory = memory;
        _workspace = workspace;
        GameDirectory = gameDirectory;
        StatusText = statusText;
        NamesEnabled = namesEnabled;
        _objPoolElementByteSize = TryReadObjPoolElementByteSize(memory);
    }

    public string GameDirectory { get; }

    public string StatusText { get; }

    public bool NamesEnabled { get; }

    public static async Task<RuntimeWatchObjectResolver> CreateAsync(
        ProcessMemory memory,
        CancellationToken cancellationToken = default
    )
    {
        var gameDirectory =
            Path.GetDirectoryName(memory.ModulePath)
            ?? throw new InvalidOperationException("Unable to derive the Arcanum installation directory.");

        try
        {
            var workspace = await EditorWorkspaceLoader
                .LoadFromGameInstallAsync(gameDirectory, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return new RuntimeWatchObjectResolver(
                memory,
                workspace,
                gameDirectory,
                $"proto/name catalog loaded from {gameDirectory}",
                namesEnabled: true
            );
        }
        catch (Exception ex)
        {
            return new RuntimeWatchObjectResolver(
                memory,
                workspace: null,
                gameDirectory,
                $"proto/name catalog unavailable ({ex.GetType().Name}: {ex.Message})",
                namesEnabled: false
            );
        }
    }

    public ResolvedObject ResolveHandle(ulong handle)
    {
        if (_objectsByHandle.TryGetValue(handle, out var cached))
            return cached;

        var resolved = ResolveHandleCore(handle);
        _objectsByHandle[handle] = resolved;
        return resolved;
    }

    public void Dispose() => _workspace?.Dispose();

    private ResolvedObject ResolveHandleCore(ulong handle)
    {
        var handleHex = RuntimeWatchEventInterpreter.FormatHandle(handle);
        if (handle == 0)
            return new ResolvedObject(handle, handleHex, null, null, null, null, null, "NullHandle");

        try
        {
            if (!TryReadObjectHeader(handle, out var header))
                return new ResolvedObject(handle, handleHex, null, null, null, null, null, "PoolLookupMiss");

            var proto = TryResolveProto(header.ProtoNumber);
            var objectTypeName = TryFormatObjectTypeName(header.ObjectType) ?? proto.ObjectTypeName;
            var name = ResolveDisplayName(header.ObjectType, objectTypeName, header.ProtoNumber, proto.DisplayName);
            var resolutionSource = ResolveResolutionSource(name, proto, objectTypeName);

            return new ResolvedObject(
                handle,
                handleHex,
                name,
                objectTypeName,
                header.ProtoNumber,
                proto.ProtoAssetPath,
                proto.ArtAssetPath,
                resolutionSource
            );
        }
        catch (Win32Exception)
        {
            return new ResolvedObject(handle, handleHex, null, null, null, null, null, "UnreadableHandle");
        }
        catch (InvalidOperationException)
        {
            return new ResolvedObject(handle, handleHex, null, null, null, null, null, "UnreadableHandle");
        }
    }

    private bool TryReadObjectHeader(ulong handle, out ObjectHeader header)
    {
        header = default;
        if (!LooksLikeHandle(handle) || _objPoolElementByteSize <= ArcanumRuntimeOffsets.ObjPoolEntryHeaderByteSize)
            return false;

        var index = checked((int)(handle >> ArcanumRuntimeOffsets.ObjHandleIndexShift));
        var bucketIndex = index / ArcanumRuntimeOffsets.ObjPoolBucketSize;
        var slotIndex = index % ArcanumRuntimeOffsets.ObjPoolBucketSize;
        var bucketTable = _memory.ReadPointer32(_memory.ResolveRva(ArcanumRuntimeOffsets.ObjPoolBucketsRva));
        if (bucketTable == 0)
            return false;

        var bucketAddress = _memory.ReadPointer32(bucketTable + bucketIndex * sizeof(uint));
        if (bucketAddress == 0)
            return false;

        var entryAddress = bucketAddress + slotIndex * _objPoolElementByteSize;
        var entryHeader = unchecked((uint)_memory.ReadInt32(entryAddress));
        var status = (byte)(entryHeader & 0xFF);
        var sequence = (entryHeader >> 8) & (uint)ArcanumRuntimeOffsets.ObjHandleSequenceMask;
        var expectedSequence = (uint)(
            (handle >> ArcanumRuntimeOffsets.ObjHandleSequenceShift) & ArcanumRuntimeOffsets.ObjHandleSequenceMask
        );
        if (status != StatusHandle || sequence != expectedSequence)
            return false;

        var objectAddress = entryAddress + ArcanumRuntimeOffsets.ObjPoolEntryHeaderByteSize;
        _memory.ReadBytes(objectAddress, _objectHeaderBuffer);

        var objectType = BinaryPrimitives.ReadInt32LittleEndian(
            _objectHeaderBuffer.AsSpan(ObjectTypeOffset, sizeof(int))
        );
        var oid = ReadOid(_objectHeaderBuffer.AsSpan(ObjectOidOffset, ObjectIdSize));
        var prototypeOid = ReadOid(_objectHeaderBuffer.AsSpan(PrototypeOidOffset, ObjectIdSize));
        var prototypeHandle = BinaryPrimitives.ReadUInt64LittleEndian(
            _objectHeaderBuffer.AsSpan(PrototypeHandleOffset, sizeof(ulong))
        );

        header = new ObjectHeader(
            objectType,
            oid.OidType,
            oid.GetProtoNumber(),
            prototypeOid.OidType,
            prototypeOid.GetProtoNumber(),
            prototypeHandle
        );
        return true;
    }

    private ResolvedProto TryResolveProto(int? protoNumber)
    {
        if (!protoNumber.HasValue || protoNumber.Value <= 0)
            return default;

        if (_protosByNumber.TryGetValue(protoNumber.Value, out var cached))
            return cached;

        var entry = _workspace?.FindObjectPaletteEntry(protoNumber.Value);
        var resolved = new ResolvedProto(
            NormalizeText(entry?.DisplayName),
            TryFormatObjectTypeName((int?)entry?.ObjectType),
            entry?.Asset.AssetPath,
            NormalizeText(entry?.ArtAssetPath)
        );
        _protosByNumber[protoNumber.Value] = resolved;
        return resolved;
    }

    private static GameObjectGuid ReadOid(ReadOnlySpan<byte> span)
    {
        var reader = new SpanReader(span);
        return GameObjectGuid.Read(ref reader);
    }

    private static bool LooksLikeHandle(ulong handle) =>
        (handle & ArcanumRuntimeOffsets.ObjHandleMarkerMask) == ArcanumRuntimeOffsets.ObjHandleMarkerValue;

    private static int TryReadObjPoolElementByteSize(ProcessMemory memory)
    {
        try
        {
            return memory.ReadInt32(memory.ResolveRva(ArcanumRuntimeOffsets.ObjPoolElementByteSizeRva));
        }
        catch (Win32Exception)
        {
            return 0;
        }
    }

    private static string? ResolveDisplayName(
        int objectType,
        string? objectTypeName,
        int? protoNumber,
        string? protoDisplayName
    ) =>
        objectType switch
        {
            (int)ObjectType.Pc => "Player Character",
            _ when !string.IsNullOrWhiteSpace(protoDisplayName) => protoDisplayName,
            _ when !string.IsNullOrWhiteSpace(objectTypeName) && protoNumber is > 0 =>
                $"{objectTypeName} proto {protoNumber}",
            _ when !string.IsNullOrWhiteSpace(objectTypeName) => objectTypeName,
            _ when protoNumber is > 0 => $"Proto {protoNumber}",
            _ => null,
        };

    private static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }

    private static string? TryFormatObjectTypeName(int? objectType) =>
        objectType.HasValue && Enum.IsDefined((ObjectType)objectType.Value)
            ? HumanizeObjectType((ObjectType)objectType.Value)
            : null;

    private string ResolveResolutionSource(string? name, ResolvedProto proto, string? objectTypeName)
    {
        if (!string.IsNullOrWhiteSpace(name))
            return "ProtoDisplayName";

        if (!string.IsNullOrWhiteSpace(proto.ProtoAssetPath) || !string.IsNullOrWhiteSpace(proto.ArtAssetPath))
            return "ProtoCatalog";

        if (!string.IsNullOrWhiteSpace(objectTypeName))
            return "LiveObjectType";

        return _workspace is null ? "HandleOnlyCatalogUnavailable" : "HandleOnly";
    }

    private static string HumanizeObjectType(ObjectType objectType) =>
        objectType switch
        {
            ObjectType.KeyRing => "Key Ring",
            ObjectType.Npc => "NPC",
            ObjectType.Pc => "Player Character",
            _ => objectType.ToString(),
        };

    internal readonly record struct ResolvedObject(
        ulong Handle,
        string HandleHex,
        string? Name,
        string? ObjectTypeName,
        int? ProtoNumber,
        string? ProtoAssetPath,
        string? ArtAssetPath,
        string ResolutionSource
    )
    {
        public string SummaryLabel(string fallbackName = "Object")
        {
            if (Handle == 0)
                return "null";

            return $"{BestName(fallbackName)} ({HandleHex})";
        }

        public string SignatureLabel(string fallbackName = "Object")
        {
            if (Handle == 0)
                return "null";

            return $"{BestName(fallbackName)}@{HandleHex}";
        }

        public string BestName(string fallbackName = "Object") =>
            !string.IsNullOrWhiteSpace(Name) ? Name!
            : !string.IsNullOrWhiteSpace(ObjectTypeName) ? ObjectTypeName!
            : fallbackName;
    }

    private readonly record struct ResolvedProto(
        string? DisplayName,
        string? ObjectTypeName,
        string? ProtoAssetPath,
        string? ArtAssetPath
    );

    private readonly record struct ObjectHeader(
        int ObjectType,
        short OidType,
        int? OidProtoNumber,
        short PrototypeOidType,
        int? PrototypeOidProtoNumber,
        ulong PrototypeHandle
    )
    {
        public int? ProtoNumber =>
            PrototypeOidType == GameObjectGuid.OidTypeA ? PrototypeOidProtoNumber
            : OidType == GameObjectGuid.OidTypeA ? OidProtoNumber
            : null;
    }

    private const byte StatusHandle = (byte)'H';
    private const int ObjectHeaderSize = 0x40;
    private const int ObjectIdSize = 0x18;
    private const int ObjectTypeOffset = 0x00;
    private const int ObjectOidOffset = 0x08;
    private const int PrototypeOidOffset = 0x20;
    private const int PrototypeHandleOffset = 0x38;
}
