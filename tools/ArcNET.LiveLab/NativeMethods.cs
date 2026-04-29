using System.Runtime.InteropServices;

namespace ArcNET.LiveLab;

[Flags]
internal enum ProcessAccess : uint
{
    VmOperation = 0x0008,
    VmRead = 0x0010,
    VmWrite = 0x0020,
    QueryInformation = 0x0400,
}

[Flags]
internal enum AllocationType : uint
{
    Commit = 0x1000,
    Reserve = 0x2000,
    Release = 0x8000,
}

[Flags]
internal enum MemoryProtection : uint
{
    ExecuteReadWrite = 0x40,
}

[Flags]
internal enum PageProtection : uint
{
    NoAccess = 0x01,
    ReadOnly = 0x02,
    ReadWrite = 0x04,
    WriteCopy = 0x08,
    Execute = 0x10,
    ExecuteRead = 0x20,
    ExecuteReadWrite = 0x40,
    ExecuteWriteCopy = 0x80,
    Guard = 0x100,
}

internal enum MemoryState : uint
{
    Commit = 0x1000,
}

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

internal static partial class NativeMethods
{
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nint OpenProcess(
        ProcessAccess desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        int processId
    );

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseHandle(nint handle);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ReadProcessMemory(
        nint processHandle,
        nint baseAddress,
        [Out] byte[] buffer,
        nuint size,
        out nuint bytesRead
    );

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool WriteProcessMemory(
        nint processHandle,
        nint baseAddress,
        byte[] buffer,
        nuint size,
        out nuint bytesWritten
    );

    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nint VirtualAllocEx(
        nint processHandle,
        nint address,
        nuint size,
        AllocationType allocationType,
        MemoryProtection protection
    );

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool VirtualFreeEx(nint processHandle, nint address, nuint size, AllocationType freeType);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool FlushInstructionCache(nint processHandle, nint baseAddress, nuint size);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nuint VirtualQueryEx(
        nint processHandle,
        nint address,
        out MemoryBasicInformation buffer,
        nuint length
    );
}
