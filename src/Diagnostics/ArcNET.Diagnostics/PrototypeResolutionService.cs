using System.Globalization;
using ArcNET.Diagnostics.Contracts;
using ArcNET.GameData.Workspace;

namespace ArcNET.Diagnostics;

public sealed class PrototypeResolutionService(IPrototypeResolutionBackend backend)
{
    public async Task<PrototypeResolutionSnapshot> ResolveAsync(PrototypeResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (TryParseHandleToken(request.PrototypeText, out var explicitHandle))
            return await CreateSuccessSnapshotAsync(request, null, explicitHandle, "ExplicitHandle")
                .ConfigureAwait(false);

        if (!request.Session.RuntimeProfile.SupportsCatalogRvas)
        {
            return CreateUnavailableSnapshot(
                request,
                "Prototype resolution unavailable",
                "This session does not expose catalog-backed runtime offsets, so proto-number and palette lookup cannot be translated into live prototype handles."
            );
        }

        var descriptorResolution = await ResolveDescriptorAsync(request).ConfigureAwait(false);
        if (!descriptorResolution.Success)
            return descriptorResolution.Snapshot!;

        var descriptor = descriptorResolution.Descriptor!.Value;
        var handleResolution = await Task.Run(() =>
                backend.ResolvePrototypeHandle(
                    request.Session.ProcessId,
                    request.Session.RuntimeProfile,
                    descriptor.ProtoNumber
                )
            )
            .ConfigureAwait(false);
        if (!handleResolution.Success)
        {
            return CreateUnavailableSnapshot(
                request,
                "Prototype handle unavailable",
                $"Unable to resolve proto {descriptor.ProtoNumber} into a live prototype handle. If ArcNET cannot match this runtime precisely, pass an explicit prototype handle instead.",
                descriptor.ProtoNumber,
                descriptor.DisplayName,
                descriptor.AssetPath,
                handleResolution.ResolutionSource,
                [handleResolution.ResolutionSource]
            );
        }

        var resolutionSource =
            descriptor.ResolutionSource == "ExplicitProtoNumber"
                ? handleResolution.ResolutionSource
                : $"{descriptor.ResolutionSource}->{handleResolution.ResolutionSource}";
        return await CreateSuccessSnapshotAsync(request, descriptor, handleResolution.Handle, resolutionSource)
            .ConfigureAwait(false);
    }

