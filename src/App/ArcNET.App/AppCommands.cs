using ArcNET.Formats;
using ArcNET.GameData;
using ArcNET.Patch;
using Spectre.Console;

namespace ArcNET.App;

/// <summary>Top-level command implementations for the ArcNET CLI.</summary>
internal static class AppCommands
{
    internal const string ParseExtractedData = "Parse extracted game data";
    internal const string InstallHighResPatch = "Install High-Res patch";
    internal const string UninstallHighResPatch = "Uninstall High-Res patch";

    internal static async Task RunAsync(string command)
    {
        switch (command)
        {
            case ParseExtractedData:
                await RunParseExtractedDataAsync();
                break;

            case InstallHighResPatch:
                await RunInstallHighResPatchAsync();
                break;

            case UninstallHighResPatch:
                AnsiConsole.MarkupLine("[yellow]Uninstall High-Res patch is not yet implemented.[/]");
                break;

            default:
                AnsiConsole.MarkupLine($"[red]Unknown command: {command}[/]");
                break;
        }
    }

    private static async Task RunParseExtractedDataAsync()
    {
        var inputPath = AnsiConsole.Ask<string>("[green]Insert path to extracted game data directory[/]:");

        if (!Directory.Exists(inputPath))
        {
            AnsiConsole.MarkupLine("[red]Directory not found![/]");
            return;
        }

        await Task.Run(() =>
        {
            var files = GameDataLoader.DiscoverFiles(inputPath);
            var table = new Table().RoundedBorder().AddColumn("Format").AddColumn("File count");

            foreach (var (format, paths) in files)
                table.AddRow(format.ToString(), paths.Count.ToString());

            AnsiConsole.Write(table);
        });
    }

    private static async Task RunInstallHighResPatchAsync()
    {
        var arcDir = AnsiConsole.Ask<string>("[green]Insert path to Arcanum installation directory[/]:");
        var highResDir = Path.Combine(arcDir, "HighRes");

        if (!Directory.Exists(highResDir))
        {
            AnsiConsole.MarkupLine("[red]HighRes directory not found inside Arcanum dir![/]");
            return;
        }

        var configPath = Path.Combine(highResDir, "config.ini");
        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine("[red]config.ini not found in HighRes directory![/]");
            return;
        }

        var config = HighResConfig.ParseFile(configPath);
        AnsiConsole.MarkupLine($"[green]Loaded config: {config.Width}x{config.Height} @ {config.BitDepth}bpp[/]");

        await Task.CompletedTask;
    }
}
