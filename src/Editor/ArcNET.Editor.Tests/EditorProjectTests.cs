using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameData;

namespace ArcNET.Editor.Tests;

public sealed class EditorProjectTests
{
    [Test]
    public async Task CreateProject_CapturesInstallBackedWorkspaceReference()
    {
        var gameDir = Path.Combine("game-root");
        var contentDir = Path.Combine(gameDir, "data");
        var saveFolder = Path.Combine(gameDir, "save");
        var workspace = new EditorWorkspace
        {
            ContentDirectory = contentDir,
            GameDirectory = gameDir,
            GameData = new GameDataStore(),
            SaveFolder = saveFolder,
            SaveSlotName = "slot0001",
        };

        var project = workspace.CreateProject();

        await Assert.That(project.FormatVersion).IsEqualTo(EditorProject.CurrentFormatVersion);
        await Assert.That(project.Workspace.Kind).IsEqualTo(EditorProjectWorkspaceKind.GameInstall);
        await Assert.That(project.Workspace.RootPath).IsEqualTo(gameDir);
        await Assert.That(project.Workspace.SaveFolder).IsEqualTo(saveFolder);
        await Assert.That(project.Workspace.SaveSlotName).IsEqualTo("slot0001");
        await Assert.That(project.OpenAssets.Count).IsEqualTo(0);
        await Assert.That(project.Bookmarks.Count).IsEqualTo(0);
        await Assert.That(project.MapViewStates.Count).IsEqualTo(0);
        await Assert.That(project.ViewStates.Count).IsEqualTo(0);
        await Assert.That(project.ToolStates.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SessionCreateProject_CapturesTrackedOpenAssets_AndNormalizesMetadata()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            ScriptFormat.WriteToFile(
                new ScriptBuilder().WithDescription("Guard").AddCondition(ScriptConditionType.True).Build(),
                Path.Combine(contentDir, "scr", "00077Guard.scr")
            );
            DialogFormat.WriteToFile(
                new DlgFile
                {
                    Entries =
                    [
                        new DialogEntry
                        {
                            Num = 10,
                            Text = "Hello",
                            GenderField = string.Empty,
                            Iq = 0,
                            Conditions = string.Empty,
                            ResponseVal = 0,
                            Actions = string.Empty,
                        },
                    ],
                },
                Path.Combine(contentDir, "dlg", "00001Guard.dlg")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();

            _ = session.GetDialogEditor("dlg\\00001Guard.dlg");
            _ = session.GetScriptEditor("scr/00077Guard.scr");
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    ViewId = "map-scene",
                    Camera = new EditorProjectMapCameraState
                    {
                        CenterTileX = 4.5,
                        CenterTileY = 5.5,
                        Zoom = 1.25,
                    },
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = "maps\\map01\\0011ff44.sec",
                        Tile = new Location(7, 8),
                        Area = new EditorProjectMapAreaSelectionState
                        {
                            MinMapTileX = 6,
                            MinMapTileY = 7,
                            MaxMapTileX = 8,
                            MaxMapTileY = 9,
                            ObjectIds =
                            [
                                new GameObjectGuid(
                                    GameObjectGuid.OidTypeGuid,
                                    0,
                                    1,
                                    Guid.Parse("11111111-1111-1111-1111-111111111111")
                                ),
                            ],
                        },
                    },
                    Preview = new EditorProjectMapPreviewState
                    {
                        UseScenePreview = false,
                        OutlineMode = EditorMapPreviewMode.Blocked,
                        ShowObjects = false,
                        ShowRoofs = false,
                        ShowLights = true,
                        ShowBlockedTiles = true,
                        ShowScripts = false,
                    },
                    WorldEdit = new EditorProjectMapWorldEditState
                    {
                        ActiveTool = EditorProjectMapWorldEditActiveTool.ObjectPlacement,
                        Terrain = new EditorProjectMapTerrainToolState
                        {
                            MapPropertiesAssetPath = "maps\\map01\\map.prp",
                            PaletteX = 1,
                            PaletteY = 2,
                        },
                        ObjectPlacement = new EditorProjectMapObjectPlacementToolState
                        {
                            Mode = EditorProjectMapObjectPlacementMode.PlacementPreset,
                            PlacementRequest = EditorObjectPalettePlacementRequest.Place(1001, alignToTileGrid: true),
                            PresetLibrary =
                            [
                                EditorObjectPalettePlacementPreset.Create(
                                    "guard",
                                    "Guard",
                                    entries: [EditorObjectPalettePlacementRequest.Place(1001, rotation: 1.25f)]
                                ),
                            ],
                            SelectedPresetId = "guard",
                        },
                    },
                }
            );

