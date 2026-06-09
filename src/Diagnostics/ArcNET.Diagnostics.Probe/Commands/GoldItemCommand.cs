using ArcNET.Core;
using ArcNET.Diagnostics;
using Probe;

namespace Probe.Commands;

internal sealed class GoldItemCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var ctx = SharedProbeContext.Load(saveDir, slot4);
        var inspection = SaveGoldItemInspectionService.Create(ctx.Save);

        Console.WriteLine("\n=== Mode 8: Gold item v2 SAR dump ===");
        Console.WriteLine(
            $"  Save: {ctx.SlotStem}  LeaderName={inspection.LeaderName}  Level={inspection.LeaderLevel}"
        );

        foreach (var file in inspection.Files)
        {
            Console.WriteLine($"\n  {file.Path}: {file.Items.Count} gold items");
            foreach (var item in file.Items)
            {
                var marker = item.FoundInPlayerCharacter ? "  *** PC GOLD ***" : string.Empty;
                var parentFlag = item.HasParent ? " [in-inventory]" : string.Empty;
                var properties = string.Join(
                    ",",
                    item.PositiveInt32Properties.Select(static property => $"b{property.Field}={property.Value}")
                );
                Console.WriteLine(
                    $"    oid={ValueBufferText.FormatHex(item.ObjectIdBytes)}  qty(bit97)={item.Quantity}{parentFlag}  props=[{properties}]{marker}"
                );
            }
        }

        return Task.CompletedTask;
    }
}
