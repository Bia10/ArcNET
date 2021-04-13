using Newtonsoft.Json;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public MessageReader Parse()
        {
            var mes = new MessageReader(_reader);
            mes.Init();

            while (true)
            {
                var curLine = _reader.ReadLine();
                //AnsiConsoleExtensions.Log($"currentLine:|{curLine}|", "warn");
                if (!string.IsNullOrEmpty(curLine))
                {
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
                }
                if (curLine == null)
                {
                    break;
                }
            }
            return mes;
        }

        public void Dispose()
        {
            Clear();
        }

        private void Clear()
        {
            if (_entries.Count > 0)
            {
                _entries.Clear();
            }
        }

        private void Init()
        {
            _entries = new Dictionary<int, string>();
        }

        public int GetEntryCount()
        {
            return _entries.Count;
        }

        public MessageEntry GetEntryWithIndex(int index)
        {
            var mesEntry = new MessageEntry(_entries.Values.ElementAt(index));
            return mesEntry;
        }

        private bool ExistEntryWithIndex(int index)
        {
            return _entries.ContainsKey(index);
        }

        public bool AddEntry(int index, string entry)
        {
            try
            {
                if (!_entries.ContainsKey(index))
                {
                    _entries.Add(index, entry);
                    return true;
                }
                AnsiConsoleExtensions.Log("Already in dictionary", "error");
                return false;
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                return false;
            }
        }

        public bool DelEntry(int index)
        {
            try
            {
                if (_entries.ContainsKey(index))
                {
                    _entries.Remove(index);
                }
                else
                {
                    AnsiConsoleExtensions.Log("Not contain in dictionary", "error");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                return false;
            }
            return true;
        }
    }
}