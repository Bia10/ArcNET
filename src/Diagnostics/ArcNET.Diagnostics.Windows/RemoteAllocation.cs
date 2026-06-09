namespace ArcNET.Diagnostics.Windows;

internal sealed class RemoteAllocation(ProcessMemory memory, int byteLength) : IDisposable
{
    public nint Address { get; } = memory.AllocateWritable(byteLength);

    public uint Address32 => memory.ToUInt32Address(Address);

    public void Dispose() => memory.TryFree(Address);
}
