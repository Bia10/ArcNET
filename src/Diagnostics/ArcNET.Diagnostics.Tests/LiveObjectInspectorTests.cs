using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.Versioning;
using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Tests;

[SupportedOSPlatform("windows")]
public sealed class LiveObjectInspectorTests
{
    [Test]
    public async Task Inspect_WhenHandleIsNotHandleShaped_ReturnsShapeFailure()
    {
        var identity = LiveObjectInspector.Inspect(new FakeArcanumProcessMemory(), 1233);

        await Assert.That(identity.HandleHex).IsEqualTo("0x00000000000004D1");
        await Assert.That(identity.LooksLikeHandle).IsFalse();
        await Assert.That(identity.ResolutionSource).IsEqualTo("NotHandleShaped");
        await Assert.That(identity.HasHeader).IsFalse();
    }

    [Test]
    public async Task Inspect_WhenMatchingPoolEntryExists_ReturnsDecodedHeader()
    {
        const int index = 1;
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
            .WriteInt32((nint)0x00004044, unchecked((int)((sequence << 8) | (uint)'H')))
            .WriteBytes((nint)0x00004048, CreateObjectHeader(objectGuid, 1234, prototypeHandle));

        var identity = LiveObjectInspector.Inspect(memory, handle);

        await Assert.That(identity.LooksLikeHandle).IsTrue();
        await Assert.That(identity.ResolutionSource).IsEqualTo("PoolEntry");
        await Assert.That(identity.EntryAddress).IsEqualTo("0x00004044");
        await Assert.That(identity.ObjectAddress).IsEqualTo("0x00004048");
        await Assert.That(identity.Sequence).IsEqualTo(sequence);
        await Assert.That(identity.ExpectedSequence).IsEqualTo(sequence);
        await Assert.That(identity.HasHeader).IsTrue();

        var header = identity.Header!.Value;
        await Assert.That(header.ObjectTypeRaw).IsEqualTo(15);
        await Assert.That(header.ObjectTypeName).IsEqualTo("Pc");
        await Assert.That(header.ObjectId.Label.StartsWith("mob:01234567", StringComparison.Ordinal)).IsTrue();
        await Assert.That(header.PrototypeId.ProtoNumber).IsEqualTo(1234);
        await Assert.That(header.PrototypeId.Label).IsEqualTo("proto#1234");
        await Assert.That(header.PrototypeHandle).IsEqualTo(RuntimeSemanticCatalog.FormatHandle(prototypeHandle));
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
