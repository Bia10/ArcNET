using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.Versioning;
using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Tests;

[SupportedOSPlatform("windows")]
public sealed class LiveMobileRosterScannerTests
{
    [Test]
    public async Task Scan_WhenPoolContainsLivePcAndNpc_ReturnsOnlyLivePrototypeBackedMobiles()
    {
        const uint livePcSequence = 7;
        const uint liveNpcSequence = 9;
        const uint templateSequence = 11;
        const uint weaponSequence = 13;
        var livePcHandle = ComposeHandle(0, livePcSequence);
        var liveNpcHandle = ComposeHandle(1, liveNpcSequence);
        var livePcPrototypeHandle = ComposeHandle(77, 3);
        var liveNpcPrototypeHandle = ComposeHandle(91, 5);
        var weaponPrototypeHandle = ComposeHandle(101, 7);
        var memory = new FakeArcanumProcessMemory()
            .MapRva(RuntimeOffsets.ObjPoolElementByteSizeRva, (nint)0x00001000)
            .MapRva(RuntimeOffsets.ObjPoolBucketsRva, (nint)0x00002000)
            .WriteInt32((nint)0x00001000, 0x44)
            .WriteUInt32((nint)0x00002000, 0x00003000)
            .WriteUInt32((nint)0x00003000, 0x00004000)
            .WritePoolEntry((nint)0x00004000, 15, livePcSequence, 1000, livePcPrototypeHandle)
            .WritePoolEntry((nint)0x00004044, 16, liveNpcSequence, 2000, liveNpcPrototypeHandle)
            .WritePoolEntry((nint)0x00004088, 15, templateSequence, 3000, 0)
            .WritePoolEntry((nint)0x000040CC, 5, weaponSequence, 14001, weaponPrototypeHandle);

        var mobiles = LiveMobileRosterScanner.Scan(memory, maxEntries: 16);

        await Assert.That(mobiles).Count().IsEqualTo(2);
        await Assert
            .That(mobiles.Select(static mobile => mobile.HandleHex))
            .IsEquivalentTo([
                RuntimeSemanticCatalog.FormatHandle(liveNpcHandle),
                RuntimeSemanticCatalog.FormatHandle(livePcHandle),
            ]);
        await Assert
            .That(mobiles.Select(static mobile => mobile.Header?.ObjectTypeName ?? string.Empty))
            .IsEquivalentTo(["Npc", "Pc"]);
    }

    private static ulong ComposeHandle(int index, uint sequence) =>
        ((ulong)(uint)index << RuntimeOffsets.ObjHandleIndexShift)
        | ((ulong)sequence << RuntimeOffsets.ObjHandleSequenceShift)
        | RuntimeOffsets.ObjHandleMarkerValue;

    private static byte[] CreateObjectHeader(int objectTypeRaw, int prototypeProtoNumber, ulong prototypeHandle)
    {
        var header = new byte[0x40];
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0x00, sizeof(int)), objectTypeRaw);
        WriteGuidOid(header.AsSpan(0x08, 0x18), Guid.NewGuid());
        WriteProtoOid(header.AsSpan(0x20, 0x18), prototypeProtoNumber);
        BinaryPrimitives.WriteUInt64LittleEndian(header.AsSpan(0x38, sizeof(ulong)), prototypeHandle);
        return header;
    }

    private static void WriteGuidOid(Span<byte> destination, Guid guid)
    {
        BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(0x00, sizeof(short)), 2);
        BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(0x02, sizeof(short)), 0);
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(0x04, sizeof(int)), 0);
        guid.TryWriteBytes(destination.Slice(0x08, 16));
    }

    private static void WriteProtoOid(Span<byte> destination, int protoNumber)
    {
        BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(0x00, sizeof(short)), 1);
        BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(0x02, sizeof(short)), 0);
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(0x04, sizeof(int)), 0);
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(0x08, sizeof(int)), protoNumber);
    }

    private sealed class FakeArcanumProcessMemory : IProcessMemory
    {
        private readonly Dictionary<long, byte> _bytes = [];
        private readonly Dictionary<int, nint> _rvas = [];

        public FakeArcanumProcessMemory MapRva(int rva, nint address)
        {
            _rvas[rva] = address;
            return this;
        }

        public FakeArcanumProcessMemory WriteInt32(nint address, int value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
            return WriteBytes(address, bytes);
        }

        public FakeArcanumProcessMemory WriteUInt32(nint address, uint value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
            return WriteBytes(address, bytes);
        }

        public FakeArcanumProcessMemory WritePoolEntry(
            nint entryAddress,
            int objectTypeRaw,
            uint sequence,
            int prototypeProtoNumber,
            ulong prototypeHandle
        )
        {
            WriteInt32(entryAddress, unchecked((int)((sequence << 8) | (uint)'H')));
            return WriteBytes(
                entryAddress + RuntimeOffsets.ObjPoolEntryHeaderByteSize,
                CreateObjectHeader(objectTypeRaw, prototypeProtoNumber, prototypeHandle)
            );
        }

        public FakeArcanumProcessMemory WriteBytes(nint address, ReadOnlySpan<byte> bytes)
        {
            var baseAddress = (long)address;
            for (var index = 0; index < bytes.Length; index++)
                _bytes[baseAddress + index] = bytes[index];

            return this;
        }

        public nint ResolveRva(int rva) =>
            _rvas.TryGetValue(rva, out var address)
                ? address
                : throw new InvalidOperationException($"RVA 0x{rva:X8} is not mapped in the fake memory reader.");

        public int ReadInt32(nint address)
        {
            var buffer = new byte[sizeof(int)];
            ReadBytes(address, buffer);
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        public nint ReadPointer32(nint address) => (nint)(long)unchecked((uint)ReadInt32(address));

        public void ReadBytes(nint address, byte[] buffer)
        {
            var baseAddress = (long)address;
            for (var index = 0; index < buffer.Length; index++)
            {
                if (!_bytes.TryGetValue(baseAddress + index, out var value))
                    throw new Win32Exception(299, $"Fake memory has no data at 0x{baseAddress + index:X8}.");

                buffer[index] = value;
            }
        }
    }
}
