using System.Buffers.Binary;
using ArcNET.Formats;
using ArcNET.GameData.SaveGames;
using ArcNET.GameObjects;

namespace ArcNET.Diagnostics;

public static class SaveFileAuditService
{
    public static SaveFileAuditSnapshot Create(string gsiPath, string tfaiPath, string tfafPath) =>
        Create(new SaveFileAuditRequest(SaveSlotLoadService.LoadFiles(gsiPath, tfaiPath, tfafPath)));

    public static SaveFileAuditSnapshot Create(SaveFileAuditRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Save);
        Validate(request);

        var validationIssues = SaveGameValidator.Validate(request.Save);
        SaveParseErrorSnapshot[] parseErrors =
        [
            .. request
                .Save.ParseErrors.OrderBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .Select(static entry => new SaveParseErrorSnapshot(entry.Key, entry.Value)),
        ];
        SaveValidationIssueSnapshot[] validationSnapshots =
        [
            .. validationIssues
                .Take(request.ValidationIssueLimit)
                .Select(issue => new SaveValidationIssueSnapshot(
                    MapSeverity(issue.Severity),
                    issue.FilePath,
                    issue.Message
                )),
        ];
        MobileMdyAuditSnapshot[] mobileMdyAudits =
        [
            .. request
                .Save.MobileMdys.OrderBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .Take(request.MobileMdyLimit)
                .Select(static entry => CreateMobileMdyAudit(entry.Key, entry.Value)),
        ];

        return new SaveFileAuditSnapshot(
            DateTimeOffset.UtcNow,
            request.Save.Info.LeaderName,
            request.Save.Info.LeaderLevel,
            request.Save.Info.MapId,
            new SaveTypedAssetSummarySnapshot(
                request.Save.Files.Count,
                request.Save.RawFiles.Count,
                request.Save.ParseErrors.Count,
                request.Save.Mobiles.Count,
                request.Save.MobileMds.Count,
                request.Save.MobileMdys.Count,
                request.Save.Sectors.Count,
                request.Save.JumpFiles.Count,
                request.Save.MapPropertiesList.Count,
                request.Save.Messages.Count,
                request.Save.TownMapFogs.Count,
                request.Save.DataSavFiles.Count,
                request.Save.Data2SavFiles.Count,
                request.Save.Scripts.Count,
                request.Save.Dialogs.Count
            ),
            new SaveValidationSummarySnapshot(
                validationIssues.Count,
                validationIssues.Count(static issue => issue.Severity == SaveValidationSeverity.Error),
                validationIssues.Count(static issue => issue.Severity == SaveValidationSeverity.Warning),
                validationIssues.Count(static issue => issue.Severity == SaveValidationSeverity.Info),
                validationIssues
                    .Where(static issue => issue.FilePath is not null)
                    .Select(static issue => issue.FilePath!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count()
            ),
            CreateObjectFieldAudit(request.Save, request.FieldLimit),
            CreatePlayerCharacterAudit(request.Save, request.CharacterSarLimit),
            parseErrors,
            validationSnapshots,
            mobileMdyAudits,
            request.ValidationIssueLimit,
            request.MobileMdyLimit
        );
    }

    private static void Validate(SaveFileAuditRequest request)
    {
        if (request.ValidationIssueLimit <= 0)
            throw new InvalidOperationException("ValidationIssueLimit must be greater than zero.");

        if (request.MobileMdyLimit <= 0)
            throw new InvalidOperationException("MobileMdyLimit must be greater than zero.");

        if (request.FieldLimit <= 0)
            throw new InvalidOperationException("FieldLimit must be greater than zero.");

        if (request.CharacterSarLimit <= 0)
            throw new InvalidOperationException("CharacterSarLimit must be greater than zero.");
    }

