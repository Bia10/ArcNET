﻿using ArcNET.DataTypes;
using ArcNET.DataTypes.GameObjects;
using ArcNET.DataTypes.GameObjects.Classes;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ArcNET.DataTypes.Generators;
using Utils.Console;

namespace ArcNET.Terminal;

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
    private static void ParseAndWriteAllInDir(string dirPath)
    {
        List<Tuple<List<string>, FileType>> data = LoadLocalData(dirPath);

        AnsiConsole.Write(Terminal.DirectoryTable(dirPath, data));

        GameObjectManager.Init();

        //Removes potential task which are done already
        //TODO: remains unclear why a finished task is not at 100%, but rather demands percentage calculation.
        var toRemove = new HashSet<Tuple<List<string>, FileType>>();
        foreach (Tuple<List<string>, FileType> tupleList in data.Where(tuple => tuple.Item1.Count == 0 || (tuple.Item2 != FileType.Text && tuple.Item2 != FileType.Message)))
            toRemove.Add(tupleList);
        data.RemoveAll(toRemove.Contains);

        var tasks = new (string name, List<string> data, FileType fileType)[data.Count];
        for (var i = 0; i < data.Count; i++)
            tasks[i] = ($"[green]Parsing {Enum.GetName(typeof(FileType), data[i].Item2)}"
                        + " files :[/]", data[i].Item1, data[i].Item2);

        AnsiConsole.Progress().Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn())
            .Start(ctx =>
            {
                foreach ((string name, List<string> files, FileType fileType) in tasks)
                {
                    ProgressTask currentTask = ctx.AddTask(name, new ProgressTaskSettings
                    {
                        MaxValue = files.Count,
                        AutoStart = false,
                    });

                    foreach (string file in files)
                        ParseAndWriteFile(file, fileType, currentTask, dirPath + @"\out\");
                }
            });

        //testing
        var mobsWithDrops = GameObjectManager.Monsters.Where(mob => mob.InventorySource > 0).ToList();
        ConsoleExtensions.Log($"mobsWithDrops: |{mobsWithDrops.Count}|", "warn");
        foreach (Monster mob in mobsWithDrops)
        {
            IEnumerable<Tuple<string, double>> namedDropTable = InventorySource.NamedDropTableFromId(mob.InventorySource);
            ConsoleExtensions.Log($"mobName: |{mob.Description.Item2}| invSrcId: |{mob.InventorySource}|", "warn");
            foreach ((string name, double chance) in namedDropTable)
                ConsoleExtensions.Log($"itemName: |{name}| chance:|{chance}|", "warn");

            string mobText = Wikia.GetEntityInfobox(mob);
            ConsoleExtensions.Log($"mobText: |{mobText}|", "warn");
        }

        AnsiConsole.Write(Terminal.ReportTable(dirPath, data));
    }

    //Todo: make async, will likely need Async BinaryRead/Write
    private static void ParseAndWriteFile(string fileName, FileType fileType, ProgressTask task, string outputFolder = null)
    {
        //ConsoleExtensions.Log($"Parsing file: {fileName} FileType: {fileType}", "info");

        string outputPath = new FileInfo(fileName).Name;
        if (!string.IsNullOrEmpty(outputFolder))
        {
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            outputPath = outputFolder + outputPath;
        }

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
                task.StartTask();
                using var reader = new StreamReader(new FileStream(fileName, FileMode.Open));
                ConsoleExtensions.Log($"Parsing text file:|{fileName}|", "warn");

                switch (new FileInfo(fileName).Name)
                {
                    case "monster.txt":
                    {
                        var mobReader = new TextDataReader(reader);
                        mobReader.Parse("Monster");

                        int mobCount = GameObjectManager.Monsters.Count;
                        if (mobCount == 0) return;

                        _textsRed++;
                        task.Increment(+1);
                        ConsoleExtensions.Log($"Monsters parsed: |{mobCount}|", "warn");
                        break;
                    }
                    case "npc.txt":
                    {
                        var npcReader = new TextDataReader(reader);
                        npcReader.Parse("NPC");

                        int npcCount = GameObjectManager.NPCs.Count;
                        if (npcCount == 0) return;

                        _textsRed++;
                        task.Increment(+1);
                        ConsoleExtensions.Log($"NPCs parsed: |{npcCount}|", "warn");
                        break;
                    }
                    case "unique.txt":
                    {
                        var uniqueReader = new TextDataReader(reader);
                        uniqueReader.Parse("Unique");

                        int uniqueCount = GameObjectManager.Uniques.Count;
                        if (uniqueCount == 0) return;

                        _textsRed++;
                        task.Increment(+1);
                        ConsoleExtensions.Log($"Uniques parsed: |{uniqueCount}|", "warn");
                        break;
                    }
                    default:
                        throw new InvalidOperationException(fileName, null);
                }

                break;
            }
            case FileType.Message:
            {
                task.StartTask();
                using var reader = new StreamReader(new FileStream(fileName, FileMode.Open));
                ConsoleExtensions.Log($"Parsing mes file:|{fileName}|", "warn");

                switch (new FileInfo(fileName).Name)
                {
                    case "InvenSource.mes":
                    {
                        List<string> textData = new MessageReader(reader).Parse("InvenSource.mes");
                        if (textData == null || textData.Count == 0) return;

                        InventorySource.InitFromText(textData);
                        _messagesRed++;
                        task.Increment(+1);

                        ConsoleExtensions.Log($"Loaded invSources: |{InventorySource.LoadedInventorySources.Count}|", "warn");
                        break;
                    }
                    case "InvenSourceBuy.mes":
                    {
                        List<string> textData = new MessageReader(reader).Parse("InvenSourceBuy.mes");
                        if (textData == null || textData.Count == 0) return;

                        InventorySourceBuy.InitFromText(textData);
                        _messagesRed++;
                        task.Increment(+1);

                        ConsoleExtensions.Log($"Loaded BuyInvSources: |{InventorySourceBuy.LoadedInventoryBuySources.Count}|", "warn");
                        break;
                    }
                    case "xp_level.mes":
                    {
                        List<string> textData = new MessageReader(reader).Parse("xp_level.mes");
                        if (textData == null || textData.Count == 0) return;

                        XpLevels.InitFromText(textData);
                        _messagesRed++;
                        task.Increment(+1);

                        ConsoleExtensions.Log($"Loaded XpLevels: |{XpLevels.LoadedXpLevels.Entries.Count}|", "warn");
                        break;
                    }
                    case "xp_critter.mes":
                    {
                        List<string> textData = new MessageReader(reader).Parse("xp_level.mes");
                        if (textData == null || textData.Count == 0) return;

                        CritterXpLevels.InitFromText(textData);
                        _messagesRed++;
                        task.Increment(+1);

                        ConsoleExtensions.Log($"Loaded XpCritterLevels: |{CritterXpLevels.LoadedCritterXpLevels.Entries.Count}|", "warn");
                        break;
                    }
                    case "xp_quest.mes":
                    {
                        List<string> textData = new MessageReader(reader).Parse("xp_quest.mes");
                        if (textData == null || textData.Count == 0) return;

                        QuestXpLevels.InitFromText(textData);
                        _messagesRed++;
                        task.Increment(+1);

                        ConsoleExtensions.Log($"Loaded XpQuestLevels: |{QuestXpLevels.LoadedXpQuestLevels.Entries.Count}|", "warn");
                        break;
                    }
                    case "backgrnd.mes":
                    {
                        List<string> textData = new MessageReader(reader).Parse("backgrnd.mes");
                        if (textData == null || textData.Count == 0) return;

                        Background.InitFromText(textData);
                        _messagesRed++;
                        task.Increment(+1);

                        ConsoleExtensions.Log($"Loaded Backgrounds: |{Background.LoadedBackgrounds.Count}|", "warn");
                        break;
                    }
                    case "faction.mes":
                    {
                        List<string> textData = new MessageReader(reader).Parse("faction.mes");
                        if (textData == null || textData.Count == 0) return;

                        Faction.InitFromText(textData);
                        _messagesRed++;
                        task.Increment(+1);

                        ConsoleExtensions.Log($"Loaded Factions: |{Faction.LoadedFactions.Entries.Count}|", "warn");
                        break;
                    }
                    case "gamelevel.mes":
                    {
                        List<string> textData = new MessageReader(reader).Parse("gamelevel.mes");
                        if (textData == null || textData.Count == 0) return;

                        AutoLevelSchemes.InitFromText(textData);
                        _messagesRed++;
                        task.Increment(+1);

                        ConsoleExtensions.Log($"Loaded Auto Level Schemes: |{AutoLevelSchemes.LoadedAutoLevelSchemes.Entries.Count}|", "warn");
                        break;
                    }
                    case "description.mes":
                    {
                        List<string> textData = new MessageReader(reader).Parse("description.mes");
                        if (textData == null || textData.Count == 0) return;

                        Descriptions.InitFromText(textData);
                        _messagesRed++;
                        task.Increment(+1);

                        ConsoleExtensions.Log($"Loaded description: |{Descriptions.LoadedDescriptions.Entries.Count}|", "warn");
                        break;
                    }

                    default:
                        _messagesRed++;
                        task.Increment(+1);
                        break;
                    //throw new InvalidOperationException(fileName, null);
                }

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
        ConsoleExtensions.Log("Insert path to file or dirPath:", "info");
        string inputPath = AnsiConsole.Ask<string>("[green]Input[/]");
        while (string.IsNullOrEmpty(inputPath) || inputPath.Length < 10)
        {
            ConsoleExtensions.Log("Path either empty or incorrect format!", "error");
            ConsoleExtensions.Log("Usage:<fileName|dirPath>", "error");
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
            string fileName = Path.GetFileName(inputPath);
            if (string.IsNullOrEmpty(fileName) || fileName.Length < 10)
            {
                ConsoleExtensions.Log($"File: {inputPath} does not exists!", "error");
                throw new Exception("File not found!");
            }

            try
            {
                FileType fileTypeToParse = FileType.Any;
                if (FacadeWalkRegex.IsMatch(fileName))
                    fileTypeToParse = FileType.FacadeWalk;
                else if (MessageRegex.IsMatch(fileName))
                    fileTypeToParse = FileType.Message;
                else if (SectorRegex.IsMatch(fileName))
                    fileTypeToParse = FileType.Sector;
                else if (ArtRegex.IsMatch(fileName))
                    fileTypeToParse = FileType.Art;

                //ParseAndWriteFile(inputPath, fileTypeToParse);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                throw;
            }
        }
    }

    public static List<Tuple<List<string>, FileType>> LoadLocalData(string dirPath)
    {
        var allFiles = Directory.EnumerateFiles(dirPath, "*.*", SearchOption.AllDirectories).ToList();

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

        return new List<Tuple<List<string>, FileType>>
        {
            new(allFiles, FileType.Any),
            new(datFiles, FileType.DataArchive),
            new(textFiles, FileType.Text),
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
            new(facFiles, FileType.FacadeWalk),
        };
    }
}