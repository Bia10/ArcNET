using System.IO;
using ArcNET.Utilities;

namespace ArcNET.DataTypes
{
    public class FacWalkReader
    {
        private readonly BinaryReader _reader;

        public FacWalkReader(BinaryReader reader)
        {
            _reader = reader;
        }

        public FacWalk Read()
        {
            var obj = new FacWalk
            {
                Marker = Marshalling.ByteArrayToStructure<FacWalkMarker>(_reader)
            };
            AnsiConsoleExtensions.Log($"Parsed file marker: {obj.Marker.fileMarker}", "success");

            const string markerConst = "FacWalk V101 ";
            var marketActual = obj.Marker.fileMarker;
            if (marketActual != markerConst)
            {
                AnsiConsoleExtensions.Log("Filetype or version mismatch!", "error");
                AnsiConsoleExtensions.Log($"Expected: {markerConst}", "error");
                AnsiConsoleExtensions.Log($"Parsed: {marketActual}", "error");
                return null;
            }

            obj.Header = Marshalling.ByteArrayToStructure<FacWalkHeader>(_reader);
            AnsiConsoleExtensions.Log($"Parsed Header: {obj.Header}", "success");

            obj.Entries = new FacWalkEntry[obj.Header.entryCount];
            AnsiConsoleExtensions.Log($"Parsing {obj.Header.entryCount} entries", "info");
            for (var i = 0; i < obj.Header.entryCount; i++)
            {
                obj.Entries[i] = Marshalling.ByteArrayToStructure<FacWalkEntry>(_reader);
                //AnsiConsole.WriteLine($"Parsing entry: {obj.Entries[i]} index:{i}");
            }

            return obj;
        }
    }
}