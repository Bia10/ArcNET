using System.Buffers.Binary;
using System.IO;
using System.Runtime.Versioning;
using Iced.Intel;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal sealed class RuntimeWatchSession : IDisposable
{
    private readonly RemoteEventBuffer _buffer;
    private readonly EntryDetourHook[] _hooks;
    private readonly Dictionary<RuntimeWatchHookId, RuntimeWatchHookDefinition> _definitionsById;
    private bool _disposed;

    private RuntimeWatchSession(
        RemoteEventBuffer buffer,
        EntryDetourHook[] hooks,
        Dictionary<RuntimeWatchHookId, RuntimeWatchHookDefinition> definitionsById
    )
    {
        _buffer = buffer;
        _hooks = hooks;
        _definitionsById = definitionsById;
    }

    public static RuntimeWatchSession Install(ProcessMemory memory, IReadOnlyList<RuntimeWatchHookDefinition> hooks)
    {
        if (hooks.Count == 0)
            throw new InvalidOperationException("At least one watch hook must be selected.");

        var definitionsById = hooks.ToDictionary(static hook => hook.Id);
        var buffer = RemoteEventBuffer.Install(memory);
        List<EntryDetourHook> installedHooks = [];
        try
        {
            foreach (var hook in hooks)
                installedHooks.Add(EntryDetourHook.Install(memory, hook, buffer));

            return new RuntimeWatchSession(buffer, [.. installedHooks], definitionsById);
        }
        catch
        {
            foreach (var hook in installedHooks)
                hook.Dispose();

            buffer.Dispose();
            throw;
        }
    }

    public RuntimeWatchReadResult ReadSince(uint lastSequence)
    {
        var snapshot = _buffer.ReadSnapshot();
        var writeSequence = snapshot.WriteSequence;
        if (writeSequence <= lastSequence)
            return new RuntimeWatchReadResult(writeSequence, 0, 0, []);

        var firstSequence = lastSequence + 1;
        var droppedEvents = 0;
        if (writeSequence - lastSequence > RemoteEventBuffer.Capacity)
        {
            firstSequence = writeSequence - RemoteEventBuffer.Capacity + 1;
            droppedEvents = checked((int)(firstSequence - (lastSequence + 1)));
        }

        var inconsistentRecords = 0;
        List<RuntimeWatchCapturedEvent> events = new(checked((int)(writeSequence - firstSequence + 1)));
        for (var sequence = firstSequence; sequence <= writeSequence; sequence++)
        {
            var record = snapshot.ReadRecord(sequence);
            if (record.Sequence != sequence || !_definitionsById.TryGetValue(record.HookId, out var definition))
            {
                inconsistentRecords++;
                continue;
            }

            events.Add(new RuntimeWatchCapturedEvent(definition, record.Sequence, record.StackDwords));
        }

        return new RuntimeWatchReadResult(
            writeSequence,
            droppedEvents,
            inconsistentRecords,
            events.Count == 0 ? [] : [.. events]
        );
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var hook in _hooks.Reverse())
            hook.Dispose();

        _buffer.Dispose();
        _disposed = true;
    }

    internal readonly record struct RuntimeWatchReadResult(
        uint WriteSequence,
        int DroppedEvents,
        int InconsistentRecords,
        RuntimeWatchCapturedEvent[] Events
    );

    internal readonly record struct RuntimeWatchCapturedEvent(
        RuntimeWatchHookDefinition Definition,
        uint Sequence,
        RuntimeWatchStackCapture StackDwords
    );

    private readonly record struct RuntimeWatchRawRecord(
        uint Sequence,
        RuntimeWatchHookId HookId,
        RuntimeWatchStackCapture StackDwords
    );

    internal struct RuntimeWatchStackCapture
    {
        public uint D0;
        public uint D1;
        public uint D2;
        public uint D3;
        public uint D4;
        public uint D5;
        public uint D6;
        public uint D7;

        public readonly uint Get(int index) =>
            index switch
            {
                0 => D0,
                1 => D1,
                2 => D2,
                3 => D3,
                4 => D4,
                5 => D5,
                6 => D6,
                7 => D7,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };

        public void Set(int index, uint value)
        {
            switch (index)
            {
                case 0:
                    D0 = value;
                    break;
                case 1:
                    D1 = value;
                    break;
                case 2:
                    D2 = value;
                    break;
                case 3:
                    D3 = value;
                    break;
                case 4:
                    D4 = value;
                    break;
                case 5:
                    D5 = value;
                    break;
                case 6:
                    D6 = value;
                    break;
                case 7:
                    D7 = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    private sealed class RemoteEventBuffer : IDisposable
    {
        public const int StackCaptureDwordCount = 8;
        public const int RecordSize = sizeof(uint) * (2 + StackCaptureDwordCount);
        public const int Capacity = 256;

        private const int LockOffset = 0;
        private const int WriteSequenceOffset = sizeof(uint);
        private const int RecordsOffset = sizeof(uint) * 2;
        private const int CapacityMask = Capacity - 1;

        private readonly ProcessMemory _memory;
        private readonly nint _remoteBlock;
        private readonly byte[] _snapshotBytes;
        private bool _disposed;

        private RemoteEventBuffer(ProcessMemory memory, nint remoteBlock, int snapshotSize)
        {
            _memory = memory;
            _remoteBlock = remoteBlock;
            _snapshotBytes = new byte[snapshotSize];
        }

        public static RemoteEventBuffer Install(ProcessMemory memory)
        {
            var size = RecordsOffset + RecordSize * Capacity;
            return new RemoteEventBuffer(memory, memory.AllocateWritable(size), size);
        }

        public uint LockAddress => _memory.ToUInt32Address(_remoteBlock + LockOffset);

        public uint WriteSequenceAddress => _memory.ToUInt32Address(_remoteBlock + WriteSequenceOffset);

        public uint RecordsAddress => _memory.ToUInt32Address(_remoteBlock + RecordsOffset);

        public BufferSnapshot ReadSnapshot()
        {
            _memory.ReadBytes(_remoteBlock, _snapshotBytes);
            var writeSequence = BinaryPrimitives.ReadUInt32LittleEndian(
                _snapshotBytes.AsSpan(WriteSequenceOffset, sizeof(uint))
            );
            return new BufferSnapshot(_snapshotBytes, writeSequence);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _memory.TryFree(_remoteBlock);
            _disposed = true;
        }

        public readonly record struct BufferSnapshot(byte[] Bytes, uint WriteSequence)
        {
            public RuntimeWatchRawRecord ReadRecord(uint sequence)
            {
                var slotIndex = checked((int)((sequence - 1) & CapacityMask));
                var offset = RecordsOffset + slotIndex * RecordSize;
                var span = Bytes.AsSpan(offset, RecordSize);
                var recordSequence = BinaryPrimitives.ReadUInt32LittleEndian(span[..sizeof(uint)]);
                var hookId = (RuntimeWatchHookId)
                    BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(sizeof(uint), sizeof(uint)));
                RuntimeWatchStackCapture stackDwords = default;
                for (var index = 0; index < StackCaptureDwordCount; index++)
                {
                    stackDwords.Set(
                        index,
                        BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(sizeof(uint) * (index + 2), sizeof(uint)))
                    );
                }

                return new RuntimeWatchRawRecord(recordSequence, hookId, stackDwords);
            }
        }
    }

    private sealed class EntryDetourHook : IDisposable
    {
        private readonly ProcessMemory _memory;
        private readonly nint _siteAddress;
        private readonly byte[] _originalBytes;
        private readonly nint _remoteBlock;
        private bool _disposed;

        private EntryDetourHook(ProcessMemory memory, nint siteAddress, byte[] originalBytes, nint remoteBlock)
        {
            _memory = memory;
            _siteAddress = siteAddress;
            _originalBytes = originalBytes;
            _remoteBlock = remoteBlock;
        }

        public static EntryDetourHook Install(
            ProcessMemory memory,
            RuntimeWatchHookDefinition definition,
            RemoteEventBuffer buffer
        )
        {
            var siteAddress = memory.ResolveRva(definition.Rva);
            var prefix = DecodePatchPrefix(memory, siteAddress);
            var remoteBlock = memory.AllocateExecutable(0x400);
            try
            {
                var trampoline = BuildTrampolineBlock(memory, remoteBlock, siteAddress, definition, buffer, prefix);
                memory.WriteBytes(remoteBlock, trampoline);
                WriteJumpPatch(memory, siteAddress, remoteBlock, prefix.HookLength);
                return new EntryDetourHook(memory, siteAddress, prefix.OriginalBytes, remoteBlock);
            }
            catch
            {
                memory.Free(remoteBlock);
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _memory.TryWriteCodeBytes(_siteAddress, _originalBytes);
            _memory.TryFree(_remoteBlock);
            _disposed = true;
        }

        private static DecodedPatchPrefix DecodePatchPrefix(ProcessMemory memory, nint siteAddress)
        {
            var bytes = memory.ReadBytes(siteAddress, 64);
            var decoder = Decoder.Create(32, bytes, (ulong)memory.ToUInt32Address(siteAddress), DecoderOptions.None);
            var hookLength = 0;
            List<Instruction> instructions = [];
            while (hookLength < 5)
            {
                decoder.Decode(out var instruction);
                if (instruction.IsInvalid)
                {
                    throw new InvalidOperationException(
                        $"Unable to decode function entry at {ProcessMemory.FormatAddress(siteAddress)}."
                    );
                }

                instructions.Add(instruction);
                hookLength += instruction.Length;
            }

            return new DecodedPatchPrefix(hookLength, bytes[..hookLength], [.. instructions]);
        }

        private static byte[] BuildTrampolineBlock(
            ProcessMemory memory,
            nint remoteBlock,
            nint siteAddress,
            RuntimeWatchHookDefinition definition,
            RemoteEventBuffer buffer,
            DecodedPatchPrefix prefix
        )
        {
            var prelude = AssemblePrelude(memory.ToUInt32Address(remoteBlock), definition, buffer);
            var relocatedOriginalBytes = RelocateOriginalInstructions(
                memory.ToUInt32Address(remoteBlock) + (uint)prelude.Length,
                prefix.Instructions
            );
            var tailBase = memory.ToUInt32Address(remoteBlock) + (uint)(prelude.Length + relocatedOriginalBytes.Length);
            var tail = AssembleTail(tailBase, memory.ToUInt32Address(siteAddress) + (uint)prefix.HookLength);
            var block = new byte[prelude.Length + relocatedOriginalBytes.Length + tail.Length];
            prelude.CopyTo(block, 0);
            relocatedOriginalBytes.CopyTo(block, prelude.Length);
            tail.CopyTo(block, prelude.Length + relocatedOriginalBytes.Length);
            return block;
        }

        private static byte[] AssemblePrelude(
            uint codeBase,
            RuntimeWatchHookDefinition definition,
            RemoteEventBuffer buffer
        )
        {
            return AssembleCode(
                codeBase,
                assembler =>
                {
                    var eax = AssemblerRegisters.eax;
                    var ecx = AssemblerRegisters.ecx;
                    var edx = AssemblerRegisters.edx;
                    var esp = AssemblerRegisters.esp;

                    var spinLabel = assembler.CreateLabel("spin");

                    assembler.pushfd();
                    assembler.pushad();

                    assembler.Label(ref spinLabel);
                    assembler.mov(eax, 1);
                    assembler.xchg(DWordPtr(buffer.LockAddress), eax);
                    assembler.test(eax, eax);
                    assembler.jne(spinLabel);

                    assembler.mov(ecx, DWordPtr(buffer.WriteSequenceAddress));
                    assembler.lea(ecx, ecx + 1);
                    assembler.mov(eax, ecx);
                    assembler.dec(eax);
                    assembler.and(eax, RemoteEventBuffer.Capacity - 1);
                    assembler.lea(eax, eax + eax * 4);
                    assembler.shl(eax, 3);
                    assembler.add(eax, unchecked((int)buffer.RecordsAddress));

                    assembler.mov(DWordPtr(eax, 0), ecx);
                    assembler.mov(DWordPtr(eax, 4), (int)definition.Id);
                    for (var index = 0; index < RemoteEventBuffer.StackCaptureDwordCount; index++)
                    {
                        assembler.mov(edx, DWordPtr(esp, 40 + index * sizeof(uint)));
                        assembler.mov(DWordPtr(eax, sizeof(uint) * (index + 2)), edx);
                    }

                    assembler.mov(DWordPtr(buffer.WriteSequenceAddress), ecx);
                    assembler.mov(DWordPtr(buffer.LockAddress), 0);
                    assembler.popad();
                    assembler.popfd();
                }
            );
        }

        private static byte[] AssembleTail(uint codeBase, uint returnAddress) =>
            AssembleCode(codeBase, assembler => assembler.jmp((ulong)returnAddress));

        private static byte[] RelocateOriginalInstructions(uint codeBase, Instruction[] instructions)
        {
            using var stream = new MemoryStream();
            var writer = new StreamCodeWriter(stream);
            var block = new InstructionBlock(writer, instructions, codeBase);
            if (!BlockEncoder.TryEncode(32, block, out var errorMessage, out _, BlockEncoderOptions.None))
            {
                throw new InvalidOperationException($"Unable to relocate function entry: {errorMessage}");
            }

            return stream.ToArray();
        }

        private static byte[] AssembleCode(uint codeBase, Action<Assembler> emit)
        {
            var assembler = new Assembler(32);
            emit(assembler);

            using var stream = new MemoryStream();
            var writer = new StreamCodeWriter(stream);
            assembler.Assemble(writer, codeBase, BlockEncoderOptions.None);
            return stream.ToArray();
        }

        private static void WriteJumpPatch(ProcessMemory memory, nint siteAddress, nint targetAddress, int hookLength)
        {
            if (hookLength < 5)
            {
                throw new InvalidOperationException(
                    $"Unexpected hook prefix length at {ProcessMemory.FormatAddress(siteAddress)}."
                );
            }

            Span<byte> patch = stackalloc byte[hookLength];
            patch[0] = 0xE9;
            BinaryPrimitives.WriteInt32LittleEndian(
                patch[1..5],
                checked((int)((long)targetAddress - ((long)siteAddress + 5)))
            );
            for (var index = 5; index < patch.Length; index++)
                patch[index] = 0x90;

            memory.WriteCodeBytes(siteAddress, patch);
        }

        private static AssemblerMemoryOperand DWordPtr(uint absoluteAddress) =>
            AssemblerRegisters.__dword_ptr[(long)absoluteAddress];

        private static AssemblerMemoryOperand DWordPtr(AssemblerRegister32 register, int displacement) =>
            AssemblerRegisters.__dword_ptr[register + displacement];

        private readonly record struct DecodedPatchPrefix(
            int HookLength,
            byte[] OriginalBytes,
            Instruction[] Instructions
        );
    }
}
