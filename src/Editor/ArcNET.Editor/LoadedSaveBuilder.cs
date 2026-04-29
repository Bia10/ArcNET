using ArcNET.Formats;

namespace ArcNET.Editor;

internal sealed class LoadedSaveBuilder
{
    public Dictionary<string, byte[]> RawFiles { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, MobData> Mobiles { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, Sector> Sectors { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, JmpFile> JumpFiles { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, MapProperties> MapPropertiesList { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, MesFile> Messages { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, TownMapFog> TownMapFogs { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, DataSavFile> DataSavFiles { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, Data2SavFile> Data2SavFiles { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, ScrFile> Scripts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, DlgFile> Dialogs { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, MobileMdFile> MobileMds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, MobileMdyFile> MobileMdys { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> ParseErrors { get; } = new(StringComparer.OrdinalIgnoreCase);

    public LoadedSave Build(SaveInfo info, SaveIndex index, IReadOnlyDictionary<string, byte[]> files) =>
        new()
        {
            Info = info,
            Index = index,
            Files = files,
            RawFiles = RawFiles,
            Mobiles = Mobiles,
            Sectors = Sectors,
            JumpFiles = JumpFiles,
            MapPropertiesList = MapPropertiesList,
            Messages = Messages,
            TownMapFogs = TownMapFogs,
            DataSavFiles = DataSavFiles,
            Data2SavFiles = Data2SavFiles,
            Scripts = Scripts,
            Dialogs = Dialogs,
            MobileMds = MobileMds,
            MobileMdys = MobileMdys,
            ParseErrors = ParseErrors,
        };
}
