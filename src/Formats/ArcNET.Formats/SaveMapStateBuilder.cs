namespace ArcNET.Formats;

internal sealed class SaveMapStateBuilder
{
    public string MapPath { get; }

    private readonly List<(string FileName, Sector Data)> _sectors = [];
    private readonly List<(string FileName, MobData Data)> _staticObjects = [];
    private readonly List<(string RelativePath, byte[] Data)> _unknownFiles = [];
    private MapProperties? _properties;
    private JmpFile? _jumpPoints;
    private MobileMdFile? _staticDiffs;
    private MobileMdyFile? _dynamicObjects;

    public SaveMapStateBuilder(string mapPath) => MapPath = mapPath;

    public void Add(string relPath, byte[] data)
    {
        if (relPath.Equals("map.prp", StringComparison.OrdinalIgnoreCase))
        {
            _properties = MapPropertiesFormat.ParseMemory(data);
        }
        else if (relPath.Equals("map.jmp", StringComparison.OrdinalIgnoreCase))
        {
            _jumpPoints = JmpFormat.ParseMemory(data);
        }
        else if (relPath.Equals("mobile.md", StringComparison.OrdinalIgnoreCase))
        {
            _staticDiffs = MobileMdFormat.ParseMemory(data);
        }
        else if (relPath.Equals("mobile.mdy", StringComparison.OrdinalIgnoreCase))
        {
            _dynamicObjects = MobileMdyFormat.ParseMemory(data);
        }
        else if (relPath.EndsWith(".sec", StringComparison.OrdinalIgnoreCase))
        {
            _sectors.Add((relPath, SectorFormat.ParseMemory(data)));
        }
        else if (
            relPath.StartsWith("mobile/", StringComparison.OrdinalIgnoreCase)
            && relPath.EndsWith(".mob", StringComparison.OrdinalIgnoreCase)
        )
        {
            var fileName = relPath.Substring("mobile/".Length);
            _staticObjects.Add((fileName, MobFormat.ParseMemory(data)));
        }
        else
        {
            _unknownFiles.Add((relPath, data));
        }
    }

    public SaveMapState Build() =>
        new()
        {
            MapPath = MapPath,
            Properties = _properties,
            JumpPoints = _jumpPoints,
            Sectors = _sectors,
            StaticObjects = _staticObjects,
            StaticDiffs = _staticDiffs,
            DynamicObjects = _dynamicObjects,
            UnknownFiles = _unknownFiles,
        };
}
