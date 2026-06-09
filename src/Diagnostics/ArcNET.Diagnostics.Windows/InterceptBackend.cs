using System.Runtime.Versioning;
using ArcNET.Diagnostics;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class InterceptBackend : IInterceptBackend
{
    public IInterceptSession StartIntercept(int processId, RuntimeInterceptionDefinition definition)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live function interception currently requires Windows.");

        var memory = ProcessMemory.Attach(processId);
        try
        {
            var interceptSession = RuntimeInterceptionSession.Install(memory, definition);
            return new InterceptSessionLease(memory, interceptSession);
        }
        catch
        {
            memory.Dispose();
            throw;
        }
    }

    private sealed class InterceptSessionLease(ProcessMemory memory, RuntimeInterceptionSession interceptSession)
        : IInterceptSession
    {
        private readonly ProcessMemory _memory = memory;
        private readonly RuntimeInterceptionSession _interceptSession = interceptSession;
        private readonly string _moduleFileName = Path.GetFileName(memory.ModulePath);

        public bool HasExited => _memory.HasExited;

        public string ModuleFileName => _moduleFileName;

        public RuntimeInterceptionReadResult ReadSince(uint lastSequence) => _interceptSession.ReadSince(lastSequence);

        public InterceptMemoryReadResult ReadMemory(uint address, int requestedByteCount)
        {
            if (address == 0)
            {
                return new InterceptMemoryReadResult(false, requestedByteCount, 0, [], "Captured address is null.");
            }

            try
            {
                var pointer = (nint)(long)address;
                if (!_memory.TryGetReadableRegion(pointer, out var region))
                {
                    return new InterceptMemoryReadResult(
                        false,
                        requestedByteCount,
                        0,
                        [],
                        "Captured address is not inside a readable remote memory region."
                    );
                }

                var regionBase = (ulong)_memory.ToUInt32Address(region.BaseAddress);
                var regionLimit = regionBase + (ulong)region.Size;
                var availableBytes = checked((int)Math.Min(regionLimit - address, (ulong)requestedByteCount));
                if (availableBytes <= 0)
                {
                    return new InterceptMemoryReadResult(
                        false,
                        requestedByteCount,
                        0,
                        [],
                        "No readable bytes remain at the captured address."
                    );
                }

                var bytes = _memory.ReadBytes(pointer, availableBytes);
                return new InterceptMemoryReadResult(true, requestedByteCount, bytes.Length, bytes, null);
            }
            catch (Exception ex)
            {
                return new InterceptMemoryReadResult(false, requestedByteCount, 0, [], ex.Message);
            }
        }

        public void Dispose()
        {
            _interceptSession.Dispose();
            _memory.Dispose();
        }
    }
}
