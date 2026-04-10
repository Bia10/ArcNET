namespace Probe.Commands;

internal sealed class HelpCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        Console.WriteLine("ArcNET Probe — Arcanum save-game diagnostic tool");
        Console.WriteLine();
        Console.WriteLine("Usage: probe [--save-dir <path>] <mode> [slot] [options]");
        Console.WriteLine();
        Console.WriteLine("Modes:");
        Console.WriteLine("  0  roundtrip       Raw round-trip byte-identical check");
        Console.WriteLine("  1  gold            Set gold = 99999");
        Console.WriteLine("  2  gold-stats      Set gold = 99999 + all base stats = 20");
        Console.WriteLine("  3  field-patch     Raw-patch field 80 to 9999");
        Console.WriteLine("  4  raw-gold-insert Insert gold at raw byte offset");
        Console.WriteLine("  5  raw-gold-coll   Gold + property collection update");
        Console.WriteLine("  6  raw-patch-files Cross-file raw gold patch");
        Console.WriteLine("  7  sar-dump        Full SAR dump of v2 PC records");
        Console.WriteLine("  8  gold-item       Gold item v2 SAR analysis");
        Console.WriteLine("  9  sar-diff        Multi-slot SAR difference engine");
        Console.WriteLine(" 10  full-sar-dump   Full element dump of player v2 record");
        Console.WriteLine(" 11  binary-diff     Binary diff between two save slots");
        Console.WriteLine(" 12  diagnostics     Quick diagnostics (files, PC info, types)");
        Console.WriteLine(" 13  field-evolution Character field evolution tracker");
        Console.WriteLine(" 14  quest-book      Quest book + reputation decoder");
        Console.WriteLine();
        Console.WriteLine("Environment: ARCNET_SAVE_DIR — override save directory");
        Console.WriteLine($"Save dir: {saveDir}");
        return Task.CompletedTask;
    }
}
