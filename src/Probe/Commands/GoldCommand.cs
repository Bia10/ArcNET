using ArcNET.Editor;
using ArcNET.Formats;
using ArcNET.GameObjects;
using Probe;

namespace Probe.Commands;

internal sealed class GoldCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var ctx = SharedProbeContext.Load(saveDir, slot4);

        Console.WriteLine("\n=== Mode 1: gold=99999 ===");
        var (gsiOut, tfaiOut, tfafOut) = SharedProbeContext.GetLegacyOutputPaths(saveDir);
        var updatedMobs =
            ctx.PcMobFiles.Count > 0
                ? ctx.PcMobFiles.ToDictionary(
                    entry => entry.Key,
                    entry => new CharacterBuilder(entry.Value).WithGold(99999).Build(),
                    StringComparer.OrdinalIgnoreCase
                )
                : null;

        ArcNET.Editor.SaveGameWriter.Save(
            ctx.Save,
            gsiOut,
            tfaiOut,
            tfafOut,
            new SaveGameUpdates
            {
                UpdatedMobiles = updatedMobs,
                UpdatedMobileMds = ctx.BuildUpdated(pc => new CharacterBuilder(pc).WithGold(99999).Build()),
            }
        );

        Console.WriteLine(
            $"  TFAF={new FileInfo(tfafOut).Length}B  delta={new FileInfo(tfafOut).Length - ctx.TfafBytes.Length}B"
        );
        var verify = SaveGameLoader.Load(gsiOut, tfaiOut, tfafOut);
        var goldCount = 0;
        var firstGold = -1;
        foreach (var (_, verifiedMd) in verify.MobileMds)
        {
            foreach (var verifiedRecord in verifiedMd.Records)
            {
                var goldProp = verifiedRecord.Data?.Properties.FirstOrDefault(p =>
                    p.Field == ObjectField.ObjFCritterGold
                );
                if (goldProp is null)
                    continue;

                goldCount++;
                if (firstGold < 0)
                    firstGold = goldProp.GetInt32();
            }
        }

        Console.WriteLine($"  VERIFY: PC records with gold: {goldCount}  first value={firstGold}");
        return Task.CompletedTask;
    }
}
