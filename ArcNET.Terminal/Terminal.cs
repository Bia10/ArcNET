using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;

namespace ArcNET.Terminal
{
    public static class Terminal
    {
        public static void RenderLogo()
        {
            AnsiConsole.Render(
                new FigletText("ArcNET v0.0.1")
                    .LeftAligned()
                    .Color(Color.Green));
        }


        public static Table GenerateSummary(string directory, List<Tuple<int, List<string>>> data)
        {
            var totalFiles = data.First(p => p.Item1 == 0).Item2.Count;
            var facWalk = data.First(p => p.Item1 == 1).Item2.Count;
            var mesFiles = data.First(p => p.Item1 == 2).Item2.Count;
            var artFiles = data.First(p => p.Item1 == 3).Item2.Count;
            var secFiles = data.First(p => p.Item1 == 4).Item2.Count;

            var table = new Table()
                .RoundedBorder()
                .AddColumn("Summary")
                .AddColumn($"{directory}")
                .AddRow("Total files", $"{totalFiles}")
                .AddRow("facwalk. files", $"{facWalk}")
                .AddRow(".mes files", $"{mesFiles}")
                .AddRow(".ART files", $"{artFiles}")
                .AddRow(".sec files", $"{secFiles}")
                .AddRow("Unrecognized files", $"{totalFiles - (facWalk + mesFiles + artFiles + secFiles)}");

            return table;
        }

        public static string GetMainMenuChoice()
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]What would you like to do[/]?")
                    .PageSize(5)
                    .MoreChoicesText("[grey](Move up and down to reveal more choices)[/]")
                    .AddChoices("Extract game data", "Parse extracted game data", "Install High-Res patch",
                        "Uninstall High-Res patch", "Reinstall High-Res Patch", "Launch Arcanum.exe"));
        }
    }
}