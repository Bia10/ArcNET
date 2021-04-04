using ArcNET.Utilities;
using Spectre.Console;
using System.Collections.Generic;
using System.Reflection;

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

        public static Table DirectoryTable(string directory, List<List<string>> data)
        {
            var table = new Table()
                .RoundedBorder()
                .AddColumn("Summary")
                .AddColumn($"{directory}");

            var totalFiles = data[0].Count;
            var facWalk = data[1].Count;
            var mesFiles = data[2].Count;
            var artFiles = data[3].Count;
            var secFiles = data[4].Count;

            table.AddRow("Total files", $"{totalFiles}");
            table.AddRow("facwalk. files", $"{facWalk}");
            table.AddRow(".mes files", $"{mesFiles}");
            table.AddRow(".ART files", $"{artFiles}");
            table.AddRow(".sec files", $"{secFiles}");
            table.AddRow("Unrecognized files", $"{totalFiles - (facWalk + mesFiles + artFiles + secFiles)}");

            return table;
        }

        public static Table ConfigTable()
        {
            var table = new Table()
                .RoundedBorder()
                .AddColumn("Parameter name")
                .AddColumn("Parameter value");

            const BindingFlags bindingFlags = BindingFlags.Public |
                                              BindingFlags.NonPublic |
                                              BindingFlags.Instance |
                                              BindingFlags.Static;

            foreach (var field in typeof(HighResConfig).GetFields(bindingFlags))
            {
                table.AddRow($"{field.Name}", $"{field.GetValue(field)}");
            }

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
                        "Uninstall High-Res patch", "Launch Arcanum.exe"));
        }
    }
}