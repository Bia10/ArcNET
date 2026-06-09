using System.Buffers.Binary;
using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public static class RuntimeActionInvoker
{
    public static RuntimeActionInvocationResult Teleport(
        ProcessMemory memory,
        RuntimeProfileSnapshot runtimeProfile,
        ulong travelerHandle,
        int tileX,
        int tileY,
        int mapId,
        uint flags,
        TimeSpan timeout
    )
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        var function = FunctionCatalog.GetDefinition("teleport_do");
        var teleportData = new byte[0x5C];
        BinaryPrimitives.WriteUInt32LittleEndian(teleportData.AsSpan(0x00), flags);
        BinaryPrimitives.WriteUInt64LittleEndian(teleportData.AsSpan(0x08), travelerHandle);
        BinaryPrimitives.WriteUInt64LittleEndian(teleportData.AsSpan(0x10), PackLocation(tileX, tileY));
        BinaryPrimitives.WriteInt32LittleEndian(teleportData.AsSpan(0x18), mapId);

        using var teleportBuffer = new RemoteAllocation(memory, teleportData.Length);
        memory.WriteBytes(teleportBuffer.Address, teleportData);

        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        var targetAddress = memory.ResolveRva(function.Rva);
        var result = dispatcher.Invoke(
            memory.ToUInt32Address(targetAddress),
            function.SuggestedCleanup,
            0,
            0,
            [teleportBuffer.Address32],
            timeout
        );

        return new RuntimeActionInvocationResult(
            function.Key,
            dispatcher.ModeDescription,
            dispatcher.SiteDescription,
            ProcessMemory.FormatAddress(targetAddress),
            result.ResultEax,
            result.ResultEdx,
            result.State.ToString()
        );
    }

    private static ulong PackLocation(int x, int y) => (uint)x | ((ulong)(uint)y << 32);

    private sealed class RemoteAllocation(ProcessMemory memory, int byteLength) : IDisposable
    {
        public nint Address { get; } = memory.AllocateWritable(byteLength);

        public uint Address32 => memory.ToUInt32Address(Address);

        public void Dispose() => memory.TryFree(Address);
    }
}
