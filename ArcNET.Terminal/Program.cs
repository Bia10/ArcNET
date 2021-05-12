using ArcNET.Utilities;
using Spectre.Console;
using System;
using System.IO;
using System.Linq;
using Utils.Console;

namespace ArcNET.Terminal
{
    internal static class Program
    {
        private static string GetHighResDir()
        {
            var pathToArcDir = AnsiConsole.Ask<string>("[green]Insert path to Arcanum dir[/]:");
            var pathToHighResDir = Path.Combine(pathToArcDir + "\\HighRes");

            while (!Directory.Exists(pathToHighResDir))
            {
                ConsoleExtensions.Log("HighRes dir not found!", "error");
                pathToArcDir = AnsiConsole.Ask<string>("[green]Insert path to Arcanum dir again[/]:");
                pathToHighResDir = Path.Combine(pathToArcDir + "\\HighRes");
            }

            return pathToHighResDir;
        }

        private static void LaunchWeidu(string launchArgs, string pathToDir = "")
        {
            var dirPath = string.IsNullOrEmpty(pathToDir) ? GetHighResDir() : pathToDir;
            var procPath = Path.Combine(dirPath + "\\weidu.exe");
            if (!File.Exists(procPath))
                throw new InvalidOperationException($"weidu.exe file not found at path: {procPath}");

            var Launcher = new ProcLauncher(procPath, "");

            switch (launchArgs)
            {
                case ProcLauncher.CmdArguments.Install:
                    Launcher.CmdArgs = ProcLauncher.CmdArguments.Install;
                    Launcher.Launch();
                    break;
                case ProcLauncher.CmdArguments.Uninstall:
                    Launcher.CmdArgs = ProcLauncher.CmdArguments.Uninstall;
                    Launcher.Launch();
                    break;

                default:
                    throw new InvalidOperationException($"Unknown launch arguments: {launchArgs}");
            }
        }

        private static void Main()
        {
            Terminal.RenderLogo();

            var choice = Terminal.GetMainMenuChoice();
            ConsoleExtensions.Log($"Selected choice: {choice}", "info");
            switch (choice)
            {
                case "Extract game data":
                    ConsoleExtensions.Log($"Choice: {choice} is currently unsupported!", "error");
                    break;
                case "Parse extracted game data":
                    Parser.ParseExtractedData();
                    break;
                case "Install High-Res patch":
                    var pathToHighResDir = GetHighResDir();
                    var files = Directory.EnumerateFiles(
                        pathToHighResDir, "*.*", SearchOption.AllDirectories).ToList();

                    if (files.Count == 0)
                    {
                        ConsoleExtensions.Log("HighResFolder empty proceeding to clone latest version", "info");
                        GitHub.CloneHighResPatch(pathToHighResDir);
                    }

                    var configPath = Path.Combine(pathToHighResDir + "\\config.ini");
                    if (!File.Exists(configPath))
                        throw new InvalidOperationException($"Config file not found at path: {configPath}");

                    ConsoleExtensions.Log("Gathering environment info", "info");
                    var envInfo = new EnvironmentInfo();
                    envInfo.Print();

                    ConsoleExtensions.Log("Auto-config according to environment info", "info");
                    HighResConfig.AutoConfigure(envInfo);

                    ConsoleExtensions.Log("Summary of config.ini:", "info");
                    AnsiConsole.Render(Terminal.ConfigTable());

                    if (AnsiConsole.Confirm("Would you like to change config?"))
                    {
                        AnsiConsole.WriteException(new NotImplementedException());
                        return;
                    }

                    HighResConfig.Write(configPath);
                    LaunchWeidu(ProcLauncher.CmdArguments.Install, pathToHighResDir);
                    break;
                case "Uninstall High-Res patch":
                    LaunchWeidu(ProcLauncher.CmdArguments.Uninstall);
                    break;

                default:
                    ConsoleExtensions.Log($"Choice: {choice} is currently unsupported!", "error");
                    break;
            }
        }
    }
}