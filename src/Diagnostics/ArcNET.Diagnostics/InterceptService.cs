using System.Buffers.Binary;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed class InterceptService(IInterceptBackend backend)
{
    public InterceptHandle Start(InterceptStartRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Target);
        ArgumentNullException.ThrowIfNull(request.Mutation);
        ArgumentNullException.ThrowIfNull(request.Mutation.Registers);
        ArgumentNullException.ThrowIfNull(request.Mutation.ArgumentOverrides);
        ArgumentNullException.ThrowIfNull(request.Dereferences);

        EnsureInterceptSupport(request.Session);
        Validate(request);

        var definition = new RuntimeInterceptionDefinition(
            request.Target.Key,
            request.Target.Address,
            request.Target.Rva,
            request.Target.Site,
            request.StackCaptureDwordCount,
            CreateMutation(request)
        );
        var session = backend.StartIntercept(request.Session.ProcessId, definition);
        try
        {
            var snapshot = CreateSnapshot(
                request,
                totalEvents: 0,
                totalDroppedEvents: 0,
                totalContentionDrops: 0,
                totalWarnings: 0,
                []
            );
            return new InterceptHandle(session, request, snapshot);
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    public InterceptSnapshot Poll(InterceptHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);

        var poll = handle.Session.ReadSince(handle.LastSequence);
        handle.LastSequence = poll.WriteSequence;
        handle.TotalDroppedEvents += poll.DroppedEvents;
        handle.TotalContentionDrops += poll.ContentionDrops;
        handle.TotalWarnings += poll.InconsistentRecords;

        var newEvents = poll.Events.Select(capturedEvent => CreateEventSnapshot(handle, capturedEvent)).ToArray();
        handle.TotalEvents += newEvents.Length;

        var snapshot = CreateSnapshot(
            handle.Request,
            handle.TotalEvents,
            handle.TotalDroppedEvents,
            handle.TotalContentionDrops,
            handle.TotalWarnings,
            newEvents
        );
        handle.UpdateSnapshot(snapshot);
        return snapshot;
    }

    private static void EnsureInterceptSupport(AttachedSessionSnapshot session)
    {
        if (!session.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.InterceptFunctions))
        {
            throw new InvalidOperationException(
                "This runtime does not currently support live function interception. Stay in watch/read mode until the runtime reaches validated interception support."
            );
        }
    }

    private static void Validate(InterceptStartRequest request)
    {
        if (request.StackCaptureDwordCount is < 1 or > RuntimeInterceptionLimits.MaximumStackCaptureDwordCount)
        {
            throw new InvalidOperationException(
                $"Interception stack capture dword count must be between 1 and {RuntimeInterceptionLimits.MaximumStackCaptureDwordCount}."
            );
        }

        foreach (var argumentOverride in request.Mutation.ArgumentOverrides)
        {
            if (argumentOverride.Index is < 0 or >= RuntimeInterceptionLimits.MaximumStackCaptureDwordCount)
            {
                throw new InvalidOperationException(
                    $"Argument override index must be between 0 and {RuntimeInterceptionLimits.MaximumStackCaptureDwordCount - 1}."
                );
            }
        }

        foreach (var dereference in request.Dereferences)
        {
            if (dereference.ByteCount is < 1 or > 256)
                throw new InvalidOperationException("Dereference byte count must be between 1 and 256.");

            if (
                dereference.SourceKind == InterceptDereferenceSourceKind.StackIndex
                && dereference.Index is < 0 or >= RuntimeInterceptionLimits.MaximumStackCaptureDwordCount
            )
            {
                throw new InvalidOperationException(
                    $"Stack dereference index must be between 0 and {RuntimeInterceptionLimits.MaximumStackCaptureDwordCount - 1}."
                );
            }
        }

        if (!request.Mutation.SkipOriginal && request.Mutation.CleanupBytes != 0)
            throw new InvalidOperationException("Cleanup bytes are only meaningful together with SkipOriginal.");

        if (
            !request.Mutation.SkipOriginal
            && (request.Mutation.ReturnEax.HasValue || request.Mutation.ReturnEdx.HasValue)
        )
        {
            throw new InvalidOperationException(
                "Synthetic return registers require SkipOriginal because they only apply to short-circuited calls."
            );
        }
    }

    private static RuntimeInterceptionMutation CreateMutation(InterceptStartRequest request)
    {
        var registers = new RuntimeInterceptionRegisters(
            request.Mutation.Registers.Edi.GetValueOrDefault(),
            request.Mutation.Registers.Esi.GetValueOrDefault(),
            request.Mutation.Registers.Ebp.GetValueOrDefault(),
            0,
            request.Mutation.Registers.Ebx.GetValueOrDefault(),
            request.Mutation.Registers.Edx.GetValueOrDefault(),
            request.Mutation.Registers.Ecx.GetValueOrDefault(),
            request.Mutation.Registers.Eax.GetValueOrDefault()
        );
        var argumentOverrides = new uint[RuntimeInterceptionLimits.MaximumStackCaptureDwordCount];
        var argumentOverrideMask = 0u;
        foreach (var argumentOverride in request.Mutation.ArgumentOverrides)
        {
            argumentOverrides[argumentOverride.Index] = argumentOverride.Value;
            argumentOverrideMask |= 1u << argumentOverride.Index;
        }

        return new RuntimeInterceptionMutation(
            request.Mutation.SkipOriginal
                ? RuntimeInterceptionExecutionMode.SkipOriginal
                : RuntimeInterceptionExecutionMode.ContinueOriginal,
            request.Mutation.CleanupBytes,
            request.Mutation.ReturnEax.GetValueOrDefault(),
            request.Mutation.ReturnEdx.GetValueOrDefault(),
            registers,
            BuildRegisterOverrideMask(request.Mutation.Registers),
            argumentOverrides,
            argumentOverrideMask
        );
    }

    private static uint BuildRegisterOverrideMask(InterceptRegisterOverrideRequest registers)
    {
        var mask = 0u;
        if (registers.Edi.HasValue)
            mask |= 1u << (int)RuntimeInterceptionRegister.Edi;
        if (registers.Esi.HasValue)
            mask |= 1u << (int)RuntimeInterceptionRegister.Esi;
        if (registers.Ebp.HasValue)
            mask |= 1u << (int)RuntimeInterceptionRegister.Ebp;
        if (registers.Ebx.HasValue)
            mask |= 1u << (int)RuntimeInterceptionRegister.Ebx;
        if (registers.Edx.HasValue)
            mask |= 1u << (int)RuntimeInterceptionRegister.Edx;
        if (registers.Ecx.HasValue)
            mask |= 1u << (int)RuntimeInterceptionRegister.Ecx;
        if (registers.Eax.HasValue)
            mask |= 1u << (int)RuntimeInterceptionRegister.Eax;
        return mask;
    }

    private static InterceptSnapshot CreateSnapshot(
        InterceptStartRequest request,
        int totalEvents,
        int totalDroppedEvents,
        int totalContentionDrops,
        int totalWarnings,
        IReadOnlyList<InterceptEventSnapshot> events
    )
    {
        var status =
            totalEvents == 0
                ? $"Intercepting {request.Target.Key} on {request.Session.DisplayName}. No events captured yet."
                : $"Intercepting {request.Target.Key} on {request.Session.DisplayName}.";
        var summary =
            $"Events {totalEvents} · dropped {totalDroppedEvents} · contention {totalContentionDrops} · warnings {totalWarnings}";
        return new InterceptSnapshot(
            DateTimeOffset.UtcNow,
            IsRunning: true,
            status,
            summary,
            request.Target.Key,
            request.Target.Site,
            request.Target.Summary,
            request.Target.Resolution,
            request.Mutation.SkipOriginal
                ? RuntimeInterceptionExecutionMode.SkipOriginal.ToString()
                : RuntimeInterceptionExecutionMode.ContinueOriginal.ToString(),
            request.StackCaptureDwordCount,
            totalEvents,
            totalDroppedEvents,
            totalContentionDrops,
            totalWarnings,
            events
        );
    }

    private static InterceptEventSnapshot CreateEventSnapshot(
        InterceptHandle handle,
        RuntimeInterceptionCapturedEvent capturedEvent
    )
    {
        var callerSite =
            capturedEvent.CallerRva > 0
                ? CodeCatalog.FormatModuleAddress(handle.Session.ModuleFileName, capturedEvent.CallerRva)
                : FormatUInt32(capturedEvent.ReturnAddress);
        return new InterceptEventSnapshot(
            DateTimeOffset.UtcNow,
            capturedEvent.Sequence,
            callerSite,
            FormatUInt32(capturedEvent.ReturnAddress),
            $"0x{capturedEvent.CallerRva:X8}",
            FormatUInt32(capturedEvent.Eflags),
            CreateRegistersSnapshot(capturedEvent.Registers),
            [.. capturedEvent.StackDwords.Select(FormatUInt32)],
            DescribePotentialHandles(capturedEvent.StackDwords),
            DescribeDereferences(handle.Session, capturedEvent, handle.Request.Dereferences)
        );
    }

    private static InterceptRegistersSnapshot CreateRegistersSnapshot(RuntimeInterceptionRegisters registers) =>
        new(
            FormatUInt32(registers.Edi),
            FormatUInt32(registers.Esi),
            FormatUInt32(registers.Ebp),
            FormatUInt32(registers.OriginalEsp),
            FormatUInt32(registers.Ebx),
            FormatUInt32(registers.Edx),
            FormatUInt32(registers.Ecx),
            FormatUInt32(registers.Eax)
        );

    private static IReadOnlyList<InterceptPotentialHandleSnapshot> DescribePotentialHandles(uint[] stackDwords)
    {
        List<InterceptPotentialHandleSnapshot> handles = [];
        for (var index = 0; index + 1 < stackDwords.Length; index++)
        {
            var candidate = ((ulong)stackDwords[index + 1] << 32) | stackDwords[index];
            if ((candidate & RuntimeOffsets.ObjHandleMarkerMask) != RuntimeOffsets.ObjHandleMarkerValue)
                continue;

            handles.Add(new InterceptPotentialHandleSnapshot(index, RuntimeSemanticCatalog.FormatHandle(candidate)));
        }

        return handles.Count == 0 ? [] : [.. handles];
    }

    private static IReadOnlyList<InterceptDereferenceSnapshot> DescribeDereferences(
        IInterceptSession session,
        RuntimeInterceptionCapturedEvent capturedEvent,
        IReadOnlyList<InterceptDereferenceRequest> dereferences
    )
    {
        if (dereferences.Count == 0)
            return [];

        return [.. dereferences.Select(dereference => DescribeDereference(session, capturedEvent, dereference))];
    }

    private static InterceptDereferenceSnapshot DescribeDereference(
        IInterceptSession session,
        RuntimeInterceptionCapturedEvent capturedEvent,
        InterceptDereferenceRequest dereference
    )
    {
        var address = ResolveDereferenceAddress(capturedEvent, dereference);
        var memory = session.ReadMemory(address, dereference.ByteCount);
        if (!memory.Success)
        {
            return new InterceptDereferenceSnapshot(
                dereference.Source,
                FormatUInt32(address),
                dereference.ByteCount,
                memory.ReadByteCount,
                string.Empty,
                string.Empty,
                [],
                memory.Error
            );
        }

        return new InterceptDereferenceSnapshot(
            dereference.Source,
            FormatUInt32(address),
            memory.RequestedByteCount,
            memory.ReadByteCount,
            Convert.ToHexString(memory.Bytes),
            ToAsciiPreview(memory.Bytes),
            DescribeUInt32Preview(memory.Bytes),
            null
        );
    }

    private static uint ResolveDereferenceAddress(
        RuntimeInterceptionCapturedEvent capturedEvent,
        InterceptDereferenceRequest dereference
    )
    {
        var registers = capturedEvent.Registers;
        return dereference.SourceKind switch
        {
            InterceptDereferenceSourceKind.Eax => registers.Eax,
            InterceptDereferenceSourceKind.Ecx => registers.Ecx,
            InterceptDereferenceSourceKind.Edx => registers.Edx,
            InterceptDereferenceSourceKind.Ebx => registers.Ebx,
            InterceptDereferenceSourceKind.Esi => registers.Esi,
            InterceptDereferenceSourceKind.Edi => registers.Edi,
            InterceptDereferenceSourceKind.Ebp => registers.Ebp,
            InterceptDereferenceSourceKind.OriginalEsp => registers.OriginalEsp,
            InterceptDereferenceSourceKind.StackIndex => dereference.Index < capturedEvent.StackDwords.Length
                ? capturedEvent.StackDwords[dereference.Index]
                : 0,
            _ => 0,
        };
    }

    private static string ToAsciiPreview(byte[] bytes)
    {
        Span<char> chars = stackalloc char[bytes.Length];
        for (var index = 0; index < bytes.Length; index++)
        {
            var value = bytes[index];
            chars[index] = value is >= 32 and <= 126 ? (char)value : '.';
        }

        return new string(chars);
    }

    private static IReadOnlyList<string> DescribeUInt32Preview(byte[] bytes)
    {
        var count = Math.Min(bytes.Length / sizeof(uint), 4);
        if (count == 0)
            return [];

        List<string> preview = new(count);
        for (var index = 0; index < count; index++)
        {
            preview.Add(
                FormatUInt32(BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(index * sizeof(uint), sizeof(uint))))
            );
        }

        return preview;
    }

    private static string FormatUInt32(uint value) => $"0x{value:X8}";
}
