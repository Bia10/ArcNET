﻿using ArcNET.Utilities;
using System.IO;
using Utils.Console;

namespace ArcNET.DataTypes
{
    public class FacadeWalkReader
    {
        private readonly BinaryReader _reader;

        public FacadeWalkReader(BinaryReader reader)
        {
            _reader = reader;
        }

        public FacadeWalk Read()
        {
            var obj = new FacadeWalk
            {
                Marker = Marshalling.ByteArrayToStructure<FacWalkMarker>(_reader)
            };
            //ConsoleExtensions.Log($"Parsed file marker: {obj.Marker.fileMarker}", "success");

            const string markerConst = "FacWalk V101 ";
            var markerActual = obj.Marker.fileMarker;
            if (markerActual != markerConst)
            {
                ConsoleExtensions.Log("Filetype or version mismatch!", "error");
                ConsoleExtensions.Log($"Expected: {markerConst}", "error");
                ConsoleExtensions.Log($"Parsed: {markerActual}", "error");
                return null;
            }

            obj.Header = Marshalling.ByteArrayToStructure<FacWalkHeader>(_reader);
            obj.Entries = new FacWalkEntry[obj.Header.entryCount];
#if DEBUG
            //ConsoleExtensions.Log($"Parsed Header: {obj.Header}", "success");
            //ConsoleExtensions.Log($"Parsing {obj.Header.entryCount} entries", "info");
#endif
            for (var i = 0; i < obj.Header.entryCount; i++)
            {
                obj.Entries[i] = Marshalling.ByteArrayToStructure<FacWalkEntry>(_reader);
                //AnsiConsole.WriteLine($"Parsing entry: {obj.Entries[i]} index:{i}");
            }

            return obj;
        }
    }
}