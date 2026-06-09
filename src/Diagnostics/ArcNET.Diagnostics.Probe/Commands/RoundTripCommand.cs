using ArcNET.Editor;
using Probe;

namespace Probe.Commands;

internal sealed class RoundTripCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var ctx = SharedProbeContext.Load(saveDir, slot4);

        Console.WriteLine("\n=== Mode 0: raw round-trip (byte-identical check) ===");
        var (gsiOut, tfaiOut, tfafOut) = SharedProbeContext.GetLegacyOutputPaths(saveDir);
        SaveGameWriter.Save(ctx.Save, gsiOut, tfaiOut, tfafOut);
        SharedProbeContext.CompareBytes($"TFAF vs {ctx.SlotStem}", File.ReadAllBytes(tfafOut), ctx.TfafBytes);

        return Task.CompletedTask;
    }
}
