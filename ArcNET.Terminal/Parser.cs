using ArcNET.DataTypes;
using ArcNET.Utilities;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AnsiConsoleExtensions = ArcNET.Utilities.AnsiConsoleExtensions;

namespace ArcNET.Terminal
{
    public static class Parser
    {
        private static int _facadeWalksRed;
        private static int _messagesRed;
        private static int _artsRed;
        private static int _sectorsRed;

        public enum FileType
        {
            FacadeWalk,
            Message,
            Sector,
            Prototype,
            Mobile,
            Art,
            Jump,
            Script,
            Dialog,
            TownMapInfo,
            MapProperties,
            Any,
        }

        private static readonly Regex FacadeWalkRegex = new(@"^.*walk\..{1,3}$");
        private static readonly Regex MessageRegex = new(@"^.*\.mes$");
        private static readonly Regex SectorRegex = new(@"^.*\.sec$");
        private static readonly Regex ArtRegex = new(@"^.*\.ART$");

        //TODO: rework, make entire parsing async
        private static async void ParseAndWriteAllInDir(string dirPath)
        {
            var allFiles = Directory.EnumerateFiles(dirPath, "*.*", 
                SearchOption.AllDirectories).ToList();

            var facWalkFiles = allFiles.Where(str => FacadeWalkRegex.IsMatch(str)).ToList();
            var mesFiles = allFiles.Where(str => MessageRegex.IsMatch(str)).ToList();
            var secFiles = allFiles.Where(str => SectorRegex.IsMatch(str)).ToList();
            var artFiles = allFiles.Where(str => ArtRegex.IsMatch(str)).ToList();

            var data = new List<Tuple<List<string>, FileType>>
            {
                new(allFiles, FileType.Any),
                new(facWalkFiles, FileType.FacadeWalk),
                new(mesFiles, FileType.Message),
                new(artFiles, FileType.Art),
                new(secFiles, FileType.Sector),
            };

            AnsiConsole.Render(Terminal.DirectoryTable(dirPath, data));

            //Removes potential task which are done already
            //TODO: remains unclear why a finished task is not at 100%, but rather demands percentage calculation.
            var toRemove = new HashSet<Tuple<List<string>, FileType>>();
            foreach (var tupleList in data.Where(tuple => tuple.Item1.Count == 0 || tuple.Item2 == FileType.Any))
                toRemove.Add(tupleList);
            data.RemoveAll(toRemove.Contains);

            var tasks = new (string name, List<string> data)[data.Count];
            for (var i = 0; i < data.Count; i++)
                tasks[i] = ($"[green]Parsing {Enum.GetName(typeof(FileType), data[i].Item2)}"
                            + " files :[/]", data[i].Item1);

            // Progress
            await AnsiConsole.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(), 
                new PercentageColumn(), 
                new RemainingTimeColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                await Task.WhenAll(tasks.Select(async item =>
                {
                    var (name, files) = item;
                    var task = ctx.AddTask(name, new ProgressTaskSettings
                    {
                        MaxValue = files.Count,
                        AutoStart = false,
                    });

                    foreach (var (fileList, fileType) in data)
                    {
                        foreach (var file in fileList)
                        {
                            await ParseAndWriteFile(file, fileType, task, dirPath + @"\out\");
                        }
                    }
                }));
            });
        }
        
        //Todo: make async, synchronous...
        private static async Task ParseAndWriteFile(string fileName, FileType fileType, ProgressTask task, string outputFolder = null)
        {
            //AnsiConsoleExtensions.Log($"Parsing file: {fileName} FileType: {fileType}", "info");

            var outputPath = new FileInfo(fileName).Name;
            if (!string.IsNullOrEmpty(outputFolder))
            {
                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                outputPath = outputFolder + outputPath;
            }

            task.StartTask();

            switch (fileType)
            {
                case FileType.FacadeWalk:
                {
                    using var reader = new BinaryReader(new FileStream(fileName, FileMode.Open));
                    var obj = new FacadeWalkReader(reader).Read();
                    if (obj == null) return;
                    _facadeWalksRed++;
                    task.Increment(_facadeWalksRed);

                    FileWriter.ToJson(outputPath, obj);
                    break;
                }

                case FileType.Message:
                {
                    using var reader = new StreamReader(new FileStream(fileName, FileMode.Open));
                    var obj = new MessageReader(reader).Parse();
                    if (obj == null) return;
                    _messagesRed++;
                    task.Increment(_messagesRed);

                    FileWriter.ToJson(outputPath, obj.GetEntriesAsJson());
                    break;
                }

                case FileType.Sector:
                {
                    using var reader = new BinaryReader(new FileStream(fileName, FileMode.Open));
                    var obj = new SectorReader(reader).ReadSector();
                    if (obj == null) return;
                    _sectorsRed++;
                    task.Increment(_sectorsRed);

                    FileWriter.ToJson(outputPath, obj.GetEntriesAsJson());
                    break;
                }

                case FileType.Art:
                    _artsRed++;
                    task.Increment(_artsRed);
                    break;
                case FileType.Prototype:
                    break;
                case FileType.Any:
                    break;
                case FileType.Mobile:
                    break;
                case FileType.Jump:
                    break;
                case FileType.Script:
                    break;
                case FileType.Dialog:
                    break;
                case FileType.TownMapInfo:
                    break;
                case FileType.MapProperties:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null);
            }
        }

        public static void ParseExtractedData()
        {
            AnsiConsoleExtensions.Log("Insert path to file or dirPath:", "info");
            var inputPath = AnsiConsole.Ask<string>("[green]Input[/]");
            while (string.IsNullOrEmpty(inputPath) || inputPath.Length < 10)
            {
                AnsiConsoleExtensions.Log("Path either empty or incorrect format!", "error");
                AnsiConsoleExtensions.Log("Usage:<fileName|dirPath>", "error");
                inputPath = AnsiConsole.Ask<string>("[green]Insert path to file or dirPath[/]:");
            }

            if (Directory.Exists(inputPath))
            {
                try
                {
                    ParseAndWriteAllInDir(inputPath);
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                    throw;
                }
            }
            else
            {
                var fileName = Path.GetFileName(inputPath);
                if (string.IsNullOrEmpty(fileName) || fileName.Length < 10)
                {
                    AnsiConsoleExtensions.Log($"File: {inputPath} does not exists!", "error");
                    throw new Exception("File not found!");
                }

                try
                {
                    var fileTypeToParse = FileType.Any;
                    if (FacadeWalkRegex.IsMatch(fileName))
                    {
                        fileTypeToParse = FileType.FacadeWalk;
                    }
                    else if (MessageRegex.IsMatch(fileName))
                    {
                        fileTypeToParse = FileType.Message;
                    }
                    else if (SectorRegex.IsMatch(fileName))
                    {
                        fileTypeToParse = FileType.Sector;
                    }
                    else if (ArtRegex.IsMatch(fileName))
                    {
                        fileTypeToParse = FileType.Art;
                    }

                    //ParseAndWriteFile(inputPath, fileTypeToParse);
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                    throw;
                }
            }
            //TODO: report
        }
    }
}