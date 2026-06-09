using System.Text.RegularExpressions;

namespace ArcNET.Diagnostics.Tests;

public sealed partial class DiagnosticsSourceLayoutTests
{
    private static readonly Regex TopLevelTypePattern = CreateTopLevelTypePattern();
    private static readonly Regex TopLevelDelegatePattern = CreateTopLevelDelegatePattern();

    [Test]
    public async Task DiagnosticsSourceFiles_HaveOneTopLevelDeclarationMatchingTheirFileName()
    {
        var diagnosticsRoot = Path.Combine(FindRepositoryRoot(), "src", "Diagnostics");
        var files = Directory
            .GetFiles(diagnosticsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path =>
                !path.Contains(
                    $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                    StringComparison.OrdinalIgnoreCase
                )
                && !path.Contains(
                    $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        List<string> violations = [];
        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file);
            var typeMatches = TopLevelTypePattern.Matches(content);
            var delegateMatches = TopLevelDelegatePattern.Matches(content);
            var declarationCount = typeMatches.Count + delegateMatches.Count;
            var relativePath = Path.GetRelativePath(diagnosticsRoot, file);

            if (declarationCount == 0 && string.Equals(Path.GetFileName(file), "Program.cs", StringComparison.Ordinal))
                continue;

            if (declarationCount != 1)
            {
                violations.Add(
                    $"{relativePath}: expected 1 top-level declaration but found {declarationCount.ToString(System.Globalization.CultureInfo.InvariantCulture)}."
                );
                continue;
            }

            var fileName = Path.GetFileNameWithoutExtension(file);
            var topLevelTypeName =
                typeMatches.Count == 1 ? typeMatches[0].Groups[1].Value : delegateMatches[0].Groups[1].Value;
            if (!string.Equals(fileName, topLevelTypeName, StringComparison.Ordinal))
                violations.Add(
                    $"{relativePath}: file name {fileName} does not match top-level type {topLevelTypeName}."
                );
        }

        await Assert.That(violations).IsEmpty();
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "global.json")))
                return current.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the test output directory.");
    }

    [GeneratedRegex(
        @"(?m)^(?:public|internal|file)\s+(?:sealed\s+|static\s+|readonly\s+|partial\s+|abstract\s+)*(?:record\s+class|record\s+struct|record|class|interface|enum|struct)\s+([A-Za-z_][A-Za-z0-9_]*)"
    )]
    private static partial Regex CreateTopLevelTypePattern();

    [GeneratedRegex(
        @"(?m)^(?:public|internal|file)\s+(?:sealed\s+|static\s+|readonly\s+|partial\s+|abstract\s+)*delegate\s+[A-Za-z_][A-Za-z0-9_<>,\[\]\?\.]*\s+([A-Za-z_][A-Za-z0-9_]*)"
    )]
    private static partial Regex CreateTopLevelDelegatePattern();
}