            var project = session.CreateProject(
                activeAssetPath: "scr\\00077Guard.scr",
                openAssets:
                [
                    new EditorProjectOpenAsset
                    {
                        AssetPath = "dlg\\00001Guard.dlg",
                        ViewId = "dialog-graph",
                        IsPinned = true,
                        Properties = new Dictionary<string, string?> { ["pane"] = "left" },
                    },
                ],
                bookmarks:
                [
                    new EditorProjectBookmark
                    {
                        Id = "bookmark-1",
                        AssetPath = "dlg\\00001Guard.dlg",
                        Title = "Guard intro",
                        ViewId = "dialog-graph",
                        LocationKey = "entry:10",
                        Properties = new Dictionary<string, string?> { ["color"] = "gold" },
                    },
                ],
                viewStates:
                [
                    new EditorProjectViewState
                    {
                        Id = "view-1",
                        AssetPath = "scr\\00077Guard.scr",
                        ViewId = "script-grid",
                        Properties = new Dictionary<string, string?> { ["selectedCondition"] = "0" },
                    },
                ],
                toolStates:
                [
                    new EditorProjectToolState
                    {
                        ToolId = "asset-browser",
                        ScopeId = "left-sidebar",
                        Properties = new Dictionary<string, string?> { ["filter"] = "dlg" },
                    },
                ]
            );

            await Assert.That(project.Workspace.Kind).IsEqualTo(EditorProjectWorkspaceKind.ContentDirectory);
            await Assert.That(project.Workspace.RootPath).IsEqualTo(contentDir);
            await Assert.That(project.ActiveAssetPath).IsEqualTo("scr/00077Guard.scr");
            await Assert.That(project.OpenAssets.Count).IsEqualTo(2);

            var dialogAsset = project.OpenAssets.Single(static asset => asset.AssetPath == "dlg/00001Guard.dlg");
            var scriptAsset = project.OpenAssets.Single(static asset => asset.AssetPath == "scr/00077Guard.scr");

