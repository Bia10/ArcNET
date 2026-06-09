using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Tests;

[SupportedOSPlatform("windows")]
public sealed class InterceptServiceTests
{
    [Test]
    public async Task StartAndPoll_WhenInterceptionSessionEmitsEvents_ProjectsNewEventsAndDereferences()
    {
        var backend = new FakeInterceptBackend();
        backend.Results.Enqueue(
            new RuntimeInterceptionReadResult(
                2,
                DroppedEvents: 1,
                InconsistentRecords: 0,
                ContentionDrops: 2,
                [CreateCapturedEvent(1, 0x00402000), CreateCapturedEvent(2, 0x00403000)]
            )
        );
        backend.Results.Enqueue(
            new RuntimeInterceptionReadResult(
                3,
                DroppedEvents: 0,
                InconsistentRecords: 1,
                ContentionDrops: 0,
                [CreateCapturedEvent(3, 0x00404000)]
            )
        );
        backend.MemoryByAddress[0x00402000] = [0x6D, 0x61, 0x70, 0x00, 0x34, 0x12, 0x00, 0x00];
        backend.MemoryByAddress[0x00403000] = [0x41, 0x42, 0x43, 0x44];
        backend.MemoryByAddress[0x00404000] = [0x10, 0x20, 0x30, 0x40];

        var service = new InterceptService(backend);
        using var handle = service.Start(
            new InterceptStartRequest(
                CreateValidatedSession(),
                CreateTarget(),
                StackCaptureDwordCount: 8,
                new InterceptMutationRequest(
                    SkipOriginal: false,
                    CleanupBytes: 0,
                    ReturnEax: null,
                    ReturnEdx: null,
                    new InterceptRegisterOverrideRequest(null, null, null, null, null, null, null),
                    []
                ),
                [new InterceptDereferenceRequest("eax", InterceptDereferenceSourceKind.Eax, -1, 8)]
            )
        );

        var first = service.Poll(handle);
        var second = service.Poll(handle);

        await Assert.That(first.TotalEvents).IsEqualTo(2);
        await Assert.That(first.TotalDroppedEvents).IsEqualTo(1);
        await Assert.That(first.TotalContentionDrops).IsEqualTo(2);
        await Assert.That(first.Events.Count).IsEqualTo(2);
        await Assert.That(first.Events[0].Dereferences.Count).IsEqualTo(1);
        await Assert.That(first.Events[0].Dereferences[0].Ascii).Contains("map");
        await Assert.That(first.Events[0].PotentialHandles.Count).IsEqualTo(1);
        await Assert.That(first.Events[0].PotentialHandles[0].StackIndex).IsEqualTo(0);
        await Assert.That(first.Events[0].CallerSite.Contains("Arcanum.exe+", StringComparison.Ordinal)).IsTrue();
        await Assert.That(second.TotalEvents).IsEqualTo(3);
        await Assert.That(second.TotalWarnings).IsEqualTo(1);
        await Assert.That(second.Events.Count).IsEqualTo(1);
        await Assert.That(second.Events[0].Sequence).IsEqualTo(3u);
    }

    [Test]
    public async Task Start_WhenSessionDoesNotSupportInterception_Throws()
    {
        var service = new InterceptService(new FakeInterceptBackend());

        await Assert
            .That(() =>
                service.Start(
                    new InterceptStartRequest(
                        CreateUnsupportedSession(),
                        CreateTarget(),
                        StackCaptureDwordCount: 8,
                        new InterceptMutationRequest(
                            SkipOriginal: false,
                            CleanupBytes: 0,
                            ReturnEax: null,
                            ReturnEdx: null,
                            new InterceptRegisterOverrideRequest(null, null, null, null, null, null, null),
                            []
                        ),
                        []
                    )
                )
            )
            .Throws<InvalidOperationException>();
    }

