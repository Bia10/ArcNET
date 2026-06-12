#!/usr/bin/env dotnet
// Task runner for ArcNET.
// Usage: dotnet Build.cs <command> [args]
// Requires: .NET 10 SDK.

#:property PublishAot=false

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

var repoRoot = RepoRoot();
var solutionPath = Path.Combine(repoRoot, "ArcNET.slnx");
var packageVersionManifestPath = Path.Combine(repoRoot, "src", "ArcNET.PackageVersions.props");
var benchmarkProject = Path.Combine(repoRoot, "src", "Benchmarks", "ArcNET.Benchmarks", "ArcNET.Benchmarks.csproj");
var docTestProject = Path.Combine(repoRoot, "src", "DocTest", "ArcNET.DocTest", "ArcNET.DocTest.csproj");
var nugetOutputDirectory = Path.Combine(repoRoot, "artifacts", "nuget");
var coverageOutputDirectory = Path.Combine(repoRoot, "artifacts", "TestResults");
var packageVersions = ReadPackageVersions(packageVersionManifestPath);
var packageProjects = FindPackableProjects(repoRoot, packageVersions)
    .OrderBy(project => project.PackageId, StringComparer.OrdinalIgnoreCase)
    .ToArray();
var runnableTestProjects = FindRunnableTestProjects(repoRoot, docTestProject).ToArray();

var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "help";
var commandArgs = args.Skip(1).ToArray();

switch (command)
{
    case "help":
        PrintHelp(packageProjects);
        return 0;

    case "build":
        Run("dotnet", ["build", solutionPath, "-c", "Release"], repoRoot);
        return 0;

    case "test":
        var treeFilter = commandArgs.FirstOrDefault();
        var testResultsDirectory = CreateRunResultsDirectory(coverageOutputDirectory, "test");
        var matchedProjects = 0;

        foreach (var testProject in runnableTestProjects)
        {
            var exitCode = Run(
                "dotnet",
                BuildTUnitTestArguments(
                    testProject,
                    CreateProjectResultsDirectory(testResultsDirectory, testProject),
                    treeFilter
                ),
                repoRoot,
                treeFilter is null ? [] : [8]
            );

            if (exitCode == 0)
            {
                matchedProjects++;
                continue;
            }

            Console.WriteLine($"No tests matched filter in {Path.GetFileNameWithoutExtension(testProject)}.");
        }

        if (treeFilter is not null && matchedProjects == 0)
        {
            throw new InvalidOperationException($"Tree filter '{treeFilter}' did not match any tests.");
        }

        return 0;

    case "coverage":
        Run("dotnet", ["tool", "restore"], repoRoot);
        Directory.CreateDirectory(coverageOutputDirectory);
        Run("dotnet", ["build", solutionPath, "-c", "Release"], repoRoot);

        foreach (var testProject in runnableTestProjects)
        {
            CollectCoverage(repoRoot, testProject, coverageOutputDirectory);
        }

        return 0;

    case "format":
        Run("dotnet", ["csharpier", "format", "."], repoRoot);
        Run("dotnet", ["format", "style", solutionPath], repoRoot);
        Run("dotnet", ["format", "analyzers", solutionPath], repoRoot);
        return 0;

    case "format-check":
        Run("dotnet", ["csharpier", "check", "."], repoRoot);
        Run("dotnet", ["format", "style", solutionPath, "--verify-no-changes"], repoRoot);
        Run("dotnet", ["format", "analyzers", solutionPath, "--verify-no-changes"], repoRoot);
        return 0;

    case "bench":
        Run("dotnet", ["run", "--project", benchmarkProject, "-c", "Release"], repoRoot);
        return 0;

    case "list-packages":
        foreach (var packageProject in packageProjects)
        {
            Console.WriteLine(
                $"{packageProject.PackageId} {packageProject.Version} {Path.GetRelativePath(repoRoot, packageProject.ProjectPath)}"
            );
        }

        return 0;

    case "package-version":
        Console.WriteLine(SelectSinglePackage(packageProjects, commandArgs).Version);
        return 0;

    case "pack":
    case "publish":
        Directory.CreateDirectory(nugetOutputDirectory);

        foreach (var packageProject in SelectPackages(packageProjects, commandArgs))
        {
            Run("dotnet", ["pack", packageProject.ProjectPath, "-c", "Release", "-o", nugetOutputDirectory], repoRoot);
        }

        return 0;

    default:
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp(packageProjects);
        return 1;
}

