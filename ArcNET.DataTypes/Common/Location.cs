namespace ArcNET.DataTypes.Common
{
    public class Location
    {
        public int X { get; set; }
        public int Y { get; set; }

        public override string ToString()
        {
            return $"{X},{Y}";
        }
    }
}