using System.Buffers.Binary;
using System.IO;
using System.Runtime.Versioning;
using ArcNET.Editor.Runtime;
using Iced.Intel;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal sealed class CharacterSheetHookSession : IDisposable
{
    private readonly RemoteHook _substructureHook;
    private readonly RemoteHook _propertyHook;

    private CharacterSheetHookSession(RemoteHook substructureHook, RemoteHook propertyHook)
    {
        _substructureHook = substructureHook;
        _propertyHook = propertyHook;
    }

    public static CharacterSheetHookSession Install(ProcessMemory memory)
    {
        var substructureHook = RemoteHook.InstallSubstructureHook(memory);
        try
        {
            var propertyHook = RemoteHook.InstallPropertyHook(memory);
            return new CharacterSheetHookSession(substructureHook, propertyHook);
        }
        catch
        {
            substructureHook.Dispose();
            throw;
        }
    }

    public CapturedPointers ReadPointers()
    {
        var sub = _substructureHook.ReadSlots();
        var prop = _propertyHook.ReadSlots();
        return new CapturedPointers(
            (nint)(long)sub[0],
            (nint)(long)sub[1],
            (nint)(long)sub[2],
            (nint)(long)sub[3],
            (nint)(long)sub[4],
            (nint)(long)prop[0],
            (nint)(long)prop[1],
            (nint)(long)prop[2],
            (nint)(long)prop[3],
            (nint)(long)prop[4],
            (nint)(long)prop[5],
            sub[8],
            sub[5],
            sub[6],
            (nint)(long)sub[7],
            prop[6],
            prop[7],
            (nint)(long)prop[8]
        );
    }

    public CapturedPointers WaitForCapture(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        var last = ReadPointers();
        while (DateTime.UtcNow < deadline)
        {
            last = ReadPointers();
            if (IsFullyCaptured(last))
                return last;

            Thread.Sleep(100);
        }

        return last;
    }

    public void Dispose()
    {
        _propertyHook.Dispose();
        _substructureHook.Dispose();
    }

    private static bool IsFullyCaptured(CapturedPointers pointers) =>
        pointers.MainStats != 0
        && pointers.BasicSkills != 0
        && pointers.TechSkills != 0
        && pointers.SpellAndTech != 0
        && pointers.HpBonus != 0
        && pointers.HpLoss != 0
        && pointers.MpBonus != 0
        && pointers.MpLoss != 0
        && pointers.Name != 0
        && pointers.Flags != 0;

    private sealed class RemoteHook : IDisposable
    {
        private readonly ProcessMemory _memory;
        private readonly nint _siteAddress;
        private readonly byte[] _originalBytes;
        private readonly nint _remoteBlock;
        private readonly int _slotCount;
        private readonly int _slotOffset;
        private bool _disposed;

        private RemoteHook(
            ProcessMemory memory,
            nint siteAddress,
            byte[] originalBytes,
            nint remoteBlock,
            int slotCount,
            int slotOffset
        )
        {
            _memory = memory;
            _siteAddress = siteAddress;
            _originalBytes = originalBytes;
            _remoteBlock = remoteBlock;
            _slotCount = slotCount;
            _slotOffset = slotOffset;
        }

        public static RemoteHook InstallSubstructureHook(ProcessMemory memory)
        {
            var siteAddress = memory.ResolveRva(ArcanumRuntimeOffsets.CharacterSheetSubstructureHookRva);
            var hookLength = ResolveHookLength(memory, siteAddress, AssemblerRegisters.edi, AssemblerRegisters.ebx);
            var originalBytes = memory.ReadBytes(siteAddress, hookLength);

            const int slotCount = 9;
            const int slotOffset = 0;
            const int codeOffset = slotCount * sizeof(uint);
            var remoteBlock = memory.AllocateExecutable(0x200);
            try
            {
                var block = BuildSubstructureBlock(memory, remoteBlock, codeOffset, hookLength);
                memory.WriteBytes(remoteBlock, block);
                WriteJumpPatch(memory, siteAddress, remoteBlock + codeOffset, hookLength);
                return new RemoteHook(memory, siteAddress, originalBytes, remoteBlock, slotCount, slotOffset);
            }
            catch
            {
                memory.Free(remoteBlock);
                throw;
            }
        }

        public static RemoteHook InstallPropertyHook(ProcessMemory memory)
        {
            var siteAddress = memory.ResolveRva(ArcanumRuntimeOffsets.CharacterSheetPropertyHookRva);
            var hookLength = ResolveHookLength(memory, siteAddress, AssemblerRegisters.esi, AssemblerRegisters.ebp);
            var originalBytes = memory.ReadBytes(siteAddress, hookLength);

            const int slotCount = 9;
            const int slotOffset = 0;
            const int codeOffset = slotCount * sizeof(uint);
            var remoteBlock = memory.AllocateExecutable(0x240);
            try
            {
                var block = BuildPropertyBlock(memory, remoteBlock, codeOffset, hookLength);
                memory.WriteBytes(remoteBlock, block);
                WriteJumpPatch(memory, siteAddress, remoteBlock + codeOffset, hookLength);
                return new RemoteHook(memory, siteAddress, originalBytes, remoteBlock, slotCount, slotOffset);
            }
            catch
            {
                memory.Free(remoteBlock);
                throw;
            }
        }

        public uint[] ReadSlots()
        {
            var bytes = _memory.ReadBytes(_remoteBlock + _slotOffset, _slotCount * sizeof(uint));
            var values = new uint[_slotCount];
            for (var i = 0; i < values.Length; i++)
                values[i] = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(i * sizeof(uint), sizeof(uint)));

            return values;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _memory.WriteBytes(_siteAddress, _originalBytes);
            _memory.Free(_remoteBlock);
            _disposed = true;
        }

        private static int ResolveHookLength(
            ProcessMemory memory,
            nint siteAddress,
            AssemblerRegister32 expectedBaseRegister,
            AssemblerRegister32 expectedSheetIdRegister
        )
        {
            var signatureBytes = memory.ReadBytes(siteAddress, 64);
            var decoder = Decoder.Create(32, signatureBytes, (ulong)(long)siteAddress, DecoderOptions.None);
            var mov = new Instruction();
            var lea = new Instruction();
            var cmp = new Instruction();
            decoder.Decode(out mov);
            decoder.Decode(out lea);
            decoder.Decode(out cmp);

            if (
                mov.IsInvalid
                || mov.Code != Code.Mov_r32_rm32
                || mov.Op0Kind != OpKind.Register
                || mov.GetOpRegister(0) != expectedBaseRegister
                || mov.Op1Kind != OpKind.Memory
                || mov.MemoryBase != expectedBaseRegister
                || mov.MemoryDisplacement32 != 0x50
                || mov.MemoryIndexScale != 1
            )
                throw new InvalidOperationException(
                    $"Unexpected bytes at {ProcessMemory.FormatAddress(siteAddress)}. "
                        + "The current Arcanum build does not match the CE-derived hook signature."
                );

            if (
                lea.IsInvalid
                || lea.Code != Code.Lea_r32_m
                || lea.Op0Kind != OpKind.Register
                || lea.Op1Kind != OpKind.Memory
                || lea.MemoryBase != expectedBaseRegister
                || lea.MemoryIndex != AssemblerRegisters.eax
                || lea.MemoryDisplacement32 != 0
                || lea.MemoryIndexScale != 4
            )
                throw new InvalidOperationException(
                    $"Unexpected bytes at {ProcessMemory.FormatAddress(siteAddress)}. "
                        + "The current Arcanum build does not match the CE-derived hook signature."
                );

            if (
                cmp.IsInvalid
                || cmp.Op0Kind != OpKind.Register
                || cmp.GetOpRegister(0) != expectedSheetIdRegister
                || !IsImmediateKind(cmp.Op1Kind)
            )
                throw new InvalidOperationException(
                    $"Unexpected bytes at {ProcessMemory.FormatAddress(siteAddress)}. "
                        + "The current Arcanum build does not match the CE-derived hook signature."
                );

            return mov.Length + lea.Length;
        }

        private static bool IsImmediateKind(OpKind kind) =>
            kind
                is OpKind.Immediate8
                    or OpKind.Immediate8_2nd
                    or OpKind.Immediate16
                    or OpKind.Immediate32
                    or OpKind.Immediate64
                    or OpKind.Immediate8to16
                    or OpKind.Immediate8to32
                    or OpKind.Immediate8to64
                    or OpKind.Immediate32to64;

        private static void WriteJumpPatch(ProcessMemory memory, nint siteAddress, nint targetAddress, int hookLength)
        {
            if (hookLength < 5)
            {
                throw new InvalidOperationException(
                    $"Unexpected hook prefix length at {ProcessMemory.FormatAddress(siteAddress)}. Expected at least 5 bytes."
                );
            }

            Span<byte> patch = new byte[hookLength];
            patch[0] = 0xE9;
            BinaryPrimitives.WriteInt32LittleEndian(
                patch[1..5],
                checked((int)((long)targetAddress - ((long)siteAddress + 5)))
            );
            for (var i = 5; i < patch.Length; i++)
                patch[i] = 0x90;
            memory.WriteBytes(siteAddress, patch);
        }

        private static byte[] BuildSubstructureBlock(
            ProcessMemory memory,
            nint remoteBlock,
            int codeOffset,
            int hookLength
        )
        {
            var ptrChar = memory.ToUInt32Address(remoteBlock + 0);
            var ptrMainStats = memory.ToUInt32Address(remoteBlock + 4);
            var ptrSkills = memory.ToUInt32Address(remoteBlock + 8);
            var ptrTech = memory.ToUInt32Address(remoteBlock + 12);
            var ptrSpells = memory.ToUInt32Address(remoteBlock + 16);
            var ptrLastIndex = memory.ToUInt32Address(remoteBlock + 20);
            var ptrLastId = memory.ToUInt32Address(remoteBlock + 24);
            var ptrLastPointer = memory.ToUInt32Address(remoteBlock + 28);
            var ptrSeenMask = memory.ToUInt32Address(remoteBlock + 32);
            var returnSite = memory.ResolveRva32(ArcanumRuntimeOffsets.CharacterSheetSubstructureHookRva) + hookLength;
            var currentSheetId = memory.ResolveRva32(ArcanumRuntimeOffsets.CurrentCharacterSheetIdRva);
            var captures = new (int Id, uint Destination, int SeenBit)[]
            {
                ((int)CharacterSheetSubstructureId.MainStats, ptrMainStats, 0x1),
                ((int)CharacterSheetSubstructureId.BasicSkills, ptrSkills, 0x2),
                ((int)CharacterSheetSubstructureId.TechSkills, ptrTech, 0x4),
                ((int)CharacterSheetSubstructureId.SpellAndTech, ptrSpells, 0x8),
            };

            return AssembleHookCode(
                memory.ToUInt32Address(remoteBlock + codeOffset),
                codeOffset,
                assembler =>
                {
                    var edi = AssemblerRegisters.edi;
                    var ecx = AssemblerRegisters.ecx;
                    var edx = AssemblerRegisters.edx;
                    var ebx = AssemblerRegisters.ebx;
                    var esi = AssemblerRegisters.esi;
                    var eax = AssemblerRegisters.eax;

                    var returnLabel = assembler.CreateLabel("substructure-return");

                    assembler.mov(ecx, AssemblerRegisters.__dword_ptr[edi + 0x50]);
                    assembler.lea(edx, ecx + eax * 4);
                    assembler.cmp(ebx, currentSheetId);
                    assembler.jne(returnLabel);
                    assembler.mov(DWordPtr(ptrChar), edi);
                    assembler.test(edx, edx);
                    assembler.je(returnLabel);
                    assembler.mov(DWordPtr(ptrLastIndex), eax);
                    assembler.mov(eax, AssemblerRegisters.__dword_ptr[edx]);
                    assembler.mov(DWordPtr(ptrLastId), esi);
                    assembler.mov(DWordPtr(ptrLastPointer), eax);

                    foreach (var capture in captures)
                    {
                        EmitConditionalStoreRegister(
                            assembler,
                            esi,
                            capture.Id,
                            eax,
                            capture.Destination,
                            capture.SeenBit,
                            ptrSeenMask
                        );
                    }

                    assembler.Label(ref returnLabel);
                    assembler.jmp((ulong)returnSite);
                }
            );
        }

        private static byte[] BuildPropertyBlock(ProcessMemory memory, nint remoteBlock, int codeOffset, int hookLength)
        {
            var ptrHpLoss = memory.ToUInt32Address(remoteBlock + 0);
            var ptrHpBonus = memory.ToUInt32Address(remoteBlock + 4);
            var ptrMpLoss = memory.ToUInt32Address(remoteBlock + 8);
            var ptrMpBonus = memory.ToUInt32Address(remoteBlock + 12);
            var ptrName = memory.ToUInt32Address(remoteBlock + 16);
            var ptrFlags = memory.ToUInt32Address(remoteBlock + 20);
            var ptrLastIndex = memory.ToUInt32Address(remoteBlock + 24);
            var ptrLastId = memory.ToUInt32Address(remoteBlock + 28);
            var ptrLastAddress = memory.ToUInt32Address(remoteBlock + 32);
            var returnSite = memory.ResolveRva32(ArcanumRuntimeOffsets.CharacterSheetPropertyHookRva) + hookLength;
            var currentSheetId = memory.ResolveRva32(ArcanumRuntimeOffsets.CurrentCharacterSheetIdRva);
            var captures = new (int Id, uint Destination)[]
            {
                ((int)CharacterSheetPropertyId.HpBonus, ptrHpBonus),
                ((int)CharacterSheetPropertyId.HpLoss, ptrHpLoss),
                ((int)CharacterSheetPropertyId.MpBonus, ptrMpBonus),
                ((int)CharacterSheetPropertyId.MpLoss, ptrMpLoss),
                ((int)CharacterSheetPropertyId.Name, ptrName),
                ((int)CharacterSheetPropertyId.Flags, ptrFlags),
            };

            return AssembleHookCode(
                memory.ToUInt32Address(remoteBlock + codeOffset),
                codeOffset,
                assembler =>
                {
                    var esi = AssemblerRegisters.esi;
                    var edi = AssemblerRegisters.edi;
                    var ecx = AssemblerRegisters.ecx;
                    var edx = AssemblerRegisters.edx;
                    var ebp = AssemblerRegisters.ebp;
                    var eax = AssemblerRegisters.eax;

                    var returnLabel = assembler.CreateLabel("property-return");

                    assembler.mov(ecx, AssemblerRegisters.__dword_ptr[esi + 0x50]);
                    assembler.lea(edx, ecx + eax * 4);
                    assembler.cmp(ebp, currentSheetId);
                    assembler.jne(returnLabel);
                    assembler.test(edx, edx);
                    assembler.je(returnLabel);
                    assembler.mov(DWordPtr(ptrLastIndex), eax);
                    assembler.mov(eax, AssemblerRegisters.__dword_ptr[edx]);
                    assembler.mov(DWordPtr(ptrLastId), edi);
                    assembler.mov(DWordPtr(ptrLastAddress), edx);

                    foreach (var capture in captures)
                    {
                        EmitConditionalStoreRegister(assembler, edi, capture.Id, edx, capture.Destination);
                    }

                    assembler.Label(ref returnLabel);
                    assembler.jmp((ulong)returnSite);
                }
            );
        }

        private static void EmitConditionalStoreRegister(
            Assembler assembler,
            AssemblerRegister32 compareRegister,
            int compareValue,
            AssemblerRegister32 valueRegister,
            uint destination,
            int? seenBit = null,
            uint? seenMaskDestination = null
        )
        {
            var nextLabel = assembler.CreateLabel($"skip-{compareValue}");
            assembler.cmp(compareRegister, compareValue);
            assembler.jne(nextLabel);
            assembler.mov(DWordPtr(destination), valueRegister);
            if (seenBit is not null && seenMaskDestination is not null)
            {
                assembler.or(DWordPtr(seenMaskDestination.Value), seenBit.Value);
            }

            assembler.Label(ref nextLabel);
        }

        private static AssemblerMemoryOperand DWordPtr(uint absoluteAddress) =>
            AssemblerRegisters.__dword_ptr[(long)absoluteAddress];

        private static byte[] AssembleHookCode(uint codeBase, int codeOffset, Action<Assembler> emit)
        {
            var assembler = new Assembler(32);
            emit(assembler);

            using var stream = new MemoryStream();
            var writer = new StreamCodeWriter(stream);
            assembler.Assemble(writer, codeBase, BlockEncoderOptions.None);

            var code = stream.ToArray();
            var block = new byte[codeOffset + code.Length];
            code.CopyTo(block, codeOffset);
            return block;
        }
    }
}