            await Assert.That(dialogAsset.ViewId).IsEqualTo("dialog-graph");
            await Assert.That(dialogAsset.IsPinned).IsTrue();
            await Assert.That(dialogAsset.Properties["pane"]).IsEqualTo("left");
            await Assert.That(scriptAsset.ViewId).IsNull();
            await Assert.That(scriptAsset.IsPinned).IsFalse();
            await Assert.That(scriptAsset.Properties.Count).IsEqualTo(0);
            await Assert.That(project.Bookmarks.Count).IsEqualTo(1);
            await Assert.That(project.Bookmarks[0].AssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(project.MapViewStates.Count).IsEqualTo(1);
            await Assert.That(project.MapViewStates[0].MapName).IsEqualTo("map01");
            await Assert.That(project.MapViewStates[0].Camera.Zoom).IsEqualTo(1.25);
            await Assert.That(project.MapViewStates[0].Selection.SectorAssetPath).IsEqualTo("maps/map01/0011ff44.sec");
            await Assert.That(project.MapViewStates[0].Selection.Tile).IsEqualTo(new Location(7, 8));
            await Assert.That(project.MapViewStates[0].Selection.Area).IsNotNull();
            await Assert.That(project.MapViewStates[0].Selection.Area!.MinMapTileX).IsEqualTo(6);
            await Assert.That(project.MapViewStates[0].Selection.Area!.MaxMapTileY).IsEqualTo(9);
            await Assert.That(project.MapViewStates[0].Selection.Area!.ObjectIds.Count).IsEqualTo(1);
            await Assert.That(project.MapViewStates[0].Preview.OutlineMode).IsEqualTo(EditorMapPreviewMode.Blocked);
            await Assert
                .That(project.MapViewStates[0].WorldEdit.ActiveTool)
                .IsEqualTo(EditorProjectMapWorldEditActiveTool.ObjectPlacement);
            await Assert
                .That(project.MapViewStates[0].WorldEdit.Terrain.MapPropertiesAssetPath)
                .IsEqualTo("maps/map01/map.prp");
            await Assert.That(project.MapViewStates[0].WorldEdit.ObjectPlacement.SelectedPresetId).IsEqualTo("guard");
            await Assert.That(project.MapViewStates[0].WorldEdit.ObjectPlacement.PresetLibrary.Count).IsEqualTo(1);
            await Assert.That(project.ViewStates.Count).IsEqualTo(1);
            await Assert.That(project.ViewStates[0].AssetPath).IsEqualTo("scr/00077Guard.scr");
            await Assert.That(project.ToolStates.Count).IsEqualTo(1);
            await Assert.That(project.ToolStates[0].ScopeId).IsEqualTo("left-sidebar");
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task OpenAndCloseAsset_TracksExplicitAssets_AndProtectsPendingEditors()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));

        try
        {
            DialogFormat.WriteToFile(
                new DlgFile
                {
                    Entries =
                    [
                        new DialogEntry
                        {
                            Num = 10,
                            Text = "Hello",
                            GenderField = string.Empty,
                            Iq = 0,
                            Conditions = string.Empty,
                            ResponseVal = 0,
                            Actions = string.Empty,
                        },
                    ],
                },
                Path.Combine(contentDir, "dlg", "00001Guard.dlg")
            );
            var mes = new MesFile { Entries = [new MessageEntry(10, "Hello from tracked asset")] };
            MessageFormat.WriteToFile(in mes, Path.Combine(contentDir, "mes", "game.mes"));

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var dialogAsset = session.OpenAsset(
                new EditorProjectOpenAsset
                {
                    AssetPath = "dlg\\00001Guard.dlg",
                    ViewId = "dialog-graph",
                    IsPinned = true,
                    Properties = new Dictionary<string, string?> { ["pane"] = "left" },
                }
            );
            var messageAsset = session.OpenAsset(
                new EditorProjectOpenAsset
                {
                    AssetPath = "mes\\game.mes",
                    ViewId = "message-grid",
                    Properties = new Dictionary<string, string?> { ["pane"] = "bottom" },
                }
            );

            session.SetActiveAsset("dlg\\00001Guard.dlg");
            session.GetDialogEditor("dlg/00001Guard.dlg").AddControlEntry(20, "E:");

            await Assert.That(dialogAsset.AssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(messageAsset.AssetPath).IsEqualTo("mes/game.mes");
            await Assert.That(session.GetOpenAssets().Count).IsEqualTo(2);
            await Assert.That(session.ActiveAssetPath).IsEqualTo("dlg/00001Guard.dlg");

            var closeException = Assert.Throws<InvalidOperationException>(() =>
                session.CloseAsset("dlg/00001Guard.dlg")
            );
            var closedMessage = session.CloseAsset("mes/game.mes");
            var closedDialog = session.CloseAsset("dlg/00001Guard.dlg", discardPendingChanges: true);
            var project = session.CreateProject();

            await Assert.That(closeException).IsNotNull();
            await Assert.That(closeException!.Message.Contains("staged changes", StringComparison.Ordinal)).IsTrue();
            await Assert.That(closedMessage).IsTrue();
            await Assert.That(closedDialog).IsTrue();
            await Assert.That(session.GetOpenAssets().Count).IsEqualTo(0);
            await Assert.That(session.ActiveAssetPath).IsNull();
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(0);
            await Assert.That(project.OpenAssets.Count).IsEqualTo(0);
            await Assert.That(project.ActiveAssetPath).IsNull();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SessionRestoreProject_ReplacesTrackedAssets_PreservesMetadata_AndReportsSkippedEntries()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));

        try
        {
            ScriptFormat.WriteToFile(
                new ScriptBuilder().WithDescription("Guard").AddCondition(ScriptConditionType.True).Build(),
                Path.Combine(contentDir, "scr", "00077Guard.scr")
            );
            DialogFormat.WriteToFile(
                new DlgFile
                {
                    Entries =
                    [
                        new DialogEntry
                        {
                            Num = 10,
                            Text = "Hello",
                            GenderField = string.Empty,
                            Iq = 0,
                            Conditions = string.Empty,
                            ResponseVal = 0,
                            Actions = string.Empty,
                        },
                    ],
                },
                Path.Combine(contentDir, "dlg", "00001Guard.dlg")
            );
            var mes = new MesFile { Entries = [new MessageEntry(10, "Hello from project restore")] };
            MessageFormat.WriteToFile(in mes, Path.Combine(contentDir, "mes", "game.mes"));

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            _ = session.GetScriptEditor("scr/00077Guard.scr");
            session.SetActiveAsset("scr/00077Guard.scr");
            var selectedObjectId = new GameObjectGuid(
                GameObjectGuid.OidTypeGuid,
                0,
                0,
                Guid.Parse("11111111-2222-3333-4444-555555555555")
            );
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "old-map-view",
                    MapName = "old-map",
                    Camera = new EditorProjectMapCameraState
                    {
                        CenterTileX = 1.5,
                        CenterTileY = 2.5,
                        Zoom = 0.5,
                    },
                }
            );

            var project = new EditorProject
            {
                Workspace = EditorProjectWorkspaceReference.ForContentDirectory(contentDir),
                ActiveAssetPath = "dlg\\00001Guard.dlg",
                OpenAssets =
                [
                    new EditorProjectOpenAsset
                    {
                        AssetPath = "dlg\\00001Guard.dlg",
                        ViewId = "dialog-graph",
                        IsPinned = true,
                        Properties = new Dictionary<string, string?> { ["pane"] = "left" },
                    },
                    new EditorProjectOpenAsset
                    {
                        AssetPath = "mes\\game.mes",
                        ViewId = "message-grid",
                        Properties = new Dictionary<string, string?> { ["pane"] = "bottom" },
                    },
                    new EditorProjectOpenAsset { AssetPath = "dlg\\missing.dlg", ViewId = "dialog-graph" },
                ],
                Bookmarks =
                [
                    new EditorProjectBookmark
                    {
                        Id = "bookmark-1",
                        AssetPath = "dlg\\00001Guard.dlg",
                        Title = "Guard intro",
                        ViewId = "dialog-graph",
                        LocationKey = "entry:10",
                        Properties = new Dictionary<string, string?> { ["color"] = "gold" },
                    },
                ],
                MapViewStates =
                [
                    new EditorProjectMapViewState
                    {
                        Id = "map-view-1",
                        MapName = "map01",
                        ViewId = "map-scene",
                        Camera = new EditorProjectMapCameraState
                        {
                            CenterTileX = 12.5,
                            CenterTileY = 23.5,
                            Zoom = 1.75,
                        },
                        Selection = new EditorProjectMapSelectionState
                        {
                            SectorAssetPath = "maps\\map01\\0011ff44.sec",
                            Tile = new Location(10, 11),
                            ObjectId = selectedObjectId,
                            Area = new EditorProjectMapAreaSelectionState
                            {
                                MinMapTileX = 9,
                                MinMapTileY = 10,
                                MaxMapTileX = 12,
                                MaxMapTileY = 13,
                                ObjectIds = [selectedObjectId],
                            },
                        },
                        Preview = new EditorProjectMapPreviewState
                        {
                            UseScenePreview = true,
                            OutlineMode = EditorMapPreviewMode.Lights,
                            ShowObjects = true,
                            ShowRoofs = false,
                            ShowLights = true,
                            ShowBlockedTiles = false,
                            ShowScripts = true,
                        },
                    },
                ],
                ViewStates =
                [
                    new EditorProjectViewState
                    {
                        Id = "view-1",
                        AssetPath = "scr\\00077Guard.scr",
                        ViewId = "script-grid",
                        Properties = new Dictionary<string, string?> { ["selectedCondition"] = "0" },
                    },
                ],
                ToolStates =
                [
                    new EditorProjectToolState
                    {
                        ToolId = "asset-browser",
                        ScopeId = "left-sidebar",
                        Properties = new Dictionary<string, string?> { ["filter"] = "dlg" },
                    },
                ],
            };

            var restore = session.RestoreProject(project);
            var roundTripped = session.CreateProject();

            await Assert.That(restore.RequestedActiveAssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(restore.RestoredActiveAssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(restore.RestoredAssetPaths).IsEquivalentTo(["dlg/00001Guard.dlg"]);
            await Assert.That(restore.SkippedAssetPaths).IsEquivalentTo(["mes/game.mes", "dlg/missing.dlg"]);
            await Assert.That(restore.RestoredProjectState.ActiveAssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(restore.RestoredProjectState.OpenAssets.Count).IsEqualTo(3);
            await Assert.That(restore.RestoredProjectState.Bookmarks.Count).IsEqualTo(1);
            await Assert.That(restore.RestoredProjectState.MapViewStates.Count).IsEqualTo(1);
            await Assert.That(restore.RestoredProjectState.ViewStates.Count).IsEqualTo(1);
            await Assert.That(restore.RestoredProjectState.ToolStates.Count).IsEqualTo(1);

            await Assert.That(roundTripped.ActiveAssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(roundTripped.OpenAssets.Count).IsEqualTo(3);
            await Assert.That(roundTripped.OpenAssets.Any(asset => asset.AssetPath == "scr/00077Guard.scr")).IsFalse();
            await Assert.That(session.GetOpenAssets().Count).IsEqualTo(3);
            await Assert.That(session.ActiveAssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(session.GetProjectStateSummary().ActiveAssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(session.GetProjectStateSummary().OpenAssets.Count).IsEqualTo(3);
            await Assert.That(session.GetMapViewStates().Count).IsEqualTo(1);
            await Assert.That(session.GetMapViewStates()[0].Id).IsEqualTo("map-view-1");
            await Assert.That(roundTripped.MapViewStates.Count).IsEqualTo(1);
            await Assert.That(roundTripped.MapViewStates[0].MapName).IsEqualTo("map01");
            await Assert.That(roundTripped.MapViewStates[0].Camera.CenterTileX).IsEqualTo(12.5);
            await Assert.That(roundTripped.MapViewStates[0].Camera.CenterTileY).IsEqualTo(23.5);
            await Assert.That(roundTripped.MapViewStates[0].Camera.Zoom).IsEqualTo(1.75);
            await Assert
                .That(roundTripped.MapViewStates[0].Selection.SectorAssetPath)
                .IsEqualTo("maps/map01/0011ff44.sec");
            await Assert.That(roundTripped.MapViewStates[0].Selection.Tile).IsEqualTo(new Location(10, 11));
            await Assert.That(roundTripped.MapViewStates[0].Selection.ObjectId).IsEqualTo(selectedObjectId);
            await Assert.That(roundTripped.MapViewStates[0].Selection.Area).IsNotNull();
            await Assert.That(roundTripped.MapViewStates[0].Selection.Area!.MinMapTileX).IsEqualTo(9);
            await Assert.That(roundTripped.MapViewStates[0].Selection.Area!.MaxMapTileY).IsEqualTo(13);
            await Assert.That(roundTripped.MapViewStates[0].Selection.Area!.ObjectIds.Count).IsEqualTo(1);
            await Assert.That(roundTripped.MapViewStates[0].Selection.Area!.ObjectIds[0]).IsEqualTo(selectedObjectId);
            await Assert.That(roundTripped.MapViewStates[0].Preview.OutlineMode).IsEqualTo(EditorMapPreviewMode.Lights);
            await Assert.That(roundTripped.MapViewStates[0].Preview.ShowRoofs).IsFalse();
            await Assert.That(roundTripped.MapViewStates[0].Preview.ShowBlockedTiles).IsFalse();

            var dialogAsset = roundTripped.OpenAssets.Single(static asset => asset.AssetPath == "dlg/00001Guard.dlg");
            var messageAsset = roundTripped.OpenAssets.Single(static asset => asset.AssetPath == "mes/game.mes");
            var missingAsset = roundTripped.OpenAssets.Single(static asset => asset.AssetPath == "dlg/missing.dlg");

            await Assert.That(dialogAsset.ViewId).IsEqualTo("dialog-graph");
            await Assert.That(dialogAsset.IsPinned).IsTrue();
            await Assert.That(dialogAsset.Properties["pane"]).IsEqualTo("left");
            await Assert.That(messageAsset.ViewId).IsEqualTo("message-grid");
            await Assert.That(messageAsset.Properties["pane"]).IsEqualTo("bottom");
            await Assert.That(missingAsset.ViewId).IsEqualTo("dialog-graph");
            await Assert.That(roundTripped.Bookmarks.Count).IsEqualTo(1);
            await Assert.That(roundTripped.Bookmarks[0].AssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(roundTripped.Bookmarks[0].Properties["color"]).IsEqualTo("gold");
            await Assert.That(roundTripped.ViewStates.Count).IsEqualTo(1);
            await Assert.That(roundTripped.ViewStates[0].AssetPath).IsEqualTo("scr/00077Guard.scr");
            await Assert.That(roundTripped.ToolStates.Count).IsEqualTo(1);
            await Assert.That(roundTripped.ToolStates[0].ScopeId).IsEqualTo("left-sidebar");
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task EditorProjectStore_SaveAndLoadAsync_RoundTripsProjectMetadata()
    {
        var gameDir = Path.Combine("game-root");
        var saveFolder = Path.Combine(gameDir, "save");
        var project = new EditorProject
        {
            Workspace = EditorProjectWorkspaceReference.ForGameInstall(
                gameDir,
                saveFolder: saveFolder,
                saveSlotName: "slot0001"
            ),
            ActiveAssetPath = "dlg/00001Virgil.dlg",
            OpenAssets =
            [
                new EditorProjectOpenAsset
                {
                    AssetPath = "dlg/00001Virgil.dlg",
                    ViewId = "dialog-graph",
                    IsPinned = true,
                    Properties = new Dictionary<string, string?> { ["pane"] = "left" },
                },
            ],
            Bookmarks =
            [
                new EditorProjectBookmark
                {
                    Id = "bookmark-1",
                    AssetPath = "dlg/00001Virgil.dlg",
                    Title = "Virgil intro",
                    ViewId = "dialog-graph",
                    LocationKey = "entry:10",
                    Properties = new Dictionary<string, string?> { ["color"] = "gold" },
                },
            ],
            MapViewStates =
            [
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    ViewId = "map-scene",
                    Camera = new EditorProjectMapCameraState
                    {
                        CenterTileX = 12.5,
                        CenterTileY = 24.5,
                        Zoom = 1.25,
                    },
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = "maps/map01/0011ff44.sec",
                        Tile = new Location(10, 11),
                        ObjectId = new GameObjectGuid(
                            GameObjectGuid.OidTypeGuid,
                            0,
                            0,
                            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")
                        ),
                        Area = new EditorProjectMapAreaSelectionState
                        {
                            MinMapTileX = 10,
                            MinMapTileY = 11,
                            MaxMapTileX = 14,
                            MaxMapTileY = 16,
                            ObjectIds =
                            [
                                new GameObjectGuid(
                                    GameObjectGuid.OidTypeGuid,
                                    0,
                                    0,
                                    Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")
                                ),
                            ],
                        },
                    },
                    Preview = new EditorProjectMapPreviewState
                    {
                        UseScenePreview = true,
                        OutlineMode = EditorMapPreviewMode.Objects,
                        ShowObjects = true,
                        ShowRoofs = false,
                        ShowLights = true,
                        ShowBlockedTiles = false,
                        ShowScripts = true,
                    },
                    WorldEdit = new EditorProjectMapWorldEditState
                    {
                        ActiveTool = EditorProjectMapWorldEditActiveTool.TerrainPaint,
                        Terrain = new EditorProjectMapTerrainToolState
                        {
                            MapPropertiesAssetPath = "maps/map01/map.prp",
                            PaletteX = 1,
                            PaletteY = 1,
                        },
                        ObjectPlacement = new EditorProjectMapObjectPlacementToolState
                        {
                            Mode = EditorProjectMapObjectPlacementMode.PlacementSet,
                            PlacementSet = EditorObjectPalettePlacementSet.Create(
                                "Guards",
                                EditorObjectPalettePlacementRequest.Place(1001, rotation: 0.5f),
                                EditorObjectPalettePlacementRequest.Place(1002, deltaTileX: 1)
                            ),
                        },
                    },
                },
            ],
            ViewStates =
            [
                new EditorProjectViewState
                {
                    Id = "view-1",
                    AssetPath = "dlg/00001Virgil.dlg",
                    ViewId = "dialog-graph",
                    Properties = new Dictionary<string, string?> { ["zoom"] = "1.25", ["selectedEntry"] = "10" },
                },
            ],
            ToolStates =
            [
                new EditorProjectToolState
                {
                    ToolId = "asset-browser",
                    ScopeId = "left-sidebar",
                    Properties = new Dictionary<string, string?> { ["filter"] = "dlg", ["sort"] = "name" },
                },
            ],
        };

        var projectDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var projectPath = Path.Combine(projectDir, "editor-project.arcnet.json");

        try
        {
            await EditorProjectStore.SaveAsync(projectPath, project);

            var loaded = await EditorProjectStore.LoadAsync(projectPath);

            await Assert.That(loaded.FormatVersion).IsEqualTo(EditorProject.CurrentFormatVersion);
            await Assert.That(loaded.ActiveAssetPath).IsEqualTo("dlg/00001Virgil.dlg");
            await Assert.That(loaded.Workspace.Kind).IsEqualTo(EditorProjectWorkspaceKind.GameInstall);
            await Assert.That(loaded.Workspace.RootPath).IsEqualTo(gameDir);
            await Assert.That(loaded.Workspace.SaveFolder).IsEqualTo(saveFolder);
            await Assert.That(loaded.Workspace.SaveSlotName).IsEqualTo("slot0001");
            await Assert.That(loaded.OpenAssets.Count).IsEqualTo(1);
            await Assert.That(loaded.OpenAssets[0].ViewId).IsEqualTo("dialog-graph");
            await Assert.That(loaded.OpenAssets[0].IsPinned).IsTrue();
            await Assert.That(loaded.OpenAssets[0].Properties["pane"]).IsEqualTo("left");
            await Assert.That(loaded.Bookmarks.Count).IsEqualTo(1);
            await Assert.That(loaded.Bookmarks[0].LocationKey).IsEqualTo("entry:10");
            await Assert.That(loaded.Bookmarks[0].Properties["color"]).IsEqualTo("gold");
            await Assert.That(loaded.MapViewStates.Count).IsEqualTo(1);
            await Assert.That(loaded.MapViewStates[0].MapName).IsEqualTo("map01");
            await Assert.That(loaded.MapViewStates[0].Camera.CenterTileX).IsEqualTo(12.5);
            await Assert.That(loaded.MapViewStates[0].Camera.CenterTileY).IsEqualTo(24.5);
            await Assert.That(loaded.MapViewStates[0].Camera.Zoom).IsEqualTo(1.25);
            await Assert.That(loaded.MapViewStates[0].Selection.SectorAssetPath).IsEqualTo("maps/map01/0011ff44.sec");
            await Assert.That(loaded.MapViewStates[0].Selection.Tile).IsEqualTo(new Location(10, 11));
            await Assert.That(loaded.MapViewStates[0].Selection.Area).IsNotNull();
            await Assert.That(loaded.MapViewStates[0].Selection.Area!.MinMapTileX).IsEqualTo(10);
            await Assert.That(loaded.MapViewStates[0].Selection.Area!.MaxMapTileY).IsEqualTo(16);
            await Assert.That(loaded.MapViewStates[0].Selection.Area!.ObjectIds.Count).IsEqualTo(1);
            await Assert.That(loaded.MapViewStates[0].Preview.OutlineMode).IsEqualTo(EditorMapPreviewMode.Objects);
            await Assert.That(loaded.MapViewStates[0].Preview.ShowRoofs).IsFalse();
            await Assert
                .That(loaded.MapViewStates[0].WorldEdit.ActiveTool)
                .IsEqualTo(EditorProjectMapWorldEditActiveTool.TerrainPaint);
            await Assert
                .That(loaded.MapViewStates[0].WorldEdit.Terrain.MapPropertiesAssetPath)
                .IsEqualTo("maps/map01/map.prp");
            await Assert
                .That(loaded.MapViewStates[0].WorldEdit.ObjectPlacement.Mode)
                .IsEqualTo(EditorProjectMapObjectPlacementMode.PlacementSet);
            await Assert.That(loaded.MapViewStates[0].WorldEdit.ObjectPlacement.PlacementSet).IsNotNull();
            await Assert
                .That(loaded.MapViewStates[0].WorldEdit.ObjectPlacement.PlacementSet!.Entries.Count)
                .IsEqualTo(2);
            await Assert.That(loaded.ViewStates.Count).IsEqualTo(1);
            await Assert.That(loaded.ViewStates[0].Properties["zoom"]).IsEqualTo("1.25");
            await Assert.That(loaded.ToolStates.Count).IsEqualTo(1);
            await Assert.That(loaded.ToolStates[0].ScopeId).IsEqualTo("left-sidebar");
            await Assert.That(loaded.ToolStates[0].Properties["filter"]).IsEqualTo("dlg");
        }
        finally
        {
            if (Directory.Exists(projectDir))
                Directory.Delete(projectDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadSessionAsync_ReopensWorkspace_AndRestoresSupportedProjectAssets()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            ScriptFormat.WriteToFile(
                new ScriptBuilder().WithDescription("Guard").AddCondition(ScriptConditionType.True).Build(),
                Path.Combine(contentDir, "scr", "00077Guard.scr")
            );
            DialogFormat.WriteToFile(
                new DlgFile
                {
                    Entries =
                    [
                        new DialogEntry
                        {
                            Num = 10,
                            Text = "Hello",
                            GenderField = string.Empty,
                            Iq = 0,
                            Conditions = string.Empty,
                            ResponseVal = 0,
                            Actions = string.Empty,
                        },
                    ],
                },
                Path.Combine(contentDir, "dlg", "00001Guard.dlg")
            );

            var project = new EditorProject
            {
                Workspace = EditorProjectWorkspaceReference.ForContentDirectory(contentDir),
                ActiveAssetPath = "dlg\\00001Guard.dlg",
                MapViewStates =
                [
                    new EditorProjectMapViewState
                    {
                        Id = "map-view-1",
                        MapName = "map01",
                        ViewId = "map-scene",
                        Camera = new EditorProjectMapCameraState
                        {
                            CenterTileX = 4.5,
                            CenterTileY = 5.5,
                            Zoom = 2.0,
                        },
                        Selection = new EditorProjectMapSelectionState
                        {
                            SectorAssetPath = "maps\\map01\\0011ff44.sec",
                            Tile = new Location(7, 8),
                        },
                        Preview = new EditorProjectMapPreviewState
                        {
                            UseScenePreview = false,
                            OutlineMode = EditorMapPreviewMode.Blocked,
                            ShowObjects = false,
                            ShowRoofs = false,
                            ShowLights = true,
                            ShowBlockedTiles = true,
                            ShowScripts = false,
                        },
                        WorldEdit = new EditorProjectMapWorldEditState
                        {
                            ActiveTool = EditorProjectMapWorldEditActiveTool.TerrainPaint,
                            Terrain = new EditorProjectMapTerrainToolState
                            {
                                MapPropertiesAssetPath = "maps\\map01\\map.prp",
                                PaletteX = 0,
                                PaletteY = 1,
                            },
                        },
                    },
                ],
                OpenAssets =
                [
                    new EditorProjectOpenAsset
                    {
                        AssetPath = "scr\\00077Guard.scr",
                        ViewId = "script-grid",
                        Properties = new Dictionary<string, string?> { ["pane"] = "right" },
                    },
                ],
            };

            var load = await project.LoadSessionWithRestoreResultAsync();
            var session = load.Session;
            var bootstrap = load.BootstrapSummary;
            var projectState = session.GetProjectStateSummary();
            var stagedTransactions = session.GetStagedTransactionSummaries();
            var stagedCommands = session.GetAvailableStagedCommandSummaries();
            var historyCommands = session.GetHistoryCommandSummaries();
            var restoredProject = session.CreateProject(openAssets: []);

            await Assert.That(session.Workspace.ContentDirectory).IsEqualTo(contentDir);
            await Assert.That(load.Restore.RestoredActiveAssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(load.Restore.RestoredProjectState.ActiveAssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(load.Restore.RestoredProjectState.MapViewStates.Count).IsEqualTo(1);
            await Assert.That(bootstrap.Restore).IsSameReferenceAs(load.Restore);
            await Assert.That(bootstrap.ProjectState.ActiveAssetPath).IsEqualTo(projectState.ActiveAssetPath);
            await Assert.That(bootstrap.ProjectState.MapViewStates.Count).IsEqualTo(projectState.MapViewStates.Count);
            await Assert.That(bootstrap.StagedTransactions.Count).IsEqualTo(stagedTransactions.Count);
            await Assert.That(bootstrap.StagedCommands.Count).IsEqualTo(stagedCommands.Count);
            await Assert.That(bootstrap.HistoryCommands.Count).IsEqualTo(historyCommands.Count);
            await Assert.That(restoredProject.ActiveAssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(restoredProject.OpenAssets.Count).IsEqualTo(2);
            await Assert.That(session.GetMapViewStates().Count).IsEqualTo(1);
            await Assert.That(session.GetMapViewStates()[0].Camera.Zoom).IsEqualTo(2.0);
            await Assert
                .That(session.GetMapViewStates()[0].Selection.SectorAssetPath)
                .IsEqualTo("maps/map01/0011ff44.sec");
            await Assert
                .That(session.GetMapViewStates()[0].WorldEdit.ActiveTool)
                .IsEqualTo(EditorProjectMapWorldEditActiveTool.TerrainPaint);
            await Assert.That(session.GetMapViewStates()[0].WorldEdit.Terrain.PaletteY).IsEqualTo(1UL);
            await Assert.That(restoredProject.MapViewStates.Count).IsEqualTo(1);
            await Assert
                .That(restoredProject.MapViewStates[0].Preview.OutlineMode)
                .IsEqualTo(EditorMapPreviewMode.Blocked);

            var dialogAsset = restoredProject.OpenAssets.Single(static asset =>
                asset.AssetPath == "dlg/00001Guard.dlg"
            );
            var scriptAsset = restoredProject.OpenAssets.Single(static asset =>
                asset.AssetPath == "scr/00077Guard.scr"
            );

            await Assert.That(dialogAsset.ViewId).IsNull();
            await Assert.That(scriptAsset.ViewId).IsNull();
            await Assert.That(scriptAsset.Properties.Count).IsEqualTo(0);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task WorkspaceReference_LoadAsync_ReopensLooseWorkspace()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));

        try
        {
            var mesPath = Path.Combine(contentDir, "mes", "game.mes");
            var mes = new MesFile { Entries = [new MessageEntry(10, "Hello from project")] };
            MessageFormat.WriteToFile(in mes, mesPath);

            var project = EditorProject.Create(EditorProjectWorkspaceReference.ForContentDirectory(contentDir));
            var workspace = await project.Workspace.LoadAsync();

            await Assert.That(workspace.ContentDirectory).IsEqualTo(contentDir);
            await Assert.That(workspace.GameDirectory).IsNull();
            await Assert.That(workspace.GameData.Messages.Count).IsEqualTo(1);
            await Assert.That(workspace.Assets.Find("mes/game.mes")).IsNotNull();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }
}
