using System.Buffers.Binary;
using System.IO;
using System.Runtime.Versioning;
using Iced.Intel;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class RuntimeInterceptionSession : IDisposable
{
    public const int MaximumStackCaptureDwordCount = 16;

    private readonly RemoteEventBuffer _buffer;
    private readonly EntryDetourHook _hook;
    private readonly RuntimeInterceptionDefinition _definition;
    private readonly uint _moduleBase;
    private uint _lastContentionDropCount;
    private bool _disposed;

    private RuntimeInterceptionSession(
        RemoteEventBuffer buffer,
        EntryDetourHook hook,
        RuntimeInterceptionDefinition definition,
        uint moduleBase
    )
    {
        _buffer = buffer;
        _hook = hook;
        _definition = definition;
        _moduleBase = moduleBase;
    }

    public static RuntimeInterceptionSession Install(ProcessMemory memory, RuntimeInterceptionDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(memory);

        if (definition.StackCaptureDwordCount is < 1 or > MaximumStackCaptureDwordCount)
        {
            throw new InvalidOperationException(
                $"Stack capture dword count must be between 1 and {MaximumStackCaptureDwordCount}."
            );
        }

        var buffer = RemoteEventBuffer.Install(memory);
        try
        {
            var hook = EntryDetourHook.Install(memory, definition, buffer);
            return new RuntimeInterceptionSession(buffer, hook, definition, memory.ToUInt32Address(memory.ModuleBase));
        }
        catch
        {
            buffer.Dispose();
            throw;
        }
    }

    public RuntimeInterceptionReadResult ReadSince(uint lastSequence)
    {
        var snapshot = _buffer.ReadSnapshot();
        var writeSequence = snapshot.WriteSequence;
        var contentionDrops = unchecked((int)(snapshot.ContentionDropCount - _lastContentionDropCount));
        _lastContentionDropCount = snapshot.ContentionDropCount;
        if (writeSequence <= lastSequence)
            return new RuntimeInterceptionReadResult(writeSequence, 0, 0, contentionDrops, []);

        var firstSequence = lastSequence + 1;
        var droppedEvents = 0;
        if (writeSequence - lastSequence > RemoteEventBuffer.Capacity)
        {
            firstSequence = writeSequence - RemoteEventBuffer.Capacity + 1;
            droppedEvents = checked((int)(firstSequence - (lastSequence + 1)));
        }

        var inconsistentRecords = 0;
        List<RuntimeInterceptionCapturedEvent> events = new(checked((int)(writeSequence - firstSequence + 1)));
        for (var sequence = firstSequence; sequence <= writeSequence; sequence++)
        {
            var record = snapshot.ReadRecord(sequence, _definition.StackCaptureDwordCount);
            if (record.Sequence != sequence)
            {
                inconsistentRecords++;
                continue;
            }

            var callerRva = record.ReturnAddress >= _moduleBase ? record.ReturnAddress - _moduleBase : 0;
            events.Add(
                new RuntimeInterceptionCapturedEvent(
                    _definition,
                    record.Sequence,
                    record.ReturnAddress,
                    callerRva,
                    record.Eflags,
                    record.Registers,
                    record.StackDwords
                )
            );
        }

        return new RuntimeInterceptionReadResult(
            writeSequence,
            droppedEvents,
            inconsistentRecords,
            contentionDrops,
            events.Count == 0 ? [] : [.. events]
        );
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _hook.Dispose();
        _buffer.Dispose();
        _disposed = true;
    }

    private readonly record struct RuntimeInterceptionRawRecord(
        uint Sequence,
        uint ReturnAddress,
        uint Eflags,
        RuntimeInterceptionRegisters Registers,
        uint[] StackDwords
    );

    private sealed class RemoteEventBuffer : IDisposable
    {
        public const int MaxStackCaptureDwordCount = MaximumStackCaptureDwordCount;
        public const int Capacity = 8192;
        public const int RecordSize = sizeof(uint) * (11 + MaxStackCaptureDwordCount);

        private const int LockOffset = 0;
        private const int WriteSequenceOffset = sizeof(uint);
        private const int ContentionDropCountOffset = sizeof(uint) * 2;
        private const int RecordsOffset = sizeof(uint) * 3;
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

        public uint ContentionDropCountAddress => _memory.ToUInt32Address(_remoteBlock + ContentionDropCountOffset);

        public uint RecordsAddress => _memory.ToUInt32Address(_remoteBlock + RecordsOffset);

        public BufferSnapshot ReadSnapshot()
        {
            _memory.ReadBytes(_remoteBlock, _snapshotBytes);
            var writeSequence = BinaryPrimitives.ReadUInt32LittleEndian(
                _snapshotBytes.AsSpan(WriteSequenceOffset, sizeof(uint))
            );
            var contentionDropCount = BinaryPrimitives.ReadUInt32LittleEndian(
                _snapshotBytes.AsSpan(ContentionDropCountOffset, sizeof(uint))
            );
            return new BufferSnapshot(_snapshotBytes, writeSequence, contentionDropCount);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _memory.TryFree(_remoteBlock);
            _disposed = true;
        }

        public readonly record struct BufferSnapshot(byte[] Bytes, uint WriteSequence, uint ContentionDropCount)
        {
            public RuntimeInterceptionRawRecord ReadRecord(uint sequence, int stackCaptureDwordCount)
            {
                var slotIndex = checked((int)((sequence - 1) & CapacityMask));
                var offset = RecordsOffset + slotIndex * RecordSize;
                var span = Bytes.AsSpan(offset, RecordSize);
                var recordSequence = BinaryPrimitives.ReadUInt32LittleEndian(span[..sizeof(uint)]);
                var returnAddress = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(sizeof(uint), sizeof(uint)));
                var eflags = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(sizeof(uint) * 2, sizeof(uint)));
                var registers = new RuntimeInterceptionRegisters(
                    BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(sizeof(uint) * 3, sizeof(uint))),
                    BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(sizeof(uint) * 4, sizeof(uint))),
                    BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(sizeof(uint) * 5, sizeof(uint))),
                    BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(sizeof(uint) * 6, sizeof(uint))),
                    BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(sizeof(uint) * 7, sizeof(uint))),
                    BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(sizeof(uint) * 8, sizeof(uint))),
                    BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(sizeof(uint) * 9, sizeof(uint))),
                    BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(sizeof(uint) * 10, sizeof(uint)))
                );
                uint[] stackDwords = new uint[stackCaptureDwordCount];
                for (var index = 0; index < stackCaptureDwordCount; index++)
                {
                    stackDwords[index] = BinaryPrimitives.ReadUInt32LittleEndian(
                        span.Slice(sizeof(uint) * (index + 11), sizeof(uint))
                    );
                }

                return new RuntimeInterceptionRawRecord(recordSequence, returnAddress, eflags, registers, stackDwords);
            }
        }
    }

    private sealed class EntryDetourHook : IDisposable
    {
        private const int ActionOffset = 0x00;
        private const int CleanupBytesOffset = 0x04;
        private const int ReturnEaxOffset = 0x08;
        private const int ReturnEdxOffset = 0x0C;
        private const int RegisterOverrideMaskOffset = 0x10;
        private const int ArgumentOverrideMaskOffset = 0x14;
        private const int RegistersOffset = 0x20;
        private const int ArgumentsOffset = 0x40;
        private const int CodeOffset = 0x100;
        private const int RemoteBlockSize = 0x800;

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
            RuntimeInterceptionDefinition definition,
            RemoteEventBuffer buffer
        )
        {
            var siteAddress = (nint)(long)definition.Address;
            var prefix = DecodePatchPrefix(memory, siteAddress);
            var remoteBlock = memory.AllocateExecutable(RemoteBlockSize);
            try
            {
                memory.WriteBytes(remoteBlock, BuildControlBlock(definition.Mutation));
                var codeAddress = remoteBlock + CodeOffset;
                var trampoline = BuildTrampolineBlock(
                    memory,
                    codeAddress,
                    siteAddress,
                    definition,
                    buffer,
                    prefix,
                    remoteBlock
                );
                memory.WriteBytes(codeAddress, trampoline);
                WriteJumpPatch(memory, siteAddress, codeAddress, prefix.HookLength);
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

        private static byte[] BuildControlBlock(RuntimeInterceptionMutation mutation)
        {
            var block = new byte[CodeOffset];
            WriteUInt32(block, ActionOffset, (uint)mutation.ExecutionMode);
            WriteUInt32(block, CleanupBytesOffset, unchecked((uint)mutation.CleanupBytes));
            WriteUInt32(block, ReturnEaxOffset, mutation.ReturnEax);
            WriteUInt32(block, ReturnEdxOffset, mutation.ReturnEdx);
            WriteUInt32(block, RegisterOverrideMaskOffset, mutation.RegisterOverrideMask);
            WriteUInt32(block, ArgumentOverrideMaskOffset, mutation.ArgumentOverrideMask);
            WriteUInt32(block, RegistersOffset + sizeof(uint) * 0, mutation.RegisterOverrides.Edi);
            WriteUInt32(block, RegistersOffset + sizeof(uint) * 1, mutation.RegisterOverrides.Esi);
            WriteUInt32(block, RegistersOffset + sizeof(uint) * 2, mutation.RegisterOverrides.Ebp);
            WriteUInt32(block, RegistersOffset + sizeof(uint) * 3, mutation.RegisterOverrides.Ebx);
            WriteUInt32(block, RegistersOffset + sizeof(uint) * 4, mutation.RegisterOverrides.Edx);
            WriteUInt32(block, RegistersOffset + sizeof(uint) * 5, mutation.RegisterOverrides.Ecx);
            WriteUInt32(block, RegistersOffset + sizeof(uint) * 6, mutation.RegisterOverrides.Eax);
            for (var index = 0; index < mutation.ArgumentOverrides.Length; index++)
                WriteUInt32(block, ArgumentsOffset + sizeof(uint) * index, mutation.ArgumentOverrides[index]);

            return block;
        }

        private static void WriteUInt32(byte[] block, int offset, uint value) =>
            BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(offset, sizeof(uint)), value);

        private static byte[] BuildTrampolineBlock(
            ProcessMemory memory,
            nint codeAddress,
            nint siteAddress,
            RuntimeInterceptionDefinition definition,
            RemoteEventBuffer buffer,
            DecodedPatchPrefix prefix,
            nint remoteBlock
        )
        {
            var codeBase = memory.ToUInt32Address(codeAddress);
            var prelude = AssemblePrelude(codeBase, definition, buffer, memory.ToUInt32Address(remoteBlock));
            var relocatedOriginalBytes = RelocateOriginalInstructions(
                codeBase + (uint)prelude.Length,
                prefix.Instructions
            );
            var tailBase = codeBase + (uint)(prelude.Length + relocatedOriginalBytes.Length);
            var tail = AssembleTail(tailBase, memory.ToUInt32Address(siteAddress) + (uint)prefix.HookLength);
            var block = new byte[prelude.Length + relocatedOriginalBytes.Length + tail.Length];
            prelude.CopyTo(block, 0);
            relocatedOriginalBytes.CopyTo(block, prelude.Length);
            tail.CopyTo(block, prelude.Length + relocatedOriginalBytes.Length);
            return block;
        }

        private static byte[] AssemblePrelude(
            uint codeBase,
            RuntimeInterceptionDefinition definition,
            RemoteEventBuffer buffer,
            uint controlBlockAddress
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
                    var acquiredLabel = assembler.CreateLabel("acquired");
                    var dropLabel = assembler.CreateLabel("drop");
                    var afterRecordLabel = assembler.CreateLabel("afterRecord");
                    var shortCircuitLabel = assembler.CreateLabel("shortCircuit");
                    var continueLabel = assembler.CreateLabel("continueOriginal");

                    assembler.pushfd();
                    assembler.pushad();

                    assembler.mov(ecx, 0x1000);
                    assembler.Label(ref spinLabel);
                    assembler.mov(eax, 1);
                    assembler.xchg(DWordPtr(buffer.LockAddress), eax);
                    assembler.test(eax, eax);
                    assembler.je(acquiredLabel);
                    assembler.dec(ecx);
                    assembler.jne(spinLabel);
                    assembler.jmp(dropLabel);

                    assembler.Label(ref acquiredLabel);
                    assembler.mov(ecx, DWordPtr(buffer.WriteSequenceAddress));
                    assembler.lea(ecx, ecx + 1);
                    assembler.mov(eax, ecx);
                    assembler.dec(eax);
                    assembler.and(eax, RemoteEventBuffer.Capacity - 1);
                    assembler.imul(eax, eax, RemoteEventBuffer.RecordSize);
                    assembler.add(eax, unchecked((int)buffer.RecordsAddress));

                    assembler.mov(edx, DWordPtr(esp, 36));
                    assembler.mov(DWordPtr(eax, 4), edx);
                    assembler.mov(edx, DWordPtr(esp, 32));
                    assembler.mov(DWordPtr(eax, 8), edx);
                    for (var index = 0; index < 8; index++)
                    {
                        assembler.mov(edx, DWordPtr(esp, index * sizeof(uint)));
                        assembler.mov(DWordPtr(eax, sizeof(uint) * (index + 3)), edx);
                    }

                    for (var index = 0; index < RemoteEventBuffer.MaxStackCaptureDwordCount; index++)
                    {
                        assembler.mov(edx, DWordPtr(esp, 40 + index * sizeof(uint)));
                        assembler.mov(DWordPtr(eax, sizeof(uint) * (index + 11)), edx);
                    }

                    assembler.mov(DWordPtr(eax, 0), ecx);
                    assembler.mov(DWordPtr(buffer.WriteSequenceAddress), ecx);
                    assembler.mov(DWordPtr(buffer.LockAddress), 0);
                    assembler.jmp(afterRecordLabel);

                    assembler.Label(ref dropLabel);
                    assembler.@lock.inc(DWordPtr(buffer.ContentionDropCountAddress));

                    assembler.Label(ref afterRecordLabel);
                    assembler.mov(eax, DWordPtr(AddOffset(controlBlockAddress, ActionOffset)));
                    assembler.test(eax, eax);
                    assembler.jne(shortCircuitLabel);

                    EmitArgumentOverrides(assembler, definition.Mutation.ArgumentOverrideMask, controlBlockAddress);
                    EmitRegisterOverrides(assembler, definition.Mutation.RegisterOverrideMask, controlBlockAddress);
                    assembler.popad();
                    assembler.popfd();
                    assembler.jmp(continueLabel);

                    assembler.Label(ref shortCircuitLabel);
                    assembler.popad();
                    assembler.popfd();
                    assembler.pop(ecx);
                    assembler.mov(edx, DWordPtr(AddOffset(controlBlockAddress, CleanupBytesOffset)));
                    assembler.add(esp, edx);
                    assembler.mov(eax, DWordPtr(AddOffset(controlBlockAddress, ReturnEaxOffset)));
                    assembler.mov(edx, DWordPtr(AddOffset(controlBlockAddress, ReturnEdxOffset)));
                    assembler.jmp(ecx);

                    assembler.Label(ref continueLabel);
                    assembler.nop();
                }
            );
        }

        private static void EmitArgumentOverrides(Assembler assembler, uint mask, uint controlBlockAddress)
        {
            if (mask == 0)
                return;

            var edx = AssemblerRegisters.edx;
            for (var index = 0; index < RemoteEventBuffer.MaxStackCaptureDwordCount; index++)
            {
                if ((mask & (1u << index)) == 0)
                    continue;

                assembler.mov(edx, DWordPtr(AddOffset(controlBlockAddress, ArgumentsOffset + sizeof(uint) * index)));
                assembler.mov(DWordPtr(AssemblerRegisters.esp, 40 + sizeof(uint) * index), edx);
            }
        }

        private static void EmitRegisterOverrides(Assembler assembler, uint mask, uint controlBlockAddress)
        {
            if (mask == 0)
                return;

            var edx = AssemblerRegisters.edx;
            for (var slot = 0; slot < 7; slot++)
            {
                if ((mask & (1u << slot)) == 0)
                    continue;

                assembler.mov(edx, DWordPtr(AddOffset(controlBlockAddress, RegistersOffset + sizeof(uint) * slot)));
                assembler.mov(
                    DWordPtr(AssemblerRegisters.esp, RegisterFrameOffset((RuntimeInterceptionRegister)slot)),
                    edx
                );
            }
        }

        private static int RegisterFrameOffset(RuntimeInterceptionRegister register) =>
            register switch
            {
                RuntimeInterceptionRegister.Edi => 0,
                RuntimeInterceptionRegister.Esi => 4,
                RuntimeInterceptionRegister.Ebp => 8,
                RuntimeInterceptionRegister.Ebx => 16,
                RuntimeInterceptionRegister.Edx => 20,
                RuntimeInterceptionRegister.Ecx => 24,
                RuntimeInterceptionRegister.Eax => 28,
                _ => throw new ArgumentOutOfRangeException(nameof(register)),
            };

        private static byte[] AssembleTail(uint codeBase, uint returnAddress) =>
            AssembleCode(codeBase, assembler => assembler.jmp((ulong)returnAddress));

        private static byte[] RelocateOriginalInstructions(uint codeBase, Instruction[] instructions)
        {
            using var stream = new MemoryStream();
            var writer = new StreamCodeWriter(stream);
            var block = new InstructionBlock(writer, instructions, codeBase);
            if (!BlockEncoder.TryEncode(32, block, out var errorMessage, out _, BlockEncoderOptions.None))
                throw new InvalidOperationException($"Unable to relocate function entry: {errorMessage}");

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

        private static uint AddOffset(uint address, int offset) => unchecked(address + (uint)offset);

        private static AssemblerMemoryOperand DWordPtr(AssemblerRegister32 register, int displacement) =>
            AssemblerRegisters.__dword_ptr[register + displacement];

        private readonly record struct DecodedPatchPrefix(
            int HookLength,
            byte[] OriginalBytes,
            Instruction[] Instructions
        );
    }
}
