using ArcNET.DataTypes;
using ArcNET.Utilities;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AnsiConsoleExtensions = ArcNET.Utilities.AnsiConsoleExtensions;

namespace ArcNET.Terminal
{
    public static class Parser
    {
        private static int _facWalkRed;
        private static int _mesRed;
        private static int _sectorsRed;

        private enum FileTypes
        {
            FacWalk,
            Mes,
            Sec,
            Pro,
            Art,
            Null,
        }

        //TODO: rework
        private static void ParseAndWriteAllInDir(string directory)
        {
            var files = Directory.EnumerateFiles(
                directory, "*.*", SearchOption.AllDirectories).ToList();
            var facWalkFiles = files.Where(str =>
                new Regex(@"^.*walk\..{1,3}$").IsMatch(str)).ToList();
            var mesFiles = files.Where(str =>
                new Regex(@"^.*\.mes$").IsMatch(str)).ToList();
            var artFiles = files.Where(str =>
                new Regex(@"^.*\.ART$").IsMatch(str)).ToList();
            var secFiles = files.Where(str =>
                new Regex(@"^.*\.sec$").IsMatch(str)).ToList();

            var data = new List<List<string>>()
            {
                files,
                facWalkFiles,
                mesFiles,
                artFiles,
                secFiles
            };

            AnsiConsole.Render(Terminal.DirectoryTable(directory, data));

            var outputFolder = directory + @"\out\";
            foreach (var file in facWalkFiles)
            {
                ParseAndWriteFile(file, FileTypes.FacWalk, outputFolder);
            }

            foreach (var file in mesFiles)
            {
                ParseAndWriteFile(file, FileTypes.Mes, outputFolder);
            }

            foreach (var file in secFiles)
            {
                ParseAndWriteFile(file, FileTypes.Sec, outputFolder);
            }
        }

        private static void ParseAndWriteFile(string fileName, FileTypes fileType, string outputFolder = null)
        {
            AnsiConsoleExtensions.Log($"Parsing file: {fileName} FileType: {fileType}", "info");

            var outputPath = new FileInfo(fileName).Name;
            if (!string.IsNullOrEmpty(outputFolder))
            {
                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                outputPath = outputFolder + outputPath;
            }

            switch (fileType)
            {
                case FileTypes.FacWalk:
                {
                    using var reader = new BinaryReader(new FileStream(fileName, FileMode.Open));
                    var obj = new FacWalkReader(reader).Read();
                    if (obj == null) return;
                    _facWalkRed++;

                    FileWriter.ToJson(outputPath, obj);
                    break;
                }

                case FileTypes.Mes:
                {
                    using var reader = new StreamReader(new FileStream(fileName, FileMode.Open));
                    var obj = new Mes(reader).Parse();
                    if (obj == null) return;
                    _mesRed++;

                    FileWriter.ToJson(outputPath, obj.GetEntriesAsJson());
                    break;
                }

                case FileTypes.Sec:
                {
                    using var reader = new BinaryReader(new FileStream(fileName, FileMode.Open));
                    var obj = new SectorReader(reader).ReadSector();
                    if (obj == null) return;
                    _sectorsRed++;

                    FileWriter.ToJson(outputPath, obj.GetEntriesAsJson());
                    break;
                }

                case FileTypes.Pro:
                    break;

                case FileTypes.Art:
                    break;

                case FileTypes.Null:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null);
            }
        }

        public static void ParseExtractedData()
        {
            AnsiConsoleExtensions.Log("Insert path to file or directory:", "info");
            var response = AnsiConsole.Ask<string>("[green]Input[/]");
            while (string.IsNullOrEmpty(response) || response.Length < 10)
            {
                AnsiConsoleExtensions.Log("Path either empty or incorrect format!", "error");
                AnsiConsoleExtensions.Log("Usage:<fileName|directory>", "error");
                response = AnsiConsole.Ask<string>("[green]Insert path to file or directory[/]:");
            }

            if (Directory.Exists(response))
            {
                try
                {
                    ParseAndWriteAllInDir(response);
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                    throw;
                }
            }
            else
            {
                var fileName = Path.GetFileName(response);
                if (string.IsNullOrEmpty(fileName) || fileName.Length < 10)
                {
                    AnsiConsoleExtensions.Log($"File: {response} does not exists!", "error");
                    throw new Exception("File not found!");
                }

                try
                {
                    var fileTypeToParse = FileTypes.Null;
                    if (fileName.Contains("facwalk."))
                    {
                        fileTypeToParse = FileTypes.FacWalk;
                    }
                    else if (fileName.Contains(".mes"))
                    {
                        fileTypeToParse = FileTypes.Mes;
                    }
                    else if (fileName.Contains(".sec"))
                    {
                        fileTypeToParse = FileTypes.Sec;
                    }
                    else if (fileName.Contains(".ART"))
                    {
                        fileTypeToParse = FileTypes.Art;
                    }

                    ParseAndWriteFile(response, fileTypeToParse);
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                    throw;
                }
            }

            AnsiConsoleExtensions.Log($"Done, Written {_facWalkRed} facades. "
                                      + $"Written {_mesRed} messages."
                                      + $"Written {_sectorsRed} sectors.", "success");
        }
    }
}