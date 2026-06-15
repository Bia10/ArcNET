using System.Globalization;
using ArcNET.Diagnostics.Contracts;
using ArcNET.GameObjects.Metadata;

namespace ArcNET.Diagnostics;

public sealed class SheetService(ISheetBackend backend)
{
    public SheetSnapshot Read(SheetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanReadSheets(request.Session))
        {
            return new SheetSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: false,
                "Sheet read unavailable",
                "Sheet diagnostics require a validated runtime profile with live function invocation support.",
                string.Empty,
                string.Empty,
                request.SheetLabel,
                SheetRoute.Stat,
                [],
                []
            );
        }

        try
        {
            var reference = SheetCatalog.ResolveReference(request.SheetLabel);
            var target = ResolveTarget(request.Session, request.HandleToken, "sheet target");
            var data = backend.ReadSheetData(request.Session.ProcessId, request.Session.RuntimeProfile, target.Handle);
            var values = CreateSheetValues(reference, data);
            return new SheetSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                "Sheet read completed",
                $"Read sheet value '{reference.DisplayName}' for {target.TargetText}.",
                target.HandleText,
                target.TargetText,
                reference.DisplayName,
                reference.Route,
                values,
                target.Notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return new SheetSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: false,
                "Invalid sheet request",
                ex.Message,
                string.Empty,
                string.Empty,
                request.SheetLabel,
                SheetRoute.Stat,
                [],
                []
            );
        }
    }

    public SheetScanSnapshot Scan(SheetScanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanReadSheets(request.Session))
            return CreateUnavailableScanSnapshot(
                "Sheet scan unavailable",
                "Sheet diagnostics require a validated runtime profile with live function invocation support."
            );

        try
        {
            var target = ResolveTarget(request.Session, request.HandleToken, "sheet-scan target");
            var data = backend.ReadSheetData(request.Session.ProcessId, request.Session.RuntimeProfile, target.Handle);
            return new SheetScanSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                "Sheet scan completed",
                $"Captured a live sheet snapshot for {target.TargetText}.",
                target.HandleText,
                target.TargetText,
                data,
                target.Notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableScanSnapshot("Invalid sheet-scan request", ex.Message);
        }
    }

    public SheetDiffSnapshot Diff(SheetDiffRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanReadSheets(request.Session))
        {
            return CreateUnavailableDiffSnapshot(
                request.DelayMilliseconds,
                "Sheet diff unavailable",
                "Sheet diagnostics require a validated runtime profile with live function invocation support."
            );
        }

        if (request.DelayMilliseconds < 0)
        {
            return CreateUnavailableDiffSnapshot(
                request.DelayMilliseconds,
                "Invalid sheet-diff request",
                "sheet-diff delay-ms must be zero or positive."
            );
        }

        try
        {
            var target = ResolveTarget(request.Session, request.HandleToken, "sheet-diff target");
            var before = backend.ReadSheetData(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle
            );
            if (request.DelayMilliseconds > 0)
                Thread.Sleep(request.DelayMilliseconds);

            var after = backend.ReadSheetData(request.Session.ProcessId, request.Session.RuntimeProfile, target.Handle);
            var changes = CreateSheetChanges(before, after);
            List<string> notes =
            [
                "This compares two live sheet snapshots separated by the requested delay. Use it around a manual gameplay action or a debugger mutation to see what changed.",
                .. target.Notes,
            ];

            return new SheetDiffSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                "Sheet diff completed",
                $"Compared two live sheet snapshots for {target.TargetText}.",
                target.HandleText,
                target.TargetText,
                request.DelayMilliseconds,
                Changed: changes.Count > 0,
                changes,
                before,
                after,
                notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableDiffSnapshot(request.DelayMilliseconds, "Invalid sheet-diff request", ex.Message);
        }
    }

    private static IReadOnlyList<ReadValueSnapshot> CreateSheetValues(
        SheetReference reference,
        SheetDataSnapshot data
    ) =>
        reference.Route switch
        {
            SheetRoute.Stat => CreateScalarValues(
                data.PrimaryStats.Concat(data.Progression),
                reference,
                "Stat Id",
                "Stat"
            ),
            SheetRoute.DerivedStat => CreateScalarValues(
                data.DerivedStats,
                reference,
                "Derived Stat Id",
                "Derived Stat"
            ),
            SheetRoute.Resistance => CreateScalarValues(data.Resistances, reference, "Resistance Id", "Resistance"),
            SheetRoute.SpellCollege => CreateScalarValues(data.SpellColleges, reference, "College Id", "College"),
            SheetRoute.SpellMastery =>
            [
                new("id", "Spell-Tech Slot", data.SpellMastery.Id.ToString(CultureInfo.InvariantCulture)),
                new("name", "Field", data.SpellMastery.Name),
                new("value", "Value", data.SpellMastery.Value.ToString(CultureInfo.InvariantCulture)),
                new(
                    "value_name",
                    "College",
                    data.SpellMastery.Value is >= 0 and < SpellTechCatalog.SpellCollegeCount
                        ? CharacterSheetMetadata.SpellCollegeName(data.SpellMastery.Value)
                        : "None"
                ),
            ],
            SheetRoute.TechDiscipline => CreateScalarValues(
                data.TechDisciplines,
                reference,
                "Discipline Id",
                "Discipline"
            ),
            SheetRoute.BasicSkill => CreateSkillValues(data.BasicSkills, reference.Id, "Basic Skill Id", "Basic Skill"),
            SheetRoute.TechSkill => CreateSkillValues(data.TechSkills, reference.Id, "Tech Skill Id", "Tech Skill"),
            _ => throw new InvalidOperationException($"Unsupported sheet route '{reference.Route}'."),
        };

    private static IReadOnlyList<ReadValueSnapshot> CreateScalarValues(
        IEnumerable<SheetScalarSnapshot> values,
        SheetReference reference,
        string idLabel,
        string nameLabel
    )
    {
        var value = values.First(entry => entry.Id == reference.Id);
        var rawValueText = value.Value.ToString(CultureInfo.InvariantCulture);
        var displayValueText = SheetValueCatalog.FormatDisplayValue(reference, value.Value);
        List<ReadValueSnapshot> result =
        [
            new("id", idLabel, value.Id.ToString(CultureInfo.InvariantCulture)),
            new("name", nameLabel, value.Name),
            new("value", "Value", rawValueText),
        ];
        if (!displayValueText.Equals(rawValueText, StringComparison.Ordinal))
            result.Add(new("value_name", "Meaning", displayValueText));

        return [.. result];
    }

    private static IReadOnlyList<ReadValueSnapshot> CreateSkillValues(
        IEnumerable<SheetSkillSnapshot> values,
        int id,
        string idLabel,
        string nameLabel
    )
    {
        var value = values.First(entry => entry.Id == id);
        return
        [
            new("id", idLabel, value.Id.ToString(CultureInfo.InvariantCulture)),
            new("name", nameLabel, value.Name),
            new("value", "Value", value.Value.ToString(CultureInfo.InvariantCulture)),
            new("training", "Training", value.Training.ToString(CultureInfo.InvariantCulture)),
            new("training_name", "Training Name", value.TrainingName),
            new("encoded", "Encoded", value.Encoded.ToString(CultureInfo.InvariantCulture)),
        ];
    }

    private static IReadOnlyList<SheetChangeSnapshot> CreateSheetChanges(
        SheetDataSnapshot before,
        SheetDataSnapshot after
    )
    {
        List<SheetChangeSnapshot> changes = [];
        AppendScalarChanges(changes, "PrimaryStat", before.PrimaryStats, after.PrimaryStats);
        AppendScalarChanges(changes, "Progression", before.Progression, after.Progression);
        AppendScalarChanges(changes, "DerivedStat", before.DerivedStats, after.DerivedStats);
        AppendScalarChanges(changes, "Resistance", before.Resistances, after.Resistances);
        AppendSkillChanges(changes, "BasicSkill", before.BasicSkills, after.BasicSkills);
        AppendSkillChanges(changes, "TechSkill", before.TechSkills, after.TechSkills);
        AppendScalarChanges(changes, "SpellCollege", before.SpellColleges, after.SpellColleges);
        if (before.SpellMastery.Value != after.SpellMastery.Value)
        {
            changes.Add(
                new SheetChangeSnapshot(
                    "SpellMastery",
                    before.SpellMastery.Name,
                    before.SpellMastery.Value,
                    after.SpellMastery.Value,
                    Detail: null
                )
            );
        }

        AppendScalarChanges(changes, "TechDiscipline", before.TechDisciplines, after.TechDisciplines);
        return changes;
    }

    private static void AppendScalarChanges(
        List<SheetChangeSnapshot> changes,
        string category,
        IReadOnlyList<SheetScalarSnapshot> before,
        IReadOnlyList<SheetScalarSnapshot> after
    )
    {
        for (var index = 0; index < Math.Min(before.Count, after.Count); index++)
        {
            if (before[index].Value == after[index].Value)
                continue;

            changes.Add(
                new SheetChangeSnapshot(
                    category,
                    before[index].Name,
                    before[index].Value,
                    after[index].Value,
                    Detail: null
                )
            );
        }
    }

    private static void AppendSkillChanges(
        List<SheetChangeSnapshot> changes,
        string category,
        IReadOnlyList<SheetSkillSnapshot> before,
        IReadOnlyList<SheetSkillSnapshot> after
    )
    {
        for (var index = 0; index < Math.Min(before.Count, after.Count); index++)
        {
            if (before[index].Encoded == after[index].Encoded)
                continue;

            var detail =
                before[index].Training != after[index].Training
                    ? $"{before[index].TrainingName}->{after[index].TrainingName}"
                    : null;
            changes.Add(
                new SheetChangeSnapshot(category, before[index].Name, before[index].Value, after[index].Value, detail)
            );
        }
    }

    private ResolvedTarget ResolveTarget(AttachedSessionSnapshot session, string token, string subject) =>
        TargetResolver.Resolve(backend, session, token, subject);

    private static bool CanReadSheets(AttachedSessionSnapshot session) =>
        !session.HasExited
        && session.RuntimeProfile.SupportsCatalogRvas
        && session.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions);

    private static SheetScanSnapshot CreateUnavailableScanSnapshot(string status, string summary) =>
        new(DateTimeOffset.UtcNow, IsAvailable: false, status, summary, string.Empty, string.Empty, EmptyData, []);

    private static SheetDiffSnapshot CreateUnavailableDiffSnapshot(
        int delayMilliseconds,
        string status,
        string summary
    ) =>
        new(
            DateTimeOffset.UtcNow,
            IsAvailable: false,
            status,
            summary,
            string.Empty,
            string.Empty,
            delayMilliseconds,
            Changed: false,
            [],
            EmptyData,
            EmptyData,
            []
        );

    private static readonly SheetDataSnapshot EmptyData = new([], [], [], [], [], [], [], default, []);
}
