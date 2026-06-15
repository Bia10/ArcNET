using ArcNET.Diagnostics;
using ArcNET.Formats;
using ArcNET.GameData.SaveGames;
using ArcNET.GameObjects;
using Probe;

namespace Probe.Commands;

internal sealed class GoldCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var ctx = SharedProbeContext.Load(saveDir, slot4);

        Console.WriteLine("\n=== Mode 1: mobile.mdy gold=99999 ===");
        var (gsiOut, tfaiOut, tfafOut) = SharedProbeContext.GetLegacyOutputPaths(saveDir);
        var updatedMobs =
            ctx.PcMobFiles.Count > 0
                ? ctx.PcMobFiles.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value,
                    StringComparer.OrdinalIgnoreCase
                )
                : null;

        SaveGameWriter.Save(ctx.Save, gsiOut, tfaiOut, tfafOut, new SaveGameUpdates { UpdatedMobiles = updatedMobs });

        Console.WriteLine(
            $"  TFAF={new FileInfo(tfafOut).Length}B  delta={new FileInfo(tfafOut).Length - ctx.TfafBytes.Length}B"
        );
        var verify = SaveSlotLoadService.LoadFiles(gsiOut, tfaiOut, tfafOut);
        var goldCount = 0;
        var firstGold = -1;
        foreach (var (_, verifiedMd) in verify.MobileMds)
        {
            foreach (var verifiedRecord in verifiedMd.Records)
            {
                var goldProp = verifiedRecord.Data?.Properties.FirstOrDefault(p => p.Field == ObjectField.CritterGold);
                if (goldProp is null)
                    continue;

                goldCount++;
                if (firstGold < 0)
                    firstGold = goldProp.GetObjectId().OidType;
            }
        }

        Console.WriteLine($"  VERIFY: PC records with gold handles: {goldCount}  first oidType={firstGold}");
        return Task.CompletedTask;
    }
}
