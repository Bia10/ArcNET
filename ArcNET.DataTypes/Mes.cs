using Newtonsoft.Json;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnsiConsoleExtensions = ArcNET.Utilities.AnsiConsoleExtensions;

namespace ArcNET.DataTypes
{
    public class Mes
    {
        private readonly StreamReader _reader;
        private Dictionary<int, string> _entries;

        public Mes(StreamReader reader)
        {
            _reader = reader;
        }

        public string GetEntriesAsJson()
        {
            return JsonConvert.SerializeObject(_entries, Formatting.Indented);
        }

        public Mes Parse()
        {
            var mes = new Mes(_reader);
            mes.Init();

            while (true)
            {
                var temp = _reader.ReadLine();
                if (!string.IsNullOrEmpty(temp))
                {
                    temp = temp.TrimStart(' ', '\t');
                    if (temp.StartsWith("//")) continue;

                    var mesEntry = new MesEntry(temp);
                    if (!mes.ExistEntryWithIndex(mesEntry.GetIndex()))
                        mes.AddEntry(mesEntry.GetIndex(), temp);
                }
                if (temp == null)
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

        public MesEntry GetEntryWithIndex(int index)
        {
            var mesEntry = new MesEntry(_entries.Values.ElementAt(index));
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