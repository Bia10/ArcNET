﻿using ArcNET.DataTypes;
using Newtonsoft.Json;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AnsiConsoleExtensions = ArcNET.Utilities.AnsiConsoleExtensions;

namespace ArcNET.Terminal
{
    public static class Parser
    {
        private static int _facWalkRed;
        private static int _mesRed;
        private static int _sectorsRed;

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
                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                using var writer = new StreamWriter(outputFolder + new FileInfo(file).Name + ".json", false, Encoding.UTF8, 8192);
                using var reader = new BinaryReader(new FileStream(file, FileMode.Open));
                var obj = new FacWalkReader(reader).Read();
                if (obj == null) return;
                _facWalkRed++;
                var serializedObj = JsonConvert.SerializeObject(obj, Formatting.Indented);

                writer.WriteLine(serializedObj);
                writer.Flush();
                writer.Close();
            }

            foreach (var file in mesFiles)
            {
                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                using var writer = new StreamWriter(outputFolder + new FileInfo(file).Name + ".json", false, Encoding.UTF8, 8192);
                using var reader = new StreamReader(new FileStream(file, FileMode.Open));
                var obj = new Mes(reader).Parse();
                if (obj == null) return;
                _mesRed++;
                var serializedObj = obj.GetEntriesAsJson();

                writer.WriteLine(serializedObj);
                writer.Flush();
                writer.Close();
            }

            foreach (var file in secFiles)
            {
                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                using var writer = new StreamWriter(outputFolder + new FileInfo(file).Name + ".json", false, Encoding.UTF8, 8192);
                using var reader = new BinaryReader(new FileStream(file, FileMode.Open));
                var ojb = new SectorReader(reader).ReadSector();
                if (ojb == null) return;
                _sectorsRed++;
                var serializedObj = ojb.GetEntriesAsJson();

                writer.WriteLine(serializedObj);
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
                        textWriter.WriteLine(obj2.GetEntriesAsJson());
                        break;
                    }
            }
        }

        public static void ParseExtractedData()
        {
            AnsiConsoleExtensions.Log("Insert path to file or directory:", "info");
            var response = AnsiConsole.Ask<string>("[green]Input[/]");
            while (response == string.Empty || response.Length < 10)
            {
                AnsiConsoleExtensions.Log("Path either empty or incorrect format!", "error");
                AnsiConsoleExtensions.Log("Usage:<filename|directory>", "error");
                response = AnsiConsole.Ask<string>("[green]Insert path to file or directory[/]:");
            }

            if (Directory.Exists(response))
            {
                try
                {
                    ParseAndWriteAllInDir(response);
                }
                catch (Exception e)
                {
                    AnsiConsole.WriteException(e);
                    throw;
                }
            }
            else
            {
                var fileName = Path.GetFileName(response);
                if (fileName == string.Empty || fileName.Length < 10)
                {
                    AnsiConsoleExtensions.Log($"File: {response} does not exists!", "error");
                    throw new Exception("File not found!");
                }

                try
                {
                    var fileTypeToParse = "";
                    if (fileName.Contains(".mes"))
                    {
                        fileTypeToParse = "mes";
                    }
                    else if (fileName.Contains("facwalk."))
                    {
                        fileTypeToParse = "facwalk";
                    }
                    else if (fileName.Contains(".ART"))
                    {
                        fileTypeToParse = "ART";
                    }

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
            AnsiConsoleExtensions.Log($"Done, Written {_facWalkRed} facades. "
                                      + $"Written {_mesRed} messages."
                                      + $"Written {_sectorsRed} sectors.", "success");
        }
    }
}