static IReadOnlyDictionary<string, string> ReadPackageVersions(string manifestPath)
{
    var document = XDocument.Load(manifestPath);
    var root =
        document.Root
        ?? throw new InvalidOperationException($"Version manifest '{manifestPath}' is missing a root element.");
    var packageVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (var packageVersionElement in root.Descendants("ArcNETPackageVersion"))
    {
        var packageId = packageVersionElement.Element("PackageId")?.Value.Trim();
        var version = packageVersionElement.Element("Version")?.Value.Trim();
        if (string.IsNullOrWhiteSpace(packageId) || string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidOperationException(
                $"Version manifest '{manifestPath}' contains an ArcNETPackageVersion entry without both PackageId and Version."
            );
        }

        packageVersions[packageId] = version;
    }

    return packageVersions;
}

static IEnumerable<PackageProject> FindPackableProjects(
    string repoRoot,
    IReadOnlyDictionary<string, string> packageVersions
)
{
    foreach (
        var projectPath in Directory.GetFiles(Path.Combine(repoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
    )
    {
        var document = XDocument.Load(projectPath);
        var projectRoot =
            document.Root
            ?? throw new InvalidOperationException($"Project file '{projectPath}' is missing a root element.");
        var isPackableValue = projectRoot.Descendants("IsPackable").Select(node => node.Value.Trim()).LastOrDefault();

        if (!bool.TryParse(isPackableValue, out var isPackable) || !isPackable)
        {
            continue;
        }

        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        var packageId = projectRoot.Descendants("PackageId").Select(node => node.Value.Trim()).LastOrDefault();
        var effectivePackageId = string.IsNullOrWhiteSpace(packageId) ? projectName : packageId;
        if (!packageVersions.TryGetValue(effectivePackageId, out var version))
        {
            throw new InvalidOperationException(
                $"Packable project '{effectivePackageId}' is missing an ArcNET.PackageVersions.props entry."
            );
        }

        yield return new PackageProject(projectName, effectivePackageId, version, projectPath);
    }
}

static IEnumerable<string> FindRunnableTestProjects(string repoRoot, string docTestProject)
{
    foreach (
        var testProject in Directory
            .GetFiles(Path.Combine(repoRoot, "src"), "*.Tests.csproj", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
    )
    {
        yield return testProject;
    }

    yield return docTestProject;
}

static void CollectCoverage(string repoRoot, string projectPath, string coverageOutputDirectory)
{
    var projectName = Path.GetFileNameWithoutExtension(projectPath);
    var projectRelativePath = NormalizeRelativePath(Path.GetRelativePath(repoRoot, projectPath));
    var projectRelativeDirectory = NormalizeRelativePath(
        Path.GetDirectoryName(projectRelativePath) ?? projectRelativePath
    );
    var outputPath = Path.Combine(coverageOutputDirectory, $"{projectName}.coverage.cobertura.xml");
    var resultsDirectory = CreateProjectResultsDirectory(
        CreateRunResultsDirectory(Path.Combine(coverageOutputDirectory, "coverage-runs"), "coverage"),
        projectPath
    );

    Console.WriteLine($"Collecting coverage for {projectName} from {projectRelativeDirectory}");
    Run(
        "dotnet",
        [
            "dotnet-coverage",
            "collect",
            BuildCommandLine(
                "dotnet",
                BuildTUnitTestArguments(
                    projectRelativePath,
                    resultsDirectory,
                    treeFilter: null,
                    noBuild: true,
                    noRestore: true
                )
            ),
            "--output",
            outputPath,
            "--output-format",
            "cobertura",
        ],
        repoRoot
    );
}

static IReadOnlyList<PackageProject> SelectPackages(
    IReadOnlyList<PackageProject> packageProjects,
    IReadOnlyList<string> selectors
)
{
    if (selectors.Count == 0)
    {
        return packageProjects;
    }

    var selectedProjects = new List<PackageProject>(selectors.Count);
    foreach (var selector in selectors)
    {
        var packageProject = packageProjects.FirstOrDefault(project =>
            string.Equals(project.PackageId, selector, StringComparison.OrdinalIgnoreCase)
            || string.Equals(project.ProjectName, selector, StringComparison.OrdinalIgnoreCase)
        );

        if (packageProject is null)
        {
            throw new InvalidOperationException(
                $"Unknown package '{selector}'. Run 'dotnet Build.cs list-packages' to see the supported package ids."
            );
        }

        if (
            !selectedProjects.Any(project =>
                string.Equals(project.PackageId, packageProject.PackageId, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            selectedProjects.Add(packageProject);
        }
    }

    return selectedProjects;
}

static PackageProject SelectSinglePackage(
    IReadOnlyList<PackageProject> packageProjects,
    IReadOnlyList<string> selectors
)
{
    if (selectors.Count != 1)
    {
        throw new InvalidOperationException(
            "Expected exactly one package id. Run 'dotnet Build.cs list-packages' to see the supported package ids."
        );
    }

    return SelectPackages(packageProjects, selectors)[0];
}

static void PrintHelp(IReadOnlyList<PackageProject> packageProjects)
{
    Console.WriteLine("ArcNET task runner");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet Build.cs help");
    Console.WriteLine("  dotnet Build.cs build");
    Console.WriteLine("  dotnet Build.cs test [treenode-filter]");
    Console.WriteLine("  dotnet Build.cs coverage");
    Console.WriteLine("  dotnet Build.cs format");
    Console.WriteLine("  dotnet Build.cs format-check");
    Console.WriteLine("  dotnet Build.cs bench");
    Console.WriteLine("  dotnet Build.cs list-packages");
    Console.WriteLine("  dotnet Build.cs package-version <PackageId>");
    Console.WriteLine("  dotnet Build.cs pack [PackageId ...]");
    Console.WriteLine("  dotnet Build.cs publish [PackageId ...]");
    Console.WriteLine();
    Console.WriteLine("Packable package ids:");
    foreach (var packageProject in packageProjects)
    {
        Console.WriteLine($"  {packageProject.PackageId} {packageProject.Version}");
    }
}

static int Run(
    string executable,
    IEnumerable<string> arguments,
    string workingDirectory,
    IReadOnlyCollection<int>? allowedExitCodes = null
)
{
    var argumentList = arguments.ToArray();
    Console.WriteLine();
    Console.WriteLine($"> {executable} {string.Join(' ', argumentList.Select(EscapeArgument))}");

    var processStartInfo = new ProcessStartInfo(executable)
    {
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
    };

    foreach (var argument in argumentList)
    {
        processStartInfo.ArgumentList.Add(argument);
    }

    using var process =
        Process.Start(processStartInfo) ?? throw new InvalidOperationException($"Failed to start '{executable}'.");

    process.WaitForExit();
    if (process.ExitCode != 0 && !(allowedExitCodes?.Contains(process.ExitCode) ?? false))
    {
        throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}: {executable}");
    }

    return process.ExitCode;
}

static string BuildCommandLine(string executable, IEnumerable<string> arguments) =>
    $"{QuoteCommandValue(executable)} {string.Join(' ', arguments.Select(EscapeArgument))}";

static string CreateRunResultsDirectory(string rootDirectory, string prefix)
{
    var resultsDirectory = Path.Combine(rootDirectory, $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmssfff}");
    Directory.CreateDirectory(resultsDirectory);
    return resultsDirectory;
}

static string CreateProjectResultsDirectory(string rootDirectory, string projectPath)
{
    var projectDirectory = Path.Combine(rootDirectory, Path.GetFileNameWithoutExtension(projectPath));
    Directory.CreateDirectory(projectDirectory);
    return projectDirectory;
}

static IReadOnlyList<string> BuildTUnitTestArguments(
    string projectPath,
    string resultsDirectory,
    string? treeFilter,
    bool noBuild = false,
    bool noRestore = false
)
{
    List<string> arguments =
    [
        "test",
        "--project",
        projectPath,
        "-c",
        "Release",
        "--timeout",
        "10m",
        "--results-directory",
        resultsDirectory,
    ];

    if (noBuild)
    {
        arguments.Add("--no-build");
    }

    if (noRestore)
    {
        arguments.Add("--no-restore");
    }

    if (!string.IsNullOrWhiteSpace(treeFilter))
    {
        arguments.Add("--treenode-filter");
        arguments.Add(treeFilter);
    }

    return arguments;
}

static string EscapeArgument(string argument) =>
    argument.Contains(' ', StringComparison.Ordinal) || argument.Contains('"', StringComparison.Ordinal)
        ? $"\"{argument.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
        : argument;

static string QuoteCommandValue(string value) => value.Contains(' ', StringComparison.Ordinal) ? $"\"{value}\"" : value;

static string NormalizeRelativePath(string path) => path.Replace('\\', '/');

static string RepoRoot([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

sealed record PackageProject(string ProjectName, string PackageId, string Version, string ProjectPath);
