using System;
using System.IO;
using ArcNET.Utilities;

namespace ArcNET.DataTypes
{
    public class MesEntry
    {
        private int _index;
        private int _dataCount;
        private string[] _data;

        public MesEntry(string[] data)
        {
            _data = data;
            if (!ParseIndex())
            {
                throw new Exception("Failure to parse index for MesEntry");
            }
        }

        public MesEntry(string line)
        {
            _data = new MesEntryReader(line).Parse();
            if (!ParseIndex())
            {
                throw new Exception("Failure to parse index for MesEntry");
            }
        }

        public MesEntry(TextReader textReader)
        {
            _data = new MesEntryReader(textReader).Parse();
            if (!ParseIndex())
            {
                throw new Exception("Failure to parse index for MesEntry");
            }
        }

        private bool ParseIndex()
        {
            //AnsiConsoleExtensions.Log($"_data: {_data}", "info");
            //AnsiConsoleExtensions.Log($"_data[0]: {_data[0]}", "info");
            var indexParsed = int.TryParse(_data[0], out _index);
            _dataCount = _data.Length;
            AnsiConsoleExtensions.Log($"Data count: {_dataCount}", "info");
            foreach (var str in _data)
            {
                AnsiConsoleExtensions.Log($"Data: {str}", "info");
            }

            return indexParsed && (_dataCount == 2 || _dataCount == 7);
        }

        public int GetIndex()
        {
            return _index;
        }
    }
}