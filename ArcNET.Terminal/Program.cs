using ArcNET.DataTypes;
using Newtonsoft.Json;
using Spectre.Console;
using System;
using System.IO;
using System.Linq;
using System.Text;
using AnsiConsoleExtensions = ArcNET.Utilities.AnsiConsoleExtensions;

namespace ArcNET.Terminal
{
    internal static class Program
    {
        private static int _facWalkRed;
        private static int _mesRed;

        private static string GetMainMenuChoice()
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]What would you like to do[/]?")
                    .PageSize(5)
                    .MoreChoicesText("[grey](Move up and down to reveal more choices)[/]")
                    .AddChoices("Extract game data", "Parse extracted game data", "Install High-Res patch",
                        "Uninstall High-Res patch", "Reinstall High-Res Patch", "Launch Arcanum.exe"));
        }

        private static string GetParsingMenuChoice()
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]What fileType would you like to parse[/]?")
                    .PageSize(4)
                    .MoreChoicesText("[grey](Move up and down to reveal more choices)[/]")
                    .AddChoices("facwalk", "mes", "none"));
        }

        private static void ParseAndWriteAllInDir(string filename, string fileType)
        {
            var searchPattern = fileType switch
            {
                "facwalk" => fileType + ".*",
                "mes" => "*." + fileType,
                _ => string.Empty
            };

            var filesToParse = Directory.EnumerateFiles(filename, searchPattern, SearchOption.AllDirectories).ToList();
            AnsiConsoleExtensions.Log($"{filesToParse.Count} files of fileType: {fileType} found!", "info");
            foreach (var file in filesToParse)
            {
                using var writer = new StreamWriter(file + ".json", false, Encoding.UTF8, 8192);
                ParseAndWriteFile(file, fileType, writer);
                writer.Flush();
                writer.Close();
            }
        }

        private static void ParseAndWriteFile(string filename, string fileType, TextWriter textWriter)
        {
            AnsiConsoleExtensions.Log($"Parsing file: {filename} FileType: {fileType}", "info");
            switch (fileType)
            {
                case "facwalk":
                {
                    _facWalkRed++;
                    FacWalk obj;
                    using (var reader = new BinaryReader(new FileStream(filename, FileMode.Open)))
                    {
                        obj = new FacWalkReader(reader).Read();
                    }
                    if (obj == null) return;
                    AnsiConsoleExtensions.Log($"Parsed: {obj}", "success");
                    var serializedObj = JsonConvert.SerializeObject(obj, Formatting.Indented);
                    AnsiConsoleExtensions.Log($"Serialized: {obj} into {serializedObj}", "success");
                    textWriter.WriteLine(serializedObj);
                    break;
                }
                case "mes":
                {
                    _mesRed++;
                    Mes obj2;
                    using (var reader2 = new StreamReader(new FileStream(filename, FileMode.Open)))
                    {
                        obj2 = new Mes(reader2).Parse();
                    }
                    if (obj2 == null) return;
                    AnsiConsoleExtensions.Log($"Parsed: {obj2}", "success");
                    AnsiConsoleExtensions.Log($"GetEntryCount: {obj2.GetEntryCount()}", "success");
                    Console.WriteLine(obj2.GetEntriesAsJson()); //fixed in next preview
                    textWriter.WriteLine(obj2.GetEntriesAsJson());
                    break;
                }
            }
        }

        private static void Main()
        {
            AnsiConsole.Render(new FigletText("ArcNET v0.0.1")
                .LeftAligned()
                .Color(Color.Green));

            var choice = GetMainMenuChoice();
            while (choice != "Parse extracted game data")
            {
                AnsiConsoleExtensions.Log($"Choice: {choice} is currently unsupported!", "warn");
                choice = GetMainMenuChoice();
            }
            AnsiConsoleExtensions.Log($"Selected choice: [blue]{choice}[/]", "info");           

            var fileTypeToParse = GetParsingMenuChoice();
            AnsiConsoleExtensions.Log($"Selected fileType: [blue]{fileTypeToParse}[/]", "info");

            AnsiConsoleExtensions.Log("Insert path to file or directory:", "info");
            var response = AnsiConsole.Ask<string>("[green]Input[/]");
            if (response == string.Empty || response.Length < 10)
            {
                AnsiConsoleExtensions.Log("Path either empty or incorrect format!", "error");
                AnsiConsoleExtensions.Log("Usage:<filename|directory>", "error");
                response = AnsiConsole.Ask<string>("[green]Insert path to file or directory[/]:");
            }

            if (Directory.Exists(response))
            {
                AnsiConsoleExtensions.Log($"Directory: {response} exists!", "info");
                try
                {
                    ParseAndWriteAllInDir(response, fileTypeToParse);
                }
                catch (Exception e)
                {
                    AnsiConsole.WriteException(e);
                    throw;
                }
            }
            else
            {
                AnsiConsoleExtensions.Log($"Directory: {response} does not exists!", "warn");
                var fileName = Path.GetFileName(response);
                if (fileName == string.Empty || fileName.Length < 10)
                {
                    AnsiConsoleExtensions.Log($"File: {response} does not exists!", "error");
                    throw new Exception("File not found!");
                }

                try
                {
                    using var writer = new StreamWriter(fileName + ".json", false, Encoding.UTF8, 8192);
                    ParseAndWriteFile(response, fileTypeToParse, writer);
                    writer.Flush();
                    writer.Close();
                }
                catch (Exception e)
                {
                    AnsiConsole.WriteException(e);
                    throw;
                }
            }

            AnsiConsoleExtensions.Log($"Done, Written {_facWalkRed} facades."
                                      + $"Written {_mesRed} messages.", "debug");
        }
    }
}