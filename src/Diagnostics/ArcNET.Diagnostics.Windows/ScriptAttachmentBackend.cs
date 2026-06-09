using System.Buffers.Binary;
using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class ScriptAttachmentBackend : IScriptAttachmentBackend
{
    public LivePlayerLocatorResult LocatePlayers(int processId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live script-attachment diagnostics currently require Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LivePlayerLocator.Locate(memory);
    }

    public LiveObjectIdentity InspectHandle(int processId, ulong handle)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live script-attachment diagnostics currently require Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LiveObjectInspector.Inspect(memory, handle);
    }

    public ScriptAttachmentPayload ReadAttachment(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int attachmentPoint
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live script-attachment diagnostics currently require Windows.");

        using var memory = ProcessMemory.Attach(processId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        using var remoteBuffer = new RemoteAllocation(memory, ScriptSize);
        memory.WriteBytes(remoteBuffer.Address, new byte[ScriptSize]);

        var invocation = NativeInvoker.Invoke(
            dispatcher,
            memory,
            "obj_array_field_script_get",
            [
                TargetResolver.ToLow32(handle),
                TargetResolver.ToHigh32(handle),
                unchecked((uint)s_scriptFieldId),
                unchecked((uint)attachmentPoint),
                remoteBuffer.Address32,
            ],
            ReadTimeout
        );

        var bytes = memory.ReadBytes(remoteBuffer.Address, ScriptSize);
        var flags = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0, sizeof(uint)));
        var countersPacked = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(sizeof(uint), sizeof(uint)));
        var scriptNumber = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(sizeof(uint) * 2, sizeof(int)));
        var counters = new[]
        {
            (int)(countersPacked & 0xFF),
            (int)((countersPacked >> 8) & 0xFF),
            (int)((countersPacked >> 16) & 0xFF),
            (int)((countersPacked >> 24) & 0xFF),
        };

        return new(
            new ScriptAttachmentRecordSnapshot(
                scriptNumber,
                flags,
                $"0x{flags:X8}",
                countersPacked,
                $"0x{countersPacked:X8}",
                counters,
                IsEmpty: scriptNumber == 0 && flags == 0 && countersPacked == 0
            ),
            invocation.Snapshot
        );
    }

    private static int ResolveFieldId(string rawName)
    {
        if (ObjectFieldCatalog.TryGetFieldId(rawName, out var fieldId))
            return fieldId;

        throw new InvalidOperationException($"Unable to resolve runtime object field '{rawName}'.");
    }

    private static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(1);
    private const int ScriptSize = 12;
    private static readonly int s_scriptFieldId = ResolveFieldId("OBJ_F_SCRIPTS_IDX");
}
