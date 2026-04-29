using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ArcNET.LiveLab;

/// <summary>Thin typed wrapper over Arcanum's process memory.</summary>
[SupportedOSPlatform("windows")]
internal sealed class ProcessMemory : IDisposable
{
    internal readonly record struct MemoryRegion(nint BaseAddress, nuint Size, PageProtection Protect);

    private readonly Process _process;

    private ProcessMemory(Process process, nint handle, nint moduleBase, string modulePath)
    {
        _process = process;
        Handle = handle;
        ModuleBase = moduleBase;
        ModulePath = modulePath;
    }

    public int ProcessId => _process.Id;

    public string ProcessName => _process.ProcessName;

    public nint Handle { get; }

    public nint ModuleBase { get; }

    public string ModulePath { get; }

    public static ProcessMemory Attach(string processName = ArcanumRuntimeOffsets.ProcessName)
    {
        var normalized = Path.GetFileNameWithoutExtension(processName);
        var process = Process.GetProcessesByName(normalized).OrderBy(p => p.Id).FirstOrDefault();
        if (process is null)
            throw new InvalidOperationException($"Process '{normalized}.exe' is not running.");

        var handle = NativeMethods.OpenProcess(
            ProcessAccess.QueryInformation | ProcessAccess.VmRead | ProcessAccess.VmWrite | ProcessAccess.VmOperation,
            false,
            process.Id
        );
        if (handle == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to open process {process.Id}.");

        try
        {
            var module = process.MainModule ?? throw new InvalidOperationException("Unable to locate the main module.");
            return new ProcessMemory(process, handle, module.BaseAddress, module.FileName);
        }
        catch
        {
            NativeMethods.CloseHandle(handle);
            process.Dispose();
            throw;
        }
    }

    public nint ResolveRva(int rva) => ModuleBase + rva;

    public uint ResolveRva32(int rva) => ToUInt32Address(ResolveRva(rva));

    public byte[] ReadBytes(nint address, int count)
    {
        var buffer = new byte[count];
        if (
            !NativeMethods.ReadProcessMemory(Handle, address, buffer, (nuint)count, out var bytesRead)
            || bytesRead != (nuint)count
        )
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                $"Failed to read {count} byte(s) at {FormatAddress(address)}."
            );

        return buffer;
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

    public void WriteBytes(nint address, ReadOnlySpan<byte> bytes)
    {
        var buffer = bytes.ToArray();
        if (
            !NativeMethods.WriteProcessMemory(Handle, address, buffer, (nuint)buffer.Length, out var bytesWritten)
            || bytesWritten != (nuint)buffer.Length
        )
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                $"Failed to write {buffer.Length} byte(s) at {FormatAddress(address)}."
            );

        _ = NativeMethods.FlushInstructionCache(Handle, address, (nuint)buffer.Length);
    }

    public void WriteInt32(nint address, int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        WriteBytes(address, buffer);
    }

    public IEnumerable<MemoryRegion> EnumerateCommittedReadableRegions()
    {
        const ulong maxUserAddress = uint.MaxValue;
        var cursor = 0UL;
        var infoSize = (nuint)Marshal.SizeOf<MemoryBasicInformation>();

        while (cursor <= maxUserAddress)
        {
            var queryAddress = (nint)(long)cursor;
            var result = NativeMethods.VirtualQueryEx(Handle, queryAddress, out var info, infoSize);
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

    public bool TryGetReadableRegion(nint address, out MemoryRegion region)
    {
        var infoSize = (nuint)Marshal.SizeOf<MemoryBasicInformation>();
        if (NativeMethods.VirtualQueryEx(Handle, address, out var info, infoSize) == 0)
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

    public nint AllocateExecutable(int size)
    {
        var address = NativeMethods.VirtualAllocEx(
            Handle,
            0,
            (nuint)size,
            AllocationType.Commit | AllocationType.Reserve,
            MemoryProtection.ExecuteReadWrite
        );
        if (address == 0)
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                $"Failed to allocate {size} byte(s) in the remote process."
            );

        return address;
    }

    public void Free(nint address)
    {
        if (address == 0)
            return;

        if (!NativeMethods.VirtualFreeEx(Handle, address, 0, AllocationType.Release))
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                $"Failed to free remote block at {FormatAddress(address)}."
            );
    }

    public uint ToUInt32Address(nint address)
    {
        var value = (long)address;
        return checked((uint)value);
    }

    public static string FormatAddress(nint address)
    {
        var value = (ulong)(long)address;
        return value <= uint.MaxValue ? $"0x{value:X8}" : $"0x{value:X16}";
    }

    private static bool IsReadable(PageProtection protect)
    {
        if ((protect & (PageProtection.NoAccess | PageProtection.Guard)) != 0)
            return false;

        return (protect & ReadableProtectionMask) != 0;
    }

    private const PageProtection ReadableProtectionMask =
        PageProtection.ReadOnly
        | PageProtection.ReadWrite
        | PageProtection.WriteCopy
        | PageProtection.ExecuteRead
        | PageProtection.ExecuteReadWrite
        | PageProtection.ExecuteWriteCopy;

    public void Dispose()
    {
        _ = NativeMethods.CloseHandle(Handle);
        _process.Dispose();
    }
}
