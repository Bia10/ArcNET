using ArcNET.Formats;
using Probe;

namespace Probe.Commands;

internal sealed class RawPatchFilesCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var ctx = SharedProbeContext.Load(saveDir, slot4);

        Console.WriteLine("\n=== Mode 6: raw-patch gold 41074→99999 across all inner files ===");
        byte[] needle = new byte[] { 0x52, 0xA0, 0x00, 0x00 };
        byte[] patch = new byte[] { 0x9F, 0x86, 0x01, 0x00 };
        var patchedFiles = new Dictionary<string, byte[]>(ctx.Save.Files, StringComparer.OrdinalIgnoreCase);
        var totalPatches = 0;
        foreach (var key in patchedFiles.Keys.ToList())
        {
            var fileBytes = patchedFiles[key];
            byte[]? modified = null;
            for (var offset = 0; offset <= fileBytes.Length - 4; offset++)
            {
                if (
                    fileBytes[offset] == needle[0]
                    && fileBytes[offset + 1] == needle[1]
                    && fileBytes[offset + 2] == needle[2]
                    && fileBytes[offset + 3] == needle[3]
                )
                {
                    modified ??= (byte[])fileBytes.Clone();
                    patch.CopyTo(modified.AsSpan(offset));
                    Console.WriteLine($"  Patched {key}  offset={offset}");
                    totalPatches++;
                }
            }

            if (modified is not null)
                patchedFiles[key] = modified;
        }

        Console.WriteLine($"  Total patches: {totalPatches}");
        var (gsiOut, tfaiOut, tfafOut) = SharedProbeContext.GetLegacyOutputPaths(saveDir);
        File.WriteAllBytes(gsiOut, SaveInfoFormat.WriteToArray(ctx.Save.Info));
        File.WriteAllBytes(tfaiOut, SaveIndexFormat.WriteToArray(ctx.Save.Index));
        File.WriteAllBytes(tfafOut, TfafFormat.Pack(ctx.Save.Index, patchedFiles));
        SharedProbeContext.CompareBytes($"TFAF vs {ctx.SlotStem}", File.ReadAllBytes(tfafOut), ctx.TfafBytes);
        Console.WriteLine($"  delta={new FileInfo(tfafOut).Length - ctx.TfafBytes.Length}B");

        return Task.CompletedTask;
    }
}
