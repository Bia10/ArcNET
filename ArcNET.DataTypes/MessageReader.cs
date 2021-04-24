using ArcNET.Utilities;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using AnsiConsoleExtensions = ArcNET.Utilities.AnsiConsoleExtensions;

namespace ArcNET.DataTypes
{
    public class MessageReader
    {
        private readonly StreamReader _reader;
        private Dictionary<int, string> _entries;

        public MessageReader(StreamReader reader)
        {
            _reader = reader;
        }

        public string GetEntriesAsJson()
        {
            return JsonConvert.SerializeObject(_entries, Formatting.Indented);
        }

        public List<string> Parse(string fileType)
        {
            var data = new List<string>();
            var lines = new List<string>();

            //Get all data
            while (!_reader.EndOfStream)
            { 
                lines.Add(_reader.ReadLine());
            }

            //for each fileType different filtering of data
            switch (fileType)
            {
                case "InvenSource.mes":
                {
                    foreach (var (line, index) in lines.WithIndex())
                    {
                        if (string.IsNullOrEmpty(line)) continue;
                        if (!line.StartsWith("{"))
                        {
                            AnsiConsoleExtensions.Log($"bad line:|{line}| at index:|{index}|", "warn");
                            continue;
                        }
                        data.Add(line);
                    }
                    break;
                }
            }
            return data;
        }

        /*
         curLine = curLine.TrimStart(' ', '\t');
         var unicodeLines = new[] { 
         "WILDERNESS (NICE) MUSIC",
         "WILDERNESS (EVIL) MUSIC",
         "VILLAGE MUSIC",
         "TOWN MUSIC MUSIC",
         "CITY TARRANT MUSIC",
         "CITY CALADON MUSIC",
         "CITY DERHOLM MUSIC",
         "ELVEN MUSIC",
         "DARK ELVEN MUSIC",
         "DWARVEN MUSIC",
         "CHAMBER DARK 1 MUSIC",
         "CHAMBER DARK 2 MUSIC",
         "CHAMBER CHASE MUSIC",
         "DARK AMBIENT (HEAVY) MUSIC",
         "DARK AMBIENT (LIGHT) MUSIC",
         "CRASH SITE MUSIC",
         "MIN'GOURAD'S LIAR MUSIC",
         "KREE MUSIC",
         "TEMPLE OF THE DERIAN KA MUSIC",
         "THE DREDGE/IRON CLAN MUSIC",
         "THE VOID MUSIC",
         "KERGHAN'S LIAR MUSIC",
         "ISLE OF DESPAIR MUSIC",
         "TULLA MUSIC",
         "ARCANUM THEME",
         "MAIN MENU MUSIC"
         };

         if (unicodeLines.Any(curLine.Contains))
         {
         AnsiConsoleExtensions.Log($"unicode line:|{curLine}|", "warn");
         continue;
         }
         if (curLine.StartsWith("//") || curLine.StartsWith("/\t\t") || curLine.StartsWith("***") || !curLine.StartsWith("{")) 
         {
         AnsiConsoleExtensions.Log($"bad line:|{curLine}|", "warn");
         continue;
         }
         if (string.IsNullOrEmpty(curLine))
         {
         AnsiConsoleExtensions.Log($"empty line:|{curLine}|", "warn");
         continue;
         }

         //TODO: multiline messages
         var mesEntry = new MessageEntry(curLine);
         if (!mes.ExistEntryWithIndex(mesEntry.GetIndex()))
         mes.AddEntry(mesEntry.GetIndex(), curLine);
       */
    }
}