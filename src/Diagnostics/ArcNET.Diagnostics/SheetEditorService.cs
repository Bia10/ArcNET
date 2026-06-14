using System.Globalization;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed class SheetEditorService(ISheetEditorBackend backend)
{
    public SheetMutationSnapshot Write(SheetWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanMutate(request.Session))
            return CreateUnavailableSnapshot("Sheet editor unavailable", CreateAvailabilitySummary(request.Session));

        try
        {
            var timeout = ParseTimeout(request.TimeoutMillisecondsText);
            var reference = SheetCatalog.ResolveReference(request.FieldToken);
            var target = TargetResolver.Resolve(backend, request.Session, request.TargetHandleToken, "sheet target");
            var execution = reference.Route switch
            {
                SheetRoute.Stat or SheetRoute.DerivedStat => backend.SetStat(
                    request.Session.ProcessId,
                    request.Session.RuntimeProfile,
                    target.Handle,
                    reference.Id,
                    SheetValueCatalog.ParseValue(reference, request.ValueText),
                    timeout
                ),
                SheetRoute.Resistance => backend.SetResistance(
                    request.Session.ProcessId,
                    request.Session.RuntimeProfile,
                    target.Handle,
                    reference.Id,
                    ParseSignedInt32(request.ValueText, reference.DisplayName),
                    timeout
                ),
                SheetRoute.BasicSkill => backend.SetBasicSkill(
                    request.Session.ProcessId,
                    request.Session.RuntimeProfile,
                    target.Handle,
                    reference.Id,
                    ParseSkillPoints(request.ValueText, reference.DisplayName),
                    ParseTraining(request.TrainingText),
                    timeout
                ),
                SheetRoute.TechSkill => backend.SetTechSkill(
                    request.Session.ProcessId,
                    request.Session.RuntimeProfile,
                    target.Handle,
                    reference.Id,
                    ParseSkillPoints(request.ValueText, reference.DisplayName),
                    ParseTraining(request.TrainingText),
                    timeout
                ),
                SheetRoute.SpellCollege => backend.SetSpellCollegeLevel(
                    request.Session.ProcessId,
                    request.Session.RuntimeProfile,
                    target.Handle,
                    reference.Id,
                    SpellTechCatalog.ParseLevel(
                        request.ValueText,
                        $"{reference.DisplayName} rank",
                        minimum: 0,
                        maximumInclusive: SpellTechCatalog.SpellMaxLevel
                    ),
                    timeout
                ),
                SheetRoute.SpellMastery => backend.SetSpellMastery(
                    request.Session.ProcessId,
                    request.Session.RuntimeProfile,
                    target.Handle,
                    ParseSpellMastery(request.ValueText),
                    timeout
                ),
                SheetRoute.TechDiscipline => backend.SetTechDisciplineLevel(
                    request.Session.ProcessId,
                    request.Session.RuntimeProfile,
                    target.Handle,
                    reference.Id,
                    SpellTechCatalog.ParseLevel(
                        request.ValueText,
                        $"{reference.DisplayName} degree",
                        minimum: 0,
                        maximumInclusive: 7
                    ),
                    timeout
                ),
                _ => throw new InvalidOperationException($"Unsupported sheet route '{reference.Route}'."),
            };

            var summary = execution.NoMutation
                ? $"{reference.DisplayName} already matches the requested value on {target.TargetText}."
                : $"Updated {reference.DisplayName} on {target.TargetText}.";
            return new SheetMutationSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                execution.NoMutation ? "Sheet field unchanged" : "Sheet field updated",
                summary,
                target.HandleText,
                target.TargetText,
                request.FieldToken.Trim(),
                reference.DisplayName,
                reference.Route,
                request.ValueText.Trim(),
                string.IsNullOrWhiteSpace(request.TrainingText) ? string.Empty : request.TrainingText.Trim(),
                $"{execution.DispatcherMode} · {execution.DispatcherSite}",
                execution.ExecutionDetailText,
                execution.ResultText,
                target.Notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableSnapshot("Invalid sheet edit request", ex.Message);
        }
    }

    private static bool CanMutate(AttachedSessionSnapshot session) =>
        !session.HasExited
        && session.RuntimeProfile.SupportsCatalogRvas
        && session.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions);

    private static string CreateAvailabilitySummary(AttachedSessionSnapshot session)
    {
        if (session.HasExited)
            return "The attached process has exited, so live sheet edits are unavailable until a new session is attached.";

        return "Sheet edits require a validated runtime profile with live function invocation support.";
    }

    private static SheetMutationSnapshot CreateUnavailableSnapshot(string status, string summary) =>
        new(
            DateTimeOffset.UtcNow,
            IsAvailable: false,
            status,
            summary,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            SheetRoute.Stat,
            string.Empty,
            string.Empty,
            "Dispatcher result unavailable.",
            "Target address and hook details will appear here after a live sheet mutation.",
            "Mutation result values will appear here after a live sheet mutation.",
            []
        );

    private static TimeSpan ParseTimeout(string? timeoutText)
    {
        if (string.IsNullOrWhiteSpace(timeoutText))
            return TimeSpan.FromSeconds(1);

        if (
            int.TryParse(timeoutText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds)
            && milliseconds > 0
        )
        {
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        throw new InvalidOperationException($"Timeout '{timeoutText}' is not a valid positive millisecond value.");
    }

    private static int ParseSignedInt32(string? valueText, string label)
    {
        if (string.IsNullOrWhiteSpace(valueText))
            throw new InvalidOperationException($"{label} value is required.");

        return int.TryParse(valueText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new InvalidOperationException($"{label} must be a signed 32-bit integer value.");
    }

    private static int ParseSkillPoints(string? valueText, string label) =>
        SpellTechCatalog.ParseLevel(valueText, $"{label} points", minimum: 0, maximumInclusive: 63);

    private static int? ParseTraining(string? trainingText)
    {
        if (string.IsNullOrWhiteSpace(trainingText))
            return null;

        var normalized = SpellTechCatalog.Normalize(trainingText);
        return normalized switch
        {
            "0" or "untrained" or "none" => 0,
            "1" or "apprentice" => 1,
            "2" or "expert" => 2,
            "3" or "master" => 3,
            _ => throw new InvalidOperationException(
                $"Unknown training '{trainingText}'. Use untrained, apprentice, expert, master, or a numeric value between 0 and 3."
            ),
        };
    }

    private static int ParseSpellMastery(string? valueText)
    {
        if (string.IsNullOrWhiteSpace(valueText))
            throw new InvalidOperationException("Spell mastery value is required.");

        var normalized = SpellTechCatalog.Normalize(valueText);
        return normalized switch
        {
            "none" or "clear" or "unset" => -1,
            _ => SpellTechCatalog.ParseSpellCollegeId(valueText),
        };
    }
}
