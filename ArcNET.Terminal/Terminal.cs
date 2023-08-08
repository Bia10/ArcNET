using ArcNET.Utilities;
using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ArcNET.Terminal;

public static class Terminal
{
    public static void RenderLogo()
    {
        AnsiConsole.Write(new FigletText("ArcNET v0.0.1").Color(Color.Green));
    }

    public static IRenderable DirectoryTable(string dirPath, IEnumerable<Tuple<List<string>, Parser.FileType>> data)
    {
        Table table = new Table()
            .RoundedBorder()
            .AddColumn("Summary for dirPath:")
            .AddColumn($"{dirPath}");

        foreach ((List<string> pathToFiles, Parser.FileType fileType) in data)
            table.AddRow($"{Enum.GetName(typeof(Parser.FileType), fileType)}", $"{pathToFiles.Count}");

        return table;
    }

    public static IRenderable ConfigTable()
    {
        Table table = new Table()
            .RoundedBorder()
            .AddColumn("Parameter name")
            .AddColumn("Parameter value");

        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                          BindingFlags.Instance | BindingFlags.Static;

        foreach (FieldInfo field in typeof(HighResConfig).GetFields(bindingFlags))
            table.AddRow($"{field.Name}", $"{field.GetValue(field)}");

        return table;
    }

    //TODO: merge with summary table?
    public static IRenderable ReportTable(string dirPath, IEnumerable<Tuple<List<string>, Parser.FileType>> data)
    {
        Table table = new Table()
            .RoundedBorder()
            .AddColumn("Parsing report for dirPath:")
            .AddColumn($"{dirPath}");

        foreach ((List<string> pathToFiles, Parser.FileType fileType) in data)
            table.AddRow($"{Enum.GetName(typeof(Parser.FileType), fileType)} files parsed:", $"{pathToFiles.Count}");

        return table;
    }

    public static string GetMainMenuChoice()
        => AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]What would you like to do[/]?")
                .PageSize(5)
                .MoreChoicesText("[grey](Move up and down to reveal more choices)[/]")
                .AddChoices("Extract game data", "Parse extracted game data", "Install High-Res patch",
                    "Uninstall High-Res patch", "Launch Arcanum.exe"));
}