using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ArcNET.Diagnostics.Windows;

/// <summary>Thin typed wrapper over an attached Arcanum process.</summary>
public sealed class ProcessMemory : IDisposable, IProcessMemory
{
    internal readonly record struct MemoryRegion(nint BaseAddress, nuint Size, PageProtection Protect);

    private readonly Process _process;

    private ProcessMemory(Process process, nint handle, nint moduleBase, int moduleSize, string modulePath)
    {
        _process = process;
        Handle = handle;
        ModuleBase = moduleBase;
        ModuleSize = moduleSize;
        ModulePath = modulePath;
    }

    public int ProcessId => _process.Id;

    public string ProcessName => _process.ProcessName;

    public bool HasExited
    {
        get
        {
            try
            {
                return _process.HasExited;
            }
            catch
            {
                return true;
            }
        }
    }

    internal nint Handle { get; }

    public nint ModuleBase { get; }

    public int ModuleSize { get; }

    public string ModulePath { get; }

    public static ProcessMemory Attach(string processName = RuntimeOffsets.ProcessName)
    {
        if (!TryAttach(out var memory, processName))
            throw new InvalidOperationException($"Process '{NormalizeProcessName(processName)}.exe' is not running.");

        return memory;
    }

    public static ProcessMemory Attach(int processId)
    {
        try
        {
            return Attach(Process.GetProcessById(processId));
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"Process id {processId} is not running.", ex);
        }
    }

    public static bool TryAttach(out ProcessMemory memory, string processName = RuntimeOffsets.ProcessName)
    {
        var normalized = NormalizeProcessName(processName);
        var process = Process.GetProcessesByName(normalized).OrderBy(static process => process.Id).FirstOrDefault();
        if (process is null)
        {
            memory = null!;
            return false;
        }

        memory = Attach(process);
        return true;
    }

    public static bool TryAttach(int processId, out ProcessMemory memory)
    {
        try
        {
            memory = Attach(processId);
            return true;
        }
        catch (InvalidOperationException)
        {
            memory = null!;
            return false;
        }
    }

    private static ProcessMemory Attach(Process process)
    {
        var handle = Kernel32NativeMethods.OpenProcess(
            ProcessAccess.QueryInformation | ProcessAccess.VmRead | ProcessAccess.VmWrite | ProcessAccess.VmOperation,
            false,
            process.Id
        );
        if (handle == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to open process {process.Id}.");

        try
        {
            var module = process.MainModule ?? throw new InvalidOperationException("Unable to locate the main module.");
            return new ProcessMemory(process, handle, module.BaseAddress, module.ModuleMemorySize, module.FileName);
        }
        catch
        {
            Kernel32NativeMethods.CloseHandle(handle);
            process.Dispose();
            throw;
        }
    }

    public nint ResolveRva(int rva) => ModuleBase + rva;

    public byte[] ReadBytes(nint address, int count)
    {
        var buffer = new byte[count];
        ReadBytes(address, buffer);
        return buffer;
    }

    public byte[] ReadModuleBytes() => ReadBytes(ModuleBase, ModuleSize);

    public void ReadBytes(nint address, byte[] buffer)
    {
        if (
            !Kernel32NativeMethods.ReadProcessMemory(Handle, address, buffer, (nuint)buffer.Length, out var bytesRead)
            || bytesRead != (nuint)buffer.Length
        )
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(
                errorCode,
                $"Failed to read {buffer.Length} byte(s) at {FormatAddress(address)} (Win32 {errorCode})."
            );
        }
    }

    public int ReadInt32(nint address) => BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(address, sizeof(int)));

    public uint ReadUInt32(nint address) => BinaryPrimitives.ReadUInt32LittleEndian(ReadBytes(address, sizeof(uint)));

    public nint ReadPointer32(nint address) => (nint)(long)ReadUInt32(address);

    public string ReadAsciiZ(nint address, int maxLength = 256)
    {
        var buffer = ReadBytes(address, maxLength);
        var zeroIndex = Array.IndexOf(buffer, (byte)0);
        var count = zeroIndex >= 0 ? zeroIndex : buffer.Length;
        return System.Text.Encoding.ASCII.GetString(buffer, 0, count);
    }

    internal void WriteBytes(nint address, ReadOnlySpan<byte> bytes)
    {
        var buffer = bytes.ToArray();
        if (
            !Kernel32NativeMethods.WriteProcessMemory(
                Handle,
                address,
                buffer,
                (nuint)buffer.Length,
                out var bytesWritten
            )
            || bytesWritten != (nuint)buffer.Length
        )
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(
                errorCode,
                $"Failed to write {buffer.Length} byte(s) at {FormatAddress(address)} (Win32 {errorCode})."
            );
        }

        _ = Kernel32NativeMethods.FlushInstructionCache(Handle, address, (nuint)buffer.Length);
    }

    internal void WriteInt32(nint address, int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        WriteBytes(address, buffer);
    }

    internal void WriteCodeBytes(nint address, ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
            return;

        if (
            !Kernel32NativeMethods.VirtualProtectEx(
                Handle,
                address,
                (nuint)bytes.Length,
                PageProtection.ExecuteReadWrite,
                out var originalProtect
            )
        )
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(
                errorCode,
                $"Failed to change page protection at {FormatAddress(address)} (Win32 {errorCode})."
            );
        }

        try
        {
            WriteBytes(address, bytes);
        }
        finally
        {
            _ = Kernel32NativeMethods.VirtualProtectEx(Handle, address, (nuint)bytes.Length, originalProtect, out _);
        }
    }

    internal bool TryWriteCodeBytes(nint address, ReadOnlySpan<byte> bytes)
    {
        try
        {
            WriteCodeBytes(address, bytes);
            return true;
        }
        catch (Win32Exception ex) when (ShouldIgnoreRemoteFailure(ex.NativeErrorCode))
        {
            return false;
        }
    }

    internal IEnumerable<MemoryRegion> EnumerateCommittedReadableRegions()
    {
        const ulong MaxUserAddress = uint.MaxValue;
        var cursor = 0UL;
        var infoSize = (nuint)Marshal.SizeOf<MemoryBasicInformation>();

        while (cursor <= MaxUserAddress)
        {
            var queryAddress = (nint)(long)cursor;
            var result = Kernel32NativeMethods.VirtualQueryEx(Handle, queryAddress, out var info, infoSize);
            if (result == 0)
                yield break;

            var baseAddress = (ulong)ToUInt32Address(info.BaseAddress);
            var regionSize = (ulong)info.RegionSize;
            if (regionSize == 0)
                yield break;

            if (info.State == MemoryState.Commit && IsReadable(info.Protect))
                yield return new MemoryRegion(info.BaseAddress, info.RegionSize, info.Protect);

            var next = baseAddress + regionSize;
            if (next <= cursor)
                yield break;

            cursor = next;
        }
    }

    internal bool TryGetReadableRegion(nint address, out MemoryRegion region)
    {
        var infoSize = (nuint)Marshal.SizeOf<MemoryBasicInformation>();
        if (Kernel32NativeMethods.VirtualQueryEx(Handle, address, out var info, infoSize) == 0)
        {
            region = default;
            return false;
        }

        if (info.State != MemoryState.Commit || !IsReadable(info.Protect))
        {
            region = default;
            return false;
        }

        var regionBase = (ulong)ToUInt32Address(info.BaseAddress);
        var regionSize = (ulong)info.RegionSize;
        var value = (ulong)ToUInt32Address(address);
        if (value < regionBase || value >= regionBase + regionSize)
        {
            region = default;
            return false;
        }

        region = new MemoryRegion(info.BaseAddress, info.RegionSize, info.Protect);
        return true;
    }

    internal nint Allocate(int size, MemoryProtection protection)
    {
        var address = Kernel32NativeMethods.VirtualAllocEx(
            Handle,
            0,
            (nuint)size,
            AllocationType.Commit | AllocationType.Reserve,
            protection
        );
        if (address == 0)
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                $"Failed to allocate {size} byte(s) in the remote process."
            );
        }

        return address;
    }

    internal nint AllocateExecutable(int size) => Allocate(size, MemoryProtection.ExecuteReadWrite);

    internal nint AllocateWritable(int size) => Allocate(size, MemoryProtection.ReadWrite);

    internal void Free(nint address)
    {
        if (address == 0)
            return;

        if (!Kernel32NativeMethods.VirtualFreeEx(Handle, address, 0, AllocationType.Release))
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(
                errorCode,
                $"Failed to free remote block at {FormatAddress(address)} (Win32 {errorCode})."
            );
        }
    }

    internal bool TryFree(nint address)
    {
        try
        {
            Free(address);
            return true;
        }
        catch (Win32Exception ex) when (ShouldIgnoreRemoteFailure(ex.NativeErrorCode))
        {
            return false;
        }
    }

    internal uint ToUInt32Address(nint address)
    {
        var value = (long)address;
        return checked((uint)value);
    }

    public static string FormatAddress(nint address)
    {
        var value = (ulong)(long)address;
        return value <= uint.MaxValue ? $"0x{value:X8}" : $"0x{value:X16}";
    }

    private static string NormalizeProcessName(string processName)
    {
        var normalized = Path.GetFileNameWithoutExtension(processName);
        if (normalized.Length == 0)
            throw new InvalidOperationException("Process name must not be empty.");

        return normalized;
    }

    private static bool IsReadable(PageProtection protect)
    {
        if ((protect & (PageProtection.NoAccess | PageProtection.Guard)) != 0)
            return false;

        return (protect & ReadableProtectionMask) != 0;
    }

    private bool ShouldIgnoreRemoteFailure(int errorCode) =>
        HasExited || s_ignorableRemoteExitErrors.Contains(errorCode);

    public void Dispose()
    {
        _ = Kernel32NativeMethods.CloseHandle(Handle);
        _process.Dispose();
    }

    private const PageProtection ReadableProtectionMask =
        PageProtection.ReadOnly
        | PageProtection.ReadWrite
        | PageProtection.WriteCopy
        | PageProtection.ExecuteRead
        | PageProtection.ExecuteReadWrite
        | PageProtection.ExecuteWriteCopy;

    private static readonly HashSet<int> s_ignorableRemoteExitErrors = [5, 6, 87, 299, 487, 998];
}
