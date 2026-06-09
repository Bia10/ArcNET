namespace ArcNET.Diagnostics;

public sealed class ModuleSymbolCatalog
{
    private readonly Dictionary<string, ModuleFunctionSymbol[]> _symbolsByExactName;
    private readonly Dictionary<string, ModuleFunctionSymbol[]> _symbolsByNormalizedName;

    public ModuleSymbolCatalog(string modulePath, IReadOnlyList<ModuleFunctionSymbol> symbols)
    {
        ModulePath = modulePath;
        ModuleFileName = Path.GetFileName(modulePath);
        Symbols = [.. symbols];
        FunctionCount = Symbols.Length;
        _symbolsByExactName = Symbols
            .GroupBy(static symbol => symbol.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        _symbolsByNormalizedName = Symbols
            .GroupBy(static symbol => NormalizeKey(symbol.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        DuplicateNameCount = _symbolsByExactName.Count(static entry => entry.Value.Length > 1);
        UniqueNameCount = _symbolsByExactName.Count;
    }

    public string ModulePath { get; }

    public string ModuleFileName { get; }

    public ModuleFunctionSymbol[] Symbols { get; }

    public int FunctionCount { get; }

    public int UniqueNameCount { get; }

    public int DuplicateNameCount { get; }

    public IEnumerable<ModuleFunctionSymbol> Query(string? filter, int limit, bool duplicatesOnly)
    {
        var predicate = NormalizeKey(filter ?? string.Empty);
        var query = Symbols.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(symbol =>
                symbol.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || NormalizeKey(symbol.Name).Contains(predicate, StringComparison.OrdinalIgnoreCase)
            );
        }

        if (duplicatesOnly)
            query = query.Where(symbol => _symbolsByExactName[symbol.Name].Length > 1);

        return query.Take(limit);
    }

    public ModuleFunctionSymbol[] FindMatches(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return [.. FindSymbolMatches(token)];
    }

    public ModuleFunctionSymbol ResolveUnique(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        if (TryResolveUnique(token, out var symbol))
            return symbol;

        var matches = FindMatches(token);
        if (matches.Length == 0)
            throw new KeyNotFoundException($"Symbol target '{token}' was not found inside {ModuleFileName}.");

        var sample = string.Join(
            ", ",
            matches
                .Take(5)
                .Select(match =>
                    $"{match.Name} ({ModuleAddressFormatter.FormatModuleAddress(ModuleFileName, match.Rva)})"
                )
        );
        throw new InvalidOperationException(
            $"Symbol target '{token}' is ambiguous inside {ModuleFileName}; it matched {matches.Length} function symbol(s): {sample}."
        );
    }

    public bool TryResolveUnique(string token, out ModuleFunctionSymbol symbol)
    {
        if (_symbolsByExactName.TryGetValue(token, out var exactMatches) && exactMatches.Length == 1)
        {
            symbol = exactMatches[0];
            return true;
        }

        var normalized = NormalizeKey(token);
        if (
            _symbolsByNormalizedName.TryGetValue(normalized, out var normalizedMatches)
            && normalizedMatches.Length == 1
        )
        {
            symbol = normalizedMatches[0];
            return true;
        }

        symbol = default;
        return false;
    }

    private IEnumerable<ModuleFunctionSymbol> FindSymbolMatches(string token)
    {
        if (_symbolsByExactName.TryGetValue(token, out var exactMatches))
            return exactMatches;

        var normalized = NormalizeKey(token);
        return _symbolsByNormalizedName.TryGetValue(normalized, out var normalizedMatches) ? normalizedMatches : [];
    }

    private static string NormalizeKey(string value)
    {
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
}
