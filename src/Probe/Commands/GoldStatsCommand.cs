using ArcNET.Editor;
using ArcNET.GameObjects;
using Probe;

namespace Probe.Commands;

internal sealed class GoldStatsCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var ctx = SharedProbeContext.Load(saveDir, slot4);

        Console.WriteLine("\n=== Mode 2: gold=99999 + stats=20 ===");
        var (gsiOut, tfaiOut, tfafOut) = SharedProbeContext.GetLegacyOutputPaths(saveDir);
        SaveGameWriter.Save(
            ctx.Save,
            gsiOut,
            tfaiOut,
            tfafOut,
            new SaveGameUpdates
            {
                UpdatedMobileMds = ctx.BuildUpdated(pc =>
                    new CharacterBuilder(pc).WithGold(99999).WithBaseStats([20, 20, 20, 20, 20, 20]).Build()
                ),
            }
        );

        Console.WriteLine(
            $"  TFAF={new FileInfo(tfafOut).Length}B  delta={new FileInfo(tfafOut).Length - ctx.TfafBytes.Length}B"
        );
        return Task.CompletedTask;
    }
}
