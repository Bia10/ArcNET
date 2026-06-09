using System.Runtime.InteropServices;

namespace ArcNET.Diagnostics.Windows;

internal static partial class Kernel32NativeMethods
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
    internal static partial bool VirtualProtectEx(
        nint processHandle,
        nint address,
        nuint size,
        PageProtection newProtect,
        out PageProtection oldProtect
    );

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool FlushInstructionCache(nint processHandle, nint baseAddress, nuint size);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nint CreateRemoteThread(
        nint processHandle,
        nint threadAttributes,
        nuint stackSize,
        nint startAddress,
        nint parameter,
        uint creationFlags,
        out uint threadId
    );

    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial uint WaitForSingleObject(nint handle, uint milliseconds);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetExitCodeThread(nint threadHandle, out uint exitCode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nuint VirtualQueryEx(
        nint processHandle,
        nint address,
        out MemoryBasicInformation buffer,
        nuint length
    );
}