    private async Task<DescriptorResolution> ResolveDescriptorAsync(PrototypeResolutionRequest request)
    {
        if (TryParseProtoNumberToken(request.PrototypeText, out var protoNumber))
            return new(new ResolvedProtoDescriptor(protoNumber, null, null, "ExplicitProtoNumber"), null);

        if (string.IsNullOrWhiteSpace(request.WorkspacePath))
        {
            return new(
                null,
                CreateUnavailableSnapshot(
                    request,
                    "Prototype catalog unavailable",
                    "The request does not expose a usable local workspace path, so ArcNET cannot load the local prototype catalog."
                )
            );
        }

        IReadOnlyList<PrototypePaletteEntry> paletteEntries;
        try
        {
            paletteEntries = await backend.LoadPaletteAsync(request.WorkspacePath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new(
                null,
                CreateUnavailableSnapshot(
                    request,
                    "Prototype catalog unavailable",
                    $"Unable to load the ArcNET workspace for proto-name lookup ({ex.GetType().Name}: {ex.Message})."
                )
            );
        }

        var searchText = request.PrototypeText.Trim();
        var candidates = SearchPalette(paletteEntries, searchText);
        if (candidates.Count != 0)
        {
            var normalizedSearch = Normalize(searchText);
            var ranked = candidates
                .Select(entry => new RankedPaletteEntry(entry, ScoreEntry(entry, normalizedSearch)))
                .OrderBy(static candidate => candidate.Score)
                .ThenBy(static candidate => candidate.Entry.ProtoNumber)
                .ToArray();
            var best = ranked[0];
            if (ranked.Length > 1 && ranked[1].Score == best.Score)
            {
                var preview = string.Join(
                    ", ",
                    ranked
                        .Take(5)
                        .Select(static candidate => $"{DescribeEntry(candidate.Entry)} [{candidate.Entry.ProtoNumber}]")
                );
                return new(
                    null,
                    CreateUnavailableSnapshot(
                        request,
                        "Ambiguous prototype reference",
                        $"Proto-name lookup for '{searchText}' is ambiguous. Top matches: {preview}."
                    )
                );
            }

            return new(
                new ResolvedProtoDescriptor(
                    best.Entry.ProtoNumber,
                    best.Entry.DisplayName,
                    best.Entry.AssetPath,
                    best.Score == 0 ? "PaletteExactMatch" : "PaletteSearchMatch"
                ),
                null
            );
        }

        IReadOnlyList<StaticObjectCatalogEntry> staticObjectEntries;
        try
        {
            staticObjectEntries = await backend
                .LoadStaticObjectCatalogAsync(request.WorkspacePath)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new(
                null,
                CreateUnavailableSnapshot(
                    request,
                    "Prototype catalog unavailable",
                    $"Unable to load the ArcNET workspace for placed-object lookup ({ex.GetType().Name}: {ex.Message})."
                )
            );
        }

        var staticCandidates = SearchStaticObjects(staticObjectEntries, searchText);
        if (staticCandidates.Count != 0)
        {
            var normalizedSearch = Normalize(searchText);
            var ranked = staticCandidates
                .Select(entry => new RankedStaticObjectEntry(entry, ScoreEntry(entry, normalizedSearch)))
                .OrderBy(static candidate => candidate.Score)
                .ThenBy(static candidate => candidate.Entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Entry.SourceAssetPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Entry.ObjectIdText, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var best = ranked[0];
            if (ranked.Length > 1 && ranked[1].Score == best.Score)
            {
                var preview = string.Join(
                    ", ",
                    ranked.Take(5).Select(static candidate => DescribeEntry(candidate.Entry))
                );
                return new(
                    null,
                    CreateUnavailableSnapshot(
                        request,
                        "Ambiguous prototype reference",
                        $"Placed-object lookup for '{searchText}' is ambiguous. Top matches: {preview}."
                    )
                );
            }

            return new(
                new ResolvedProtoDescriptor(
                    best.Entry.ProtoNumber!.Value,
                    best.Entry.DisplayName,
                    best.Entry.SourceAssetPath,
                    best.Score == 0 ? "StaticObjectExactMatch" : "StaticObjectSearchMatch"
                ),
                null
            );
        }

        var paletteSuggestions = SuggestPaletteEntries(paletteEntries, searchText);
        var staticSuggestions = SuggestStaticObjects(staticObjectEntries, searchText);
        List<string> summaryParts = [$"No proto palette or placed object entries matched '{searchText}'."];
        if (paletteSuggestions.Count != 0)
        {
            summaryParts.Add(
                $"Nearby prototypes: {string.Join(", ", paletteSuggestions.Select(static entry => $"{DescribeEntry(entry)} [{entry.ProtoNumber}]"))}."
            );
        }

        if (staticSuggestions.Count != 0)
        {
            summaryParts.Add(
                $"Nearby placed objects: {string.Join(", ", staticSuggestions.Select(static entry => DescribeEntry(entry)))}."
            );
        }

        return new(
            null,
            CreateUnavailableSnapshot(request, "Unknown prototype reference", string.Join(" ", summaryParts))
        );
    }

    private Task<PrototypeResolutionSnapshot> CreateSuccessSnapshotAsync(
        PrototypeResolutionRequest request,
        ResolvedProtoDescriptor? descriptor,
        ulong handle,
        string resolutionSource
    ) => Task.Run(() => CreateSuccessSnapshot(request, descriptor, handle, resolutionSource));

    private PrototypeResolutionSnapshot CreateSuccessSnapshot(
        PrototypeResolutionRequest request,
        ResolvedProtoDescriptor? descriptor,
        ulong handle,
        string resolutionSource
    )
    {
        var identity = backend.InspectHandle(request.Session.ProcessId, handle);
        var handleText = RuntimeSemanticCatalog.FormatHandle(handle);
        List<string> notes = [];
        if (!identity.HasHeader)
        {
            notes.Add(
                $"Prototype inspection resolved through {identity.ResolutionSource} without a decoded object header."
            );
        }

        var summary = descriptor is { } resolvedDescriptor
            ? $"Resolved {DescribeResolvedPrototype(request.TokenOrFallback(), resolvedDescriptor)} to live handle {handleText} via {resolutionSource}."
            : $"Using explicit prototype handle {handleText}.";
        return new PrototypeResolutionSnapshot(
            DateTimeOffset.UtcNow,
            IsAvailable: true,
            "Prototype handle resolved",
            summary,
            request.TokenOrFallback(),
            descriptor?.ProtoNumber,
            descriptor?.DisplayName,
            descriptor?.AssetPath,
            handle,
            handleText,
            resolutionSource,
            CreateResolvedObjectSnapshot(handle, identity, "Prototype"),
            notes
        );
    }

    private static PrototypeResolutionSnapshot CreateUnavailableSnapshot(
        PrototypeResolutionRequest request,
        string status,
        string summary,
        int? protoNumber = null,
        string? displayName = null,
        string? assetPath = null,
        string resolutionSource = "",
        IReadOnlyList<string>? notes = null
    ) =>
        new(
            DateTimeOffset.UtcNow,
            IsAvailable: false,
            status,
            summary,
            request.TokenOrFallback(),
            protoNumber,
            displayName,
            assetPath,
            null,
            string.Empty,
            resolutionSource,
            null,
            notes ?? []
        );

    private static ResolvedObjectSnapshot CreateResolvedObjectSnapshot(
        ulong handle,
        LiveObjectIdentity identity,
        string fallbackName
    )
    {
        var handleText = RuntimeSemanticCatalog.FormatHandle(handle);
        if (handle == 0)
        {
            return new ResolvedObjectSnapshot(handleText, "null", null, null, null, "NullHandle", identity);
        }

        if (identity.Header is not { } header)
        {
            return new ResolvedObjectSnapshot(
                handleText,
                handleText,
                null,
                null,
                null,
                identity.ResolutionSource,
                identity
            );
        }

        var objectTypeName = string.IsNullOrWhiteSpace(header.ObjectTypeName) ? fallbackName : header.ObjectTypeName;
        var protoNumber = header.PrototypeId.ProtoNumber ?? header.ObjectId.ProtoNumber;
        var name = header.ObjectId.OidType switch
        {
            2 when !string.IsNullOrWhiteSpace(header.ObjectId.Label) => header.ObjectId.Label,
            _ when !string.IsNullOrWhiteSpace(header.PrototypeId.Label) => header.PrototypeId.Label,
            _ when !string.IsNullOrWhiteSpace(header.ObjectId.Label) => header.ObjectId.Label,
            _ => objectTypeName,
        };
        var displayValue = header.ObjectId.OidType switch
        {
            2 when !string.IsNullOrWhiteSpace(header.PrototypeId.Label) =>
                $"{objectTypeName} {header.ObjectId.Label} from {header.PrototypeId.Label} ({handleText})",
            _ => $"{objectTypeName} {name} ({handleText})",
        };
        return new ResolvedObjectSnapshot(
            handleText,
            displayValue,
            name,
            objectTypeName,
            protoNumber,
            $"Runtime{identity.ResolutionSource}",
            identity
        );
    }

    private static bool TryParseHandleToken(string token, out ulong handle)
    {
        var value = token.Trim();
        if (
            value.StartsWith("proto:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("prototype:", StringComparison.OrdinalIgnoreCase)
        )
        {
            handle = 0;
            return false;
        }

        return TryParseUInt64(value, out handle) && RuntimeSemanticCatalog.LooksLikeObjectHandle(handle);
    }

    private static bool TryParseProtoNumberToken(string token, out int protoNumber)
    {
        var value = token.Trim();
        if (value.StartsWith("proto:", StringComparison.OrdinalIgnoreCase))
            value = value["proto:".Length..];
        else if (value.StartsWith("prototype:", StringComparison.OrdinalIgnoreCase))
            value = value["prototype:".Length..];

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (uint.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue))
            {
                protoNumber = unchecked((int)hexValue);
                return true;
            }

            protoNumber = 0;
            return false;
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out protoNumber);
    }

    private static bool TryParseUInt64(string value, out ulong result)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return ulong.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);

