using System.Globalization;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed class SpellTechEditorService(ISpellTechEditorBackend backend)
{
    public SpellTechMutationSnapshot AddSpell(SpellAddRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanMutate(request.Session))
            return CreateUnavailableSnapshot(
                "Spell/tech editor unavailable",
                CreateAvailabilitySummary(request.Session)
            );

        try
        {
            var timeout = ParseTimeout(request.TimeoutMillisecondsText);
            var spell = SpellTechCatalog.ParseSpell(request.SpellToken);
            var target = TargetResolver.Resolve(backend, request.Session, request.TargetHandleToken, "spell target");
            var execution = backend.AddSpell(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                spell.Id,
                timeout
            );
            return new SpellTechMutationSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                execution.NoMutation ? "Spell already known" : "Spell added",
                execution.NoMutation
                    ? $"{spell.Name} is already covered by the current {spell.CollegeName} rank on {target.TargetText}."
                    : $"Force-learned {spell.Name} on {target.TargetText}. Because Arcanum spellbooks are college-based, this guarantees {spell.CollegeName} rank {spell.Level.ToString(CultureInfo.InvariantCulture)} and every lower spell in that college.",
                "Add Spell",
                target.HandleText,
                target.TargetText,
                $"{spell.Name} ({spell.CollegeName} rank {spell.Level.ToString(CultureInfo.InvariantCulture)})",
                spell.Id.ToString(CultureInfo.InvariantCulture),
                $"{execution.DispatcherMode} · {execution.DispatcherSite}",
                execution.ExecutionDetailText,
                execution.ResultText,
                target.Notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableSnapshot("Invalid spell add request", ex.Message);
        }
    }

    public SpellTechMutationSnapshot GrantSchematic(SchematicGrantRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanMutate(request.Session))
            return CreateUnavailableSnapshot(
                "Spell/tech editor unavailable",
                CreateAvailabilitySummary(request.Session)
            );

        try
        {
            var timeout = ParseTimeout(request.TimeoutMillisecondsText);
            var schematicId = SpellTechCatalog.ParseSchematicId(request.SchematicIdText);
            var target = TargetResolver.Resolve(
                backend,
                request.Session,
                request.TargetHandleToken,
                "schematic target"
            );
            var execution = backend.GrantSchematic(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                schematicId,
                timeout
            );
            var summary = execution.NoMutation
                ? $"Schematic {schematicId.ToString(CultureInfo.InvariantCulture)} is already known by {target.TargetText}."
                : $"Granted schematic {schematicId.ToString(CultureInfo.InvariantCulture)} to {target.TargetText}.";
            if (execution.RelatedIndex >= 0)
            {
                summary +=
                    $" Slot {execution.RelatedIndex.ToString(CultureInfo.InvariantCulture)}"
                    + (execution.NoMutation ? " already held that value." : " now stores the schematic id.");
            }

            return new SpellTechMutationSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                execution.NoMutation ? "Schematic already known" : "Schematic granted",
                summary,
                "Grant Schematic",
                target.HandleText,
                target.TargetText,
                $"Schematic {schematicId.ToString(CultureInfo.InvariantCulture)}",
                schematicId.ToString(CultureInfo.InvariantCulture),
                $"{execution.DispatcherMode} · {execution.DispatcherSite}",
                execution.ExecutionDetailText,
                execution.ResultText,
                target.Notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableSnapshot("Invalid schematic grant request", ex.Message);
        }
    }

    public SpellTechMutationSnapshot RemoveSchematic(SchematicRemoveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanMutate(request.Session))
            return CreateUnavailableSnapshot(
                "Spell/tech editor unavailable",
                CreateAvailabilitySummary(request.Session)
            );

        try
        {
            var timeout = ParseTimeout(request.TimeoutMillisecondsText);
            var schematicId = SpellTechCatalog.ParseSchematicId(request.SchematicIdText);
            var target = TargetResolver.Resolve(
                backend,
                request.Session,
                request.TargetHandleToken,
                "schematic target"
            );
            var execution = backend.RemoveSchematic(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                schematicId,
                timeout
            );
            var summary = execution.NoMutation
                ? $"Schematic {schematicId.ToString(CultureInfo.InvariantCulture)} is not currently known by {target.TargetText}."
                : $"Removed schematic {schematicId.ToString(CultureInfo.InvariantCulture)} from {target.TargetText}.";
            if (execution.RelatedIndex >= 0)
            {
                summary += execution.NoMutation
                    ? string.Empty
                    : $" Slot {execution.RelatedIndex.ToString(CultureInfo.InvariantCulture)} was compacted out of the runtime schematic list.";
            }

            return new SpellTechMutationSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                execution.NoMutation ? "Schematic not known" : "Schematic removed",
                summary,
                "Remove Schematic",
                target.HandleText,
                target.TargetText,
                $"Schematic {schematicId.ToString(CultureInfo.InvariantCulture)}",
                schematicId.ToString(CultureInfo.InvariantCulture),
                $"{execution.DispatcherMode} · {execution.DispatcherSite}",
                execution.ExecutionDetailText,
                execution.ResultText,
                target.Notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableSnapshot("Invalid schematic removal request", ex.Message);
        }
    }

    public SpellTechMutationSnapshot SetSpellCollegeLevel(SpellCollegeWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanMutate(request.Session))
            return CreateUnavailableSnapshot(
                "Spell/tech editor unavailable",
                CreateAvailabilitySummary(request.Session)
            );

        try
        {
            var timeout = ParseTimeout(request.TimeoutMillisecondsText);
            var collegeId = SpellTechCatalog.ParseSpellCollegeId(request.CollegeToken);
            var level = SpellTechCatalog.ParseLevel(
                request.LevelText,
                "Spell college rank",
                minimum: 0,
                maximumInclusive: 5
            );
            var target = TargetResolver.Resolve(
                backend,
                request.Session,
                request.TargetHandleToken,
                "spell college target"
            );
            var execution = backend.SetSpellCollegeLevel(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                collegeId,
                level,
                timeout
            );
            var collegeName = SpellTechCatalog.SpellCollegeName(collegeId);
            return new SpellTechMutationSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                execution.NoMutation ? "Spell college unchanged" : "Spell college updated",
                execution.NoMutation
                    ? $"{collegeName} was already rank {level.ToString(CultureInfo.InvariantCulture)} on {target.TargetText}."
                    : $"Set {collegeName} to rank {level.ToString(CultureInfo.InvariantCulture)} on {target.TargetText}.",
                "Set Spell College",
                target.HandleText,
                target.TargetText,
                collegeName,
                level.ToString(CultureInfo.InvariantCulture),
                $"{execution.DispatcherMode} · {execution.DispatcherSite}",
                execution.ExecutionDetailText,
                execution.ResultText,
                target.Notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableSnapshot("Invalid spell college request", ex.Message);
        }
    }

    public SpellTechMutationSnapshot SetTechDisciplineLevel(TechDisciplineWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanMutate(request.Session))
            return CreateUnavailableSnapshot(
                "Spell/tech editor unavailable",
                CreateAvailabilitySummary(request.Session)
            );

        try
        {
            var timeout = ParseTimeout(request.TimeoutMillisecondsText);
            var disciplineId = SpellTechCatalog.ParseTechDisciplineId(request.DisciplineToken);
            var level = SpellTechCatalog.ParseLevel(
                request.DegreeText,
                "Tech discipline degree",
                minimum: 0,
                maximumInclusive: 7
            );
            var target = TargetResolver.Resolve(
                backend,
                request.Session,
                request.TargetHandleToken,
                "tech discipline target"
            );
            var execution = backend.SetTechDisciplineLevel(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                disciplineId,
                level,
                timeout
            );
            var disciplineName = SpellTechCatalog.TechDisciplineName(disciplineId);
            return new SpellTechMutationSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                execution.NoMutation ? "Tech discipline unchanged" : "Tech discipline updated",
                execution.NoMutation
                    ? $"{disciplineName} was already degree {level.ToString(CultureInfo.InvariantCulture)} on {target.TargetText}."
                    : $"Set {disciplineName} to degree {level.ToString(CultureInfo.InvariantCulture)} on {target.TargetText}.",
                "Set Tech Discipline",
                target.HandleText,
                target.TargetText,
                disciplineName,
                level.ToString(CultureInfo.InvariantCulture),
                $"{execution.DispatcherMode} · {execution.DispatcherSite}",
                execution.ExecutionDetailText,
                execution.ResultText,
                target.Notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableSnapshot("Invalid tech discipline request", ex.Message);
        }
    }

    public SpellTechMutationSnapshot SetTechSkillPoints(TechSkillWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanMutate(request.Session))
            return CreateUnavailableSnapshot(
                "Spell/tech editor unavailable",
                CreateAvailabilitySummary(request.Session)
            );

        try
        {
            var timeout = ParseTimeout(request.TimeoutMillisecondsText);
            var skillId = SpellTechCatalog.ParseTechSkillId(request.SkillToken);
            var points = SpellTechCatalog.ParseTechSkillPoints(request.PointsText);
            var target = TargetResolver.Resolve(
                backend,
                request.Session,
                request.TargetHandleToken,
                "tech skill target"
            );
            var execution = backend.SetTechSkillPoints(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                skillId,
                points,
                timeout
            );
            var skillName = SpellTechCatalog.TechSkillName(skillId);
            return new SpellTechMutationSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                execution.NoMutation ? "Tech skill unchanged" : "Tech skill updated",
                execution.NoMutation
                    ? $"{skillName} already had {points.ToString(CultureInfo.InvariantCulture)} point(s) on {target.TargetText}."
                    : $"Set {skillName} to {points.ToString(CultureInfo.InvariantCulture)} point(s) on {target.TargetText}.",
                "Set Tech Skill",
                target.HandleText,
                target.TargetText,
                skillName,
                points.ToString(CultureInfo.InvariantCulture),
                $"{execution.DispatcherMode} · {execution.DispatcherSite}",
                execution.ExecutionDetailText,
                execution.ResultText,
                target.Notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableSnapshot("Invalid tech skill request", ex.Message);
        }
    }

    private static bool CanMutate(AttachedSessionSnapshot session) =>
        !session.HasExited
        && session.RuntimeProfile.SupportsCatalogRvas
        && session.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions);

    private static string CreateAvailabilitySummary(AttachedSessionSnapshot session)
    {
        if (session.HasExited)
            return "The attached process has exited, so live spell and tech edits are unavailable until a new session is attached.";

        return "Live spell and tech edits require a validated runtime profile with live function invocation support.";
    }

    private static SpellTechMutationSnapshot CreateUnavailableSnapshot(string status, string summary) =>
        new(
            DateTimeOffset.UtcNow,
            IsAvailable: false,
            status,
            summary,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            "Dispatcher result unavailable.",
            "Target address and hook details will appear here after a live spell or tech mutation.",
            "Mutation result values will appear here after a live spell or tech mutation.",
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
}
