using ArcNET.Formats;
using ArcNET.GameData;

namespace ArcNET.Editor.Tests;

public sealed class EditorCapabilitySummaryTests
{
    [Test]
    public async Task GetCapabilities_ReturnsSupportedContract_AndAvailableWorkspaceFeatures()
    {
        var workspace = new EditorWorkspace
        {
            ContentDirectory = Path.Combine("content"),
            GameDirectory = Path.Combine("game-root"),
            Module = new EditorWorkspaceModuleContext
            {
                ModuleName = "co8",
                ModuleDirectory = Path.Combine("game-root", "modules", "co8"),
                ArchivePaths = [],
            },
            GameData = new GameDataStore(),
            Assets = EditorAssetCatalog.Create([
                CreateAsset("dlg/00001Guard.dlg", FileFormat.Dialog),
                CreateAsset("scr/00077Guard.scr", FileFormat.Script),
                CreateAsset("maps/map01/map.prp", FileFormat.MapProperties),
                CreateAsset("proto/0001.pro", FileFormat.Proto),
                CreateAsset("art/1000.art", FileFormat.Art),
                CreateAsset("maps/map01/0011ff44.sec", FileFormat.Sector),
            ]),
            AudioAssets = EditorAudioAssetCatalog.Create([CreateAudioAsset("sound/guard.wav")]),
            Index = EditorAssetIndex.Create(EditorAssetIndexData.Empty with { MapNames = ["map01"] }),
            Save = CreateLoadedSave(),
            SaveFolder = Path.Combine("game-root", "save"),
            SaveSlotName = "slot0001",
        };

        var capabilities = workspace.GetCapabilities();

        await Assert
            .That(capabilities.SupportedCapabilities.ToArray())
            .IsEquivalentTo(EditorCapabilitySummary.SupportedCapabilityOrder.ToArray());
        await Assert
            .That(capabilities.AvailableCapabilities.Count)
            .IsEqualTo(capabilities.AvailableCapabilities.Distinct().Count());
        await Assert.That(capabilities.Supports(EditorCapability.WorkspaceLoadGameInstall)).IsTrue();
        await Assert.That(capabilities.Supports(EditorCapability.SaveEditing)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.WorkspaceLoadGameInstall)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.WorkspaceLoadModule)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.WorkspaceComposeSaveSlot)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.DialogEditing)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ScriptEditing)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.SaveEditing)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.TerrainPaletteBrowsing)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.TerrainLayerEditing)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.TrackedTerrainToolWorkflow)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectPaletteBrowsing)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectPlacement)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.TrackedObjectPlacementWorkflow)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectInspectorSummary)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectInspectorFlags)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectInspectorScriptAttachments)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectInspectorCritterProgression)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectInspectorLight)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectInspectorGenerator)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectInspectorBlending)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectTransformEditing)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.SectorLightEditing)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.SectorTileScriptEditing)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.MapPreview)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.MapScenePreview)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.SceneHitTesting)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ArtPreview)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.AudioPreviewWave)).IsTrue();
    }

    [Test]
    public async Task GetCapabilities_KeepsWorkspaceSpecificSlicesUnavailable_WhenInputsAreMissing()
    {
        var workspace = new EditorWorkspace
        {
            ContentDirectory = Path.Combine("content"),
            GameData = new GameDataStore(),
        };

        var capabilities = workspace.GetCapabilities();

        await Assert.That(capabilities.IsAvailable(EditorCapability.WorkspaceLoadContentDirectory)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.AssetCatalog)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ProjectPersistence)).IsTrue();
        await Assert.That(capabilities.Supports(EditorCapability.SaveEditing)).IsTrue();
        await Assert.That(capabilities.Supports(EditorCapability.MapScenePreview)).IsTrue();
        await Assert.That(capabilities.IsAvailable(EditorCapability.WorkspaceLoadGameInstall)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.WorkspaceLoadModule)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.WorkspaceComposeSaveSlot)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.SaveEditing)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.DialogEditing)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ScriptEditing)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.TerrainPaletteBrowsing)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.TerrainLayerEditing)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.TrackedTerrainToolWorkflow)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectPaletteBrowsing)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectInspectorSummary)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectInspectorFlags)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectInspectorScriptAttachments)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectInspectorCritterProgression)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectInspectorLight)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectInspectorGenerator)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ObjectInspectorBlending)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.TrackedObjectPlacementWorkflow)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.MapPreview)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.MapScenePreview)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.SceneHitTesting)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.ArtPreview)).IsFalse();
        await Assert.That(capabilities.IsAvailable(EditorCapability.AudioPreviewWave)).IsFalse();
    }

    private static EditorAssetEntry CreateAsset(string assetPath, FileFormat format) =>
        new()
        {
            AssetPath = assetPath,
            Format = format,
            ItemCount = 1,
            SourceKind = EditorAssetSourceKind.LooseFile,
            SourcePath = Path.Combine("content", assetPath.Replace('/', Path.DirectorySeparatorChar)),
        };

    private static EditorAudioAssetEntry CreateAudioAsset(string assetPath) =>
        new()
        {
            AssetPath = assetPath,
            SourceKind = EditorAssetSourceKind.LooseFile,
            SourcePath = Path.Combine("content", assetPath.Replace('/', Path.DirectorySeparatorChar)),
            ByteLength = 128,
        };

    private static LoadedSave CreateLoadedSave() =>
        new()
        {
            Info = new SaveInfo
            {
                ModuleName = "Arcanum",
                LeaderName = "Virgil",
                DisplayName = "slot0001",
                MapId = 1,
                GameTimeDays = 0,
                GameTimeMs = 0,
                LeaderPortraitId = 0,
                LeaderLevel = 1,
                LeaderTileX = 0,
                LeaderTileY = 0,
                StoryState = 0,
            },
            Index = new SaveIndex { Root = [] },
            Files = new Dictionary<string, byte[]>(),
            RawFiles = new Dictionary<string, byte[]>(),
            Mobiles = new Dictionary<string, MobData>(),
            Sectors = new Dictionary<string, Sector>(),
            JumpFiles = new Dictionary<string, JmpFile>(),
            MapPropertiesList = new Dictionary<string, MapProperties>(),
            Messages = new Dictionary<string, MesFile>(),
            TownMapFogs = new Dictionary<string, TownMapFog>(),
            DataSavFiles = new Dictionary<string, DataSavFile>(),
            Data2SavFiles = new Dictionary<string, Data2SavFile>(),
            Scripts = new Dictionary<string, ScrFile>(),
            Dialogs = new Dictionary<string, DlgFile>(),
            MobileMds = new Dictionary<string, MobileMdFile>(),
            MobileMdys = new Dictionary<string, MobileMdyFile>(),
            ParseErrors = new Dictionary<string, string>(),
        };
}