        return ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    private static IReadOnlyList<PrototypePaletteEntry> SearchPalette(
        IReadOnlyList<PrototypePaletteEntry> entries,
        string searchText
    )
    {
        var normalizedSearch = Normalize(searchText);
        if (normalizedSearch.Length == 0)
            return [];

        var searchTokens = TokenizeSearch(searchText);

        return
        [
            .. entries
                .Where(entry => MatchesNormalized(entry, normalizedSearch, searchTokens))
                .OrderBy(entry => ScoreEntry(entry, normalizedSearch))
                .ThenBy(static entry => entry.ProtoNumber)
                .Take(64),
        ];
    }

    private static IReadOnlyList<PrototypePaletteEntry> SuggestPaletteEntries(
        IReadOnlyList<PrototypePaletteEntry> entries,
        string searchText
    )
    {
        var searchTokens = TokenizeSearch(searchText);
        if (searchTokens.Length == 0)
            return [];

        return
        [
            .. entries
                .Select(entry => new { Entry = entry, Score = ScoreSuggestion(entry, searchTokens) })
                .Where(static candidate => candidate.Score >= 0)
                .OrderByDescending(static candidate => candidate.Score)
                .ThenBy(static candidate => candidate.Entry.ProtoNumber)
                .Take(8)
                .Select(static candidate => candidate.Entry),
        ];
    }

