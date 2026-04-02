// Build.cs — task runner for ArcNET
// Usage: dotnet run --project Build.cs -- <command>
// Commands: build | test | format | format-check | bench | publish
//
// Requires .NET 10 SDK. Run from the repo root.

using System;
using System.Diagnostics;
using System.IO;

var command = args.Length > 0 ? args[0].ToLowerInvariant() : "build";
var root = Path.GetDirectoryName(Path.GetFullPath("Build.cs"))!;
var slnx = Path.Combine(root, "ArcNET.Build.slnx");

switch (command)
{
    case "build":
        Run("dotnet", $"build {slnx} -c Release");
        break;

    case "test":
        foreach (var testProj in Directory.GetFiles(root, "*.Tests.csproj", SearchOption.AllDirectories))
            Run("dotnet", $"run --project {testProj}");
        break;

    case "format":
        Run("dotnet", "csharpier format .");
        Run("dotnet", $"format style {slnx}");
        Run("dotnet", $"format analyzers {slnx}");
        break;

    case "format-check":
        Run("dotnet", "csharpier check .");
        Run("dotnet", $"format style {slnx} --verify-no-changes");
        Run("dotnet", $"format analyzers {slnx} --verify-no-changes");
        break;

    case "bench":
        var benchProj = Path.Combine(root, "src", "Benchmarks", "ArcNET.Benchmarks");
        Run("dotnet", $"run --project {benchProj} -c Release");
        break;

    case "publish":
        Run("dotnet", $"pack {slnx} -c Release -o artifacts/nuget");
        break;

    default:
        Console.Error.WriteLine($"Unknown command: {command}");
        Console.Error.WriteLine("Usage: dotnet run --project Build.cs -- build|test|format|format-check|bench|publish");
        Environment.Exit(1);
        break;
}

static void Run(string cmd, string args)
{
    Console.WriteLine($"\n> {cmd} {args}");
    var psi = new ProcessStartInfo(cmd, args) { UseShellExecute = false };
    var p = Process.Start(psi)!;
    p.WaitForExit();
    if (p.ExitCode != 0)
    {
        Console.Error.WriteLine($"Command failed with exit code {p.ExitCode}");
        Environment.Exit(p.ExitCode);
    }
}
