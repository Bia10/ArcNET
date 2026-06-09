using System.Runtime.InteropServices;

namespace ArcNET.Diagnostics.Windows;

[StructLayout(LayoutKind.Sequential)]
internal struct MemoryBasicInformation
{
    internal nint BaseAddress;
    internal nint AllocationBase;
    internal uint AllocationProtect;
    internal ushort PartitionId;
    internal nuint RegionSize;
    internal MemoryState State;
    internal PageProtection Protect;
    internal uint Type;
}