    private static SaveObjectFieldAuditSnapshot CreateObjectFieldAudit(LoadedSave save, int fieldLimit)
    {
        Dictionary<ObjectField, FieldCounter> counters = [];
        var objectCount = 0;
        var mobileMdyMobCount = 0;

        foreach (var mob in save.Mobiles.Values)
        {
            objectCount++;
            AddMobProperties(mob, counters);
        }

        foreach (var mdy in save.MobileMdys.Values)
        {
            foreach (var record in mdy.Records)
            {
                if (!record.IsMob)
                    continue;

                objectCount++;
                mobileMdyMobCount++;
                AddMobProperties(record.Mob!, counters);
            }
        }

        var totalPropertyCount = counters.Values.Sum(static counter => counter.Count);
        var parseNoteCount = counters.Values.Sum(static counter => counter.ParseNoteCount);

        return new SaveObjectFieldAuditSnapshot(
            objectCount,
            save.Mobiles.Count,
            mobileMdyMobCount,
            counters.Count,
            totalPropertyCount,
            parseNoteCount,
            CreateFieldSnapshots(counters, fieldLimit, static field => true),
            CreateFieldSnapshots(counters, fieldLimit, IsLikelyLinkField)
        );
    }

    private static void AddMobProperties(MobData mob, Dictionary<ObjectField, FieldCounter> counters)
    {
        foreach (var property in mob.Properties)
        {
            if (!counters.TryGetValue(property.Field, out var counter))
            {
                counter = new FieldCounter();
                counters[property.Field] = counter;
            }

            counter.Count++;
            if (property.ParseNote is not null)
                counter.ParseNoteCount++;

            counter.TotalRawBytes += property.RawBytes.Length;
        }
    }

    private static ObjectFieldUsageSnapshot[] CreateFieldSnapshots(
        Dictionary<ObjectField, FieldCounter> counters,
        int fieldLimit,
        Func<ObjectField, bool> predicate
    ) =>
        [
            .. counters
                .Where(static entry => entry.Value.Count > 0)
                .Where(entry => predicate(entry.Key))
                .OrderByDescending(static entry => entry.Value.Count)
                .ThenByDescending(static entry => entry.Value.ParseNoteCount)
                .ThenBy(static entry => entry.Key.ToString(), StringComparer.OrdinalIgnoreCase)
                .Take(fieldLimit)
                .Select(static entry => new ObjectFieldUsageSnapshot(
                    entry.Key.ToString(),
                    entry.Value.Count,
                    entry.Value.ParseNoteCount,
                    entry.Value.TotalRawBytes
                )),
        ];

    private static bool IsLikelyLinkField(ObjectField field)
    {
        var name = field.ToString();
        return name.EndsWith("Idx", StringComparison.Ordinal)
            || name.Contains("Parent", StringComparison.Ordinal)
            || name.Contains("Leader", StringComparison.Ordinal)
            || name.Contains("Follower", StringComparison.Ordinal)
            || name.Contains("NotifyNpc", StringComparison.Ordinal)
            || name.Contains("WhosInMe", StringComparison.Ordinal)
            || name.Contains("WhoHitMeLast", StringComparison.Ordinal)
            || name.Contains("FleeingFrom", StringComparison.Ordinal)
            || name.Contains("InventorySource", StringComparison.Ordinal)
            || name.Contains("InventoryListIdx", StringComparison.Ordinal)
            || name.Contains("SubstituteInventory", StringComparison.Ordinal)
            || name.Contains("CombatFocus", StringComparison.Ordinal)
            || name.Contains("Waypoints", StringComparison.Ordinal)
            || name.Contains("ReactionPc", StringComparison.Ordinal)
            || name.Contains("ReactionLevel", StringComparison.Ordinal)
            || name.Contains("ReactionTime", StringComparison.Ordinal)
            || name.Contains("UseAidFragment", StringComparison.Ordinal)
            || name
                is nameof(ObjectField.Aid)
                    or nameof(ObjectField.CurrentAid)
                    or nameof(ObjectField.DestroyedAid)
                    or nameof(ObjectField.LightAid)
                    or nameof(ObjectField.OverlayLightAid)
                    or nameof(ObjectField.Dispatcher)
                    or nameof(ObjectField.NpcAiData)
                    or nameof(ObjectField.NpcOrigin)
                    or nameof(ObjectField.NpcGeneratorData);
    }

