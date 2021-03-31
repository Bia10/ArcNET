using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spectre.Console;

namespace ArcNET.Utilities
{
    public static class HighResConfig
    {
        //Todo: config.ini is not standardized, create sections?
        //Arcanum High Resolution Patch Settings
        //Basic:
        private static int Width; // original: 800
        private static int Height; // original: 600
        private static int BitDepth; // original: 16
        private static int DialogFont; // 0 = size 12, 1 = size 14, 2 = size 18
        private static int LogbookFont; // 0 = size 12, 1 = size 14
        private static int MenuPosition; // 0 = top, 1 = center, 2 = bottom
        private static int MainMenuArt; // 0 = black, 1 = fade to black, 2 = wood
        private static int Borders; // 1 = add borders to most UI graphics
        private static int Language; // 0 = English, 1 = German, 2 = French, 3 = Russian
        //Graphics:
        private static int Windowed; // 0 = fullscreen mode, 1 = windowed mode
        private static int Renderer; // 0 = software, 1 = hardware
        private static int DoubleBuffer; // 0 = disabled, 1 = enabled (unless windowed)
        private static int DDrawWrapper; // 1 = install DDrawCompat wrapper
        private static int DxWrapper; // 1 = install DxWrapper's DDrawCompat 
        private static int ShowFPS; // 0 = no change, 1 = always enabled
        //Advanced:
        private static int ScrollFPS; // original: 35, max: 255
        private static int ScrollDist; // original: 10, infinite: 0
        private static int PreloadLimit; // original: 30 tiles, max: 255
        private static int BroadcastLimit; // original: 10 tiles, max 255
        private static int Logos; // 0 = skip Sierra/Troika logos
        private static int Intro; // 0 = skip the main menu intro clip

        public static void Init(string iniPath)
        {
            foreach (var (key, value) in ParseIni(iniPath))
            {
                switch (key)
                {
                    case "Width":
                        Width = int.Parse(value);
                        break;
                    case "Height":
                        Height = int.Parse(value);
                        break;
                    case "BitDepth":
                        BitDepth = int.Parse(value);
                        break;
                    case "DialogFont":
                        DialogFont = int.Parse(value);
                        break;
                    case "LogbookFont":
                        LogbookFont = int.Parse(value);
                        break;
                    case "MenuPosition":
                        MenuPosition = int.Parse(value);
                        break;
                    case "MainMenuArt":
                        MainMenuArt = int.Parse(value);
                        break;
                    case "Borders":
                        Borders = int.Parse(value);
                        break;
                    case "Language":
                        Language = int.Parse(value);
                        break;
                    case "Windowed":
                        Windowed = int.Parse(value);
                        break;
                    case "Renderer":
                        Renderer = int.Parse(value);
                        break;
                    case "DoubleBuffer":
                        DoubleBuffer = int.Parse(value);
                        break;
                    case "DDrawWrapper":
                        DDrawWrapper = int.Parse(value);
                        break;
                    case "DxWrapper":
                        DxWrapper = int.Parse(value);
                        break;
                    case "ShowFPS":
                        ShowFPS = int.Parse(value);
                        break;
                    case "ScrollFPS":
                        ScrollFPS = int.Parse(value);
                        break;
                    case "ScrollDist":
                        ScrollDist = int.Parse(value);
                        break;
                    case "PreloadLimit":
                        PreloadLimit = int.Parse(value);
                        break;
                    case "BroadcastLimit":
                        BroadcastLimit = int.Parse(value);
                        break;
                    case "Logos":
                        Logos = int.Parse(value);
                        break;
                    case "Intro":
                        Intro = int.Parse(value);
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown parameter: {key}");
                }
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> ParseIni(string iniPath)
        {
            if (!File.Exists(iniPath)) 
                throw new FileNotFoundException("Unable to find " + iniPath);

            TextReader iniFile = null;
            var kvpList = new List<KeyValuePair<string, string>>();

            try
            {
                iniFile = new StreamReader(iniPath);
                var strLine = iniFile.ReadLine();

                while (strLine != null)
                {
                    if (strLine != "" && strLine.Substring(0, 2) != "//")
                    {
                        var keyValuePair = strLine.Split(new[] { '=' }, 2);
                        var key = keyValuePair[0];
                        var value = keyValuePair[1];

                        if (value.Contains("//"))
                            value = value.Split(new[] { '/' }, 2).First();

                        var nameValuePair = new KeyValuePair<string, string>(key.Trim(' '), value.Trim(' '));
                        kvpList.Add(nameValuePair);
                    }

                    strLine = iniFile.ReadLine();
                }

                return kvpList;
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                throw;
            }
            finally
            {
                iniFile?.Close();
            }
        }

    }
}