    private static AttachedSessionSnapshot CreateValidatedSession() =>
        new(
            DateTimeOffset.UtcNow,
            SessionOrigin.Attach,
            "Arcanum.exe (PID 4242)",
            "Attached live session",
            @"C:\Games\Arcanum\Arcanum.exe @ 0x00400000",
            "Arcanum",
            4242,
            HasExited: false,
            new RuntimeFingerprint(
                "Arcanum",
                4242,
                RuntimeKind.Classic,
                "Arcanum.exe",
                @"C:\Games\Arcanum\Arcanum.exe",
                "0x00400000",
                3538944,
                3538944,
                DateTime.UtcNow
            ),
            new RuntimeProfileSnapshot(
                "validated-classic",
                "Arcanum.exe validated runtime profile",
                RuntimeKind.Classic,
                RuntimeSupportLevel.Validated,
                SupportsCatalogRvas: true,
                "Validated classic profile.",
                ModuleSha256: null,
                HashError: null
            ),
            new RuntimeCapabilityReport(
                RuntimeSupportLevel.Validated,
                DiagnosticsCapability.ReadMemory
                    | DiagnosticsCapability.ResolveRuntimeProfile
                    | DiagnosticsCapability.InterceptFunctions,
                []
            ),
            LaunchPreview: null,
            Notes: []
        );

    private static AttachedSessionSnapshot CreateUnsupportedSession() =>
        CreateValidatedSession() with
        {
            Capabilities = new RuntimeCapabilityReport(
                RuntimeSupportLevel.Exploratory,
                DiagnosticsCapability.ReadMemory | DiagnosticsCapability.ResolveRuntimeProfile,
                ["Interception unavailable."]
            ),
        };

    private static InterceptTarget CreateTarget() =>
        new(
            "teleport_do",
            0x00401000,
            0x00001000,
            "Arcanum.exe+0x00001000",
            "Low-level teleport entrypoint.",
            "catalog-rva"
        );

    private static RuntimeInterceptionCapturedEvent CreateCapturedEvent(uint sequence, uint eaxAddress)
    {
        var handle = ComposeHandle((int)sequence, sequence + 4);
        return new RuntimeInterceptionCapturedEvent(
            new RuntimeInterceptionDefinition(
                "teleport_do",
                0x00401000,
                0x00001000,
                "Arcanum.exe+0x00001000",
                8,
                new RuntimeInterceptionMutation(
                    RuntimeInterceptionExecutionMode.ContinueOriginal,
                    0,
                    0,
                    0,
                    default,
                    0,
                    new uint[RuntimeInterceptionSession.MaximumStackCaptureDwordCount],
                    0
                )
            ),
            sequence,
            0x00405000 + sequence,
            0x00005000 + sequence,
            0x00000202,
            new RuntimeInterceptionRegisters(
                Edi: 0,
                Esi: 0,
                Ebp: 0,
                OriginalEsp: 0x0018FF00,
                Ebx: 0,
                Edx: 0,
                Ecx: 0,
                Eax: eaxAddress
            ),
            [unchecked((uint)handle), unchecked((uint)(handle >> 32)), sequence + 2, sequence + 3]
        );
    }

    private static ulong ComposeHandle(int index, uint sequence) =>
        ((ulong)(uint)index << RuntimeOffsets.ObjHandleIndexShift)
        | ((ulong)sequence << RuntimeOffsets.ObjHandleSequenceShift)
        | RuntimeOffsets.ObjHandleMarkerValue;

    private sealed class FakeInterceptBackend : IInterceptBackend
    {
        public Queue<RuntimeInterceptionReadResult> Results { get; } = [];

        public Dictionary<uint, byte[]> MemoryByAddress { get; } = [];

        public IInterceptSession StartIntercept(int processId, RuntimeInterceptionDefinition definition) =>
            new FakeInterceptSession(Results, MemoryByAddress);
    }

    private sealed class FakeInterceptSession(
        Queue<RuntimeInterceptionReadResult> results,
        Dictionary<uint, byte[]> memoryByAddress
    ) : IInterceptSession
    {
        public bool HasExited => false;

        public string ModuleFileName => "Arcanum.exe";

        public RuntimeInterceptionReadResult ReadSince(uint lastSequence) =>
            results.Count > 0 ? results.Dequeue() : new RuntimeInterceptionReadResult(lastSequence, 0, 0, 0, []);

        public InterceptMemoryReadResult ReadMemory(uint address, int requestedByteCount)
        {
            if (!memoryByAddress.TryGetValue(address, out var bytes))
            {
                return new InterceptMemoryReadResult(false, requestedByteCount, 0, [], "Missing fake memory payload.");
            }

            var actualBytes = bytes.Length > requestedByteCount ? bytes[..requestedByteCount] : bytes;
            return new InterceptMemoryReadResult(true, requestedByteCount, actualBytes.Length, actualBytes, null);
        }

        public void Dispose() { }
    }
}
