using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.Versioning;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Tests;

[SupportedOSPlatform("windows")]
public sealed class LivePlayerLocatorTests
{
    [Test]
    public async Task Locate_WhenSingleLivePcExists_AutoResolvesHandle()
    {
        const int index = 0;
        const uint sequence = 7;
        var handle = ComposeHandle(index, sequence);
        var prototypeHandle = ComposeHandle(77, 9);
        var objectGuid = new Guid("01234567-89ab-cdef-0123-456789abcdef");
        var memory = new FakeArcanumProcessMemory()
            .MapRva(RuntimeOffsets.ObjPoolElementByteSizeRva, (nint)0x00001000)
            .MapRva(RuntimeOffsets.ObjPoolBucketsRva, (nint)0x00002000)
            .WriteInt32((nint)0x00001000, 0x44)
            .WriteUInt32((nint)0x00002000, 0x00003000)
            .WriteUInt32((nint)0x00003000, 0x00004000)
            .WriteInt32((nint)0x00004000, unchecked((int)((sequence << 8) | (uint)'H')))
            .WriteBytes((nint)0x00004004, CreateObjectHeader(objectGuid, 1234, prototypeHandle));

        var result = LivePlayerLocator.Locate(memory);

        await Assert.That(result.AutoResolvedHandle).IsEqualTo(handle);
        await Assert.That(result.ResolutionSource).IsEqualTo("SingleLivePcInstance");
        await Assert.That(result.LivePlayerCandidates).HasSingleItem();
        await Assert.That(result.LivePlayerCandidates[0].CandidateKind).IsEqualTo("LiveInstance");
    }

    [Test]
    public async Task Locate_WhenOnlyPrototypePcExists_DoesNotAutoResolve()
    {
        const uint sequence = 7;
        var memory = new FakeArcanumProcessMemory()
            .MapRva(RuntimeOffsets.ObjPoolElementByteSizeRva, (nint)0x00001000)
            .MapRva(RuntimeOffsets.ObjPoolBucketsRva, (nint)0x00002000)
            .WriteInt32((nint)0x00001000, 0x44)
            .WriteUInt32((nint)0x00002000, 0x00003000)
            .WriteUInt32((nint)0x00003000, 0x00004000)
            .WriteInt32((nint)0x00004000, unchecked((int)((sequence << 8) | (uint)'H')))
            .WriteBytes((nint)0x00004004, CreatePrototypeTemplateHeader(protoNumber: 1234));

        var result = LivePlayerLocator.Locate(memory);

        await Assert.That(result.AutoResolvedHandle).IsNull();
        await Assert.That(result.ResolutionSource).IsEqualTo("OnlyPrototypePcEntries");
        await Assert.That(result.PrototypeTemplates).HasSingleItem();
        await Assert.That(result.PrototypeTemplates[0].CandidateKind).IsEqualTo("PrototypeTemplate");
        await Assert.That(result.Summary.Contains("prototype templates", StringComparison.Ordinal)).IsTrue();
    }

    private static ulong ComposeHandle(int index, uint sequence) =>
        ((ulong)(uint)index << RuntimeOffsets.ObjHandleIndexShift)
        | ((ulong)sequence << RuntimeOffsets.ObjHandleSequenceShift)
        | RuntimeOffsets.ObjHandleMarkerValue;

    private static byte[] CreateObjectHeader(Guid objectGuid, int prototypeProtoNumber, ulong prototypeHandle)
    {
        var header = new byte[0x40];
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0x00, sizeof(int)), 15);
        WriteGuidOid(header.AsSpan(0x08, 0x18), objectGuid);
        WriteProtoOid(header.AsSpan(0x20, 0x18), prototypeProtoNumber);
        BinaryPrimitives.WriteUInt64LittleEndian(header.AsSpan(0x38, sizeof(ulong)), prototypeHandle);
        return header;
    }

    private static byte[] CreatePrototypeTemplateHeader(int protoNumber)
    {
        var header = new byte[0x40];
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0x00, sizeof(int)), 15);
        WriteProtoOid(header.AsSpan(0x08, 0x18), protoNumber);
        WriteProtoOid(header.AsSpan(0x20, 0x18), protoNumber);
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
