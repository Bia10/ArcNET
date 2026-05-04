#!/usr/bin/env dotnet
// Task runner for ArcNET.
// Usage: dotnet Build.cs <command> [args]
// Requires: .NET 10 SDK.

#:property PublishAot=false

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

var repoRoot = RepoRoot();
var solutionPath = Path.Combine(repoRoot, "ArcNET.Build.slnx");
var packageVersionManifestPath = Path.Combine(repoRoot, "src", "ArcNET.PackageVersions.props");
var benchmarkProject = Path.Combine(repoRoot, "src", "Benchmarks", "ArcNET.Benchmarks", "ArcNET.Benchmarks.csproj");
var docTestProject = Path.Combine(repoRoot, "src", "DocTest", "ArcNET.DocTest", "ArcNET.DocTest.csproj");
var nugetOutputDirectory = Path.Combine(repoRoot, "artifacts", "nuget");
var packageVersions = ReadPackageVersions(packageVersionManifestPath);
var packageProjects = FindPackableProjects(repoRoot, packageVersions)
    .OrderBy(project => project.PackageId, StringComparer.OrdinalIgnoreCase)
    .ToArray();

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
        foreach (var testProject in FindTestProjects(repoRoot, docTestProject))
        {
            Run("dotnet", ["run", "--project", testProject, "-c", "Release"], repoRoot);
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

static IEnumerable<string> FindTestProjects(string repoRoot, string docTestProject)
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
    Console.WriteLine("  dotnet Build.cs test");
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

static void Run(string executable, IEnumerable<string> arguments, string workingDirectory)
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
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}: {executable}");
    }
}

static string EscapeArgument(string argument) =>
    argument.Contains(' ', StringComparison.Ordinal) || argument.Contains('"', StringComparison.Ordinal)
        ? $"\"{argument.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
        : argument;

static string RepoRoot([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

sealed record PackageProject(string ProjectName, string PackageId, string Version, string ProjectPath);
