using ArcNET.Formats;
using ArcNET.GameData.SaveGames;
using SharedLoadedSave = ArcNET.GameData.SaveGames.LoadedSave;
using SharedSaveGameLoader = ArcNET.GameData.SaveGames.SaveGameLoader;
using SharedSaveGameSnapshotComposer = ArcNET.GameData.SaveGames.SaveGameSnapshotComposer;
using SharedSaveGameUpdates = ArcNET.GameData.SaveGames.SaveGameUpdates;
using SharedSaveGameValidator = ArcNET.GameData.SaveGames.SaveGameValidator;
using SharedSaveGameWriter = ArcNET.GameData.SaveGames.SaveGameWriter;
using SharedSaveValidationSeverity = ArcNET.GameData.SaveGames.SaveValidationSeverity;

namespace ArcNET.Editor.Tests;

public sealed class SaveGameSharedNamespaceCompatibilityTests
{
    [Test]
    public async Task SaveGameWriter_WhenUsingSharedNamespaceSurface_RoundTripsUpdatedRawFiles()
    {
        var save = CreateSave();
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var gsiPath = Path.Combine(directory.FullName, "slot.gsi");
            var tfaiPath = Path.Combine(directory.FullName, "slot.tfai");
            var tfafPath = Path.Combine(directory.FullName, "slot.tfaf");
            byte[] updatedBytes = [9, 8, 7];

            SharedSaveGameWriter.Save(
                save,
                gsiPath,
                tfaiPath,
                tfafPath,
                new SharedSaveGameUpdates
                {
                    RawFileUpdates = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["test.bin"] = updatedBytes,
                    },
                }
            );

            var roundTripped = SharedSaveGameLoader.Load(gsiPath, tfaiPath, tfafPath);

            await Assert.That(roundTripped.Files["test.bin"]).IsEquivalentTo(updatedBytes);
            await Assert.That(roundTripped.RawFiles["test.bin"]).IsEquivalentTo(updatedBytes);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Test]
    public async Task SaveGameSnapshotComposer_WhenUsingSharedNamespaceSurface_ComposesUpdatedSnapshot()
    {
        var save = CreateSave();
        byte[] updatedBytes = [9, 8, 7, 6, 5];

        var snapshot = SharedSaveGameSnapshotComposer.Compose(
            save,
            new SharedSaveGameUpdates
            {
                UpdatedInfo = save.Info.With(displayName: "Updated Slot"),
                RawFileUpdates = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["test.bin"] = updatedBytes,
                },
            }
        );

        await Assert.That(snapshot.Info.DisplayName).IsEqualTo("Updated Slot");
        await Assert.That(snapshot.Files["test.bin"]).IsEquivalentTo(updatedBytes);
        await Assert.That(snapshot.RawFiles["test.bin"]).IsEquivalentTo(updatedBytes);
        await Assert.That(((TfaiFileEntry)snapshot.Index.Root[0]).Size).IsEqualTo(updatedBytes.Length);
    }

    [Test]
    public async Task SaveGameValidator_WhenUsingSharedNamespaceSurface_MapsLegacyFindingsToSharedTypes()
    {
        var save = CreateSave(
            new SaveInfo
            {
                ModuleName = "test-module",
                LeaderName = "",
                DisplayName = "Broken Slot",
                MapId = -1,
                GameTimeDays = 0,
                GameTimeMs = 0,
                LeaderPortraitId = 0,
                LeaderLevel = 150,
                LeaderTileX = 0,
                LeaderTileY = 0,
                StoryState = 0,
            }
        );

        var issues = SharedSaveGameValidator.Validate(save);

        await Assert
            .That(issues.Count(static issue => issue.Severity == SharedSaveValidationSeverity.Error))
            .IsEqualTo(2);
        await Assert
            .That(issues.Count(static issue => issue.Severity == SharedSaveValidationSeverity.Warning))
            .IsEqualTo(1);
        await Assert
            .That(issues.Any(static issue => issue.Message.Contains("invalid map reference", StringComparison.Ordinal)))
            .IsTrue();
    }

    private static SharedLoadedSave CreateSave(SaveInfo? info = null)
    {
        info ??= new SaveInfo
        {
            ModuleName = "test-module",
            LeaderName = "Hero",
            DisplayName = "Slot 0001",
            MapId = 1,
            GameTimeDays = 0,
            GameTimeMs = 0,
            LeaderPortraitId = 0,
            LeaderLevel = 1,
            LeaderTileX = 0,
            LeaderTileY = 0,
            StoryState = 0,
        };

        byte[] fileBytes = [1, 2, 3, 4];
        Dictionary<string, byte[]> files = new(StringComparer.OrdinalIgnoreCase) { ["test.bin"] = fileBytes };

        return SharedLoadedSave.FromLegacy(
            new ArcNET.Editor.LoadedSave
            {
                Info = info,
                Index = new SaveIndex { Root = [new TfaiFileEntry { Name = "test.bin", Size = fileBytes.Length }] },
                Files = files,
                RawFiles = new Dictionary<string, byte[]>(files, StringComparer.OrdinalIgnoreCase),
                Mobiles = new Dictionary<string, MobData>(StringComparer.OrdinalIgnoreCase),
                Sectors = new Dictionary<string, Sector>(StringComparer.OrdinalIgnoreCase),
                JumpFiles = new Dictionary<string, JmpFile>(StringComparer.OrdinalIgnoreCase),
                MapPropertiesList = new Dictionary<string, MapProperties>(StringComparer.OrdinalIgnoreCase),
                Messages = new Dictionary<string, MesFile>(StringComparer.OrdinalIgnoreCase),
                TownMapFogs = new Dictionary<string, TownMapFog>(StringComparer.OrdinalIgnoreCase),
                DataSavFiles = new Dictionary<string, DataSavFile>(StringComparer.OrdinalIgnoreCase),
                Data2SavFiles = new Dictionary<string, Data2SavFile>(StringComparer.OrdinalIgnoreCase),
                Scripts = new Dictionary<string, ScrFile>(StringComparer.OrdinalIgnoreCase),
                Dialogs = new Dictionary<string, DlgFile>(StringComparer.OrdinalIgnoreCase),
                MobileMds = new Dictionary<string, MobileMdFile>(StringComparer.OrdinalIgnoreCase),
                MobileMdys = new Dictionary<string, MobileMdyFile>(StringComparer.OrdinalIgnoreCase),
                ParseErrors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            }
        );
    }
}
