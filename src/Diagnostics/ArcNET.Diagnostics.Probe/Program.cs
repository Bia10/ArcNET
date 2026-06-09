using Probe;
using Probe.Commands;

var saveDir = ProbeConfig.ResolveSaveDir(args);
var outputOptions = ProbeConfig.ResolveOutputOptions(args);
var strippedArgs = ProbeConfig.StripGlobalArgs(args);
var modeArg = strippedArgs.Length > 0 ? strippedArgs[0] : "help";
var modeArgs = strippedArgs.Length > 1 ? strippedArgs[1..] : [];

IProbeCommand command = modeArg switch
{
    "0" or "roundtrip" => new RoundTripCommand(),
    "1" or "gold" => new GoldCommand(),
    "2" or "gold-stats" => new GoldStatsCommand(),
    "3" or "field-patch" => new FieldPatchCommand(),
    "4" or "raw-gold-insert" => new RawGoldInsertCommand(),
    "5" or "raw-gold-coll" => new RawGoldCollCommand(),
    "6" or "raw-patch-files" => new RawPatchFilesCommand(),
    "7" or "sar-dump" => new SarDumpCommand(),
    "8" or "gold-item" => new GoldItemCommand(),
    "9" or "sar-diff" => new SarDiffCommand(),
    "10" or "full-sar-dump" => new FullSarDumpCommand(),
    "11" or "binary-diff" => new BinaryDiffCommand(),
    "12" or "diagnostics" => new DiagnosticsCommand(),
    "13" or "field-evolution" => new FieldEvolutionCommand(),
    "14" or "quest-book" => new QuestBookCommand(),
    "15" or "npc-scan" => new NpcScanCommand(),
    "16" or "char-summary" => new CharSummaryCommand(),
    "17" or "pc-data" => new PcDataCommand(),
    _ => new HelpCommand(),
};

using var outputScope = ProbeOutputScope.Create(modeArg, outputOptions);
await command.RunAsync(saveDir, modeArgs);
