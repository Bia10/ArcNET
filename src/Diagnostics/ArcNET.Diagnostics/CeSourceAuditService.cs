namespace ArcNET.Diagnostics;

public static class CeSourceAuditService
{
    public static CeSourceAuditSnapshot Create(CeSourceAuditRequest request, ModuleSymbolCatalog? symbolCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var sourceCatalog = request.SourceRoot is null
            ? CeSourceCatalogLoader.LoadDefault()
            : CeSourceCatalogLoader.Load(request.SourceRoot);

        var watchKeys = RuntimeWatchCatalog
            .AllHooks.Select(static hook => CatalogAddressResolver.NormalizeKey(hook.Key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var debuggerKeys = FunctionCatalog
            .KnownFunctionKeys.Select(CatalogAddressResolver.NormalizeKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var signatureKeys = CatalogSignatureCatalog
            .Keys.Select(CatalogAddressResolver.NormalizeKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var auditedFunctions = sourceCatalog
            .Query(request.Filter, request.Area)
            .Select(function => BuildFunctionSnapshot(function, watchKeys, debuggerKeys, signatureKeys, symbolCatalog))
            .Where(function => !request.MissingOnly || !function.Coverage.AnyCatalogCoverage)
            .Where(function => !request.CoveredOnly || function.Coverage.AnyCatalogCoverage)
            .ToArray();

        var returnedFunctions = auditedFunctions.Take(request.Limit).ToArray();
        var areas = auditedFunctions
            .GroupBy(static function => function.Area, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static group => new CeSourceAuditAreaSummary(
                group.Key,
                group.Count(),
                group.Count(static function => function.Coverage.AnyCatalogCoverage),
                group.Count(static function => !function.Coverage.AnyCatalogCoverage),
                group.Count(static function => function.Symbol.UniqueSymbolMatch)
            ))
            .ToArray();

        var summary = new CeSourceAuditSummarySnapshot(
            sourceCatalog.FunctionCount,
            sourceCatalog.UniqueNameCount,
            sourceCatalog.DuplicateNameCount,
            auditedFunctions.Length,
            auditedFunctions.Count(static function => function.Coverage.WatchHookCoverage),
            auditedFunctions.Count(static function => function.Coverage.DebuggerFunctionCoverage),
            auditedFunctions.Count(static function => function.Coverage.SignatureCoverage),
            auditedFunctions.Count(static function => function.Coverage.AnyCatalogCoverage),
            auditedFunctions.Count(static function => !function.Coverage.AnyCatalogCoverage),
            auditedFunctions.Count(static function => function.Symbol.UniqueSymbolMatch),
            auditedFunctions.Count(static function => function.Symbol.MatchCount > 1)
        );

        var symbolSnapshot = symbolCatalog is null
            ? null
            : new CeSourceAuditSymbolCatalogSnapshot(
                symbolCatalog.ModulePath,
                symbolCatalog.ModuleFileName,
                symbolCatalog.FunctionCount,
                symbolCatalog.UniqueNameCount,
                symbolCatalog.DuplicateNameCount
            );

        return new CeSourceAuditSnapshot(
            DateTimeOffset.UtcNow,
            sourceCatalog.SourceRoot,
            request.SourceRoot is null,
            request.Filter,
            request.Area,
            request.Limit,
            request.MissingOnly,
            request.CoveredOnly,
            symbolSnapshot,
            summary,
            areas,
            returnedFunctions
        );
    }

    private static void Validate(CeSourceAuditRequest request)
    {
        if (request.Limit <= 0)
            throw new InvalidOperationException("Limit must be greater than zero.");

        if (request.MissingOnly && request.CoveredOnly)
            throw new InvalidOperationException("MissingOnly and CoveredOnly cannot be combined.");
    }

    private static CeSourceFunctionSnapshot BuildFunctionSnapshot(
        CeSourceCatalogLoader.CeSourceFunction function,
        HashSet<string> watchKeys,
        HashSet<string> debuggerKeys,
        HashSet<string> signatureKeys,
        ModuleSymbolCatalog? symbolCatalog
    )
    {
        var normalizedName = CatalogAddressResolver.NormalizeKey(function.Name);
        var coverage = new CeSourceCoverageSnapshot(
            watchKeys.Contains(normalizedName),
            debuggerKeys.Contains(normalizedName),
            signatureKeys.Contains(normalizedName),
            watchKeys.Contains(normalizedName)
                || debuggerKeys.Contains(normalizedName)
                || signatureKeys.Contains(normalizedName)
        );
        var symbol = BuildSymbolCoverage(function, symbolCatalog);
        return new CeSourceFunctionSnapshot(
            function.Name,
            function.RelativePath,
            function.LineNumber,
            function.Area,
            function.IsStatic,
            function.Signature,
            coverage,
            symbol
        );
    }

    private static CeSourceSymbolCoverageSnapshot BuildSymbolCoverage(
        CeSourceCatalogLoader.CeSourceFunction function,
        ModuleSymbolCatalog? symbolCatalog
    )
    {
        if (symbolCatalog is null)
            return new CeSourceSymbolCoverageSnapshot(false, 0, []);

        var matches = symbolCatalog.FindMatches(function.Name);
        var sampleSites = matches
            .Take(3)
            .Select(match =>
                $"{match.Name} ({CodeCatalog.FormatModuleAddress(symbolCatalog.ModuleFileName, match.Rva)})"
            )
            .ToArray();
        return new CeSourceSymbolCoverageSnapshot(matches.Length == 1, matches.Length, sampleSites);
    }
}
