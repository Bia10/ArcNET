using System.Collections.Concurrent;
using System.Text;

namespace ArcNET.Diagnostics;

public static class CeSourceCatalogLoader
{
    public const string SourceRootEnvironmentVariable = "ARCNET_ARCANUM_CE_SOURCE";

    public static CeSourceCatalog LoadDefault() => Load(ResolveDefaultSourceRoot());

    public static CeSourceCatalog Load(string sourceRoot)
    {
        var resolvedSourceRoot = ResolveSourceRoot(sourceRoot);
        return s_catalogs.GetOrAdd(resolvedSourceRoot, static root => BuildCatalog(root));
    }

    public static string ResolveDefaultSourceRoot()
    {
        var envValue = Environment.GetEnvironmentVariable(SourceRootEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(envValue))
            return ResolveSourceRoot(envValue);

        var candidates = new[]
        {
            Path.Combine(Environment.CurrentDirectory, "..", "arcanum-ce", "src"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "arcanum-ce", "src"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "arcanum-ce"),
        };

        foreach (var candidate in candidates)
        {
            try
            {
                return ResolveSourceRoot(candidate);
            }
            catch (DirectoryNotFoundException) { }
        }

        throw new DirectoryNotFoundException(
            $"Unable to locate the arcanum-ce source tree automatically. Set {SourceRootEnvironmentVariable} or pass an explicit source-root path."
        );
    }

    public static string ResolveSourceRoot(string sourceRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRoot);

        var fullPath = Path.GetFullPath(sourceRoot);
        if (Directory.Exists(fullPath))
        {
            if (IsSourceRoot(fullPath))
                return fullPath;

            var nestedSrc = Path.Combine(fullPath, "src");
            if (Directory.Exists(nestedSrc) && IsSourceRoot(nestedSrc))
                return Path.GetFullPath(nestedSrc);
        }

