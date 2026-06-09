using System.Buffers.Binary;
using System.IO;
using System.Runtime.Versioning;
using ArcNET.Diagnostics.Contracts;
using Iced.Intel;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class RuntimeCallDispatcher : IDisposable
{
    private const int MaxArguments = 32;
    private const int StateOffset = 0x00;
    private const int RequestIdOffset = 0x04;
    private const int CompletedIdOffset = 0x08;
    private const int CleanupModeOffset = 0x0C;
    private const int TargetAddressOffset = 0x10;
    private const int EcxValueOffset = 0x14;
    private const int EdxValueOffset = 0x18;
    private const int ArgCountOffset = 0x1C;
    private const int ResultEaxOffset = 0x20;
    private const int ResultEdxOffset = 0x24;
    private const int HeartbeatCountOffset = 0x28;
    private const int ArgsOffset = 0x40;
    private const int CodeOffset = 0x200;
    private const int RemoteBlockSize = 0x800;
    private const int HeaderReadByteCount = ArgsOffset;
    private const uint WaitObject0 = 0x00000000;
    private const uint WaitTimeout = 0x00000102;
    private static readonly TimeSpan HookValidationTimeout = TimeSpan.FromMilliseconds(250);

    private readonly ProcessMemory _memory;
    private readonly nint _siteAddress;
    private readonly byte[] _originalBytes;
    private readonly nint _remoteBlock;
    private readonly InvocationMode _mode;
    private readonly string _siteDescription;
    private uint _nextRequestId;
    private bool _disposed;

    private RuntimeCallDispatcher(
        ProcessMemory memory,
        nint siteAddress,
        byte[] originalBytes,
        nint remoteBlock,
        InvocationMode mode,
        string siteDescription
    )
    {
        _memory = memory;
        _siteAddress = siteAddress;
        _originalBytes = originalBytes;
        _remoteBlock = remoteBlock;
        _mode = mode;
        _siteDescription = siteDescription;
        _nextRequestId = 1;
    }

    public string ModeDescription =>
        _mode == InvocationMode.MainThreadHook ? "main-thread-hook" : "remote-thread-fallback";

    public string SiteDescription => _siteDescription;

    public static RuntimeCallDispatcher Install(ProcessMemory memory, RuntimeProfileSnapshot runtimeProfile)
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!runtimeProfile.SupportsCatalogRvas)
        {
            throw new InvalidOperationException(
                "Runtime call dispatch currently requires a validated fixed-RVA profile."
            );
        }

        var remoteBlock = memory.AllocateExecutable(RemoteBlockSize);
        try
        {
            foreach (var candidate in FunctionCatalog.DispatcherCandidates)
            {
                var siteAddress = memory.ResolveRva(candidate.Rva);
                if (!TryInstallMainThreadHook(memory, remoteBlock, siteAddress, out var originalBytes))
                    continue;

                if (WaitForHeartbeat(memory, remoteBlock, HookValidationTimeout))
                {
                    return new RuntimeCallDispatcher(
                        memory,
                        siteAddress,
                        originalBytes,
                        remoteBlock,
                        InvocationMode.MainThreadHook,
                        $"{candidate.Key} @ {candidate.Site}"
                    );
                }

                memory.TryWriteCodeBytes(siteAddress, originalBytes);
                ZeroHeader(memory, remoteBlock);
            }

            var block = BuildRemoteThreadBlock(memory, remoteBlock);
            memory.WriteBytes(remoteBlock, block);
            return new RuntimeCallDispatcher(
                memory,
                0,
                [],
                remoteBlock,
                InvocationMode.RemoteThreadFallback,
                "remote-thread-fallback"
            );
        }
        catch
        {
            memory.Free(remoteBlock);
            throw;
        }
    }

    public InvocationResult Invoke(
        uint targetAddress,
        StackCleanupMode cleanupMode,
        uint ecxValue,
        uint edxValue,
        IReadOnlyList<uint> stackArguments,
        TimeSpan timeout
    )
    {
        if (_mode == InvocationMode.RemoteThreadFallback)
            return InvokeViaRemoteThread(targetAddress, cleanupMode, ecxValue, edxValue, stackArguments, timeout);

        ValidateArgumentCount(stackArguments);

        var requestId = NextRequestId();
        var state = BuildStateBlock(requestId, targetAddress, cleanupMode, ecxValue, edxValue, stackArguments);
        _memory.WriteBytes(_remoteBlock, state);

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (_memory.HasExited)
            {
                throw new InvalidOperationException(
                    "The target Arcanum process exited before the debugger call completed."
                );
            }

            var snapshot = ReadSnapshot();
            if (snapshot.State == DispatcherState.Completed && snapshot.CompletedRequestId == requestId)
            {
                ResetState();
                return new InvocationResult(requestId, snapshot.ResultEax, snapshot.ResultEdx, snapshot.State);
            }

            Thread.Sleep(10);
        }

        var timedOut = ReadSnapshot();
        throw new InvalidOperationException(
            $"Timed out waiting for main-thread debugger call {requestId} to complete. Last state was {timedOut.State}."
        );
    }

    public int InvokeInt32(FunctionDefinition function, IReadOnlyList<uint> stackArguments, TimeSpan timeout)
    {
        var targetAddress = _memory.ToUInt32Address(_memory.ResolveRva(function.Rva));
        var result = Invoke(targetAddress, function.SuggestedCleanup, 0, 0, stackArguments, timeout);
        return unchecked((int)result.ResultEax);
    }

    public int InvokeInt32(string functionKey, IReadOnlyList<uint> stackArguments, TimeSpan timeout) =>
        InvokeInt32(FunctionCatalog.GetDefinition(functionKey), stackArguments, timeout);

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_mode == InvocationMode.MainThreadHook)
            _memory.TryWriteCodeBytes(_siteAddress, _originalBytes);

        _memory.TryFree(_remoteBlock);
        _disposed = true;
    }

    private static void ValidateArgumentCount(IReadOnlyList<uint> stackArguments)
    {
        if (stackArguments.Count > MaxArguments)
        {
            throw new InvalidOperationException(
                $"Debugger call supports at most {MaxArguments} stack dword(s); received {stackArguments.Count}."
            );
        }
    }

    private uint NextRequestId()
    {
        var requestId = _nextRequestId++;
        if (requestId != 0)
            return requestId;

        requestId = _nextRequestId++;
        return requestId == 0 ? 1u : requestId;
    }

    private void ResetState()
    {
        Span<byte> zero = stackalloc byte[sizeof(uint)];
        _memory.WriteBytes(_remoteBlock + StateOffset, zero);
    }

    private static void ZeroHeader(ProcessMemory memory, nint remoteBlock)
    {
        Span<byte> zero = stackalloc byte[ArgsOffset];
        memory.WriteBytes(remoteBlock, zero);
    }

    private InvocationResult InvokeViaRemoteThread(
        uint targetAddress,
        StackCleanupMode cleanupMode,
        uint ecxValue,
        uint edxValue,
        IReadOnlyList<uint> stackArguments,
        TimeSpan timeout
    )
    {
        ValidateArgumentCount(stackArguments);

        var requestId = NextRequestId();
        var state = BuildStateBlock(requestId, targetAddress, cleanupMode, ecxValue, edxValue, stackArguments);
        _memory.WriteBytes(_remoteBlock, state);

        var threadHandle = StartRemoteThread(_memory, _remoteBlock + CodeOffset);
        try
        {
            var waitResult = Kernel32NativeMethods.WaitForSingleObject(threadHandle, TimeoutMilliseconds(timeout));
            if (waitResult == WaitTimeout)
            {
                var timedOut = ReadSnapshot();
                throw new InvalidOperationException(
                    $"Timed out waiting for remote debugger call {requestId} to complete. Last state was {timedOut.State}."
                );
            }

            if (waitResult != WaitObject0)
            {
                throw new InvalidOperationException(
                    $"Waiting for remote debugger call {requestId} failed with Win32 wait status 0x{waitResult:X8}."
                );
            }

            if (!Kernel32NativeMethods.GetExitCodeThread(threadHandle, out _))
                throw new IOException("Unable to query the remote debugger worker thread exit code.");

            var snapshot = ReadSnapshot();
            if (snapshot.State == DispatcherState.Completed && snapshot.CompletedRequestId == requestId)
            {
                ResetState();
                return new InvocationResult(requestId, snapshot.ResultEax, snapshot.ResultEdx, snapshot.State);
            }

            throw new InvalidOperationException(
                $"Remote debugger call {requestId} returned without publishing a completed state. Last state was {snapshot.State}."
            );
        }
        finally
        {
            _ = Kernel32NativeMethods.CloseHandle(threadHandle);
        }
    }

    private byte[] BuildStateBlock(
        uint requestId,
        uint targetAddress,
        StackCleanupMode cleanupMode,
        uint ecxValue,
        uint edxValue,
        IReadOnlyList<uint> stackArguments
    )
    {
        var block = new byte[CodeOffset];
        WriteUInt32(block, RequestIdOffset, requestId);
        WriteUInt32(block, CompletedIdOffset, 0);
        WriteUInt32(block, CleanupModeOffset, (uint)cleanupMode);
        WriteUInt32(block, TargetAddressOffset, targetAddress);
        WriteUInt32(block, EcxValueOffset, ecxValue);
        WriteUInt32(block, EdxValueOffset, edxValue);
        WriteUInt32(block, ArgCountOffset, (uint)stackArguments.Count);
        WriteUInt32(block, ResultEaxOffset, 0);
        WriteUInt32(block, ResultEdxOffset, 0);
        for (var index = 0; index < stackArguments.Count; index++)
            WriteUInt32(block, ArgsOffset + index * sizeof(uint), stackArguments[index]);

        WriteUInt32(block, StateOffset, (uint)DispatcherState.Pending);
        return block;
    }

    private Snapshot ReadSnapshot()
    {
        var bytes = _memory.ReadBytes(_remoteBlock, HeaderReadByteCount);
        return new Snapshot(
            (DispatcherState)ReadUInt32(bytes, StateOffset),
            ReadUInt32(bytes, CompletedIdOffset),
            ReadUInt32(bytes, ResultEaxOffset),
            ReadUInt32(bytes, ResultEdxOffset),
            ReadUInt32(bytes, HeartbeatCountOffset)
        );
    }

    private static bool WaitForHeartbeat(ProcessMemory memory, nint remoteBlock, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        var initialHeartbeat = ReadHeartbeatCount(memory, remoteBlock);
        while (DateTime.UtcNow < deadline)
        {
            if (memory.HasExited)
                return false;

            if (ReadHeartbeatCount(memory, remoteBlock) != initialHeartbeat)
                return true;

            Thread.Sleep(10);
        }

        return false;
    }

    private static uint ReadHeartbeatCount(ProcessMemory memory, nint remoteBlock)
    {
        var bytes = memory.ReadBytes(remoteBlock, HeaderReadByteCount);
        return ReadUInt32(bytes, HeartbeatCountOffset);
    }

    private static byte[] BuildDispatcherBlock(
        ProcessMemory memory,
        nint remoteBlock,
        nint siteAddress,
        DecodedPatchPrefix prefix
    )
    {
        var codeBase = memory.ToUInt32Address(remoteBlock + CodeOffset);
        var prelude = AssemblePrelude(
            codeBase,
            memory.ToUInt32Address(remoteBlock + StateOffset),
            memory.ToUInt32Address(remoteBlock + RequestIdOffset),
            memory.ToUInt32Address(remoteBlock + CompletedIdOffset),
            memory.ToUInt32Address(remoteBlock + CleanupModeOffset),
            memory.ToUInt32Address(remoteBlock + TargetAddressOffset),
            memory.ToUInt32Address(remoteBlock + EcxValueOffset),
            memory.ToUInt32Address(remoteBlock + EdxValueOffset),
            memory.ToUInt32Address(remoteBlock + ArgCountOffset),
            memory.ToUInt32Address(remoteBlock + ResultEaxOffset),
            memory.ToUInt32Address(remoteBlock + ResultEdxOffset),
            memory.ToUInt32Address(remoteBlock + HeartbeatCountOffset),
            memory.ToUInt32Address(remoteBlock + ArgsOffset)
        );
        var relocatedOriginalBytes = RelocateOriginalInstructions(codeBase + (uint)prelude.Length, prefix.Instructions);
        var tail = AssembleTail(
            codeBase + (uint)(prelude.Length + relocatedOriginalBytes.Length),
            memory.ToUInt32Address(siteAddress) + (uint)prefix.HookLength
        );
        var block = new byte[CodeOffset + prelude.Length + relocatedOriginalBytes.Length + tail.Length];
        prelude.CopyTo(block, CodeOffset);
        relocatedOriginalBytes.CopyTo(block, CodeOffset + prelude.Length);
        tail.CopyTo(block, CodeOffset + prelude.Length + relocatedOriginalBytes.Length);
        return block;
    }

    private static byte[] BuildRemoteThreadBlock(ProcessMemory memory, nint remoteBlock)
    {
        var codeBase = memory.ToUInt32Address(remoteBlock + CodeOffset);
        var worker = AssembleRemoteThreadWorker(
            codeBase,
            memory.ToUInt32Address(remoteBlock + StateOffset),
            memory.ToUInt32Address(remoteBlock + RequestIdOffset),
            memory.ToUInt32Address(remoteBlock + CompletedIdOffset),
            memory.ToUInt32Address(remoteBlock + CleanupModeOffset),
            memory.ToUInt32Address(remoteBlock + TargetAddressOffset),
            memory.ToUInt32Address(remoteBlock + EcxValueOffset),
            memory.ToUInt32Address(remoteBlock + EdxValueOffset),
            memory.ToUInt32Address(remoteBlock + ArgCountOffset),
            memory.ToUInt32Address(remoteBlock + ResultEaxOffset),
            memory.ToUInt32Address(remoteBlock + ResultEdxOffset),
            memory.ToUInt32Address(remoteBlock + ArgsOffset)
        );
        var block = new byte[CodeOffset + worker.Length];
        worker.CopyTo(block, CodeOffset);
        return block;
    }

    private static byte[] AssemblePrelude(
        uint codeBase,
        uint stateAddress,
        uint requestIdAddress,
        uint completedIdAddress,
        uint cleanupModeAddress,
        uint targetAddress,
        uint ecxValueAddress,
        uint edxValueAddress,
        uint argCountAddress,
        uint resultEaxAddress,
        uint resultEdxAddress,
        uint heartbeatCountAddress,
        uint argsAddress
    ) =>
        AssembleCode(
            codeBase,
            assembler =>
            {
                var eax = AssemblerRegisters.eax;
                var ecx = AssemblerRegisters.ecx;
                var edx = AssemblerRegisters.edx;
                var esi = AssemblerRegisters.esi;
                var edi = AssemblerRegisters.edi;
                var esp = AssemblerRegisters.esp;

                var invokeLabel = assembler.CreateLabel("invoke-target");
                var pushLoopLabel = assembler.CreateLabel("push-loop");
                var skipCleanupLabel = assembler.CreateLabel("skip-cleanup");
                var epilogueLabel = assembler.CreateLabel("dispatcher-epilogue");

                assembler.pushfd();
                assembler.pushad();
                assembler.inc(DWordPtr(heartbeatCountAddress));

                assembler.mov(eax, DWordPtr(stateAddress));
                assembler.cmp(eax, (int)DispatcherState.Pending);
                assembler.jne(epilogueLabel);

                assembler.mov(DWordPtr(stateAddress), (int)DispatcherState.Executing);
                assembler.mov(ecx, DWordPtr(argCountAddress));
                assembler.test(ecx, ecx);
                assembler.je(invokeLabel);

                assembler.mov(esi, ecx);
                assembler.dec(esi);
                assembler.shl(esi, 2);
                assembler.mov(edi, unchecked((int)argsAddress));
                assembler.add(edi, esi);

                assembler.Label(ref pushLoopLabel);
                assembler.mov(eax, AssemblerRegisters.__dword_ptr[edi]);
                assembler.push(eax);
                assembler.sub(edi, 4);
                assembler.dec(ecx);
                assembler.jne(pushLoopLabel);

                assembler.Label(ref invokeLabel);
                assembler.mov(ecx, DWordPtr(ecxValueAddress));
                assembler.mov(edx, DWordPtr(edxValueAddress));
                assembler.mov(eax, DWordPtr(targetAddress));
                assembler.call(eax);

                assembler.mov(DWordPtr(resultEaxAddress), eax);
                assembler.mov(DWordPtr(resultEdxAddress), edx);
                assembler.mov(ecx, DWordPtr(cleanupModeAddress));
                assembler.test(ecx, ecx);
                assembler.jne(skipCleanupLabel);
                assembler.mov(ecx, DWordPtr(argCountAddress));
                assembler.shl(ecx, 2);
                assembler.add(esp, ecx);

                assembler.Label(ref skipCleanupLabel);
                assembler.mov(eax, DWordPtr(requestIdAddress));
                assembler.mov(DWordPtr(completedIdAddress), eax);
                assembler.mov(DWordPtr(stateAddress), (int)DispatcherState.Completed);

                assembler.Label(ref epilogueLabel);
                assembler.popad();
                assembler.popfd();
            }
        );

    private static byte[] AssembleRemoteThreadWorker(
        uint codeBase,
        uint stateAddress,
        uint requestIdAddress,
        uint completedIdAddress,
        uint cleanupModeAddress,
        uint targetAddress,
        uint ecxValueAddress,
        uint edxValueAddress,
        uint argCountAddress,
        uint resultEaxAddress,
        uint resultEdxAddress,
        uint argsAddress
    ) =>
        AssembleCode(
            codeBase,
            assembler =>
            {
                var eax = AssemblerRegisters.eax;
                var ecx = AssemblerRegisters.ecx;
                var edx = AssemblerRegisters.edx;
                var esi = AssemblerRegisters.esi;
                var edi = AssemblerRegisters.edi;
                var esp = AssemblerRegisters.esp;

                var invokeLabel = assembler.CreateLabel("invoke-target");
                var pushLoopLabel = assembler.CreateLabel("push-loop");
                var skipCleanupLabel = assembler.CreateLabel("skip-cleanup");

                assembler.pushfd();
                assembler.pushad();

                assembler.mov(DWordPtr(stateAddress), (int)DispatcherState.Executing);
                assembler.mov(ecx, DWordPtr(argCountAddress));
                assembler.test(ecx, ecx);
                assembler.je(invokeLabel);

                assembler.mov(esi, ecx);
                assembler.dec(esi);
                assembler.shl(esi, 2);
                assembler.mov(edi, unchecked((int)argsAddress));
                assembler.add(edi, esi);

                assembler.Label(ref pushLoopLabel);
                assembler.mov(eax, AssemblerRegisters.__dword_ptr[edi]);
                assembler.push(eax);
                assembler.sub(edi, 4);
                assembler.dec(ecx);
                assembler.jne(pushLoopLabel);

                assembler.Label(ref invokeLabel);
                assembler.mov(ecx, DWordPtr(ecxValueAddress));
                assembler.mov(edx, DWordPtr(edxValueAddress));
                assembler.mov(eax, DWordPtr(targetAddress));
                assembler.call(eax);

                assembler.mov(DWordPtr(resultEaxAddress), eax);
                assembler.mov(DWordPtr(resultEdxAddress), edx);
                assembler.mov(ecx, DWordPtr(cleanupModeAddress));
                assembler.test(ecx, ecx);
                assembler.jne(skipCleanupLabel);
                assembler.mov(ecx, DWordPtr(argCountAddress));
                assembler.shl(ecx, 2);
                assembler.add(esp, ecx);

                assembler.Label(ref skipCleanupLabel);
                assembler.mov(eax, DWordPtr(requestIdAddress));
                assembler.mov(DWordPtr(completedIdAddress), eax);
                assembler.mov(DWordPtr(stateAddress), (int)DispatcherState.Completed);
                assembler.popad();
                assembler.popfd();
                assembler.xor(eax, eax);
                assembler.ret(4);
            }
        );

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

    private static byte[] AssembleTail(uint codeBase, uint returnAddress) =>
        AssembleCode(codeBase, assembler => assembler.jmp((ulong)returnAddress));

    private static bool TryInstallMainThreadHook(
        ProcessMemory memory,
        nint remoteBlock,
        nint siteAddress,
        out byte[] originalBytes
    )
    {
        originalBytes = [];
        if (siteAddress == 0 || !memory.TryGetReadableRegion(siteAddress, out _))
            return false;

        try
        {
            var prefix = DecodePatchPrefix(memory, siteAddress);
            var block = BuildDispatcherBlock(memory, remoteBlock, siteAddress, prefix);
            memory.WriteBytes(remoteBlock, block);
            WriteJumpPatch(memory, siteAddress, remoteBlock + CodeOffset, prefix.HookLength);
            originalBytes = prefix.OriginalBytes;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static nint StartRemoteThread(ProcessMemory memory, nint startAddress)
    {
        var threadHandle = Kernel32NativeMethods.CreateRemoteThread(memory.Handle, 0, 0, startAddress, 0, 0, out _);
        if (threadHandle == 0)
            throw new IOException("Unable to create the remote debugger worker thread.");

        return threadHandle;
    }

    private static uint TimeoutMilliseconds(TimeSpan timeout)
    {
        var milliseconds = timeout.TotalMilliseconds;
        if (milliseconds <= 0)
            return 1;

        return milliseconds >= uint.MaxValue ? uint.MaxValue : (uint)milliseconds;
    }

    private static byte[] RelocateOriginalInstructions(uint codeBase, Instruction[] instructions)
    {
        using var stream = new MemoryStream();
        var writer = new StreamCodeWriter(stream);
        var block = new InstructionBlock(writer, instructions, codeBase);
        if (!BlockEncoder.TryEncode(32, block, out var errorMessage, out _, BlockEncoderOptions.None))
            throw new InvalidOperationException($"Unable to relocate debugger dispatcher entry: {errorMessage}");

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
                $"Unexpected dispatcher hook prefix length at {ProcessMemory.FormatAddress(siteAddress)}."
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

    private static uint ReadUInt32(byte[] bytes, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, sizeof(uint)));

    private static void WriteUInt32(byte[] bytes, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset, sizeof(uint)), value);

    public readonly record struct InvocationResult(
        uint RequestId,
        uint ResultEax,
        uint ResultEdx,
        DispatcherState State
    );

    private readonly record struct Snapshot(
        DispatcherState State,
        uint CompletedRequestId,
        uint ResultEax,
        uint ResultEdx,
        uint HeartbeatCount
    );

    private readonly record struct DecodedPatchPrefix(int HookLength, byte[] OriginalBytes, Instruction[] Instructions);

    private enum InvocationMode
    {
        MainThreadHook = 0,
        RemoteThreadFallback = 1,
    }

    public enum DispatcherState : uint
    {
        Idle = 0,
        Pending = 1,
        Executing = 2,
        Completed = 3,
    }
}
