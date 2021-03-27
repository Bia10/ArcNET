using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spectre.Console;

namespace ArcNET.Utilities
{
    public class HighResConfig
    {
        //Todo: config.ini is not standardized, create sections?
        //Arcanum High Resolution Patch Settings
        //Basic:
        public int Width; // original: 800
        public int Height; // original: 600
        public int DialogFont; // 0 = size 12, 1 = size 14, 2 = size 18
        public int LogbookFont; // 0 = size 12, 1 = size 14
        public int MenuPosition; // 0 = top, 1 = center, 2 = bottom
        public int MainMenuArt; // 0 = black, 1 = fade to black, 2 = wood
        public int Borders; // 1 = add borders to most UI graphics
        public int Language; // 0 = English, 1 = German, 2 = French, 3 = Russian
        //Graphics:
        public int Windowed; // 0 = fullscreen mode, 1 = windowed mode
        public int Renderer; // 0 = software, 1 = hardware
        public int DoubleBuffer; // 0 = disabled, 1 = enabled (unless windowed)
        public int DDrawWrapper; // 1 = install DDrawCompat wrapper
        public int ShowFPS; // 0 = no change, 1 = always enabled
        //Advanced:
        public int ScrollFPS; // original: 35, max: 255
        public int ScrollDist; // original: 10, infinite: 0
        public int PreloadLimit; // original: 30 tiles, max: 255
        public int BroadcastLimit; // original: 10 tiles, max 255
        public int Logos; // 0 = skip Sierra/Troika logos
        public int Intro; // 0 = skip the main menu intro clip

        public HighResConfig(string iniPath)
        {
            var data = ParseIni(iniPath);
            var width = data.First(kvp => kvp.Item1.Equals("Width"));
            Width = int.Parse(width.Item2);

            //Width = 2560;
             //Height = 1600;
             //DialogFont = 1;
             //LogbookFont = 1;
             //MenuPosition = 1;
             //MainMenuArt = 1;
             //Borders = 1;
             //Language = 0;
             //Windowed = 0;
             //Renderer = 0;
             //DoubleBuffer = 0;
             //DDrawWrapper = 1;
             //ShowFPS = 1;
             //ScrollFPS = 60;
             //ScrollDist = 30;
             //PreloadLimit = 60;
             //BroadcastLimit = 20;
             //Logos = 0;
             //Intro = 0;
        }

        private static IEnumerable<Tuple<string, string>> ParseIni(string iniPath)
        {
            if (!File.Exists(iniPath)) 
                throw new FileNotFoundException("Unable to find " + iniPath);

            TextReader iniFile = null;
            var kvpList = new List<Tuple<string, string>>();

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

                        var nameValuePair = new Tuple<string, string>(key.Trim(' '), value.Trim(' '));
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