    private static MobileMdyAuditSnapshot CreateMobileMdyAudit(string path, MobileMdyFile file)
    {
        var mobRecordCount = 0;
        var characterRecordCount = 0;
        var duplicateObjectIdCount = 0;
        var propertyCount = 0;
        var propertyParseNoteCount = 0;
        HashSet<string> objectIds = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> objectTypeCounts = new(StringComparer.OrdinalIgnoreCase);

        foreach (var record in file.Records)
        {
            if (record.IsCharacter)
            {
                characterRecordCount++;
                continue;
            }

            var mob = record.Mob!;
            mobRecordCount++;
            propertyCount += mob.Properties.Count;
            propertyParseNoteCount += mob.Properties.Count(static property => property.ParseNote is not null);

            var objectId = mob.Header.ObjectId.ToString();
            if (!objectIds.Add(objectId))
                duplicateObjectIdCount++;

            var objectType = mob.Header.GameObjectType.ToString();
            objectTypeCounts[objectType] = objectTypeCounts.TryGetValue(objectType, out var count) ? count + 1 : 1;
        }

        return new MobileMdyAuditSnapshot(
            path,
            file.Records.Count,
            mobRecordCount,
            characterRecordCount,
            duplicateObjectIdCount,
            propertyCount,
            propertyParseNoteCount,
            [
                .. objectTypeCounts
                    .OrderBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(static entry => new MobileMdyObjectTypeSnapshot(entry.Key, entry.Value)),
            ]
        );
    }

    private static PlayerCharacterAuditSnapshot? CreatePlayerCharacterAudit(LoadedSave save, int characterSarLimit)
    {
        var candidate = SavePlayerCharacterResolver.Resolve(save);
        if (candidate is null)
            return null;

        var positionAi = candidate.Record.PositionAiRaw;
        return new PlayerCharacterAuditSnapshot(
            candidate.Path,
            candidate.Record.RawBytes.Length,
            candidate.Record.HasCompleteData,
            candidate.Record.Name,
            candidate.Record.Stats.Length > 17 ? candidate.Record.Stats[17] : 0,
            candidate.Record.Gold,
            candidate.Record.Arrows,
            candidate.Record.Bullets,
            candidate.Record.PowerCells,
            candidate.Record.TotalKills,
            candidate.Record.QuestCount,
            candidate.Record.RumorsCount,
            candidate.Record.BlessingProtoElementCount,
            candidate.Record.CurseProtoElementCount,
            candidate.Record.SchematicsElementCount,
            candidate.Record.ReputationRaw?.Length ?? 0,
            candidate.Record.Effects?.Length ?? 0,
            positionAi is { Length: >= 1 } ? positionAi[0] : null,
            positionAi is { Length: >= 2 } ? positionAi[1] : null,
            positionAi is { Length: >= 3 } ? positionAi[2] : null,
            candidate.Record.HpDamage,
            candidate.Record.FatigueDamage,
            [.. CharacterSarDiagnostics.CreateAuditSnapshots(candidate.Record.RawBytes, characterSarLimit)]
        );
    }

    private static DiagnosticIssueSeverity MapSeverity(SaveValidationSeverity severity) =>
        severity switch
        {
            SaveValidationSeverity.Error => DiagnosticIssueSeverity.Error,
            SaveValidationSeverity.Warning => DiagnosticIssueSeverity.Warning,
            _ => DiagnosticIssueSeverity.Info,
        };

    private sealed class FieldCounter
    {
        public int Count { get; set; }

        public int ParseNoteCount { get; set; }

        public long TotalRawBytes { get; set; }
    }
}
