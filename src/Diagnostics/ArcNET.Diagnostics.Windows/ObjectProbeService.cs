using System.Globalization;
using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

public sealed class ObjectProbeService(IObjectProbeBackend backend)
{
    [SupportedOSPlatform("windows")]
    public static ObjectProbeService Default { get; } = new(new ObjectProbeBackend());

    public ObjectProbeSnapshot Inspect(ObjectProbeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var capabilities = request.Session.Capabilities.Capabilities;
        if (!capabilities.HasFlag(DiagnosticsCapability.ReadStructuredState))
        {
            return new ObjectProbeSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: false,
                "Object probe unavailable",
                "This session does not currently expose structured-state capability, so live object header inspection stays disabled.",
                request.SourceLabel,
                [],
                []
            );
        }

        var playerResolution = UsesPlayerToken(request.HandleTexts)
            ? backend.LocatePlayers(request.Session.ProcessId)
            : (LivePlayerLocatorResult?)null;
        var handles = ParseHandles(request.HandleTexts, request.MaxObjects, playerResolution);
        if (handles.Count == 0)
        {
            return new ObjectProbeSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                playerResolution is null ? "No object handle selected" : "No live player resolved",
                playerResolution?.Summary
                    ?? "Enter one handle, use the player token, or reuse a live timeline candidate to inspect runtime object-pool state.",
                request.SourceLabel,
                [],
                []
            );
        }

        var includeExtendedDetails =
            capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions)
            && request.Session.RuntimeProfile.SupportsCatalogRvas;
        var inspections = backend.InspectHandles(
            request.Session.ProcessId,
            request.Session.RuntimeProfile,
            includeExtendedDetails,
            handles
        );
        var objects = inspections.Select(CreateObjectSnapshot).ToArray();
        var decodedHeaders = inspections.Count(static inspection => inspection.Identity.HasHeader);
        var expandedDetailCards = inspections.Count(static inspection => inspection.HasDetails);

        return new ObjectProbeSnapshot(
            DateTimeOffset.UtcNow,
            IsAvailable: true,
            $"Inspected {handles.Count} live handle candidate(s)",
            CreateSummary(request.SourceLabel, decodedHeaders, expandedDetailCards, playerResolution),
            request.SourceLabel,
            [.. handles.Select(RuntimeSemanticCatalog.FormatHandle)],
            objects
        );
    }

    private static string CreateSummary(
        string sourceLabel,
        int decodedHeaders,
        int expandedDetailCards,
        LivePlayerLocatorResult? playerResolution
    )
    {
        var probeSummary =
            expandedDetailCards > 0
                ? $"{decodedHeaders} object header(s) decoded and {expandedDetailCards} runtime detail card(s) expanded from {sourceLabel}."
                : $"{decodedHeaders} object header(s) decoded from {sourceLabel}.";

        return string.Join(
            " ",
            new[] { probeSummary, playerResolution?.Summary }.Where(static text => !string.IsNullOrWhiteSpace(text))
        );
    }

    private static IReadOnlyList<ulong> ParseHandles(
        IReadOnlyList<string> handleTexts,
        int maxObjects,
        LivePlayerLocatorResult? playerResolution
    )
    {
        ArgumentNullException.ThrowIfNull(handleTexts);

        HashSet<ulong> handles = [];
        if (playerResolution is { } resolution)
        {
            if (resolution.AutoResolvedHandle.HasValue)
            {
                handles.Add(resolution.AutoResolvedHandle.Value);
            }
            else
            {
                foreach (var candidate in resolution.LivePlayerCandidates)
                {
                    if (handles.Count >= maxObjects)
                        break;

                    handles.Add(candidate.Handle);
                }
            }
        }

        foreach (var handleText in handleTexts)
        {
            if (handles.Count >= maxObjects)
                break;

            if (TryParseHandle(handleText, out var handle))
                handles.Add(handle);
        }

        return [.. handles];
    }

    private static bool UsesPlayerToken(IReadOnlyList<string> handleTexts) =>
        handleTexts.Any(static handleText => IsPlayerToken(handleText));

    private static bool TryParseHandle(string? handleText, out ulong handle)
    {
        if (string.IsNullOrWhiteSpace(handleText))
        {
            handle = 0;
            return false;
        }

        var trimmed = handleText.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return ulong.TryParse(trimmed[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out handle);

        return ulong.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out handle);
    }

    private static bool IsPlayerToken(string? handleText)
    {
        if (string.IsNullOrWhiteSpace(handleText))
            return false;

        return handleText.Trim() switch
        {
            var token when token.Equals("player", StringComparison.OrdinalIgnoreCase) => true,
            var token when token.Equals("pc", StringComparison.OrdinalIgnoreCase) => true,
            var token when token.Equals("auto", StringComparison.OrdinalIgnoreCase) => true,
            var token when token.Equals("self", StringComparison.OrdinalIgnoreCase) => true,
            var token when token.Equals("current", StringComparison.OrdinalIgnoreCase) => true,
            _ => false,
        };
    }

    private static ObjectProbeObjectSnapshot CreateObjectSnapshot(LiveObjectInspection inspection)
    {
        var identity = inspection.Identity;
        var header = identity.Header;
        var objectTypeText = header?.ObjectTypeName ?? "Unknown runtime object";
        var objectIdText = header?.ObjectId.Label ?? "No decoded object id";
        var prototypeText = header?.PrototypeId.Label ?? "No decoded prototype id";
        var prototypeHandleText = header?.PrototypeHandle ?? "No decoded prototype handle";
        var addressText = identity.ObjectAddress ?? identity.EntryAddress ?? "Object-pool address unavailable";
        var statusText = header is not null
            ? $"{identity.ResolutionSource} · pool index {identity.PoolIndex?.ToString(CultureInfo.InvariantCulture) ?? "?"}"
            : $"{identity.ResolutionSource} · No decoded header";
        IReadOnlyList<ObjectProbeDetailSnapshot> details = [.. inspection.Details.Select(CreateDetailSnapshot)];
        var sections = CreateSections(inspection.Details);

        return new ObjectProbeObjectSnapshot(
            identity.HandleHex,
            identity.ResolutionSource,
            objectTypeText,
            objectIdText,
            prototypeText,
            prototypeHandleText,
            addressText,
            statusText,
            sections,
            details
        );
    }

    private static ObjectProbeDetailSnapshot CreateDetailSnapshot(LiveObjectDetail detail) =>
        new(detail.Label, detail.Value, detail.ResolutionSource);

    private static IReadOnlyList<ObjectProbeSectionSnapshot> CreateSections(IReadOnlyList<LiveObjectDetail> details)
    {
        if (details.Count == 0)
            return [];

        List<LiveObjectDetail> remaining = [.. details];
        List<ObjectProbeSectionSnapshot> sections = [];

        AddSection(sections, remaining, "vitals", "Vitals", static detail => s_vitalKeys.Contains(detail.Key));
        AddSection(
            sections,
            remaining,
            "primary_stats",
            "Primary Stats",
            static detail => IsStatInRange(detail.Key, 0, 8)
        );
        AddSection(
            sections,
            remaining,
            "derived_stats",
            "Derived Stats",
            static detail => IsStatInRange(detail.Key, 8, 9)
        );
        AddSection(
            sections,
            remaining,
            "progression",
            "Progression",
            static detail => IsStatInRange(detail.Key, 17, 7)
        );
        AddSection(
            sections,
            remaining,
            "resistances",
            "Resistances",
            static detail => detail.Key.StartsWith("resistance_", StringComparison.OrdinalIgnoreCase)
        );
        AddSection(
            sections,
            remaining,
            "basic_skills",
            "Basic Skills",
            static detail => detail.Key.StartsWith("basic_skill_", StringComparison.OrdinalIgnoreCase)
        );
        AddSection(
            sections,
            remaining,
            "tech_skills",
            "Tech Skills",
            static detail => detail.Key.StartsWith("tech_skill_", StringComparison.OrdinalIgnoreCase)
        );
        AddSection(
            sections,
            remaining,
            "magick_tech",
            "Magick && Tech",
            static detail =>
                detail.Key.StartsWith("spell_college_", StringComparison.OrdinalIgnoreCase)
                || detail.Key.Equals("spell_mastery", StringComparison.OrdinalIgnoreCase)
                || detail.Key.StartsWith("tech_discipline_", StringComparison.OrdinalIgnoreCase)
        );
        AddSection(sections, remaining, "resources", "Resources", static detail => s_resourceKeys.Contains(detail.Key));
        AddSection(
            sections,
            remaining,
            "npc_state",
            "NPC State",
            static detail => detail.Key.StartsWith("obj_f_npc_", StringComparison.OrdinalIgnoreCase)
        );
        AddSection(
            sections,
            remaining,
            "container_state",
            "Container State",
            static detail => detail.Key.StartsWith("obj_f_container_", StringComparison.OrdinalIgnoreCase)
        );
        AddSection(
            sections,
            remaining,
            "portal_state",
            "Portal State",
            static detail => detail.Key.StartsWith("obj_f_portal_", StringComparison.OrdinalIgnoreCase)
        );
        AddSection(
            sections,
            remaining,
            "item_state",
            "Item State",
            static detail =>
                detail.Key.StartsWith("obj_f_item_", StringComparison.OrdinalIgnoreCase)
                || detail.Key.StartsWith("obj_f_weapon_", StringComparison.OrdinalIgnoreCase)
                || detail.Key.StartsWith("obj_f_ammo_", StringComparison.OrdinalIgnoreCase)
                || detail.Key.StartsWith("obj_f_armor_", StringComparison.OrdinalIgnoreCase)
                || detail.Key.StartsWith("obj_f_gold_", StringComparison.OrdinalIgnoreCase)
                || detail.Key.StartsWith("obj_f_key_", StringComparison.OrdinalIgnoreCase)
                || detail.Key.StartsWith("obj_f_written_", StringComparison.OrdinalIgnoreCase)
                || detail.Key.StartsWith("obj_f_generic_", StringComparison.OrdinalIgnoreCase)
        );

        if (remaining.Count > 0)
            sections.Add(CreateSectionSnapshot("additional_details", "Additional Details", remaining));

        return sections;
    }

    private static void AddSection(
        List<ObjectProbeSectionSnapshot> sections,
        List<LiveObjectDetail> remaining,
        string key,
        string title,
        Func<LiveObjectDetail, bool> predicate
    )
    {
        List<LiveObjectDetail> matches = [];
        for (var index = remaining.Count - 1; index >= 0; index--)
        {
            var detail = remaining[index];
            if (!predicate(detail))
                continue;

            matches.Insert(0, detail);
            remaining.RemoveAt(index);
        }

        if (matches.Count == 0)
            return;

        sections.Add(CreateSectionSnapshot(key, title, matches));
    }

    private static ObjectProbeSectionSnapshot CreateSectionSnapshot(
        string key,
        string defaultTitle,
        IReadOnlyList<LiveObjectDetail> details
    ) => new(key, defaultTitle, CreateSectionSourceText(details), [.. details.Select(CreateDetailSnapshot)]);

    private static string CreateSectionSourceText(IReadOnlyList<LiveObjectDetail> details)
    {
        var sourceLabels = details
            .Select(static detail => DescribeResolutionSource(detail.ResolutionSource))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return sourceLabels.Length switch
        {
            0 => "Source unavailable",
            1 => sourceLabels[0],
            2 => $"{sourceLabels[0]} + {sourceLabels[1]}",
            _ => $"{sourceLabels[0]} + {sourceLabels[1]} + {sourceLabels.Length - 2} more",
        };
    }

    private static string DescribeResolutionSource(string resolutionSource) =>
        resolutionSource switch
        {
            "character_base_aggregate" => "Experimental aggregate read",
            "stat_base_get" => "Getter-backed stat read",
            "object_get_resistance" => "Getter-backed resistance read",
            "obj_field_int32_get" => "Raw object fields",
            "obj_array_field_int32_get" => "Getter-backed array read",
            "Computed" => "Derived from raw fields",
            _ => resolutionSource,
        };

    private static bool IsStatInRange(string key, int start, int count) =>
        key.StartsWith("stat_", StringComparison.OrdinalIgnoreCase)
        && int.TryParse(key[5..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var statId)
        && statId >= start
        && statId < start + count;

    private static readonly HashSet<string> s_vitalKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "obj_f_current_aid",
        "obj_f_ac",
        "obj_f_hp_pts",
        "obj_f_hp_damage",
        "health_remaining",
        "obj_f_critter_fatigue_pts",
        "obj_f_critter_fatigue_damage",
        "fatigue_remaining",
    };

    private static readonly HashSet<string> s_resourceKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "obj_f_critter_gold",
        "obj_f_critter_arrows",
        "obj_f_critter_bullets",
        "obj_f_critter_power_cells",
        "obj_f_critter_fuel",
        "obj_f_critter_inventory_num",
        "obj_f_pc_bank_money",
        "obj_f_pc_party_id",
    };
}
