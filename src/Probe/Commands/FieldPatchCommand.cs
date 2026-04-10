using ArcNET.Editor;
using ArcNET.Formats;
using ArcNET.GameObjects;
using Probe;

namespace Probe.Commands;

internal sealed class FieldPatchCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var ctx = SharedProbeContext.Load(saveDir, slot4);

        Console.WriteLine("\n=== Mode 3: raw-patch field 80 to 9999 ===");
        Console.WriteLine(
            $"  Field 80 current: {ctx.AllPcs[0].data.Properties.First(p => p.Field == (ObjectField)80).GetInt32()}"
        );

        var patched = ctx.BuildUpdated(pc =>
        {
            var newProps = pc
                .Properties.Select(p =>
                    p.Field == (ObjectField)80 ? ObjectPropertyFactory.ForInt32((ObjectField)80, 9999) : p
                )
                .ToList();
            return new MobData { Header = pc.Header, Properties = newProps.AsReadOnly() };
        });

        var (gsiOut, tfaiOut, tfafOut) = SharedProbeContext.GetLegacyOutputPaths(saveDir);
        ArcNET.Editor.SaveGameWriter.Save(
            ctx.Save,
            gsiOut,
            tfaiOut,
            tfafOut,
            new SaveGameUpdates { UpdatedMobileMds = patched }
        );
        SharedProbeContext.CompareBytes($"TFAF vs {ctx.SlotStem}", File.ReadAllBytes(tfafOut), ctx.TfafBytes);
        Console.WriteLine($"  delta={new FileInfo(tfafOut).Length - ctx.TfafBytes.Length}B");

        return Task.CompletedTask;
    }
}
