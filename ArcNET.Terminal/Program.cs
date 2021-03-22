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
        private static int _facWalksRed;

        private static void Main()
        {
            var response = AnsiConsole.Ask<string>("[green]Insert path to facwalk file or directory[/]:");
            if (response == string.Empty || response.Length < 10) 
            {
                AnsiConsoleExtensions.Log("Path either empty or incorrect format!", "error");
                AnsiConsoleExtensions.Log("Usage: <facwalk-filename|directory>", "error");
                response = AnsiConsole.Ask<string>("[green]Insert path to facwalk file or directory[/]:");
            }

            if (Directory.Exists(response))
            {
                AnsiConsoleExtensions.Log($"Directory: {response} exists!", "info");
                try
                {
                    DumpAllIn(response);
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
                    DumpFile(response, writer);
                    writer.Flush();
                    writer.Close();
                }
                catch (Exception e)
                {
                    AnsiConsole.WriteException(e);
                    throw;
                }
            }

            AnsiConsoleExtensions.Log($"Done. Written {_facWalksRed} facades.", "debug");
            Console.ReadKey();
        }

        private static void DumpAllIn(string filename)
        {
            var facWalkFiles = Directory.EnumerateFiles(filename, "facwalk.*", SearchOption.AllDirectories).ToList();
            AnsiConsoleExtensions.Log($"FacWalk files found: {facWalkFiles.Count}", "info");
            foreach (var file in facWalkFiles)
            {
                using var writer = new StreamWriter(file + ".json", false, Encoding.UTF8, 8192);
                DumpFile(file, writer);
                writer.Flush();
                writer.Close();
            }
        }

        private static void DumpFile(string filename, TextWriter textWriter)
        {
            _facWalksRed++;

            FacWalk obj;
            AnsiConsoleExtensions.Log($"Parsing file:{filename}", "info");
            using (var reader = new BinaryReader(new FileStream(filename, FileMode.Open)))
            {
                obj = new FacWalkReader(reader).Read();
            }
            if (obj == null) return;

            AnsiConsoleExtensions.Log($"Parsed: {obj}", "success");
            var serializedObj = JsonConvert.SerializeObject(obj);
            textWriter.WriteLine(serializedObj);
        }
    }
}