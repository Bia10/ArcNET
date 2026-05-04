using System.Buffers.Binary;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameData;
using ArcNET.GameObjects;

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
                            PaletteSearchText = "  wolf  ",
                            PaletteCategory = "  pc ",
                            SelectedPaletteProtoNumber = 1002,
                        },
                        Shell = new EditorProjectMapWorldEditShellState
                        {
                            ViewMode = EditorMapSceneViewMode.TopDown,
                            ViewportWidth = 320d,
                            ViewportHeight = 200d,
                            ObjectPaletteSearchText = "  guard  ",
                            ObjectPaletteCategory = "  pc ",
                            IncludeTrackedPlacementPreview = false,
                        },
                        Inspector = new EditorProjectMapObjectInspectorState
                        {
                            TargetMode = EditorProjectMapObjectInspectorTargetMode.ProtoDefinition,
                            PinnedProtoNumber = 1002,
                            ActivePane = EditorObjectInspectorPane.Flags,
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
            await Assert.That(project.MapViewStates[0].WorldEdit.ObjectPlacement.PaletteSearchText).IsEqualTo("wolf");
            await Assert.That(project.MapViewStates[0].WorldEdit.ObjectPlacement.PaletteCategory).IsEqualTo("pc");
            await Assert
                .That(project.MapViewStates[0].WorldEdit.ObjectPlacement.SelectedPaletteProtoNumber)
                .IsEqualTo(1002);
            await Assert
                .That(project.MapViewStates[0].WorldEdit.Shell.ViewMode)
                .IsEqualTo(EditorMapSceneViewMode.TopDown);
            await Assert.That(project.MapViewStates[0].WorldEdit.Shell.ViewportWidth).IsEqualTo(320d);
            await Assert.That(project.MapViewStates[0].WorldEdit.Shell.ViewportHeight).IsEqualTo(200d);
            await Assert.That(project.MapViewStates[0].WorldEdit.Shell.ObjectPaletteSearchText).IsEqualTo("guard");
            await Assert.That(project.MapViewStates[0].WorldEdit.Shell.ObjectPaletteCategory).IsEqualTo("pc");
            await Assert.That(project.MapViewStates[0].WorldEdit.Shell.IncludeTrackedPlacementPreview).IsFalse();
            await Assert
                .That(project.MapViewStates[0].WorldEdit.Inspector.TargetMode)
                .IsEqualTo(EditorProjectMapObjectInspectorTargetMode.ProtoDefinition);
            await Assert.That(project.MapViewStates[0].WorldEdit.Inspector.PinnedProtoNumber).IsEqualTo(1002);
            await Assert
                .That(project.MapViewStates[0].WorldEdit.Inspector.ActivePane)
                .IsEqualTo(EditorObjectInspectorPane.Flags);
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
    public async Task LoadSessionAsync_ReopensTrackedWorldEditWorkflowBuiltThroughHelpers()
    {
        const int selectedProtoNumber = 1001;
        const int pinnedProtoNumber = 1002;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var projectDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var projectPath = Path.Combine(projectDir, "workflow.arcnet.json");
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));
        Directory.CreateDirectory(projectDir);

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(selectedProtoNumber),
                Path.Combine(contentDir, "proto", "001001 - Selected.pro")
            );
            ProtoFormat.WriteToFile(
                MakeNpcProto(pinnedProtoNumber),
                Path.Combine(contentDir, "proto", "001002 - Pinned.pro")
            );

            MapProperties mapProperties = new()
            {
                ArtId = 200,
                Unused = 0,
                LimitX = 2,
                LimitY = 2,
            };
            MapPropertiesFormat.WriteToFile(in mapProperties, Path.Combine(contentDir, "maps", "map01", "map.prp"));

            var selectedObject = new MobDataBuilder(MakePc(selectedProtoNumber)).WithLocation(5, 6).Build();
            SectorFormat.WriteToFile(
                new SectorBuilder(MakeSector(selectedObject)).SetTile(5, 6, 201u).SetTile(1, 1, 203u).Build(),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            var placementPreset = EditorObjectPalettePlacementPreset.Create(
                "pinned-guard",
                "Pinned Guard",
                entries:
                [
                    EditorObjectPalettePlacementRequest.Place(pinnedProtoNumber, rotation: 2.5f),
                    EditorObjectPalettePlacementRequest.Place(selectedProtoNumber, deltaTileX: 1),
                ]
            );
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    ViewId = "map-scene",
                    Camera = new EditorProjectMapCameraState
                    {
                        CenterTileX = 5d,
                        CenterTileY = 6d,
                        Zoom = 1.5d,
                    },
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                }
            );

            _ = session.SetTrackedObjectPlacementEntry("map-view-1", pinnedProtoNumber, rotation: 0.5f);
            _ = session.SelectTrackedObjectPaletteEntry(
                "map-view-1",
                pinnedProtoNumber,
                searchText: "Pinned",
                category: "npc"
            );
            _ = session.SetTrackedObjectPlacementPreset("map-view-1", placementPreset);
            _ = session.SetTrackedTerrainPaletteEntry("map-view-1", 1, 1, activateTool: false);
            _ = session.SetTrackedMapWorldEditShellPreferences(
                "map-view-1",
                new EditorMapWorldEditShellRequest
                {
                    ViewMode = EditorMapSceneViewMode.TopDown,
                    ViewportWidth = 320d,
                    ViewportHeight = 200d,
                    ObjectPaletteSearchText = "Pinned",
                    ObjectPaletteCategory = "npc",
                    IncludeTrackedPlacementPreview = false,
                }
            );
            _ = session.SetTrackedObjectInspectorState(
                "map-view-1",
                new EditorProjectMapObjectInspectorState
                {
                    TargetMode = EditorProjectMapObjectInspectorTargetMode.ProtoDefinition,
                    PinnedProtoNumber = pinnedProtoNumber,
                    ActivePane = EditorObjectInspectorPane.Generator,
                }
            );

            var project = session.CreateProject();
            await EditorProjectStore.SaveAsync(projectPath, project);

            var reloadedProject = await EditorProjectStore.LoadAsync(projectPath);
            var load = await reloadedProject.LoadSessionWithRestoreResultAsync();
            var restoredSession = load.Session;
            var restoredMapViewState = restoredSession.GetMapViewStates().Single();
            var restoredPaletteSummary = restoredSession.GetTrackedObjectPaletteSummary("map-view-1");
            var restoredPlacementTool = restoredSession.GetTrackedObjectPlacementToolSummary("map-view-1");
            var restoredInspector = restoredSession.GetTrackedObjectInspectorSummary("map-view-1");
            var restoredShell = restoredSession.CreateTrackedMapWorldEditShell("map-view-1");

            await Assert.That(load.Restore.RestoredProjectState.MapViewStates.Count).IsEqualTo(1);
            await Assert.That(restoredMapViewState.MapName).IsEqualTo("map01");
            await Assert.That(restoredMapViewState.Camera.Zoom).IsEqualTo(1.5d);
            await Assert.That(restoredMapViewState.Selection.SectorAssetPath).IsEqualTo(sectorAssetPath);
            await Assert.That(restoredMapViewState.Selection.ObjectId).IsEqualTo(selectedObject.Header.ObjectId);
            await Assert
                .That(restoredMapViewState.WorldEdit.ObjectPlacement.Mode)
                .IsEqualTo(EditorProjectMapObjectPlacementMode.PlacementPreset);
            await Assert.That(restoredMapViewState.WorldEdit.ObjectPlacement.PlacementRequest).IsNotNull();
            await Assert
                .That(restoredMapViewState.WorldEdit.ObjectPlacement.PlacementRequest!.ProtoNumber)
                .IsEqualTo(pinnedProtoNumber);
            await Assert
                .That(restoredMapViewState.WorldEdit.ObjectPlacement.SelectedPresetId)
                .IsEqualTo("pinned-guard");
            await Assert.That(restoredMapViewState.WorldEdit.ObjectPlacement.PresetLibrary.Count).IsEqualTo(1);
            await Assert
                .That(restoredMapViewState.WorldEdit.ObjectPlacement.PresetLibrary[0].Entries.Count)
                .IsEqualTo(2);
            await Assert.That(restoredMapViewState.WorldEdit.ObjectPlacement.PaletteSearchText).IsEqualTo("Pinned");
            await Assert.That(restoredMapViewState.WorldEdit.ObjectPlacement.PaletteCategory).IsEqualTo("npc");
            await Assert
                .That(restoredMapViewState.WorldEdit.ObjectPlacement.SelectedPaletteProtoNumber)
                .IsEqualTo(pinnedProtoNumber);
            await Assert
                .That(restoredMapViewState.WorldEdit.Terrain.MapPropertiesAssetPath)
                .IsEqualTo("maps/map01/map.prp");
            await Assert.That(restoredMapViewState.WorldEdit.Terrain.PaletteX).IsEqualTo(1UL);
            await Assert.That(restoredMapViewState.WorldEdit.Terrain.PaletteY).IsEqualTo(1UL);
            await Assert.That(restoredMapViewState.WorldEdit.Shell.ViewMode).IsEqualTo(EditorMapSceneViewMode.TopDown);
            await Assert.That(restoredMapViewState.WorldEdit.Shell.ViewportWidth).IsEqualTo(320d);
            await Assert.That(restoredMapViewState.WorldEdit.Shell.ViewportHeight).IsEqualTo(200d);
            await Assert.That(restoredMapViewState.WorldEdit.Shell.ObjectPaletteSearchText).IsEqualTo("Pinned");
            await Assert.That(restoredMapViewState.WorldEdit.Shell.ObjectPaletteCategory).IsEqualTo("npc");
            await Assert.That(restoredMapViewState.WorldEdit.Shell.IncludeTrackedPlacementPreview).IsFalse();
            await Assert
                .That(restoredMapViewState.WorldEdit.Inspector.TargetMode)
                .IsEqualTo(EditorProjectMapObjectInspectorTargetMode.ProtoDefinition);
            await Assert.That(restoredMapViewState.WorldEdit.Inspector.PinnedProtoNumber).IsEqualTo(pinnedProtoNumber);
            await Assert
                .That(restoredMapViewState.WorldEdit.Inspector.ActivePane)
                .IsEqualTo(EditorObjectInspectorPane.Generator);
            await Assert.That(restoredPaletteSummary.SearchText).IsEqualTo("Pinned");
            await Assert.That(restoredPaletteSummary.Category).IsEqualTo("npc");
            await Assert.That(restoredPaletteSummary.SelectedEntry).IsNotNull();
            await Assert.That(restoredPaletteSummary.SelectedEntry!.ProtoNumber).IsEqualTo(pinnedProtoNumber);
            await Assert
                .That(restoredPlacementTool.ToolState.Mode)
                .IsEqualTo(EditorProjectMapObjectPlacementMode.PlacementPreset);
            await Assert.That(restoredPlacementTool.SelectedPreset).IsNotNull();
            await Assert.That(restoredPlacementTool.SelectedPreset!.PresetId).IsEqualTo("pinned-guard");
            await Assert.That(restoredPlacementTool.EffectivePlacementSet).IsNotNull();
            await Assert.That(restoredPlacementTool.EffectivePlacementSet!.Entries.Count).IsEqualTo(2);
            await Assert.That(restoredPlacementTool.ResolvedPaletteEntries.Count).IsEqualTo(2);
            await Assert.That(restoredInspector.TargetKind).IsEqualTo(EditorObjectInspectorTargetKind.ProtoDefinition);
            await Assert.That(restoredInspector.ProtoNumber).IsEqualTo(pinnedProtoNumber);
            await Assert
                .That(restoredShell.ObjectInspectorState.ActivePane)
                .IsEqualTo(EditorObjectInspectorPane.Generator);
            await Assert
                .That(restoredShell.ObjectInspector.TargetKind)
                .IsEqualTo(EditorObjectInspectorTargetKind.ProtoDefinition);
            await Assert.That(restoredShell.ObjectInspector.ProtoNumber).IsEqualTo(pinnedProtoNumber);
            await Assert
                .That(restoredShell.ObjectPlacementTool.ToolState.Mode)
                .IsEqualTo(EditorProjectMapObjectPlacementMode.PlacementPreset);
            await Assert.That(restoredShell.ObjectPlacementTool.SelectedPreset).IsNotNull();
            await Assert.That(restoredShell.ObjectPlacementTool.SelectedPreset!.PresetId).IsEqualTo("pinned-guard");
            await Assert.That(restoredShell.TerrainPalette.SelectedEntry).IsNotNull();
            await Assert.That(restoredShell.TerrainPalette.SelectedEntry!.ArtId.Value).IsEqualTo(203u);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);

            if (Directory.Exists(projectDir))
                Directory.Delete(projectDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadSessionAsync_ReopensTrackedPlacementPresetLibraryAfterReplaceSelectAndRemoveMutations()
    {
        const int guardProtoNumber = 1001;
        const int wolfProtoNumber = 1002;
        const ulong sectorKey = 101334386389UL;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var projectDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var projectPath = Path.Combine(projectDir, "preset-mutations.arcnet.json");
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));
        Directory.CreateDirectory(projectDir);

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(guardProtoNumber),
                Path.Combine(contentDir, "proto", "001001 - Guard.pro")
            );
            ProtoFormat.WriteToFile(MakeProto(wolfProtoNumber), Path.Combine(contentDir, "proto", "001002 - Wolf.pro"));

            MapProperties mapProperties = new()
            {
                ArtId = 200,
                Unused = 0,
                LimitX = 2,
                LimitY = 2,
            };
            MapPropertiesFormat.WriteToFile(in mapProperties, Path.Combine(contentDir, "maps", "map01", "map.prp"));
            SectorFormat.WriteToFile(
                new SectorBuilder(MakeSector()).SetTile(0, 0, 201u).Build(),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var guardPostPreset = EditorObjectPalettePlacementPreset.Create(
                "guard-post",
                "Guard Post",
                entries: [EditorObjectPalettePlacementRequest.Place(guardProtoNumber, rotation: 1.5f)]
            );
            var wolfPackPreset = EditorObjectPalettePlacementPreset.Create(
                "wolf-pack",
                "Wolf Pack",
                entries: [EditorObjectPalettePlacementRequest.Place(wolfProtoNumber, deltaTileX: 1)]
            );
            var updatedGuardPostPreset = EditorObjectPalettePlacementPreset.Create(
                "guard-post",
                "Guard Post",
                entries:
                [
                    EditorObjectPalettePlacementRequest.Place(guardProtoNumber, rotation: 2.5f),
                    EditorObjectPalettePlacementRequest.Place(wolfProtoNumber, deltaTileY: 1),
                ]
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(new EditorProjectMapViewState { Id = "map-view-1", MapName = "map01" });
            _ = session.SetTrackedObjectPlacementPresetLibrary(
                "map-view-1",
                [wolfPackPreset, guardPostPreset],
                selectedPresetId: "wolf-pack",
                activateTool: true
            );
            _ = session.SelectTrackedObjectPlacementPreset("map-view-1", "guard-post");
            _ = session.SetTrackedObjectPlacementPreset("map-view-1", updatedGuardPostPreset);
            await Assert.That(session.RemoveTrackedObjectPlacementPreset("map-view-1", "wolf-pack")).IsTrue();

            await EditorProjectStore.SaveAsync(projectPath, session.CreateProject());

            var reloadedProject = await EditorProjectStore.LoadAsync(projectPath);
            var load = await reloadedProject.LoadSessionWithRestoreResultAsync();
            var restoredSession = load.Session;
            var restoredMapViewState = restoredSession.GetMapViewStates().Single();
            var restoredPlacementTool = restoredSession.GetTrackedObjectPlacementToolSummary("map-view-1");
            var restoredShell = restoredSession.CreateTrackedMapWorldEditShell("map-view-1");

            await Assert
                .That(restoredMapViewState.WorldEdit.ObjectPlacement.Mode)
                .IsEqualTo(EditorProjectMapObjectPlacementMode.PlacementPreset);
            await Assert.That(restoredMapViewState.WorldEdit.ObjectPlacement.SelectedPresetId).IsEqualTo("guard-post");
            await Assert.That(restoredMapViewState.WorldEdit.ObjectPlacement.PresetLibrary.Count).IsEqualTo(1);
            await Assert
                .That(restoredMapViewState.WorldEdit.ObjectPlacement.PresetLibrary[0].PresetId)
                .IsEqualTo("guard-post");
            await Assert
                .That(restoredMapViewState.WorldEdit.ObjectPlacement.PresetLibrary[0].Entries.Count)
                .IsEqualTo(2);
            await Assert.That(restoredSession.FindTrackedObjectPlacementPreset("map-view-1", "wolf-pack")).IsNull();
            await Assert
                .That(restoredPlacementTool.ToolState.Mode)
                .IsEqualTo(EditorProjectMapObjectPlacementMode.PlacementPreset);
            await Assert.That(restoredPlacementTool.SelectedPreset).IsNotNull();
            await Assert.That(restoredPlacementTool.SelectedPreset!.PresetId).IsEqualTo("guard-post");
            await Assert.That(restoredPlacementTool.EffectivePlacementSet).IsNotNull();
            await Assert.That(restoredPlacementTool.EffectivePlacementSet!.Entries.Count).IsEqualTo(2);
            await Assert.That(restoredPlacementTool.ResolvedPaletteEntries.Count).IsEqualTo(2);
            await Assert.That(restoredPlacementTool.ResolvedPaletteEntries[0].ProtoNumber).IsEqualTo(guardProtoNumber);
            await Assert.That(restoredPlacementTool.ResolvedPaletteEntries[1].ProtoNumber).IsEqualTo(wolfProtoNumber);
            await Assert.That(restoredShell.ObjectPlacementTool.SelectedPreset).IsNotNull();
            await Assert.That(restoredShell.ObjectPlacementTool.SelectedPreset!.PresetId).IsEqualTo("guard-post");
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);

            if (Directory.Exists(projectDir))
                Directory.Delete(projectDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadSessionAsync_ReopensSaveBackedSelectedObjectInspectorWorkflowAcrossFullInspectorSlice()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var saveDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var projectDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var projectPath = Path.Combine(projectDir, "save-backed-workflow.arcnet.json");
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));
        Directory.CreateDirectory(saveDir);
        Directory.CreateDirectory(projectDir);

        try
        {
            ProtoFormat.WriteToFile(
                MakeNpcProto(protoNumber),
                Path.Combine(contentDir, "proto", "001001 - InspectorTarget.pro")
            );
            ScriptFormat.WriteToFile(
                new ScriptBuilder().WithDescription("Examine script").AddCondition(ScriptConditionType.True).Build(),
                Path.Combine(contentDir, "scr", "00077Guard.scr")
            );
            SectorFormat.WriteToFile(MakeSector(), Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec"));

            var selectedObject = new CharacterBuilder(
                ObjectType.Npc,
                new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid()),
                MakeProtoId(protoNumber)
            )
                .WithLocation(5, 6)
                .WithHitPoints(80)
                .WithFatigue(70, 8)
                .WithProperty(
                    ObjectPropertyFactory.ForInt32(ObjectField.ObjFFlags, unchecked((int)ObjFFlags.Inventory))
                )
                .WithProperty(
                    ObjectPropertyFactory.ForInt32(
                        ObjectField.ObjFCritterFlags,
                        unchecked((int)ObjFCritterFlags.Animal)
                    )
                )
                .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFLightFlags, 1))
                .WithProperty(MakeArtProperty(ObjectField.ObjFLightAid, 0x100u))
                .WithProperty(MakeColorProperty(ObjectField.ObjFLightColor, 0x01, 0x02, 0x03))
                .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFNpcGeneratorData, 5))
                .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFBlitAlpha, 10))
                .Build();

            SaveGameWriter.Save(
                CreateLoadedSaveWithSector(
                    CreateMinimalLoadedSave(
                        new SaveInfo
                        {
                            ModuleName = "Arcanum",
                            LeaderName = "WorkspacePc",
                            DisplayName = "Save-backed reopen",
                            MapId = 1,
                            GameTimeDays = 0,
                            GameTimeMs = 0,
                            LeaderPortraitId = 1,
                            LeaderLevel = 1,
                            LeaderTileX = 0,
                            LeaderTileY = 0,
                            StoryState = 0,
                        }
                    ),
                    sectorAssetPath,
                    new SectorBuilder(MakeSector(selectedObject)).SetTile(5, 6, 201u).Build()
                ),
                saveDir,
                "slot0001"
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(
                contentDir,
                new EditorWorkspaceLoadOptions { SaveFolder = saveDir, SaveSlotName = "slot0001" }
            );
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    ViewId = "map-scene",
                    Camera = new EditorProjectMapCameraState
                    {
                        CenterTileX = 5d,
                        CenterTileY = 6d,
                        Zoom = 1.25d,
                    },
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                }
            );

            _ = session.SetTrackedObjectInspectorState(
                "map-view-1",
                new EditorProjectMapObjectInspectorState { ActivePane = EditorObjectInspectorPane.Blending }
            );
            _ = session.SetTrackedObjectInspectorFlags(
                "map-view-1",
                new EditorObjectInspectorFlagsUpdate
                {
                    ObjectFlags = ObjFFlags.Flat | ObjFFlags.Translucent,
                    CritterFlags = ObjFCritterFlags.Undead | ObjFCritterFlags.NoFlee,
                }
            );
            _ = session.SetTrackedObjectInspectorCritterProgression(
                "map-view-1",
                new EditorObjectInspectorCritterProgressionUpdate
                {
                    FatiguePoints = 90,
                    Level = 15,
                    ExperiencePoints = 4321,
                    SkillPersuasion = 21,
                    SkillRepair = 30,
                    SpellTemporal = 99,
                    TechTherapeutics = 55,
                }
            );
            _ = session.SetTrackedObjectInspectorScriptAttachment("map-view-1", ScriptAttachmentPoint.Examine, 77);
            _ = session.SetTrackedObjectInspectorGenerator(
                "map-view-1",
                new EditorObjectInspectorGeneratorUpdate { GeneratorData = 42 }
            );
            _ = session.SetTrackedObjectInspectorBlending(
                "map-view-1",
                new EditorObjectInspectorBlendingUpdate
                {
                    BlitFlags = ObjFBlitFlags.BlendAdd,
                    BlitColor = new Color(0x44, 0x55, 0x66),
                    BlitAlpha = 77,
                    BlitScale = 88,
                    Material = 99,
                }
            );
            _ = session.SetTrackedObjectInspectorLight(
                "map-view-1",
                new EditorObjectInspectorLightUpdate
                {
                    LightFlags = 9,
                    LightArtId = new ArtId(0x234u),
                    LightColor = new Color(0x10, 0x20, 0x30),
                    OverlayLightFlags = 4,
                    OverlayLightArtIds = [7, 8],
                    OverlayLightColor = 12,
                }
            );

            _ = session.BeginChangeGroup("Persist save-backed inspector reopen workflow").SavePendingChanges();

            await EditorProjectStore.SaveAsync(projectPath, session.CreateProject());

            var reloadedProject = await EditorProjectStore.LoadAsync(projectPath);
            var load = await reloadedProject.LoadSessionWithRestoreResultAsync();
            var restoredSession = load.Session;
            var restoredMapViewState = restoredSession.GetMapViewStates().Single();
            var restoredInspector = restoredSession.GetTrackedObjectInspectorSummary("map-view-1");
            var restoredFlags = restoredSession.GetTrackedObjectInspectorFlagsSummary("map-view-1");
            var restoredProgression = restoredSession.GetTrackedObjectInspectorCritterProgressionSummary("map-view-1");
            var restoredScripts = restoredSession.GetTrackedObjectInspectorScriptAttachmentsSummary("map-view-1");
            var restoredLight = restoredSession.GetTrackedObjectInspectorLightSummary("map-view-1");
            var restoredGenerator = restoredSession.GetTrackedObjectInspectorGeneratorSummary("map-view-1");
            var restoredBlending = restoredSession.GetTrackedObjectInspectorBlendingSummary("map-view-1");
            var restoredShell = restoredSession.CreateTrackedMapWorldEditShell("map-view-1");
            var restoredExamine = restoredScripts.Attachments.Single(attachment =>
                attachment.AttachmentPoint == ScriptAttachmentPoint.Examine
            );
            var restoredSelection = restoredMapViewState.Selection;

            await Assert.That(load.Restore.RestoredProjectState.MapViewStates.Count).IsEqualTo(1);
            await Assert.That(restoredSession.Workspace.SaveFolder).IsEqualTo(saveDir);
            await Assert.That(restoredSession.Workspace.SaveSlotName).IsEqualTo("slot0001");
            await Assert.That(restoredMapViewState.MapName).IsEqualTo("map01");
            await Assert.That(restoredMapViewState.Camera.Zoom).IsEqualTo(1.25d);
            await Assert.That(restoredSelection).IsNotNull();
            await Assert.That(restoredSelection!.SectorAssetPath).IsEqualTo(sectorAssetPath);
            await Assert.That(restoredSelection.ObjectId).IsEqualTo(selectedObject.Header.ObjectId);
            await Assert
                .That(restoredMapViewState.WorldEdit!.Inspector.ActivePane)
                .IsEqualTo(EditorObjectInspectorPane.Blending);
            await Assert.That(restoredInspector.TargetKind).IsEqualTo(EditorObjectInspectorTargetKind.SelectedObject);
            await Assert.That(restoredInspector.SelectionSummary!.SelectedObjects.Count).IsEqualTo(1);
            await Assert
                .That(restoredShell.ObjectInspector!.TargetKind)
                .IsEqualTo(EditorObjectInspectorTargetKind.SelectedObject);
            await Assert
                .That(restoredShell.ObjectInspectorState!.ActivePane)
                .IsEqualTo(EditorObjectInspectorPane.Blending);
            await Assert.That(restoredFlags.ObjectFlags).IsEqualTo(ObjFFlags.Flat | ObjFFlags.Translucent);
            await Assert.That(restoredFlags.CritterFlags).IsEqualTo(ObjFCritterFlags.Undead | ObjFCritterFlags.NoFlee);
            await Assert.That(restoredProgression.FatiguePoints).IsEqualTo(90);
            await Assert.That(restoredProgression.Level).IsEqualTo(15);
            await Assert.That(restoredProgression.ExperiencePoints).IsEqualTo(4321);
            await Assert.That(restoredProgression.SkillPersuasion).IsEqualTo(21);
            await Assert.That(restoredProgression.SkillRepair).IsEqualTo(30);
            await Assert.That(restoredProgression.SpellTemporal).IsEqualTo(99);
            await Assert.That(restoredProgression.TechTherapeutics).IsEqualTo(55);
            await Assert.That(restoredExamine.ScriptId).IsEqualTo(77);
            await Assert.That(restoredExamine.IsMissingScript).IsFalse();
            await Assert.That(restoredLight.LightFlags).IsEqualTo(9);
            await Assert.That(restoredLight.LightArtId).IsEqualTo(new ArtId(0x234u));
            await Assert.That(restoredLight.LightColor).IsEqualTo(new Color(0x10, 0x20, 0x30));
            await Assert.That(restoredLight.OverlayLightFlags).IsEqualTo(4);
            await Assert.That(restoredLight.OverlayLightArtIds).IsEquivalentTo([7, 8]);
            await Assert.That(restoredLight.OverlayLightColor).IsEqualTo(12);
            await Assert.That(restoredGenerator.GeneratorData).IsEqualTo(42);
            await Assert.That(restoredBlending.BlitFlags).IsEqualTo(ObjFBlitFlags.BlendAdd);
            await Assert.That(restoredBlending.BlitColor).IsEqualTo(new Color(0x44, 0x55, 0x66));
            await Assert.That(restoredBlending.BlitAlpha).IsEqualTo(77);
            await Assert.That(restoredBlending.BlitScale).IsEqualTo(88);
            await Assert.That(restoredBlending.Material).IsEqualTo(99);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);

            if (Directory.Exists(saveDir))
                Directory.Delete(saveDir, recursive: true);

            if (Directory.Exists(projectDir))
                Directory.Delete(projectDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadSessionAsync_ReopensPinnedProtoInspectorWorkflowAcrossFullInspectorSlice()
    {
        const int selectedProtoNumber = 1001;
        const int pinnedProtoNumber = 1002;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var projectDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var projectPath = Path.Combine(projectDir, "proto-inspector-workflow.arcnet.json");
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));
        Directory.CreateDirectory(projectDir);

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(selectedProtoNumber),
                Path.Combine(contentDir, "proto", "001001 - Selected.pro")
            );
            ProtoFormat.WriteToFile(
                MakeNpcProto(pinnedProtoNumber),
                Path.Combine(contentDir, "proto", "001002 - InspectorTarget.pro")
            );
            ScriptFormat.WriteToFile(
                new ScriptBuilder()
                    .WithDescription("Pinned examine script")
                    .AddCondition(ScriptConditionType.True)
                    .Build(),
                Path.Combine(contentDir, "scr", "00077Guard.scr")
            );

            MapProperties mapProperties = new()
            {
                ArtId = 200,
                Unused = 0,
                LimitX = 2,
                LimitY = 2,
            };
            MapPropertiesFormat.WriteToFile(in mapProperties, Path.Combine(contentDir, "maps", "map01", "map.prp"));

            var selectedObject = new MobDataBuilder(MakePc(selectedProtoNumber)).WithLocation(5, 6).Build();
            SectorFormat.WriteToFile(
                new SectorBuilder(MakeSector(selectedObject)).SetTile(5, 6, 201u).SetTile(1, 1, 203u).Build(),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    ViewId = "map-scene",
                    Camera = new EditorProjectMapCameraState
                    {
                        CenterTileX = 5d,
                        CenterTileY = 6d,
                        Zoom = 1.5d,
                    },
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                }
            );

            _ = session.SetTrackedObjectPlacementEntry("map-view-1", pinnedProtoNumber, rotation: 0.5f);
            _ = session.SelectTrackedObjectPaletteEntry(
                "map-view-1",
                pinnedProtoNumber,
                searchText: "Pinned",
                category: "npc"
            );
            _ = session.SetTrackedTerrainPaletteEntry("map-view-1", 1, 1, activateTool: false);
            _ = session.SetTrackedMapWorldEditShellPreferences(
                "map-view-1",
                new EditorMapWorldEditShellRequest
                {
                    ViewMode = EditorMapSceneViewMode.TopDown,
                    ViewportWidth = 320d,
                    ViewportHeight = 200d,
                    ObjectPaletteSearchText = "Pinned",
                    ObjectPaletteCategory = "npc",
                    IncludeTrackedPlacementPreview = false,
                }
            );
            _ = session.SetTrackedObjectInspectorState(
                "map-view-1",
                new EditorProjectMapObjectInspectorState
                {
                    TargetMode = EditorProjectMapObjectInspectorTargetMode.ProtoDefinition,
                    PinnedProtoNumber = pinnedProtoNumber,
                    ActivePane = EditorObjectInspectorPane.Blending,
                }
            );

            _ = session.SetProtoInspectorFlags(
                pinnedProtoNumber,
                new EditorObjectInspectorFlagsUpdate
                {
                    ObjectFlags = ObjFFlags.Flat | ObjFFlags.Translucent,
                    CritterFlags = ObjFCritterFlags.Undead | ObjFCritterFlags.NoFlee,
                }
            );
            _ = session.SetProtoInspectorCritterProgression(
                pinnedProtoNumber,
                new EditorObjectInspectorCritterProgressionUpdate
                {
                    FatiguePoints = 90,
                    Level = 15,
                    ExperiencePoints = 4321,
                    SkillPersuasion = 21,
                    SkillRepair = 30,
                    SpellTemporal = 99,
                    TechTherapeutics = 55,
                }
            );
            _ = session.SetProtoInspectorScriptAttachment(pinnedProtoNumber, ScriptAttachmentPoint.Examine, 77);
            _ = session.SetProtoInspectorGenerator(
                pinnedProtoNumber,
                new EditorObjectInspectorGeneratorUpdate { GeneratorData = 42 }
            );
            _ = session.SetProtoInspectorBlending(
                pinnedProtoNumber,
                new EditorObjectInspectorBlendingUpdate
                {
                    BlitFlags = ObjFBlitFlags.BlendAdd,
                    BlitColor = new Color(0x44, 0x55, 0x66),
                    BlitAlpha = 77,
                    BlitScale = 88,
                    Material = 99,
                }
            );
            _ = session.SetProtoInspectorLight(
                pinnedProtoNumber,
                new EditorObjectInspectorLightUpdate
                {
                    LightFlags = 9,
                    LightArtId = new ArtId(0x234u),
                    LightColor = new Color(0x10, 0x20, 0x30),
                    OverlayLightFlags = 4,
                    OverlayLightArtIds = [7, 8],
                    OverlayLightColor = 12,
                }
            );

            _ = session.BeginChangeGroup("Persist pinned proto inspector reopen workflow").SavePendingChanges();

            await EditorProjectStore.SaveAsync(projectPath, session.CreateProject());

            var reloadedProject = await EditorProjectStore.LoadAsync(projectPath);
            var load = await reloadedProject.LoadSessionWithRestoreResultAsync();
            var restoredSession = load.Session;
            var restoredMapViewState = restoredSession.GetMapViewStates().Single();
            var restoredInspector = restoredSession.GetTrackedObjectInspectorSummary("map-view-1");
            var restoredFlags = restoredSession.GetTrackedObjectInspectorFlagsSummary("map-view-1");
            var restoredProgression = restoredSession.GetTrackedObjectInspectorCritterProgressionSummary("map-view-1");
            var restoredScripts = restoredSession.GetTrackedObjectInspectorScriptAttachmentsSummary("map-view-1");
            var restoredLight = restoredSession.GetTrackedObjectInspectorLightSummary("map-view-1");
            var restoredGenerator = restoredSession.GetTrackedObjectInspectorGeneratorSummary("map-view-1");
            var restoredBlending = restoredSession.GetTrackedObjectInspectorBlendingSummary("map-view-1");
            var restoredPaletteSummary = restoredSession.GetTrackedObjectPaletteSummary("map-view-1");
            var restoredShell = restoredSession.CreateTrackedMapWorldEditShell("map-view-1");
            var restoredExamine = restoredScripts.Attachments.Single(attachment =>
                attachment.AttachmentPoint == ScriptAttachmentPoint.Examine
            );

            await Assert.That(load.Restore.RestoredProjectState.MapViewStates.Count).IsEqualTo(1);
            await Assert.That(restoredMapViewState.MapName).IsEqualTo("map01");
            await Assert.That(restoredMapViewState.Camera.Zoom).IsEqualTo(1.5d);
            await Assert.That(restoredMapViewState.Selection).IsNotNull();
            await Assert.That(restoredMapViewState.Selection!.SectorAssetPath).IsEqualTo(sectorAssetPath);
            await Assert.That(restoredMapViewState.Selection.ObjectId).IsEqualTo(selectedObject.Header.ObjectId);
            await Assert
                .That(restoredMapViewState.WorldEdit!.Inspector.ActivePane)
                .IsEqualTo(EditorObjectInspectorPane.Blending);
            await Assert
                .That(restoredMapViewState.WorldEdit.Inspector.TargetMode)
                .IsEqualTo(EditorProjectMapObjectInspectorTargetMode.ProtoDefinition);
            await Assert.That(restoredMapViewState.WorldEdit.Inspector.PinnedProtoNumber).IsEqualTo(pinnedProtoNumber);
            await Assert.That(restoredPaletteSummary.SelectedEntry).IsNotNull();
            await Assert.That(restoredPaletteSummary.SelectedEntry!.ProtoNumber).IsEqualTo(pinnedProtoNumber);
            await Assert.That(restoredInspector.TargetKind).IsEqualTo(EditorObjectInspectorTargetKind.ProtoDefinition);
            await Assert.That(restoredInspector.ProtoNumber).IsEqualTo(pinnedProtoNumber);
            await Assert
                .That(restoredShell.ObjectInspectorState!.ActivePane)
                .IsEqualTo(EditorObjectInspectorPane.Blending);
            await Assert
                .That(restoredShell.ObjectInspector!.TargetKind)
                .IsEqualTo(EditorObjectInspectorTargetKind.ProtoDefinition);
            await Assert.That(restoredShell.ObjectInspector.ProtoNumber).IsEqualTo(pinnedProtoNumber);
            await Assert.That(restoredFlags.ObjectFlags).IsEqualTo(ObjFFlags.Flat | ObjFFlags.Translucent);
            await Assert.That(restoredFlags.CritterFlags).IsEqualTo(ObjFCritterFlags.Undead | ObjFCritterFlags.NoFlee);
            await Assert.That(restoredProgression.FatiguePoints).IsEqualTo(90);
            await Assert.That(restoredProgression.Level).IsEqualTo(15);
            await Assert.That(restoredProgression.ExperiencePoints).IsEqualTo(4321);
            await Assert.That(restoredProgression.SkillPersuasion).IsEqualTo(21);
            await Assert.That(restoredProgression.SkillRepair).IsEqualTo(30);
            await Assert.That(restoredProgression.SpellTemporal).IsEqualTo(99);
            await Assert.That(restoredProgression.TechTherapeutics).IsEqualTo(55);
            await Assert.That(restoredExamine.ScriptId).IsEqualTo(77);
            await Assert.That(restoredExamine.IsMissingScript).IsFalse();
            await Assert.That(restoredExamine.Script).IsNotNull();
            await Assert.That(restoredExamine.Script!.Description).IsEqualTo("Pinned examine script");
            await Assert.That(restoredLight.LightFlags).IsEqualTo(9);
            await Assert.That(restoredLight.LightArtId).IsEqualTo(new ArtId(0x234u));
            await Assert.That(restoredLight.LightColor).IsEqualTo(new Color(0x10, 0x20, 0x30));
            await Assert.That(restoredLight.OverlayLightFlags).IsEqualTo(4);
            await Assert.That(restoredLight.OverlayLightArtIds).IsEquivalentTo([7, 8]);
            await Assert.That(restoredLight.OverlayLightColor).IsEqualTo(12);
            await Assert.That(restoredGenerator.GeneratorData).IsEqualTo(42);
            await Assert.That(restoredBlending.BlitFlags).IsEqualTo(ObjFBlitFlags.BlendAdd);
            await Assert.That(restoredBlending.BlitColor).IsEqualTo(new Color(0x44, 0x55, 0x66));
            await Assert.That(restoredBlending.BlitAlpha).IsEqualTo(77);
            await Assert.That(restoredBlending.BlitScale).IsEqualTo(88);
            await Assert.That(restoredBlending.Material).IsEqualTo(99);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);

            if (Directory.Exists(projectDir))
                Directory.Delete(projectDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadSessionAsync_ReopensMixedInspectorWorkflowAfterRetargetingFromPinnedProtoToSelectedObject()
    {
        const int selectedProtoNumber = 1001;
        const int pinnedProtoNumber = 1002;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var projectDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var projectPath = Path.Combine(projectDir, "mixed-inspector-workflow.arcnet.json");
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));
        Directory.CreateDirectory(projectDir);

        try
        {
            ProtoFormat.WriteToFile(
                MakeNpcProto(selectedProtoNumber),
                Path.Combine(contentDir, "proto", "001001 - Selected.pro")
            );
            ProtoFormat.WriteToFile(
                MakeNpcProto(pinnedProtoNumber),
                Path.Combine(contentDir, "proto", "001002 - Pinned.pro")
            );
            ScriptFormat.WriteToFile(
                new ScriptBuilder()
                    .WithDescription("Pinned examine script")
                    .AddCondition(ScriptConditionType.True)
                    .Build(),
                Path.Combine(contentDir, "scr", "00077Guard.scr")
            );

            var selectedObject = new CharacterBuilder(
                ObjectType.Npc,
                new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, selectedProtoNumber, Guid.NewGuid()),
                MakeProtoId(selectedProtoNumber)
            )
                .WithLocation(5, 6)
                .WithHitPoints(80)
                .WithFatigue(70, 8)
                .WithProperty(
                    ObjectPropertyFactory.ForInt32(ObjectField.ObjFFlags, unchecked((int)ObjFFlags.Inventory))
                )
                .WithProperty(
                    ObjectPropertyFactory.ForInt32(
                        ObjectField.ObjFCritterFlags,
                        unchecked((int)ObjFCritterFlags.Animal)
                    )
                )
                .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFBlitAlpha, 10))
                .Build();
            SectorFormat.WriteToFile(
                new SectorBuilder(MakeSector(selectedObject)).SetTile(5, 6, 201u).Build(),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    ViewId = "map-scene",
                    Camera = new EditorProjectMapCameraState
                    {
                        CenterTileX = 5d,
                        CenterTileY = 6d,
                        Zoom = 1.5d,
                    },
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                }
            );

            _ = session.SetTrackedObjectInspectorState(
                "map-view-1",
                new EditorProjectMapObjectInspectorState
                {
                    TargetMode = EditorProjectMapObjectInspectorTargetMode.ProtoDefinition,
                    PinnedProtoNumber = pinnedProtoNumber,
                    ActivePane = EditorObjectInspectorPane.ScriptAttachments,
                }
            );
            _ = session.SetProtoInspectorScriptAttachment(pinnedProtoNumber, ScriptAttachmentPoint.Examine, 77);
            _ = session.SetProtoInspectorLight(
                pinnedProtoNumber,
                new EditorObjectInspectorLightUpdate
                {
                    LightFlags = 3,
                    LightArtId = new ArtId(0x200u),
                    LightColor = new Color(0x20, 0x30, 0x40),
                }
            );

            _ = session.SetTrackedObjectInspectorState(
                "map-view-1",
                new EditorProjectMapObjectInspectorState
                {
                    TargetMode = EditorProjectMapObjectInspectorTargetMode.Selection,
                    ActivePane = EditorObjectInspectorPane.Blending,
                }
            );
            _ = session.SetTrackedObjectInspectorFlags(
                "map-view-1",
                new EditorObjectInspectorFlagsUpdate
                {
                    ObjectFlags = ObjFFlags.Flat | ObjFFlags.Translucent,
                    CritterFlags = ObjFCritterFlags.Undead | ObjFCritterFlags.NoFlee,
                }
            );
            _ = session.SetTrackedObjectInspectorBlending(
                "map-view-1",
                new EditorObjectInspectorBlendingUpdate
                {
                    BlitFlags = ObjFBlitFlags.BlendAdd,
                    BlitColor = new Color(0x44, 0x55, 0x66),
                    BlitAlpha = 77,
                    BlitScale = 88,
                    Material = 99,
                }
            );

            _ = session.BeginChangeGroup("Persist mixed inspector workflow").SavePendingChanges();

            await EditorProjectStore.SaveAsync(projectPath, session.CreateProject());

            var reloadedProject = await EditorProjectStore.LoadAsync(projectPath);
            var load = await reloadedProject.LoadSessionWithRestoreResultAsync();
            var restoredSession = load.Session;
            var restoredMapViewState = restoredSession.GetMapViewStates().Single();
            var restoredInspector = restoredSession.GetTrackedObjectInspectorSummary("map-view-1");
            var restoredFlags = restoredSession.GetTrackedObjectInspectorFlagsSummary("map-view-1");
            var restoredBlending = restoredSession.GetTrackedObjectInspectorBlendingSummary("map-view-1");
            var restoredShell = restoredSession.CreateTrackedMapWorldEditShell("map-view-1");
            var restoredPinnedScripts = restoredSession.Workspace.FindObjectInspectorScriptAttachmentsSummary(
                pinnedProtoNumber
            );
            var restoredPinnedLight = restoredSession.Workspace.FindObjectInspectorLightSummary(pinnedProtoNumber);
            var restoredPinnedExamine = restoredPinnedScripts!.Attachments.Single(attachment =>
                attachment.AttachmentPoint == ScriptAttachmentPoint.Examine
            );

            await Assert.That(load.Restore.RestoredProjectState.MapViewStates.Count).IsEqualTo(1);
            await Assert.That(restoredMapViewState.MapName).IsEqualTo("map01");
            await Assert.That(restoredMapViewState.Selection).IsNotNull();
            await Assert.That(restoredMapViewState.Selection!.ObjectId).IsEqualTo(selectedObject.Header.ObjectId);
            await Assert
                .That(restoredMapViewState.WorldEdit.Inspector.TargetMode)
                .IsEqualTo(EditorProjectMapObjectInspectorTargetMode.Selection);
            await Assert
                .That(restoredMapViewState.WorldEdit.Inspector.ActivePane)
                .IsEqualTo(EditorObjectInspectorPane.Blending);
            await Assert.That(restoredInspector.TargetKind).IsEqualTo(EditorObjectInspectorTargetKind.SelectedObject);
            await Assert.That(restoredFlags.ObjectFlags).IsEqualTo(ObjFFlags.Flat | ObjFFlags.Translucent);
            await Assert.That(restoredFlags.CritterFlags).IsEqualTo(ObjFCritterFlags.Undead | ObjFCritterFlags.NoFlee);
            await Assert.That(restoredBlending.BlitFlags).IsEqualTo(ObjFBlitFlags.BlendAdd);
            await Assert.That(restoredBlending.BlitColor).IsEqualTo(new Color(0x44, 0x55, 0x66));
            await Assert.That(restoredBlending.BlitAlpha).IsEqualTo(77);
            await Assert.That(restoredBlending.BlitScale).IsEqualTo(88);
            await Assert.That(restoredBlending.Material).IsEqualTo(99);
            await Assert
                .That(restoredShell.ObjectInspectorState.TargetMode)
                .IsEqualTo(EditorProjectMapObjectInspectorTargetMode.Selection);
            await Assert
                .That(restoredShell.ObjectInspector.TargetKind)
                .IsEqualTo(EditorObjectInspectorTargetKind.SelectedObject);
            await Assert
                .That(restoredShell.ObjectInspectorFlags.ObjectFlags)
                .IsEqualTo(ObjFFlags.Flat | ObjFFlags.Translucent);
            await Assert.That(restoredShell.ObjectInspectorBlending.BlitAlpha).IsEqualTo(77);
            await Assert.That(restoredPinnedExamine.ScriptId).IsEqualTo(77);
            await Assert.That(restoredPinnedExamine.IsMissingScript).IsFalse();
            await Assert.That(restoredPinnedExamine.Script).IsNotNull();
            await Assert.That(restoredPinnedExamine.Script!.Description).IsEqualTo("Pinned examine script");
            await Assert.That(restoredPinnedLight).IsNotNull();
            await Assert.That(restoredPinnedLight!.LightFlags).IsEqualTo(3);
            await Assert.That(restoredPinnedLight.LightArtId).IsEqualTo(new ArtId(0x200u));
            await Assert.That(restoredPinnedLight.LightColor).IsEqualTo(new Color(0x20, 0x30, 0x40));
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);

            if (Directory.Exists(projectDir))
                Directory.Delete(projectDir, recursive: true);
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
                        WorldEdit = new EditorProjectMapWorldEditState
                        {
                            ActiveTool = EditorProjectMapWorldEditActiveTool.ObjectPlacement,
                            ObjectPlacement = new EditorProjectMapObjectPlacementToolState
                            {
                                PaletteSearchText = "  wolf  ",
                                PaletteCategory = "  pc ",
                                SelectedPaletteProtoNumber = 1002,
                            },
                            Shell = new EditorProjectMapWorldEditShellState
                            {
                                ViewMode = EditorMapSceneViewMode.TopDown,
                                ViewportWidth = 300d,
                                ViewportHeight = 180d,
                                ObjectPaletteSearchText = "guard",
                                ObjectPaletteCategory = "pc",
                                IncludeTrackedPlacementPreview = false,
                            },
                            Inspector = new EditorProjectMapObjectInspectorState
                            {
                                TargetMode = EditorProjectMapObjectInspectorTargetMode.ProtoDefinition,
                                PinnedProtoNumber = 1002,
                                ActivePane = EditorObjectInspectorPane.ScriptAttachments,
                            },
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
            await Assert
                .That(session.GetMapViewStates()[0].WorldEdit.Shell.ViewMode)
                .IsEqualTo(EditorMapSceneViewMode.TopDown);
            await Assert.That(session.GetMapViewStates()[0].WorldEdit.Shell.ViewportWidth).IsEqualTo(300d);
            await Assert.That(session.GetMapViewStates()[0].WorldEdit.Shell.ViewportHeight).IsEqualTo(180d);
            await Assert.That(session.GetMapViewStates()[0].WorldEdit.Shell.ObjectPaletteSearchText).IsEqualTo("guard");
            await Assert.That(session.GetMapViewStates()[0].WorldEdit.Shell.ObjectPaletteCategory).IsEqualTo("pc");
            await Assert.That(session.GetMapViewStates()[0].WorldEdit.Shell.IncludeTrackedPlacementPreview).IsFalse();
            await Assert
                .That(session.GetMapViewStates()[0].WorldEdit.Inspector.TargetMode)
                .IsEqualTo(EditorProjectMapObjectInspectorTargetMode.ProtoDefinition);
            await Assert.That(session.GetMapViewStates()[0].WorldEdit.Inspector.PinnedProtoNumber).IsEqualTo(1002);
            await Assert
                .That(session.GetMapViewStates()[0].WorldEdit.Inspector.ActivePane)
                .IsEqualTo(EditorObjectInspectorPane.ScriptAttachments);
            await Assert
                .That(session.GetMapViewStates()[0].WorldEdit.ObjectPlacement.PaletteSearchText)
                .IsEqualTo("wolf");
            await Assert.That(session.GetMapViewStates()[0].WorldEdit.ObjectPlacement.PaletteCategory).IsEqualTo("pc");
            await Assert
                .That(session.GetMapViewStates()[0].WorldEdit.ObjectPlacement.SelectedPaletteProtoNumber)
                .IsEqualTo(1002);
            await Assert
                .That(roundTripped.MapViewStates[0].WorldEdit.Shell.ViewMode)
                .IsEqualTo(EditorMapSceneViewMode.TopDown);
            await Assert.That(roundTripped.MapViewStates[0].WorldEdit.Shell.ViewportWidth).IsEqualTo(300d);
            await Assert.That(roundTripped.MapViewStates[0].WorldEdit.Shell.ViewportHeight).IsEqualTo(180d);
            await Assert.That(roundTripped.MapViewStates[0].WorldEdit.Shell.ObjectPaletteSearchText).IsEqualTo("guard");
            await Assert.That(roundTripped.MapViewStates[0].WorldEdit.Shell.ObjectPaletteCategory).IsEqualTo("pc");
            await Assert.That(roundTripped.MapViewStates[0].WorldEdit.Shell.IncludeTrackedPlacementPreview).IsFalse();
            await Assert
                .That(roundTripped.MapViewStates[0].WorldEdit.Inspector.TargetMode)
                .IsEqualTo(EditorProjectMapObjectInspectorTargetMode.ProtoDefinition);
            await Assert.That(roundTripped.MapViewStates[0].WorldEdit.Inspector.PinnedProtoNumber).IsEqualTo(1002);
            await Assert
                .That(roundTripped.MapViewStates[0].WorldEdit.Inspector.ActivePane)
                .IsEqualTo(EditorObjectInspectorPane.ScriptAttachments);
            await Assert
                .That(roundTripped.MapViewStates[0].WorldEdit.ObjectPlacement.PaletteSearchText)
                .IsEqualTo("wolf");
            await Assert.That(roundTripped.MapViewStates[0].WorldEdit.ObjectPlacement.PaletteCategory).IsEqualTo("pc");
            await Assert
                .That(roundTripped.MapViewStates[0].WorldEdit.ObjectPlacement.SelectedPaletteProtoNumber)
                .IsEqualTo(1002);

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
                            PaletteSearchText = "guards",
                            PaletteCategory = "pc",
                            SelectedPaletteProtoNumber = 1002,
                        },
                        Shell = new EditorProjectMapWorldEditShellState
                        {
                            ViewMode = EditorMapSceneViewMode.TopDown,
                            ViewportWidth = 480d,
                            ViewportHeight = 270d,
                            ObjectPaletteSearchText = "guards",
                            ObjectPaletteCategory = "pc",
                            IncludeTrackedPlacementPreview = false,
                        },
                        Inspector = new EditorProjectMapObjectInspectorState
                        {
                            TargetMode = EditorProjectMapObjectInspectorTargetMode.ProtoDefinition,
                            PinnedProtoNumber = 1002,
                            ActivePane = EditorObjectInspectorPane.Light,
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
            await Assert.That(loaded.MapViewStates[0].WorldEdit.ObjectPlacement.PaletteSearchText).IsEqualTo("guards");
            await Assert.That(loaded.MapViewStates[0].WorldEdit.ObjectPlacement.PaletteCategory).IsEqualTo("pc");
            await Assert
                .That(loaded.MapViewStates[0].WorldEdit.ObjectPlacement.SelectedPaletteProtoNumber)
                .IsEqualTo(1002);
            await Assert
                .That(loaded.MapViewStates[0].WorldEdit.Shell.ViewMode)
                .IsEqualTo(EditorMapSceneViewMode.TopDown);
            await Assert.That(loaded.MapViewStates[0].WorldEdit.Shell.ViewportWidth).IsEqualTo(480d);
            await Assert.That(loaded.MapViewStates[0].WorldEdit.Shell.ViewportHeight).IsEqualTo(270d);
            await Assert.That(loaded.MapViewStates[0].WorldEdit.Shell.ObjectPaletteSearchText).IsEqualTo("guards");
            await Assert.That(loaded.MapViewStates[0].WorldEdit.Shell.ObjectPaletteCategory).IsEqualTo("pc");
            await Assert.That(loaded.MapViewStates[0].WorldEdit.Shell.IncludeTrackedPlacementPreview).IsFalse();
            await Assert
                .That(loaded.MapViewStates[0].WorldEdit.Inspector.TargetMode)
                .IsEqualTo(EditorProjectMapObjectInspectorTargetMode.ProtoDefinition);
            await Assert.That(loaded.MapViewStates[0].WorldEdit.Inspector.PinnedProtoNumber).IsEqualTo(1002);
            await Assert
                .That(loaded.MapViewStates[0].WorldEdit.Inspector.ActivePane)
                .IsEqualTo(EditorObjectInspectorPane.Light);
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

    [Test]
    public async Task LoadSessionAsync_ReopensGenericViewAndToolProjectStateEntries()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var projectDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var projectPath = Path.Combine(projectDir, "layout.arcnet.json");
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(projectDir);

        try
        {
            ScriptFormat.WriteToFile(
                new ScriptBuilder().WithDescription("Guard").AddCondition(ScriptConditionType.True).Build(),
                Path.Combine(contentDir, "scr", "00077Guard.scr")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();

            _ = session.SetMapViewState(new EditorProjectMapViewState { Id = "map-view-1", MapName = "map01" });
            _ = session.SetViewState(
                new EditorProjectViewState
                {
                    Id = "map-layout-1",
                    ViewId = "map-scene",
                    Properties = new Dictionary<string, string?>
                    {
                        ["mapViewStateId"] = "map-view-1",
                        ["dock"] = "center",
                    },
                }
            );
            _ = session.SetToolState(
                new EditorProjectToolState
                {
                    ToolId = "object-inspector",
                    ScopeId = "right-sidebar",
                    Properties = new Dictionary<string, string?>
                    {
                        ["mapViewStateId"] = "map-view-1",
                        ["collapsed"] = "false",
                    },
                }
            );

            await EditorProjectStore.SaveAsync(projectPath, session.CreateProject());

            var reloadedProject = await EditorProjectStore.LoadAsync(projectPath);
            var load = await reloadedProject.LoadSessionWithRestoreResultAsync();
            var restoredSession = load.Session;
            var restoredProject = restoredSession.CreateProject();
            var restoredViewState = restoredSession.GetViewStates().Single();
            var restoredToolState = restoredSession.GetToolStates().Single();

            await Assert.That(load.Restore.RestoredProjectState.ViewStates.Count).IsEqualTo(1);
            await Assert.That(load.Restore.RestoredProjectState.ToolStates.Count).IsEqualTo(1);
            await Assert.That(load.BootstrapSummary.ProjectState.ViewStates.Count).IsEqualTo(1);
            await Assert.That(load.BootstrapSummary.ProjectState.ToolStates.Count).IsEqualTo(1);
            await Assert.That(restoredViewState.Id).IsEqualTo("map-layout-1");
            await Assert.That(restoredViewState.ViewId).IsEqualTo("map-scene");
            await Assert.That(restoredViewState.Properties["mapViewStateId"]).IsEqualTo("map-view-1");
            await Assert.That(restoredViewState.Properties["dock"]).IsEqualTo("center");
            await Assert.That(restoredToolState.ToolId).IsEqualTo("object-inspector");
            await Assert.That(restoredToolState.ScopeId).IsEqualTo("right-sidebar");
            await Assert.That(restoredToolState.Properties["mapViewStateId"]).IsEqualTo("map-view-1");
            await Assert.That(restoredToolState.Properties["collapsed"]).IsEqualTo("false");
            await Assert.That(restoredProject.ViewStates.Count).IsEqualTo(1);
            await Assert.That(restoredProject.ToolStates.Count).IsEqualTo(1);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);

            if (Directory.Exists(projectDir))
                Directory.Delete(projectDir, recursive: true);
        }
    }

    private static GameObjectGuid MakeProtoId(int protoNumber)
    {
        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, protoNumber);
        return new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, new Guid(bytes));
    }

    private static MobData MakePc(int protoNumber = 1)
    {
        var protoId = MakeProtoId(protoNumber);
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid());
        return new CharacterBuilder(ObjectType.Pc, objectId, protoId)
            .WithPlayerName("WorkspacePc")
            .WithHitPoints(80)
            .Build();
    }

    private static MobData MakeNpc(int protoNumber = 1)
    {
        var protoId = MakeProtoId(protoNumber);
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid());
        return new CharacterBuilder(ObjectType.Npc, objectId, protoId).WithHitPoints(80).Build();
    }

    private static ProtoData MakeProto(int protoNumber)
    {
        var mob = MakePc(protoNumber);
        return new ProtoData
        {
            Header = new GameObjectHeader
            {
                Version = mob.Header.Version,
                ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeBlocked, 0, 0, Guid.Empty),
                ObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid()),
                GameObjectType = mob.Header.GameObjectType,
                PropCollectionItems = 0,
                Bitmap = [.. mob.Header.Bitmap],
            },
            Properties = [.. mob.Properties],
        };
    }

    private static ProtoData MakeNpcProto(int protoNumber)
    {
        var mob = MakeNpc(protoNumber);
        return new ProtoData
        {
            Header = new GameObjectHeader
            {
                Version = mob.Header.Version,
                ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeBlocked, 0, 0, Guid.Empty),
                ObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid()),
                GameObjectType = mob.Header.GameObjectType,
                PropCollectionItems = 0,
                Bitmap = [.. mob.Header.Bitmap],
            },
            Properties = [.. mob.Properties],
        };
    }

    private static ObjectProperty MakeArtProperty(ObjectField field, uint artId) =>
        ObjectPropertyFactory.ForInt32(field, unchecked((int)artId));

    private static ObjectProperty MakeColorProperty(ObjectField field, byte r, byte g, byte b) =>
        new() { Field = field, RawBytes = [r, g, b] };

    private static Sector MakeSector(params MobData[] objects) =>
        new()
        {
            Lights = [],
            Tiles = new uint[4096],
            HasRoofs = false,
            Roofs = null,
            SectorScript = null,
            TileScripts = [],
            TownmapInfo = 0,
            AptitudeAdjustment = 0,
            LightSchemeIdx = 0,
            SoundList = new SectorSoundList
            {
                Flags = 0,
                MusicSchemeIdx = -1,
                AmbientSchemeIdx = -1,
            },
            BlockMask = new uint[128],
            Objects = objects,
        };

    private static LoadedSave CreateMinimalLoadedSave(SaveInfo info)
    {
        var mobBytes = MobFormat.WriteToArray(MakePc());
        var jmpBytes = JmpFormat.WriteToArray(new JmpFile { Jumps = [] });
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["maps/map01/mobile/G_pc.mob"] = mobBytes,
            ["maps/map01/map.jmp"] = jmpBytes,
        };

        var index = new SaveIndex
        {
            Root =
            [
                new TfaiDirectoryEntry
                {
                    Name = "maps",
                    Children =
                    [
                        new TfaiDirectoryEntry
                        {
                            Name = "map01",
                            Children =
                            [
                                new TfaiDirectoryEntry
                                {
                                    Name = "mobile",
                                    Children = [new TfaiFileEntry { Name = "G_pc.mob", Size = mobBytes.Length }],
                                },
                                new TfaiFileEntry { Name = "map.jmp", Size = jmpBytes.Length },
                            ],
                        },
                    ],
                },
            ],
        };

        return SaveGameLoader.LoadFromParsed(info, index, TfafFormat.Pack(index, files));
    }

    private static LoadedSave CreateLoadedSaveWithSector(LoadedSave baseSave, string sectorPath, Sector sector)
    {
        var sectorBytes = SectorFormat.WriteToArray(sector);
        var files = new Dictionary<string, byte[]>(baseSave.Files, StringComparer.OrdinalIgnoreCase)
        {
            [sectorPath] = sectorBytes,
        };
        var mobBytes = files["maps/map01/mobile/G_pc.mob"];
        var jmpBytes = files["maps/map01/map.jmp"];

        var index = new SaveIndex
        {
            Root =
            [
                new TfaiDirectoryEntry
                {
                    Name = "maps",
                    Children =
                    [
                        new TfaiDirectoryEntry
                        {
                            Name = "map01",
                            Children =
                            [
                                new TfaiDirectoryEntry
                                {
                                    Name = "mobile",
                                    Children = [new TfaiFileEntry { Name = "G_pc.mob", Size = mobBytes.Length }],
                                },
                                new TfaiFileEntry { Name = "map.jmp", Size = jmpBytes.Length },
                                new TfaiFileEntry { Name = Path.GetFileName(sectorPath), Size = sectorBytes.Length },
                            ],
                        },
                    ],
                },
            ],
        };

        return SaveGameLoader.LoadFromParsed(baseSave.Info, index, TfafFormat.Pack(index, files));
    }
}