        throw new DirectoryNotFoundException(
            $"The arcanum-ce source root '{fullPath}' was not found. Pass the CE repo root or its src directory."
        );
    }

    private static bool IsSourceRoot(string path) =>
        Directory.Exists(path)
        && Directory.Exists(Path.Combine(path, "game"))
        && Directory.Exists(Path.Combine(path, "ui"));

    private static CeSourceCatalog BuildCatalog(string sourceRoot)
    {
        var functions = Directory
            .EnumerateFiles(sourceRoot, "*.*", SearchOption.AllDirectories)
            .Where(static path => s_sourceExtensions.Contains(Path.GetExtension(path)))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .SelectMany(path => ParseFile(sourceRoot, path))
            .OrderBy(static function => function.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static function => function.LineNumber)
            .ThenBy(static function => function.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new CeSourceCatalog(sourceRoot, functions);
    }

    private static IEnumerable<CeSourceFunction> ParseFile(string sourceRoot, string filePath)
    {
        var text = File.ReadAllText(filePath);
        if (text.Length == 0)
            return [];

        var sanitized = StripTrivia(text);
        var lineStarts = BuildLineStarts(sanitized);
        var relativePath = Path.GetRelativePath(sourceRoot, filePath).Replace('\\', '/');
        var area = relativePath.Contains('/')
            ? relativePath.Split('/', 2, StringSplitOptions.RemoveEmptyEntries)[0]
            : "(root)";
        List<CeSourceFunction> functions = [];

        var segmentStart = 0;
        var braceDepth = 0;
        for (var index = 0; index < sanitized.Length; index++)
        {
            var ch = sanitized[index];
            switch (ch)
            {
                case '{':
                    if (
                        braceDepth == 0
                        && TryParseFunction(
                            sanitized,
                            segmentStart,
                            index,
                            lineStarts,
                            relativePath,
                            area,
                            out var function
                        )
                    )
                        functions.Add(function);

                    braceDepth++;
                    break;
                case '}':
                    if (braceDepth > 0)
                    {
                        braceDepth--;
                        if (braceDepth == 0)
                            segmentStart = index + 1;
                    }

                    break;
                case ';':
                    if (braceDepth == 0)
                        segmentStart = index + 1;

                    break;
            }
        }

        return functions;
    }

    private static bool TryParseFunction(
        string sanitizedText,
        int segmentStart,
        int openBraceIndex,
        int[] lineStarts,
        string relativePath,
        string area,
        out CeSourceFunction function
    )
    {
        var end = openBraceIndex - 1;
        while (end >= segmentStart && char.IsWhiteSpace(sanitizedText[end]))
            end--;

        if (end < segmentStart)
        {
            function = default;
            return false;
        }

        var start = segmentStart;
        while (start <= end && char.IsWhiteSpace(sanitizedText[start]))
            start++;

        if (start > end)
        {
            function = default;
            return false;
        }

        var snippet = sanitizedText[start..(end + 1)];
        if (!LooksLikeFunctionSignature(snippet))
        {
            function = default;
            return false;
        }

        var closeParenIndex = snippet.LastIndexOf(')');
        var openParenIndex = FindMatchingOpenParen(snippet, closeParenIndex);
        if (openParenIndex < 0)
        {
            function = default;
            return false;
        }

        var name = ParseFunctionName(snippet, openParenIndex);
        if (name.Length == 0 || s_disallowedNames.Contains(name))
        {
            function = default;
            return false;
        }

        var signature = CollapseWhitespace(snippet);
        var lineNumber = GetLineNumber(lineStarts, start);
        function = new CeSourceFunction(
            name,
            relativePath,
            lineNumber,
            area,
            ContainsToken(signature, "static"),
            signature
        );
        return true;
    }

    private static bool LooksLikeFunctionSignature(string snippet)
    {
        if (snippet.IndexOf('(') < 0 || snippet.IndexOf(')') < 0)
            return false;

        if (snippet.Contains('='))
            return false;

        if (
            ContainsToken(snippet, "typedef")
            || ContainsToken(snippet, "struct")
            || ContainsToken(snippet, "enum")
            || ContainsToken(snippet, "union")
        )
        {
            return false;
        }

        var closeParenIndex = snippet.LastIndexOf(')');
        if (closeParenIndex < 0)
            return false;

        for (var index = closeParenIndex + 1; index < snippet.Length; index++)
        {
            if (!char.IsWhiteSpace(snippet[index]))
                return false;
        }

        return true;
    }

    private static int FindMatchingOpenParen(string snippet, int closeParenIndex)
    {
        var depth = 0;
        for (var index = closeParenIndex; index >= 0; index--)
        {
            switch (snippet[index])
            {
                case ')':
                    depth++;
                    break;
                case '(':
                    depth--;
                    if (depth == 0)
                        return index;

                    break;
            }
        }

        return -1;
    }

    private static string ParseFunctionName(string snippet, int openParenIndex)
    {
        var end = openParenIndex - 1;
        while (end >= 0 && char.IsWhiteSpace(snippet[end]))
            end--;

        if (end < 0)
            return string.Empty;

        var start = end;
        while (start >= 0 && IsIdentifierCharacter(snippet[start]))
            start--;

        return snippet[(start + 1)..(end + 1)];
    }

    private static bool IsIdentifierCharacter(char ch) => char.IsLetterOrDigit(ch) || ch == '_';

    private static string StripTrivia(string text)
    {
        var builder = new StringBuilder(text.Length);
        var state = ParserState.Normal;
        var atLineStart = true;

        for (var index = 0; index < text.Length; index++)
        {
            var ch = text[index];
            var next = index + 1 < text.Length ? text[index + 1] : '\0';

            switch (state)
            {
                case ParserState.Normal:
                    if (atLineStart && ch == '#')
                    {
                        builder.Append(' ');
                        state = ParserState.Preprocessor;
                        atLineStart = false;
                        continue;
                    }

                    if (ch == '/' && next == '/')
                    {
                        builder.Append("  ");
                        index++;
                        state = ParserState.LineComment;
                        atLineStart = false;
                        continue;
                    }

                    if (ch == '/' && next == '*')
                    {
                        builder.Append("  ");
                        index++;
                        state = ParserState.BlockComment;
                        atLineStart = false;
                        continue;
                    }

                    if (ch == '"')
                    {
                        builder.Append(' ');
                        state = ParserState.StringLiteral;
                        atLineStart = false;
                        continue;
                    }

                    if (ch == '\'')
                    {
                        builder.Append(' ');
                        state = ParserState.CharLiteral;
                        atLineStart = false;
                        continue;
                    }

                    builder.Append(ch);
                    atLineStart = ch is '\r' or '\n' || (atLineStart && char.IsWhiteSpace(ch));
                    break;
                case ParserState.Preprocessor:
                    if (ch == '\\' && next == '\n')
                    {
                        builder.Append("  ");
                        index++;
                        continue;
                    }

                    if (ch == '\n')
                    {
                        builder.Append('\n');
                        state = ParserState.Normal;
                        atLineStart = true;
                        continue;
                    }

                    builder.Append(ch == '\r' ? '\r' : ' ');
                    break;
                case ParserState.LineComment:
                    if (ch == '\n')
                    {
                        builder.Append('\n');
                        state = ParserState.Normal;
                        atLineStart = true;
                        continue;
                    }

                    builder.Append(ch == '\r' ? '\r' : ' ');
                    break;
                case ParserState.BlockComment:
                    if (ch == '*' && next == '/')
                    {
                        builder.Append("  ");
                        index++;
                        state = ParserState.Normal;
                        continue;
                    }

                    builder.Append(ch is '\r' or '\n' ? ch : ' ');
                    break;
                case ParserState.StringLiteral:
                    if (ch == '\\' && next != '\0')
                    {
                        builder.Append("  ");
                        index++;
                        continue;
                    }

                    builder.Append(ch is '\r' or '\n' ? ch : ' ');
                    if (ch == '"')
                        state = ParserState.Normal;

                    break;
                case ParserState.CharLiteral:
                    if (ch == '\\' && next != '\0')
                    {
                        builder.Append("  ");
                        index++;
                        continue;
                    }

                    builder.Append(ch is '\r' or '\n' ? ch : ' ');
                    if (ch == '\'')
                        state = ParserState.Normal;

                    break;
            }
        }

        return builder.ToString();
    }

    private static int[] BuildLineStarts(string text)
    {
        List<int> lineStarts = [0];
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\n' && index + 1 < text.Length)
                lineStarts.Add(index + 1);
        }

        return [.. lineStarts];
    }

    private static int GetLineNumber(int[] lineStarts, int characterIndex)
    {
        var position = Array.BinarySearch(lineStarts, characterIndex);
        if (position >= 0)
            return position + 1;

        return ~position;
    }

    private static string CollapseWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;
        foreach (var ch in value)
        {
            if (char.IsWhiteSpace(ch))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(ch);
        }

        return builder.ToString().Trim();
    }

    private static bool ContainsToken(string text, string token)
    {
        for (var index = 0; index <= text.Length - token.Length; index++)
        {
            if (!text.AsSpan(index, token.Length).Equals(token, StringComparison.OrdinalIgnoreCase))
                continue;

            var before = index == 0 ? '\0' : text[index - 1];
            var after = index + token.Length >= text.Length ? '\0' : text[index + token.Length];
            if (!IsTokenCharacter(before) && !IsTokenCharacter(after))
                return true;
        }

        return false;
    }

    private static bool IsTokenCharacter(char ch) => char.IsLetterOrDigit(ch) || ch == '_';

    private static readonly ConcurrentDictionary<string, CeSourceCatalog> s_catalogs = new(
        StringComparer.OrdinalIgnoreCase
    );

    private static readonly HashSet<string> s_sourceExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".c",
        ".cc",
        ".cpp",
        ".cxx",
        ".m",
        ".mm",
    };

    private static readonly HashSet<string> s_disallowedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "if",
        "for",
        "while",
        "switch",
        "return",
    };

    public sealed class CeSourceCatalog
    {
        private readonly Dictionary<string, CeSourceFunction[]> _functionsByNormalizedName;

        public CeSourceCatalog(string sourceRoot, IReadOnlyList<CeSourceFunction> functions)
        {
            SourceRoot = sourceRoot;
            Functions = [.. functions];
            FunctionCount = Functions.Length;
            _functionsByNormalizedName = Functions
                .GroupBy(static function => Normalize(function.Name), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.ToArray(),
                    StringComparer.OrdinalIgnoreCase
                );
            UniqueNameCount = _functionsByNormalizedName.Count;
            DuplicateNameCount = _functionsByNormalizedName.Count(static entry => entry.Value.Length > 1);
        }

        public string SourceRoot { get; }

        public CeSourceFunction[] Functions { get; }

        public int FunctionCount { get; }

        public int UniqueNameCount { get; }

        public int DuplicateNameCount { get; }

        public IEnumerable<CeSourceFunction> Query(string? filter, string? area)
        {
            var query = Functions.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(area))
                query = query.Where(function => function.Area.Equals(area, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(filter))
                return query;

            var normalized = Normalize(filter);
            return query.Where(function =>
                function.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || function.RelativePath.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || Normalize(function.Name).Contains(normalized, StringComparison.OrdinalIgnoreCase)
            );
        }

        public CeSourceFunction[] FindMatches(string token)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(token);
            var normalized = Normalize(token);
            return _functionsByNormalizedName.TryGetValue(normalized, out var matches) ? matches : [];
        }

        private static string Normalize(string value)
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

    public readonly record struct CeSourceFunction(
        string Name,
        string RelativePath,
        int LineNumber,
        string Area,
        bool IsStatic,
        string Signature
    );

    private enum ParserState
    {
        Normal,
        Preprocessor,
        LineComment,
        BlockComment,
        StringLiteral,
        CharLiteral,
    }
}
