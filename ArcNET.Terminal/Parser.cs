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
        private static int _textsRed;
        private static int _sectorsRed;
        private static int _prototypesRed;
        private static int _artsRed;

        public enum FileType
        {
            DataArchive,
            FacadeWalk,
            Message,
            Sector,
            Prototype,
            PlayerBackground,
            Mobile,
            Art,
            Jump,
            Script,
            Dialog,
            Terrain,
            MapProperties,
            SoundWav,
            SoundMp3,
            Video,
            Bitmap,
            Text,
            Any
        }

        private static readonly Regex DataArchiveRegex = new(@"^.*\.dat$");
        private static readonly Regex FacadeWalkRegex = new(@"facwalk\..{1,3}$");
        private static readonly Regex MessageRegex = new(@"^.*\.mes$");
        private static readonly Regex SectorRegex = new(@"^.*\.sec$");
        private static readonly Regex PrototypeRegex = new(@"^.*\.pro$");
        private static readonly Regex PlayerRegex = new(@"^.*\.mpc$");
        private static readonly Regex MobileRegex = new(@"^.*\.mob$");
        private static readonly Regex ArtRegex = new(@"^.*\.art$", RegexOptions.IgnoreCase);
        private static readonly Regex JumpRegex = new(@"^.*\.jmp$");
        private static readonly Regex ScriptRegex = new(@"^.*\.scr$");
        private static readonly Regex DialogRegex = new(@"^.*\.dlg$");
        private static readonly Regex TerrainRegex = new(@"^.*\.tdf$");
        private static readonly Regex MapPropertiesRegex = new(@"^.*\.prp$");
        private static readonly Regex SoundWavRegex = new(@"^.*\.wav$", RegexOptions.IgnoreCase);
        private static readonly Regex SoundMp3Regex = new(@"^.*\.mp3$");
        private static readonly Regex VideoRegex = new(@"^.*\.bik$");
        private static readonly Regex BitmapRegex = new(@"^.*\.bmp$");
        private static readonly Regex TextRegex = new(@"^.*\.txt$");

        //TODO: rework, make entire parsing async
        private static async void ParseAndWriteAllInDir(string dirPath)
        {
            var allFiles = Directory.EnumerateFiles(dirPath, "*.*", 
                SearchOption.AllDirectories).ToList();

            var datFiles = allFiles.Where(str => DataArchiveRegex.IsMatch(str)).ToList();
            var facFiles = allFiles.Where(str => FacadeWalkRegex.IsMatch(str)).ToList();
            var mesFiles = allFiles.Where(str => MessageRegex.IsMatch(str)).ToList();
            var secFiles = allFiles.Where(str => SectorRegex.IsMatch(str)).ToList();
            var proFiles = allFiles.Where(str => PrototypeRegex.IsMatch(str)).ToList();
            var playerFiles = allFiles.Where(str => PlayerRegex.IsMatch(str)).ToList();
            var mobFiles = allFiles.Where(str => MobileRegex.IsMatch(str)).ToList();
            var artFiles = allFiles.Where(str => ArtRegex.IsMatch(str)).ToList();
            var jumpFiles = allFiles.Where(str => JumpRegex.IsMatch(str)).ToList();
            var scriptFiles = allFiles.Where(str => ScriptRegex.IsMatch(str)).ToList();
            var dialogFiles = allFiles.Where(str => DialogRegex.IsMatch(str)).ToList();
            var terrainFiles = allFiles.Where(str => TerrainRegex.IsMatch(str)).ToList();
            var mapPropertiesFiles = allFiles.Where(str => MapPropertiesRegex.IsMatch(str)).ToList();
            var soundWavFiles = allFiles.Where(str => SoundWavRegex.IsMatch(str)).ToList();
            var soundMp3Files = allFiles.Where(str => SoundMp3Regex.IsMatch(str)).ToList();
            var videoFiles = allFiles.Where(str => VideoRegex.IsMatch(str)).ToList();
            var bitmapFiles = allFiles.Where(str => BitmapRegex.IsMatch(str)).ToList();
            var textFiles = allFiles.Where(str => TextRegex.IsMatch(str)).ToList();

            var otherFiles = allFiles.Where(str => 
                !DataArchiveRegex.IsMatch(str) && !FacadeWalkRegex.IsMatch(str) && !MessageRegex.IsMatch(str) 
                && !SectorRegex.IsMatch(str) && !PrototypeRegex.IsMatch(str) && !PlayerRegex.IsMatch(str) 
                && !MobileRegex.IsMatch(str) && !ArtRegex.IsMatch(str) && !JumpRegex.IsMatch(str) 
                && !ScriptRegex.IsMatch(str) && !DialogRegex.IsMatch(str) && !TerrainRegex.IsMatch(str)
                && !MapPropertiesRegex.IsMatch(str) && !SoundWavRegex.IsMatch(str) && !SoundMp3Regex.IsMatch(str) 
                && !VideoRegex.IsMatch(str) && !BitmapRegex.IsMatch(str) && !TextRegex.IsMatch(str)).ToList();

            var data = new List<Tuple<List<string>, FileType>>
            {
                new(allFiles, FileType.Any),
                new(datFiles, FileType.DataArchive),
                new(facFiles, FileType.FacadeWalk),
                new(mesFiles, FileType.Message),
                new(secFiles, FileType.Sector),
                new(proFiles, FileType.Prototype),
                new(playerFiles, FileType.PlayerBackground),
                new(mobFiles, FileType.Mobile),
                new(artFiles, FileType.Art),
                new(jumpFiles, FileType.Jump),
                new(scriptFiles, FileType.Script),
                new(dialogFiles, FileType.Dialog),
                new(terrainFiles, FileType.Terrain),
                new(mapPropertiesFiles, FileType.MapProperties),
                new(soundWavFiles, FileType.SoundWav),
                new(soundMp3Files, FileType.SoundMp3),
                new(videoFiles, FileType.Video),
                new(bitmapFiles, FileType.Bitmap),
                new(textFiles, FileType.Text),
            };

            AnsiConsole.Render(Terminal.DirectoryTable(dirPath, data));

            //Removes potential task which are done already
            //TODO: remains unclear why a finished task is not at 100%, but rather demands percentage calculation.
            var toRemove = new HashSet<Tuple<List<string>, FileType>>();
            foreach (var tupleList in data.Where(tuple => tuple.Item1.Count == 0 || tuple.Item2 != FileType.Text))
                toRemove.Add(tupleList);
            data.RemoveAll(toRemove.Contains);

            var tasks = new (string name, List<string> data)[data.Count];
            for (var i = 0; i < data.Count; i++)
                tasks[i] = ($"[green]Parsing {Enum.GetName(typeof(FileType), data[i].Item2)}" 
                            + " files :[/]", data[i].Item1);

            await AnsiConsole.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(), 
                new PercentageColumn(), 
                new RemainingTimeColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                await Task.WhenAll(tasks.Select(async task =>
                {
                    var (name, files) = task;
                    var currentTask = ctx.AddTask(name, new ProgressTaskSettings
                    {
                        MaxValue = files.Count,
                        AutoStart = false,
                    });

                    foreach (var (fileList, fileType) in data)
                    {
                        foreach (var file in fileList)
                        {
                            await ParseAndWriteFile(file, fileType, currentTask, dirPath + @"\out\");
                        }
                    }
                }));
            });

            AnsiConsole.Render(Terminal.ReportTable(dirPath, data));
        }
        
        //Todo: make async, synchronous, will likely need Async BinaryRead/Write
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
                    /*using var reader = new BinaryReader(new FileStream(fileName, FileMode.Open));
                    var obj = new FacadeWalkReader(reader).Read();
                    if (obj == null) return;
                    _facadeWalksRed++;
                    task.Increment(_facadeWalksRed);

                    FileWriter.ToJson(outputPath, obj);*/
                    break;
                }

                case FileType.Text:
                {
                    using var reader = new StreamReader(new FileStream(fileName, FileMode.Open));
                    AnsiConsoleExtensions.Log($"Parsing text file:|{fileName}|", "warn");

                    switch (new FileInfo(fileName).Name)
                    {
                        case "monster.txt":
                        {
                            var mobReader = new TextDataReader(reader);
                            mobReader.Parse("Monster");
                            var mobCount = mobReader._monsters.Count;
                            if (mobCount == 0) return;

                            _textsRed++;
                            task.Increment(_textsRed);
                            AnsiConsoleExtensions.Log($"Monsters parsed: |{mobCount}|", "warn");
                            break;
                        }
                        case "npc.txt":
                        {
                            var npcs = new TextDataReader(reader);
                            npcs.Parse("NPC");
                            var npcCount = npcs._monsters.Count;
                            if (npcCount == 0) return;

                            _textsRed++;
                            task.Increment(_textsRed);
                            AnsiConsoleExtensions.Log($"NPCs parsed: |{npcCount}|", "warn");
                            break;
                        }
                        case "unique.txt":
                        {
                            var uniques = new TextDataReader(reader);
                            uniques.Parse("NPC");
                            var uniqueCount = uniques._monsters.Count;
                            if (uniqueCount == 0) return;

                            _textsRed++;
                            task.Increment(_textsRed);
                            AnsiConsoleExtensions.Log($"Uniques parsed: |{uniqueCount}|", "warn");
                            break;
                        }

                        default:
                            throw new InvalidOperationException(fileName, null);
                        }
                    break;
                }

                case FileType.Message:
                {
                    using var reader = new StreamReader(new FileStream(fileName, FileMode.Open));
                    AnsiConsoleExtensions.Log($"Parsing mes file:|{fileName}|", "warn");
                    var obj = new MessageReader(reader).Parse();
                    if (obj == null) return;
                    _messagesRed++;
                    task.Increment(_messagesRed);

                    FileWriter.ToJson(outputPath, obj.GetEntriesAsJson());
                    break;
                }

                case FileType.Sector:
                {
                    /*using var reader = new BinaryReader(new FileStream(fileName, FileMode.Open));
                    var obj = new SectorReader(reader).ReadSector();
                    if (obj == null) return;
                    _sectorsRed++;
                    task.Increment(_sectorsRed);

                    FileWriter.ToJson(outputPath, obj.GetEntriesAsJson());*/
                    break;
                }

                case FileType.Art:
                    /*_artsRed++;
                    task.Increment(_artsRed);*/
                    break;
                case FileType.Prototype:
                    /*_prototypesRed++;
                    task.Increment(_prototypesRed);*/
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
                case FileType.Terrain:
                    break;
                case FileType.MapProperties:
                    break;
                case FileType.DataArchive:
                    break;
                case FileType.PlayerBackground:
                    break;
                case FileType.SoundWav:
                    break;
                case FileType.SoundMp3:
                    break;
                case FileType.Video:
                    break;
                case FileType.Bitmap:
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
        }
    }
}