    private static IReadOnlyList<StaticObjectCatalogEntry> SearchStaticObjects(
        IReadOnlyList<StaticObjectCatalogEntry> entries,
        string searchText
    )
    {
        var normalizedSearch = Normalize(searchText);
        if (normalizedSearch.Length == 0)
            return [];

        var searchTokens = TokenizeSearch(searchText);
        return
        [
            .. entries
                .Where(static entry => entry.HasPrototype)
                .Where(entry => MatchesNormalized(entry, normalizedSearch, searchTokens))
                .OrderBy(entry => ScoreEntry(entry, normalizedSearch))
                .ThenBy(static entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.SourceAssetPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.ObjectIdText, StringComparer.OrdinalIgnoreCase)
                .Take(64),
        ];
    }

    private static IReadOnlyList<StaticObjectCatalogEntry> SuggestStaticObjects(
        IReadOnlyList<StaticObjectCatalogEntry> entries,
        string searchText
    )
    {
        var searchTokens = TokenizeSearch(searchText);
        if (searchTokens.Length == 0)
            return [];

        return
        [
            .. entries
                .Where(static entry => entry.HasPrototype)
                .Select(entry => new { Entry = entry, Score = ScoreSuggestion(entry, searchTokens) })
                .Where(static candidate => candidate.Score >= 0)
                .OrderByDescending(static candidate => candidate.Score)
                .ThenBy(static candidate => candidate.Entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Entry.SourceAssetPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Entry.ObjectIdText, StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .Select(static candidate => candidate.Entry),
        ];
    }

    private static bool MatchesNormalized(
        PrototypePaletteEntry entry,
        string normalizedSearch,
        IReadOnlyList<string> searchTokens
    )
    {
        foreach (var candidateText in EnumerateSearchTexts(entry))
        {
            var normalizedCandidate = Normalize(candidateText);
            if (normalizedCandidate.Length == 0)
                continue;

            if (
                normalizedCandidate == normalizedSearch
                || normalizedCandidate.Contains(normalizedSearch, StringComparison.Ordinal)
            )
            {
                return true;
            }

            if (
                searchTokens.Count != 0
                && searchTokens.All(token => normalizedCandidate.Contains(token, StringComparison.Ordinal))
            )
            {
                return true;
            }
        }

        return false;
    }

    private static int ScoreEntry(PrototypePaletteEntry entry, string normalizedSearch)
    {
        var bestScore = int.MaxValue;
        foreach (var candidateText in EnumerateSearchTexts(entry))
        {
            var normalizedCandidate = Normalize(candidateText);
            if (normalizedCandidate.Length == 0)
                continue;

            if (normalizedCandidate == normalizedSearch)
                return 0;

            if (normalizedCandidate.StartsWith(normalizedSearch, StringComparison.Ordinal))
                bestScore = Math.Min(bestScore, 1);
            else if (normalizedCandidate.Contains(normalizedSearch, StringComparison.Ordinal))
                bestScore = Math.Min(bestScore, 2);
        }

        if (entry.ProtoNumber.ToString(CultureInfo.InvariantCulture) == normalizedSearch)
            return 0;

        return bestScore != int.MaxValue ? bestScore : 3;
    }

    private static bool MatchesNormalized(
        StaticObjectCatalogEntry entry,
        string normalizedSearch,
        IReadOnlyList<string> searchTokens
    )
    {
        foreach (var candidateText in EnumerateSearchTexts(entry))
        {
            var normalizedCandidate = Normalize(candidateText);
            if (normalizedCandidate.Length == 0)
                continue;

            if (
                normalizedCandidate == normalizedSearch
                || normalizedCandidate.Contains(normalizedSearch, StringComparison.Ordinal)
            )
            {
                return true;
            }

            if (
                searchTokens.Count != 0
                && searchTokens.All(token => normalizedCandidate.Contains(token, StringComparison.Ordinal))
            )
            {
                return true;
            }
        }

        return false;
    }

    private static int ScoreEntry(StaticObjectCatalogEntry entry, string normalizedSearch)
    {
        var bestScore = int.MaxValue;
        foreach (var candidateText in EnumerateSearchTexts(entry))
        {
            var normalizedCandidate = Normalize(candidateText);
            if (normalizedCandidate.Length == 0)
                continue;

            if (normalizedCandidate == normalizedSearch)
                return 0;

            if (normalizedCandidate.StartsWith(normalizedSearch, StringComparison.Ordinal))
                bestScore = Math.Min(bestScore, 1);
            else if (normalizedCandidate.Contains(normalizedSearch, StringComparison.Ordinal))
                bestScore = Math.Min(bestScore, 2);
        }

        if (
            entry.ProtoNumber is int protoNumber
            && protoNumber.ToString(CultureInfo.InvariantCulture) == normalizedSearch
        )
            return 0;

        return bestScore != int.MaxValue ? bestScore : 3;
    }

    private static int ScoreSuggestion(PrototypePaletteEntry entry, IReadOnlyList<string> searchTokens)
    {
        var candidateTexts = EnumerateSearchTexts(entry)
            .Select(Normalize)
            .Where(static text => text.Length != 0)
            .ToArray();
        if (candidateTexts.Length == 0)
            return -1;

        var matchedTokens = 0;
        var bonus = 0;
        foreach (var token in searchTokens)
        {
            var matched = false;
            foreach (var candidateText in candidateTexts)
            {
                if (!candidateText.Contains(token, StringComparison.Ordinal))
                    continue;

                matched = true;
                bonus += candidateText.StartsWith(token, StringComparison.Ordinal) ? 4 : 2;
                break;
            }

            if (matched)
                matchedTokens++;
        }

        return matchedTokens == 0 ? -1 : (matchedTokens * 10) + bonus;
    }

    private static int ScoreSuggestion(StaticObjectCatalogEntry entry, IReadOnlyList<string> searchTokens)
    {
        var candidateTexts = EnumerateSearchTexts(entry)
            .Select(Normalize)
            .Where(static text => text.Length != 0)
            .ToArray();
        if (candidateTexts.Length == 0)
            return -1;

        var matchedTokens = 0;
        var bonus = 0;
        foreach (var token in searchTokens)
        {
            var matched = false;
            foreach (var candidateText in candidateTexts)
            {
                if (!candidateText.Contains(token, StringComparison.Ordinal))
                    continue;

                matched = true;
                bonus += candidateText.StartsWith(token, StringComparison.Ordinal) ? 4 : 2;
                break;
            }

            if (matched)
                matchedTokens++;
        }

        return matchedTokens == 0 ? -1 : (matchedTokens * 10) + bonus;
    }

    private static IEnumerable<string> EnumerateSearchTexts(PrototypePaletteEntry entry)
    {
        yield return entry.AssetPath;
        yield return entry.ObjectType;
        yield return entry.ProtoNumber.ToString(CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(entry.DisplayName))
            yield return entry.DisplayName;

        if (!string.IsNullOrWhiteSpace(entry.Description))
            yield return entry.Description;

        if (!string.IsNullOrWhiteSpace(entry.PaletteGroup))
            yield return entry.PaletteGroup;

        if (!string.IsNullOrWhiteSpace(entry.ArtAssetPath))
            yield return entry.ArtAssetPath;
    }

    private static IEnumerable<string> EnumerateSearchTexts(StaticObjectCatalogEntry entry)
    {
        yield return entry.DisplayName;
        yield return entry.ObjectType;
        yield return entry.ObjectIdText;
        yield return entry.PrototypeText;
        yield return entry.SourceAssetPath;
        yield return entry.LocationText;
        yield return entry.SourceKindText;
        yield return entry.SummaryText;

        if (!string.IsNullOrWhiteSpace(entry.ObjectGuidText))
            yield return entry.ObjectGuidText;

        if (entry.ProtoNumber is int protoNumber and > 0)
            yield return protoNumber.ToString(CultureInfo.InvariantCulture);
    }

    private static string[] TokenizeSearch(string searchText) =>
        [
            .. searchText
                .Split(
                    [' ', '\t', '_', '-', '/', '\\'],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
                .Select(Normalize)
                .Where(static token => token.Length != 0)
                .Distinct(StringComparer.Ordinal),
        ];

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        Span<char> buffer = stackalloc char[value.Length];
        var count = 0;
        foreach (var ch in value)
        {
            if (!char.IsLetterOrDigit(ch))
                continue;

            buffer[count++] = char.ToLowerInvariant(ch);
        }

        return new string(buffer[..count]);
    }

    private static string DescribeResolvedPrototype(string token, ResolvedProtoDescriptor descriptor) =>
        !string.IsNullOrWhiteSpace(descriptor.DisplayName)
            ? $"{descriptor.DisplayName} [{descriptor.ProtoNumber}]"
            : descriptor.AssetPath ?? $"proto {descriptor.ProtoNumber} from '{token}'";

    private static string DescribeEntry(PrototypePaletteEntry entry) =>
        !string.IsNullOrWhiteSpace(entry.DisplayName) ? entry.DisplayName! : entry.AssetPath;

    private static string DescribeEntry(StaticObjectCatalogEntry entry) =>
        $"{entry.DisplayName} ({entry.ObjectIdText})";

    private readonly record struct DescriptorResolution(
        ResolvedProtoDescriptor? Descriptor,
        PrototypeResolutionSnapshot? Snapshot
    )
    {
        public bool Success => Descriptor.HasValue;
    }

    private readonly record struct ResolvedProtoDescriptor(
        int ProtoNumber,
        string? DisplayName,
        string? AssetPath,
        string ResolutionSource
    );

    private readonly record struct RankedPaletteEntry(PrototypePaletteEntry Entry, int Score);

    private readonly record struct RankedStaticObjectEntry(StaticObjectCatalogEntry Entry, int Score);
}
