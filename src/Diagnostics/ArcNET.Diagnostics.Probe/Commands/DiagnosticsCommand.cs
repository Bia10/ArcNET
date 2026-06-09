using ArcNET.Diagnostics;
using Probe;

namespace Probe.Commands;

internal sealed class DiagnosticsCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var ctx = SharedProbeContext.Load(saveDir, slot4);
        var audit = SaveFileAuditService.Create(new SaveFileAuditRequest(ctx.Save));

        Console.WriteLine($"\n=== Mode 12: diagnostics - {ctx.SlotStem} ===\n");
        WriteSaveSummary(audit);
        WriteAssetSummary(audit);
        WriteValidationSummary(audit);
        WriteMobileMdySummary(audit);
        WriteObjectFieldSummary(audit);
        WritePlayerCharacterSummary(audit.PlayerCharacter);
        return Task.CompletedTask;
    }

    private static void WriteSaveSummary(SaveFileAuditSnapshot audit)
    {
        Console.WriteLine("  SAVE");
        Console.WriteLine($"    leader=\"{audit.LeaderName}\"  lv={audit.LeaderLevel}  map={audit.MapId}");
        Console.WriteLine($"    captured={audit.CapturedAt:O}");
        Console.WriteLine();
    }

    private static void WriteAssetSummary(SaveFileAuditSnapshot audit)
    {
        Console.WriteLine("  ASSETS");
        Console.WriteLine(
            $"    files={audit.Assets.TotalFileCount} raw={audit.Assets.RawFileCount} parseErrors={audit.Assets.ParseErrorCount}"
        );
        Console.WriteLine(
            $"    mob={audit.Assets.MobCount} mobileMd={audit.Assets.MobileMdCount} mobileMdy={audit.Assets.MobileMdyCount} sectors={audit.Assets.SectorCount}"
        );
        Console.WriteLine(
            $"    jumps={audit.Assets.JumpFileCount} props={audit.Assets.MapPropertiesCount} mes={audit.Assets.MessageCount} tmf={audit.Assets.TownMapFogCount}"
        );
        Console.WriteLine(
            $"    dataSav={audit.Assets.DataSavCount} data2Sav={audit.Assets.Data2SavCount} scripts={audit.Assets.ScriptCount} dialogs={audit.Assets.DialogCount}"
        );
        Console.WriteLine();
    }

    private static void WriteValidationSummary(SaveFileAuditSnapshot audit)
    {
        Console.WriteLine("  VALIDATION");
        Console.WriteLine(
            $"    issues={audit.Validation.IssueCount} errors={audit.Validation.ErrorCount} warnings={audit.Validation.WarningCount} info={audit.Validation.InfoCount} filesWithIssues={audit.Validation.FileCountWithIssues}"
        );

        if (audit.ParseErrors.Count == 0)
        {
            Console.WriteLine("    parseErrors=(none)");
        }
        else
        {
            foreach (var parseError in audit.ParseErrors.Take(8))
                Console.WriteLine($"    parse-error {parseError.FilePath}: {Truncate(parseError.Message, 120)}");
        }

        if (audit.ValidationIssues.Count == 0)
        {
            Console.WriteLine("    findings=(none)");
        }
        else
        {
            foreach (var issue in audit.ValidationIssues.Take(12))
            {
                Console.WriteLine(
                    $"    [{issue.Severity}] {issue.FilePath ?? "(save)"}: {Truncate(issue.Message, 120)}"
                );
            }
        }

        Console.WriteLine();
    }

    private static void WriteMobileMdySummary(SaveFileAuditSnapshot audit)
    {
        Console.WriteLine("  MOBILE.MDY");
        if (audit.MobileMdys.Count == 0)
        {
            Console.WriteLine("    (none)");
            Console.WriteLine();
            return;
        }

        foreach (var entry in audit.MobileMdys.Take(10))
        {
            Console.WriteLine(
                $"    {entry.Path}: records={entry.RecordCount} mobs={entry.MobRecordCount} chars={entry.CharacterRecordCount} dupObjIds={entry.DuplicateObjectIdCount} props={entry.PropertyCount} parseNotes={entry.PropertyParseNoteCount}"
            );
            if (entry.ObjectTypes.Count > 0)
                Console.WriteLine(
                    $"      types: {string.Join(", ", entry.ObjectTypes.Take(8).Select(type => $"{type.ObjectType}={type.Count}"))}"
                );
        }

        Console.WriteLine();
    }

    private static void WriteObjectFieldSummary(SaveFileAuditSnapshot audit)
    {
        Console.WriteLine("  OBJECT FIELDS");
        Console.WriteLine(
            $"    objects={audit.Objects.ObjectCount} mobFiles={audit.Objects.MobileFileCount} mobileMdyMobs={audit.Objects.MobileMdyMobCount}"
        );
        Console.WriteLine(
            $"    distinctFields={audit.Objects.DistinctFieldCount} props={audit.Objects.TotalPropertyCount} parseNotes={audit.Objects.ParseNoteCount}"
        );

        if (audit.Objects.TopFields.Count == 0)
        {
            Console.WriteLine("    topFields=(none)");
        }
        else
        {
            foreach (var field in audit.Objects.TopFields)
            {
                Console.WriteLine(
                    $"    field {field.Field}: count={field.Count} parseNotes={field.ParseNoteCount} raw={field.TotalRawBytes}B"
                );
            }
        }

        if (audit.Objects.LinkFields.Count == 0)
        {
            Console.WriteLine("    linkFields=(none)");
        }
        else
        {
            Console.WriteLine("    likely link fields");
            foreach (var field in audit.Objects.LinkFields)
            {
                Console.WriteLine(
                    $"      {field.Field}: count={field.Count} parseNotes={field.ParseNoteCount} raw={field.TotalRawBytes}B"
                );
            }
        }

        Console.WriteLine();
    }

    private static void WritePlayerCharacterSummary(PlayerCharacterAuditSnapshot? player)
    {
        Console.WriteLine("  PLAYER CHARACTER");
        if (player is null)
        {
            Console.WriteLine("    no v2 player record found");
            Console.WriteLine();
            return;
        }

        Console.WriteLine($"    source={player.SourcePath}");
        Console.WriteLine(
            $"    name=\"{player.Name ?? "(none)"}\" lv={player.Level} gold={player.Gold} arrows={player.Arrows} bullets={player.Bullets} powerCells={player.PowerCells} kills={player.TotalKills}"
        );
        Console.WriteLine(
            $"    quests={player.QuestCount} rumors={player.RumorsCount} rep={player.ReputationCount} bless={player.BlessingCount} curse={player.CurseCount} schematics={player.SchematicsCount} effects={player.EffectsCount}"
        );
        Console.WriteLine(
            $"    hpDmg={player.HpDamage} fatDmg={player.FatigueDamage} complete={player.HasCompleteData} raw={player.RecordSize}B"
        );
        if (player.PositionLocation is not null)
        {
            Console.WriteLine(
                $"    position aid={player.PositionAid ?? 0} loc={player.PositionLocation.Value} offX={player.PositionOffsetX ?? 0}"
            );
        }

        if (player.Sars.Count == 0)
        {
            Console.WriteLine("    sars=(none)");
            Console.WriteLine();
            return;
        }

        Console.WriteLine("    sars");
        foreach (var sar in player.Sars)
        {
            Console.WriteLine(
                $"      [{sar.Fingerprint}] {sar.Annotation} off=0x{sar.Offset:X} bytes={sar.TotalBytes} eSize={sar.ElementSize} eCnt={sar.ElementCount} bsCnt={sar.BitsetWordCount} bitsetId=0x{sar.BitsetId:X}"
            );
            if (sar.SampleValues.Count > 0)
                Console.WriteLine($"        samples={FormatList(sar.SampleValues, 8)}");
            if (sar.BitSlotCount > 0)
                Console.WriteLine($"        bitSlots={FormatList(sar.BitSlots, 12)} total={sar.BitSlotCount}");
        }

        Console.WriteLine();
    }

    private static string FormatList(IReadOnlyList<int> values, int maxShow)
    {
        if (values.Count == 0)
            return "[]";

        var shown = string.Join(',', values.Take(maxShow));
        return values.Count > maxShow ? $"[{shown},...]" : $"[{shown}]";
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
