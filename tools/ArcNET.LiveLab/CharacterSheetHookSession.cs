using System.Buffers.Binary;
using System.Runtime.Versioning;
using ArcNET.Editor.Runtime;

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
            var originalBytes = memory.ReadBytes(
                siteAddress,
                ArcanumRuntimeOffsets.CharacterSheetSubstructureOriginal.Length
            );
            ValidateOriginal(siteAddress, originalBytes, ArcanumRuntimeOffsets.CharacterSheetSubstructureOriginal);

            const int slotCount = 9;
            const int slotOffset = 0;
            const int codeOffset = slotCount * sizeof(uint);
            var remoteBlock = memory.AllocateExecutable(0x200);
            try
            {
                var block = BuildSubstructureBlock(memory, remoteBlock, codeOffset);
                memory.WriteBytes(remoteBlock, block);
                WriteJumpPatch(memory, siteAddress, remoteBlock + codeOffset);
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
            var originalBytes = memory.ReadBytes(
                siteAddress,
                ArcanumRuntimeOffsets.CharacterSheetPropertyOriginal.Length
            );
            ValidateOriginal(siteAddress, originalBytes, ArcanumRuntimeOffsets.CharacterSheetPropertyOriginal);

            const int slotCount = 9;
            const int slotOffset = 0;
            const int codeOffset = slotCount * sizeof(uint);
            var remoteBlock = memory.AllocateExecutable(0x240);
            try
            {
                var block = BuildPropertyBlock(memory, remoteBlock, codeOffset);
                memory.WriteBytes(remoteBlock, block);
                WriteJumpPatch(memory, siteAddress, remoteBlock + codeOffset);
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

        private static void ValidateOriginal(nint siteAddress, byte[] actual, ReadOnlySpan<byte> expected)
        {
            if (actual.AsSpan().SequenceEqual(expected))
                return;

            throw new InvalidOperationException(
                $"Unexpected bytes at {ProcessMemory.FormatAddress(siteAddress)}. "
                    + "The current Arcanum build does not match the CE-derived hook signature."
            );
        }

        private static void WriteJumpPatch(ProcessMemory memory, nint siteAddress, nint targetAddress)
        {
            Span<byte> patch = stackalloc byte[6];
            patch[0] = 0xE9;
            BinaryPrimitives.WriteInt32LittleEndian(
                patch[1..5],
                checked((int)((long)targetAddress - ((long)siteAddress + 5)))
            );
            patch[5] = 0x90;
            memory.WriteBytes(siteAddress, patch);
        }

        private static byte[] BuildSubstructureBlock(ProcessMemory memory, nint remoteBlock, int codeOffset)
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
            var codeBase = memory.ToUInt32Address(remoteBlock + codeOffset);
            var returnSite = memory.ResolveRva32(ArcanumRuntimeOffsets.CharacterSheetSubstructureHookRva) + 6;
            var currentSheetId = memory.ResolveRva32(ArcanumRuntimeOffsets.CurrentCharacterSheetIdRva);

            var code = new List<byte>(128);
            var returnShortJumps = new List<int>();

            Emit(code, 0x8B, 0x4F, 0x50); // mov ecx,[edi+50]
            Emit(code, 0x8D, 0x14, 0x81); // lea edx,[ecx+eax*4]
            Emit(code, 0x3B, 0x1D);
            EmitUInt32(code, currentSheetId); // cmp ebx,[module+24E010]
            returnShortJumps.Add(EmitShortJumpPlaceholder(code, 0x75)); // jne return
            Emit(code, 0x89, 0x3D);
            EmitUInt32(code, ptrChar); // mov [ptr_char],edi
            Emit(code, 0x85, 0xD2); // test edx,edx
            returnShortJumps.Add(EmitShortJumpPlaceholder(code, 0x74)); // je return
            Emit(code, 0xA3);
            EmitUInt32(code, ptrLastIndex); // mov [last_substructure_index],eax
            Emit(code, 0x8B, 0x02); // mov eax,[edx]
            Emit(code, 0x89, 0x35);
            EmitUInt32(code, ptrLastId); // mov [last_substructure_id],esi
            Emit(code, 0xA3);
            EmitUInt32(code, ptrLastPointer); // mov [last_substructure_pointer],eax

            EmitConditionalStoreEaxAndMarkSeen(
                code,
                (int)CharacterSheetSubstructureId.MainStats,
                ptrMainStats,
                ptrSeenMask,
                0x1
            );
            EmitConditionalStoreEaxAndMarkSeen(
                code,
                (int)CharacterSheetSubstructureId.BasicSkills,
                ptrSkills,
                ptrSeenMask,
                0x2
            );
            EmitConditionalStoreEaxAndMarkSeen(
                code,
                (int)CharacterSheetSubstructureId.TechSkills,
                ptrTech,
                ptrSeenMask,
                0x4
            );
            EmitConditionalStoreEaxAndMarkSeen(
                code,
                (int)CharacterSheetSubstructureId.SpellAndTech,
                ptrSpells,
                ptrSeenMask,
                0x8
            );

            var returnLabel = code.Count;
            PatchShortJumps(code, returnShortJumps, returnLabel);

            Emit(code, 0xE9);
            var rel = checked((int)(returnSite - (codeBase + code.Count + 4)));
            EmitInt32(code, rel);

            var block = new byte[codeOffset + code.Count];
            code.CopyTo(block, codeOffset);
            return block;
        }

        private static byte[] BuildPropertyBlock(ProcessMemory memory, nint remoteBlock, int codeOffset)
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
            var codeBase = memory.ToUInt32Address(remoteBlock + codeOffset);
            var returnSite = memory.ResolveRva32(ArcanumRuntimeOffsets.CharacterSheetPropertyHookRva) + 6;
            var currentSheetId = memory.ResolveRva32(ArcanumRuntimeOffsets.CurrentCharacterSheetIdRva);

            var code = new List<byte>(160);
            var returnShortJumps = new List<int>();

            Emit(code, 0x8B, 0x4E, 0x50); // mov ecx,[esi+50]
            Emit(code, 0x8D, 0x14, 0x81); // lea edx,[ecx+eax*4]
            Emit(code, 0x3B, 0x2D);
            EmitUInt32(code, currentSheetId); // cmp ebp,[module+24E010]
            returnShortJumps.Add(EmitShortJumpPlaceholder(code, 0x75)); // jne return
            Emit(code, 0x85, 0xD2); // test edx,edx
            returnShortJumps.Add(EmitShortJumpPlaceholder(code, 0x74)); // je return
            Emit(code, 0xA3);
            EmitUInt32(code, ptrLastIndex); // mov [last_property_index],eax
            Emit(code, 0x8B, 0x02); // mov eax,[edx]
            Emit(code, 0x89, 0x3D);
            EmitUInt32(code, ptrLastId); // mov [last_property_id],edi
            Emit(code, 0x89, 0x15);
            EmitUInt32(code, ptrLastAddress); // mov [last_property_address],edx

            EmitConditionalStoreEdx(code, (int)CharacterSheetPropertyId.HpBonus, ptrHpBonus);
            EmitConditionalStoreEdx(code, (int)CharacterSheetPropertyId.HpLoss, ptrHpLoss);
            EmitConditionalStoreEdx(code, (int)CharacterSheetPropertyId.MpBonus, ptrMpBonus);
            EmitConditionalStoreEdx(code, (int)CharacterSheetPropertyId.MpLoss, ptrMpLoss);
            EmitConditionalStoreEdx(code, (int)CharacterSheetPropertyId.Name, ptrName);
            EmitConditionalStoreEdx(code, (int)CharacterSheetPropertyId.Flags, ptrFlags);

            var returnLabel = code.Count;
            PatchShortJumps(code, returnShortJumps, returnLabel);

            Emit(code, 0xE9);
            var rel = checked((int)(returnSite - (codeBase + code.Count + 4)));
            EmitInt32(code, rel);

            var block = new byte[codeOffset + code.Count];
            code.CopyTo(block, codeOffset);
            return block;
        }

        private static void EmitConditionalStoreEax(List<byte> code, int compareValue, uint destination)
        {
            Emit(code, 0x81, 0xFE);
            EmitInt32(code, compareValue); // cmp esi,imm32
            Emit(code, 0x75, 0x05); // jne +5
            Emit(code, 0xA3);
            EmitUInt32(code, destination); // mov [imm32],eax
        }

        private static void EmitConditionalStoreEaxAndMarkSeen(
            List<byte> code,
            int compareValue,
            uint destination,
            uint seenMaskDestination,
            int seenBit
        )
        {
            Emit(code, 0x81, 0xFE);
            EmitInt32(code, compareValue); // cmp esi,imm32
            Emit(code, 0x75, 0x0F); // jne +15
            Emit(code, 0xA3);
            EmitUInt32(code, destination); // mov [imm32],eax
            Emit(code, 0x81, 0x0D);
            EmitUInt32(code, seenMaskDestination); // or dword ptr [imm32],imm32
            EmitInt32(code, seenBit);
        }

        private static void EmitConditionalStoreEdx(List<byte> code, int compareValue, uint destination)
        {
            Emit(code, 0x81, 0xFF);
            EmitInt32(code, compareValue); // cmp edi,imm32
            Emit(code, 0x75, 0x06); // jne +6
            Emit(code, 0x89, 0x15);
            EmitUInt32(code, destination); // mov [imm32],edx
        }

        private static int EmitShortJumpPlaceholder(List<byte> code, byte opcode)
        {
            code.Add(opcode);
            var index = code.Count;
            code.Add(0);
            return index;
        }

        private static void PatchShortJumps(List<byte> code, IEnumerable<int> jumpOffsetIndexes, int targetIndex)
        {
            foreach (var offsetIndex in jumpOffsetIndexes)
            {
                var displacement = targetIndex - (offsetIndex + 1);
                code[offsetIndex] = checked((byte)(sbyte)displacement);
            }
        }

        private static void Emit(List<byte> code, params byte[] bytes) => code.AddRange(bytes);

        private static void EmitUInt32(List<byte> code, uint value) => code.AddRange(BitConverter.GetBytes(value));

        private static void EmitInt32(List<byte> code, int value) => code.AddRange(BitConverter.GetBytes(value));
    }
}
