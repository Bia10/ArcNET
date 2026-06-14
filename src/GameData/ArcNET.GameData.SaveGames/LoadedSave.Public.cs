namespace ArcNET.GameData.SaveGames;

/// <summary>
/// Shared save-slot surface that mirrors the legacy <c>ArcNET.Editor.LoadedSave</c> type
/// while presenting a package-local namespace to non-editor consumers.
/// </summary>
public sealed class LoadedSave : ArcNET.Editor.LoadedSave
{
    private LoadedSave() { }

    public static LoadedSave FromLegacy(ArcNET.Editor.LoadedSave save)
    {
        ArgumentNullException.ThrowIfNull(save);

        return save as LoadedSave
            ?? new LoadedSave
            {
                Info = save.Info,
                Index = save.Index,
                Files = save.Files,
                RawFiles = save.RawFiles,
                Mobiles = save.Mobiles,
                Sectors = save.Sectors,
                JumpFiles = save.JumpFiles,
                MapPropertiesList = save.MapPropertiesList,
                Messages = save.Messages,
                TownMapFogs = save.TownMapFogs,
                DataSavFiles = save.DataSavFiles,
                Data2SavFiles = save.Data2SavFiles,
                Scripts = save.Scripts,
                Dialogs = save.Dialogs,
                MobileMds = save.MobileMds,
                MobileMdys = save.MobileMdys,
                ParseErrors = save.ParseErrors,
            };
    }
}
