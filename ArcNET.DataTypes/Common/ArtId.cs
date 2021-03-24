using System.Collections.Generic;

namespace ArcNET.DataTypes.Common
{
    public class ArtId
    {
        public string Path;
        public static List<string> ArtIds = new();

        public ArtId(string path)
        {
            Path = path;
        }

        public override string ToString()
        {
            return Path;
        }
    }
}