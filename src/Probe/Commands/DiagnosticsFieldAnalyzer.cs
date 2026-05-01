using ArcNET.Core;
using ArcNET.GameObjects;
using Bia.ValueBuffers;
using Probe;

namespace Probe.Commands;

internal static class DiagnosticsFieldAnalyzer
{
    private static readonly ObjectField[] s_linkSummaryFields =
    [
        ObjectField.ObjFCritterFleeingFrom,
        ObjectField.ObjFCritterGold,
        ObjectField.ObjFCritterArrows,
        ObjectField.ObjFCritterBullets,
        ObjectField.ObjFCritterPowerCells,
        ObjectField.ObjFCritterFuel,
        ObjectField.ObjFCritterInventoryListIdx,
        ObjectField.ObjFCritterFollowerIdx,
    ];

    public static async Task WriteLinkFieldSummaryAsync(SharedProbeContext ctx)
    {
        await Console.Out.WriteLineAsync("  PC link-field raw sizes:");

        foreach (var field in s_linkSummaryFields)
            await WriteLinkFieldSummaryAsync(ctx, field);
    }

    private static async Task WriteLinkFieldSummaryAsync(SharedProbeContext ctx, ObjectField field)
    {
        var matches = ctx
            .AllPcs.SelectMany(pc => pc.data.Properties.Where(property => property.Field == field))
            .ToList();

        if (matches.Count == 0)
        {
            await Console.Out.WriteLineAsync($"    {field}(bit={(int)field}): absent");
            return;
        }

        var rawSizes = matches.Select(property => property.RawBytes.Length).Distinct().Order().ToArray();
        var sampleHex = ValueBufferText.FormatHex(matches[0].RawBytes);
        await Console.Out.WriteLineAsync(
            $"    {field}(bit={(int)field}): count={matches.Count} rawSizes=[{string.Join(",", rawSizes)}] sample={sampleHex}"
        );
    }
}
