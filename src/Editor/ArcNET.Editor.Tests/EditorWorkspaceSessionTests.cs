using System.Buffers.Binary;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameData;
using ArcNET.GameObjects;

namespace ArcNET.Editor.Tests;

public sealed class EditorWorkspaceSessionTests
{
    [Test]
    public async Task CreateSession_ReusesWorkspaceBackedDialogAndScriptEditors_AndReportsPendingChanges()
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

            await Assert.That(workspace.FindDialog("dlg\\00001Guard.dlg")).IsNotNull();
            await Assert.That(workspace.FindScript("scr\\00077Guard.scr")).IsNotNull();

            var dialogEditor = session.GetDialogEditor("dlg\\00001Guard.dlg");
            var sameDialogEditor = session.GetDialogEditor("dlg/00001Guard.dlg");
            var scriptEditor = session.GetScriptEditor("scr/00077Guard.scr");
            var sameScriptEditor = session.GetScriptEditor("scr\\00077Guard.scr");

            await Assert.That(ReferenceEquals(dialogEditor, sameDialogEditor)).IsTrue();
            await Assert.That(ReferenceEquals(scriptEditor, sameScriptEditor)).IsTrue();
            await Assert.That(session.HasPendingChanges).IsFalse();

            dialogEditor.AddControlEntry(20, "E:");
            scriptEditor.WithDescription("Updated guard");

            var pendingChanges = session.GetPendingChanges();

            await Assert.That(session.HasPendingChanges).IsTrue();
            await Assert.That(pendingChanges.Count).IsEqualTo(2);
            await Assert.That(pendingChanges[0].Kind).IsEqualTo(EditorSessionChangeKind.Dialog);
            await Assert.That(pendingChanges[0].Target).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(pendingChanges[1].Kind).IsEqualTo(EditorSessionChangeKind.Script);
            await Assert.That(pendingChanges[1].Target).IsEqualTo("scr/00077Guard.scr");
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task CreateSession_SaveEditorContributesPendingSaveChanges()
    {
        var save = CreateLoadedSave(
            new SaveInfo
            {
                ModuleName = "Arcanum",
                LeaderName = "Virgil",
                DisplayName = "Original",
                MapId = -1,
                GameTimeDays = 1,
                GameTimeMs = 2,
                LeaderPortraitId = 3,
                LeaderLevel = 4,
                LeaderTileX = 5,
                LeaderTileY = 6,
                StoryState = 0,
            }
        );
        var workspace = new EditorWorkspace
        {
            ContentDirectory = Path.Combine("game-root", "data"),
            GameDirectory = "game-root",
            GameData = new GameDataStore(),
            Save = save,
            SaveFolder = Path.Combine("game-root", "save"),
            SaveSlotName = "slot0001",
        };
        var session = workspace.CreateSession();

        var saveEditor = session.GetSaveEditor();
        var sameSaveEditor = session.GetSaveEditor();

        saveEditor.WithSaveInfo(info => info.With(displayName: "Updated"));

        var pendingChanges = session.GetPendingChanges();

        await Assert.That(ReferenceEquals(saveEditor, sameSaveEditor)).IsTrue();
        await Assert.That(saveEditor.HasPendingChanges).IsTrue();
        await Assert.That(session.HasPendingChanges).IsTrue();
        await Assert.That(pendingChanges.Count).IsEqualTo(1);
        await Assert.That(pendingChanges[0].Kind).IsEqualTo(EditorSessionChangeKind.Save);
        await Assert.That(pendingChanges[0].Target).IsEqualTo("slot0001");
    }

    [Test]
    public async Task StagedHistoryScopes_ReportAndDispatchDialogAndScriptEditorHistory()
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var dialogEditor = session.GetDialogEditor("dlg/00001Guard.dlg");
            var scriptEditor = session.GetScriptEditor("scr/00077Guard.scr");

            dialogEditor.AddControlEntry(20, "E:");
            scriptEditor.WithDescription("Updated guard");

            var scopes = session.GetStagedHistoryScopes();
            var dialogScope = scopes.Single(scope => scope.Kind == EditorSessionStagedHistoryScopeKind.Dialog);
            var scriptScope = scopes.Single(scope => scope.Kind == EditorSessionStagedHistoryScopeKind.Script);

            await Assert.That(scopes.Count).IsEqualTo(2);
            await Assert.That(dialogScope.Target).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(dialogScope.HasPendingChanges).IsTrue();
            await Assert.That(dialogScope.CanUndo).IsTrue();
            await Assert.That(dialogScope.CanRedo).IsFalse();
            await Assert.That(scriptScope.Target).IsEqualTo("scr/00077Guard.scr");
            await Assert.That(scriptScope.HasPendingChanges).IsTrue();
            await Assert.That(scriptScope.CanUndo).IsTrue();
            await Assert.That(scriptScope.CanRedo).IsFalse();

            session.UndoStagedChanges(dialogScope);
            session.UndoStagedChanges(scriptScope);

            var undoneScopes = session.GetStagedHistoryScopes();
            var undoneDialogScope = undoneScopes.Single(scope =>
                scope.Kind == EditorSessionStagedHistoryScopeKind.Dialog
            );
            var undoneScriptScope = undoneScopes.Single(scope =>
                scope.Kind == EditorSessionStagedHistoryScopeKind.Script
            );

            await Assert.That(session.HasPendingChanges).IsFalse();
            await Assert.That(dialogEditor.GetCurrentDialog().Entries.Count).IsEqualTo(1);
            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Guard");
            await Assert.That(undoneDialogScope.HasPendingChanges).IsFalse();
            await Assert.That(undoneDialogScope.CanUndo).IsFalse();
            await Assert.That(undoneDialogScope.CanRedo).IsTrue();
            await Assert.That(undoneScriptScope.HasPendingChanges).IsFalse();
            await Assert.That(undoneScriptScope.CanUndo).IsFalse();
            await Assert.That(undoneScriptScope.CanRedo).IsTrue();

            session.RedoStagedChanges(undoneDialogScope);
            session.RedoStagedChanges(undoneScriptScope);

            await Assert.That(session.HasPendingChanges).IsTrue();
            await Assert.That(dialogEditor.GetCurrentDialog().Entries.Count).IsEqualTo(2);
            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Updated guard");
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task StagedHistoryScopes_ReportAndDispatchSaveHistory()
    {
        var save = CreateLoadedSave(
            new SaveInfo
            {
                ModuleName = "Arcanum",
                LeaderName = "Virgil",
                DisplayName = "Original",
                MapId = -1,
                GameTimeDays = 1,
                GameTimeMs = 2,
                LeaderPortraitId = 3,
                LeaderLevel = 4,
                LeaderTileX = 5,
                LeaderTileY = 6,
                StoryState = 0,
            }
        );
        var workspace = new EditorWorkspace
        {
            ContentDirectory = Path.Combine("game-root", "data"),
            GameDirectory = "game-root",
            GameData = new GameDataStore(),
            Save = save,
            SaveFolder = Path.Combine("game-root", "save"),
            SaveSlotName = "slot0001",
        };
        var session = workspace.CreateSession();
        var saveEditor = session.GetSaveEditor();

        saveEditor.WithSaveInfo(info => info.With(displayName: "Updated"));

        var scope = session.GetStagedHistoryScopes().Single();

        await Assert.That(scope.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.Save);
        await Assert.That(scope.Target).IsEqualTo("slot0001");
        await Assert.That(scope.HasPendingChanges).IsTrue();
        await Assert.That(scope.CanUndo).IsTrue();
        await Assert.That(scope.CanRedo).IsFalse();

        session.UndoStagedChanges(scope);

        var undoneScope = session.GetStagedHistoryScopes().Single();

        await Assert.That(session.HasPendingChanges).IsFalse();
        await Assert.That(saveEditor.GetCurrentSaveInfo().DisplayName).IsEqualTo("Original");
        await Assert.That(undoneScope.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.Save);
        await Assert.That(undoneScope.HasPendingChanges).IsFalse();
        await Assert.That(undoneScope.CanUndo).IsFalse();
        await Assert.That(undoneScope.CanRedo).IsTrue();

        session.RedoStagedChanges(undoneScope);

        await Assert.That(session.HasPendingChanges).IsTrue();
        await Assert.That(saveEditor.GetCurrentSaveInfo().DisplayName).IsEqualTo("Updated");
    }

    [Test]
    public async Task StagedTransactionSummaries_ReportDialogAndScriptTransactions()
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.GetDialogEditor("dlg/00001Guard.dlg").AddControlEntry(20, "E:");
            session.GetScriptEditor("scr/00077Guard.scr").WithDescription("Updated guard");

            var summaries = session.GetStagedTransactionSummaries();
            var dialogSummary = summaries.Single(summary => summary.Kind == EditorSessionStagedHistoryScopeKind.Dialog);
            var scriptSummary = summaries.Single(summary => summary.Kind == EditorSessionStagedHistoryScopeKind.Script);

            await Assert.That(summaries.Count).IsEqualTo(2);
            await Assert.That(dialogSummary.Label).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(dialogSummary.Target).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(dialogSummary.AffectedTargets).IsEquivalentTo(["dlg/00001Guard.dlg"]);
            await Assert.That(dialogSummary.PendingChangeCount).IsEqualTo(1);
            await Assert.That(dialogSummary.PendingChanges[0].Kind).IsEqualTo(EditorSessionChangeKind.Dialog);
            await Assert.That(dialogSummary.PendingChanges[0].Target).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(dialogSummary.HasPendingChanges).IsTrue();
            await Assert.That(dialogSummary.CanUndo).IsTrue();
            await Assert.That(dialogSummary.CanRedo).IsFalse();
            await Assert.That(dialogSummary.CanApplyFromSession).IsTrue();
            await Assert.That(dialogSummary.CanDiscardFromSession).IsTrue();
            await Assert.That(dialogSummary.CanApplyIndividually).IsTrue();
            await Assert.That(dialogSummary.CanSaveIndividually).IsTrue();
            await Assert.That(dialogSummary.BlockingValidation.HasIssues).IsFalse();
            await Assert.That(dialogSummary.RepairCandidateCount).IsEqualTo(0);
            await Assert.That(dialogSummary.CanRepairFromSession).IsFalse();

            await Assert.That(scriptSummary.Label).IsEqualTo("scr/00077Guard.scr");
            await Assert.That(scriptSummary.Target).IsEqualTo("scr/00077Guard.scr");
            await Assert.That(scriptSummary.AffectedTargets).IsEquivalentTo(["scr/00077Guard.scr"]);
            await Assert.That(scriptSummary.PendingChangeCount).IsEqualTo(1);
            await Assert.That(scriptSummary.PendingChanges[0].Kind).IsEqualTo(EditorSessionChangeKind.Script);
            await Assert.That(scriptSummary.PendingChanges[0].Target).IsEqualTo("scr/00077Guard.scr");
            await Assert.That(scriptSummary.HasPendingChanges).IsTrue();
            await Assert.That(scriptSummary.CanUndo).IsTrue();
            await Assert.That(scriptSummary.CanRedo).IsFalse();
            await Assert.That(scriptSummary.CanApplyFromSession).IsTrue();
            await Assert.That(scriptSummary.CanDiscardFromSession).IsTrue();
            await Assert.That(scriptSummary.CanApplyIndividually).IsTrue();
            await Assert.That(scriptSummary.CanSaveIndividually).IsTrue();
            await Assert.That(scriptSummary.BlockingValidation.HasIssues).IsFalse();
            await Assert.That(scriptSummary.RepairCandidateCount).IsEqualTo(0);
            await Assert.That(scriptSummary.CanRepairFromSession).IsFalse();
            await Assert.That(session.CanApplyPendingChanges).IsTrue();
            await Assert.That(session.CanDiscardPendingChanges).IsTrue();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task StagedTransactionSummaries_ReportAndDispatchUndoRedo()
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var dialogEditor = session.GetDialogEditor("dlg/00001Guard.dlg");
            var scriptEditor = session.GetScriptEditor("scr/00077Guard.scr");

            dialogEditor.AddControlEntry(20, "E:");
            scriptEditor.WithDescription("Updated guard");

            var summaries = session.GetStagedTransactionSummaries();
            var dialogSummary = summaries.Single(summary => summary.Kind == EditorSessionStagedHistoryScopeKind.Dialog);
            var scriptSummary = summaries.Single(summary => summary.Kind == EditorSessionStagedHistoryScopeKind.Script);

            session.UndoStagedChanges(dialogSummary);
            session.UndoStagedChanges(scriptSummary);

            var undoneSummaries = session.GetStagedTransactionSummaries();
            var undoneDialogSummary = undoneSummaries.Single(summary =>
                summary.Kind == EditorSessionStagedHistoryScopeKind.Dialog
            );
            var undoneScriptSummary = undoneSummaries.Single(summary =>
                summary.Kind == EditorSessionStagedHistoryScopeKind.Script
            );

            await Assert.That(session.HasPendingChanges).IsFalse();
            await Assert.That(dialogEditor.GetCurrentDialog().Entries.Count).IsEqualTo(1);
            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Guard");
            await Assert.That(undoneDialogSummary.CanUndo).IsFalse();
            await Assert.That(undoneDialogSummary.CanRedo).IsTrue();
            await Assert.That(undoneScriptSummary.CanUndo).IsFalse();
            await Assert.That(undoneScriptSummary.CanRedo).IsTrue();

            session.RedoStagedChanges(undoneDialogSummary);
            session.RedoStagedChanges(undoneScriptSummary);

            await Assert.That(session.HasPendingChanges).IsTrue();
            await Assert.That(dialogEditor.GetCurrentDialog().Entries.Count).IsEqualTo(2);
            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Updated guard");
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task HistoryCommandSummaries_ReportDefaultUndoAndRedoCommands()
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.OpenAsset(new EditorProjectOpenAsset { AssetPath = "scr/00077Guard.scr", ViewId = "script-grid" });
            session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    ViewId = "map-scene",
                    Camera = new EditorProjectMapCameraState { Zoom = 2.5 },
                    Selection = new EditorProjectMapSelectionState(),
                    Preview = new EditorProjectMapPreviewState { OutlineMode = EditorMapPreviewMode.Lights },
                }
            );
            session.SetActiveAsset("scr/00077Guard.scr");
            session.GetDialogEditor("dlg/00001Guard.dlg").AddControlEntry(20, "E:");

            var updatedWorkspace = session.BeginChangeGroup("Guard touch-up").ApplyPendingChanges();
            var defaultUndo = session.GetDefaultUndoHistoryCommandSummary();
            var defaultRedo = session.GetDefaultRedoHistoryCommandSummary();
            var commands = session.GetHistoryCommandSummaries();

            await Assert.That(updatedWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Count).IsEqualTo(2);
            await Assert.That(defaultUndo).IsNotNull();
            await Assert.That(defaultUndo!.Kind).IsEqualTo(EditorSessionHistoryCommandKind.Undo);
            await Assert.That(defaultUndo.Label).IsEqualTo("Undo Guard touch-up");
            await Assert.That(defaultUndo.CanExecute).IsTrue();
            await Assert.That(defaultUndo.Entry.ProjectState.ActiveAssetPath).IsEqualTo("scr/00077Guard.scr");
            await Assert.That(defaultUndo.Entry.ProjectState.OpenAssets.Count).IsEqualTo(2);
            await Assert.That(defaultUndo.Entry.ProjectState.MapViewStates.Count).IsEqualTo(1);
            await Assert.That(defaultRedo).IsNull();
            await Assert.That(commands.Count).IsEqualTo(1);
            await Assert.That(commands[0].Kind).IsEqualTo(EditorSessionHistoryCommandKind.Undo);

            session.Undo();

            var redoAfterUndo = session.GetDefaultRedoHistoryCommandSummary();
            var commandsAfterUndo = session.GetHistoryCommandSummaries();

            await Assert.That(redoAfterUndo).IsNotNull();
            await Assert.That(redoAfterUndo!.Kind).IsEqualTo(EditorSessionHistoryCommandKind.Redo);
            await Assert.That(redoAfterUndo.Label).IsEqualTo("Redo Guard touch-up");
            await Assert.That(redoAfterUndo.CanExecute).IsTrue();
            await Assert.That(redoAfterUndo.Entry.ProjectState.ActiveAssetPath).IsEqualTo("scr/00077Guard.scr");
            await Assert.That(commandsAfterUndo.Count).IsEqualTo(1);
            await Assert.That(commandsAfterUndo[0].Kind).IsEqualTo(EditorSessionHistoryCommandKind.Redo);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ExecuteHistoryCommand_RoutesDefaultUndoAndRedo()
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var dialogEditor = session.GetDialogEditor("dlg/00001Guard.dlg");
            dialogEditor.AddControlEntry(20, "E:");
            session.SetActiveAsset("dlg/00001Guard.dlg");

            var updatedWorkspace = session.BeginChangeGroup("Guard touch-up").ApplyPendingChanges();
            var undoCommand = session.GetDefaultUndoHistoryCommandSummary();

            await Assert.That(undoCommand).IsNotNull();
            await Assert.That(updatedWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Count).IsEqualTo(2);

            var undoneWorkspace = session.ExecuteHistoryCommand(undoCommand!);

            await Assert.That(ReferenceEquals(session.Workspace, undoneWorkspace)).IsTrue();
            await Assert.That(undoneWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Count).IsEqualTo(1);

            var redoCommand = session.GetDefaultRedoHistoryCommandSummary();

            await Assert.That(redoCommand).IsNotNull();
            await Assert.That(redoCommand!.Entry.ProjectState.ActiveAssetPath).IsEqualTo("dlg/00001Guard.dlg");

            var redoneWorkspace = session.ExecuteHistoryCommand(redoCommand);

            await Assert.That(ReferenceEquals(session.Workspace, redoneWorkspace)).IsTrue();
            await Assert.That(redoneWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Count).IsEqualTo(2);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ApplyPendingChanges_RefreshesWorkspaceAndCommitsDialogAndScriptBaselines()
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

            var originalWorkspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = originalWorkspace.CreateSession();
            var dialogEditor = session.GetDialogEditor("dlg/00001Guard.dlg");
            var scriptEditor = session.GetScriptEditor("scr/00077Guard.scr");

            dialogEditor.AddControlEntry(20, "E:");
            scriptEditor.WithDescription("Updated guard");

            var updatedWorkspace = session.ApplyPendingChanges();
            var updatedDialog = updatedWorkspace.FindDialog("dlg/00001Guard.dlg");
            var updatedScript = updatedWorkspace.FindScript("scr/00077Guard.scr");

            await Assert.That(ReferenceEquals(updatedWorkspace, originalWorkspace)).IsFalse();
            await Assert.That(ReferenceEquals(session.Workspace, updatedWorkspace)).IsTrue();
            await Assert.That(updatedDialog).IsNotNull();
            await Assert.That(updatedDialog!.Entries.Count).IsEqualTo(2);
            await Assert.That(updatedDialog.Entries.Any(entry => entry.Num == 20 && entry.Text == "E:")).IsTrue();
            await Assert.That(updatedScript).IsNotNull();
            await Assert.That(updatedScript!.Description).IsEqualTo("Updated guard");
            await Assert.That(dialogEditor.HasPendingChanges).IsFalse();
            await Assert.That(scriptEditor.HasPendingChanges).IsFalse();
            await Assert.That(session.HasPendingChanges).IsFalse();
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(0);
            await Assert.That(ReferenceEquals(dialogEditor, session.GetDialogEditor("dlg\\00001Guard.dlg"))).IsTrue();
            await Assert.That(ReferenceEquals(scriptEditor, session.GetScriptEditor("scr\\00077Guard.scr"))).IsTrue();

            dialogEditor.AddControlEntry(30, "F:");
            scriptEditor.WithDescription("Discard me");
            session.DiscardPendingChanges();

            await Assert.That(dialogEditor.GetCurrentDialog().Entries.Count).IsEqualTo(2);
            await Assert.That(dialogEditor.GetCurrentDialog().Entries.Any(entry => entry.Num == 30)).IsFalse();
            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Updated guard");
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ApplyPendingChanges_RefreshesWorkspaceAndCommitsSaveBaseline()
    {
        var originalSave = CreateLoadedSave(
            new SaveInfo
            {
                ModuleName = "Arcanum",
                LeaderName = "Virgil",
                DisplayName = "Original",
                MapId = -1,
                GameTimeDays = 1,
                GameTimeMs = 2,
                LeaderPortraitId = 3,
                LeaderLevel = 4,
                LeaderTileX = 5,
                LeaderTileY = 6,
                StoryState = 0,
            }
        );
        var workspace = new EditorWorkspace
        {
            ContentDirectory = Path.Combine("game-root", "data"),
            GameDirectory = "game-root",
            GameData = new GameDataStore(),
            Save = originalSave,
            SaveFolder = Path.Combine("game-root", "save"),
            SaveSlotName = "slot0001",
        };
        var session = workspace.CreateSession();
        var saveEditor = session.GetSaveEditor();

        saveEditor.WithSaveInfo(info => info.With(displayName: "Updated"));

        var updatedWorkspace = session.ApplyPendingChanges();

        await Assert.That(ReferenceEquals(updatedWorkspace, workspace)).IsFalse();
        await Assert.That(ReferenceEquals(updatedWorkspace.Save, originalSave)).IsFalse();
        await Assert.That(ReferenceEquals(session.Workspace, updatedWorkspace)).IsTrue();
        await Assert.That(updatedWorkspace.Save).IsNotNull();
        await Assert.That(updatedWorkspace.Save!.Info.DisplayName).IsEqualTo("Updated");
        await Assert.That(saveEditor.HasPendingChanges).IsFalse();
        await Assert.That(saveEditor.GetCurrentSaveInfo().DisplayName).IsEqualTo("Updated");
        await Assert.That(session.HasPendingChanges).IsFalse();
        await Assert.That(session.GetPendingChanges().Count).IsEqualTo(0);
        await Assert.That(ReferenceEquals(saveEditor, session.GetSaveEditor())).IsTrue();

        saveEditor.WithSaveInfo(info => info.With(displayName: "Discard me"));
        session.DiscardPendingChanges();

        await Assert.That(saveEditor.GetCurrentSaveInfo().DisplayName).IsEqualTo("Updated");
        await Assert.That(saveEditor.HasPendingChanges).IsFalse();
    }

    [Test]
    public async Task SavePendingChanges_PersistsDialogAndScriptToContentDirectory()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            var scriptPath = Path.Combine(contentDir, "scr", "00077Guard.scr");
            var dialogPath = Path.Combine(contentDir, "dlg", "00001Guard.dlg");

            ScriptFormat.WriteToFile(
                new ScriptBuilder().WithDescription("Guard").AddCondition(ScriptConditionType.True).Build(),
                scriptPath
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
                dialogPath
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.GetDialogEditor("dlg/00001Guard.dlg").AddControlEntry(20, "E:");
            session.GetScriptEditor("scr/00077Guard.scr").WithDescription("Persisted guard");

            var updatedWorkspace = session.SavePendingChanges();
            var persistedDialog = DialogFormat.ParseFile(dialogPath);
            var persistedScript = ScriptFormat.ParseFile(scriptPath);

            await Assert.That(updatedWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Count).IsEqualTo(2);
            await Assert.That(persistedDialog.Entries.Count).IsEqualTo(2);
            await Assert.That(persistedDialog.Entries.Any(entry => entry.Num == 20 && entry.Text == "E:")).IsTrue();
            await Assert.That(persistedScript.Description).IsEqualTo("Persisted guard");
            await Assert.That(session.HasPendingChanges).IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SavePendingChanges_PersistsSaveToConfiguredSlot()
    {
        var saveDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            var originalSave = CreateLoadedSave(
                new SaveInfo
                {
                    ModuleName = "Arcanum",
                    LeaderName = "Virgil",
                    DisplayName = "Original",
                    MapId = -1,
                    GameTimeDays = 1,
                    GameTimeMs = 2,
                    LeaderPortraitId = 3,
                    LeaderLevel = 4,
                    LeaderTileX = 5,
                    LeaderTileY = 6,
                    StoryState = 0,
                }
            );
            var workspace = new EditorWorkspace
            {
                ContentDirectory = Path.Combine("game-root", "data"),
                GameDirectory = "game-root",
                GameData = new GameDataStore(),
                Save = originalSave,
                SaveFolder = saveDir,
                SaveSlotName = "slot0001",
            };
            var session = workspace.CreateSession();
            var saveEditor = session.GetSaveEditor();

            saveEditor.WithSaveInfo(info => info.With(displayName: "Persisted Save"));

            var updatedWorkspace = session.SavePendingChanges();
            var persistedSave = SaveGameLoader.Load(saveDir, "slot0001");

            await Assert.That(updatedWorkspace.Save).IsNotNull();
            await Assert.That(updatedWorkspace.Save!.Info.DisplayName).IsEqualTo("Persisted Save");
            await Assert.That(persistedSave.Info.DisplayName).IsEqualTo("Persisted Save");
            await Assert.That(saveEditor.HasPendingChanges).IsFalse();
            await Assert.That(session.HasPendingChanges).IsFalse();
        }
        finally
        {
            if (Directory.Exists(saveDir))
                Directory.Delete(saveDir, recursive: true);
        }
    }

    [Test]
    public async Task SavePendingChanges_PersistsSaveToExistingDecoratedGsiSlot()
    {
        var saveDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            Directory.CreateDirectory(saveDir);

            var originalSave = CreateLoadedSave(
                new SaveInfo
                {
                    ModuleName = "Arcanum",
                    LeaderName = "Virgil",
                    DisplayName = "Original",
                    MapId = -1,
                    GameTimeDays = 1,
                    GameTimeMs = 2,
                    LeaderPortraitId = 3,
                    LeaderLevel = 4,
                    LeaderTileX = 5,
                    LeaderTileY = 6,
                    StoryState = 0,
                }
            );
            SaveGameWriter.Save(originalSave, saveDir, "slot0001");

            var exactGsiPath = Path.Combine(saveDir, "slot0001.gsi");
            var decoratedGsiPath = Path.Combine(saveDir, "slot0001Persisted Save.gsi");
            File.Move(exactGsiPath, decoratedGsiPath);

            var workspace = new EditorWorkspace
            {
                ContentDirectory = Path.Combine("game-root", "data"),
                GameDirectory = "game-root",
                GameData = new GameDataStore(),
                Save = SaveGameLoader.Load(saveDir, "slot0001"),
                SaveFolder = saveDir,
                SaveSlotName = "slot0001",
            };
            var session = workspace.CreateSession();

            session.GetSaveEditor().WithSaveInfo(info => info.With(displayName: "Persisted Save"));

            var updatedWorkspace = session.SavePendingChanges();
            var persistedSave = SaveGameLoader.Load(saveDir, "slot0001");

            await Assert.That(updatedWorkspace.Save).IsNotNull();
            await Assert.That(updatedWorkspace.Save!.Info.DisplayName).IsEqualTo("Persisted Save");
            await Assert.That(persistedSave.Info.DisplayName).IsEqualTo("Persisted Save");
            await Assert.That(File.Exists(decoratedGsiPath)).IsTrue();
            await Assert.That(File.Exists(exactGsiPath)).IsFalse();
        }
        finally
        {
            if (Directory.Exists(saveDir))
                Directory.Delete(saveDir, recursive: true);
        }
    }

    [Test]
    public async Task GetPendingValidation_ReportsStagedDialogIssues_AndMatchesApplyFailureValidation()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session
                .GetDialogEditor("dlg/00001Guard.dlg")
                .UpdateEntry(
                    10,
                    entry => new DialogEntry
                    {
                        Num = entry.Num,
                        Text = entry.Text,
                        GenderField = entry.GenderField,
                        Iq = -1,
                        Conditions = entry.Conditions,
                        ResponseVal = entry.ResponseVal,
                        Actions = entry.Actions,
                    }
                );

            var pendingValidation = session.GetPendingValidation();

            await Assert.That(session.Workspace.Validation.HasIssues).IsFalse();
            await Assert.That(pendingValidation.HasIssues).IsTrue();
            await Assert.That(pendingValidation.HasErrors).IsTrue();
            await Assert.That(pendingValidation.Issues.Count).IsEqualTo(1);
            await Assert.That(pendingValidation.Issues[0].AssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert
                .That(pendingValidation.Issues[0].Message.Contains("Negative IQ requirement", StringComparison.Ordinal))
                .IsTrue();

            var exception = Assert.Throws<EditorSessionValidationException>(() => session.ApplyPendingChanges());

            await Assert.That(exception).IsNotNull();
            await Assert.That(exception!.Validation.HasIssues).IsTrue();
            await Assert.That(exception.Validation.HasErrors).IsTrue();
            await Assert.That(exception.Validation.Issues.Count).IsEqualTo(1);
            await Assert.That(exception.Validation.Issues[0].AssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert
                .That(
                    exception.Validation.Issues[0].Message.Contains("Negative IQ requirement", StringComparison.Ordinal)
                )
                .IsTrue();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task GetValidationRepairCandidates_StagedDialogIssues_ReturnsActionableDialogRepairs()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session
                .GetDialogEditor("dlg/00001Guard.dlg")
                .UpdateEntry(
                    10,
                    entry => new DialogEntry
                    {
                        Num = entry.Num,
                        Text = entry.Text,
                        GenderField = entry.GenderField,
                        Iq = -1,
                        Conditions = entry.Conditions,
                        ResponseVal = 99,
                        Actions = entry.Actions,
                    }
                );

            var candidates = session.GetValidationRepairCandidates();

            await Assert.That(candidates.Count).IsEqualTo(3);
            await Assert
                .That(
                    candidates.Count(candidate =>
                        candidate.Kind
                        == EditorSessionValidationRepairCandidateKind.SetDialogEntryIntelligenceRequirement
                    )
                )
                .IsEqualTo(2);
            await Assert
                .That(
                    candidates.Any(candidate =>
                        candidate.Kind == EditorSessionValidationRepairCandidateKind.SetDialogResponseTarget
                    )
                )
                .IsTrue();
            await Assert.That(candidates.All(candidate => candidate.AssetPath == "dlg/00001Guard.dlg")).IsTrue();
            await Assert.That(candidates.All(candidate => candidate.DialogEntryNumber == 10)).IsTrue();
            await Assert
                .That(
                    candidates
                        .Where(candidate =>
                            candidate.Kind
                            == EditorSessionValidationRepairCandidateKind.SetDialogEntryIntelligenceRequirement
                        )
                        .Select(candidate => candidate.SuggestedIntelligenceRequirement!.Value)
                )
                .IsEquivalentTo([0, 1]);
            await Assert
                .That(
                    candidates
                        .Single(candidate =>
                            candidate.Kind == EditorSessionValidationRepairCandidateKind.SetDialogResponseTarget
                        )
                        .SuggestedResponseTargetNumber
                )
                .IsEqualTo(0);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ApplyValidationRepairCandidate_FixesStagedDialogIssues_AndAllowsApply()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session
                .GetDialogEditor("dlg/00001Guard.dlg")
                .UpdateEntry(
                    10,
                    entry => new DialogEntry
                    {
                        Num = entry.Num,
                        Text = entry.Text,
                        GenderField = entry.GenderField,
                        Iq = -1,
                        Conditions = entry.Conditions,
                        ResponseVal = 99,
                        Actions = entry.Actions,
                    }
                );

            var candidates = session.GetValidationRepairCandidates();
            var iqRepair = candidates.Single(candidate =>
                candidate.Kind == EditorSessionValidationRepairCandidateKind.SetDialogEntryIntelligenceRequirement
                && candidate.SuggestedIntelligenceRequirement == 1
            );
            var responseRepair = candidates.Single(candidate =>
                candidate.Kind == EditorSessionValidationRepairCandidateKind.SetDialogResponseTarget
            );

            var iqChange = session.ApplyValidationRepairCandidate(iqRepair);
            var responseChange = session.ApplyValidationRepairCandidate(responseRepair);
            var pendingValidation = session.GetPendingValidation();
            var appliedWorkspace = session.ApplyPendingChanges();

            await Assert.That(iqChange.Kind).IsEqualTo(EditorSessionChangeKind.Dialog);
            await Assert.That(iqChange.Target).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(responseChange.Kind).IsEqualTo(EditorSessionChangeKind.Dialog);
            await Assert.That(responseChange.Target).IsEqualTo("dlg/00001Guard.dlg");
            await Assert
                .That(session.GetDialogEditor("dlg/00001Guard.dlg").GetCurrentDialog().Entries[0].Iq)
                .IsEqualTo(1);
            await Assert
                .That(session.GetDialogEditor("dlg/00001Guard.dlg").GetCurrentDialog().Entries[0].ResponseVal)
                .IsEqualTo(0);
            await Assert.That(pendingValidation.HasIssues).IsFalse();
            await Assert.That(appliedWorkspace.Validation.HasIssues).IsFalse();
            await Assert.That(appliedWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries[0].Iq).IsEqualTo(1);
            await Assert.That(appliedWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries[0].ResponseVal).IsEqualTo(0);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ValidationRepairCandidates_RenumberDuplicateDialogEntries_PreserveFirstEntryNumber()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

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
                        new DialogEntry
                        {
                            Num = 10,
                            Text = "Duplicate",
                            GenderField = string.Empty,
                            Iq = 1,
                            Conditions = string.Empty,
                            ResponseVal = 0,
                            Actions = string.Empty,
                        },
                        new DialogEntry
                        {
                            Num = 20,
                            Text = "Farewell",
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var candidates = session.GetValidationRepairCandidates();
            var repair = candidates.Single();

            await Assert.That(session.Workspace.Validation.HasErrors).IsTrue();
            await Assert.That(candidates.Count).IsEqualTo(1);
            await Assert
                .That(repair.Kind)
                .IsEqualTo(EditorSessionValidationRepairCandidateKind.RenumberDuplicateDialogEntryNumber);
            await Assert.That(repair.AssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(repair.DialogEntryNumber).IsEqualTo(10);

            var change = session.ApplyValidationRepairCandidate(repair);
            var pendingDialog = session.GetDialogEditor("dlg/00001Guard.dlg").GetCurrentDialog();
            var pendingValidation = session.GetPendingValidation();
            var appliedWorkspace = session.BeginChangeGroup("Repair duplicate dialog entries").ApplyPendingChanges();

            await Assert.That(change.Kind).IsEqualTo(EditorSessionChangeKind.Dialog);
            await Assert.That(change.Target).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(pendingValidation.HasIssues).IsFalse();
            await Assert.That(appliedWorkspace.Validation.HasIssues).IsFalse();
            await Assert.That(pendingDialog.Entries.Select(static entry => entry.Num)).IsEquivalentTo([10, 20, 21]);
            await Assert.That(pendingDialog.Entries.Single(entry => entry.Text == "Hello").Num).IsEqualTo(10);
            await Assert.That(pendingDialog.Entries.Single(entry => entry.Text == "Duplicate").Num).IsEqualTo(21);
            await Assert
                .That(appliedWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Select(static entry => entry.Num))
                .IsEquivalentTo([10, 20, 21]);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ScriptValidationRepairCandidates_NormalizeDiskUnsafeDescriptions()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), Path.Combine(contentDir, "scr", "00077Guard.scr"));

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            const string problematicDescription = "Héro description that is far too long for script storage";
            var expectedDescription = System.Text.Encoding.ASCII.GetString(
                System.Text.Encoding.ASCII.GetBytes(problematicDescription[..40])
            );

            session.GetScriptEditor("scr/00077Guard.scr").WithDescription(problematicDescription);

            var summary = session.GetStagedTransactionSummaries().Single();
            var candidates = session.GetValidationRepairCandidates();
            var scopedCandidates = session.GetValidationRepairCandidates(summary);
            var repair = candidates.Single();

            await Assert.That(session.GetPendingValidation().HasIssues).IsTrue();
            await Assert.That(summary.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.Script);
            await Assert.That(summary.CanApplyIndividually).IsTrue();
            await Assert.That(summary.RepairCandidateCount).IsEqualTo(1);
            await Assert.That(summary.CanRepairFromSession).IsTrue();
            await Assert.That(candidates.Count).IsEqualTo(1);
            await Assert.That(scopedCandidates.Count).IsEqualTo(1);
            await Assert.That(repair.Kind).IsEqualTo(EditorSessionValidationRepairCandidateKind.SetScriptDescription);
            await Assert.That(repair.AssetPath).IsEqualTo("scr/00077Guard.scr");
            await Assert.That(repair.SuggestedScriptDescription).IsEqualTo(expectedDescription);

            var change = session.ApplyValidationRepairCandidate(repair);
            var pendingValidation = session.GetPendingValidation();
            var appliedWorkspace = session.ApplyPendingChanges();

            await Assert.That(change.Kind).IsEqualTo(EditorSessionChangeKind.Script);
            await Assert.That(change.Target).IsEqualTo("scr/00077Guard.scr");
            await Assert
                .That(session.GetScriptEditor("scr/00077Guard.scr").GetCurrentScript().Description)
                .IsEqualTo(expectedDescription);
            await Assert.That(pendingValidation.HasIssues).IsFalse();
            await Assert.That(appliedWorkspace.Validation.HasIssues).IsFalse();
            await Assert
                .That(appliedWorkspace.FindScript("scr/00077Guard.scr")!.Description)
                .IsEqualTo(expectedDescription);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ScriptValidationRepairCandidates_ClearUnknownAttachmentSlots()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));

        try
        {
            var builder = new ScriptBuilder().WithDescription("Unknown attachment slot");
            for (var i = 0; i < 37; i++)
                builder.AddCondition(ScriptConditionType.ObjIsDead);

            ScriptFormat.WriteToFile(builder.Build(), Path.Combine(contentDir, "scr", "00077Guard.scr"));

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var candidates = session.GetValidationRepairCandidates();
            var repair = candidates.Single();

            await Assert.That(session.Workspace.Validation.HasIssues).IsTrue();
            await Assert.That(candidates.Count).IsEqualTo(1);
            await Assert
                .That(repair.Kind)
                .IsEqualTo(EditorSessionValidationRepairCandidateKind.ClearUnknownScriptAttachmentSlots);
            await Assert.That(repair.AssetPath).IsEqualTo("scr/00077Guard.scr");
            await Assert.That(repair.Description.Contains("36", StringComparison.Ordinal)).IsTrue();

            var change = session.ApplyValidationRepairCandidate(repair);
            var pendingScript = session.GetScriptEditor("scr/00077Guard.scr").GetCurrentScript();
            var pendingValidation = session.GetPendingValidation();
            var appliedWorkspace = session
                .BeginChangeGroup("Repair unknown script attachment slots")
                .ApplyPendingChanges();

            await Assert.That(change.Kind).IsEqualTo(EditorSessionChangeKind.Script);
            await Assert.That(change.Target).IsEqualTo("scr/00077Guard.scr");
            await Assert.That(pendingValidation.HasIssues).IsFalse();
            await Assert.That(appliedWorkspace.Validation.HasIssues).IsFalse();
            await Assert.That(ScriptValidator.Validate(pendingScript).Count).IsEqualTo(0);
            await Assert
                .That(ScriptValidator.Validate(appliedWorkspace.FindScript("scr/00077Guard.scr")!).Count)
                .IsEqualTo(0);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ValidationRepairCandidates_ClearMissingScriptReferences_PerAsset()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "mob"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            const int missingScriptId = 77;
            const int protoNumber = 1001;

            ProtoFormat.WriteToFile(
                WithProperties(MakeProto(protoNumber), MakeScriptProperty(missingScriptId)),
                Path.Combine(contentDir, "proto", "001001 - Test.pro")
            );
            MobFormat.WriteToFile(
                WithProperties(MakePc(protoNumber), MakeScriptProperty(missingScriptId)),
                Path.Combine(contentDir, "mob", "test.mob")
            );
            SectorFormat.WriteToFile(
                MakeSectorWithScriptRefs(
                    missingScriptId,
                    WithProperties(MakePc(protoNumber), MakeScriptProperty(missingScriptId))
                ),
                Path.Combine(contentDir, "maps", "map01", "sector.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var candidates = session.GetValidationRepairCandidates();

            await Assert.That(session.GetPendingValidation().HasIssues).IsTrue();
            await Assert.That(candidates.Count).IsEqualTo(3);
            await Assert
                .That(
                    candidates.All(candidate =>
                        candidate.Kind == EditorSessionValidationRepairCandidateKind.ClearAssetScriptReference
                    )
                )
                .IsTrue();
            await Assert.That(candidates.All(candidate => candidate.ReferencedScriptId == missingScriptId)).IsTrue();

            var changes = candidates.Select(session.ApplyValidationRepairCandidate).ToArray();
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(3);
            var pendingValidation = session.GetPendingValidation();
            var updatedWorkspace = session.BeginChangeGroup("Clear missing script references").ApplyPendingChanges();

            await Assert.That(changes.Any(change => change.Kind == EditorSessionChangeKind.Proto)).IsTrue();
            await Assert.That(changes.Any(change => change.Kind == EditorSessionChangeKind.Mob)).IsTrue();
            await Assert.That(changes.Any(change => change.Kind == EditorSessionChangeKind.Sector)).IsTrue();
            await Assert.That(pendingValidation.HasIssues).IsFalse();
            await Assert.That(updatedWorkspace.Validation.HasIssues).IsFalse();
            await Assert.That(updatedWorkspace.Index.FindScriptReferences(missingScriptId).Count).IsEqualTo(0);
            await Assert
                .That(
                    GetScriptIds(
                        updatedWorkspace.GameData.ProtosBySource["proto/001001 - Test.pro"].Single().Properties
                    )
                )
                .IsEquivalentTo([0]);
            await Assert
                .That(GetScriptIds(updatedWorkspace.GameData.MobsBySource["mob/test.mob"].Single().Properties))
                .IsEquivalentTo([0]);
            await Assert
                .That(
                    updatedWorkspace
                        .GameData.SectorsBySource["maps/map01/sector.sec"]
                        .Single()
                        .SectorScript!.Value.ScriptId
                )
                .IsEqualTo(0);
            await Assert
                .That(
                    GetScriptIds(
                        updatedWorkspace
                            .GameData.SectorsBySource["maps/map01/sector.sec"]
                            .Single()
                            .Objects[0]
                            .Properties
                    )
                )
                .IsEquivalentTo([0]);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ValidationRepairCandidates_ClearMissingProtoReferences_PerAsset()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mob"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            const int missingProtoNumber = 1001;

            MobFormat.WriteToFile(MakePc(missingProtoNumber), Path.Combine(contentDir, "mob", "test.mob"));
            SectorFormat.WriteToFile(
                MakeSector(MakePc(missingProtoNumber)),
                Path.Combine(contentDir, "maps", "map01", "sector.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var candidates = session.GetValidationRepairCandidates();

            await Assert.That(session.GetPendingValidation().HasIssues).IsTrue();
            await Assert.That(candidates.Count).IsEqualTo(2);
            await Assert
                .That(
                    candidates.All(candidate =>
                        candidate.Kind == EditorSessionValidationRepairCandidateKind.ClearAssetProtoReference
                    )
                )
                .IsTrue();
            await Assert
                .That(candidates.All(candidate => candidate.ReferencedProtoNumber == missingProtoNumber))
                .IsTrue();

            var changes = candidates.Select(session.ApplyValidationRepairCandidate).ToArray();
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(2);
            var pendingValidation = session.GetPendingValidation();
            var updatedWorkspace = session.BeginChangeGroup("Clear missing proto references").ApplyPendingChanges();

            await Assert.That(changes.Any(change => change.Kind == EditorSessionChangeKind.Mob)).IsTrue();
            await Assert.That(changes.Any(change => change.Kind == EditorSessionChangeKind.Sector)).IsTrue();
            await Assert.That(pendingValidation.HasIssues).IsFalse();
            await Assert.That(updatedWorkspace.Validation.HasIssues).IsFalse();
            await Assert.That(updatedWorkspace.Index.FindProtoReferences(missingProtoNumber).Count).IsEqualTo(0);
            await Assert
                .That(updatedWorkspace.GameData.MobsBySource["mob/test.mob"].Single().Header.ProtoId.OidType)
                .IsEqualTo(GameObjectGuid.OidTypeNull);
            await Assert
                .That(
                    updatedWorkspace
                        .GameData.SectorsBySource["maps/map01/sector.sec"]
                        .Single()
                        .Objects[0]
                        .Header.ProtoId.OidType
                )
                .IsEqualTo(GameObjectGuid.OidTypeNull);
            await Assert
                .That(updatedWorkspace.GameData.MobsBySource["mob/test.mob"].Single().Header.ProtoId.GetProtoNumber())
                .IsNull();
            await Assert
                .That(
                    updatedWorkspace
                        .GameData.SectorsBySource["maps/map01/sector.sec"]
                        .Single()
                        .Objects[0]
                        .Header.ProtoId.GetProtoNumber()
                )
                .IsNull();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SetProtoDisplayName_CreatesOverrideMessageAsset_AndClearsValidationWarning()
    {
        const int protoNumber = 21;

        var gameDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(gameDir, "data", "proto"));
        Directory.CreateDirectory(Path.Combine(gameDir, "data", "mes"));

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(protoNumber),
                Path.Combine(gameDir, "data", "proto", "00021 - MissingName.pro")
            );
            MessageFormat.WriteToFile(
                new MesFile { Entries = [new MessageEntry(999, "Different entry")] },
                Path.Combine(gameDir, "data", "mes", "description.mes")
            );

            var session = (await EditorWorkspaceLoader.LoadFromGameInstallAsync(gameDir)).CreateSession();

            await Assert.That(session.Workspace.Validation.HasIssues).IsTrue();

            var change = session.SetProtoDisplayName(protoNumber, "Town Guard", useNameOverrideAsset: true);
            var pendingValidation = session.GetPendingValidation();
            var pendingChanges = session.GetPendingChanges();
            var updatedWorkspace = session.BeginChangeGroup("Add proto display name").ApplyPendingChanges();

            await Assert.That(change).IsNotNull();
            await Assert.That(change!.Kind).IsEqualTo(EditorSessionChangeKind.Message);
            await Assert.That(change.Target).IsEqualTo("oemes/oname.mes");
            await Assert.That(pendingChanges.Count).IsEqualTo(1);
            await Assert.That(pendingChanges[0].Kind).IsEqualTo(EditorSessionChangeKind.Message);
            await Assert.That(pendingValidation.HasIssues).IsFalse();
            await Assert.That(updatedWorkspace.Validation.HasIssues).IsFalse();
            await Assert.That(updatedWorkspace.FindMessageFile("oemes/oname.mes")).IsNotNull();
            await Assert.That(updatedWorkspace.FindMessageFile("oemes/oname.mes")!.Entries.Count).IsEqualTo(1);
            await Assert
                .That(updatedWorkspace.FindMessageFile("oemes/oname.mes")!.Entries[0].Index)
                .IsEqualTo(protoNumber);
            await Assert
                .That(updatedWorkspace.FindMessageFile("oemes/oname.mes")!.Entries[0].Text)
                .IsEqualTo("Town Guard");
            await Assert.That(updatedWorkspace.GetObjectPalette().Single().DisplayName).IsEqualTo("Town Guard");
        }
        finally
        {
            if (Directory.Exists(gameDir))
                Directory.Delete(gameDir, recursive: true);
        }
    }

    [Test]
    public async Task ValidationRepairCandidates_AddMissingProtoDisplayNameEntry_ThroughNameOverrideAsset()
    {
        const int protoNumber = 21;

        var gameDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(gameDir, "data", "proto"));
        Directory.CreateDirectory(Path.Combine(gameDir, "data", "mes"));

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(protoNumber),
                Path.Combine(gameDir, "data", "proto", "00021 - MissingName.pro")
            );
            MessageFormat.WriteToFile(
                new MesFile { Entries = [new MessageEntry(999, "Different entry")] },
                Path.Combine(gameDir, "data", "mes", "description.mes")
            );

            var session = (await EditorWorkspaceLoader.LoadFromGameInstallAsync(gameDir)).CreateSession();
            var candidates = session.GetValidationRepairCandidates();
            var repair = candidates.Single();

            await Assert.That(session.Workspace.Validation.HasIssues).IsTrue();
            await Assert.That(candidates.Count).IsEqualTo(1);
            await Assert.That(repair.Kind).IsEqualTo(EditorSessionValidationRepairCandidateKind.SetProtoDisplayName);
            await Assert.That(repair.AssetPath).IsEqualTo("proto/00021 - MissingName.pro");
            await Assert.That(repair.ProtoNumber).IsEqualTo(protoNumber);
            await Assert.That(repair.SuggestedProtoDisplayName).IsEqualTo("MissingName");
            await Assert.That(repair.UseNameOverrideAsset).IsTrue();

            var change = session.ApplyValidationRepairCandidate(repair);
            var pendingValidation = session.GetPendingValidation();
            var updatedWorkspace = session.BeginChangeGroup("Repair proto display name").ApplyPendingChanges();

            await Assert.That(change.Kind).IsEqualTo(EditorSessionChangeKind.Message);
            await Assert.That(change.Target).IsEqualTo("oemes/oname.mes");
            await Assert.That(pendingValidation.HasIssues).IsFalse();
            await Assert.That(updatedWorkspace.Validation.HasIssues).IsFalse();
            await Assert.That(updatedWorkspace.FindMessageFile("oemes/oname.mes")).IsNotNull();
            await Assert.That(updatedWorkspace.FindMessageFile("oemes/oname.mes")!.Entries.Count).IsEqualTo(1);
            await Assert
                .That(updatedWorkspace.FindMessageFile("oemes/oname.mes")!.Entries[0].Index)
                .IsEqualTo(protoNumber);
            await Assert
                .That(updatedWorkspace.FindMessageFile("oemes/oname.mes")!.Entries[0].Text)
                .IsEqualTo("MissingName");
            await Assert.That(updatedWorkspace.GetObjectPalette().Single().DisplayName).IsEqualTo("MissingName");
        }
        finally
        {
            if (Directory.Exists(gameDir))
                Directory.Delete(gameDir, recursive: true);
        }
    }

    [Test]
    public async Task GetPendingChangeSummary_GroupsDialogAndScriptChanges()
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.GetDialogEditor("dlg/00001Guard.dlg").AddControlEntry(20, "E:");
            session.GetScriptEditor("scr/00077Guard.scr").WithDescription("Updated guard");

            var summary = session.GetPendingChangeSummary();

            await Assert.That(summary.HasChanges).IsTrue();
            await Assert.That(summary.TotalChangeCount).IsEqualTo(2);
            await Assert.That(summary.Changes.Count).IsEqualTo(2);
            await Assert.That(summary.Groups.Count).IsEqualTo(2);
            await Assert.That(summary.Groups[0].Kind).IsEqualTo(EditorSessionChangeKind.Dialog);
            await Assert.That(summary.Groups[0].Count).IsEqualTo(1);
            await Assert.That(summary.Groups[0].Targets.Single()).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(summary.Groups[1].Kind).IsEqualTo(EditorSessionChangeKind.Script);
            await Assert.That(summary.Groups[1].Count).IsEqualTo(1);
            await Assert.That(summary.Groups[1].Targets.Single()).IsEqualTo("scr/00077Guard.scr");
            await Assert.That(summary.Validation.HasIssues).IsFalse();
            await Assert.That(summary.BlockingValidation.HasIssues).IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task GetPendingChangeSummary_NewBlockingValidationError_SurfacesBlockingReport()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session
                .GetDialogEditor("dlg/00001Guard.dlg")
                .UpdateEntry(
                    10,
                    entry => new DialogEntry
                    {
                        Num = entry.Num,
                        Text = entry.Text,
                        GenderField = entry.GenderField,
                        Iq = -1,
                        Conditions = entry.Conditions,
                        ResponseVal = entry.ResponseVal,
                        Actions = entry.Actions,
                    }
                );

            var summary = session.GetPendingChangeSummary();

            await Assert.That(summary.HasChanges).IsTrue();
            await Assert.That(summary.TotalChangeCount).IsEqualTo(1);
            await Assert.That(summary.Groups.Count).IsEqualTo(1);
            await Assert.That(summary.Groups[0].Kind).IsEqualTo(EditorSessionChangeKind.Dialog);
            await Assert.That(summary.Validation.HasErrors).IsTrue();
            await Assert.That(summary.Validation.Issues.Count).IsEqualTo(1);
            await Assert.That(summary.BlockingValidation.HasErrors).IsTrue();
            await Assert.That(summary.BlockingValidation.Issues.Count).IsEqualTo(1);
            await Assert.That(summary.BlockingValidation.Issues[0].AssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(summary.RepairCandidateCount).IsEqualTo(2);
            await Assert.That(summary.CanRepairFromSession).IsTrue();
            await Assert
                .That(summary.RepairCandidates.Select(candidate => candidate.AssetPath))
                .IsEquivalentTo(["dlg/00001Guard.dlg", "dlg/00001Guard.dlg"]);
            await Assert
                .That(summary.RepairCandidates.Select(candidate => candidate.SuggestedIntelligenceRequirement))
                .IsEquivalentTo([(int?)0, (int?)1]);
            await Assert
                .That(
                    summary
                        .BlockingValidation.Issues[0]
                        .Message.Contains("Negative IQ requirement", StringComparison.Ordinal)
                )
                .IsTrue();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task GetPendingValidation_SaveOnlyChanges_ReusesCurrentWorkspaceValidation()
    {
        var validation = new EditorWorkspaceValidationReport
        {
            Issues =
            [
                new EditorWorkspaceValidationIssue
                {
                    Severity = EditorWorkspaceValidationSeverity.Warning,
                    AssetPath = "dlg/00001Guard.dlg",
                    Message = "Existing workspace warning",
                },
            ],
        };
        var workspace = new EditorWorkspace
        {
            ContentDirectory = Path.Combine("game-root", "data"),
            GameDirectory = "game-root",
            GameData = new GameDataStore(),
            Validation = validation,
            Save = CreateLoadedSave(
                new SaveInfo
                {
                    ModuleName = "Arcanum",
                    LeaderName = "Virgil",
                    DisplayName = "Original",
                    MapId = -1,
                    GameTimeDays = 1,
                    GameTimeMs = 2,
                    LeaderPortraitId = 3,
                    LeaderLevel = 4,
                    LeaderTileX = 5,
                    LeaderTileY = 6,
                    StoryState = 0,
                }
            ),
            SaveFolder = Path.Combine("game-root", "save"),
            SaveSlotName = "slot0001",
        };
        var session = workspace.CreateSession();

        session.GetSaveEditor().WithSaveInfo(info => info.With(displayName: "Updated"));

        var pendingValidation = session.GetPendingValidation();

        await Assert.That(object.ReferenceEquals(pendingValidation, validation)).IsTrue();
        await Assert.That(pendingValidation.HasIssues).IsTrue();
        await Assert.That(pendingValidation.Issues.Count).IsEqualTo(1);
        await Assert.That(pendingValidation.Issues[0].Message).IsEqualTo("Existing workspace warning");
    }

    [Test]
    public async Task ApplyPendingChanges_NewBlockingValidationError_ThrowsAndKeepsPendingState()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

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

            var originalWorkspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = originalWorkspace.CreateSession();
            var dialogEditor = session.GetDialogEditor("dlg/00001Guard.dlg");

            dialogEditor.UpdateEntry(
                10,
                entry => new DialogEntry
                {
                    Num = entry.Num,
                    Text = entry.Text,
                    GenderField = entry.GenderField,
                    Iq = -1,
                    Conditions = entry.Conditions,
                    ResponseVal = entry.ResponseVal,
                    Actions = entry.Actions,
                }
            );

            var exception = Assert.Throws<EditorSessionValidationException>(() => session.ApplyPendingChanges());

            await Assert.That(exception).IsNotNull();
            await Assert.That(exception!.Validation.HasErrors).IsTrue();
            await Assert.That(exception.Validation.Issues.Count).IsEqualTo(1);
            await Assert.That(exception.Validation.Issues[0].AssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(exception.ImpactSummary.DirectTargets).IsEquivalentTo(["dlg/00001Guard.dlg"]);
            await Assert.That(exception.ImpactSummary.DefinedDialogIds).IsEquivalentTo([1]);
            await Assert.That(exception.ImpactSummary.HasDirectTargets).IsTrue();
            await Assert.That(exception.ImpactSummary.HasRelatedAssets).IsFalse();
            await Assert
                .That(
                    exception.Validation.Issues[0].Message.Contains("Negative IQ requirement", StringComparison.Ordinal)
                )
                .IsTrue();
            await Assert.That(session.HasPendingChanges).IsTrue();
            await Assert.That(dialogEditor.HasPendingChanges).IsTrue();
            await Assert.That(dialogEditor.GetCurrentDialog().Entries[0].Iq).IsEqualTo(-1);
            await Assert.That(object.ReferenceEquals(session.Workspace, originalWorkspace)).IsTrue();
            await Assert.That(session.GetUndoHistory().Count).IsEqualTo(0);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ApplyPendingChanges_SelectedBlockingTransaction_ExceptionExposesScopedRepairCandidates()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));

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
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), Path.Combine(contentDir, "scr", "00077Guard.scr"));

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session
                .GetDialogEditor("dlg/00001Guard.dlg")
                .UpdateEntry(
                    10,
                    entry => new DialogEntry
                    {
                        Num = entry.Num,
                        Text = entry.Text,
                        GenderField = entry.GenderField,
                        Iq = -1,
                        Conditions = entry.Conditions,
                        ResponseVal = entry.ResponseVal,
                        Actions = entry.Actions,
                    }
                );
            session
                .GetScriptEditor("scr/00077Guard.scr")
                .WithDescription("Héro description that is far too long for script storage");

            var dialogSummary = session
                .GetStagedTransactionSummaries()
                .Single(summary => summary.Kind == EditorSessionStagedHistoryScopeKind.Dialog);

            var exception = Assert.Throws<EditorSessionValidationException>(() =>
                session.ApplyPendingChanges([dialogSummary])
            );

            await Assert.That(exception).IsNotNull();
            await Assert.That(exception!.Validation.HasErrors).IsTrue();
            await Assert.That(exception.Validation.Issues.Count).IsEqualTo(1);
            await Assert.That(exception.Validation.Issues[0].AssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(exception.RepairCandidateCount).IsEqualTo(2);
            await Assert.That(exception.CanRepairFromSession).IsTrue();
            await Assert
                .That(exception.RepairCandidates.Select(candidate => candidate.AssetPath))
                .IsEquivalentTo(["dlg/00001Guard.dlg", "dlg/00001Guard.dlg"]);
            await Assert
                .That(exception.RepairCandidates.Select(candidate => candidate.Kind))
                .IsEquivalentTo([
                    EditorSessionValidationRepairCandidateKind.SetDialogEntryIntelligenceRequirement,
                    EditorSessionValidationRepairCandidateKind.SetDialogEntryIntelligenceRequirement,
                ]);
            await Assert
                .That(exception.RepairCandidates.Select(candidate => candidate.SuggestedIntelligenceRequirement))
                .IsEquivalentTo([(int?)0, (int?)1]);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ApplyPendingChanges_SaveOnlyChange_DoesNotBlockOnPreExistingWorkspaceError()
    {
        var validation = new EditorWorkspaceValidationReport
        {
            Issues =
            [
                new EditorWorkspaceValidationIssue
                {
                    Severity = EditorWorkspaceValidationSeverity.Error,
                    AssetPath = "dlg/00001Guard.dlg",
                    Message = "Existing workspace error",
                },
            ],
        };
        var originalSave = CreateLoadedSave(
            new SaveInfo
            {
                ModuleName = "Arcanum",
                LeaderName = "Virgil",
                DisplayName = "Original",
                MapId = -1,
                GameTimeDays = 1,
                GameTimeMs = 2,
                LeaderPortraitId = 3,
                LeaderLevel = 4,
                LeaderTileX = 5,
                LeaderTileY = 6,
                StoryState = 0,
            }
        );
        var workspace = new EditorWorkspace
        {
            ContentDirectory = Path.Combine("game-root", "data"),
            GameDirectory = "game-root",
            GameData = new GameDataStore(),
            Validation = validation,
            Save = originalSave,
            SaveFolder = Path.Combine("game-root", "save"),
            SaveSlotName = "slot0001",
        };
        var session = workspace.CreateSession();

        session.GetSaveEditor().WithSaveInfo(info => info.With(displayName: "Updated"));

        var updatedWorkspace = session.ApplyPendingChanges();

        await Assert.That(updatedWorkspace.Save).IsNotNull();
        await Assert.That(updatedWorkspace.Save!.Info.DisplayName).IsEqualTo("Updated");
        await Assert.That(object.ReferenceEquals(updatedWorkspace.Validation, validation)).IsTrue();
        await Assert.That(updatedWorkspace.Validation.HasErrors).IsTrue();
        await Assert.That(updatedWorkspace.Validation.Issues.Count).IsEqualTo(1);
        await Assert.That(updatedWorkspace.Validation.Issues[0].Message).IsEqualTo("Existing workspace error");
    }

    [Test]
    public async Task ApplyPendingChanges_AllowsUndoAndRedoForDialogAndScriptChangeGroup()
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var dialogEditor = session.GetDialogEditor("dlg/00001Guard.dlg");
            var scriptEditor = session.GetScriptEditor("scr/00077Guard.scr");

            dialogEditor.AddControlEntry(20, "E:");
            scriptEditor.WithDescription("Updated guard");

            var appliedWorkspace = session.ApplyPendingChanges();

            await Assert.That(session.CanUndo).IsTrue();
            await Assert.That(session.CanRedo).IsFalse();
            await Assert.That(appliedWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Count).IsEqualTo(2);
            await Assert
                .That(appliedWorkspace.FindScript("scr/00077Guard.scr")!.Description)
                .IsEqualTo("Updated guard");

            var undoneWorkspace = session.Undo();

            await Assert.That(ReferenceEquals(session.Workspace, undoneWorkspace)).IsTrue();
            await Assert.That(session.CanUndo).IsFalse();
            await Assert.That(session.CanRedo).IsTrue();
            await Assert.That(undoneWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Count).IsEqualTo(1);
            await Assert.That(undoneWorkspace.FindScript("scr/00077Guard.scr")!.Description).IsEqualTo("Guard");
            await Assert.That(dialogEditor.GetCurrentDialog().Entries.Count).IsEqualTo(1);
            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Guard");
            await Assert.That(dialogEditor.HasPendingChanges).IsFalse();
            await Assert.That(scriptEditor.HasPendingChanges).IsFalse();

            var redoneWorkspace = session.Redo();

            await Assert.That(ReferenceEquals(session.Workspace, redoneWorkspace)).IsTrue();
            await Assert.That(session.CanUndo).IsTrue();
            await Assert.That(session.CanRedo).IsFalse();
            await Assert.That(redoneWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Count).IsEqualTo(2);
            await Assert.That(redoneWorkspace.FindScript("scr/00077Guard.scr")!.Description).IsEqualTo("Updated guard");
            await Assert.That(dialogEditor.GetCurrentDialog().Entries.Count).IsEqualTo(2);
            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Updated guard");
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ApplyPendingChanges_AllowsUndoAndRedoForSaveChangeGroup()
    {
        var originalSave = CreateLoadedSave(
            new SaveInfo
            {
                ModuleName = "Arcanum",
                LeaderName = "Virgil",
                DisplayName = "Original",
                MapId = -1,
                GameTimeDays = 1,
                GameTimeMs = 2,
                LeaderPortraitId = 3,
                LeaderLevel = 4,
                LeaderTileX = 5,
                LeaderTileY = 6,
                StoryState = 0,
            }
        );
        var workspace = new EditorWorkspace
        {
            ContentDirectory = Path.Combine("game-root", "data"),
            GameDirectory = "game-root",
            GameData = new GameDataStore(),
            Save = originalSave,
            SaveFolder = Path.Combine("game-root", "save"),
            SaveSlotName = "slot0001",
        };
        var session = workspace.CreateSession();
        var saveEditor = session.GetSaveEditor();

        saveEditor.WithSaveInfo(info => info.With(displayName: "Updated"));

        var appliedWorkspace = session.ApplyPendingChanges();

        await Assert.That(session.CanUndo).IsTrue();
        await Assert.That(session.CanRedo).IsFalse();
        await Assert.That(appliedWorkspace.Save).IsNotNull();
        await Assert.That(appliedWorkspace.Save!.Info.DisplayName).IsEqualTo("Updated");

        var undoneWorkspace = session.Undo();

        await Assert.That(session.CanUndo).IsFalse();
        await Assert.That(session.CanRedo).IsTrue();
        await Assert.That(undoneWorkspace.Save).IsNotNull();
        await Assert.That(undoneWorkspace.Save!.Info.DisplayName).IsEqualTo("Original");
        await Assert.That(saveEditor.GetCurrentSaveInfo().DisplayName).IsEqualTo("Original");
        await Assert.That(saveEditor.HasPendingChanges).IsFalse();

        var redoneWorkspace = session.Redo();

        await Assert.That(session.CanUndo).IsTrue();
        await Assert.That(session.CanRedo).IsFalse();
        await Assert.That(redoneWorkspace.Save).IsNotNull();
        await Assert.That(redoneWorkspace.Save!.Info.DisplayName).IsEqualTo("Updated");
        await Assert.That(saveEditor.GetCurrentSaveInfo().DisplayName).IsEqualTo("Updated");
    }

    [Test]
    public async Task UndoAndRedo_RestoreTrackedSessionStateAlongsideWorkspaceSnapshot()
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            _ = session.OpenAsset(
                new EditorProjectOpenAsset
                {
                    AssetPath = "dlg\\00001Guard.dlg",
                    ViewId = "dialog-graph",
                    IsPinned = true,
                    Properties = new Dictionary<string, string?> { ["pane"] = "left" },
                }
            );
            session.SetActiveAsset("dlg\\00001Guard.dlg");
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
                }
            );

            session.GetDialogEditor("dlg/00001Guard.dlg").AddControlEntry(20, "E:");

            var appliedWorkspace = session.BeginChangeGroup("Guard session snapshot").ApplyPendingChanges();
            var closedDialog = session.CloseAsset("dlg/00001Guard.dlg");
            var openedScript = session.OpenAsset(
                new EditorProjectOpenAsset
                {
                    AssetPath = "scr\\00077Guard.scr",
                    ViewId = "script-grid",
                    Properties = new Dictionary<string, string?> { ["pane"] = "right" },
                }
            );
            session.SetActiveAsset("scr\\00077Guard.scr");
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    ViewId = "map-scene",
                    Camera = new EditorProjectMapCameraState
                    {
                        CenterTileX = 14.5,
                        CenterTileY = 15.5,
                        Zoom = 2.5,
                    },
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = "maps\\map01\\0099aa00.sec",
                        Tile = new Location(10, 11),
                        Area = new EditorProjectMapAreaSelectionState
                        {
                            MinMapTileX = 9,
                            MinMapTileY = 10,
                            MaxMapTileX = 12,
                            MaxMapTileY = 13,
                            ObjectIds =
                            [
                                new GameObjectGuid(
                                    GameObjectGuid.OidTypeGuid,
                                    0,
                                    2,
                                    Guid.Parse("22222222-2222-2222-2222-222222222222")
                                ),
                            ],
                        },
                    },
                    Preview = new EditorProjectMapPreviewState
                    {
                        UseScenePreview = true,
                        OutlineMode = EditorMapPreviewMode.Lights,
                        ShowObjects = true,
                        ShowRoofs = true,
                        ShowLights = true,
                        ShowBlockedTiles = false,
                        ShowScripts = true,
                    },
                }
            );

            await Assert.That(appliedWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Count).IsEqualTo(2);
            await Assert.That(closedDialog).IsTrue();
            await Assert.That(openedScript.AssetPath).IsEqualTo("scr/00077Guard.scr");
            await Assert.That(session.GetOpenAssets().Count).IsEqualTo(1);
            await Assert.That(session.ActiveAssetPath).IsEqualTo("scr/00077Guard.scr");

            var undoHistory = session.GetUndoHistory();

            await Assert.That(undoHistory.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].ProjectState.ActiveAssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(undoHistory[0].ProjectState.OpenAssets.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].ProjectState.OpenAssets[0].AssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(undoHistory[0].ProjectState.OpenAssets[0].ViewId).IsEqualTo("dialog-graph");
            await Assert.That(undoHistory[0].ProjectState.MapViewStates.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].ProjectState.MapViewStates[0].Camera.Zoom).IsEqualTo(1.25);
            await Assert
                .That(undoHistory[0].ProjectState.MapViewStates[0].Preview.OutlineMode)
                .IsEqualTo(EditorMapPreviewMode.Blocked);

            var undoneWorkspace = session.Undo();
            var undoneOpenAssets = session.GetOpenAssets();
            var undoneMapViews = session.GetMapViewStates();
            var undoneDialogAsset = undoneOpenAssets.Single();

            await Assert.That(ReferenceEquals(session.Workspace, undoneWorkspace)).IsTrue();
            await Assert.That(session.CanUndo).IsFalse();
            await Assert.That(session.CanRedo).IsTrue();
            await Assert.That(undoneWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Count).IsEqualTo(1);
            await Assert.That(session.ActiveAssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(undoneOpenAssets.Count).IsEqualTo(1);
            await Assert.That(undoneDialogAsset.AssetPath).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(undoneDialogAsset.ViewId).IsEqualTo("dialog-graph");
            await Assert.That(undoneDialogAsset.IsPinned).IsTrue();
            await Assert.That(undoneDialogAsset.Properties["pane"]).IsEqualTo("left");
            await Assert.That(undoneMapViews.Count).IsEqualTo(1);
            await Assert.That(undoneMapViews[0].Camera.Zoom).IsEqualTo(1.25);
            await Assert.That(undoneMapViews[0].Selection.SectorAssetPath).IsEqualTo("maps/map01/0011ff44.sec");
            await Assert.That(undoneMapViews[0].Selection.Tile).IsEqualTo(new Location(7, 8));
            await Assert.That(undoneMapViews[0].Selection.Area).IsNotNull();
            await Assert.That(undoneMapViews[0].Selection.Area!.MinMapTileX).IsEqualTo(6);
            await Assert.That(undoneMapViews[0].Selection.Area!.MaxMapTileY).IsEqualTo(9);
            await Assert.That(undoneMapViews[0].Selection.Area!.ObjectIds.Count).IsEqualTo(1);
            await Assert.That(undoneMapViews[0].Preview.OutlineMode).IsEqualTo(EditorMapPreviewMode.Blocked);
            await Assert
                .That(session.GetDialogEditor("dlg/00001Guard.dlg").GetCurrentDialog().Entries.Count)
                .IsEqualTo(1);

            var redoneWorkspace = session.Redo();
            var redoneOpenAssets = session.GetOpenAssets();
            var redoneMapViews = session.GetMapViewStates();
            var redoneScriptAsset = redoneOpenAssets.Single();

            await Assert.That(ReferenceEquals(session.Workspace, redoneWorkspace)).IsTrue();
            await Assert.That(session.CanUndo).IsTrue();
            await Assert.That(session.CanRedo).IsFalse();
            await Assert.That(redoneWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Count).IsEqualTo(2);
            await Assert.That(session.ActiveAssetPath).IsEqualTo("scr/00077Guard.scr");
            await Assert.That(redoneOpenAssets.Count).IsEqualTo(1);
            await Assert.That(redoneScriptAsset.AssetPath).IsEqualTo("scr/00077Guard.scr");
            await Assert.That(redoneScriptAsset.ViewId).IsEqualTo("script-grid");
            await Assert.That(redoneScriptAsset.IsPinned).IsFalse();
            await Assert.That(redoneScriptAsset.Properties["pane"]).IsEqualTo("right");
            await Assert.That(redoneMapViews.Count).IsEqualTo(1);
            await Assert.That(redoneMapViews[0].Camera.Zoom).IsEqualTo(2.5);
            await Assert.That(redoneMapViews[0].Selection.SectorAssetPath).IsEqualTo("maps/map01/0099aa00.sec");
            await Assert.That(redoneMapViews[0].Selection.Tile).IsEqualTo(new Location(10, 11));
            await Assert.That(redoneMapViews[0].Selection.Area).IsNotNull();
            await Assert.That(redoneMapViews[0].Selection.Area!.MinMapTileX).IsEqualTo(9);
            await Assert.That(redoneMapViews[0].Selection.Area!.MaxMapTileY).IsEqualTo(13);
            await Assert.That(redoneMapViews[0].Selection.Area!.ObjectIds.Count).IsEqualTo(1);
            await Assert.That(redoneMapViews[0].Preview.OutlineMode).IsEqualTo(EditorMapPreviewMode.Lights);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ApplyPendingChanges_AfterUndo_ClearsRedoHistory()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var dialogEditor = session.GetDialogEditor("dlg/00001Guard.dlg");

            dialogEditor.AddControlEntry(20, "E:");
            session.ApplyPendingChanges();
            session.Undo();

            dialogEditor.AddControlEntry(30, "F:");
            var updatedWorkspace = session.ApplyPendingChanges();

            await Assert.That(session.CanUndo).IsTrue();
            await Assert.That(session.CanRedo).IsFalse();
            await Assert.That(updatedWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Count).IsEqualTo(2);
            await Assert
                .That(updatedWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Any(entry => entry.Num == 20))
                .IsFalse();
            await Assert
                .That(updatedWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Any(entry => entry.Num == 30))
                .IsTrue();

            Assert.Throws<InvalidOperationException>(() => session.Redo());
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task BeginChangeGroup_RecordsLabeledUndoAndRedoHistory()
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.GetDialogEditor("dlg/00001Guard.dlg").AddControlEntry(20, "E:");
            session.GetScriptEditor("scr/00077Guard.scr").WithDescription("Updated guard");

            var updatedWorkspace = session.BeginChangeGroup("Guard touch-up").ApplyPendingChanges();
            var undoHistory = session.GetUndoHistory();

            await Assert.That(updatedWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Count).IsEqualTo(2);
            await Assert.That(undoHistory.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Label).IsEqualTo("Guard touch-up");
            await Assert.That(undoHistory[0].PersistedToDisk).IsFalse();
            await Assert.That(undoHistory[0].Changes.Count).IsEqualTo(2);
            await Assert
                .That(undoHistory[0].Changes.Any(change => change.Kind == EditorSessionChangeKind.Dialog))
                .IsTrue();
            await Assert
                .That(undoHistory[0].Changes.Any(change => change.Kind == EditorSessionChangeKind.Script))
                .IsTrue();
            await Assert.That(undoHistory[0].RecordedAtUtc).IsNotEqualTo(default(DateTimeOffset));
            await Assert.That(undoHistory[0].ProjectState.ActiveAssetPath).IsNull();
            await Assert.That(undoHistory[0].ProjectState.OpenAssets.Count).IsEqualTo(2);
            await Assert.That(undoHistory[0].ProjectState.MapViewStates.Count).IsEqualTo(0);
            await Assert.That(session.GetRedoHistory().Count).IsEqualTo(0);

            session.Undo();

            var redoHistory = session.GetRedoHistory();

            await Assert.That(redoHistory.Count).IsEqualTo(1);
            await Assert.That(redoHistory[0].Label).IsEqualTo("Guard touch-up");
            await Assert.That(redoHistory[0].PersistedToDisk).IsFalse();
            await Assert.That(redoHistory[0].Changes.Count).IsEqualTo(2);
            await Assert.That(redoHistory[0].ProjectState.ActiveAssetPath).IsNull();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task DirectAssetDraftHistory_RetargetScriptReferences_CanUndoAndRedoBeforeApply()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "mob"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            const int sourceScriptId = 77;
            const int targetScriptId = 88;
            const int protoNumber = 1001;

            ScriptFormat.WriteToFile(MakeScriptFile("Alpha"), Path.Combine(contentDir, "scr", "00077Alpha.scr"));
            ScriptFormat.WriteToFile(MakeScriptFile("Beta"), Path.Combine(contentDir, "scr", "00088Beta.scr"));

            ProtoFormat.WriteToFile(
                WithProperties(MakeProto(protoNumber), MakeScriptProperty(sourceScriptId, 123)),
                Path.Combine(contentDir, "proto", "001001 - Test.pro")
            );
            MobFormat.WriteToFile(
                WithProperties(MakePc(protoNumber), MakeScriptProperty(sourceScriptId)),
                Path.Combine(contentDir, "mob", "test.mob")
            );
            SectorFormat.WriteToFile(
                MakeSectorWithScriptRefs(
                    sourceScriptId,
                    WithProperties(MakePc(protoNumber), MakeScriptProperty(sourceScriptId))
                ),
                Path.Combine(contentDir, "maps", "map01", "sector.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();

            session.RetargetScriptReferences(sourceScriptId, targetScriptId);

            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(3);
            await Assert.That(session.CanUndoDirectAssetChanges).IsTrue();
            await Assert.That(session.CanRedoDirectAssetChanges).IsFalse();
            await Assert.That(session.GetUndoHistory().Count).IsEqualTo(0);

            session.UndoDirectAssetChanges();

            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(0);
            await Assert.That(session.CanUndoDirectAssetChanges).IsFalse();
            await Assert.That(session.CanRedoDirectAssetChanges).IsTrue();
            await Assert.That(session.GetUndoHistory().Count).IsEqualTo(0);

            session.RedoDirectAssetChanges();

            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(3);
            await Assert.That(session.CanUndoDirectAssetChanges).IsTrue();
            await Assert.That(session.CanRedoDirectAssetChanges).IsFalse();

            var updatedWorkspace = session.BeginChangeGroup("Retarget script 77 to 88").ApplyPendingChanges();

            await Assert.That(updatedWorkspace.Index.FindScriptReferences(sourceScriptId).Count).IsEqualTo(0);
            await Assert.That(updatedWorkspace.Index.FindScriptReferences(targetScriptId).Count).IsEqualTo(3);
            await Assert.That(session.CanUndoDirectAssetChanges).IsFalse();
            await Assert.That(session.CanRedoDirectAssetChanges).IsFalse();

            Assert.Throws<InvalidOperationException>(() => session.RedoDirectAssetChanges());
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task StagedHistoryScopes_ReportAndDispatchDirectAssetHistory()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            const int protoNumber = 1001;
            const uint tileArtId = 0x00112233u;

            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(new MobDataBuilder(MakePc(protoNumber)).WithLocation(1, 2).Build()),
                Path.Combine(contentDir, "maps", "map01", "sector.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();

            session.SetSectorTileArt("maps/map01/sector.sec", 5, 6, tileArtId);

            var scope = session.GetStagedHistoryScopes().Single();

            await Assert.That(scope.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.DirectAssets);
            await Assert.That(scope.Target).IsNull();
            await Assert.That(scope.HasPendingChanges).IsTrue();
            await Assert.That(scope.CanUndo).IsTrue();
            await Assert.That(scope.CanRedo).IsFalse();

            session.UndoStagedChanges(scope);

            var undoneScope = session.GetStagedHistoryScopes().Single();

            await Assert.That(session.HasPendingChanges).IsFalse();
            await Assert.That(undoneScope.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.DirectAssets);
            await Assert.That(undoneScope.HasPendingChanges).IsFalse();
            await Assert.That(undoneScope.CanUndo).IsFalse();
            await Assert.That(undoneScope.CanRedo).IsTrue();

            session.RedoStagedChanges(undoneScope);

            var redoneScope = session.GetStagedHistoryScopes().Single();

            await Assert.That(session.HasPendingChanges).IsTrue();
            await Assert.That(redoneScope.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.DirectAssets);
            await Assert.That(redoneScope.HasPendingChanges).IsTrue();
            await Assert.That(redoneScope.CanUndo).IsTrue();
            await Assert.That(redoneScope.CanRedo).IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task StagedTransactionSummaries_GroupDirectAssetDrafts()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            const int protoNumber = 1001;
            const uint tileArtId = 0x00112233u;

            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(new MobDataBuilder(MakePc(protoNumber)).WithLocation(1, 2).Build()),
                Path.Combine(contentDir, "maps", "map01", "sector.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();

            session.SetSectorTileArt("maps/map01/sector.sec", 5, 6, tileArtId);

            var summary = session.GetStagedTransactionSummaries().Single();

            await Assert.That(summary.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.DirectAssets);
            await Assert.That(summary.Target).IsNull();
            await Assert.That(summary.Label).IsEqualTo("direct-assets");
            await Assert.That(summary.AffectedTargets).IsEquivalentTo(["maps/map01/sector.sec"]);
            await Assert.That(summary.PendingChangeCount).IsEqualTo(1);
            await Assert.That(summary.PendingChanges[0].Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(summary.PendingChanges[0].Target).IsEqualTo("maps/map01/sector.sec");
            await Assert.That(summary.HasPendingChanges).IsTrue();
            await Assert.That(summary.CanUndo).IsTrue();
            await Assert.That(summary.CanRedo).IsFalse();
            await Assert.That(summary.CanApplyFromSession).IsTrue();
            await Assert.That(summary.CanDiscardFromSession).IsTrue();
            await Assert.That(summary.CanApplyIndividually).IsTrue();
            await Assert.That(summary.CanSaveIndividually).IsTrue();
            await Assert.That(summary.BlockingValidation.HasIssues).IsFalse();
            await Assert.That(summary.RepairCandidateCount).IsEqualTo(0);
            await Assert.That(summary.CanRepairFromSession).IsFalse();

            session.UndoDirectAssetChanges();

            var undoneSummary = session.GetStagedTransactionSummaries().Single();

            await Assert.That(undoneSummary.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.DirectAssets);
            await Assert.That(undoneSummary.Label).IsEqualTo("direct-assets");
            await Assert.That(undoneSummary.AffectedTargets.Count).IsEqualTo(0);
            await Assert.That(undoneSummary.PendingChangeCount).IsEqualTo(0);
            await Assert.That(undoneSummary.HasPendingChanges).IsFalse();
            await Assert.That(undoneSummary.CanUndo).IsFalse();
            await Assert.That(undoneSummary.CanRedo).IsTrue();
            await Assert.That(undoneSummary.CanApplyFromSession).IsFalse();
            await Assert.That(undoneSummary.CanDiscardFromSession).IsFalse();
            await Assert.That(undoneSummary.CanApplyIndividually).IsFalse();
            await Assert.That(undoneSummary.CanSaveIndividually).IsFalse();
            await Assert.That(undoneSummary.BlockingValidation.HasIssues).IsFalse();
            await Assert.That(undoneSummary.RepairCandidateCount).IsEqualTo(0);
            await Assert.That(undoneSummary.CanRepairFromSession).IsFalse();
            await Assert.That(session.CanApplyPendingChanges).IsFalse();
            await Assert.That(session.CanDiscardPendingChanges).IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task StagedTransactionSummaries_BlockSessionApply_WhenPendingValidationHasErrors()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session
                .GetDialogEditor("dlg/00001Guard.dlg")
                .UpdateEntry(
                    10,
                    entry => new DialogEntry
                    {
                        Num = entry.Num,
                        Text = entry.Text,
                        GenderField = entry.GenderField,
                        Iq = -1,
                        Conditions = entry.Conditions,
                        ResponseVal = entry.ResponseVal,
                        Actions = entry.Actions,
                    }
                );

            var summary = session.GetStagedTransactionSummaries().Single();

            await Assert.That(summary.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.Dialog);
            await Assert.That(summary.HasPendingChanges).IsTrue();
            await Assert.That(summary.CanApplyFromSession).IsFalse();
            await Assert.That(summary.CanDiscardFromSession).IsTrue();
            await Assert.That(summary.CanApplyIndividually).IsFalse();
            await Assert.That(summary.CanSaveIndividually).IsFalse();
            await Assert.That(summary.BlockingValidation.HasErrors).IsTrue();
            await Assert.That(summary.BlockingValidation.Issues.Count).IsEqualTo(1);
            await Assert.That(summary.RepairCandidateCount).IsEqualTo(2);
            await Assert.That(summary.CanRepairFromSession).IsTrue();
            await Assert.That(session.CanApplyPendingChanges).IsFalse();
            await Assert.That(session.CanDiscardPendingChanges).IsTrue();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task TransactionScopedValidationAndRepairQueries_FollowSelectedTransaction()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), Path.Combine(contentDir, "scr", "00077Guard.scr"));
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.GetScriptEditor("scr/00077Guard.scr").WithDescription("Updated guard");
            session
                .GetDialogEditor("dlg/00001Guard.dlg")
                .UpdateEntry(
                    10,
                    entry => new DialogEntry
                    {
                        Num = entry.Num,
                        Text = entry.Text,
                        GenderField = entry.GenderField,
                        Iq = -1,
                        Conditions = entry.Conditions,
                        ResponseVal = entry.ResponseVal,
                        Actions = entry.Actions,
                    }
                );

            var summaries = session.GetStagedTransactionSummaries();
            var dialogSummary = summaries.Single(summary => summary.Kind == EditorSessionStagedHistoryScopeKind.Dialog);
            var scriptSummary = summaries.Single(summary => summary.Kind == EditorSessionStagedHistoryScopeKind.Script);
            var pendingSummary = session.GetPendingChangeSummary();

            var dialogValidation = session.GetPendingValidation(dialogSummary);
            var scriptValidation = session.GetPendingValidation(scriptSummary);
            var dialogRepairs = session.GetValidationRepairCandidates(dialogSummary);
            var scriptRepairs = session.GetValidationRepairCandidates(scriptSummary);

            await Assert.That(dialogValidation.HasErrors).IsTrue();
            await Assert.That(scriptValidation.HasErrors).IsFalse();
            await Assert.That(dialogSummary.BlockingValidation.HasErrors).IsTrue();
            await Assert.That(scriptSummary.BlockingValidation.HasIssues).IsFalse();
            await Assert.That(dialogRepairs.Count).IsEqualTo(2);
            await Assert.That(scriptRepairs.Count).IsEqualTo(0);
            await Assert.That(dialogSummary.RepairCandidateCount).IsEqualTo(2);
            await Assert.That(scriptSummary.RepairCandidateCount).IsEqualTo(0);
            await Assert
                .That(
                    pendingSummary
                        .TargetSummaries.Single(target => target.Target == "dlg/00001Guard.dlg")
                        .RepairCandidateCount
                )
                .IsEqualTo(2);
            await Assert
                .That(
                    pendingSummary
                        .TargetSummaries.Single(target => target.Target == "dlg/00001Guard.dlg")
                        .CanRepairFromSession
                )
                .IsTrue();
            await Assert
                .That(
                    pendingSummary
                        .TargetSummaries.Single(target => target.Target == "scr/00077Guard.scr")
                        .RepairCandidateCount
                )
                .IsEqualTo(0);
            await Assert
                .That(
                    pendingSummary
                        .TargetSummaries.Single(target => target.Target == "scr/00077Guard.scr")
                        .CanRepairFromSession
                )
                .IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ApplyPendingChanges_SelectedTransaction_AppliesOnlySelectedScope()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), Path.Combine(contentDir, "scr", "00077Guard.scr"));
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var dialogEditor = session.GetDialogEditor("dlg/00001Guard.dlg");
            var scriptEditor = session.GetScriptEditor("scr/00077Guard.scr");

            dialogEditor.AddControlEntry(20, "E:");
            scriptEditor.WithDescription("Updated guard");

            var dialogSummary = session
                .GetStagedTransactionSummaries()
                .Single(summary => summary.Kind == EditorSessionStagedHistoryScopeKind.Dialog);

            var updatedWorkspace = session.ApplyPendingChanges(dialogSummary);

            await Assert.That(updatedWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Count).IsEqualTo(2);
            await Assert.That(updatedWorkspace.FindScript("scr/00077Guard.scr")!.Description).IsEqualTo("Guard");
            await Assert.That(dialogEditor.HasPendingChanges).IsFalse();
            await Assert.That(scriptEditor.HasPendingChanges).IsTrue();
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(1);
            await Assert.That(session.GetPendingChanges()[0].Kind).IsEqualTo(EditorSessionChangeKind.Script);
            await Assert.That(session.GetPendingChanges()[0].Target).IsEqualTo("scr/00077Guard.scr");
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task DiscardPendingChanges_SelectedTransaction_DiscardsOnlySelectedScope()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), Path.Combine(contentDir, "scr", "00077Guard.scr"));
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var dialogEditor = session.GetDialogEditor("dlg/00001Guard.dlg");
            var scriptEditor = session.GetScriptEditor("scr/00077Guard.scr");

            dialogEditor.AddControlEntry(20, "E:");
            scriptEditor.WithDescription("Updated guard");

            var dialogSummary = session
                .GetStagedTransactionSummaries()
                .Single(summary => summary.Kind == EditorSessionStagedHistoryScopeKind.Dialog);

            session.DiscardPendingChanges(dialogSummary);

            await Assert.That(dialogEditor.HasPendingChanges).IsFalse();
            await Assert.That(dialogEditor.GetCurrentDialog().Entries.Count).IsEqualTo(1);
            await Assert.That(scriptEditor.HasPendingChanges).IsTrue();
            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Updated guard");
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(1);
            await Assert.That(session.GetPendingChanges()[0].Kind).IsEqualTo(EditorSessionChangeKind.Script);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ApplyPendingChanges_SelectedTransaction_ValidatesOnlySelectedScope()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), Path.Combine(contentDir, "scr", "00077Guard.scr"));
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var dialogEditor = session.GetDialogEditor("dlg/00001Guard.dlg");
            var scriptEditor = session.GetScriptEditor("scr/00077Guard.scr");

            dialogEditor.UpdateEntry(
                10,
                entry => new DialogEntry
                {
                    Num = entry.Num,
                    Text = entry.Text,
                    GenderField = entry.GenderField,
                    Iq = -1,
                    Conditions = entry.Conditions,
                    ResponseVal = entry.ResponseVal,
                    Actions = entry.Actions,
                }
            );
            scriptEditor.WithDescription("Updated guard");

            var scriptSummary = session
                .GetStagedTransactionSummaries()
                .Single(summary => summary.Kind == EditorSessionStagedHistoryScopeKind.Script);

            var updatedWorkspace = session.ApplyPendingChanges(scriptSummary);

            await Assert
                .That(updatedWorkspace.FindScript("scr/00077Guard.scr")!.Description)
                .IsEqualTo("Updated guard");
            await Assert.That(updatedWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries[0].Iq).IsEqualTo(0);
            await Assert.That(dialogEditor.HasPendingChanges).IsTrue();
            await Assert.That(scriptEditor.HasPendingChanges).IsFalse();
            await Assert.That(session.CanApplyPendingChanges).IsFalse();
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(1);
            await Assert.That(session.GetPendingChanges()[0].Kind).IsEqualTo(EditorSessionChangeKind.Dialog);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SavePendingChanges_SelectedTransaction_PersistsOnlySelectedScope()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        var scriptPath = Path.Combine(contentDir, "scr", "00077Guard.scr");
        var dialogPath = Path.Combine(contentDir, "dlg", "00001Guard.dlg");

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), scriptPath);
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
                dialogPath
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var dialogEditor = session.GetDialogEditor("dlg/00001Guard.dlg");
            var scriptEditor = session.GetScriptEditor("scr/00077Guard.scr");

            dialogEditor.AddControlEntry(20, "E:");
            scriptEditor.WithDescription("Persisted guard");

            var dialogSummary = session
                .GetStagedTransactionSummaries()
                .Single(summary => summary.Kind == EditorSessionStagedHistoryScopeKind.Dialog);

            var updatedWorkspace = session.SavePendingChanges(dialogSummary);
            var persistedDialog = DialogFormat.ParseFile(dialogPath);
            var persistedScript = ScriptFormat.ParseFile(scriptPath);
            var undoHistory = session.GetUndoHistory();

            await Assert.That(updatedWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries.Count).IsEqualTo(2);
            await Assert.That(updatedWorkspace.FindScript("scr/00077Guard.scr")!.Description).IsEqualTo("Guard");
            await Assert.That(persistedDialog.Entries.Count).IsEqualTo(2);
            await Assert.That(persistedScript.Description).IsEqualTo("Guard");
            await Assert.That(dialogEditor.HasPendingChanges).IsFalse();
            await Assert.That(scriptEditor.HasPendingChanges).IsTrue();
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(1);
            await Assert.That(session.GetPendingChanges()[0].Kind).IsEqualTo(EditorSessionChangeKind.Script);
            await Assert.That(undoHistory.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].PersistedToDisk).IsTrue();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SavePendingChanges_SelectedTransaction_ValidatesOnlySelectedScope()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        var scriptPath = Path.Combine(contentDir, "scr", "00077Guard.scr");
        var dialogPath = Path.Combine(contentDir, "dlg", "00001Guard.dlg");

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), scriptPath);
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
                dialogPath
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var dialogEditor = session.GetDialogEditor("dlg/00001Guard.dlg");
            var scriptEditor = session.GetScriptEditor("scr/00077Guard.scr");

            dialogEditor.UpdateEntry(
                10,
                entry => new DialogEntry
                {
                    Num = entry.Num,
                    Text = entry.Text,
                    GenderField = entry.GenderField,
                    Iq = -1,
                    Conditions = entry.Conditions,
                    ResponseVal = entry.ResponseVal,
                    Actions = entry.Actions,
                }
            );
            scriptEditor.WithDescription("Persisted guard");

            var scriptSummary = session
                .GetStagedTransactionSummaries()
                .Single(summary => summary.Kind == EditorSessionStagedHistoryScopeKind.Script);

            var updatedWorkspace = session.SavePendingChanges(scriptSummary);
            var persistedDialog = DialogFormat.ParseFile(dialogPath);
            var persistedScript = ScriptFormat.ParseFile(scriptPath);
            var undoHistory = session.GetUndoHistory();

            await Assert
                .That(updatedWorkspace.FindScript("scr/00077Guard.scr")!.Description)
                .IsEqualTo("Persisted guard");
            await Assert.That(updatedWorkspace.FindDialog("dlg/00001Guard.dlg")!.Entries[0].Iq).IsEqualTo(0);
            await Assert.That(persistedScript.Description).IsEqualTo("Persisted guard");
            await Assert.That(persistedDialog.Entries[0].Iq).IsEqualTo(0);
            await Assert.That(dialogEditor.HasPendingChanges).IsTrue();
            await Assert.That(scriptEditor.HasPendingChanges).IsFalse();
            await Assert.That(session.CanApplyPendingChanges).IsFalse();
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(1);
            await Assert.That(session.GetPendingChanges()[0].Kind).IsEqualTo(EditorSessionChangeKind.Dialog);
            await Assert.That(undoHistory.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].PersistedToDisk).IsTrue();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task StagedHistoryScopes_MergedUndoAndRedo_FollowChronologyAcrossScopes()
    {
        const int protoNumber = 1001;
        const uint tileArtId = 0x00112233u;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), Path.Combine(contentDir, "scr", "00077Guard.scr"));
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
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(new MobDataBuilder(MakePc(protoNumber)).WithLocation(1, 2).Build()),
                Path.Combine(contentDir, "maps", "map01", "sector.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var dialogEditor = session.GetDialogEditor("dlg/00001Guard.dlg");
            var scriptEditor = session.GetScriptEditor("scr/00077Guard.scr");

            dialogEditor.AddControlEntry(20, "E:");
            scriptEditor.WithDescription("Updated guard");
            session.SetSectorTileArt("maps/map01/sector.sec", 5, 6, tileArtId);

            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(3);
            await Assert.That(session.CanUndoStagedChanges).IsTrue();
            await Assert.That(session.CanRedoStagedChanges).IsFalse();

            session.UndoStagedChanges();

            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(2);
            await Assert.That(dialogEditor.GetCurrentDialog().Entries.Count).IsEqualTo(2);
            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Updated guard");
            await Assert.That(session.CanUndoStagedChanges).IsTrue();
            await Assert.That(session.CanRedoStagedChanges).IsTrue();

            session.UndoStagedChanges();

            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(1);
            await Assert.That(dialogEditor.GetCurrentDialog().Entries.Count).IsEqualTo(2);
            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Guard");

            session.UndoStagedChanges();

            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(0);
            await Assert.That(dialogEditor.GetCurrentDialog().Entries.Count).IsEqualTo(1);
            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Guard");
            await Assert.That(session.CanUndoStagedChanges).IsFalse();
            await Assert.That(session.CanRedoStagedChanges).IsTrue();

            session.RedoStagedChanges();
            session.RedoStagedChanges();
            session.RedoStagedChanges();

            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(3);
            await Assert.That(dialogEditor.GetCurrentDialog().Entries.Count).IsEqualTo(2);
            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Updated guard");
            await Assert.That(session.CanUndoStagedChanges).IsTrue();
            await Assert.That(session.CanRedoStagedChanges).IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task PreferredStagedHistoryScope_PrefersActiveAssetAndFallsBackToMergedRedo()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), Path.Combine(contentDir, "scr", "00077Guard.scr"));
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.GetDialogEditor("dlg/00001Guard.dlg").AddControlEntry(20, "E:");
            session.GetScriptEditor("scr/00077Guard.scr").WithDescription("Updated guard");
            session.SetActiveAsset("dlg/00001Guard.dlg");

            var preferredUndo = session.GetPreferredUndoStagedHistoryScope();

            await Assert.That(preferredUndo).IsNotNull();
            await Assert.That(preferredUndo!.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.Dialog);
            await Assert.That(preferredUndo.Target).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(session.GetPreferredRedoStagedHistoryScope()).IsNull();

            session.UndoStagedChanges();

            var preferredUndoAfterUndo = session.GetPreferredUndoStagedHistoryScope();
            var preferredRedo = session.GetPreferredRedoStagedHistoryScope();

            await Assert.That(preferredUndoAfterUndo).IsNotNull();
            await Assert.That(preferredUndoAfterUndo!.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.Dialog);
            await Assert.That(preferredRedo).IsNotNull();
            await Assert.That(preferredRedo!.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.Script);
            await Assert.That(preferredRedo.Target).IsEqualTo("scr/00077Guard.scr");
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task PreferredStagedTransactionSummary_PrefersActiveAssetAndFallsBackToMergedRedo()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), Path.Combine(contentDir, "scr", "00077Guard.scr"));
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.GetDialogEditor("dlg/00001Guard.dlg").AddControlEntry(20, "E:");
            session.GetScriptEditor("scr/00077Guard.scr").WithDescription("Updated guard");
            session.SetActiveAsset("dlg/00001Guard.dlg");

            var preferredUndo = session.GetPreferredUndoStagedTransactionSummary();

            await Assert.That(preferredUndo).IsNotNull();
            await Assert.That(preferredUndo!.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.Dialog);
            await Assert.That(preferredUndo.Target).IsEqualTo("dlg/00001Guard.dlg");
            await Assert.That(session.GetPreferredRedoStagedTransactionSummary()).IsNull();

            session.UndoStagedChanges();

            var preferredUndoAfterUndo = session.GetPreferredUndoStagedTransactionSummary();
            var preferredRedo = session.GetPreferredRedoStagedTransactionSummary();

            await Assert.That(preferredUndoAfterUndo).IsNotNull();
            await Assert.That(preferredUndoAfterUndo!.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.Dialog);
            await Assert.That(preferredRedo).IsNotNull();
            await Assert.That(preferredRedo!.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.Script);
            await Assert.That(preferredRedo.Target).IsEqualTo("scr/00077Guard.scr");
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task StagedCommandSummaries_ReportDefaultUndoAndRedoCommands()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), Path.Combine(contentDir, "scr", "00077Guard.scr"));
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.GetDialogEditor("dlg/00001Guard.dlg").AddControlEntry(20, "E:");
            session.GetScriptEditor("scr/00077Guard.scr").WithDescription("Updated guard");
            session.SetActiveAsset("dlg/00001Guard.dlg");

            var defaultUndo = session.GetDefaultUndoStagedCommandSummary();
            var defaultRedo = session.GetDefaultRedoStagedCommandSummary();
            var commands = session.GetStagedCommandSummaries();

            await Assert.That(defaultUndo).IsNotNull();
            await Assert.That(defaultUndo!.Kind).IsEqualTo(EditorSessionStagedCommandKind.Undo);
            await Assert.That(defaultUndo.Label).IsEqualTo("Undo dlg/00001Guard.dlg");
            await Assert.That(defaultUndo.Transaction.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.Dialog);
            await Assert.That(defaultUndo.CanExecute).IsTrue();
            await Assert.That(defaultUndo.IsDefault).IsTrue();
            await Assert.That(defaultRedo).IsNull();
            await Assert.That(commands.Count).IsEqualTo(1);
            await Assert.That(commands[0].Kind).IsEqualTo(EditorSessionStagedCommandKind.Undo);
            await Assert.That(commands[0].IsDefault).IsTrue();

            session.UndoStagedChanges();

            var redoAfterUndo = session.GetDefaultRedoStagedCommandSummary();
            var commandsAfterUndo = session.GetStagedCommandSummaries();

            await Assert.That(redoAfterUndo).IsNotNull();
            await Assert.That(redoAfterUndo!.Kind).IsEqualTo(EditorSessionStagedCommandKind.Redo);
            await Assert.That(redoAfterUndo.Transaction.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.Script);
            await Assert.That(redoAfterUndo.CanExecute).IsTrue();
            await Assert.That(redoAfterUndo.IsDefault).IsTrue();
            await Assert.That(commandsAfterUndo.Count).IsEqualTo(2);
            await Assert.That(commandsAfterUndo[0].Kind).IsEqualTo(EditorSessionStagedCommandKind.Undo);
            await Assert.That(commandsAfterUndo[1].Kind).IsEqualTo(EditorSessionStagedCommandKind.Redo);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ExecuteStagedCommand_RoutesDefaultUndoAndRedo()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), Path.Combine(contentDir, "scr", "00077Guard.scr"));
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var dialogEditor = session.GetDialogEditor("dlg/00001Guard.dlg");
            var scriptEditor = session.GetScriptEditor("scr/00077Guard.scr");

            dialogEditor.AddControlEntry(20, "E:");
            scriptEditor.WithDescription("Updated guard");
            session.SetActiveAsset("dlg/00001Guard.dlg");

            var undoCommand = session.GetDefaultUndoStagedCommandSummary();

            await Assert.That(undoCommand).IsNotNull();

            session.ExecuteStagedCommand(undoCommand!);

            await Assert.That(dialogEditor.GetCurrentDialog().Entries.Count).IsEqualTo(1);
            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Updated guard");

            var redoCommand = session.GetDefaultRedoStagedCommandSummary();

            await Assert.That(redoCommand).IsNotNull();

            session.ExecuteStagedCommand(redoCommand!);

            await Assert.That(dialogEditor.GetCurrentDialog().Entries.Count).IsEqualTo(2);
            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Updated guard");
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task AvailableStagedCommandSummaries_ExposePreferredAndNonDefaultUndoCommands()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), Path.Combine(contentDir, "scr", "00077Guard.scr"));
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.GetDialogEditor("dlg/00001Guard.dlg").AddControlEntry(20, "E:");
            session.GetScriptEditor("scr/00077Guard.scr").WithDescription("Updated guard");
            session.SetActiveAsset("dlg/00001Guard.dlg");

            var undoCommands = session.GetUndoStagedCommandSummaries();
            var allCommands = session.GetAvailableStagedCommandSummaries();

            await Assert.That(undoCommands.Count).IsEqualTo(2);
            await Assert.That(undoCommands[0].Kind).IsEqualTo(EditorSessionStagedCommandKind.Undo);
            await Assert.That(undoCommands[0].Transaction.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.Dialog);
            await Assert.That(undoCommands[0].IsDefault).IsTrue();
            await Assert.That(undoCommands[1].Kind).IsEqualTo(EditorSessionStagedCommandKind.Undo);
            await Assert.That(undoCommands[1].Transaction.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.Script);
            await Assert.That(undoCommands[1].IsDefault).IsFalse();
            await Assert.That(allCommands.Count).IsEqualTo(2);
            await Assert.That(allCommands[0].Kind).IsEqualTo(EditorSessionStagedCommandKind.Undo);
            await Assert.That(allCommands[1].Kind).IsEqualTo(EditorSessionStagedCommandKind.Undo);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task GetBootstrapSummary_CombinesProjectState_StagedCommands_HistoryCommands_AndDefaultCommands()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), Path.Combine(contentDir, "scr", "00077Guard.scr"));
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.OpenAsset(new EditorProjectOpenAsset { AssetPath = "scr/00077Guard.scr", ViewId = "script-grid" });
            session.SetActiveAsset("scr/00077Guard.scr");
            session.GetDialogEditor("dlg/00001Guard.dlg").AddControlEntry(20, "E:");
            _ = session.BeginChangeGroup("Guard touch-up").ApplyPendingChanges();
            session.GetScriptEditor("scr/00077Guard.scr").WithDescription("Updated guard");

            var projectState = session.GetProjectStateSummary();
            var stagedTransactions = session.GetStagedTransactionSummaries();
            var stagedCommands = session.GetAvailableStagedCommandSummaries();
            var historyCommands = session.GetHistoryCommandSummaries();
            var commands = session.GetCommandSummaries();
            var bootstrap = session.GetBootstrapSummary();

            await Assert.That(bootstrap.Restore).IsNull();
            await Assert.That(bootstrap.ProjectState.ActiveAssetPath).IsEqualTo(projectState.ActiveAssetPath);
            await Assert.That(bootstrap.ProjectState.OpenAssets.Count).IsEqualTo(projectState.OpenAssets.Count);
            await Assert.That(bootstrap.StagedTransactions.Count).IsEqualTo(stagedTransactions.Count);
            await Assert.That(bootstrap.StagedTransactions[0].Kind).IsEqualTo(stagedTransactions[0].Kind);
            await Assert.That(bootstrap.StagedCommands.Count).IsEqualTo(stagedCommands.Count);
            await Assert.That(bootstrap.StagedCommands[0].Kind).IsEqualTo(stagedCommands[0].Kind);
            await Assert.That(bootstrap.HistoryCommands.Count).IsEqualTo(historyCommands.Count);
            await Assert.That(bootstrap.HistoryCommands[0].Kind).IsEqualTo(historyCommands[0].Kind);
            await Assert.That(bootstrap.Commands.Count).IsEqualTo(commands.Count);
            await Assert.That(bootstrap.Commands[0].Kind).IsEqualTo(commands[0].Kind);
            await Assert
                .That(bootstrap.HistoryCommands[0].Entry.ProjectState.ActiveAssetPath)
                .IsEqualTo(historyCommands[0].Entry.ProjectState.ActiveAssetPath);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ProjectStateHelpers_ManageGenericViewAndToolEntries()
    {
        var session = new EditorWorkspace
        {
            ContentDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()),
            GameData = new GameDataStore(),
        }.CreateSession();

        var initialViewState = session.SetViewState(
            new EditorProjectViewState
            {
                Id = " map-layout-1 ",
                AssetPath = "scr\\00077Guard.scr",
                ViewId = " script-grid ",
                Properties = new Dictionary<string, string?> { ["dock"] = "left" },
            }
        );
        var initialToolState = session.SetToolState(
            new EditorProjectToolState
            {
                ToolId = " asset-browser ",
                ScopeId = " left-sidebar ",
                Properties = new Dictionary<string, string?> { ["filter"] = "dlg" },
            }
        );

        var replacedViewState = session.SetViewState(
            new EditorProjectViewState
            {
                Id = "MAP-LAYOUT-1",
                AssetPath = "scr/00077Guard.scr",
                ViewId = "script-grid",
                Properties = new Dictionary<string, string?> { ["dock"] = "right" },
            }
        );
        var replacedToolState = session.SetToolState(
            new EditorProjectToolState
            {
                ToolId = "ASSET-BROWSER",
                ScopeId = "LEFT-SIDEBAR",
                Properties = new Dictionary<string, string?> { ["filter"] = "scr" },
            }
        );

        var projectState = session.GetProjectStateSummary();

        await Assert.That(initialViewState.Id).IsEqualTo("map-layout-1");
        await Assert.That(initialViewState.AssetPath).IsEqualTo("scr/00077Guard.scr");
        await Assert.That(initialViewState.ViewId).IsEqualTo("script-grid");
        await Assert.That(initialToolState.ToolId).IsEqualTo("asset-browser");
        await Assert.That(initialToolState.ScopeId).IsEqualTo("left-sidebar");
        await Assert.That(replacedViewState.Properties["dock"]).IsEqualTo("right");
        await Assert.That(replacedToolState.Properties["filter"]).IsEqualTo("scr");
        await Assert.That(session.GetViewStates().Count).IsEqualTo(1);
        await Assert.That(session.GetToolStates().Count).IsEqualTo(1);
        await Assert.That(session.GetViewStates()[0].Id).IsEqualTo("MAP-LAYOUT-1");
        await Assert.That(session.GetToolStates()[0].ToolId).IsEqualTo("ASSET-BROWSER");
        await Assert.That(projectState.ViewStates.Count).IsEqualTo(1);
        await Assert.That(projectState.ToolStates.Count).IsEqualTo(1);
        await Assert.That(projectState.ViewStates[0].AssetPath).IsEqualTo("scr/00077Guard.scr");
        await Assert.That(projectState.ToolStates[0].Properties["filter"]).IsEqualTo("scr");
        await Assert.That(session.RemoveViewState("map-layout-1")).IsTrue();
        await Assert.That(session.RemoveToolState("asset-browser", "left-sidebar")).IsTrue();
        await Assert.That(session.RemoveViewState("missing-view")).IsFalse();
        await Assert.That(session.RemoveToolState("missing-tool")).IsFalse();
        await Assert.That(session.GetViewStates()).IsEmpty();
        await Assert.That(session.GetToolStates()).IsEmpty();
    }

    [Test]
    public async Task ExecuteCommand_RoutesDefaultUndoRedoAcrossStagedAndAppliedHistory()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), Path.Combine(contentDir, "scr", "00077Guard.scr"));
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            _ = session.GetDialogEditor("dlg/00001Guard.dlg").AddControlEntry(20, "E:");
            _ = session.BeginChangeGroup("Guard touch-up").ApplyPendingChanges();
            var scriptEditor = session.GetScriptEditor("scr/00077Guard.scr");
            scriptEditor.WithDescription("Updated guard");

            var defaultUndo = session.GetDefaultUndoCommandSummary();

            await Assert.That(defaultUndo).IsNotNull();
            await Assert.That(defaultUndo!.SourceKind).IsEqualTo(EditorSessionCommandSourceKind.Staged);
            await Assert.That(defaultUndo.StagedCommand).IsNotNull();
            await Assert.That(defaultUndo.HistoryCommand).IsNull();

            _ = session.ExecuteCommand(defaultUndo);

            var afterUndoCommands = session.GetCommandSummaries();

            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Guard");
            await Assert.That(afterUndoCommands.Count).IsEqualTo(2);
            await Assert.That(afterUndoCommands[0].Kind).IsEqualTo(EditorSessionCommandKind.Undo);
            await Assert.That(afterUndoCommands[0].SourceKind).IsEqualTo(EditorSessionCommandSourceKind.History);
            await Assert.That(afterUndoCommands[1].Kind).IsEqualTo(EditorSessionCommandKind.Redo);
            await Assert.That(afterUndoCommands[1].SourceKind).IsEqualTo(EditorSessionCommandSourceKind.Staged);

            _ = session.ExecuteCommand(afterUndoCommands[1]);

            var afterRedo = session.GetDefaultUndoCommandSummary();

            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Updated guard");
            await Assert.That(afterRedo).IsNotNull();
            await Assert.That(afterRedo!.SourceKind).IsEqualTo(EditorSessionCommandSourceKind.Staged);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ExecuteStagedCommand_CanRouteNonDefaultUndoCommand()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "dlg"));

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Guard"), Path.Combine(contentDir, "scr", "00077Guard.scr"));
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

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var dialogEditor = session.GetDialogEditor("dlg/00001Guard.dlg");
            var scriptEditor = session.GetScriptEditor("scr/00077Guard.scr");

            dialogEditor.AddControlEntry(20, "E:");
            scriptEditor.WithDescription("Updated guard");
            session.SetActiveAsset("dlg/00001Guard.dlg");

            var scriptUndoCommand = session
                .GetUndoStagedCommandSummaries()
                .Single(command =>
                    command.Transaction.Kind == EditorSessionStagedHistoryScopeKind.Script && !command.IsDefault
                );

            session.ExecuteStagedCommand(scriptUndoCommand);

            await Assert.That(dialogEditor.GetCurrentDialog().Entries.Count).IsEqualTo(2);
            await Assert.That(scriptEditor.GetCurrentScript().Description).IsEqualTo("Guard");
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(1);
            await Assert.That(session.GetPendingChanges()[0].Kind).IsEqualTo(EditorSessionChangeKind.Dialog);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task PreferredStagedHistoryScope_PrefersActiveDirectAssetTarget()
    {
        const int protoNumber = 1001;
        const uint tileArtId = 0x00112233u;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(new MobDataBuilder(MakePc(protoNumber)).WithLocation(1, 2).Build()),
                Path.Combine(contentDir, "maps", "map01", "sector.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.SetActiveAsset("maps/map01/sector.sec");
            session.SetSectorTileArt("maps/map01/sector.sec", 5, 6, tileArtId);

            var preferredUndo = session.GetPreferredUndoStagedHistoryScope();

            await Assert.That(preferredUndo).IsNotNull();
            await Assert.That(preferredUndo!.Kind).IsEqualTo(EditorSessionStagedHistoryScopeKind.DirectAssets);
            await Assert.That(preferredUndo.Target).IsNull();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task DirectAssetDraftHistory_SectorEdits_ClearRedoOnNewEdit_AndClearHistoryOnDiscard()
    {
        const int protoNumber = 1001;
        const uint firstArtId = 0x00112233u;
        const uint secondArtId = 0x00445566u;
        const uint replacementArtId = 0x00778899u;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(new MobDataBuilder(MakePc(protoNumber)).WithLocation(1, 2).Build()),
                Path.Combine(contentDir, "maps", "map01", "sector.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();

            session.SetSectorTileArt("maps/map01/sector.sec", 5, 6, firstArtId);
            session.SetSectorTileArt("maps/map01/sector.sec", 5, 6, secondArtId);

            await Assert.That(session.CanUndoDirectAssetChanges).IsTrue();
            await Assert.That(session.CanRedoDirectAssetChanges).IsFalse();

            session.UndoDirectAssetChanges();

            await Assert.That(session.CanUndoDirectAssetChanges).IsTrue();
            await Assert.That(session.CanRedoDirectAssetChanges).IsTrue();

            var replacementChange = session.SetSectorTileArt("maps/map01/sector.sec", 5, 6, replacementArtId);

            await Assert.That(replacementChange).IsNotNull();
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(1);
            await Assert.That(session.CanUndoDirectAssetChanges).IsTrue();
            await Assert.That(session.CanRedoDirectAssetChanges).IsFalse();

            Assert.Throws<InvalidOperationException>(() => session.RedoDirectAssetChanges());

            session.DiscardPendingChanges();

            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(0);
            await Assert.That(session.HasPendingChanges).IsFalse();
            await Assert.That(session.CanUndoDirectAssetChanges).IsFalse();
            await Assert.That(session.CanRedoDirectAssetChanges).IsFalse();

            Assert.Throws<InvalidOperationException>(() => session.UndoDirectAssetChanges());
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task RetargetScriptReferences_StagesAndAppliesProtoMobAndSectorChanges()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "mob"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            const int sourceScriptId = 77;
            const int targetScriptId = 88;
            const int protoNumber = 1001;

            ScriptFormat.WriteToFile(MakeScriptFile("Alpha"), Path.Combine(contentDir, "scr", "00077Alpha.scr"));
            ScriptFormat.WriteToFile(MakeScriptFile("Beta"), Path.Combine(contentDir, "scr", "00088Beta.scr"));

            var proto = WithProperties(MakeProto(protoNumber), MakeScriptProperty(sourceScriptId, 123));
            var mob = WithProperties(MakePc(protoNumber), MakeScriptProperty(sourceScriptId));
            var sector = MakeSectorWithScriptRefs(
                sourceScriptId,
                WithProperties(MakePc(protoNumber), MakeScriptProperty(sourceScriptId))
            );

            ProtoFormat.WriteToFile(proto, Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            MobFormat.WriteToFile(mob, Path.Combine(contentDir, "mob", "test.mob"));
            SectorFormat.WriteToFile(sector, Path.Combine(contentDir, "maps", "map01", "sector.sec"));

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();

            var stagedChanges = session.RetargetScriptReferences(sourceScriptId, targetScriptId);

            await Assert.That(stagedChanges.Count).IsEqualTo(3);
            await Assert.That(stagedChanges.Any(change => change.Kind == EditorSessionChangeKind.Proto)).IsTrue();
            await Assert.That(stagedChanges.Any(change => change.Kind == EditorSessionChangeKind.Mob)).IsTrue();
            await Assert.That(stagedChanges.Any(change => change.Kind == EditorSessionChangeKind.Sector)).IsTrue();
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(3);

            var updatedWorkspace = session.BeginChangeGroup("Retarget script 77 to 88").ApplyPendingChanges();

            await Assert.That(updatedWorkspace.Index.FindScriptReferences(sourceScriptId).Count).IsEqualTo(0);
            await Assert.That(updatedWorkspace.Index.FindScriptReferences(targetScriptId).Count).IsEqualTo(3);

            var updatedProto = updatedWorkspace.GameData.ProtosBySource["proto/001001 - Test.pro"].Single();
            var updatedMob = updatedWorkspace.GameData.MobsBySource["mob/test.mob"].Single();
            var updatedSector = updatedWorkspace.GameData.SectorsBySource["maps/map01/sector.sec"].Single();

            await Assert.That(GetScriptIds(updatedProto.Properties)).IsEquivalentTo([targetScriptId, 123]);
            await Assert.That(GetScriptIds(updatedMob.Properties)).IsEquivalentTo([targetScriptId]);
            await Assert.That(updatedSector.SectorScript).IsNotNull();
            await Assert.That(updatedSector.SectorScript!.Value.ScriptId).IsEqualTo(targetScriptId);
            await Assert.That(updatedSector.TileScripts.Count).IsEqualTo(1);
            await Assert.That(updatedSector.TileScripts[0].ScriptNum).IsEqualTo(targetScriptId);
            await Assert.That(GetScriptIds(updatedSector.Objects[0].Properties)).IsEquivalentTo([targetScriptId]);

            var undoHistory = session.GetUndoHistory();

            await Assert.That(undoHistory.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Label).IsEqualTo("Retarget script 77 to 88");
            await Assert.That(undoHistory[0].Changes.Count).IsEqualTo(3);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task PendingAndTransactionImpactSummaries_StagedScriptChange_ReportRelatedReferencingAssets()
    {
        const int scriptId = 77;
        const int protoNumber = 1001;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "mob"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ScriptFormat.WriteToFile(MakeScriptFile("Alpha"), Path.Combine(contentDir, "scr", "00077Alpha.scr"));

            var proto = WithProperties(MakeProto(protoNumber), MakeScriptProperty(scriptId, 123));
            var mob = WithProperties(MakePc(protoNumber), MakeScriptProperty(scriptId));
            var sector = MakeSectorWithScriptRefs(
                scriptId,
                WithProperties(MakePc(protoNumber), MakeScriptProperty(scriptId))
            );

            ProtoFormat.WriteToFile(proto, Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            MobFormat.WriteToFile(mob, Path.Combine(contentDir, "mob", "test.mob"));
            SectorFormat.WriteToFile(sector, Path.Combine(contentDir, "maps", "map01", "sector.sec"));

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.GetScriptEditor("scr/00077Alpha.scr").WithDescription("Updated alpha");

            var pendingSummary = session.GetPendingChangeSummary();
            var transactionSummary = session
                .GetStagedTransactionSummaries()
                .Single(summary => summary.Kind == EditorSessionStagedHistoryScopeKind.Script);

            await Assert.That(pendingSummary.ImpactSummary.DirectTargets).IsEquivalentTo(["scr/00077Alpha.scr"]);
            await Assert
                .That(pendingSummary.ImpactSummary.RelatedAssetPaths)
                .IsEquivalentTo(["mob/test.mob", "maps/map01/sector.sec", "proto/001001 - Test.pro"]);
            await Assert
                .That(pendingSummary.ImpactSummary.DirectKinds)
                .IsEquivalentTo([EditorSessionChangeKind.Script]);
            await Assert
                .That(pendingSummary.ImpactSummary.RelatedKinds)
                .IsEquivalentTo([
                    EditorSessionChangeKind.Proto,
                    EditorSessionChangeKind.Mob,
                    EditorSessionChangeKind.Sector,
                ]);
            await Assert.That(pendingSummary.ImpactSummary.MapNames).IsEquivalentTo(["map01"]);
            await Assert.That(pendingSummary.ImpactSummary.DefinedScriptIds).IsEquivalentTo([scriptId]);
            await Assert.That(pendingSummary.ImpactSummary.DefinedProtoNumbers.Count).IsEqualTo(0);
            await Assert.That(pendingSummary.ImpactSummary.HasRelatedAssets).IsTrue();
            await Assert.That(transactionSummary.ImpactSummary.DirectTargets).IsEquivalentTo(["scr/00077Alpha.scr"]);
            await Assert
                .That(transactionSummary.ImpactSummary.RelatedAssetPaths)
                .IsEquivalentTo(["mob/test.mob", "maps/map01/sector.sec", "proto/001001 - Test.pro"]);
            await Assert
                .That(transactionSummary.ImpactSummary.DirectKinds)
                .IsEquivalentTo([EditorSessionChangeKind.Script]);
            await Assert
                .That(transactionSummary.ImpactSummary.RelatedKinds)
                .IsEquivalentTo([
                    EditorSessionChangeKind.Proto,
                    EditorSessionChangeKind.Mob,
                    EditorSessionChangeKind.Sector,
                ]);
            await Assert.That(transactionSummary.ImpactSummary.MapNames).IsEquivalentTo(["map01"]);
            await Assert.That(transactionSummary.ImpactSummary.DefinedScriptIds).IsEquivalentTo([scriptId]);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task GetPendingChangeSummary_RetargetScriptReferences_GroupsProtoMobAndSectorChanges()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "mob"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            const int sourceScriptId = 77;
            const int targetScriptId = 88;
            const int protoNumber = 1001;

            ScriptFormat.WriteToFile(MakeScriptFile("Alpha"), Path.Combine(contentDir, "scr", "00077Alpha.scr"));
            ScriptFormat.WriteToFile(MakeScriptFile("Beta"), Path.Combine(contentDir, "scr", "00088Beta.scr"));

            var proto = WithProperties(MakeProto(protoNumber), MakeScriptProperty(sourceScriptId, 123));
            var mob = WithProperties(MakePc(protoNumber), MakeScriptProperty(sourceScriptId));
            var sector = MakeSectorWithScriptRefs(
                sourceScriptId,
                WithProperties(MakePc(protoNumber), MakeScriptProperty(sourceScriptId))
            );

            ProtoFormat.WriteToFile(proto, Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            MobFormat.WriteToFile(mob, Path.Combine(contentDir, "mob", "test.mob"));
            SectorFormat.WriteToFile(sector, Path.Combine(contentDir, "maps", "map01", "sector.sec"));

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.RetargetScriptReferences(sourceScriptId, targetScriptId);

            var summary = session.GetPendingChangeSummary();

            await Assert.That(summary.HasChanges).IsTrue();
            await Assert.That(summary.TotalChangeCount).IsEqualTo(3);
            await Assert.That(summary.TargetSummaries.Count).IsEqualTo(3);
            await Assert.That(summary.Groups.Count).IsEqualTo(3);
            await Assert.That(summary.Groups[0].Kind).IsEqualTo(EditorSessionChangeKind.Proto);
            await Assert.That(summary.Groups[0].Targets.Single()).IsEqualTo("proto/001001 - Test.pro");
            await Assert.That(summary.Groups[1].Kind).IsEqualTo(EditorSessionChangeKind.Mob);
            await Assert.That(summary.Groups[1].Targets.Single()).IsEqualTo("mob/test.mob");
            await Assert.That(summary.Groups[2].Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(summary.Groups[2].Targets.Single()).IsEqualTo("maps/map01/sector.sec");
            await Assert
                .That(
                    summary
                        .TargetSummaries.Single(target => target.Target == "proto/001001 - Test.pro")
                        .DependencySummary!.ScriptReferences.Select(reference => reference.ScriptId)
                )
                .Contains(targetScriptId);
            await Assert
                .That(
                    summary
                        .TargetSummaries.Single(target => target.Target == "proto/001001 - Test.pro")
                        .DependencySummary!.ScriptReferences.Select(reference => reference.ScriptId)
                )
                .DoesNotContain(sourceScriptId);
            await Assert
                .That(
                    summary
                        .TargetSummaries.Single(target => target.Target == "maps/map01/sector.sec")
                        .DependencySummary!.MapName
                )
                .IsEqualTo("map01");
            await Assert.That(summary.BlockingValidation.HasIssues).IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task RetargetProtoReferences_StagesAndAppliesMobAndSectorChanges()
    {
        const int sourceProtoNumber = 1001;
        const int targetProtoNumber = 1002;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "mob"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(sourceProtoNumber),
                Path.Combine(contentDir, "proto", "001001 - Source.pro")
            );
            ProtoFormat.WriteToFile(
                MakeProto(targetProtoNumber),
                Path.Combine(contentDir, "proto", "001002 - Target.pro")
            );
            MobFormat.WriteToFile(MakePc(sourceProtoNumber), Path.Combine(contentDir, "mob", "test.mob"));
            SectorFormat.WriteToFile(
                MakeSector(MakePc(sourceProtoNumber), MakePc(targetProtoNumber)),
                Path.Combine(contentDir, "maps", "map01", "sector.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();

            var stagedChanges = session.RetargetProtoReferences(sourceProtoNumber, targetProtoNumber);

            await Assert.That(stagedChanges.Count).IsEqualTo(2);
            await Assert.That(stagedChanges.Any(change => change.Kind == EditorSessionChangeKind.Mob)).IsTrue();
            await Assert.That(stagedChanges.Any(change => change.Kind == EditorSessionChangeKind.Sector)).IsTrue();
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(2);

            var updatedWorkspace = session.BeginChangeGroup("Retarget proto 1001 to 1002").ApplyPendingChanges();

            await Assert.That(updatedWorkspace.Index.FindProtoReferences(sourceProtoNumber).Count).IsEqualTo(0);
            await Assert.That(updatedWorkspace.Index.FindProtoReferences(targetProtoNumber).Count).IsEqualTo(2);
            await Assert
                .That(updatedWorkspace.GameData.MobsBySource["mob/test.mob"].Single().Header.ProtoId.GetProtoNumber())
                .IsEqualTo(targetProtoNumber);
            await Assert
                .That(
                    updatedWorkspace
                        .GameData.SectorsBySource["maps/map01/sector.sec"]
                        .Single()
                        .Objects.Select(obj => obj.Header.ProtoId.GetProtoNumber())
                )
                .IsEquivalentTo([(int?)targetProtoNumber, (int?)targetProtoNumber]);

            var undoHistory = session.GetUndoHistory();

            await Assert.That(undoHistory.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Label).IsEqualTo("Retarget proto 1001 to 1002");
            await Assert.That(undoHistory[0].Changes.Count).IsEqualTo(2);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task GetPendingChangeSummary_RetargetProtoReferences_GroupsMobAndSectorChanges()
    {
        const int sourceProtoNumber = 1001;
        const int targetProtoNumber = 1002;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "mob"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(sourceProtoNumber),
                Path.Combine(contentDir, "proto", "001001 - Source.pro")
            );
            ProtoFormat.WriteToFile(
                MakeProto(targetProtoNumber),
                Path.Combine(contentDir, "proto", "001002 - Target.pro")
            );
            MobFormat.WriteToFile(MakePc(sourceProtoNumber), Path.Combine(contentDir, "mob", "test.mob"));
            SectorFormat.WriteToFile(
                MakeSector(MakePc(sourceProtoNumber), MakePc(targetProtoNumber)),
                Path.Combine(contentDir, "maps", "map01", "sector.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.RetargetProtoReferences(sourceProtoNumber, targetProtoNumber);

            var summary = session.GetPendingChangeSummary();

            await Assert.That(summary.HasChanges).IsTrue();
            await Assert.That(summary.TotalChangeCount).IsEqualTo(2);
            await Assert.That(summary.TargetSummaries.Count).IsEqualTo(2);
            await Assert.That(summary.Groups.Count).IsEqualTo(2);
            await Assert
                .That(summary.Groups.Select(group => group.Kind))
                .IsEquivalentTo([EditorSessionChangeKind.Mob, EditorSessionChangeKind.Sector]);
            await Assert
                .That(summary.Groups.SelectMany(group => group.Targets))
                .IsEquivalentTo(["mob/test.mob", "maps/map01/sector.sec"]);
            await Assert
                .That(
                    summary
                        .TargetSummaries.Single(target => target.Target == "mob/test.mob")
                        .DependencySummary!.ProtoReferences.Select(reference => reference.ProtoNumber)
                )
                .Contains(targetProtoNumber);
            await Assert
                .That(
                    summary
                        .TargetSummaries.Single(target => target.Target == "mob/test.mob")
                        .DependencySummary!.ProtoReferences.Select(reference => reference.ProtoNumber)
                )
                .DoesNotContain(sourceProtoNumber);
            await Assert
                .That(
                    summary
                        .TargetSummaries.Single(target => target.Target == "maps/map01/sector.sec")
                        .DependencySummary!.MapName
                )
                .IsEqualTo("map01");
            await Assert.That(summary.BlockingValidation.HasIssues).IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task ReplaceArtReferences_StagesAndAppliesProtoMobAndSectorChanges()
    {
        const uint sourceArtId = 0x00112233u;
        const uint targetArtId = 0x00556677u;
        const uint unaffectedArtId = 0x00ABCDEFu;
        const int protoNumber = 1001;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "mob"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            var proto = WithProperties(
                MakeProto(protoNumber),
                MakeArtProperty(ObjectField.ObjFAid, sourceArtId),
                MakeArtProperty(ObjectField.ObjFLightAid, unaffectedArtId),
                MakeArtProperty(ObjectField.ObjFDestroyedAid, sourceArtId)
            );
            var mob = WithProperties(
                MakePc(protoNumber),
                MakeArtProperty(ObjectField.ObjFCurrentAid, sourceArtId),
                MakeArtProperty(ObjectField.ObjFShadow, unaffectedArtId)
            );
            var sector = MakeSectorWithArtRefs(
                sourceArtId,
                WithProperties(MakePc(protoNumber), MakeArtProperty(ObjectField.ObjFAid, sourceArtId))
            );

            ProtoFormat.WriteToFile(proto, Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            MobFormat.WriteToFile(mob, Path.Combine(contentDir, "mob", "test.mob"));
            SectorFormat.WriteToFile(sector, Path.Combine(contentDir, "maps", "map01", "sector.sec"));

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();

            var stagedChanges = session.ReplaceArtReferences(sourceArtId, targetArtId);

            await Assert.That(stagedChanges.Count).IsEqualTo(3);
            await Assert.That(stagedChanges.Any(change => change.Kind == EditorSessionChangeKind.Proto)).IsTrue();
            await Assert.That(stagedChanges.Any(change => change.Kind == EditorSessionChangeKind.Mob)).IsTrue();
            await Assert.That(stagedChanges.Any(change => change.Kind == EditorSessionChangeKind.Sector)).IsTrue();
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(3);

            var updatedWorkspace = session.BeginChangeGroup("Replace art references").ApplyPendingChanges();

            await Assert.That(updatedWorkspace.Index.FindArtReferences(sourceArtId).Count).IsEqualTo(0);
            await Assert.That(updatedWorkspace.Index.FindArtReferences(targetArtId).Count).IsEqualTo(3);

            var updatedProto = updatedWorkspace.GameData.ProtosBySource["proto/001001 - Test.pro"].Single();
            var updatedMob = updatedWorkspace.GameData.MobsBySource["mob/test.mob"].Single();
            var updatedSector = updatedWorkspace.GameData.SectorsBySource["maps/map01/sector.sec"].Single();

            await Assert
                .That(GetArtIds(updatedProto.Properties))
                .IsEquivalentTo([targetArtId, unaffectedArtId, targetArtId]);
            await Assert.That(GetArtIds(updatedMob.Properties)).IsEquivalentTo([targetArtId, unaffectedArtId]);
            await Assert.That(updatedSector.Lights.Count).IsEqualTo(1);
            await Assert.That(updatedSector.Lights[0].ArtId).IsEqualTo(targetArtId);
            await Assert.That(updatedSector.Tiles[7]).IsEqualTo(targetArtId);
            await Assert.That(updatedSector.HasRoofs).IsTrue();
            await Assert.That(updatedSector.Roofs).IsNotNull();
            await Assert.That(updatedSector.Roofs![3]).IsEqualTo(targetArtId);
            await Assert.That(GetArtIds(updatedSector.Objects[0].Properties)).IsEquivalentTo([targetArtId]);

            var undoHistory = session.GetUndoHistory();

            await Assert.That(undoHistory.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Label).IsEqualTo("Replace art references");
            await Assert.That(undoHistory[0].Changes.Count).IsEqualTo(3);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task GetPendingChangeSummary_ReplaceArtReferences_GroupsProtoMobAndSectorChanges()
    {
        const uint sourceArtId = 0x00112233u;
        const uint targetArtId = 0x00556677u;
        const int protoNumber = 1001;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "mob"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(
                WithProperties(MakeProto(protoNumber), MakeArtProperty(ObjectField.ObjFAid, sourceArtId)),
                Path.Combine(contentDir, "proto", "001001 - Test.pro")
            );
            MobFormat.WriteToFile(
                WithProperties(MakePc(protoNumber), MakeArtProperty(ObjectField.ObjFCurrentAid, sourceArtId)),
                Path.Combine(contentDir, "mob", "test.mob")
            );
            SectorFormat.WriteToFile(
                MakeSectorWithArtRefs(
                    sourceArtId,
                    WithProperties(MakePc(protoNumber), MakeArtProperty(ObjectField.ObjFAid, sourceArtId))
                ),
                Path.Combine(contentDir, "maps", "map01", "sector.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.ReplaceArtReferences(sourceArtId, targetArtId);

            var summary = session.GetPendingChangeSummary();

            await Assert.That(summary.HasChanges).IsTrue();
            await Assert.That(summary.TotalChangeCount).IsEqualTo(3);
            await Assert.That(summary.TargetSummaries.Count).IsEqualTo(3);
            await Assert.That(summary.Groups.Count).IsEqualTo(3);
            await Assert.That(summary.Groups[0].Kind).IsEqualTo(EditorSessionChangeKind.Proto);
            await Assert.That(summary.Groups[0].Targets.Single()).IsEqualTo("proto/001001 - Test.pro");
            await Assert.That(summary.Groups[1].Kind).IsEqualTo(EditorSessionChangeKind.Mob);
            await Assert.That(summary.Groups[1].Targets.Single()).IsEqualTo("mob/test.mob");
            await Assert.That(summary.Groups[2].Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(summary.Groups[2].Targets.Single()).IsEqualTo("maps/map01/sector.sec");
            await Assert
                .That(
                    summary
                        .TargetSummaries.Single(target => target.Target == "maps/map01/sector.sec")
                        .DependencySummary!.ArtReferences.Select(reference => reference.ArtId)
                )
                .Contains(targetArtId);
            await Assert
                .That(
                    summary
                        .TargetSummaries.Single(target => target.Target == "maps/map01/sector.sec")
                        .DependencySummary!.ArtReferences.Select(reference => reference.ArtId)
                )
                .DoesNotContain(sourceArtId);
            await Assert.That(summary.BlockingValidation.HasIssues).IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorCompositionHelpers_StageOneSectorChange_AndApplyLayerUpdates()
    {
        const int protoNumber = 1001;
        const uint tileArtId = 0x00112233u;
        const uint roofArtId = 0x00445566u;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(new MobDataBuilder(MakePc(protoNumber)).WithLocation(1, 2).Build()),
                Path.Combine(contentDir, "maps", "map01", "sector.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();

            var tileChange = session.SetSectorTileArt("maps/map01/sector.sec", 5, 6, tileArtId);
            var roofChange = session.SetSectorRoofArt("maps/map01/sector.sec", 2, 3, roofArtId);
            var blockChange = session.SetSectorBlockedTile("maps/map01/sector.sec", 7, 8, blocked: true);

            await Assert.That(tileChange).IsNotNull();
            await Assert.That(roofChange).IsNotNull();
            await Assert.That(blockChange).IsNotNull();
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(1);

            var summary = session.GetPendingChangeSummary();

            await Assert.That(summary.HasChanges).IsTrue();
            await Assert.That(summary.TotalChangeCount).IsEqualTo(1);
            await Assert.That(summary.Groups.Count).IsEqualTo(1);
            await Assert.That(summary.Groups[0].Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(summary.Groups[0].Targets.Single()).IsEqualTo("maps/map01/sector.sec");
            await Assert.That(summary.BlockingValidation.HasIssues).IsFalse();

            var updatedWorkspace = session.BeginChangeGroup("Edit sector layers").ApplyPendingChanges();
            var updatedSector = updatedWorkspace.FindSector("maps/map01/sector.sec");

            await Assert.That(updatedSector).IsNotNull();
            await Assert.That(updatedSector!.Tiles[(6 * 64) + 5]).IsEqualTo(tileArtId);
            await Assert.That(updatedSector.HasRoofs).IsTrue();
            await Assert.That(updatedSector.Roofs).IsNotNull();
            await Assert.That(updatedSector.Roofs![(3 * 16) + 2]).IsEqualTo(roofArtId);
            await Assert.That(updatedSector.BlockMask.IsBlocked(7, 8)).IsTrue();

            var undoHistory = session.GetUndoHistory();

            await Assert.That(undoHistory.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Label).IsEqualTo("Edit sector layers");
            await Assert.That(undoHistory[0].Changes.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Changes[0].Kind).IsEqualTo(EditorSessionChangeKind.Sector);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorCompositionHelpers_StageBulkTileArtChangesFromGroupedSectorHits()
    {
        const int protoNumber = 1001;
        const uint tileArtId = 0x00778899u;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(new MobDataBuilder(MakePc(protoNumber)).WithLocation(1, 2).Build()),
                Path.Combine(contentDir, "maps", "map01", "sector_a.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(new MobDataBuilder(MakePc(protoNumber)).WithLocation(3, 4).Build()),
                Path.Combine(contentDir, "maps", "map01", "sector_b.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var groupedHits = new EditorMapSceneSectorHitGroup[]
            {
                new()
                {
                    SectorAssetPath = "maps/map01/sector_a.sec",
                    LocalX = 0,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 5,
                            MapTileY = 6,
                            SectorAssetPath = "maps/map01/sector_a.sec",
                            Tile = new Location(5, 6),
                            ObjectHits = [],
                        },
                        new EditorMapSceneHit
                        {
                            MapTileX = 7,
                            MapTileY = 8,
                            SectorAssetPath = "maps/map01/sector_a.sec",
                            Tile = new Location(7, 8),
                            ObjectHits = [],
                        },
                    ],
                },
                new()
                {
                    SectorAssetPath = "maps/map01/sector_b.sec",
                    LocalX = 1,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 65,
                            MapTileY = 2,
                            SectorAssetPath = "maps/map01/sector_b.sec",
                            Tile = new Location(1, 2),
                            ObjectHits = [],
                        },
                    ],
                },
            };

            var changes = session.SetSectorTileArt(groupedHits, tileArtId);

            await Assert.That(changes.Count).IsEqualTo(2);
            await Assert.That(changes[0].Target).IsEqualTo("maps/map01/sector_a.sec");
            await Assert.That(changes[1].Target).IsEqualTo("maps/map01/sector_b.sec");
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(2);

            var summary = session.GetPendingChangeSummary();

            await Assert.That(summary.HasChanges).IsTrue();
            await Assert.That(summary.TotalChangeCount).IsEqualTo(2);
            await Assert.That(summary.Groups.Count).IsEqualTo(1);
            await Assert.That(summary.Groups[0].Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(summary.Groups[0].Targets.Count).IsEqualTo(2);

            var updatedWorkspace = session.BeginChangeGroup("Bulk edit sector tiles").ApplyPendingChanges();
            var updatedSectorA = updatedWorkspace.FindSector("maps/map01/sector_a.sec");
            var updatedSectorB = updatedWorkspace.FindSector("maps/map01/sector_b.sec");

            await Assert.That(updatedSectorA).IsNotNull();
            await Assert.That(updatedSectorB).IsNotNull();
            await Assert.That(updatedSectorA!.Tiles[(6 * 64) + 5]).IsEqualTo(tileArtId);
            await Assert.That(updatedSectorA.Tiles[(8 * 64) + 7]).IsEqualTo(tileArtId);
            await Assert.That(updatedSectorB!.Tiles[(2 * 64) + 1]).IsEqualTo(tileArtId);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorCompositionHelpers_StageBulkBlockedTileChangesFromGroupedSectorHits()
    {
        const int protoNumber = 1001;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(new MobDataBuilder(MakePc(protoNumber)).WithLocation(1, 2).Build()),
                Path.Combine(contentDir, "maps", "map01", "sector_a.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(new MobDataBuilder(MakePc(protoNumber)).WithLocation(3, 4).Build()),
                Path.Combine(contentDir, "maps", "map01", "sector_b.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var groupedHits = new EditorMapSceneSectorHitGroup[]
            {
                new()
                {
                    SectorAssetPath = "maps/map01/sector_a.sec",
                    LocalX = 0,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 5,
                            MapTileY = 6,
                            SectorAssetPath = "maps/map01/sector_a.sec",
                            Tile = new Location(5, 6),
                            ObjectHits = [],
                        },
                        new EditorMapSceneHit
                        {
                            MapTileX = 7,
                            MapTileY = 8,
                            SectorAssetPath = "maps/map01/sector_a.sec",
                            Tile = new Location(7, 8),
                            ObjectHits = [],
                        },
                    ],
                },
                new()
                {
                    SectorAssetPath = "maps/map01/sector_b.sec",
                    LocalX = 1,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 65,
                            MapTileY = 2,
                            SectorAssetPath = "maps/map01/sector_b.sec",
                            Tile = new Location(1, 2),
                            ObjectHits = [],
                        },
                    ],
                },
            };

            var changes = session.SetSectorBlockedTile(groupedHits, blocked: true);

            await Assert.That(changes.Count).IsEqualTo(2);
            await Assert.That(changes[0].Target).IsEqualTo("maps/map01/sector_a.sec");
            await Assert.That(changes[1].Target).IsEqualTo("maps/map01/sector_b.sec");
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(2);

            var summary = session.GetPendingChangeSummary();

            await Assert.That(summary.HasChanges).IsTrue();
            await Assert.That(summary.TotalChangeCount).IsEqualTo(2);
            await Assert.That(summary.Groups.Count).IsEqualTo(1);
            await Assert.That(summary.Groups[0].Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(summary.Groups[0].Targets.Count).IsEqualTo(2);

            var updatedWorkspace = session.BeginChangeGroup("Bulk edit blocked tiles").ApplyPendingChanges();
            var updatedSectorA = updatedWorkspace.FindSector("maps/map01/sector_a.sec");
            var updatedSectorB = updatedWorkspace.FindSector("maps/map01/sector_b.sec");

            await Assert.That(updatedSectorA).IsNotNull();
            await Assert.That(updatedSectorB).IsNotNull();
            await Assert.That(updatedSectorA!.BlockMask.IsBlocked(5, 6)).IsTrue();
            await Assert.That(updatedSectorA.BlockMask.IsBlocked(7, 8)).IsTrue();
            await Assert.That(updatedSectorB!.BlockMask.IsBlocked(1, 2)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_ApplyPendingChanges_AddMoveAndRemoveObjects_AndRefreshPreview()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            var movedObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(1, 2).Build();
            var removedObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(8, 9).Build();
            var addedObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(4, 5).Build();

            SectorFormat.WriteToFile(
                MakeSector(movedObject, removedObject),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();

            var addChange = session.AddSectorObject(sectorAssetPath, addedObject);
            var moveChange = session.MoveSectorObject(sectorAssetPath, movedObject.Header.ObjectId, 10, 11);
            var removeChange = session.RemoveSectorObject(sectorAssetPath, removedObject.Header.ObjectId);

            await Assert.That(addChange.Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(moveChange).IsNotNull();
            await Assert.That(removeChange.Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(1);

            var updatedWorkspace = session.BeginChangeGroup("Edit sector objects").ApplyPendingChanges();
            var updatedSector = updatedWorkspace.FindSector(sectorAssetPath);

            await Assert.That(updatedSector).IsNotNull();
            await Assert.That(updatedSector!.Objects.Count).IsEqualTo(2);
            await Assert
                .That(updatedSector.Objects.Any(obj => obj.Header.ObjectId == removedObject.Header.ObjectId))
                .IsFalse();

            var updatedMovedObject = updatedSector.Objects.Single(obj =>
                obj.Header.ObjectId == movedObject.Header.ObjectId
            );
            var updatedAddedObject = updatedSector.Objects.Single(obj =>
                obj.Header.ObjectId == addedObject.Header.ObjectId
            );

            await Assert
                .That(updatedMovedObject.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((10, 11));
            await Assert
                .That(updatedAddedObject.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((4, 5));

            var preview = updatedWorkspace.CreateMapScenePreview("map01");
            var sectorPreview = preview.Sectors.Single();
            var movedPreview = sectorPreview.Objects.Single(obj => obj.ObjectId == movedObject.Header.ObjectId);
            var addedPreview = sectorPreview.Objects.Single(obj => obj.ObjectId == addedObject.Header.ObjectId);

            await Assert.That(sectorPreview.Objects.Count).IsEqualTo(2);
            await Assert.That(movedPreview.Location).IsEqualTo(new Location(10, 11));
            await Assert.That(addedPreview.Location).IsEqualTo(new Location(4, 5));

            var undoHistory = session.GetUndoHistory();

            await Assert.That(undoHistory.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Label).IsEqualTo("Edit sector objects");
            await Assert.That(undoHistory[0].Changes.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Changes[0].Kind).IsEqualTo(EditorSessionChangeKind.Sector);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task AddSectorObjectFromProto_StagesNewObject_AndReturnsGeneratedIdentity()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386390UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            var proto = MakeProto(protoNumber);
            ProtoFormat.WriteToFile(proto, Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(MakeSector(), Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec"));

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();

            var addedObject = session.AddSectorObjectFromProto(sectorAssetPath, protoNumber, 12, 13);

            await Assert.That(addedObject.Header.GameObjectType).IsEqualTo(proto.Header.GameObjectType);
            await Assert.That(addedObject.Header.ObjectId.OidType).IsEqualTo(GameObjectGuid.OidTypeGuid);
            await Assert.That(addedObject.Header.ProtoId.GetProtoNumber()).IsEqualTo(protoNumber);
            await Assert.That(addedObject.Header.PropCollectionItems).IsEqualTo((short)addedObject.Properties.Count);
            await Assert.That(addedObject.GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((12, 13));
            await Assert
                .That(addedObject.GetProperty(ObjectField.ObjFPcPlayerName)!.GetString())
                .IsEqualTo("WorkspacePc");
            await Assert.That(addedObject.GetProperty(ObjectField.ObjFHpPts)!.GetInt32()).IsEqualTo(80);
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(1);

            var updatedWorkspace = session.BeginChangeGroup("Instantiate sector object").ApplyPendingChanges();
            var updatedSector = updatedWorkspace.FindSector(sectorAssetPath);

            await Assert.That(updatedSector).IsNotNull();

            var updatedObject = updatedSector!.Objects.Single();

            await Assert.That(updatedObject.Header.ObjectId).IsEqualTo(addedObject.Header.ObjectId);
            await Assert.That(updatedObject.Header.ProtoId.GetProtoNumber()).IsEqualTo(protoNumber);
            await Assert.That(updatedObject.GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((12, 13));

            var preview = updatedWorkspace.CreateMapScenePreview("map01");
            var sectorPreview = preview.Sectors.Single();
            var objectPreview = sectorPreview.Objects.Single();

            await Assert.That(objectPreview.ObjectId).IsEqualTo(addedObject.Header.ObjectId);
            await Assert.That(objectPreview.Location).IsEqualTo(new Location(12, 13));

            var undoHistory = session.GetUndoHistory();

            await Assert.That(undoHistory.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Label).IsEqualTo("Instantiate sector object");
            await Assert.That(undoHistory[0].Changes.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Changes[0].Kind).IsEqualTo(EditorSessionChangeKind.Sector);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorCompositionHelpers_StageBulkRoofArtChangesFromGroupedSectorHits()
    {
        const int protoNumber = 1001;
        const uint roofArtId = 0x00AABBCCu;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(new MobDataBuilder(MakePc(protoNumber)).WithLocation(1, 2).Build()),
                Path.Combine(contentDir, "maps", "map01", "sector_a.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(new MobDataBuilder(MakePc(protoNumber)).WithLocation(3, 4).Build()),
                Path.Combine(contentDir, "maps", "map01", "sector_b.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var groupedHits = new EditorMapSceneSectorHitGroup[]
            {
                new()
                {
                    SectorAssetPath = "maps/map01/sector_a.sec",
                    LocalX = 0,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 8,
                            MapTileY = 12,
                            SectorAssetPath = "maps/map01/sector_a.sec",
                            Tile = new Location(8, 12),
                            ObjectHits = [],
                        },
                        new EditorMapSceneHit
                        {
                            MapTileX = 11,
                            MapTileY = 15,
                            SectorAssetPath = "maps/map01/sector_a.sec",
                            Tile = new Location(11, 15),
                            ObjectHits = [],
                        },
                    ],
                },
                new()
                {
                    SectorAssetPath = "maps/map01/sector_b.sec",
                    LocalX = 1,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 65,
                            MapTileY = 2,
                            SectorAssetPath = "maps/map01/sector_b.sec",
                            Tile = new Location(1, 2),
                            ObjectHits = [],
                        },
                    ],
                },
            };

            var changes = session.SetSectorRoofArt(groupedHits, roofArtId);

            await Assert.That(changes.Count).IsEqualTo(2);
            await Assert.That(changes[0].Target).IsEqualTo("maps/map01/sector_a.sec");
            await Assert.That(changes[1].Target).IsEqualTo("maps/map01/sector_b.sec");
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(2);

            var summary = session.GetPendingChangeSummary();

            await Assert.That(summary.HasChanges).IsTrue();
            await Assert.That(summary.TotalChangeCount).IsEqualTo(2);
            await Assert.That(summary.Groups.Count).IsEqualTo(1);
            await Assert.That(summary.Groups[0].Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(summary.Groups[0].Targets.Count).IsEqualTo(2);

            var updatedWorkspace = session.BeginChangeGroup("Bulk edit sector roofs").ApplyPendingChanges();
            var updatedSectorA = updatedWorkspace.FindSector("maps/map01/sector_a.sec");
            var updatedSectorB = updatedWorkspace.FindSector("maps/map01/sector_b.sec");

            await Assert.That(updatedSectorA).IsNotNull();
            await Assert.That(updatedSectorB).IsNotNull();
            await Assert.That(updatedSectorA!.HasRoofs).IsTrue();
            await Assert.That(updatedSectorA.Roofs).IsNotNull();
            await Assert.That(updatedSectorA.Roofs![(3 * 16) + 2]).IsEqualTo(roofArtId);
            await Assert.That(updatedSectorA.Roofs.Count(static roof => roof != 0)).IsEqualTo(1);
            await Assert.That(updatedSectorB!.HasRoofs).IsTrue();
            await Assert.That(updatedSectorB.Roofs).IsNotNull();
            await Assert.That(updatedSectorB.Roofs![0]).IsEqualTo(roofArtId);
            await Assert.That(updatedSectorB.Roofs.Count(static roof => roof != 0)).IsEqualTo(1);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorCompositionHelpers_ApplyLayerBrushRequest_StagesTileRoofAndBlockedModes()
    {
        const int protoNumber = 1001;
        const uint tileArtId = 0x00010203u;
        const uint roofArtId = 0x00040506u;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(new MobDataBuilder(MakePc(protoNumber)).WithLocation(1, 2).Build()),
                Path.Combine(contentDir, "maps", "map01", "sector_a.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(new MobDataBuilder(MakePc(protoNumber)).WithLocation(3, 4).Build()),
                Path.Combine(contentDir, "maps", "map01", "sector_b.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);

            var layerHits = new EditorMapSceneSectorHitGroup[]
            {
                new()
                {
                    SectorAssetPath = "maps/map01/sector_a.sec",
                    LocalX = 0,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 8,
                            MapTileY = 12,
                            SectorAssetPath = "maps/map01/sector_a.sec",
                            Tile = new Location(8, 12),
                            ObjectHits = [],
                        },
                    ],
                },
                new()
                {
                    SectorAssetPath = "maps/map01/sector_b.sec",
                    LocalX = 1,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 65,
                            MapTileY = 2,
                            SectorAssetPath = "maps/map01/sector_b.sec",
                            Tile = new Location(1, 2),
                            ObjectHits = [],
                        },
                    ],
                },
            };

            var tileSession = workspace.CreateSession();
            var tileResult = tileSession.ApplySectorLayerBrush(
                layerHits,
                EditorMapLayerBrushRequest.SetTileArt(tileArtId)
            );

            await Assert.That(tileResult.HasChanges).IsTrue();
            await Assert.That(tileResult.ChangeCount).IsEqualTo(2);
            await Assert.That(tileResult.Changes[0].Target).IsEqualTo("maps/map01/sector_a.sec");
            await Assert.That(tileResult.Changes[1].Target).IsEqualTo("maps/map01/sector_b.sec");

            var tiledWorkspace = tileSession.BeginChangeGroup("Brush tile layers").ApplyPendingChanges();
            var tiledSectorA = tiledWorkspace.FindSector("maps/map01/sector_a.sec");
            var tiledSectorB = tiledWorkspace.FindSector("maps/map01/sector_b.sec");

            await Assert.That(tiledSectorA).IsNotNull();
            await Assert.That(tiledSectorB).IsNotNull();
            await Assert.That(tiledSectorA!.Tiles[(12 * 64) + 8]).IsEqualTo(tileArtId);
            await Assert.That(tiledSectorB!.Tiles[(2 * 64) + 1]).IsEqualTo(tileArtId);

            var roofSession = workspace.CreateSession();
            var roofResult = roofSession.ApplySectorLayerBrush(
                layerHits,
                EditorMapLayerBrushRequest.SetRoofArt(roofArtId)
            );

            await Assert.That(roofResult.HasChanges).IsTrue();
            await Assert.That(roofResult.ChangeCount).IsEqualTo(2);

            var roofedWorkspace = roofSession.BeginChangeGroup("Brush roof layers").ApplyPendingChanges();
            var roofedSectorA = roofedWorkspace.FindSector("maps/map01/sector_a.sec");
            var roofedSectorB = roofedWorkspace.FindSector("maps/map01/sector_b.sec");

            await Assert.That(roofedSectorA).IsNotNull();
            await Assert.That(roofedSectorB).IsNotNull();
            await Assert.That(roofedSectorA!.Roofs).IsNotNull();
            await Assert.That(roofedSectorB!.Roofs).IsNotNull();
            await Assert.That(roofedSectorA.Roofs![(3 * 16) + 2]).IsEqualTo(roofArtId);
            await Assert.That(roofedSectorB.Roofs![0]).IsEqualTo(roofArtId);

            var blockedSession = workspace.CreateSession();
            var blockedResult = blockedSession.ApplySectorLayerBrush(
                layerHits,
                EditorMapLayerBrushRequest.SetBlocked(blocked: true)
            );

            await Assert.That(blockedResult.HasChanges).IsTrue();
            await Assert.That(blockedResult.ChangeCount).IsEqualTo(2);

            var blockedWorkspace = blockedSession.BeginChangeGroup("Brush blocked tiles").ApplyPendingChanges();
            var blockedSectorA = blockedWorkspace.FindSector("maps/map01/sector_a.sec");
            var blockedSectorB = blockedWorkspace.FindSector("maps/map01/sector_b.sec");

            await Assert.That(blockedSectorA).IsNotNull();
            await Assert.That(blockedSectorB).IsNotNull();
            await Assert.That(blockedSectorA!.BlockMask.IsBlocked(8, 12)).IsTrue();
            await Assert.That(blockedSectorB!.BlockMask.IsBlocked(1, 2)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorCompositionHelpers_ApplyLayerBrushRequest_FromAreaSelection_StagesProjectedRectangleTiles()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        const uint tileArtId = 0x00070809u;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            var preview = workspace.CreateMapScenePreview("map01");
            var area = new EditorProjectMapAreaSelectionState
            {
                MinMapTileX = 63,
                MinMapTileY = 2,
                MaxMapTileX = 64,
                MaxMapTileY = 2,
            };

            var result = session.ApplySectorLayerBrush(preview, area, EditorMapLayerBrushRequest.SetTileArt(tileArtId));

            await Assert.That(result.HasChanges).IsTrue();
            await Assert.That(result.ChangeCount).IsEqualTo(2);
            await Assert.That(result.Changes[0].Target).IsEqualTo(sectorAssetPathA);
            await Assert.That(result.Changes[1].Target).IsEqualTo(sectorAssetPathB);

            var updatedWorkspace = session.BeginChangeGroup("Brush area tile layers").ApplyPendingChanges();
            var updatedSectorA = updatedWorkspace.FindSector(sectorAssetPathA);
            var updatedSectorB = updatedWorkspace.FindSector(sectorAssetPathB);

            await Assert.That(updatedSectorA).IsNotNull();
            await Assert.That(updatedSectorB).IsNotNull();
            await Assert.That(updatedSectorA!.Tiles[(2 * 64) + 63]).IsEqualTo(tileArtId);
            await Assert.That(updatedSectorB!.Tiles[(2 * 64) + 0]).IsEqualTo(tileArtId);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorCompositionHelpers_ApplyLayerBrushRequest_FromSelectionState_RoutesPointAndAreaSelections()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        const uint pointTileArtId = 0x000A0B0Cu;
        const uint areaTileArtId = 0x000D0E0Fu;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var preview = workspace.CreateMapScenePreview("map01");

            var pointSession = workspace.CreateSession();
            var pointSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(5, 6),
            };

            var pointResult = pointSession.ApplySectorLayerBrush(
                preview,
                pointSelection,
                EditorMapLayerBrushRequest.SetTileArt(pointTileArtId)
            );

            await Assert.That(pointResult.HasChanges).IsTrue();
            await Assert.That(pointResult.ChangeCount).IsEqualTo(1);
            await Assert.That(pointResult.Changes[0].Target).IsEqualTo(sectorAssetPathA);

            var pointWorkspace = pointSession.BeginChangeGroup("Brush point tile selection").ApplyPendingChanges();
            var pointSectorA = pointWorkspace.FindSector(sectorAssetPathA);

            await Assert.That(pointSectorA).IsNotNull();
            await Assert.That(pointSectorA!.Tiles[(6 * 64) + 5]).IsEqualTo(pointTileArtId);

            var areaSession = workspace.CreateSession();
            var areaSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(63, 2),
                Area = new EditorProjectMapAreaSelectionState
                {
                    MinMapTileX = 63,
                    MinMapTileY = 2,
                    MaxMapTileX = 64,
                    MaxMapTileY = 2,
                },
            };

            var areaResult = areaSession.ApplySectorLayerBrush(
                preview,
                areaSelection,
                EditorMapLayerBrushRequest.SetTileArt(areaTileArtId)
            );

            await Assert.That(areaResult.HasChanges).IsTrue();
            await Assert.That(areaResult.ChangeCount).IsEqualTo(2);
            await Assert.That(areaResult.Changes[0].Target).IsEqualTo(sectorAssetPathA);
            await Assert.That(areaResult.Changes[1].Target).IsEqualTo(sectorAssetPathB);

            var areaWorkspace = areaSession.BeginChangeGroup("Brush area tile selection").ApplyPendingChanges();
            var areaSectorA = areaWorkspace.FindSector(sectorAssetPathA);
            var areaSectorB = areaWorkspace.FindSector(sectorAssetPathB);

            await Assert.That(areaSectorA).IsNotNull();
            await Assert.That(areaSectorB).IsNotNull();
            await Assert.That(areaSectorA!.Tiles[(2 * 64) + 63]).IsEqualTo(areaTileArtId);
            await Assert.That(areaSectorB!.Tiles[(2 * 64) + 0]).IsEqualTo(areaTileArtId);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task TerrainPaletteHelpers_ApplyTerrainPaletteEntry_FromSelectionState_RoutesPointAndAreaSelections()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            MapProperties mapProperties = new()
            {
                ArtId = 200,
                Unused = 0,
                LimitX = 2,
                LimitY = 2,
            };
            MapPropertiesFormat.WriteToFile(in mapProperties, Path.Combine(contentDir, "maps", "map01", "map.prp"));
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var preview = workspace.CreateMapScenePreview("map01");

            var pointEntry = workspace.FindTerrainPaletteEntry("maps/map01/map.prp", 1, 0);
            var pointSession = workspace.CreateSession();
            var pointSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(5, 6),
            };

            var pointResult = pointSession.ApplyTerrainPaletteEntry(preview, pointSelection, pointEntry!);

            await Assert.That(pointEntry).IsNotNull();
            await Assert.That(pointResult.HasChanges).IsTrue();
            await Assert.That(pointResult.ChangeCount).IsEqualTo(1);
            await Assert.That(pointResult.Changes[0].Target).IsEqualTo(sectorAssetPathA);

            var pointWorkspace = pointSession.BeginChangeGroup("Paint point terrain tile").ApplyPendingChanges();
            var pointSectorA = pointWorkspace.FindSector(sectorAssetPathA);

            await Assert.That(pointSectorA).IsNotNull();
            await Assert.That(pointSectorA!.Tiles[(6 * 64) + 5]).IsEqualTo(201u);

            var areaEntry = workspace.FindTerrainPaletteEntry("maps/map01/map.prp", 1, 1);
            var areaSession = workspace.CreateSession();
            var areaSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(63, 2),
                Area = new EditorProjectMapAreaSelectionState
                {
                    MinMapTileX = 63,
                    MinMapTileY = 2,
                    MaxMapTileX = 64,
                    MaxMapTileY = 2,
                },
            };

            var areaResult = areaSession.ApplyTerrainPaletteEntry(preview, areaSelection, areaEntry!);

            await Assert.That(areaEntry).IsNotNull();
            await Assert.That(areaResult.HasChanges).IsTrue();
            await Assert.That(areaResult.ChangeCount).IsEqualTo(2);
            await Assert.That(areaResult.Changes[0].Target).IsEqualTo(sectorAssetPathA);
            await Assert.That(areaResult.Changes[1].Target).IsEqualTo(sectorAssetPathB);

            var areaWorkspace = areaSession.BeginChangeGroup("Paint area terrain tiles").ApplyPendingChanges();
            var areaSectorA = areaWorkspace.FindSector(sectorAssetPathA);
            var areaSectorB = areaWorkspace.FindSector(sectorAssetPathB);

            await Assert.That(areaSectorA).IsNotNull();
            await Assert.That(areaSectorB).IsNotNull();
            await Assert.That(areaSectorA!.Tiles[(2 * 64) + 63]).IsEqualTo(203u);
            await Assert.That(areaSectorB!.Tiles[(2 * 64) + 0]).IsEqualTo(203u);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_ApplyTrackedTerrainTool_UsesPersistedTerrainSelection()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{southWestSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            MapProperties mapProperties = new()
            {
                ArtId = 200,
                Unused = 0,
                LimitX = 2,
                LimitY = 2,
            };
            MapPropertiesFormat.WriteToFile(in mapProperties, Path.Combine(contentDir, "maps", "map01", "map.prp"));
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                    },
                    WorldEdit = new EditorProjectMapWorldEditState
                    {
                        ActiveTool = EditorProjectMapWorldEditActiveTool.TerrainPaint,
                        Terrain = new EditorProjectMapTerrainToolState
                        {
                            MapPropertiesAssetPath = "maps/map01/map.prp",
                            PaletteX = 1,
                            PaletteY = 0,
                        },
                    },
                }
            );

            var result = session.ApplyTrackedTerrainTool("map-view-1");

            await Assert.That(result.HasChanges).IsTrue();
            var updatedWorkspace = session.BeginChangeGroup("Apply tracked terrain tool").ApplyPendingChanges();
            var updatedSector = updatedWorkspace.FindSector(sectorAssetPath);

            await Assert.That(updatedSector).IsNotNull();
            await Assert.That(updatedSector!.Tiles[(6 * 64) + 5]).IsEqualTo(201u);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_GetTrackedTerrainPaletteSummary_UsesDefaultPaletteAndCoordinateSelection()
    {
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            MapProperties mapProperties = new()
            {
                ArtId = 200,
                Unused = 0,
                LimitX = 2,
                LimitY = 2,
            };
            MapPropertiesFormat.WriteToFile(in mapProperties, Path.Combine(contentDir, "maps", "map01", "map.prp"));
            SectorFormat.WriteToFile(MakeSector(), Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                    },
                }
            );

            var initialSummary = session.GetTrackedTerrainPaletteSummary("map-view-1");
            var toolState = session.SetTrackedTerrainPaletteEntry("map-view-1", 1, 1);
            var selectedSummary = session.GetTrackedTerrainPaletteSummary("map-view-1");

            await Assert.That(initialSummary.MapPropertiesAssetPath).IsEqualTo("maps/map01/map.prp");
            await Assert.That(initialSummary.Entries.Count).IsEqualTo(4);
            await Assert.That(initialSummary.CanBrowse).IsTrue();
            await Assert.That(initialSummary.SelectedEntry).IsNull();
            await Assert.That(toolState.MapPropertiesAssetPath).IsEqualTo("maps/map01/map.prp");
            await Assert.That(toolState.PaletteX).IsEqualTo(1UL);
            await Assert.That(toolState.PaletteY).IsEqualTo(1UL);
            await Assert.That(selectedSummary.SelectedEntry).IsNotNull();
            await Assert.That(selectedSummary.SelectedEntry!.ArtId.Value).IsEqualTo(203u);
            await Assert.That(selectedSummary.ToolState.MapPropertiesAssetPath).IsEqualTo("maps/map01/map.prp");
            await Assert.That(selectedSummary.ToolState.PaletteX).IsEqualTo(1UL);
            await Assert.That(selectedSummary.ToolState.PaletteY).IsEqualTo(1UL);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_SetTrackedTerrainPaletteEntry_UpdatesTrackedToolSummary()
    {
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            MapProperties mapProperties = new()
            {
                ArtId = 200,
                Unused = 0,
                LimitX = 2,
                LimitY = 2,
            };
            MapPropertiesFormat.WriteToFile(in mapProperties, Path.Combine(contentDir, "maps", "map01", "map.prp"));
            SectorFormat.WriteToFile(MakeSector(), Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                    },
                }
            );

            var paletteEntry = workspace.FindTerrainPaletteEntry("maps/map01/map.prp", 1, 0);

            await Assert.That(paletteEntry).IsNotNull();

            var toolState = session.SetTrackedTerrainPaletteEntry("map-view-1", paletteEntry!);
            var summary = session.GetTrackedTerrainToolSummary("map-view-1");
            var worldEditState = session.GetMapWorldEditState("map-view-1");

            await Assert.That(toolState.MapPropertiesAssetPath).IsEqualTo("maps/map01/map.prp");
            await Assert.That(toolState.PaletteX).IsEqualTo(1UL);
            await Assert.That(toolState.PaletteY).IsEqualTo(0UL);
            await Assert.That(worldEditState.ActiveTool).IsEqualTo(EditorProjectMapWorldEditActiveTool.TerrainPaint);
            await Assert.That(summary.MapViewStateId).IsEqualTo("map-view-1");
            await Assert.That(summary.MapName).IsEqualTo("map01");
            await Assert.That(summary.CanApply).IsTrue();
            await Assert.That(summary.SelectedEntry).IsNotNull();
            await Assert.That(summary.SelectedEntry!.ArtId.Value).IsEqualTo(201u);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_SetTrackedTerrainPaletteEntry_PreservesPinnedInspectorState()
    {
        const int selectedProtoNumber = 1001;
        const int pinnedProtoNumber = 1002;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

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
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                    WorldEdit = new EditorProjectMapWorldEditState
                    {
                        ActiveTool = EditorProjectMapWorldEditActiveTool.ObjectPlacement,
                        Inspector = new EditorProjectMapObjectInspectorState
                        {
                            TargetMode = EditorProjectMapObjectInspectorTargetMode.ProtoDefinition,
                            PinnedProtoNumber = pinnedProtoNumber,
                            ActivePane = EditorObjectInspectorPane.Generator,
                        },
                    },
                }
            );

            _ = session.SetTrackedTerrainPaletteEntry("map-view-1", 1, 1);

            var inspectorState = session.GetTrackedObjectInspectorState("map-view-1");
            var inspector = session.GetTrackedObjectInspectorSummary("map-view-1");

            await Assert
                .That(inspectorState.TargetMode)
                .IsEqualTo(EditorProjectMapObjectInspectorTargetMode.ProtoDefinition);
            await Assert.That(inspectorState.PinnedProtoNumber).IsEqualTo(pinnedProtoNumber);
            await Assert.That(inspectorState.ActivePane).IsEqualTo(EditorObjectInspectorPane.Generator);
            await Assert.That(inspector.TargetKind).IsEqualTo(EditorObjectInspectorTargetKind.ProtoDefinition);
            await Assert.That(inspector.ProtoNumber).IsEqualTo(pinnedProtoNumber);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_ApplyObjectBrushRequest_FromAreaSelection_StagesProjectedRectangleTiles()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            var preview = workspace.CreateMapScenePreview("map01");
            var area = new EditorProjectMapAreaSelectionState
            {
                MinMapTileX = 63,
                MinMapTileY = 2,
                MaxMapTileX = 64,
                MaxMapTileY = 2,
            };

            var result = session.ApplySectorObjectBrush(
                preview,
                area,
                EditorMapObjectBrushRequest.StampFromProto(protoNumber)
            );

            await Assert.That(result.HasChanges).IsTrue();
            await Assert.That(result.CreatedObjectCount).IsEqualTo(2);
            await Assert.That(result.RemovedObjectCount).IsEqualTo(0);
            await Assert
                .That(
                    result
                        .CreatedObjects.Select(obj => obj.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                        .ToArray()
                )
                .IsEquivalentTo(new[] { (63, 2), (0, 2) });

            var updatedWorkspace = session.BeginChangeGroup("Brush area object selection").ApplyPendingChanges();
            var updatedSectorA = updatedWorkspace.FindSector(sectorAssetPathA);
            var updatedSectorB = updatedWorkspace.FindSector(sectorAssetPathB);

            await Assert.That(updatedSectorA).IsNotNull();
            await Assert.That(updatedSectorB).IsNotNull();
            await Assert.That(updatedSectorA!.Objects.Count).IsEqualTo(1);
            await Assert.That(updatedSectorB!.Objects.Count).IsEqualTo(1);
            await Assert
                .That(updatedSectorA.Objects.Single().GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((63, 2));
            await Assert
                .That(updatedSectorB.Objects.Single().GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((0, 2));
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_ApplyObjectBrushRequest_FromSelectionState_RoutesPointAndAreaSelections()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            var removedObjectA = new MobDataBuilder(MakePc(protoNumber)).WithLocation(63, 2).Build();
            var retainedObjectA = new MobDataBuilder(MakePc(protoNumber)).WithLocation(10, 10).Build();
            var removedObjectB = new MobDataBuilder(MakePc(protoNumber)).WithLocation(0, 2).Build();
            var retainedObjectB = new MobDataBuilder(MakePc(protoNumber)).WithLocation(3, 4).Build();

            SectorFormat.WriteToFile(
                MakeSector(removedObjectA, retainedObjectA),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(removedObjectB, retainedObjectB),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var preview = workspace.CreateMapScenePreview("map01");

            var pointSession = workspace.CreateSession();
            var pointSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(5, 6),
            };

            var pointResult = pointSession.ApplySectorObjectBrush(
                preview,
                pointSelection,
                EditorMapObjectBrushRequest.StampFromProto(protoNumber)
            );

            await Assert.That(pointResult.HasChanges).IsTrue();
            await Assert.That(pointResult.CreatedObjectCount).IsEqualTo(1);
            await Assert.That(pointResult.RemovedObjectCount).IsEqualTo(0);
            await Assert
                .That(pointResult.CreatedObjects.Single().GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((5, 6));

            var pointWorkspace = pointSession.BeginChangeGroup("Brush point object selection").ApplyPendingChanges();
            var pointSectorA = pointWorkspace.FindSector(sectorAssetPathA);

            await Assert.That(pointSectorA).IsNotNull();
            await Assert.That(pointSectorA!.Objects.Count).IsEqualTo(3);
            await Assert
                .That(
                    pointSectorA.Objects.Any(obj => obj.GetProperty(ObjectField.ObjFLocation)!.GetLocation() == (5, 6))
                )
                .IsTrue();

            var areaSession = workspace.CreateSession();
            var areaSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(63, 2),
                ObjectId = removedObjectA.Header.ObjectId,
                Area = new EditorProjectMapAreaSelectionState
                {
                    MinMapTileX = 63,
                    MinMapTileY = 2,
                    MaxMapTileX = 64,
                    MaxMapTileY = 2,
                    ObjectIds = [removedObjectA.Header.ObjectId, removedObjectB.Header.ObjectId],
                },
            };

            var areaResult = areaSession.ApplySectorObjectBrush(
                preview,
                areaSelection,
                EditorMapObjectBrushRequest.Erase()
            );

            await Assert.That(areaResult.HasChanges).IsTrue();
            await Assert.That(areaResult.CreatedObjectCount).IsEqualTo(0);
            await Assert.That(areaResult.RemovedObjectCount).IsEqualTo(2);
            await Assert.That(areaResult.RemovedObjectIds[0]).IsEqualTo(removedObjectA.Header.ObjectId);
            await Assert.That(areaResult.RemovedObjectIds[1]).IsEqualTo(removedObjectB.Header.ObjectId);

            var areaWorkspace = areaSession.BeginChangeGroup("Brush area object erase").ApplyPendingChanges();
            var areaSectorA = areaWorkspace.FindSector(sectorAssetPathA);
            var areaSectorB = areaWorkspace.FindSector(sectorAssetPathB);

            await Assert.That(areaSectorA).IsNotNull();
            await Assert.That(areaSectorB).IsNotNull();
            await Assert.That(areaSectorA!.Objects.Count).IsEqualTo(1);
            await Assert.That(areaSectorB!.Objects.Count).IsEqualTo(1);
            await Assert.That(areaSectorA.Objects.Single().Header.ObjectId).IsEqualTo(retainedObjectA.Header.ObjectId);
            await Assert.That(areaSectorB.Objects.Single().Header.ObjectId).IsEqualTo(retainedObjectB.Header.ObjectId);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_StageGroupedProtoStampAcrossSceneSectorHits()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            var proto = MakeProto(protoNumber);
            ProtoFormat.WriteToFile(proto, Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var groupedHits = new EditorMapSceneSectorHitGroup[]
            {
                new()
                {
                    SectorAssetPath = sectorAssetPathA,
                    LocalX = 0,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 5,
                            MapTileY = 6,
                            SectorAssetPath = sectorAssetPathA,
                            Tile = new Location(5, 6),
                            ObjectHits = [],
                        },
                        new EditorMapSceneHit
                        {
                            MapTileX = 5,
                            MapTileY = 6,
                            SectorAssetPath = sectorAssetPathA,
                            Tile = new Location(5, 6),
                            ObjectHits = [],
                        },
                        new EditorMapSceneHit
                        {
                            MapTileX = 7,
                            MapTileY = 8,
                            SectorAssetPath = sectorAssetPathA,
                            Tile = new Location(7, 8),
                            ObjectHits = [],
                        },
                    ],
                },
                new()
                {
                    SectorAssetPath = sectorAssetPathB,
                    LocalX = 1,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 65,
                            MapTileY = 2,
                            SectorAssetPath = sectorAssetPathB,
                            Tile = new Location(1, 2),
                            ObjectHits = [],
                        },
                    ],
                },
            };

            var addedObjects = session.AddSectorObjectsFromProto(groupedHits, protoNumber);

            await Assert.That(addedObjects.Count).IsEqualTo(3);
            await Assert.That(addedObjects.Select(obj => obj.Header.ObjectId).Distinct().Count()).IsEqualTo(3);
            await Assert.That(addedObjects[0].GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((5, 6));
            await Assert.That(addedObjects[1].GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((7, 8));
            await Assert.That(addedObjects[2].GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((1, 2));
            await Assert.That(addedObjects.All(obj => obj.Header.ProtoId.GetProtoNumber() == protoNumber)).IsTrue();
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(2);

            var summary = session.GetPendingChangeSummary();

            await Assert.That(summary.HasChanges).IsTrue();
            await Assert.That(summary.TotalChangeCount).IsEqualTo(2);
            await Assert.That(summary.Groups.Count).IsEqualTo(1);
            await Assert.That(summary.Groups[0].Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(summary.Groups[0].Targets.Count).IsEqualTo(2);

            var updatedWorkspace = session.BeginChangeGroup("Stamp sector objects").ApplyPendingChanges();
            var updatedSectorA = updatedWorkspace.FindSector(sectorAssetPathA);
            var updatedSectorB = updatedWorkspace.FindSector(sectorAssetPathB);

            await Assert.That(updatedSectorA).IsNotNull();
            await Assert.That(updatedSectorB).IsNotNull();
            await Assert.That(updatedSectorA!.Objects.Count).IsEqualTo(2);
            await Assert.That(updatedSectorB!.Objects.Count).IsEqualTo(1);

            await Assert
                .That(
                    updatedSectorA
                        .Objects.Select(obj => obj.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                        .ToArray()
                )
                .IsEquivalentTo(new[] { (5, 6), (7, 8) });
            await Assert
                .That(
                    updatedSectorB
                        .Objects.Select(obj => obj.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                        .ToArray()
                )
                .IsEquivalentTo(new[] { (1, 2) });

            var preview = updatedWorkspace.CreateMapScenePreview("map01");
            var previewSectorA = preview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathA);
            var previewSectorB = preview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathB);

            await Assert.That(previewSectorA.Objects.Count).IsEqualTo(2);
            await Assert.That(previewSectorB.Objects.Count).IsEqualTo(1);
            await Assert
                .That(previewSectorA.Objects.Select(obj => obj.Location).ToArray())
                .IsEquivalentTo(new[] { new Location(5, 6), new Location(7, 8) });
            await Assert.That(previewSectorB.Objects.Single().Location).IsEqualTo(new Location(1, 2));

            var undoHistory = session.GetUndoHistory();

            await Assert.That(undoHistory.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Label).IsEqualTo("Stamp sector objects");
            await Assert.That(undoHistory[0].Changes.Count).IsEqualTo(2);
            await Assert
                .That(undoHistory[0].Changes.All(change => change.Kind == EditorSessionChangeKind.Sector))
                .IsTrue();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_StageGroupedEraseAcrossSceneSectorHits()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            var removedObjectA = new MobDataBuilder(MakePc(protoNumber)).WithLocation(5, 6).Build();
            var removedObjectB = new MobDataBuilder(MakePc(protoNumber)).WithLocation(7, 8).Build();
            var retainedObjectA = new MobDataBuilder(MakePc(protoNumber)).WithLocation(9, 10).Build();
            var removedObjectC = new MobDataBuilder(MakePc(protoNumber)).WithLocation(1, 2).Build();
            var retainedObjectB = new MobDataBuilder(MakePc(protoNumber)).WithLocation(3, 4).Build();

            SectorFormat.WriteToFile(
                MakeSector(removedObjectA, removedObjectB, retainedObjectA),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(removedObjectC, retainedObjectB),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var groupedHits = new EditorMapSceneSectorHitGroup[]
            {
                new()
                {
                    SectorAssetPath = sectorAssetPathA,
                    LocalX = 0,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 5,
                            MapTileY = 6,
                            SectorAssetPath = sectorAssetPathA,
                            Tile = new Location(5, 6),
                            ObjectHits = [CreateSceneObjectHit(removedObjectA, 5, 6)],
                        },
                        new EditorMapSceneHit
                        {
                            MapTileX = 5,
                            MapTileY = 6,
                            SectorAssetPath = sectorAssetPathA,
                            Tile = new Location(5, 6),
                            ObjectHits = [CreateSceneObjectHit(removedObjectA, 5, 6)],
                        },
                        new EditorMapSceneHit
                        {
                            MapTileX = 7,
                            MapTileY = 8,
                            SectorAssetPath = sectorAssetPathA,
                            Tile = new Location(7, 8),
                            ObjectHits = [CreateSceneObjectHit(removedObjectB, 7, 8)],
                        },
                    ],
                },
                new()
                {
                    SectorAssetPath = sectorAssetPathB,
                    LocalX = 1,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 65,
                            MapTileY = 2,
                            SectorAssetPath = sectorAssetPathB,
                            Tile = new Location(1, 2),
                            ObjectHits = [CreateSceneObjectHit(removedObjectC, 1, 2)],
                        },
                    ],
                },
            };

            var removedObjectIds = session.RemoveSectorObjects(groupedHits);

            await Assert.That(removedObjectIds.Count).IsEqualTo(3);
            await Assert.That(removedObjectIds[0]).IsEqualTo(removedObjectA.Header.ObjectId);
            await Assert.That(removedObjectIds[1]).IsEqualTo(removedObjectB.Header.ObjectId);
            await Assert.That(removedObjectIds[2]).IsEqualTo(removedObjectC.Header.ObjectId);
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(2);

            var summary = session.GetPendingChangeSummary();

            await Assert.That(summary.HasChanges).IsTrue();
            await Assert.That(summary.TotalChangeCount).IsEqualTo(2);
            await Assert.That(summary.Groups.Count).IsEqualTo(1);
            await Assert.That(summary.Groups[0].Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(summary.Groups[0].Targets.Count).IsEqualTo(2);

            var updatedWorkspace = session.BeginChangeGroup("Erase sector objects").ApplyPendingChanges();
            var updatedSectorA = updatedWorkspace.FindSector(sectorAssetPathA);
            var updatedSectorB = updatedWorkspace.FindSector(sectorAssetPathB);

            await Assert.That(updatedSectorA).IsNotNull();
            await Assert.That(updatedSectorB).IsNotNull();
            await Assert.That(updatedSectorA!.Objects.Count).IsEqualTo(1);
            await Assert.That(updatedSectorB!.Objects.Count).IsEqualTo(1);
            await Assert
                .That(updatedSectorA.Objects.Single().Header.ObjectId)
                .IsEqualTo(retainedObjectA.Header.ObjectId);
            await Assert
                .That(updatedSectorB.Objects.Single().Header.ObjectId)
                .IsEqualTo(retainedObjectB.Header.ObjectId);

            var preview = updatedWorkspace.CreateMapScenePreview("map01");
            var previewSectorA = preview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathA);
            var previewSectorB = preview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathB);

            await Assert.That(previewSectorA.Objects.Count).IsEqualTo(1);
            await Assert.That(previewSectorB.Objects.Count).IsEqualTo(1);
            await Assert.That(previewSectorA.Objects.Single().ObjectId).IsEqualTo(retainedObjectA.Header.ObjectId);
            await Assert.That(previewSectorB.Objects.Single().ObjectId).IsEqualTo(retainedObjectB.Header.ObjectId);

            var undoHistory = session.GetUndoHistory();

            await Assert.That(undoHistory.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Label).IsEqualTo("Erase sector objects");
            await Assert.That(undoHistory[0].Changes.Count).IsEqualTo(2);
            await Assert
                .That(undoHistory[0].Changes.All(change => change.Kind == EditorSessionChangeKind.Sector))
                .IsTrue();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_ApplyObjectPalettePlacement_RoutesPointAndAreaSelections()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        const float pointRotation = 0.5f;
        const float pointRotationPitch = 1.25f;
        const float areaRotation = 2.5f;
        const float areaRotationPitch = 4.25f;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            MessageFormat.WriteToFile(
                new MesFile { Entries = [new MessageEntry(protoNumber, "Palette proto")] },
                Path.Combine(contentDir, "mes", "description.mes")
            );
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var preview = workspace.CreateMapScenePreview("map01");
            var paletteEntry = workspace.FindObjectPaletteEntry(protoNumber);

            await Assert.That(paletteEntry).IsNotNull();

            var pointSession = workspace.CreateSession();
            var pointSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(5, 6),
            };

            var pointCreatedObjects = pointSession.ApplySectorObjectPalettePlacement(
                preview,
                pointSelection,
                paletteEntry!.CreatePlacementRequest(
                    deltaTileX: 1,
                    deltaTileY: 2,
                    rotation: pointRotation,
                    rotationPitch: pointRotationPitch
                )
            );

            await Assert.That(pointCreatedObjects.Count).IsEqualTo(1);

            var pointWorkspace = pointSession.BeginChangeGroup("Palette place point selection").ApplyPendingChanges();
            var pointSectorA = pointWorkspace.FindSector(sectorAssetPathA);
            var pointPreview = pointWorkspace.CreateMapScenePreview("map01");
            var pointPreviewSectorA = pointPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathA);

            await Assert.That(pointSectorA).IsNotNull();

            var createdPointObjectId = pointCreatedObjects[0].Header.ObjectId;
            var createdPointObject = pointSectorA!.Objects.Single(obj => obj.Header.ObjectId == createdPointObjectId);
            await Assert
                .That(createdPointObject.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((6, 8));
            await Assert
                .That(createdPointObject.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat())
                .IsEqualTo(pointRotation);
            await Assert
                .That(createdPointObject.GetProperty(ObjectField.ObjFRotationPitch)!.GetFloat())
                .IsEqualTo(pointRotationPitch);
            await Assert
                .That(pointPreviewSectorA.Objects.Single(obj => obj.ObjectId == createdPointObjectId).Location)
                .IsEqualTo(new Location(6, 8));

            var areaSession = workspace.CreateSession();
            var areaSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(63, 2),
                Area = new EditorProjectMapAreaSelectionState
                {
                    MinMapTileX = 63,
                    MinMapTileY = 2,
                    MaxMapTileX = 64,
                    MaxMapTileY = 2,
                },
            };

            var areaCreatedObjects = areaSession.ApplySectorObjectPalettePlacement(
                preview,
                areaSelection,
                paletteEntry.CreatePlacementRequest(
                    deltaTileX: 0,
                    deltaTileY: 1,
                    rotation: areaRotation,
                    rotationPitch: areaRotationPitch
                )
            );

            await Assert.That(areaCreatedObjects.Count).IsEqualTo(2);

            var areaWorkspace = areaSession.BeginChangeGroup("Palette place area selection").ApplyPendingChanges();
            var areaSectorA = areaWorkspace.FindSector(sectorAssetPathA);
            var areaSectorB = areaWorkspace.FindSector(sectorAssetPathB);
            var areaPreview = areaWorkspace.CreateMapScenePreview("map01");
            var areaPreviewSectorA = areaPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathA);
            var areaPreviewSectorB = areaPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathB);

            await Assert.That(areaSectorA).IsNotNull();
            await Assert.That(areaSectorB).IsNotNull();

            var areaObjectAId = areaCreatedObjects[0].Header.ObjectId;
            var areaObjectBId = areaCreatedObjects[1].Header.ObjectId;
            var createdAreaObjectA = areaSectorA!.Objects.Single(obj => obj.Header.ObjectId == areaObjectAId);
            var createdAreaObjectB = areaSectorB!.Objects.Single(obj => obj.Header.ObjectId == areaObjectBId);

            await Assert
                .That(createdAreaObjectA.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((63, 3));
            await Assert
                .That(createdAreaObjectB.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((0, 3));
            await Assert
                .That(createdAreaObjectA.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat())
                .IsEqualTo(areaRotation);
            await Assert
                .That(createdAreaObjectB.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat())
                .IsEqualTo(areaRotation);
            await Assert
                .That(createdAreaObjectA.GetProperty(ObjectField.ObjFRotationPitch)!.GetFloat())
                .IsEqualTo(areaRotationPitch);
            await Assert
                .That(createdAreaObjectB.GetProperty(ObjectField.ObjFRotationPitch)!.GetFloat())
                .IsEqualTo(areaRotationPitch);
            await Assert
                .That(areaPreviewSectorA.Objects.Single(obj => obj.ObjectId == areaObjectAId).Location)
                .IsEqualTo(new Location(63, 3));
            await Assert
                .That(areaPreviewSectorB.Objects.Single(obj => obj.ObjectId == areaObjectBId).Location)
                .IsEqualTo(new Location(0, 3));
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_ApplyObjectPalettePlacementPreset_RoutesPointAndAreaSelections()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        const float pointRotation = 0.5f;
        const float areaRotation = 1.25f;
        const float areaRotationPitch = 2.25f;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            MessageFormat.WriteToFile(
                new MesFile { Entries = [new MessageEntry(protoNumber, "Palette proto")] },
                Path.Combine(contentDir, "mes", "description.mes")
            );
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var preview = workspace.CreateMapScenePreview("map01");
            var paletteEntry = workspace.FindObjectPaletteEntry(protoNumber);

            await Assert.That(paletteEntry).IsNotNull();

            var pointPreset = paletteEntry!.CreatePlacementPreset(
                "preset.point",
                deltaTileX: 1,
                rotation: pointRotation
            );
            var pointSession = workspace.CreateSession();
            var pointSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(5, 6),
            };

            var pointCreatedObjects = pointSession.ApplySectorObjectPalettePlacementPreset(
                preview,
                pointSelection,
                pointPreset
            );

            await Assert.That(pointCreatedObjects.Count).IsEqualTo(1);

            var pointWorkspace = pointSession.BeginChangeGroup("Palette preset point selection").ApplyPendingChanges();
            var pointSectorA = pointWorkspace.FindSector(sectorAssetPathA);
            var pointPreview = pointWorkspace.CreateMapScenePreview("map01");
            var pointPreviewSectorA = pointPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathA);

            await Assert.That(pointSectorA).IsNotNull();

            var pointObjectId = pointCreatedObjects[0].Header.ObjectId;
            var pointObject = pointSectorA!.Objects.Single(obj => obj.Header.ObjectId == pointObjectId);
            await Assert.That(pointObject.GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((6, 6));
            await Assert.That(pointObject.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat()).IsEqualTo(pointRotation);
            await Assert
                .That(pointPreviewSectorA.Objects.Single(obj => obj.ObjectId == pointObjectId).Location)
                .IsEqualTo(new Location(6, 6));

            var areaPreset = EditorObjectPalettePlacementPreset.Create(
                "preset.area",
                "Area preset",
                "Two-object named preset",
                EditorObjectPalettePlacementRequest.Place(protoNumber, rotation: areaRotation),
                EditorObjectPalettePlacementRequest.Place(
                    protoNumber,
                    deltaTileY: 1,
                    rotation: areaRotation,
                    rotationPitch: areaRotationPitch
                )
            );
            var areaSession = workspace.CreateSession();
            var areaSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(63, 2),
                Area = new EditorProjectMapAreaSelectionState
                {
                    MinMapTileX = 63,
                    MinMapTileY = 2,
                    MaxMapTileX = 64,
                    MaxMapTileY = 2,
                },
            };

            var areaCreatedObjects = areaSession.ApplySectorObjectPalettePlacementPreset(
                preview,
                areaSelection,
                areaPreset
            );

            await Assert.That(areaCreatedObjects.Count).IsEqualTo(4);

            var areaWorkspace = areaSession.BeginChangeGroup("Palette preset area selection").ApplyPendingChanges();
            var areaSectorA = areaWorkspace.FindSector(sectorAssetPathA);
            var areaSectorB = areaWorkspace.FindSector(sectorAssetPathB);
            var areaPreview = areaWorkspace.CreateMapScenePreview("map01");
            var areaPreviewSectorA = areaPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathA);
            var areaPreviewSectorB = areaPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathB);

            await Assert.That(areaSectorA).IsNotNull();
            await Assert.That(areaSectorB).IsNotNull();

            var areaAnchorAId = areaCreatedObjects[0].Header.ObjectId;
            var areaOffsetAId = areaCreatedObjects[1].Header.ObjectId;
            var areaAnchorBId = areaCreatedObjects[2].Header.ObjectId;
            var areaOffsetBId = areaCreatedObjects[3].Header.ObjectId;
            var areaAnchorA = areaSectorA!.Objects.Single(obj => obj.Header.ObjectId == areaAnchorAId);
            var areaOffsetA = areaSectorA.Objects.Single(obj => obj.Header.ObjectId == areaOffsetAId);
            var areaAnchorB = areaSectorB!.Objects.Single(obj => obj.Header.ObjectId == areaAnchorBId);
            var areaOffsetB = areaSectorB.Objects.Single(obj => obj.Header.ObjectId == areaOffsetBId);

            await Assert.That(areaAnchorA.GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((63, 2));
            await Assert.That(areaOffsetA.GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((63, 3));
            await Assert.That(areaAnchorB.GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((0, 2));
            await Assert.That(areaOffsetB.GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((0, 3));
            await Assert.That(areaAnchorA.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat()).IsEqualTo(areaRotation);
            await Assert.That(areaAnchorB.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat()).IsEqualTo(areaRotation);
            await Assert
                .That(areaOffsetA.GetProperty(ObjectField.ObjFRotationPitch)!.GetFloat())
                .IsEqualTo(areaRotationPitch);
            await Assert
                .That(areaOffsetB.GetProperty(ObjectField.ObjFRotationPitch)!.GetFloat())
                .IsEqualTo(areaRotationPitch);
            await Assert
                .That(areaPreviewSectorA.Objects.Single(obj => obj.ObjectId == areaAnchorAId).Location)
                .IsEqualTo(new Location(63, 2));
            await Assert
                .That(areaPreviewSectorB.Objects.Single(obj => obj.ObjectId == areaOffsetBId).Location)
                .IsEqualTo(new Location(0, 3));
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_ObjectPaletteEntryPlacementDefaults_SnapProtoOffsetsToTileGrid()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            MessageFormat.WriteToFile(
                new MesFile { Entries = [new MessageEntry(protoNumber, "Palette proto")] },
                Path.Combine(contentDir, "mes", "description.mes")
            );
            ProtoFormat.WriteToFile(
                WithProperties(
                    MakeProto(protoNumber),
                    ObjectPropertyFactory.ForInt32(ObjectField.ObjFOffsetX, 13),
                    ObjectPropertyFactory.ForInt32(ObjectField.ObjFOffsetY, -9)
                ),
                Path.Combine(contentDir, "proto", "001001 - Test.pro")
            );
            SectorFormat.WriteToFile(MakeSector(), Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var preview = workspace.CreateMapScenePreview("map01");
            var paletteEntry = workspace.FindObjectPaletteEntry(protoNumber);

            await Assert.That(paletteEntry).IsNotNull();

            var session = workspace.CreateSession();
            var selection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPath,
                Tile = new Location(5, 6),
            };

            var createdObjects = session.ApplySectorObjectPalettePlacement(
                preview,
                selection,
                paletteEntry!.CreatePlacementRequest()
            );

            await Assert.That(createdObjects.Count).IsEqualTo(1);
            await Assert.That(createdObjects[0].GetProperty(ObjectField.ObjFOffsetX)!.GetInt32()).IsEqualTo(0);
            await Assert.That(createdObjects[0].GetProperty(ObjectField.ObjFOffsetY)!.GetInt32()).IsEqualTo(0);

            var updatedWorkspace = session
                .BeginChangeGroup("Palette place snapped object selection")
                .ApplyPendingChanges();
            var sector = updatedWorkspace.FindSector(sectorAssetPath);
            var scene = updatedWorkspace.CreateMapScenePreview("map01");
            var previewSector = scene.Sectors.Single(candidate => candidate.AssetPath == sectorAssetPath);
            var createdObjectId = createdObjects[0].Header.ObjectId;

            await Assert.That(sector).IsNotNull();
            var createdObject = sector!.Objects.Single(obj => obj.Header.ObjectId == createdObjectId);
            await Assert.That(createdObject.GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((5, 6));
            await Assert.That(createdObject.GetProperty(ObjectField.ObjFOffsetX)!.GetInt32()).IsEqualTo(0);
            await Assert.That(createdObject.GetProperty(ObjectField.ObjFOffsetY)!.GetInt32()).IsEqualTo(0);
            await Assert
                .That(previewSector.Objects.Single(obj => obj.ObjectId == createdObjectId).IsTileGridSnapped)
                .IsTrue();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_PreviewObjectPalettePlacement_ProjectsSnappedGhostIntoUnifiedRenderQueue()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            MessageFormat.WriteToFile(
                new MesFile { Entries = [new MessageEntry(protoNumber, "Palette proto")] },
                Path.Combine(contentDir, "mes", "description.mes")
            );
            ProtoFormat.WriteToFile(
                WithProperties(
                    MakeProto(protoNumber),
                    ObjectPropertyFactory.ForInt32(ObjectField.ObjFOffsetX, 13),
                    ObjectPropertyFactory.ForInt32(ObjectField.ObjFOffsetY, -9)
                ),
                Path.Combine(contentDir, "proto", "001001 - Test.pro")
            );

            var existingObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(5, 5).Build();
            var sector = MakeSector(existingObject);
            sector.Tiles[(5 * 64) + 5] = 100u;
            sector.Tiles[(6 * 64) + 6] = 200u;
            SectorFormat.WriteToFile(sector, Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            var scenePreview = workspace.CreateMapScenePreview("map01");
            var paletteEntry = workspace.FindObjectPaletteEntry(protoNumber);

            await Assert.That(paletteEntry).IsNotNull();

            var selection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPath,
                Tile = new Location(6, 6),
            };

            var preview = session.PreviewSectorObjectPalettePlacement(
                scenePreview,
                selection,
                paletteEntry!.CreatePlacementRequest()
            );

            await Assert.That(preview.Objects.Count).IsEqualTo(1);
            await Assert.That(preview.RenderQueue.Count).IsEqualTo(4);
            await Assert
                .That(preview.RenderQueue.Any(item => item.Kind == EditorMapRenderQueueItemKind.Object))
                .IsTrue();
            await Assert
                .That(preview.RenderQueue.Any(item => item.Kind == EditorMapRenderQueueItemKind.PlacementPreviewObject))
                .IsTrue();

            var previewObject = preview.Objects[0];
            var previewTile = preview
                .RenderQueue.Where(item => item.Kind == EditorMapRenderQueueItemKind.FloorTile)
                .Select(item => item.Tile)
                .OfType<EditorMapFloorTileRenderItem>()
                .Single(tile => tile.MapTileX == 6 && tile.MapTileY == 6);

            await Assert.That(previewObject.Tile).IsEqualTo(new Location(6, 6));
            await Assert.That(previewObject.IsTileGridSnapped).IsTrue();
            await Assert.That(previewObject.State).IsEqualTo(EditorMapPlacementPreviewState.Valid);
            await Assert.That(previewObject.ValidationMessage).IsNull();
            await Assert.That(previewObject.SuggestedOpacity).IsEqualTo(0.85d);
            await Assert.That(previewObject.SuggestedTintColor).IsEqualTo(0xAA66CC66u);
            await Assert.That(previewObject.AnchorX).IsEqualTo(previewTile.CenterX);
            await Assert.That(previewObject.AnchorY).IsEqualTo(previewTile.CenterY);

            var loadedSector = workspace.FindSector(sectorAssetPath);
            await Assert.That(loadedSector).IsNotNull();
            await Assert.That(loadedSector!.Objects.Count).IsEqualTo(1);
            await Assert.That(session.HasPendingChanges).IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewRenderHelpers_CreateMapFloorRenderPreview_UsesPreviewFlagsAndPendingSectorState()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            MessageFormat.WriteToFile(
                new MesFile { Entries = [new MessageEntry(protoNumber, "Palette proto")] },
                Path.Combine(contentDir, "mes", "description.mes")
            );
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            var existingObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(5, 5).Build();
            var sector = new SectorBuilder(MakeSector(existingObject))
                .SetTile(5, 5, 100u)
                .SetRoof(0, 15, 999u)
                .AddLight(MakeSectorLight(5, 5, artId: 0x01020304u))
                .AddTileScript(MakeTileScript((5u * 64u) + 5u, 77))
                .Build();
            SectorFormat.WriteToFile(sector, Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-scene",
                    MapName = "map01",
                    ViewId = "scene",
                    Camera = new EditorProjectMapCameraState(),
                    Selection = new EditorProjectMapSelectionState(),
                    Preview = new EditorProjectMapPreviewState
                    {
                        UseScenePreview = true,
                        ShowObjects = false,
                        ShowRoofs = false,
                        ShowLights = false,
                        ShowBlockedTiles = true,
                        ShowScripts = false,
                    },
                }
            );

            var blockedChange = session.SetSectorBlockedTile(sectorAssetPath, 5, 5, blocked: true);
            var preview = session.CreateMapFloorRenderPreview(
                "map-view-scene",
                new EditorMapFloorRenderRequest
                {
                    ViewMode = EditorMapSceneViewMode.TopDown,
                    TileWidthPixels = 32d,
                    TileHeightPixels = 32d,
                }
            );

            await Assert.That(blockedChange).IsNotNull();
            await Assert.That(session.HasPendingChanges).IsTrue();
            await Assert.That(preview.MapName).IsEqualTo("map01");
            await Assert.That(preview.ViewMode).IsEqualTo(EditorMapSceneViewMode.TopDown);
            await Assert.That(preview.TileWidthPixels).IsEqualTo(32d);
            await Assert.That(preview.TileHeightPixels).IsEqualTo(32d);
            await Assert.That(preview.Tiles.Count).IsEqualTo(1);
            await Assert.That(preview.Objects).IsEmpty();
            await Assert.That(preview.Roofs).IsEmpty();
            await Assert.That(preview.Overlays.Count).IsEqualTo(1);
            await Assert.That(preview.Overlays[0].Kind).IsEqualTo(EditorMapTileOverlayKind.BlockedTile);
            await Assert.That(preview.Overlays[0].MapTileX).IsEqualTo(5);
            await Assert.That(preview.Overlays[0].MapTileY).IsEqualTo(5);
            await Assert.That(preview.Overlays[0].CenterX).IsEqualTo(16d);
            await Assert.That(preview.Overlays[0].CenterY).IsEqualTo(16d);
            await Assert.That(preview.RenderQueue.Count).IsEqualTo(2);
            await Assert.That(preview.RenderQueue[0].Kind).IsEqualTo(EditorMapRenderQueueItemKind.FloorTile);
            await Assert.That(preview.RenderQueue[1].Kind).IsEqualTo(EditorMapRenderQueueItemKind.TileOverlay);
            await Assert.That(preview.RenderQueue[1].TileOverlay?.Kind).IsEqualTo(EditorMapTileOverlayKind.BlockedTile);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewRenderHelpers_PreviewObjectPalettePlacement_UsesMapViewSelectionFlagsAndPendingSectorState()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            MessageFormat.WriteToFile(
                new MesFile { Entries = [new MessageEntry(protoNumber, "Palette proto")] },
                Path.Combine(contentDir, "mes", "description.mes")
            );
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            var existingObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(5, 5).Build();
            var sector = new SectorBuilder(MakeSector(existingObject))
                .SetTile(5, 5, 100u)
                .SetTile(6, 6, 200u)
                .SetRoof(0, 15, 999u)
                .AddLight(MakeSectorLight(6, 6, artId: 0x01020304u))
                .AddTileScript(MakeTileScript((6u * 64u) + 6u, 77))
                .Build();
            SectorFormat.WriteToFile(sector, Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            var paletteEntry = workspace.FindObjectPaletteEntry(protoNumber);

            await Assert.That(paletteEntry).IsNotNull();

            session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-scene",
                    MapName = "map01",
                    ViewId = "scene",
                    Camera = new EditorProjectMapCameraState(),
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(6, 6),
                    },
                    Preview = new EditorProjectMapPreviewState
                    {
                        UseScenePreview = true,
                        ShowObjects = false,
                        ShowRoofs = false,
                        ShowLights = false,
                        ShowBlockedTiles = true,
                        ShowScripts = false,
                    },
                }
            );

            var blockedChange = session.SetSectorBlockedTile(sectorAssetPath, 6, 6, blocked: true);
            var preview = session.PreviewSectorObjectPalettePlacement(
                "map-view-scene",
                paletteEntry!.CreatePlacementRequest(),
                new EditorMapFloorRenderRequest
                {
                    ViewMode = EditorMapSceneViewMode.TopDown,
                    TileWidthPixels = 32d,
                    TileHeightPixels = 32d,
                }
            );

            await Assert.That(blockedChange).IsNotNull();
            await Assert.That(session.HasPendingChanges).IsTrue();
            await Assert.That(preview.Objects.Count).IsEqualTo(1);
            await Assert.That(preview.Objects[0].Tile).IsEqualTo(new Location(6, 6));
            await Assert.That(preview.Objects[0].State).IsEqualTo(EditorMapPlacementPreviewState.BlockedTile);
            await Assert.That(preview.Objects[0].ValidationMessage).IsEqualTo("Targets a blocked tile.");
            await Assert.That(preview.Objects[0].SuggestedTintColor).IsEqualTo(0xAACC6666u);
            await Assert.That(preview.RenderQueue.Count).IsEqualTo(4);
            await Assert
                .That(preview.RenderQueue.Count(item => item.Kind == EditorMapRenderQueueItemKind.FloorTile))
                .IsEqualTo(2);
            await Assert
                .That(
                    preview.RenderQueue.Any(item =>
                        item.Kind == EditorMapRenderQueueItemKind.TileOverlay
                        && item.TileOverlay?.Kind == EditorMapTileOverlayKind.BlockedTile
                    )
                )
                .IsTrue();
            await Assert
                .That(
                    preview.RenderQueue.Any(item =>
                        item.Kind == EditorMapRenderQueueItemKind.PlacementPreviewObject
                        && item.PlacementPreviewObject?.State == EditorMapPlacementPreviewState.BlockedTile
                    )
                )
                .IsTrue();
            await Assert
                .That(preview.RenderQueue.Any(item => item.Kind == EditorMapRenderQueueItemKind.Object))
                .IsFalse();
            await Assert
                .That(preview.RenderQueue.Any(item => item.Kind == EditorMapRenderQueueItemKind.Roof))
                .IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapBootstrapHelpers_ResolveDefaultMap_PrefersSaveMapIdThenMap01Fallback()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map02"));

        try
        {
            SectorFormat.WriteToFile(MakeSector(), Path.Combine(contentDir, "maps", "map01", "101334386389.sec"));
            SectorFormat.WriteToFile(MakeSector(), Path.Combine(contentDir, "maps", "map02", "101334386390.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var fallbackMap = workspace.ResolveDefaultMap();

            var save = CreateLoadedSave(
                new SaveInfo
                {
                    ModuleName = "Arcanum",
                    LeaderName = "Virgil",
                    DisplayName = "Slot",
                    MapId = 2,
                    GameTimeDays = 1,
                    GameTimeMs = 2,
                    LeaderPortraitId = 3,
                    LeaderLevel = 4,
                    LeaderTileX = 5,
                    LeaderTileY = 6,
                    StoryState = 0,
                }
            );
            var workspaceWithSave = CloneWorkspaceWithSave(workspace, save);
            var resolvedMap = workspaceWithSave.ResolveDefaultMap();

            await Assert.That(fallbackMap).IsNotNull();
            await Assert.That(fallbackMap!.MapName).IsEqualTo("map01");
            await Assert.That(fallbackMap.Source).IsEqualTo(EditorWorkspaceDefaultMapSource.ConventionalMap01);
            await Assert.That(resolvedMap).IsNotNull();
            await Assert.That(resolvedMap!.MapName).IsEqualTo("map02");
            await Assert.That(resolvedMap.Source).IsEqualTo(EditorWorkspaceDefaultMapSource.SaveInfoMapId);
            await Assert.That(resolvedMap.SaveMapId).IsEqualTo(2);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapBootstrapHelpers_CreateDefaultMapWorldEditScene_UsesResolvedDefaultMapAndCenteredCamera()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map02"));

        try
        {
            var sector = new SectorBuilder(MakeSector()).SetTile(0, 0, 100u).Build();
            SectorFormat.WriteToFile(sector, Path.Combine(contentDir, "maps", "map01", "101334386389.sec"));
            SectorFormat.WriteToFile(MakeSector(), Path.Combine(contentDir, "maps", "map02", "101334386390.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            var scene = session.CreateDefaultMapWorldEditScene(
                request: new EditorMapWorldEditSceneRequest
                {
                    RenderRequest = new EditorMapFloorRenderRequest
                    {
                        ViewMode = EditorMapSceneViewMode.Isometric,
                        TileWidthPixels = 64d,
                        TileHeightPixels = 32d,
                    },
                    ViewportWidth = 320d,
                    ViewportHeight = 200d,
                }
            );

            await Assert.That(scene.MapName).IsEqualTo("map01");
            await Assert.That(scene.MapViewState.MapName).IsEqualTo("map01");
            await Assert.That(scene.SceneRender.Tiles.Count).IsEqualTo(1);
            await Assert.That(scene.ViewportLayout.ViewportWidth).IsEqualTo(320d);
            await Assert.That(scene.ViewportLayout.ViewportHeight).IsEqualTo(200d);
            await Assert.That(scene.ViewportLayout.CenterRenderX).IsGreaterThan(0d);
            await Assert.That(scene.ViewportLayout.CenterRenderY).IsGreaterThan(0d);
            await Assert.That(scene.PaintableScene.Items.Count).IsEqualTo(scene.SceneRender.RenderQueue.Count);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapBootstrapHelpers_CreateDefaultMapWorldEditScene_AutoBindsConservativeNumericArtForPaintableScene()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var protoId = MakeProtoId(protoNumber);
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid());
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "art"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(
                WithProperties(MakeProto(protoNumber), MakeArtProperty(ObjectField.ObjFCurrentAid, 200u)),
                Path.Combine(contentDir, "proto", "001001 - Test.pro")
            );
            ArtFormat.WriteToFile(CreateArtFile(1, 1, [1]), Path.Combine(contentDir, "art", "200.art"));
            SectorFormat.WriteToFile(
                new SectorBuilder(
                    MakeSector(
                        new CharacterBuilder(ObjectType.Npc, objectId, protoId)
                            .WithHitPoints(80)
                            .WithLocation(0, 0)
                            .WithProperty(MakeArtProperty(ObjectField.ObjFCurrentAid, 200u))
                            .Build()
                    )
                )
                    .SetTile(0, 0, 1u)
                    .Build(),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            var scene = session.CreateDefaultMapWorldEditScene(
                request: new EditorMapWorldEditSceneRequest
                {
                    RenderRequest = new EditorMapFloorRenderRequest
                    {
                        ViewMode = EditorMapSceneViewMode.Isometric,
                        TileWidthPixels = 64d,
                        TileHeightPixels = 32d,
                    },
                    PlacementRequest = workspace.FindObjectPaletteEntry(protoNumber)!.CreatePlacementRequest(),
                }
            );

            await Assert.That(scene.PaintableScene.Items.Any(item => item.Sprite is not null)).IsTrue();
            await Assert.That(scene.SpriteCoverage.ResolvedArtIds).Contains(new ArtId(200u));
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapBootstrapHelpers_CreateMapWorldEditScene_ComposesViewportPaintableSceneAndPlacementGhost()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "art"));
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            MessageFormat.WriteToFile(
                new MesFile { Entries = [new MessageEntry(protoNumber, "Palette proto")] },
                Path.Combine(contentDir, "mes", "description.mes")
            );
            ProtoFormat.WriteToFile(
                WithProperties(MakeProto(protoNumber), MakeArtProperty(ObjectField.ObjFCurrentAid, 200u)),
                Path.Combine(contentDir, "proto", "001001 - Test.pro")
            );
            ArtFormat.WriteToFile(CreateArtFile(1, 1, [1]), Path.Combine(contentDir, "art", "test.art"));

            var sector = new SectorBuilder(MakeSector()).SetTile(6, 6, 100u).SetTile(7, 6, 100u).Build();
            SectorFormat.WriteToFile(sector, Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            var paletteEntry = workspace.FindObjectPaletteEntry(protoNumber);
            var artResolver = workspace.CreateArtResolver();
            artResolver.Bind(new ArtId(100u), "art/test.art");
            artResolver.Bind(new ArtId(200u), "art/test.art");

            await Assert.That(paletteEntry).IsNotNull();

            session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "world-edit",
                    MapName = "map01",
                    ViewId = "scene",
                    Camera = new EditorProjectMapCameraState
                    {
                        CenterTileX = 6d,
                        CenterTileY = 6d,
                        Zoom = 2d,
                    },
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(6, 6),
                    },
                    Preview = new EditorProjectMapPreviewState
                    {
                        UseScenePreview = true,
                        ShowObjects = true,
                        ShowRoofs = true,
                        ShowLights = true,
                        ShowBlockedTiles = true,
                        ShowScripts = true,
                    },
                }
            );

            var worldScene = session.CreateMapWorldEditScene(
                "world-edit",
                new EditorMapWorldEditSceneRequest
                {
                    RenderRequest = new EditorMapFloorRenderRequest
                    {
                        ViewMode = EditorMapSceneViewMode.Isometric,
                        TileWidthPixels = 64d,
                        TileHeightPixels = 32d,
                    },
                    PlacementRequest = paletteEntry!.CreatePlacementRequest(),
                    ViewportWidth = 320d,
                    ViewportHeight = 200d,
                    ArtResolver = artResolver,
                }
            );

            var tileCenter = EditorMapSceneRenderSpaceMath.ProjectMapTileCenter(worldScene.SceneRender, 6d, 6d);
            var viewportX = EditorMapSceneRenderSpaceMath.RenderToViewportX(worldScene.ViewportLayout, tileCenter.X);
            var viewportY = EditorMapSceneRenderSpaceMath.RenderToViewportY(worldScene.ViewportLayout, tileCenter.Y);
            var hit = EditorMapSceneRenderSpaceMath.HitTestScene(
                worldScene.SceneRender,
                worldScene.ViewportLayout,
                viewportX,
                viewportY
            );

            await Assert.That(worldScene.MapName).IsEqualTo("map01");
            await Assert.That(worldScene.SceneRender.Tiles.Count).IsEqualTo(2);
            await Assert.That(worldScene.PlacementPreview).IsNotNull();
            await Assert.That(worldScene.ViewportLayout.Zoom).IsEqualTo(2d);
            await Assert.That(worldScene.SpriteCoverage.IsComplete).IsTrue();
            await Assert.That(worldScene.SpriteCoverage.ReferencedArtIds.Count).IsEqualTo(2);
            await Assert.That(worldScene.SpriteCoverage.ResolvedArtIds.Count).IsEqualTo(2);
            await Assert.That(worldScene.SpriteCoverage.UnresolvedArtIds).IsEmpty();
            await Assert
                .That(worldScene.PaintableScene.Items.Count)
                .IsEqualTo(worldScene.PlacementPreview!.RenderQueue.Count);
            await Assert.That(worldScene.PaintableScene.SpriteCoverage.IsComplete).IsTrue();
            await Assert
                .That(
                    worldScene.PaintableScene.Items.Any(item =>
                        item.Kind == EditorMapRenderQueueItemKind.FloorTile && item.Sprite is not null
                    )
                )
                .IsTrue();
            await Assert
                .That(
                    worldScene.PaintableScene.Items.Any(item =>
                        item.Kind == EditorMapRenderQueueItemKind.PlacementPreviewObject && item.Sprite is not null
                    )
                )
                .IsTrue();
            await Assert.That(hit).IsNotNull();
            await Assert.That(hit!.MapTileX).IsEqualTo(6);
            await Assert.That(hit.MapTileY).IsEqualTo(6);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapBootstrapHelpers_CreateMapWorldEditScene_ReportsUnresolvedSpriteCoverageWithoutBindings()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            MessageFormat.WriteToFile(
                new MesFile { Entries = [new MessageEntry(protoNumber, "Palette proto")] },
                Path.Combine(contentDir, "mes", "description.mes")
            );
            ProtoFormat.WriteToFile(
                WithProperties(MakeProto(protoNumber), MakeArtProperty(ObjectField.ObjFCurrentAid, 200u)),
                Path.Combine(contentDir, "proto", "001001 - Test.pro")
            );

            var sector = new SectorBuilder(MakeSector()).SetTile(6, 6, 100u).Build();
            SectorFormat.WriteToFile(sector, Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            var paletteEntry = workspace.FindObjectPaletteEntry(protoNumber);

            await Assert.That(paletteEntry).IsNotNull();

            session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "world-edit-unresolved",
                    MapName = "map01",
                    ViewId = "scene",
                    Camera = new EditorProjectMapCameraState
                    {
                        CenterTileX = 6d,
                        CenterTileY = 6d,
                        Zoom = 1d,
                    },
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(6, 6),
                    },
                    Preview = new EditorProjectMapPreviewState(),
                }
            );

            var worldScene = session.CreateMapWorldEditScene(
                "world-edit-unresolved",
                new EditorMapWorldEditSceneRequest
                {
                    RenderRequest = new EditorMapFloorRenderRequest
                    {
                        ViewMode = EditorMapSceneViewMode.TopDown,
                        TileWidthPixels = 32d,
                        TileHeightPixels = 32d,
                    },
                    PlacementRequest = paletteEntry!.CreatePlacementRequest(),
                    ViewportWidth = 128d,
                    ViewportHeight = 128d,
                }
            );

            await Assert.That(worldScene.SpriteCoverage.IsComplete).IsFalse();
            await Assert.That(worldScene.SpriteCoverage.ReferencedArtIds.Count).IsEqualTo(2);
            await Assert.That(worldScene.SpriteCoverage.ResolvedArtIds).IsEmpty();
            await Assert
                .That(worldScene.SpriteCoverage.UnresolvedArtIds.Select(static artId => artId.Value).ToArray())
                .IsEquivalentTo([100u, 200u]);
            await Assert.That(worldScene.PaintableScene.SpriteCoverage.IsComplete).IsFalse();
            await Assert
                .That(
                    worldScene.PaintableScene.Items.Any(item =>
                        item.Kind == EditorMapRenderQueueItemKind.FloorTile && item.Sprite is not null
                    )
                )
                .IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_PreviewObjectPalettePlacement_FlagsBlockedAndOccupiedGhostTiles()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "mes"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            MessageFormat.WriteToFile(
                new MesFile { Entries = [new MessageEntry(protoNumber, "Palette proto")] },
                Path.Combine(contentDir, "mes", "description.mes")
            );
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            var occupiedObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(7, 7).Build();
            var sector = MakeSector(occupiedObject);
            sector.Tiles[(6 * 64) + 6] = 100u;
            sector.Tiles[(7 * 64) + 7] = 200u;
            sector.BlockMask[(6 * 64 + 6) / 32] |= 1u << ((6 * 64 + 6) % 32);
            SectorFormat.WriteToFile(sector, Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            var scenePreview = workspace.CreateMapScenePreview("map01");
            var paletteEntry = workspace.FindObjectPaletteEntry(protoNumber);

            await Assert.That(paletteEntry).IsNotNull();

            var blockedSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPath,
                Tile = new Location(6, 6),
            };
            var occupiedSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPath,
                Tile = new Location(7, 7),
            };

            var blockedPreview = session.PreviewSectorObjectPalettePlacement(
                scenePreview,
                blockedSelection,
                paletteEntry!.CreatePlacementRequest()
            );
            var occupiedPreview = session.PreviewSectorObjectPalettePlacement(
                scenePreview,
                occupiedSelection,
                paletteEntry.CreatePlacementRequest()
            );

            await Assert
                .That(blockedPreview.Objects.Single().State)
                .IsEqualTo(EditorMapPlacementPreviewState.BlockedTile);
            await Assert.That(blockedPreview.Objects.Single().ValidationMessage).IsEqualTo("Targets a blocked tile.");
            await Assert.That(blockedPreview.Objects.Single().SuggestedOpacity).IsEqualTo(0.55d);
            await Assert.That(blockedPreview.Objects.Single().SuggestedTintColor).IsEqualTo(0xAACC6666u);

            await Assert
                .That(occupiedPreview.Objects.Single().State)
                .IsEqualTo(EditorMapPlacementPreviewState.OccupiedTile);
            await Assert
                .That(occupiedPreview.Objects.Single().ValidationMessage)
                .IsEqualTo("Targets an occupied tile.");
            await Assert.That(occupiedPreview.Objects.Single().SuggestedOpacity).IsEqualTo(0.55d);
            await Assert.That(occupiedPreview.Objects.Single().SuggestedTintColor).IsEqualTo(0xAACCA066u);
            await Assert
                .That(
                    occupiedPreview.RenderQueue.Any(item =>
                        item.Kind == EditorMapRenderQueueItemKind.PlacementPreviewObject
                        && item.PlacementPreviewObject?.State == EditorMapPlacementPreviewState.OccupiedTile
                    )
                )
                .IsTrue();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_ApplyObjectPalettePlacementSet_RoutesPointAndAreaSelections()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        const float anchorRotation = 0.25f;
        const float offsetRotation = 1.75f;
        const float offsetRotationPitch = 2.5f;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var preview = workspace.CreateMapScenePreview("map01");
            var placementSet = EditorObjectPalettePlacementSet.Create(
                "Two-object preset",
                EditorObjectPalettePlacementRequest.Place(protoNumber, rotation: anchorRotation),
                EditorObjectPalettePlacementRequest.Place(
                    protoNumber,
                    deltaTileY: 1,
                    rotation: offsetRotation,
                    rotationPitch: offsetRotationPitch
                )
            );

            var pointSession = workspace.CreateSession();
            var pointSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(5, 6),
            };

            var pointCreatedObjects = pointSession.ApplySectorObjectPalettePlacementSet(
                preview,
                pointSelection,
                placementSet
            );

            await Assert.That(pointCreatedObjects.Count).IsEqualTo(2);

            var pointWorkspace = pointSession
                .BeginChangeGroup("Palette place set point selection")
                .ApplyPendingChanges();
            var pointSectorA = pointWorkspace.FindSector(sectorAssetPathA);
            var pointPreview = pointWorkspace.CreateMapScenePreview("map01");
            var pointPreviewSectorA = pointPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathA);

            await Assert.That(pointSectorA).IsNotNull();

            var pointAnchorId = pointCreatedObjects[0].Header.ObjectId;
            var pointOffsetId = pointCreatedObjects[1].Header.ObjectId;
            var pointAnchorObject = pointSectorA!.Objects.Single(obj => obj.Header.ObjectId == pointAnchorId);
            var pointOffsetObject = pointSectorA.Objects.Single(obj => obj.Header.ObjectId == pointOffsetId);

            await Assert.That(pointAnchorObject.GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((5, 6));
            await Assert.That(pointOffsetObject.GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((5, 7));
            await Assert
                .That(pointAnchorObject.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat())
                .IsEqualTo(anchorRotation);
            await Assert
                .That(pointOffsetObject.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat())
                .IsEqualTo(offsetRotation);
            await Assert
                .That(pointOffsetObject.GetProperty(ObjectField.ObjFRotationPitch)!.GetFloat())
                .IsEqualTo(offsetRotationPitch);
            await Assert
                .That(pointPreviewSectorA.Objects.Single(obj => obj.ObjectId == pointAnchorId).Location)
                .IsEqualTo(new Location(5, 6));
            await Assert
                .That(pointPreviewSectorA.Objects.Single(obj => obj.ObjectId == pointOffsetId).Location)
                .IsEqualTo(new Location(5, 7));

            var areaSession = workspace.CreateSession();
            var areaSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(63, 2),
                Area = new EditorProjectMapAreaSelectionState
                {
                    MinMapTileX = 63,
                    MinMapTileY = 2,
                    MaxMapTileX = 64,
                    MaxMapTileY = 2,
                },
            };

            var areaCreatedObjects = areaSession.ApplySectorObjectPalettePlacementSet(
                preview,
                areaSelection,
                placementSet
            );

            await Assert.That(areaCreatedObjects.Count).IsEqualTo(4);

            var areaWorkspace = areaSession.BeginChangeGroup("Palette place set area selection").ApplyPendingChanges();
            var areaSectorA = areaWorkspace.FindSector(sectorAssetPathA);
            var areaSectorB = areaWorkspace.FindSector(sectorAssetPathB);
            var areaPreview = areaWorkspace.CreateMapScenePreview("map01");
            var areaPreviewSectorA = areaPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathA);
            var areaPreviewSectorB = areaPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathB);

            await Assert.That(areaSectorA).IsNotNull();
            await Assert.That(areaSectorB).IsNotNull();

            var areaAnchorAId = areaCreatedObjects[0].Header.ObjectId;
            var areaOffsetAId = areaCreatedObjects[1].Header.ObjectId;
            var areaAnchorBId = areaCreatedObjects[2].Header.ObjectId;
            var areaOffsetBId = areaCreatedObjects[3].Header.ObjectId;
            var areaAnchorA = areaSectorA!.Objects.Single(obj => obj.Header.ObjectId == areaAnchorAId);
            var areaOffsetA = areaSectorA.Objects.Single(obj => obj.Header.ObjectId == areaOffsetAId);
            var areaAnchorB = areaSectorB!.Objects.Single(obj => obj.Header.ObjectId == areaAnchorBId);
            var areaOffsetB = areaSectorB.Objects.Single(obj => obj.Header.ObjectId == areaOffsetBId);

            await Assert.That(areaAnchorA.GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((63, 2));
            await Assert.That(areaOffsetA.GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((63, 3));
            await Assert.That(areaAnchorB.GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((0, 2));
            await Assert.That(areaOffsetB.GetProperty(ObjectField.ObjFLocation)!.GetLocation()).IsEqualTo((0, 3));
            await Assert.That(areaAnchorA.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat()).IsEqualTo(anchorRotation);
            await Assert.That(areaAnchorB.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat()).IsEqualTo(anchorRotation);
            await Assert.That(areaOffsetA.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat()).IsEqualTo(offsetRotation);
            await Assert.That(areaOffsetB.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat()).IsEqualTo(offsetRotation);
            await Assert
                .That(areaOffsetA.GetProperty(ObjectField.ObjFRotationPitch)!.GetFloat())
                .IsEqualTo(offsetRotationPitch);
            await Assert
                .That(areaOffsetB.GetProperty(ObjectField.ObjFRotationPitch)!.GetFloat())
                .IsEqualTo(offsetRotationPitch);
            await Assert
                .That(areaPreviewSectorA.Objects.Single(obj => obj.ObjectId == areaAnchorAId).Location)
                .IsEqualTo(new Location(63, 2));
            await Assert
                .That(areaPreviewSectorA.Objects.Single(obj => obj.ObjectId == areaOffsetAId).Location)
                .IsEqualTo(new Location(63, 3));
            await Assert
                .That(areaPreviewSectorB.Objects.Single(obj => obj.ObjectId == areaAnchorBId).Location)
                .IsEqualTo(new Location(0, 2));
            await Assert
                .That(areaPreviewSectorB.Objects.Single(obj => obj.ObjectId == areaOffsetBId).Location)
                .IsEqualTo(new Location(0, 3));
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_PreviewAndApplyTrackedObjectPlacementTool_UsesPersistedPresetLibrary()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(MakeSector(), Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(9, 10),
                    },
                    WorldEdit = new EditorProjectMapWorldEditState
                    {
                        ActiveTool = EditorProjectMapWorldEditActiveTool.ObjectPlacement,
                        ObjectPlacement = new EditorProjectMapObjectPlacementToolState
                        {
                            Mode = EditorProjectMapObjectPlacementMode.PlacementPreset,
                            PresetLibrary =
                            [
                                EditorObjectPalettePlacementPreset.Create(
                                    "guard",
                                    "Guard",
                                    entries: [EditorObjectPalettePlacementRequest.Place(protoNumber, rotation: 1.25f)]
                                ),
                            ],
                            SelectedPresetId = "guard",
                        },
                    },
                }
            );

            var preview = session.PreviewTrackedObjectPlacementTool("map-view-1");
            var createdObjects = session.ApplyTrackedObjectPlacementTool("map-view-1");

            await Assert.That(preview.Objects.Count).IsEqualTo(1);
            await Assert.That(preview.RenderQueue.Count).IsGreaterThan(0);
            await Assert.That(createdObjects.Count).IsEqualTo(1);

            var updatedWorkspace = session.BeginChangeGroup("Apply tracked object tool").ApplyPendingChanges();
            var updatedSector = updatedWorkspace.FindSector(sectorAssetPath);

            await Assert.That(updatedSector).IsNotNull();
            await Assert.That(updatedSector!.Objects.Count).IsEqualTo(1);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_SaveTrackedObjectPlacementTool_PersistsAndReloadsSectorChanges()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";
        var sectorPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var contentDir = sectorPath;

        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(MakeSector(), Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec"));

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(9, 10),
                    },
                }
            );

            _ = session.SetTrackedObjectPlacementEntry("map-view-1", protoNumber, rotation: 1.25f);

            var preview = session.PreviewTrackedObjectPlacementTool("map-view-1");
            var createdObjects = session.ApplyTrackedObjectPlacementTool("map-view-1");

            await Assert.That(preview.Objects.Count).IsEqualTo(1);
            await Assert.That(createdObjects.Count).IsEqualTo(1);
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(1);

            var updatedWorkspace = session.BeginChangeGroup("Save tracked object tool").SavePendingChanges();
            var persistedSector = SectorFormat.ParseFile(Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec"));
            var reloadedWorkspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var reloadedSector = reloadedWorkspace.FindSector(sectorAssetPath);
            var reloadedPreview = reloadedWorkspace.CreateMapScenePreview("map01");
            var previewSector = reloadedPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPath);

            await Assert.That(updatedWorkspace.FindSector(sectorAssetPath)).IsNotNull();
            await Assert.That(persistedSector.Objects.Count).IsEqualTo(1);
            await Assert.That(reloadedSector).IsNotNull();
            await Assert.That(reloadedSector!.Objects.Count).IsEqualTo(1);
            await Assert.That(persistedSector.Objects[0].Header.ProtoId.GetProtoNumber()).IsEqualTo(protoNumber);
            await Assert
                .That(persistedSector.Objects[0].GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((9, 10));
            await Assert.That(reloadedSector.Objects[0].Header.ProtoId.GetProtoNumber()).IsEqualTo(protoNumber);
            await Assert
                .That(reloadedSector.Objects[0].GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((9, 10));
            await Assert.That(previewSector.Objects.Count).IsEqualTo(1);
            await Assert.That(previewSector.Objects[0].Location).IsEqualTo(new Location(9, 10));
            await Assert.That(session.HasPendingChanges).IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_SaveTrackedObjectPlacementTool_PersistsSaveBackedSectorChangesToSaveSlot()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var saveDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));
        Directory.CreateDirectory(saveDir);

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(MakeSector(), Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec"));
            SaveGameWriter.Save(
                CreateLoadedSaveWithSector(
                    CreateMinimalLoadedSave(
                        new SaveInfo
                        {
                            ModuleName = "Arcanum",
                            LeaderName = "WorkspacePc",
                            DisplayName = "Save-backed world edit",
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
                    MakeSector()
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
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(9, 10),
                    },
                }
            );

            _ = session.SetTrackedObjectPlacementEntry("map-view-1", protoNumber, rotation: 0.5f);
            var createdObjects = session.ApplyTrackedObjectPlacementTool("map-view-1");

            await Assert.That(createdObjects.Count).IsEqualTo(1);

            var updatedWorkspace = session.BeginChangeGroup("Save tracked object tool to save").SavePendingChanges();
            var persistedContentSector = SectorFormat.ParseFile(
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );
            var persistedSave = SaveGameLoader.Load(saveDir, "slot0001");
            var reloadedWorkspace = await EditorWorkspaceLoader.LoadAsync(
                contentDir,
                new EditorWorkspaceLoadOptions { SaveFolder = saveDir, SaveSlotName = "slot0001" }
            );
            var reloadedSector = reloadedWorkspace.FindSector(sectorAssetPath);

            await Assert.That(updatedWorkspace.Save).IsNotNull();
            await Assert.That(persistedContentSector.Objects.Count).IsEqualTo(0);
            await Assert.That(persistedSave.Sectors.ContainsKey(sectorAssetPath)).IsTrue();
            await Assert.That(persistedSave.Sectors[sectorAssetPath].Objects.Count).IsEqualTo(1);
            await Assert
                .That(persistedSave.Sectors[sectorAssetPath].Objects[0].Header.ProtoId.GetProtoNumber())
                .IsEqualTo(protoNumber);
            await Assert.That(reloadedSector).IsNotNull();
            await Assert.That(reloadedSector!.Objects.Count).IsEqualTo(1);
            await Assert.That(reloadedSector.Objects[0].Header.ProtoId.GetProtoNumber()).IsEqualTo(protoNumber);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);

            if (Directory.Exists(saveDir))
                Directory.Delete(saveDir, recursive: true);
        }
    }

    [Test]
    public async Task RetargetProtoReferences_PersistsSaveBackedMobChangesToSaveSlot()
    {
        const int sourceProtoNumber = 1001;
        const int targetProtoNumber = 1002;
        const string mobAssetPath = "maps/map01/mobile/G_pc.mob";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var saveDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01", "mobile"));
        Directory.CreateDirectory(saveDir);

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(sourceProtoNumber),
                Path.Combine(contentDir, "proto", "001001 - Source.pro")
            );
            ProtoFormat.WriteToFile(
                MakeProto(targetProtoNumber),
                Path.Combine(contentDir, "proto", "001002 - Target.pro")
            );
            MobFormat.WriteToFile(
                MakePc(sourceProtoNumber),
                Path.Combine(contentDir, "maps", "map01", "mobile", "G_pc.mob")
            );
            SaveGameWriter.Save(
                CreateLoadedSaveWithMob(
                    CreateMinimalLoadedSave(
                        new SaveInfo
                        {
                            ModuleName = "Arcanum",
                            LeaderName = "WorkspacePc",
                            DisplayName = "Save-backed mob edit",
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
                    mobAssetPath,
                    MakePc(sourceProtoNumber)
                ),
                saveDir,
                "slot0001"
            );

            var session = (
                await EditorWorkspaceLoader.LoadAsync(
                    contentDir,
                    new EditorWorkspaceLoadOptions { SaveFolder = saveDir, SaveSlotName = "slot0001" }
                )
            ).CreateSession();

            var stagedChanges = session.RetargetProtoReferences(sourceProtoNumber, targetProtoNumber);

            await Assert.That(stagedChanges.Count).IsEqualTo(1);
            await Assert.That(stagedChanges[0].Kind).IsEqualTo(EditorSessionChangeKind.Mob);
            await Assert.That(stagedChanges[0].Target).IsEqualTo(mobAssetPath);

            var updatedWorkspace = session.BeginChangeGroup("Persist save-backed mob retarget").SavePendingChanges();
            var persistedContentMob = MobFormat.ParseFile(
                Path.Combine(contentDir, "maps", "map01", "mobile", "G_pc.mob")
            );
            var persistedSave = SaveGameLoader.Load(saveDir, "slot0001");
            var reloadedWorkspace = await EditorWorkspaceLoader.LoadAsync(
                contentDir,
                new EditorWorkspaceLoadOptions { SaveFolder = saveDir, SaveSlotName = "slot0001" }
            );
            var reloadedMob = reloadedWorkspace.GameData.MobsBySource[mobAssetPath].Single();

            await Assert.That(updatedWorkspace.Save).IsNotNull();
            await Assert.That(persistedContentMob.Header.ProtoId.GetProtoNumber()).IsEqualTo(sourceProtoNumber);
            await Assert.That(persistedSave.Mobiles.ContainsKey(mobAssetPath)).IsTrue();
            await Assert
                .That(persistedSave.Mobiles[mobAssetPath].Header.ProtoId.GetProtoNumber())
                .IsEqualTo(targetProtoNumber);
            await Assert.That(reloadedMob.Header.ProtoId.GetProtoNumber()).IsEqualTo(targetProtoNumber);
            await Assert.That(session.HasPendingChanges).IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);

            if (Directory.Exists(saveDir))
                Directory.Delete(saveDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_GetTrackedObjectPaletteSummary_UsesPersistedBrowserState_AndTracksBrowserSelection()
    {
        const int guardProtoNumber = 1001;
        const int wolfProtoNumber = 1002;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(guardProtoNumber),
                Path.Combine(contentDir, "proto", "001001 - Guard.pro")
            );
            ProtoFormat.WriteToFile(MakeProto(wolfProtoNumber), Path.Combine(contentDir, "proto", "001002 - Wolf.pro"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(new EditorProjectMapViewState { Id = "map-view-1", MapName = "map01" });
            var toolState = session.SetTrackedObjectPlacementEntry("map-view-1", guardProtoNumber, rotation: 1.5f);
            var browserState = session.SelectTrackedObjectPaletteEntry(
                "map-view-1",
                wolfProtoNumber,
                searchText: "  wolf  ",
                category: "  pc "
            );
            var persistedSummary = session.GetTrackedObjectPaletteSummary("map-view-1");
            var overriddenSummary = session.GetTrackedObjectPaletteSummary("map-view-1", searchText: "guard");

            await Assert.That(toolState.Mode).IsEqualTo(EditorProjectMapObjectPlacementMode.SinglePlacement);
            await Assert.That(toolState.PlacementRequest).IsNotNull();
            await Assert.That(toolState.PlacementRequest!.ProtoNumber).IsEqualTo(guardProtoNumber);
            await Assert.That(browserState.SelectedPaletteProtoNumber).IsEqualTo(wolfProtoNumber);
            await Assert.That(browserState.PaletteSearchText).IsEqualTo("wolf");
            await Assert.That(browserState.PaletteCategory).IsEqualTo("pc");
            await Assert.That(persistedSummary.CanBrowse).IsTrue();
            await Assert.That(persistedSummary.Entries.Count).IsEqualTo(1);
            await Assert.That(persistedSummary.Entries[0].ProtoNumber).IsEqualTo(wolfProtoNumber);
            await Assert.That(persistedSummary.AvailableCategories).IsEquivalentTo(["Pc"]);
            await Assert.That(persistedSummary.ToolState.PlacementRequest).IsNotNull();
            await Assert.That(persistedSummary.ToolState.PlacementRequest!.ProtoNumber).IsEqualTo(guardProtoNumber);
            await Assert.That(persistedSummary.SearchText).IsEqualTo("wolf");
            await Assert.That(persistedSummary.Category).IsEqualTo("pc");
            await Assert.That(persistedSummary.HasSelectedEntry).IsTrue();
            await Assert.That(persistedSummary.SelectedEntry).IsNotNull();
            await Assert.That(persistedSummary.SelectedEntry!.ProtoNumber).IsEqualTo(wolfProtoNumber);
            await Assert.That(overriddenSummary.SearchText).IsEqualTo("guard");
            await Assert.That(overriddenSummary.Category).IsEqualTo("pc");
            await Assert.That(overriddenSummary.Entries.Count).IsEqualTo(1);
            await Assert.That(overriddenSummary.Entries[0].ProtoNumber).IsEqualTo(guardProtoNumber);
            await Assert.That(overriddenSummary.SelectedEntry).IsNull();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_AppendTrackedObjectPaletteSelectionToPlacementSet_PreservesBrowserAndModeContext()
    {
        const int guardProtoNumber = 1001;
        const int wolfProtoNumber = 1002;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(guardProtoNumber),
                Path.Combine(contentDir, "proto", "001001 - Guard.pro")
            );
            ProtoFormat.WriteToFile(MakeProto(wolfProtoNumber), Path.Combine(contentDir, "proto", "001002 - Wolf.pro"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(new EditorProjectMapViewState { Id = "map-view-1", MapName = "map01" });

            var wolfEntry = workspace.FindObjectPaletteEntry(wolfProtoNumber);

            await Assert.That(wolfEntry).IsNotNull();

            _ = session.SetTrackedObjectPlacementPresetLibrary(
                "map-view-1",
                [wolfEntry!.CreatePlacementPreset("wolf-pack", "Wolf Pack", deltaTileX: 2)],
                selectedPresetId: "wolf-pack",
                activateTool: false
            );
            _ = session.SetTrackedObjectPlacementEntry("map-view-1", guardProtoNumber, rotation: 0.5f);
            _ = session.SelectTrackedObjectPaletteEntry(
                "map-view-1",
                wolfProtoNumber,
                searchText: "wolf",
                category: "pc"
            );

            var updatedState = session.AppendTrackedObjectPaletteSelectionToPlacementSet(
                "map-view-1",
                deltaTileX: 1,
                rotation: 1.25f
            );
            var summary = session.GetTrackedObjectPlacementToolSummary("map-view-1");
            var paletteSummary = session.GetTrackedObjectPaletteSummary("map-view-1");

            await Assert.That(updatedState.Mode).IsEqualTo(EditorProjectMapObjectPlacementMode.PlacementSet);
            await Assert.That(updatedState.PlacementRequest).IsNotNull();
            await Assert.That(updatedState.PlacementRequest!.ProtoNumber).IsEqualTo(guardProtoNumber);
            await Assert.That(updatedState.PlacementRequest.Rotation).IsEqualTo(0.5f);
            await Assert.That(updatedState.PlacementSet).IsNotNull();
            await Assert.That(updatedState.PlacementSet!.Entries.Count).IsEqualTo(2);
            await Assert.That(updatedState.PlacementSet.Entries[0].ProtoNumber).IsEqualTo(guardProtoNumber);
            await Assert.That(updatedState.PlacementSet.Entries[1].ProtoNumber).IsEqualTo(wolfProtoNumber);
            await Assert.That(updatedState.PlacementSet.Entries[1].DeltaTileX).IsEqualTo(1);
            await Assert.That(updatedState.PlacementSet.Entries[1].Rotation).IsEqualTo(1.25f);
            await Assert.That(updatedState.PresetLibrary.Count).IsEqualTo(1);
            await Assert.That(updatedState.SelectedPresetId).IsEqualTo("wolf-pack");
            await Assert.That(updatedState.PaletteSearchText).IsEqualTo("wolf");
            await Assert.That(updatedState.PaletteCategory).IsEqualTo("pc");
            await Assert.That(updatedState.SelectedPaletteProtoNumber).IsEqualTo(wolfProtoNumber);
            await Assert.That(summary.ToolState.Mode).IsEqualTo(EditorProjectMapObjectPlacementMode.PlacementSet);
            await Assert.That(summary.ToolState.PlacementRequest).IsNotNull();
            await Assert.That(summary.ToolState.PlacementRequest!.ProtoNumber).IsEqualTo(guardProtoNumber);
            await Assert.That(summary.EffectivePlacementSet).IsNotNull();
            await Assert.That(summary.EffectivePlacementSet!.Entries.Count).IsEqualTo(2);
            await Assert.That(summary.ResolvedPaletteEntries.Count).IsEqualTo(2);
            await Assert.That(summary.CanPreviewOrApply).IsTrue();
            await Assert.That(paletteSummary.SelectedEntry).IsNotNull();
            await Assert.That(paletteSummary.SelectedEntry!.ProtoNumber).IsEqualTo(wolfProtoNumber);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_GetTrackedObjectSelectionSummary_AndApplyTrackedObjectTransform_UsePersistedSelection()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            var selectedObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(5, 6).Build();
            var retainedObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(5, 6).Build();
            SectorFormat.WriteToFile(
                MakeSector(selectedObject, retainedObject),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                }
            );

            var selectionSummary = session.GetTrackedObjectSelectionSummary("map-view-1");
            var transformResult = session.ApplyTrackedObjectTransform(
                "map-view-1",
                EditorMapObjectTransformRequest.Transform(deltaTileX: 1, rotation: 0.75f, rotationPitch: 1.25f)
            );

            await Assert.That(selectionSummary.HasResolvedObjects).IsTrue();
            await Assert.That(selectionSummary.CanApplyTrackedEdit).IsTrue();
            await Assert.That(selectionSummary.SelectedObjects.Count).IsEqualTo(1);
            await Assert.That(selectionSummary.SelectedObjects[0].ObjectId).IsEqualTo(selectedObject.Header.ObjectId);
            await Assert.That(selectionSummary.MissingObjectIds.Count).IsEqualTo(0);
            await Assert.That(selectionSummary.SectorAssetPaths).IsEquivalentTo([sectorAssetPath]);
            await Assert.That(transformResult.HasChanges).IsTrue();
            await Assert.That(transformResult.UpdatedObjectCount).IsEqualTo(1);
            await Assert.That(transformResult.UpdatedObjectIds.Single()).IsEqualTo(selectedObject.Header.ObjectId);

            var updatedWorkspace = session.BeginChangeGroup("Apply tracked object transform").ApplyPendingChanges();
            var updatedSector = updatedWorkspace.FindSector(sectorAssetPath);

            await Assert.That(updatedSector).IsNotNull();

            var updatedSelectedObject = updatedSector!.Objects.Single(obj =>
                obj.Header.ObjectId == selectedObject.Header.ObjectId
            );
            var updatedRetainedObject = updatedSector.Objects.Single(obj =>
                obj.Header.ObjectId == retainedObject.Header.ObjectId
            );

            await Assert
                .That(updatedSelectedObject.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((6, 6));
            await Assert.That(updatedSelectedObject.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat()).IsEqualTo(0.75f);
            await Assert
                .That(updatedSelectedObject.GetProperty(ObjectField.ObjFRotationPitch)!.GetFloat())
                .IsEqualTo(1.25f);
            await Assert
                .That(updatedRetainedObject.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((5, 6));
            await Assert.That(updatedRetainedObject.GetProperty(ObjectField.ObjFPadIas1)).IsNull();
            await Assert.That(updatedRetainedObject.GetProperty(ObjectField.ObjFRotationPitch)).IsNull();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_GetTrackedObjectInspectorSummary_UsesPersistedSelection_AndStagedProtoDisplayName()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(protoNumber),
                Path.Combine(contentDir, "proto", "001001 - InspectorTarget.pro")
            );

            var selectedObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(5, 6).Build();
            SectorFormat.WriteToFile(
                MakeSector(selectedObject),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                }
            );

            _ = session.SetProtoDisplayName(protoNumber, "Town Guard", useNameOverrideAsset: true);

            var workspaceInspector = workspace.FindObjectInspectorSummary(protoNumber);
            var inspector = session.GetTrackedObjectInspectorSummary("map-view-1");

            await Assert.That(workspaceInspector).IsNotNull();
            await Assert.That(workspaceInspector!.Proto).IsNotNull();
            await Assert.That(workspaceInspector.Proto!.DisplayName).IsNull();
            await Assert.That(inspector.TargetKind).IsEqualTo(EditorObjectInspectorTargetKind.SelectedObject);
            await Assert.That(inspector.CanInspect).IsTrue();
            await Assert.That(inspector.HasSelectionContext).IsTrue();
            await Assert.That(inspector.HasSelectedObject).IsTrue();
            await Assert.That(inspector.HasProto).IsTrue();
            await Assert.That(inspector.ProtoNumber).IsEqualTo(protoNumber);
            await Assert.That(inspector.TargetObjectType).IsEqualTo(ObjectType.Pc);
            await Assert.That(inspector.SelectionSummary).IsNotNull();
            await Assert.That(inspector.SelectionSummary!.SelectedObjects.Count).IsEqualTo(1);
            await Assert.That(inspector.SelectedObject).IsNotNull();
            await Assert.That(inspector.SelectedObject!.ObjectId).IsEqualTo(selectedObject.Header.ObjectId);
            await Assert.That(inspector.Proto).IsNotNull();
            await Assert.That(inspector.Proto!.Asset.AssetPath).IsEqualTo("proto/001001 - InspectorTarget.pro");
            await Assert.That(inspector.Proto.DisplayName).IsEqualTo("Town Guard");

            var overviewPane = inspector.Panes.Single(pane => pane.Pane == EditorObjectInspectorPane.Overview);
            var flagsPane = inspector.Panes.Single(pane => pane.Pane == EditorObjectInspectorPane.Flags);
            var scriptPane = inspector.Panes.Single(pane => pane.Pane == EditorObjectInspectorPane.ScriptAttachments);
            var critterPane = inspector.Panes.Single(pane => pane.Pane == EditorObjectInspectorPane.CritterProgression);
            var generatorPane = inspector.Panes.Single(pane => pane.Pane == EditorObjectInspectorPane.Generator);

            await Assert.That(overviewPane.IsApplicable).IsTrue();
            await Assert.That(overviewPane.HasContract).IsTrue();
            await Assert.That(flagsPane.IsApplicable).IsTrue();
            await Assert.That(flagsPane.HasContract).IsTrue();
            await Assert.That(scriptPane.IsApplicable).IsTrue();
            await Assert.That(scriptPane.HasContract).IsTrue();
            await Assert.That(critterPane.IsApplicable).IsTrue();
            await Assert.That(generatorPane.IsApplicable).IsFalse();
            await Assert
                .That(generatorPane.UnavailableReason)
                .IsEqualTo("Generator settings only apply to Npc targets.");
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_SetTrackedObjectInspectorState_PinsProtoAndFeedsShell()
    {
        const int selectedProtoNumber = 1001;
        const int pinnedProtoNumber = 1002;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

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
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                    WorldEdit = new EditorProjectMapWorldEditState
                    {
                        ActiveTool = EditorProjectMapWorldEditActiveTool.ObjectPlacement,
                    },
                }
            );

            var persistedState = session.SetTrackedObjectInspectorState(
                "map-view-1",
                new EditorProjectMapObjectInspectorState
                {
                    TargetMode = EditorProjectMapObjectInspectorTargetMode.ProtoDefinition,
                    PinnedProtoNumber = pinnedProtoNumber,
                    ActivePane = EditorObjectInspectorPane.Generator,
                }
            );

            var inspector = session.GetTrackedObjectInspectorSummary("map-view-1");
            var shell = session.CreateTrackedMapWorldEditShell("map-view-1");

            await Assert
                .That(persistedState.TargetMode)
                .IsEqualTo(EditorProjectMapObjectInspectorTargetMode.ProtoDefinition);
            await Assert.That(persistedState.PinnedProtoNumber).IsEqualTo(pinnedProtoNumber);
            await Assert.That(persistedState.ActivePane).IsEqualTo(EditorObjectInspectorPane.Generator);
            await Assert.That(inspector.TargetKind).IsEqualTo(EditorObjectInspectorTargetKind.ProtoDefinition);
            await Assert.That(inspector.HasSelectionContext).IsTrue();
            await Assert.That(inspector.HasSelectedObject).IsFalse();
            await Assert.That(inspector.ProtoNumber).IsEqualTo(pinnedProtoNumber);
            await Assert.That(inspector.TargetObjectType).IsEqualTo(ObjectType.Npc);
            await Assert.That(inspector.Proto).IsNotNull();
            await Assert.That(inspector.Proto!.Asset.AssetPath).IsEqualTo("proto/001002 - Pinned.pro");
            await Assert
                .That(session.GetMapViewStates()[0].WorldEdit.Inspector.TargetMode)
                .IsEqualTo(EditorProjectMapObjectInspectorTargetMode.ProtoDefinition);
            await Assert
                .That(session.GetMapViewStates()[0].WorldEdit.Inspector.PinnedProtoNumber)
                .IsEqualTo(pinnedProtoNumber);
            await Assert.That(shell.ObjectInspectorState.ActivePane).IsEqualTo(EditorObjectInspectorPane.Generator);
            await Assert
                .That(shell.ObjectInspector.TargetKind)
                .IsEqualTo(EditorObjectInspectorTargetKind.ProtoDefinition);
            await Assert.That(shell.ObjectInspector.ProtoNumber).IsEqualTo(pinnedProtoNumber);
            await Assert.That(shell.ObjectSelection.SelectedObjects.Count).IsEqualTo(1);
            await Assert
                .That(shell.ObjectSelection.SelectedObjects[0].ObjectId)
                .IsEqualTo(selectedObject.Header.ObjectId);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_SetTrackedObjectInspectorFlags_StagesSelectedObjectFlagEdits()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(protoNumber),
                Path.Combine(contentDir, "proto", "001001 - InspectorTarget.pro")
            );

            var selectedObject = new CharacterBuilder(
                ObjectType.Npc,
                new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid()),
                MakeProtoId(protoNumber)
            )
                .WithLocation(0, 0)
                .WithHitPoints(80)
                .WithProperty(
                    ObjectPropertyFactory.ForInt32(ObjectField.ObjFFlags, unchecked((int)ObjFFlags.Inventory))
                )
                .WithProperty(
                    ObjectPropertyFactory.ForInt32(
                        ObjectField.ObjFCritterFlags,
                        unchecked((int)ObjFCritterFlags.Animal)
                    )
                )
                .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFPcFlags, 1))
                .Build();
            SectorFormat.WriteToFile(
                MakeSector(selectedObject),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(0, 0),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                }
            );

            var before = session.GetTrackedObjectInspectorFlagsSummary("map-view-1");
            var change = session.SetTrackedObjectInspectorFlags(
                "map-view-1",
                new EditorObjectInspectorFlagsUpdate
                {
                    ObjectFlags = ObjFFlags.Flat | ObjFFlags.Translucent,
                    CritterFlags = ObjFCritterFlags.Undead | ObjFCritterFlags.NoFlee,
                    PcFlags = 7,
                }
            );
            var after = session.GetTrackedObjectInspectorFlagsSummary("map-view-1");
            var appliedWorkspace = session.ApplyPendingChanges();
            var appliedSector = appliedWorkspace.FindSector(sectorAssetPath);

            await Assert.That(before.ObjectFlags).IsEqualTo(ObjFFlags.Inventory);
            await Assert.That(before.CritterFlags).IsEqualTo(ObjFCritterFlags.Animal);
            await Assert.That(before.PcFlags).IsEqualTo(1);
            await Assert.That(change).IsNotNull();
            await Assert.That(change!.Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(change.Target).IsEqualTo(sectorAssetPath);
            await Assert.That(after.ObjectFlags).IsEqualTo(ObjFFlags.Flat | ObjFFlags.Translucent);
            await Assert.That(after.CritterFlags).IsEqualTo(ObjFCritterFlags.Undead | ObjFCritterFlags.NoFlee);
            await Assert.That(after.PcFlags).IsEqualTo(7);
            await Assert.That(appliedSector).IsNotNull();
            await Assert.That(appliedSector!.Objects.Count).IsEqualTo(1);
            await Assert
                .That(appliedSector.Objects[0].GetProperty(ObjectField.ObjFFlags)!.GetInt32())
                .IsEqualTo(unchecked((int)(ObjFFlags.Flat | ObjFFlags.Translucent)));
            await Assert
                .That(appliedSector.Objects[0].GetProperty(ObjectField.ObjFCritterFlags)!.GetInt32())
                .IsEqualTo(unchecked((int)(ObjFCritterFlags.Undead | ObjFCritterFlags.NoFlee)));
            await Assert.That(appliedSector.Objects[0].GetProperty(ObjectField.ObjFPcFlags)!.GetInt32()).IsEqualTo(7);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_SetTrackedObjectInspectorScriptAttachment_StagesSelectedObjectScriptEdit()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(protoNumber),
                Path.Combine(contentDir, "proto", "001001 - InspectorTarget.pro")
            );
            ScriptFormat.WriteToFile(
                MakeScriptFile("Examine script"),
                Path.Combine(contentDir, "scr", "00077Guard.scr")
            );

            var selectedObject = new CharacterBuilder(
                ObjectType.Npc,
                new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid()),
                MakeProtoId(protoNumber)
            )
                .WithLocation(0, 0)
                .WithHitPoints(80)
                .Build();
            SectorFormat.WriteToFile(
                MakeSector(selectedObject),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(0, 0),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                }
            );

            var before = session.GetTrackedObjectInspectorScriptAttachmentsSummary("map-view-1");
            var change = session.SetTrackedObjectInspectorScriptAttachment(
                "map-view-1",
                ScriptAttachmentPoint.Examine,
                77
            );
            var after = session.GetTrackedObjectInspectorScriptAttachmentsSummary("map-view-1");
            var appliedWorkspace = session.ApplyPendingChanges();
            var appliedSector = appliedWorkspace.FindSector(sectorAssetPath);

            var beforeExamine = before.Attachments.Single(attachment =>
                attachment.AttachmentPoint == ScriptAttachmentPoint.Examine
            );
            var afterExamine = after.Attachments.Single(attachment =>
                attachment.AttachmentPoint == ScriptAttachmentPoint.Examine
            );

            await Assert.That(beforeExamine.IsEmpty).IsTrue();
            await Assert.That(change).IsNotNull();
            await Assert.That(change!.Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(change.Target).IsEqualTo(sectorAssetPath);
            await Assert.That(afterExamine.ScriptId).IsEqualTo(77);
            await Assert.That(afterExamine.IsMissingScript).IsFalse();
            await Assert.That(afterExamine.Script).IsNotNull();
            await Assert.That(afterExamine.Script!.Description).IsEqualTo("Examine script");
            await Assert.That(appliedSector).IsNotNull();
            await Assert.That(appliedSector!.Objects.Count).IsEqualTo(1);
            await Assert.That(GetScriptIds(appliedSector.Objects[0].Properties)).IsEquivalentTo(new[] { 77 });
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_SetTrackedObjectInspectorCritterProgression_StagesSelectedObjectProgressionEdits()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(protoNumber),
                Path.Combine(contentDir, "proto", "001001 - InspectorTarget.pro")
            );

            var selectedObject = new CharacterBuilder(
                ObjectType.Npc,
                new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid()),
                MakeProtoId(protoNumber)
            )
                .WithLocation(0, 0)
                .WithHitPoints(80)
                .WithFatigue(70, 8)
                .Build();
            SectorFormat.WriteToFile(
                MakeSector(selectedObject),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(0, 0),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                }
            );

            var before = session.GetTrackedObjectInspectorCritterProgressionSummary("map-view-1");
            var change = session.SetTrackedObjectInspectorCritterProgression(
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
            var appliedWorkspace = session.ApplyPendingChanges();
            var appliedSector = appliedWorkspace.FindSector(sectorAssetPath);

            await Assert.That(before.FatiguePoints).IsEqualTo(70);
            await Assert.That(before.Level).IsEqualTo(0);
            await Assert.That(before.ExperiencePoints).IsEqualTo(0);
            await Assert.That(before.SkillPersuasion).IsEqualTo(0);
            await Assert.That(before.SkillRepair).IsEqualTo(0);
            await Assert.That(before.SpellTemporal).IsEqualTo(0);
            await Assert.That(before.TechTherapeutics).IsEqualTo(0);
            await Assert.That(change).IsNotNull();
            await Assert.That(change!.Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(change.Target).IsEqualTo(sectorAssetPath);
            await Assert.That(appliedSector).IsNotNull();
            await Assert.That(appliedSector!.Objects.Count).IsEqualTo(1);

            var appliedBaseStats = GetInt32Array(
                appliedSector.Objects[0].Properties,
                ObjectField.ObjFCritterStatBaseIdx
            );
            var appliedBasicSkills = GetInt32Array(
                appliedSector.Objects[0].Properties,
                ObjectField.ObjFCritterBasicSkillIdx
            );
            var appliedTechSkills = GetInt32Array(
                appliedSector.Objects[0].Properties,
                ObjectField.ObjFCritterTechSkillIdx
            );
            var appliedSpellTech = GetInt32Array(
                appliedSector.Objects[0].Properties,
                ObjectField.ObjFCritterSpellTechIdx
            );

            await Assert
                .That(appliedSector.Objects[0].GetProperty(ObjectField.ObjFCritterFatiguePts)!.GetInt32())
                .IsEqualTo(90);
            await Assert.That(appliedBaseStats[17]).IsEqualTo(15);
            await Assert.That(appliedBaseStats[18]).IsEqualTo(4321);
            await Assert.That(appliedBasicSkills[11]).IsEqualTo(21);
            await Assert.That(appliedTechSkills[0]).IsEqualTo(30);
            await Assert.That(appliedSpellTech[15]).IsEqualTo(99);
            await Assert.That(appliedSpellTech[24]).IsEqualTo(55);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_SetTrackedObjectInspectorLightGeneratorAndBlending_StagesSelectedObjectEdits()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(
                MakeNpcProto(protoNumber),
                Path.Combine(contentDir, "proto", "001001 - InspectorTarget.pro")
            );

            var selectedObject = new CharacterBuilder(
                ObjectType.Npc,
                new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid()),
                MakeProtoId(protoNumber)
            )
                .WithLocation(0, 0)
                .WithHitPoints(80)
                .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFLightFlags, 1))
                .WithProperty(MakeArtProperty(ObjectField.ObjFLightAid, 0x100u))
                .WithProperty(MakeColorProperty(ObjectField.ObjFLightColor, 0x01, 0x02, 0x03))
                .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFNpcGeneratorData, 5))
                .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFBlitAlpha, 10))
                .Build();
            SectorFormat.WriteToFile(
                MakeSector(selectedObject),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(0, 0),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                }
            );

            var beforeLight = session.GetTrackedObjectInspectorLightSummary("map-view-1");
            var beforeGenerator = session.GetTrackedObjectInspectorGeneratorSummary("map-view-1");
            var beforeBlending = session.GetTrackedObjectInspectorBlendingSummary("map-view-1");

            var generatorChange = session.SetTrackedObjectInspectorGenerator(
                "map-view-1",
                new EditorObjectInspectorGeneratorUpdate { GeneratorData = 42 }
            );
            var blendingChange = session.SetTrackedObjectInspectorBlending(
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
            var lightChange = session.SetTrackedObjectInspectorLight(
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

            var appliedWorkspace = session.ApplyPendingChanges();
            var appliedSector = appliedWorkspace.FindSector(sectorAssetPath);

            await Assert.That(beforeLight.LightFlags).IsEqualTo(1);
            await Assert.That(beforeLight.LightArtId).IsEqualTo(new ArtId(0x100u));
            await Assert.That(beforeLight.LightColor).IsEqualTo(new Color(0x01, 0x02, 0x03));
            await Assert.That(beforeGenerator.GeneratorData).IsEqualTo(5);
            await Assert.That(beforeBlending.BlitAlpha).IsEqualTo(10);
            await Assert.That(lightChange).IsNotNull();
            await Assert.That(generatorChange).IsNotNull();
            await Assert.That(blendingChange).IsNotNull();
            await Assert.That(appliedSector).IsNotNull();
            await Assert.That(appliedSector!.Objects.Count).IsEqualTo(1);
            await Assert
                .That(appliedSector.Objects[0].GetProperty(ObjectField.ObjFLightFlags)!.GetInt32())
                .IsEqualTo(9);
            await Assert
                .That(appliedSector.Objects[0].GetProperty(ObjectField.ObjFLightAid)!.GetInt32())
                .IsEqualTo(unchecked((int)0x234u));
            await Assert
                .That(GetColor(appliedSector.Objects[0].Properties, ObjectField.ObjFLightColor))
                .IsEqualTo(new Color(0x10, 0x20, 0x30));
            await Assert
                .That(appliedSector.Objects[0].GetProperty(ObjectField.ObjFOverlayLightFlags)!.GetInt32())
                .IsEqualTo(4);
            await Assert
                .That(GetInt32Array(appliedSector.Objects[0].Properties, ObjectField.ObjFOverlayLightAid))
                .IsEquivalentTo([7, 8]);
            await Assert
                .That(appliedSector.Objects[0].GetProperty(ObjectField.ObjFOverlayLightColor)!.GetInt32())
                .IsEqualTo(12);
            await Assert
                .That(appliedSector.Objects[0].GetProperty(ObjectField.ObjFNpcGeneratorData)!.GetInt32())
                .IsEqualTo(42);
            await Assert
                .That(appliedSector.Objects[0].GetProperty(ObjectField.ObjFBlitFlags)!.GetInt32())
                .IsEqualTo(unchecked((int)ObjFBlitFlags.BlendAdd));
            await Assert
                .That(GetColor(appliedSector.Objects[0].Properties, ObjectField.ObjFBlitColor))
                .IsEqualTo(new Color(0x44, 0x55, 0x66));
            await Assert
                .That(appliedSector.Objects[0].GetProperty(ObjectField.ObjFBlitAlpha)!.GetInt32())
                .IsEqualTo(77);
            await Assert
                .That(appliedSector.Objects[0].GetProperty(ObjectField.ObjFBlitScale)!.GetInt32())
                .IsEqualTo(88);
            await Assert.That(appliedSector.Objects[0].GetProperty(ObjectField.ObjFMaterial)!.GetInt32()).IsEqualTo(99);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_SaveTrackedObjectInspectorEdits_PersistSaveBackedSectorChangesToSaveSlot()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var saveDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));
        Directory.CreateDirectory(saveDir);

        try
        {
            ProtoFormat.WriteToFile(
                MakeNpcProto(protoNumber),
                Path.Combine(contentDir, "proto", "001001 - InspectorTarget.pro")
            );
            ScriptFormat.WriteToFile(
                MakeScriptFile("Examine script"),
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
                            DisplayName = "Save-backed inspector edit",
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
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                }
            );

            var flagsChange = session.SetTrackedObjectInspectorFlags(
                "map-view-1",
                new EditorObjectInspectorFlagsUpdate
                {
                    ObjectFlags = ObjFFlags.Flat | ObjFFlags.Translucent,
                    CritterFlags = ObjFCritterFlags.Undead | ObjFCritterFlags.NoFlee,
                }
            );
            var scriptChange = session.SetTrackedObjectInspectorScriptAttachment(
                "map-view-1",
                ScriptAttachmentPoint.Examine,
                77
            );
            var progressionChange = session.SetTrackedObjectInspectorCritterProgression(
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
            var generatorChange = session.SetTrackedObjectInspectorGenerator(
                "map-view-1",
                new EditorObjectInspectorGeneratorUpdate { GeneratorData = 42 }
            );
            var blendingChange = session.SetTrackedObjectInspectorBlending(
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
            var lightChange = session.SetTrackedObjectInspectorLight(
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

            var updatedWorkspace = session.BeginChangeGroup("Persist save-backed inspector edits").SavePendingChanges();
            var persistedContentSector = SectorFormat.ParseFile(
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );
            var persistedSave = SaveGameLoader.Load(saveDir, "slot0001");
            var reloadedWorkspace = await EditorWorkspaceLoader.LoadAsync(
                contentDir,
                new EditorWorkspaceLoadOptions { SaveFolder = saveDir, SaveSlotName = "slot0001" }
            );
            var reloadedSector = reloadedWorkspace.FindSector(sectorAssetPath);
            var reloadedSession = reloadedWorkspace.CreateSession();

            _ = reloadedSession.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                }
            );

            var reloadedFlags = reloadedSession.GetTrackedObjectInspectorFlagsSummary("map-view-1");
            var reloadedScripts = reloadedSession.GetTrackedObjectInspectorScriptAttachmentsSummary("map-view-1");
            var reloadedProgression = reloadedSession.GetTrackedObjectInspectorCritterProgressionSummary("map-view-1");
            var reloadedLight = reloadedSession.GetTrackedObjectInspectorLightSummary("map-view-1");
            var reloadedGenerator = reloadedSession.GetTrackedObjectInspectorGeneratorSummary("map-view-1");
            var reloadedBlending = reloadedSession.GetTrackedObjectInspectorBlendingSummary("map-view-1");

            await Assert.That(flagsChange).IsNotNull();
            await Assert.That(scriptChange).IsNotNull();
            await Assert.That(progressionChange).IsNotNull();
            await Assert.That(generatorChange).IsNotNull();
            await Assert.That(blendingChange).IsNotNull();
            await Assert.That(lightChange).IsNotNull();
            await Assert.That(updatedWorkspace.Save).IsNotNull();
            await Assert.That(persistedContentSector.Objects.Count).IsEqualTo(0);
            await Assert.That(persistedSave.Sectors.ContainsKey(sectorAssetPath)).IsTrue();
            await Assert.That(persistedSave.Sectors[sectorAssetPath].Objects.Count).IsEqualTo(1);
            await Assert.That(reloadedSector).IsNotNull();
            await Assert.That(reloadedSector!.Objects.Count).IsEqualTo(1);
            await Assert.That(reloadedSector.Objects[0].Header.ObjectId).IsEqualTo(selectedObject.Header.ObjectId);
            await Assert.That(reloadedFlags.ObjectFlags).IsEqualTo(ObjFFlags.Flat | ObjFFlags.Translucent);
            await Assert.That(reloadedFlags.CritterFlags).IsEqualTo(ObjFCritterFlags.Undead | ObjFCritterFlags.NoFlee);

            var reloadedExamine = reloadedScripts.Attachments.Single(attachment =>
                attachment.AttachmentPoint == ScriptAttachmentPoint.Examine
            );

            await Assert.That(reloadedExamine.ScriptId).IsEqualTo(77);
            await Assert.That(reloadedExamine.IsMissingScript).IsFalse();
            await Assert.That(reloadedProgression.FatiguePoints).IsEqualTo(90);
            await Assert.That(reloadedProgression.ExperiencePoints).IsEqualTo(4321);
            await Assert.That(reloadedProgression.SkillPersuasion).IsEqualTo(21);
            await Assert.That(reloadedProgression.SkillRepair).IsEqualTo(30);
            await Assert.That(reloadedProgression.SpellTemporal).IsEqualTo(99);
            await Assert.That(reloadedProgression.TechTherapeutics).IsEqualTo(55);
            await Assert.That(reloadedLight.LightFlags).IsEqualTo(9);
            await Assert.That(reloadedLight.LightArtId).IsEqualTo(new ArtId(0x234u));
            await Assert.That(reloadedLight.LightColor).IsEqualTo(new Color(0x10, 0x20, 0x30));
            await Assert.That(reloadedLight.OverlayLightFlags).IsEqualTo(4);
            await Assert.That(reloadedLight.OverlayLightArtIds).IsEquivalentTo([7, 8]);
            await Assert.That(reloadedLight.OverlayLightColor).IsEqualTo(12);
            await Assert.That(reloadedGenerator.GeneratorData).IsEqualTo(42);
            await Assert.That(reloadedBlending.BlitFlags).IsEqualTo(ObjFBlitFlags.BlendAdd);
            await Assert.That(reloadedBlending.BlitColor).IsEqualTo(new Color(0x44, 0x55, 0x66));
            await Assert.That(reloadedBlending.BlitAlpha).IsEqualTo(77);
            await Assert.That(reloadedBlending.BlitScale).IsEqualTo(88);
            await Assert.That(reloadedBlending.Material).IsEqualTo(99);
            await Assert.That(session.HasPendingChanges).IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);

            if (Directory.Exists(saveDir))
                Directory.Delete(saveDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_TrackedSelectionConvenienceHelpers_ReplaceAndEraseObjects()
    {
        const int sourceProtoNumber = 1001;
        const int replacementProtoNumber = 1002;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(sourceProtoNumber),
                Path.Combine(contentDir, "proto", "001001 - Source.pro")
            );
            ProtoFormat.WriteToFile(
                MakeProto(replacementProtoNumber),
                Path.Combine(contentDir, "proto", "001002 - Replacement.pro")
            );

            var selectedObject = new MobDataBuilder(MakePc(sourceProtoNumber)).WithLocation(5, 6).Build();
            var retainedObject = new MobDataBuilder(MakePc(sourceProtoNumber)).WithLocation(5, 6).Build();
            SectorFormat.WriteToFile(
                MakeSector(selectedObject, retainedObject),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                }
            );

            var replaceResult = session.ReplaceTrackedSelectedObjects("map-view-1", replacementProtoNumber);

            await Assert.That(replaceResult.HasChanges).IsTrue();
            await Assert.That(replaceResult.CreatedObjectCount).IsEqualTo(1);
            await Assert.That(replaceResult.RemovedObjectCount).IsEqualTo(1);
            await Assert.That(replaceResult.RemovedObjectIds.Single()).IsEqualTo(selectedObject.Header.ObjectId);

            var replacedWorkspace = session.BeginChangeGroup("Apply tracked replace").ApplyPendingChanges();
            var replacedSector = replacedWorkspace.FindSector(sectorAssetPath);

            await Assert.That(replacedSector).IsNotNull();
            await Assert.That(replacedSector!.Objects.Count).IsEqualTo(2);
            await Assert
                .That(
                    replacedSector.Objects.Count(obj => obj.Header.ProtoId.GetProtoNumber() == replacementProtoNumber)
                )
                .IsEqualTo(1);
            await Assert
                .That(replacedSector.Objects.Count(obj => obj.Header.ObjectId == retainedObject.Header.ObjectId))
                .IsEqualTo(1);

            var eraseSession = replacedWorkspace.CreateSession();
            _ = eraseSession.SetMapViewState(
                new EditorProjectMapViewState
                {
                    Id = "map-view-1",
                    MapName = "map01",
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                        ObjectId = retainedObject.Header.ObjectId,
                    },
                }
            );

            var eraseResult = eraseSession.EraseTrackedSelectedObjects("map-view-1");

            await Assert.That(eraseResult.HasChanges).IsTrue();
            await Assert.That(eraseResult.RemovedObjectCount).IsEqualTo(1);
            await Assert.That(eraseResult.RemovedObjectIds.Single()).IsEqualTo(retainedObject.Header.ObjectId);

            var erasedWorkspace = eraseSession.BeginChangeGroup("Apply tracked erase").ApplyPendingChanges();
            var erasedSector = erasedWorkspace.FindSector(sectorAssetPath);

            await Assert.That(erasedSector).IsNotNull();
            await Assert.That(erasedSector!.Objects.Count).IsEqualTo(1);
            await Assert
                .That(erasedSector.Objects[0].Header.ProtoId.GetProtoNumber())
                .IsEqualTo(replacementProtoNumber);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_CreateTrackedMapWorldEditShell_BundlesTopDownSceneAndWorkflowSummaries()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            MapProperties mapProperties = new()
            {
                ArtId = 200,
                Unused = 0,
                LimitX = 2,
                LimitY = 2,
            };
            MapPropertiesFormat.WriteToFile(in mapProperties, Path.Combine(contentDir, "maps", "map01", "map.prp"));

            var selectedObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(5, 6).Build();
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
                    WorldEdit = new EditorProjectMapWorldEditState
                    {
                        ActiveTool = EditorProjectMapWorldEditActiveTool.ObjectPlacement,
                    },
                }
            );
            _ = session.SetTrackedObjectPlacementEntry("map-view-1", protoNumber, rotation: 0.5f);
            _ = session.SetTrackedTerrainPaletteEntry("map-view-1", 1, 1, activateTool: false);

            var shell = session.CreateTrackedMapWorldEditShell(
                "map-view-1",
                new EditorMapWorldEditShellRequest
                {
                    ViewMode = EditorMapSceneViewMode.TopDown,
                    ViewportWidth = 320d,
                    ViewportHeight = 200d,
                    ObjectPaletteSearchText = "test",
                    ObjectPaletteCategory = "pc",
                }
            );

            await Assert.That(shell.MapViewStateId).IsEqualTo("map-view-1");
            await Assert.That(shell.MapName).IsEqualTo("map01");
            await Assert.That(shell.ActiveTool).IsEqualTo(EditorProjectMapWorldEditActiveTool.ObjectPlacement);
            await Assert.That(shell.ViewMode).IsEqualTo(EditorMapSceneViewMode.TopDown);
            await Assert.That(shell.RenderRequest.ViewMode).IsEqualTo(EditorMapSceneViewMode.TopDown);
            await Assert.That(shell.RenderRequest.TileWidthPixels).IsEqualTo(32d);
            await Assert.That(shell.RenderRequest.TileHeightPixels).IsEqualTo(32d);
            await Assert.That(shell.Scene.SceneRender.ViewMode).IsEqualTo(EditorMapSceneViewMode.TopDown);
            await Assert.That(shell.Scene.ViewportLayout.ViewportWidth).IsEqualTo(320d);
            await Assert.That(shell.Scene.ViewportLayout.ViewportHeight).IsEqualTo(200d);
            await Assert.That(shell.HasTrackedPlacementPreview).IsTrue();
            await Assert.That(shell.TrackedPlacementPreview).IsNotNull();
            await Assert.That(shell.TerrainPalette.SelectedEntry).IsNotNull();
            await Assert.That(shell.ObjectPalette.Entries.Count).IsEqualTo(1);
            await Assert.That(shell.ObjectPalette.SelectedEntry).IsNotNull();
            await Assert.That(shell.ObjectPalette.SelectedEntry!.ProtoNumber).IsEqualTo(protoNumber);
            await Assert.That(shell.ObjectSelection.SelectedObjects.Count).IsEqualTo(1);
            await Assert
                .That(shell.ObjectSelection.SelectedObjects[0].ObjectId)
                .IsEqualTo(selectedObject.Header.ObjectId);
            await Assert
                .That(shell.ObjectInspectorState.TargetMode)
                .IsEqualTo(EditorProjectMapObjectInspectorTargetMode.Selection);
            await Assert
                .That(shell.ObjectInspector.TargetKind)
                .IsEqualTo(EditorObjectInspectorTargetKind.SelectedObject);
            await Assert
                .That(shell.ObjectInspectorFlags.Inspector.TargetKind)
                .IsEqualTo(EditorObjectInspectorTargetKind.SelectedObject);
            await Assert
                .That(shell.ObjectInspectorScriptAttachments.Inspector.TargetKind)
                .IsEqualTo(EditorObjectInspectorTargetKind.SelectedObject);
            await Assert
                .That(shell.ObjectInspectorCritterProgression.Inspector.TargetKind)
                .IsEqualTo(EditorObjectInspectorTargetKind.SelectedObject);
            await Assert
                .That(shell.ObjectInspectorLight.Inspector.TargetKind)
                .IsEqualTo(EditorObjectInspectorTargetKind.SelectedObject);
            await Assert
                .That(shell.ObjectInspectorGenerator.Inspector.TargetKind)
                .IsEqualTo(EditorObjectInspectorTargetKind.SelectedObject);
            await Assert
                .That(shell.ObjectInspectorBlending.Inspector.TargetKind)
                .IsEqualTo(EditorObjectInspectorTargetKind.SelectedObject);
            await Assert
                .That(shell.ObjectPlacementTool.ToolState.Mode)
                .IsEqualTo(EditorProjectMapObjectPlacementMode.SinglePlacement);
            await Assert.That(shell.ObjectPlacementTool.ResolvedPaletteEntries.Count).IsEqualTo(1);
            await Assert.That(shell.ObjectPlacementTool.ResolvedPaletteEntries[0].ProtoNumber).IsEqualTo(protoNumber);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_CreateTrackedMapWorldEditShell_UsesCustomSpriteSource()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            MapProperties mapProperties = new()
            {
                ArtId = 200,
                Unused = 0,
                LimitX = 2,
                LimitY = 2,
            };
            MapPropertiesFormat.WriteToFile(in mapProperties, Path.Combine(contentDir, "maps", "map01", "map.prp"));

            var selectedObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(5, 6).Build();
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
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                }
            );

            var spriteSource = new TestSpriteSource();
            var shell = session.CreateTrackedMapWorldEditShell(
                "map-view-1",
                new EditorMapWorldEditShellRequest { SpriteSource = spriteSource }
            );

            await Assert.That(spriteSource.ResolvedArtIds.Count).IsGreaterThan(0);
            await Assert
                .That(
                    shell.Scene.PaintableScene.Items.Any(item =>
                        item.Sprite is { PixelFormat: EditorArtPreviewPixelFormat.Bgra32 }
                    )
                )
                .IsTrue();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_CreateTrackedMapWorldEditShell_PreservesSelectionAndInspectorState_WhenSelectedObjectCarriesFollowerArrayField()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeNpcProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            MapProperties mapProperties = new()
            {
                ArtId = 200,
                Unused = 0,
                LimitX = 2,
                LimitY = 2,
            };
            MapPropertiesFormat.WriteToFile(in mapProperties, Path.Combine(contentDir, "maps", "map01", "map.prp"));

            var selectedObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 42, Guid.NewGuid());
            var selectedObject = new MobData
            {
                Header = new GameObjectHeader
                {
                    Version = 0x08,
                    ProtoId = MakeProtoId(protoNumber),
                    ObjectId = selectedObjectId,
                    GameObjectType = ObjectType.Npc,
                    PropCollectionItems = 0,
                    Bitmap = new byte[ObjectFieldBitmapSize.For(ObjectType.Npc)],
                },
                Properties = [],
            }
                .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFCurrentAid, unchecked((int)0x01020304u)))
                .WithProperty(ObjectPropertyFactory.ForLocation(ObjectField.ObjFLocation, 5, 6))
                .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFOffsetX, 3))
                .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFOffsetY, 4))
                .WithProperty(ObjectPropertyFactory.ForFloat(ObjectField.ObjFOffsetZ, 5.5f))
                .WithProperty(ObjectPropertyFactory.ForFloat(ObjectField.ObjFHeight, 6.5f))
                .WithProperty(ObjectPropertyFactory.ForEmptyObjectIdArray(ObjectField.ObjFCritterFollowerIdx));
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
                    Selection = new EditorProjectMapSelectionState
                    {
                        SectorAssetPath = sectorAssetPath,
                        Tile = new Location(5, 6),
                        ObjectId = selectedObject.Header.ObjectId,
                    },
                }
            );

            var shell = session.CreateTrackedMapWorldEditShell("map-view-1");

            await Assert.That(shell.Scene.SceneRender.MapName).IsEqualTo("map01");
            await Assert.That(shell.Scene.SceneRender.Objects.Count).IsEqualTo(1);
            await Assert.That(shell.Scene.SceneRender.Objects[0].ObjectId).IsEqualTo(selectedObject.Header.ObjectId);
            await Assert.That(shell.ObjectSelection.SelectedObjects.Count).IsEqualTo(1);
            await Assert.That(shell.ObjectSelection.SectorAssetPaths).IsEquivalentTo([sectorAssetPath]);
            await Assert
                .That(shell.ObjectSelection.SelectedObjects[0].ObjectId)
                .IsEqualTo(selectedObject.Header.ObjectId);
            await Assert
                .That(shell.ObjectInspector.TargetKind)
                .IsEqualTo(EditorObjectInspectorTargetKind.SelectedObject);
            await Assert.That(shell.ObjectInspector.SelectedObject).IsNotNull();
            await Assert
                .That(shell.ObjectInspectorFlags.Inspector.TargetKind)
                .IsEqualTo(EditorObjectInspectorTargetKind.SelectedObject);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_SetTrackedMapWorldEditShellPreferences_UsesPersistedDefaults_AndPreservesTrackedState()
    {
        const int guardProtoNumber = 1001;
        const int wolfProtoNumber = 1002;
        const ulong sectorKey = 101334386389UL;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

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

            var selectedObject = new MobDataBuilder(MakePc(guardProtoNumber)).WithLocation(5, 6).Build();
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
                    WorldEdit = new EditorProjectMapWorldEditState
                    {
                        ActiveTool = EditorProjectMapWorldEditActiveTool.ObjectPlacement,
                    },
                }
            );
            _ = session.SetTrackedObjectPlacementEntry("map-view-1", wolfProtoNumber, rotation: 0.5f);
            _ = session.SelectTrackedObjectPaletteEntry(
                "map-view-1",
                wolfProtoNumber,
                searchText: "wolf",
                category: "pc"
            );
            _ = session.SetTrackedTerrainPaletteEntry("map-view-1", 1, 1, activateTool: false);

            var persistedShellState = session.SetTrackedMapWorldEditShellPreferences(
                "map-view-1",
                new EditorMapWorldEditShellRequest
                {
                    ViewMode = EditorMapSceneViewMode.TopDown,
                    ViewportWidth = 320d,
                    ViewportHeight = 200d,
                    ObjectPaletteSearchText = "wolf",
                    ObjectPaletteCategory = "pc",
                    IncludeTrackedPlacementPreview = false,
                }
            );

            var shell = session.CreateTrackedMapWorldEditShell("map-view-1");

            await Assert.That(persistedShellState.ViewMode).IsEqualTo(EditorMapSceneViewMode.TopDown);
            await Assert.That(persistedShellState.ViewportWidth).IsEqualTo(320d);
            await Assert.That(persistedShellState.ViewportHeight).IsEqualTo(200d);
            await Assert.That(persistedShellState.ObjectPaletteSearchText).IsEqualTo("wolf");
            await Assert.That(persistedShellState.ObjectPaletteCategory).IsEqualTo("pc");
            await Assert.That(persistedShellState.IncludeTrackedPlacementPreview).IsFalse();
            await Assert
                .That(session.GetMapViewStates()[0].WorldEdit.Shell.ViewMode)
                .IsEqualTo(EditorMapSceneViewMode.TopDown);
            await Assert.That(session.GetMapViewStates()[0].WorldEdit.Terrain.PaletteX).IsEqualTo(1UL);
            await Assert.That(session.GetMapViewStates()[0].WorldEdit.Terrain.PaletteY).IsEqualTo(1UL);
            await Assert.That(session.GetMapViewStates()[0].WorldEdit.ObjectPlacement.PlacementRequest).IsNotNull();
            await Assert
                .That(session.GetMapViewStates()[0].WorldEdit.ObjectPlacement.PlacementRequest!.ProtoNumber)
                .IsEqualTo(wolfProtoNumber);
            await Assert
                .That(session.GetMapViewStates()[0].WorldEdit.ObjectPlacement.SelectedPaletteProtoNumber)
                .IsEqualTo(wolfProtoNumber);
            await Assert.That(shell.ViewMode).IsEqualTo(EditorMapSceneViewMode.TopDown);
            await Assert.That(shell.Scene.SceneRender.ViewMode).IsEqualTo(EditorMapSceneViewMode.TopDown);
            await Assert.That(shell.Scene.ViewportLayout.ViewportWidth).IsEqualTo(320d);
            await Assert.That(shell.Scene.ViewportLayout.ViewportHeight).IsEqualTo(200d);
            await Assert.That(shell.HasTrackedPlacementPreview).IsFalse();
            await Assert.That(shell.TrackedPlacementPreview).IsNull();
            await Assert.That(shell.ObjectPalette.SearchText).IsEqualTo("wolf");
            await Assert.That(shell.ObjectPalette.Category).IsEqualTo("pc");
            await Assert.That(shell.ObjectPalette.Entries.Count).IsEqualTo(1);
            await Assert.That(shell.ObjectPalette.Entries[0].ProtoNumber).IsEqualTo(wolfProtoNumber);
            await Assert.That(shell.ObjectPalette.SelectedEntry).IsNotNull();
            await Assert.That(shell.ObjectPalette.SelectedEntry!.ProtoNumber).IsEqualTo(wolfProtoNumber);
            await Assert.That(shell.ObjectPlacementTool.CanPreviewOrApply).IsTrue();
            await Assert.That(shell.TerrainPalette.SelectedEntry).IsNotNull();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_ManageTrackedObjectPlacementPresetLibrary_PreservesCurrentToolState()
    {
        const int guardProtoNumber = 1001;
        const int wolfProtoNumber = 1002;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(guardProtoNumber),
                Path.Combine(contentDir, "proto", "001001 - Guard.pro")
            );
            ProtoFormat.WriteToFile(MakeProto(wolfProtoNumber), Path.Combine(contentDir, "proto", "001002 - Wolf.pro"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(new EditorProjectMapViewState { Id = "map-view-1", MapName = "map01" });

            var guardEntry = workspace.FindObjectPaletteEntry(guardProtoNumber);
            var wolfEntry = workspace.FindObjectPaletteEntry(wolfProtoNumber);

            await Assert.That(guardEntry).IsNotNull();
            await Assert.That(wolfEntry).IsNotNull();

            _ = session.SetTrackedObjectPlacementEntry("map-view-1", guardEntry!, rotation: 1.25f);

            var updatedState = session.SetTrackedObjectPlacementPresetLibrary(
                "map-view-1",
                [
                    wolfEntry!.CreatePlacementPreset("wolf-pack", "Wolf Pack", deltaTileX: 1),
                    guardEntry.CreatePlacementPreset("guard-post", "Guard Post", rotation: 2.5f),
                ],
                selectedPresetId: "missing-preset",
                activateTool: false
            );
            var presetLibrary = session.GetTrackedObjectPlacementPresetLibrary("map-view-1");
            var foundPreset = session.FindTrackedObjectPlacementPreset("map-view-1", "GUARD-POST");
            var summary = session.GetTrackedObjectPlacementToolSummary("map-view-1");

            await Assert.That(updatedState.Mode).IsEqualTo(EditorProjectMapObjectPlacementMode.SinglePlacement);
            await Assert.That(updatedState.PresetLibrary.Count).IsEqualTo(2);
            await Assert.That(updatedState.SelectedPresetId).IsEqualTo("wolf-pack");
            await Assert
                .That(presetLibrary.Select(static preset => preset.PresetId))
                .IsEquivalentTo(["wolf-pack", "guard-post"]);
            await Assert.That(foundPreset).IsNotNull();
            await Assert.That(foundPreset!.Name).IsEqualTo("Guard Post");
            await Assert.That(summary.ToolState.Mode).IsEqualTo(EditorProjectMapObjectPlacementMode.SinglePlacement);
            await Assert.That(summary.ToolState.PlacementRequest).IsNotNull();
            await Assert.That(summary.SelectedPreset).IsNull();
            await Assert.That(summary.CanPreviewOrApply).IsTrue();

            var removed = session.RemoveTrackedObjectPlacementPreset("map-view-1", "wolf-pack");
            var afterRemove = session.GetTrackedObjectPlacementToolSummary("map-view-1");

            await Assert.That(removed).IsTrue();
            await Assert
                .That(afterRemove.ToolState.Mode)
                .IsEqualTo(EditorProjectMapObjectPlacementMode.SinglePlacement);
            await Assert.That(afterRemove.ToolState.PresetLibrary.Count).IsEqualTo(1);
            await Assert.That(afterRemove.ToolState.SelectedPresetId).IsEqualTo("guard-post");
            await Assert.That(afterRemove.CanPreviewOrApply).IsTrue();
            await Assert.That(session.RemoveTrackedObjectPlacementPreset("map-view-1", "missing-preset")).IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task MapViewWorldEditToolHelpers_SetTrackedObjectPlacementPresetLibrary_CanActivatePresetWorkflow()
    {
        const int guardProtoNumber = 1001;
        const int wolfProtoNumber = 1002;

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));

        try
        {
            ProtoFormat.WriteToFile(
                MakeProto(guardProtoNumber),
                Path.Combine(contentDir, "proto", "001001 - Guard.pro")
            );
            ProtoFormat.WriteToFile(MakeProto(wolfProtoNumber), Path.Combine(contentDir, "proto", "001002 - Wolf.pro"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var session = workspace.CreateSession();
            _ = session.SetMapViewState(new EditorProjectMapViewState { Id = "map-view-1", MapName = "map01" });

            var guardEntry = workspace.FindObjectPaletteEntry(guardProtoNumber);
            var wolfEntry = workspace.FindObjectPaletteEntry(wolfProtoNumber);

            await Assert.That(guardEntry).IsNotNull();
            await Assert.That(wolfEntry).IsNotNull();

            var presetState = session.SetTrackedObjectPlacementPresetLibrary(
                "map-view-1",
                [
                    EditorObjectPalettePlacementPreset.Create(
                        "encounter",
                        "Encounter",
                        entries:
                        [
                            guardEntry!.CreatePlacementRequest(rotation: 0.5f),
                            wolfEntry!.CreatePlacementRequest(deltaTileX: 1),
                        ]
                    ),
                ],
                activateTool: true
            );
            var summary = session.GetTrackedObjectPlacementToolSummary("map-view-1");

            await Assert.That(presetState.Mode).IsEqualTo(EditorProjectMapObjectPlacementMode.PlacementPreset);
            await Assert.That(presetState.SelectedPresetId).IsEqualTo("encounter");
            await Assert.That(summary.ToolState.Mode).IsEqualTo(EditorProjectMapObjectPlacementMode.PlacementPreset);
            await Assert.That(summary.SelectedPreset).IsNotNull();
            await Assert.That(summary.SelectedPreset!.PresetId).IsEqualTo("encounter");
            await Assert.That(summary.EffectivePlacementSet).IsNotNull();
            await Assert.That(summary.EffectivePlacementSet!.Entries.Count).IsEqualTo(2);
            await Assert.That(summary.ResolvedPaletteEntries.Count).IsEqualTo(2);
            await Assert.That(summary.CanPreviewOrApply).IsTrue();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_ApplyObjectTransformRequest_RoutesPointAndAreaSelections()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        const float pointRotation = 0.75f;
        const float pointRotationPitch = 1.5f;
        const float areaRotation = 2.25f;
        const float areaRotationPitch = 3.5f;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            var transformedPointObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(5, 6).Build();
            var selectedAreaObjectA = new MobDataBuilder(MakePc(protoNumber)).WithLocation(63, 2).Build();
            var retainedAreaObjectA = new MobDataBuilder(MakePc(protoNumber)).WithLocation(63, 2).Build();
            var selectedAreaObjectB = new MobDataBuilder(MakePc(protoNumber)).WithLocation(0, 2).Build();
            var retainedAreaObjectB = new MobDataBuilder(MakePc(protoNumber)).WithLocation(0, 2).Build();

            SectorFormat.WriteToFile(
                MakeSector(transformedPointObject, selectedAreaObjectA, retainedAreaObjectA),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(selectedAreaObjectB, retainedAreaObjectB),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var preview = workspace.CreateMapScenePreview("map01");

            var pointSession = workspace.CreateSession();
            var pointSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(5, 6),
                ObjectId = transformedPointObject.Header.ObjectId,
            };

            var pointResult = pointSession.ApplySectorObjectTransform(
                preview,
                pointSelection,
                EditorMapObjectTransformRequest.Transform(
                    deltaTileX: 1,
                    deltaTileY: 2,
                    rotation: pointRotation,
                    rotationPitch: pointRotationPitch
                )
            );

            await Assert.That(pointResult.HasChanges).IsTrue();
            await Assert.That(pointResult.CreatedObjectCount).IsEqualTo(0);
            await Assert.That(pointResult.RemovedObjectCount).IsEqualTo(0);
            await Assert.That(pointResult.UpdatedObjectCount).IsEqualTo(1);
            await Assert.That(pointResult.UpdatedObjectIds.Single()).IsEqualTo(transformedPointObject.Header.ObjectId);

            var pointWorkspace = pointSession
                .BeginChangeGroup("Transform point object selection")
                .ApplyPendingChanges();
            var pointSectorA = pointWorkspace.FindSector(sectorAssetPathA);

            await Assert.That(pointSectorA).IsNotNull();

            var updatedPointObject = pointSectorA!.Objects.Single(obj =>
                obj.Header.ObjectId == transformedPointObject.Header.ObjectId
            );
            await Assert
                .That(updatedPointObject.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((6, 8));
            await Assert
                .That(updatedPointObject.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat())
                .IsEqualTo(pointRotation);
            await Assert
                .That(updatedPointObject.GetProperty(ObjectField.ObjFRotationPitch)!.GetFloat())
                .IsEqualTo(pointRotationPitch);

            var areaSession = workspace.CreateSession();
            var areaSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(63, 2),
                ObjectId = selectedAreaObjectA.Header.ObjectId,
                Area = new EditorProjectMapAreaSelectionState
                {
                    MinMapTileX = 63,
                    MinMapTileY = 2,
                    MaxMapTileX = 64,
                    MaxMapTileY = 2,
                    ObjectIds = [selectedAreaObjectA.Header.ObjectId, selectedAreaObjectB.Header.ObjectId],
                },
            };

            var areaResult = areaSession.ApplySectorObjectTransform(
                preview,
                areaSelection,
                EditorMapObjectTransformRequest.Transform(
                    deltaTileX: 0,
                    deltaTileY: 1,
                    rotation: areaRotation,
                    rotationPitch: areaRotationPitch
                )
            );

            await Assert.That(areaResult.HasChanges).IsTrue();
            await Assert.That(areaResult.CreatedObjectCount).IsEqualTo(0);
            await Assert.That(areaResult.RemovedObjectCount).IsEqualTo(0);
            await Assert.That(areaResult.UpdatedObjectCount).IsEqualTo(2);
            await Assert.That(areaResult.UpdatedObjectIds[0]).IsEqualTo(selectedAreaObjectA.Header.ObjectId);
            await Assert.That(areaResult.UpdatedObjectIds[1]).IsEqualTo(selectedAreaObjectB.Header.ObjectId);

            var areaWorkspace = areaSession.BeginChangeGroup("Transform area object selection").ApplyPendingChanges();
            var areaSectorA = areaWorkspace.FindSector(sectorAssetPathA);
            var areaSectorB = areaWorkspace.FindSector(sectorAssetPathB);

            await Assert.That(areaSectorA).IsNotNull();
            await Assert.That(areaSectorB).IsNotNull();

            var updatedSelectedAreaObjectA = areaSectorA!.Objects.Single(obj =>
                obj.Header.ObjectId == selectedAreaObjectA.Header.ObjectId
            );
            var updatedRetainedAreaObjectA = areaSectorA.Objects.Single(obj =>
                obj.Header.ObjectId == retainedAreaObjectA.Header.ObjectId
            );
            var updatedSelectedAreaObjectB = areaSectorB!.Objects.Single(obj =>
                obj.Header.ObjectId == selectedAreaObjectB.Header.ObjectId
            );
            var updatedRetainedAreaObjectB = areaSectorB.Objects.Single(obj =>
                obj.Header.ObjectId == retainedAreaObjectB.Header.ObjectId
            );

            await Assert
                .That(updatedSelectedAreaObjectA.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((63, 3));
            await Assert
                .That(updatedSelectedAreaObjectB.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((0, 3));
            await Assert
                .That(updatedRetainedAreaObjectA.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((63, 2));
            await Assert
                .That(updatedRetainedAreaObjectB.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((0, 2));
            await Assert
                .That(updatedSelectedAreaObjectA.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat())
                .IsEqualTo(areaRotation);
            await Assert
                .That(updatedSelectedAreaObjectB.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat())
                .IsEqualTo(areaRotation);
            await Assert
                .That(updatedSelectedAreaObjectA.GetProperty(ObjectField.ObjFRotationPitch)!.GetFloat())
                .IsEqualTo(areaRotationPitch);
            await Assert
                .That(updatedSelectedAreaObjectB.GetProperty(ObjectField.ObjFRotationPitch)!.GetFloat())
                .IsEqualTo(areaRotationPitch);
            await Assert.That(updatedRetainedAreaObjectA.GetProperty(ObjectField.ObjFPadIas1)).IsNull();
            await Assert.That(updatedRetainedAreaObjectB.GetProperty(ObjectField.ObjFPadIas1)).IsNull();
            await Assert.That(updatedRetainedAreaObjectA.GetProperty(ObjectField.ObjFRotationPitch)).IsNull();
            await Assert.That(updatedRetainedAreaObjectB.GetProperty(ObjectField.ObjFRotationPitch)).IsNull();

            var areaPreview = areaWorkspace.CreateMapScenePreview("map01");
            var areaPreviewSectorA = areaPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathA);
            var areaPreviewSectorB = areaPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathB);

            await Assert
                .That(
                    areaPreviewSectorA
                        .Objects.Single(obj => obj.ObjectId == selectedAreaObjectA.Header.ObjectId)
                        .Location
                )
                .IsEqualTo(new Location(63, 3));
            await Assert
                .That(
                    areaPreviewSectorA
                        .Objects.Single(obj => obj.ObjectId == selectedAreaObjectA.Header.ObjectId)
                        .Rotation
                )
                .IsEqualTo(areaRotation);
            await Assert
                .That(
                    areaPreviewSectorA
                        .Objects.Single(obj => obj.ObjectId == selectedAreaObjectA.Header.ObjectId)
                        .RotationPitch
                )
                .IsEqualTo(areaRotationPitch);
            await Assert
                .That(
                    areaPreviewSectorB
                        .Objects.Single(obj => obj.ObjectId == selectedAreaObjectB.Header.ObjectId)
                        .Location
                )
                .IsEqualTo(new Location(0, 3));
            await Assert
                .That(
                    areaPreviewSectorB
                        .Objects.Single(obj => obj.ObjectId == selectedAreaObjectB.Header.ObjectId)
                        .Rotation
                )
                .IsEqualTo(areaRotation);
            await Assert
                .That(
                    areaPreviewSectorB
                        .Objects.Single(obj => obj.ObjectId == selectedAreaObjectB.Header.ObjectId)
                        .RotationPitch
                )
                .IsEqualTo(areaRotationPitch);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_ApplyObjectBrushRequest_MoveByOffset_RoutesPointAndAreaSelections()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            var movedPointObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(5, 6).Build();
            var selectedAreaObjectA = new MobDataBuilder(MakePc(protoNumber)).WithLocation(63, 2).Build();
            var retainedAreaObjectA = new MobDataBuilder(MakePc(protoNumber)).WithLocation(63, 2).Build();
            var selectedAreaObjectB = new MobDataBuilder(MakePc(protoNumber)).WithLocation(0, 2).Build();
            var retainedAreaObjectB = new MobDataBuilder(MakePc(protoNumber)).WithLocation(0, 2).Build();

            SectorFormat.WriteToFile(
                MakeSector(movedPointObject, selectedAreaObjectA, retainedAreaObjectA),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(selectedAreaObjectB, retainedAreaObjectB),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var preview = workspace.CreateMapScenePreview("map01");

            var pointSession = workspace.CreateSession();
            var pointSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(5, 6),
                ObjectId = movedPointObject.Header.ObjectId,
            };

            var pointResult = pointSession.ApplySectorObjectBrush(
                preview,
                pointSelection,
                EditorMapObjectBrushRequest.MoveByOffset(deltaTileX: 2, deltaTileY: 3)
            );

            await Assert.That(pointResult.HasChanges).IsTrue();
            await Assert.That(pointResult.CreatedObjectCount).IsEqualTo(0);
            await Assert.That(pointResult.RemovedObjectCount).IsEqualTo(0);
            await Assert.That(pointResult.UpdatedObjectCount).IsEqualTo(1);
            await Assert.That(pointResult.UpdatedObjectIds.Single()).IsEqualTo(movedPointObject.Header.ObjectId);

            var pointWorkspace = pointSession.BeginChangeGroup("Move point object selection").ApplyPendingChanges();
            var pointSectorA = pointWorkspace.FindSector(sectorAssetPathA);

            await Assert.That(pointSectorA).IsNotNull();
            await Assert
                .That(
                    pointSectorA!
                        .Objects.Single(obj => obj.Header.ObjectId == movedPointObject.Header.ObjectId)
                        .GetProperty(ObjectField.ObjFLocation)!
                        .GetLocation()
                )
                .IsEqualTo((7, 9));

            var areaSession = workspace.CreateSession();
            var areaSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(63, 2),
                ObjectId = selectedAreaObjectA.Header.ObjectId,
                Area = new EditorProjectMapAreaSelectionState
                {
                    MinMapTileX = 63,
                    MinMapTileY = 2,
                    MaxMapTileX = 64,
                    MaxMapTileY = 2,
                    ObjectIds = [selectedAreaObjectA.Header.ObjectId, selectedAreaObjectB.Header.ObjectId],
                },
            };

            var areaResult = areaSession.ApplySectorObjectBrush(
                preview,
                areaSelection,
                EditorMapObjectBrushRequest.MoveByOffset(deltaTileX: 0, deltaTileY: 1)
            );

            await Assert.That(areaResult.HasChanges).IsTrue();
            await Assert.That(areaResult.CreatedObjectCount).IsEqualTo(0);
            await Assert.That(areaResult.RemovedObjectCount).IsEqualTo(0);
            await Assert.That(areaResult.UpdatedObjectCount).IsEqualTo(2);
            await Assert.That(areaResult.UpdatedObjectIds[0]).IsEqualTo(selectedAreaObjectA.Header.ObjectId);
            await Assert.That(areaResult.UpdatedObjectIds[1]).IsEqualTo(selectedAreaObjectB.Header.ObjectId);

            var areaWorkspace = areaSession.BeginChangeGroup("Move area object selection").ApplyPendingChanges();
            var areaSectorA = areaWorkspace.FindSector(sectorAssetPathA);
            var areaSectorB = areaWorkspace.FindSector(sectorAssetPathB);

            await Assert.That(areaSectorA).IsNotNull();
            await Assert.That(areaSectorB).IsNotNull();

            var updatedSelectedAreaObjectA = areaSectorA!.Objects.Single(obj =>
                obj.Header.ObjectId == selectedAreaObjectA.Header.ObjectId
            );
            var updatedRetainedAreaObjectA = areaSectorA.Objects.Single(obj =>
                obj.Header.ObjectId == retainedAreaObjectA.Header.ObjectId
            );
            var updatedSelectedAreaObjectB = areaSectorB!.Objects.Single(obj =>
                obj.Header.ObjectId == selectedAreaObjectB.Header.ObjectId
            );
            var updatedRetainedAreaObjectB = areaSectorB.Objects.Single(obj =>
                obj.Header.ObjectId == retainedAreaObjectB.Header.ObjectId
            );

            await Assert
                .That(updatedSelectedAreaObjectA.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((63, 3));
            await Assert
                .That(updatedSelectedAreaObjectB.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((0, 3));
            await Assert
                .That(updatedRetainedAreaObjectA.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((63, 2));
            await Assert
                .That(updatedRetainedAreaObjectB.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((0, 2));

            var areaPreview = areaWorkspace.CreateMapScenePreview("map01");
            var areaPreviewSectorA = areaPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathA);
            var areaPreviewSectorB = areaPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathB);

            await Assert
                .That(
                    areaPreviewSectorA
                        .Objects.Single(obj => obj.ObjectId == selectedAreaObjectA.Header.ObjectId)
                        .Location
                )
                .IsEqualTo(new Location(63, 3));
            await Assert
                .That(
                    areaPreviewSectorB
                        .Objects.Single(obj => obj.ObjectId == selectedAreaObjectB.Header.ObjectId)
                        .Location
                )
                .IsEqualTo(new Location(0, 3));
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_ApplyObjectTransformRequest_SnapToTileGrid_RoutesPointAndAreaSelections()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            var pointObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(5, 6).WithOffset(12, -8).Build();
            var selectedAreaObjectA = new MobDataBuilder(MakePc(protoNumber))
                .WithLocation(63, 2)
                .WithOffset(3, 4)
                .Build();
            var retainedAreaObjectA = new MobDataBuilder(MakePc(protoNumber))
                .WithLocation(63, 2)
                .WithOffset(9, 10)
                .Build();
            var selectedAreaObjectB = new MobDataBuilder(MakePc(protoNumber))
                .WithLocation(0, 2)
                .WithOffset(-7, 11)
                .Build();
            var retainedAreaObjectB = new MobDataBuilder(MakePc(protoNumber))
                .WithLocation(0, 2)
                .WithOffset(-5, 6)
                .Build();

            SectorFormat.WriteToFile(
                MakeSector(pointObject, selectedAreaObjectA, retainedAreaObjectA),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(selectedAreaObjectB, retainedAreaObjectB),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var preview = workspace.CreateMapScenePreview("map01");

            var pointSession = workspace.CreateSession();
            var pointSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(5, 6),
                ObjectId = pointObject.Header.ObjectId,
            };

            var pointResult = pointSession.ApplySectorObjectTransform(
                preview,
                pointSelection,
                EditorMapObjectTransformRequest.SnapToTileGrid()
            );

            await Assert.That(pointResult.HasChanges).IsTrue();
            await Assert.That(pointResult.UpdatedObjectCount).IsEqualTo(1);
            await Assert.That(pointResult.UpdatedObjectIds.Single()).IsEqualTo(pointObject.Header.ObjectId);

            var pointWorkspace = pointSession.BeginChangeGroup("Snap point object to tile grid").ApplyPendingChanges();
            var pointSectorA = pointWorkspace.FindSector(sectorAssetPathA);

            await Assert.That(pointSectorA).IsNotNull();

            var updatedPointObject = pointSectorA!.Objects.Single(obj =>
                obj.Header.ObjectId == pointObject.Header.ObjectId
            );
            await Assert
                .That(updatedPointObject.GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((5, 6));
            await Assert.That(updatedPointObject.GetProperty(ObjectField.ObjFOffsetX)!.GetInt32()).IsEqualTo(0);
            await Assert.That(updatedPointObject.GetProperty(ObjectField.ObjFOffsetY)!.GetInt32()).IsEqualTo(0);

            var pointPreview = pointWorkspace.CreateMapScenePreview("map01");
            var pointPreviewSectorA = pointPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathA);
            await Assert
                .That(
                    pointPreviewSectorA
                        .Objects.Single(obj => obj.ObjectId == pointObject.Header.ObjectId)
                        .IsTileGridSnapped
                )
                .IsTrue();

            var areaSession = workspace.CreateSession();
            var areaSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(63, 2),
                ObjectId = selectedAreaObjectA.Header.ObjectId,
                Area = new EditorProjectMapAreaSelectionState
                {
                    MinMapTileX = 63,
                    MinMapTileY = 2,
                    MaxMapTileX = 64,
                    MaxMapTileY = 2,
                    ObjectIds = [selectedAreaObjectA.Header.ObjectId, selectedAreaObjectB.Header.ObjectId],
                },
            };

            var areaResult = areaSession.ApplySectorObjectTransform(
                preview,
                areaSelection,
                EditorMapObjectTransformRequest.SnapToTileGrid()
            );

            await Assert.That(areaResult.HasChanges).IsTrue();
            await Assert.That(areaResult.UpdatedObjectCount).IsEqualTo(2);
            await Assert.That(areaResult.UpdatedObjectIds[0]).IsEqualTo(selectedAreaObjectA.Header.ObjectId);
            await Assert.That(areaResult.UpdatedObjectIds[1]).IsEqualTo(selectedAreaObjectB.Header.ObjectId);

            var areaWorkspace = areaSession.BeginChangeGroup("Snap area objects to tile grid").ApplyPendingChanges();
            var areaSectorA = areaWorkspace.FindSector(sectorAssetPathA);
            var areaSectorB = areaWorkspace.FindSector(sectorAssetPathB);

            await Assert.That(areaSectorA).IsNotNull();
            await Assert.That(areaSectorB).IsNotNull();

            var updatedSelectedAreaObjectA = areaSectorA!.Objects.Single(obj =>
                obj.Header.ObjectId == selectedAreaObjectA.Header.ObjectId
            );
            var updatedRetainedAreaObjectA = areaSectorA.Objects.Single(obj =>
                obj.Header.ObjectId == retainedAreaObjectA.Header.ObjectId
            );
            var updatedSelectedAreaObjectB = areaSectorB!.Objects.Single(obj =>
                obj.Header.ObjectId == selectedAreaObjectB.Header.ObjectId
            );
            var updatedRetainedAreaObjectB = areaSectorB.Objects.Single(obj =>
                obj.Header.ObjectId == retainedAreaObjectB.Header.ObjectId
            );

            await Assert.That(updatedSelectedAreaObjectA.GetProperty(ObjectField.ObjFOffsetX)!.GetInt32()).IsEqualTo(0);
            await Assert.That(updatedSelectedAreaObjectA.GetProperty(ObjectField.ObjFOffsetY)!.GetInt32()).IsEqualTo(0);
            await Assert.That(updatedSelectedAreaObjectB.GetProperty(ObjectField.ObjFOffsetX)!.GetInt32()).IsEqualTo(0);
            await Assert.That(updatedSelectedAreaObjectB.GetProperty(ObjectField.ObjFOffsetY)!.GetInt32()).IsEqualTo(0);
            await Assert.That(updatedRetainedAreaObjectA.GetProperty(ObjectField.ObjFOffsetX)).IsNotNull();
            await Assert.That(updatedRetainedAreaObjectA.GetProperty(ObjectField.ObjFOffsetY)).IsNotNull();
            await Assert.That(updatedRetainedAreaObjectB.GetProperty(ObjectField.ObjFOffsetX)).IsNotNull();
            await Assert.That(updatedRetainedAreaObjectB.GetProperty(ObjectField.ObjFOffsetY)).IsNotNull();

            var areaPreview = areaWorkspace.CreateMapScenePreview("map01");
            var areaPreviewSectorA = areaPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathA);
            var areaPreviewSectorB = areaPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathB);

            await Assert
                .That(
                    areaPreviewSectorA
                        .Objects.Single(obj => obj.ObjectId == selectedAreaObjectA.Header.ObjectId)
                        .IsTileGridSnapped
                )
                .IsTrue();
            await Assert
                .That(
                    areaPreviewSectorB
                        .Objects.Single(obj => obj.ObjectId == selectedAreaObjectB.Header.ObjectId)
                        .IsTileGridSnapped
                )
                .IsTrue();
            await Assert
                .That(
                    areaPreviewSectorA
                        .Objects.Single(obj => obj.ObjectId == retainedAreaObjectA.Header.ObjectId)
                        .IsTileGridSnapped
                )
                .IsFalse();
            await Assert
                .That(
                    areaPreviewSectorB
                        .Objects.Single(obj => obj.ObjectId == retainedAreaObjectB.Header.ObjectId)
                        .IsTileGridSnapped
                )
                .IsFalse();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_ApplyObjectBrushRequest_RotatePitch_RoutesPointAndAreaSelections()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        const float pointRotationPitch = 2.5f;
        const float areaRotationPitch = 7.25f;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            var rotatedPointObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(5, 6).Build();
            var selectedAreaObjectA = new MobDataBuilder(MakePc(protoNumber)).WithLocation(63, 2).Build();
            var retainedAreaObjectA = new MobDataBuilder(MakePc(protoNumber)).WithLocation(63, 2).Build();
            var selectedAreaObjectB = new MobDataBuilder(MakePc(protoNumber)).WithLocation(0, 2).Build();
            var retainedAreaObjectB = new MobDataBuilder(MakePc(protoNumber)).WithLocation(0, 2).Build();

            SectorFormat.WriteToFile(
                MakeSector(rotatedPointObject, selectedAreaObjectA, retainedAreaObjectA),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(selectedAreaObjectB, retainedAreaObjectB),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var preview = workspace.CreateMapScenePreview("map01");

            var pointSession = workspace.CreateSession();
            var pointSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(5, 6),
                ObjectId = rotatedPointObject.Header.ObjectId,
            };

            var pointResult = pointSession.ApplySectorObjectBrush(
                preview,
                pointSelection,
                EditorMapObjectBrushRequest.RotatePitch(pointRotationPitch)
            );

            await Assert.That(pointResult.HasChanges).IsTrue();
            await Assert.That(pointResult.CreatedObjectCount).IsEqualTo(0);
            await Assert.That(pointResult.RemovedObjectCount).IsEqualTo(0);
            await Assert.That(pointResult.UpdatedObjectCount).IsEqualTo(1);
            await Assert.That(pointResult.UpdatedObjectIds.Single()).IsEqualTo(rotatedPointObject.Header.ObjectId);

            var pointWorkspace = pointSession
                .BeginChangeGroup("Rotate point object pitch selection")
                .ApplyPendingChanges();
            var pointSectorA = pointWorkspace.FindSector(sectorAssetPathA);

            await Assert.That(pointSectorA).IsNotNull();
            await Assert
                .That(
                    pointSectorA!
                        .Objects.Single(obj => obj.Header.ObjectId == rotatedPointObject.Header.ObjectId)
                        .GetProperty(ObjectField.ObjFRotationPitch)!
                        .GetFloat()
                )
                .IsEqualTo(pointRotationPitch);

            var areaSession = workspace.CreateSession();
            var areaSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(63, 2),
                ObjectId = selectedAreaObjectA.Header.ObjectId,
                Area = new EditorProjectMapAreaSelectionState
                {
                    MinMapTileX = 63,
                    MinMapTileY = 2,
                    MaxMapTileX = 64,
                    MaxMapTileY = 2,
                    ObjectIds = [selectedAreaObjectA.Header.ObjectId, selectedAreaObjectB.Header.ObjectId],
                },
            };

            var areaResult = areaSession.ApplySectorObjectBrush(
                preview,
                areaSelection,
                EditorMapObjectBrushRequest.RotatePitch(areaRotationPitch)
            );

            await Assert.That(areaResult.HasChanges).IsTrue();
            await Assert.That(areaResult.CreatedObjectCount).IsEqualTo(0);
            await Assert.That(areaResult.RemovedObjectCount).IsEqualTo(0);
            await Assert.That(areaResult.UpdatedObjectCount).IsEqualTo(2);
            await Assert.That(areaResult.UpdatedObjectIds[0]).IsEqualTo(selectedAreaObjectA.Header.ObjectId);
            await Assert.That(areaResult.UpdatedObjectIds[1]).IsEqualTo(selectedAreaObjectB.Header.ObjectId);

            var areaWorkspace = areaSession
                .BeginChangeGroup("Rotate area object pitch selection")
                .ApplyPendingChanges();
            var areaSectorA = areaWorkspace.FindSector(sectorAssetPathA);
            var areaSectorB = areaWorkspace.FindSector(sectorAssetPathB);

            await Assert.That(areaSectorA).IsNotNull();
            await Assert.That(areaSectorB).IsNotNull();

            var updatedSelectedAreaObjectA = areaSectorA!.Objects.Single(obj =>
                obj.Header.ObjectId == selectedAreaObjectA.Header.ObjectId
            );
            var updatedRetainedAreaObjectA = areaSectorA.Objects.Single(obj =>
                obj.Header.ObjectId == retainedAreaObjectA.Header.ObjectId
            );
            var updatedSelectedAreaObjectB = areaSectorB!.Objects.Single(obj =>
                obj.Header.ObjectId == selectedAreaObjectB.Header.ObjectId
            );
            var updatedRetainedAreaObjectB = areaSectorB.Objects.Single(obj =>
                obj.Header.ObjectId == retainedAreaObjectB.Header.ObjectId
            );

            await Assert.That(updatedSelectedAreaObjectA.GetProperty(ObjectField.ObjFRotationPitch)).IsNotNull();
            await Assert.That(updatedSelectedAreaObjectB.GetProperty(ObjectField.ObjFRotationPitch)).IsNotNull();
            await Assert.That(updatedRetainedAreaObjectA.GetProperty(ObjectField.ObjFRotationPitch)).IsNull();
            await Assert.That(updatedRetainedAreaObjectB.GetProperty(ObjectField.ObjFRotationPitch)).IsNull();
            await Assert
                .That(updatedSelectedAreaObjectA.GetProperty(ObjectField.ObjFRotationPitch)!.GetFloat())
                .IsEqualTo(areaRotationPitch);
            await Assert
                .That(updatedSelectedAreaObjectB.GetProperty(ObjectField.ObjFRotationPitch)!.GetFloat())
                .IsEqualTo(areaRotationPitch);

            var areaPreview = areaWorkspace.CreateMapScenePreview("map01");
            var areaPreviewSectorA = areaPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathA);
            var areaPreviewSectorB = areaPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathB);

            await Assert
                .That(
                    areaPreviewSectorA
                        .Objects.Single(obj => obj.ObjectId == selectedAreaObjectA.Header.ObjectId)
                        .RotationPitch
                )
                .IsEqualTo(areaRotationPitch);
            await Assert
                .That(
                    areaPreviewSectorB
                        .Objects.Single(obj => obj.ObjectId == selectedAreaObjectB.Header.ObjectId)
                        .RotationPitch
                )
                .IsEqualTo(areaRotationPitch);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_ApplyObjectBrushRequest_Rotate_RoutesPointAndAreaSelections()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        const float pointRotation = 0.5f;
        const float areaRotation = 1.25f;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            var rotatedPointObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(5, 6).Build();
            var selectedAreaObjectA = new MobDataBuilder(MakePc(protoNumber)).WithLocation(63, 2).Build();
            var retainedAreaObjectA = new MobDataBuilder(MakePc(protoNumber)).WithLocation(63, 2).Build();
            var selectedAreaObjectB = new MobDataBuilder(MakePc(protoNumber)).WithLocation(0, 2).Build();
            var retainedAreaObjectB = new MobDataBuilder(MakePc(protoNumber)).WithLocation(0, 2).Build();

            SectorFormat.WriteToFile(
                MakeSector(rotatedPointObject, selectedAreaObjectA, retainedAreaObjectA),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(selectedAreaObjectB, retainedAreaObjectB),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var preview = workspace.CreateMapScenePreview("map01");

            var pointSession = workspace.CreateSession();
            var pointSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(5, 6),
                ObjectId = rotatedPointObject.Header.ObjectId,
            };

            var pointResult = pointSession.ApplySectorObjectBrush(
                preview,
                pointSelection,
                EditorMapObjectBrushRequest.Rotate(pointRotation)
            );

            await Assert.That(pointResult.HasChanges).IsTrue();
            await Assert.That(pointResult.CreatedObjectCount).IsEqualTo(0);
            await Assert.That(pointResult.RemovedObjectCount).IsEqualTo(0);
            await Assert.That(pointResult.UpdatedObjectCount).IsEqualTo(1);
            await Assert.That(pointResult.UpdatedObjectIds.Single()).IsEqualTo(rotatedPointObject.Header.ObjectId);

            var pointWorkspace = pointSession.BeginChangeGroup("Rotate point object selection").ApplyPendingChanges();
            var pointSectorA = pointWorkspace.FindSector(sectorAssetPathA);

            await Assert.That(pointSectorA).IsNotNull();
            await Assert
                .That(
                    pointSectorA!
                        .Objects.Single(obj => obj.Header.ObjectId == rotatedPointObject.Header.ObjectId)
                        .GetProperty(ObjectField.ObjFPadIas1)!
                        .GetFloat()
                )
                .IsEqualTo(pointRotation);

            var areaSession = workspace.CreateSession();
            var areaSelection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = sectorAssetPathA,
                Tile = new Location(63, 2),
                ObjectId = selectedAreaObjectA.Header.ObjectId,
                Area = new EditorProjectMapAreaSelectionState
                {
                    MinMapTileX = 63,
                    MinMapTileY = 2,
                    MaxMapTileX = 64,
                    MaxMapTileY = 2,
                    ObjectIds = [selectedAreaObjectA.Header.ObjectId, selectedAreaObjectB.Header.ObjectId],
                },
            };

            var areaResult = areaSession.ApplySectorObjectBrush(
                preview,
                areaSelection,
                EditorMapObjectBrushRequest.Rotate(areaRotation)
            );

            await Assert.That(areaResult.HasChanges).IsTrue();
            await Assert.That(areaResult.CreatedObjectCount).IsEqualTo(0);
            await Assert.That(areaResult.RemovedObjectCount).IsEqualTo(0);
            await Assert.That(areaResult.UpdatedObjectCount).IsEqualTo(2);
            await Assert.That(areaResult.UpdatedObjectIds[0]).IsEqualTo(selectedAreaObjectA.Header.ObjectId);
            await Assert.That(areaResult.UpdatedObjectIds[1]).IsEqualTo(selectedAreaObjectB.Header.ObjectId);

            var areaWorkspace = areaSession.BeginChangeGroup("Rotate area object selection").ApplyPendingChanges();
            var areaSectorA = areaWorkspace.FindSector(sectorAssetPathA);
            var areaSectorB = areaWorkspace.FindSector(sectorAssetPathB);

            await Assert.That(areaSectorA).IsNotNull();
            await Assert.That(areaSectorB).IsNotNull();

            var updatedSelectedAreaObjectA = areaSectorA!.Objects.Single(obj =>
                obj.Header.ObjectId == selectedAreaObjectA.Header.ObjectId
            );
            var updatedRetainedAreaObjectA = areaSectorA.Objects.Single(obj =>
                obj.Header.ObjectId == retainedAreaObjectA.Header.ObjectId
            );
            var updatedSelectedAreaObjectB = areaSectorB!.Objects.Single(obj =>
                obj.Header.ObjectId == selectedAreaObjectB.Header.ObjectId
            );
            var updatedRetainedAreaObjectB = areaSectorB.Objects.Single(obj =>
                obj.Header.ObjectId == retainedAreaObjectB.Header.ObjectId
            );

            await Assert.That(updatedSelectedAreaObjectA.GetProperty(ObjectField.ObjFPadIas1)).IsNotNull();
            await Assert.That(updatedSelectedAreaObjectB.GetProperty(ObjectField.ObjFPadIas1)).IsNotNull();
            await Assert.That(updatedRetainedAreaObjectA.GetProperty(ObjectField.ObjFPadIas1)).IsNull();
            await Assert.That(updatedRetainedAreaObjectB.GetProperty(ObjectField.ObjFPadIas1)).IsNull();
            await Assert
                .That(updatedSelectedAreaObjectA.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat())
                .IsEqualTo(areaRotation);
            await Assert
                .That(updatedSelectedAreaObjectB.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat())
                .IsEqualTo(areaRotation);

            var areaPreview = areaWorkspace.CreateMapScenePreview("map01");
            var areaPreviewSectorA = areaPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathA);
            var areaPreviewSectorB = areaPreview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathB);

            await Assert
                .That(
                    areaPreviewSectorA
                        .Objects.Single(obj => obj.ObjectId == selectedAreaObjectA.Header.ObjectId)
                        .Rotation
                )
                .IsEqualTo(areaRotation);
            await Assert
                .That(
                    areaPreviewSectorB
                        .Objects.Single(obj => obj.ObjectId == selectedAreaObjectB.Header.ObjectId)
                        .Rotation
                )
                .IsEqualTo(areaRotation);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_ApplyObjectBrushRequest_StagesStampAndEraseWorkflows()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var stampSession = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var stampHits = new EditorMapSceneSectorHitGroup[]
            {
                new()
                {
                    SectorAssetPath = sectorAssetPathA,
                    LocalX = 0,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 5,
                            MapTileY = 6,
                            SectorAssetPath = sectorAssetPathA,
                            Tile = new Location(5, 6),
                            ObjectHits = [],
                        },
                        new EditorMapSceneHit
                        {
                            MapTileX = 7,
                            MapTileY = 8,
                            SectorAssetPath = sectorAssetPathA,
                            Tile = new Location(7, 8),
                            ObjectHits = [],
                        },
                    ],
                },
                new()
                {
                    SectorAssetPath = sectorAssetPathB,
                    LocalX = 1,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 65,
                            MapTileY = 2,
                            SectorAssetPath = sectorAssetPathB,
                            Tile = new Location(1, 2),
                            ObjectHits = [],
                        },
                    ],
                },
            };

            var stampResult = stampSession.ApplySectorObjectBrush(
                stampHits,
                EditorMapObjectBrushRequest.StampFromProto(protoNumber)
            );

            await Assert.That(stampResult.HasChanges).IsTrue();
            await Assert.That(stampResult.CreatedObjectCount).IsEqualTo(3);
            await Assert.That(stampResult.RemovedObjectCount).IsEqualTo(0);

            var stampedWorkspace = stampSession.BeginChangeGroup("Brush stamp objects").ApplyPendingChanges();
            var eraseSession = stampedWorkspace.CreateSession();
            var eraseHits = new EditorMapSceneSectorHitGroup[]
            {
                new()
                {
                    SectorAssetPath = sectorAssetPathA,
                    LocalX = 0,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 5,
                            MapTileY = 6,
                            SectorAssetPath = sectorAssetPathA,
                            Tile = new Location(5, 6),
                            ObjectHits = [CreateSceneObjectHit(stampResult.CreatedObjects[0], 5, 6)],
                        },
                    ],
                },
                new()
                {
                    SectorAssetPath = sectorAssetPathB,
                    LocalX = 1,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 65,
                            MapTileY = 2,
                            SectorAssetPath = sectorAssetPathB,
                            Tile = new Location(1, 2),
                            ObjectHits = [CreateSceneObjectHit(stampResult.CreatedObjects[2], 1, 2)],
                        },
                    ],
                },
            };

            var eraseResult = eraseSession.ApplySectorObjectBrush(eraseHits, EditorMapObjectBrushRequest.Erase());

            await Assert.That(eraseResult.HasChanges).IsTrue();
            await Assert.That(eraseResult.CreatedObjectCount).IsEqualTo(0);
            await Assert.That(eraseResult.RemovedObjectCount).IsEqualTo(2);
            await Assert.That(eraseResult.RemovedObjectIds[0]).IsEqualTo(stampResult.CreatedObjects[0].Header.ObjectId);
            await Assert.That(eraseResult.RemovedObjectIds[1]).IsEqualTo(stampResult.CreatedObjects[2].Header.ObjectId);

            var erasedWorkspace = eraseSession.BeginChangeGroup("Brush erase objects").ApplyPendingChanges();
            var updatedSectorA = erasedWorkspace.FindSector(sectorAssetPathA);
            var updatedSectorB = erasedWorkspace.FindSector(sectorAssetPathB);

            await Assert.That(updatedSectorA).IsNotNull();
            await Assert.That(updatedSectorB).IsNotNull();
            await Assert.That(updatedSectorA!.Objects.Count).IsEqualTo(1);
            await Assert.That(updatedSectorB!.Objects.Count).IsEqualTo(0);
            await Assert
                .That(updatedSectorA.Objects.Single().Header.ObjectId)
                .IsEqualTo(stampResult.CreatedObjects[1].Header.ObjectId);

            var preview = erasedWorkspace.CreateMapScenePreview("map01");
            var previewSectorA = preview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathA);
            var previewSectorB = preview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathB);

            await Assert.That(previewSectorA.Objects.Count).IsEqualTo(1);
            await Assert.That(previewSectorB.Objects.Count).IsEqualTo(0);
            await Assert.That(previewSectorA.Objects.Single().Location).IsEqualTo(new Location(7, 8));
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorObjectHelpers_ApplyObjectBrushRequest_ReplaceWithProto_StagesOneUndoableEdit()
    {
        const int protoNumber = 1001;
        const ulong southWestSectorKey = 101334386389UL;
        const ulong southEastSectorKey = 101334386390UL;
        var sectorAssetPathA = $"maps/map01/{southWestSectorKey}.sec";
        var sectorAssetPathB = $"maps/map01/{southEastSectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            var removedObjectA = new MobDataBuilder(MakePc(protoNumber)).WithLocation(5, 6).Build();
            var retainedObjectA = new MobDataBuilder(MakePc(protoNumber)).WithLocation(7, 8).Build();
            var removedObjectB = new MobDataBuilder(MakePc(protoNumber)).WithLocation(1, 2).Build();
            var retainedObjectB = new MobDataBuilder(MakePc(protoNumber)).WithLocation(3, 4).Build();

            SectorFormat.WriteToFile(
                MakeSector(removedObjectA, retainedObjectA),
                Path.Combine(contentDir, "maps", "map01", $"{southWestSectorKey}.sec")
            );
            SectorFormat.WriteToFile(
                MakeSector(removedObjectB, retainedObjectB),
                Path.Combine(contentDir, "maps", "map01", $"{southEastSectorKey}.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            var groupedHits = new EditorMapSceneSectorHitGroup[]
            {
                new()
                {
                    SectorAssetPath = sectorAssetPathA,
                    LocalX = 0,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 5,
                            MapTileY = 6,
                            SectorAssetPath = sectorAssetPathA,
                            Tile = new Location(5, 6),
                            ObjectHits = [CreateSceneObjectHit(removedObjectA, 5, 6)],
                        },
                        new EditorMapSceneHit
                        {
                            MapTileX = 5,
                            MapTileY = 6,
                            SectorAssetPath = sectorAssetPathA,
                            Tile = new Location(5, 6),
                            ObjectHits = [CreateSceneObjectHit(removedObjectA, 5, 6)],
                        },
                    ],
                },
                new()
                {
                    SectorAssetPath = sectorAssetPathB,
                    LocalX = 1,
                    LocalY = 0,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = 65,
                            MapTileY = 2,
                            SectorAssetPath = sectorAssetPathB,
                            Tile = new Location(1, 2),
                            ObjectHits = [CreateSceneObjectHit(removedObjectB, 1, 2)],
                        },
                    ],
                },
            };

            var result = session.ApplySectorObjectBrush(
                groupedHits,
                EditorMapObjectBrushRequest.ReplaceWithProto(protoNumber)
            );

            await Assert.That(result.HasChanges).IsTrue();
            await Assert.That(result.CreatedObjectCount).IsEqualTo(2);
            await Assert.That(result.RemovedObjectCount).IsEqualTo(2);
            await Assert.That(result.RemovedObjectIds[0]).IsEqualTo(removedObjectA.Header.ObjectId);
            await Assert.That(result.RemovedObjectIds[1]).IsEqualTo(removedObjectB.Header.ObjectId);
            await Assert
                .That(result.CreatedObjects[0].GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((5, 6));
            await Assert
                .That(result.CreatedObjects[1].GetProperty(ObjectField.ObjFLocation)!.GetLocation())
                .IsEqualTo((1, 2));
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(2);
            await Assert.That(session.CanUndoDirectAssetChanges).IsTrue();
            await Assert.That(session.CanRedoDirectAssetChanges).IsFalse();

            session.UndoDirectAssetChanges();

            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(0);
            await Assert.That(session.CanUndoDirectAssetChanges).IsFalse();
            await Assert.That(session.CanRedoDirectAssetChanges).IsTrue();

            session.RedoDirectAssetChanges();

            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(2);
            await Assert.That(session.CanUndoDirectAssetChanges).IsTrue();

            var updatedWorkspace = session.BeginChangeGroup("Replace sector objects").ApplyPendingChanges();
            var updatedSectorA = updatedWorkspace.FindSector(sectorAssetPathA);
            var updatedSectorB = updatedWorkspace.FindSector(sectorAssetPathB);

            await Assert.That(updatedSectorA).IsNotNull();
            await Assert.That(updatedSectorB).IsNotNull();
            await Assert.That(updatedSectorA!.Objects.Count).IsEqualTo(2);
            await Assert.That(updatedSectorB!.Objects.Count).IsEqualTo(2);
            await Assert
                .That(updatedSectorA.Objects.Any(obj => obj.Header.ObjectId == removedObjectA.Header.ObjectId))
                .IsFalse();
            await Assert
                .That(updatedSectorB.Objects.Any(obj => obj.Header.ObjectId == removedObjectB.Header.ObjectId))
                .IsFalse();
            await Assert
                .That(updatedSectorA.Objects.Any(obj => obj.Header.ObjectId == retainedObjectA.Header.ObjectId))
                .IsTrue();
            await Assert
                .That(updatedSectorB.Objects.Any(obj => obj.Header.ObjectId == retainedObjectB.Header.ObjectId))
                .IsTrue();
            await Assert
                .That(
                    updatedSectorA.Objects.Any(obj => obj.Header.ObjectId == result.CreatedObjects[0].Header.ObjectId)
                )
                .IsTrue();
            await Assert
                .That(
                    updatedSectorB.Objects.Any(obj => obj.Header.ObjectId == result.CreatedObjects[1].Header.ObjectId)
                )
                .IsTrue();

            var preview = updatedWorkspace.CreateMapScenePreview("map01");
            var previewSectorA = preview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathA);
            var previewSectorB = preview.Sectors.Single(sector => sector.AssetPath == sectorAssetPathB);

            await Assert.That(previewSectorA.Objects.Count).IsEqualTo(2);
            await Assert.That(previewSectorB.Objects.Count).IsEqualTo(2);
            await Assert
                .That(previewSectorA.Objects.Any(obj => obj.ObjectId == result.CreatedObjects[0].Header.ObjectId))
                .IsTrue();
            await Assert
                .That(previewSectorB.Objects.Any(obj => obj.ObjectId == result.CreatedObjects[1].Header.ObjectId))
                .IsTrue();
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SetSectorObjectRotation_StagesUpdatedRotation_AndRefreshesPreview()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386392UL;
        const float rotation = 1.25f;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            var rotatedObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(6, 7).Build();
            SectorFormat.WriteToFile(
                MakeSector(rotatedObject),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();

            var rotateChange = session.SetSectorObjectRotation(
                sectorAssetPath,
                rotatedObject.Header.ObjectId,
                rotation
            );

            await Assert.That(rotateChange).IsNotNull();
            await Assert.That(rotateChange!.Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(1);

            var updatedWorkspace = session.BeginChangeGroup("Rotate sector object").ApplyPendingChanges();
            var updatedSector = updatedWorkspace.FindSector(sectorAssetPath);

            await Assert.That(updatedSector).IsNotNull();

            var updatedObject = updatedSector!.Objects.Single();

            await Assert.That(updatedObject.GetProperty(ObjectField.ObjFPadIas1)).IsNotNull();
            await Assert.That(updatedObject.GetProperty(ObjectField.ObjFPadIas1)!.GetFloat()).IsEqualTo(rotation);

            var preview = updatedWorkspace.CreateMapScenePreview("map01");
            var sectorPreview = preview.Sectors.Single();
            var objectPreview = sectorPreview.Objects.Single();

            await Assert.That(objectPreview.ObjectId).IsEqualTo(rotatedObject.Header.ObjectId);
            await Assert.That(objectPreview.Location).IsEqualTo(new Location(6, 7));
            await Assert.That(objectPreview.Rotation).IsEqualTo(rotation);

            var undoHistory = session.GetUndoHistory();

            await Assert.That(undoHistory.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Label).IsEqualTo("Rotate sector object");
            await Assert.That(undoHistory[0].Changes.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Changes[0].Kind).IsEqualTo(EditorSessionChangeKind.Sector);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SetSectorObjectRotationPitch_StagesUpdatedRotation_AndRefreshesPreview()
    {
        const int protoNumber = 1001;
        const ulong sectorKey = 101334386392UL;
        const float rotationPitch = 37.5f;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            ProtoFormat.WriteToFile(MakeProto(protoNumber), Path.Combine(contentDir, "proto", "001001 - Test.pro"));

            var rotatedObject = new MobDataBuilder(MakePc(protoNumber)).WithLocation(6, 7).Build();
            SectorFormat.WriteToFile(
                MakeSector(rotatedObject),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();

            var rotateChange = session.SetSectorObjectRotationPitch(
                sectorAssetPath,
                rotatedObject.Header.ObjectId,
                rotationPitch
            );

            await Assert.That(rotateChange).IsNotNull();
            await Assert.That(rotateChange!.Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(1);

            var updatedWorkspace = session.BeginChangeGroup("Rotate sector object").ApplyPendingChanges();
            var updatedSector = updatedWorkspace.FindSector(sectorAssetPath);

            await Assert.That(updatedSector).IsNotNull();

            var updatedObject = updatedSector!.Objects.Single();

            await Assert.That(updatedObject.GetProperty(ObjectField.ObjFRotationPitch)).IsNotNull();
            await Assert
                .That(updatedObject.GetProperty(ObjectField.ObjFRotationPitch)!.GetFloat())
                .IsEqualTo(rotationPitch);

            var preview = updatedWorkspace.CreateMapScenePreview("map01");
            var sectorPreview = preview.Sectors.Single();
            var objectPreview = sectorPreview.Objects.Single();

            await Assert.That(objectPreview.ObjectId).IsEqualTo(rotatedObject.Header.ObjectId);
            await Assert.That(objectPreview.Location).IsEqualTo(new Location(6, 7));
            await Assert.That(objectPreview.RotationPitch).IsEqualTo(rotationPitch);

            var undoHistory = session.GetUndoHistory();

            await Assert.That(undoHistory.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Label).IsEqualTo("Rotate sector object");
            await Assert.That(undoHistory[0].Changes.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Changes[0].Kind).IsEqualTo(EditorSessionChangeKind.Sector);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task SectorLightingAndTileScriptHelpers_ApplyPendingChanges_AndRefreshPreview()
    {
        const ulong sectorKey = 101334386391UL;
        const uint replacementLightArtId = 0x00445566u;
        var sectorAssetPath = $"maps/map01/{sectorKey}.sec";

        var originalLight = MakeSectorLight(tileX: 1, tileY: 2, artId: 0x00112233u, offsetX: 3, offsetY: 4);
        var addedLight = MakeSectorLight(tileX: 8, tileY: 9, artId: 0x00778899u, offsetX: 1, offsetY: 2);
        var replacementLight = originalLight with
        {
            TileLoc = 5L | ((long)6 << 32),
            OffsetX = 7,
            OffsetY = 8,
            ArtId = replacementLightArtId,
            Flags = SectorLightFlags.Off,
            R = 9,
            G = 10,
            B = 11,
            TintColor = 12u,
            Palette = 13,
        };
        var originalTileScript = MakeTileScript(tileId: 65u, scriptId: 77);
        var addedTileScript = MakeTileScript(tileId: 193u, scriptId: 99, nodeFlags: 1u, scriptFlags: 4u);
        var replacementTileScript = originalTileScript with
        {
            TileId = 130u,
            ScriptFlags = 2u,
            ScriptCounters = 3u,
            ScriptNum = 88,
        };

        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            SectorFormat.WriteToFile(
                new SectorBuilder(MakeSector()).AddLight(originalLight).AddTileScript(originalTileScript).Build(),
                Path.Combine(contentDir, "maps", "map01", $"{sectorKey}.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();

            var addLightChange = session.AddSectorLight(sectorAssetPath, addedLight);
            var replaceLightChange = session.ReplaceSectorLight(sectorAssetPath, 0, replacementLight);
            var removeLightChange = session.RemoveSectorLight(sectorAssetPath, 1);
            var addTileScriptChange = session.AddSectorTileScript(sectorAssetPath, addedTileScript);
            var replaceTileScriptChange = session.ReplaceSectorTileScript(sectorAssetPath, 0, replacementTileScript);
            var removeTileScriptChange = session.RemoveSectorTileScript(sectorAssetPath, 1);

            await Assert.That(addLightChange.Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(replaceLightChange).IsNotNull();
            await Assert.That(removeLightChange.Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(addTileScriptChange.Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(replaceTileScriptChange).IsNotNull();
            await Assert.That(removeTileScriptChange.Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(session.GetPendingChanges().Count).IsEqualTo(1);

            var summary = session.GetPendingChangeSummary();

            await Assert.That(summary.HasChanges).IsTrue();
            await Assert.That(summary.TotalChangeCount).IsEqualTo(1);
            await Assert.That(summary.Groups.Count).IsEqualTo(1);
            await Assert.That(summary.Groups[0].Kind).IsEqualTo(EditorSessionChangeKind.Sector);
            await Assert.That(summary.Groups[0].Targets.Single()).IsEqualTo(sectorAssetPath);
            await Assert.That(summary.BlockingValidation.HasIssues).IsFalse();

            var updatedWorkspace = session.BeginChangeGroup("Edit sector lighting and scripts").ApplyPendingChanges();
            var updatedSector = updatedWorkspace.FindSector(sectorAssetPath);

            await Assert.That(updatedSector).IsNotNull();
            await Assert.That(updatedSector!.Lights.Count).IsEqualTo(1);
            await Assert.That(updatedSector.Lights[0]).IsEqualTo(replacementLight);
            await Assert.That(updatedSector.TileScripts.Count).IsEqualTo(1);
            await Assert.That(updatedSector.TileScripts[0]).IsEqualTo(replacementTileScript);

            var preview = updatedWorkspace.CreateMapScenePreview("map01");
            var sectorPreview = preview.Sectors.Single();

            await Assert.That(sectorPreview.Lights.Count).IsEqualTo(1);
            await Assert.That(sectorPreview.Lights[0].TileX).IsEqualTo(5);
            await Assert.That(sectorPreview.Lights[0].TileY).IsEqualTo(6);
            await Assert.That(sectorPreview.Lights[0].ArtId).IsEqualTo(new ArtId(replacementLightArtId));
            await Assert.That(sectorPreview.TileScripts.Count).IsEqualTo(1);
            await Assert.That(sectorPreview.TileScripts[0].TileX).IsEqualTo(2);
            await Assert.That(sectorPreview.TileScripts[0].TileY).IsEqualTo(2);
            await Assert.That(sectorPreview.TileScripts[0].ScriptId).IsEqualTo(88);

            var undoHistory = session.GetUndoHistory();

            await Assert.That(undoHistory.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Label).IsEqualTo("Edit sector lighting and scripts");
            await Assert.That(undoHistory[0].Changes.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Changes[0].Kind).IsEqualTo(EditorSessionChangeKind.Sector);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task RetargetScriptReferences_SavePendingChanges_PersistsAssetsAndMarksHistoryPersisted()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "scr"));
        Directory.CreateDirectory(Path.Combine(contentDir, "proto"));
        Directory.CreateDirectory(Path.Combine(contentDir, "mob"));
        Directory.CreateDirectory(Path.Combine(contentDir, "maps", "map01"));

        try
        {
            const int sourceScriptId = 77;
            const int targetScriptId = 88;
            const int protoNumber = 1001;

            ScriptFormat.WriteToFile(MakeScriptFile("Alpha"), Path.Combine(contentDir, "scr", "00077Alpha.scr"));
            ScriptFormat.WriteToFile(MakeScriptFile("Beta"), Path.Combine(contentDir, "scr", "00088Beta.scr"));

            ProtoFormat.WriteToFile(
                WithProperties(MakeProto(protoNumber), MakeScriptProperty(sourceScriptId)),
                Path.Combine(contentDir, "proto", "001001 - Test.pro")
            );
            MobFormat.WriteToFile(
                WithProperties(MakePc(protoNumber), MakeScriptProperty(sourceScriptId)),
                Path.Combine(contentDir, "mob", "test.mob")
            );
            SectorFormat.WriteToFile(
                MakeSectorWithScriptRefs(
                    sourceScriptId,
                    WithProperties(MakePc(protoNumber), MakeScriptProperty(sourceScriptId))
                ),
                Path.Combine(contentDir, "maps", "map01", "sector.sec")
            );

            var session = (await EditorWorkspaceLoader.LoadAsync(contentDir)).CreateSession();
            session.RetargetScriptReferences(sourceScriptId, targetScriptId);

            var updatedWorkspace = session.BeginChangeGroup("Persist retarget").SavePendingChanges();
            var persistedProto = ProtoFormat.ParseFile(Path.Combine(contentDir, "proto", "001001 - Test.pro"));
            var persistedMob = MobFormat.ParseFile(Path.Combine(contentDir, "mob", "test.mob"));
            var persistedSector = SectorFormat.ParseFile(Path.Combine(contentDir, "maps", "map01", "sector.sec"));
            var undoHistory = session.GetUndoHistory();

            await Assert.That(updatedWorkspace.Index.FindScriptReferences(sourceScriptId).Count).IsEqualTo(0);
            await Assert.That(GetScriptIds(persistedProto.Properties)).IsEquivalentTo([targetScriptId]);
            await Assert.That(GetScriptIds(persistedMob.Properties)).IsEquivalentTo([targetScriptId]);
            await Assert.That(persistedSector.SectorScript).IsNotNull();
            await Assert.That(persistedSector.SectorScript!.Value.ScriptId).IsEqualTo(targetScriptId);
            await Assert.That(GetScriptIds(persistedSector.Objects[0].Properties)).IsEquivalentTo([targetScriptId]);
            await Assert.That(undoHistory.Count).IsEqualTo(1);
            await Assert.That(undoHistory[0].Label).IsEqualTo("Persist retarget");
            await Assert.That(undoHistory[0].PersistedToDisk).IsTrue();
            await Assert.That(undoHistory[0].ProjectState.ActiveAssetPath).IsNull();
            await Assert.That(undoHistory[0].ProjectState.OpenAssets.Count).IsEqualTo(0);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    private static GameObjectGuid MakeProtoId(int protoNumber)
    {
        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, protoNumber);
        return new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, new Guid(bytes));
    }

    private static GameObjectScript MakeScript(int scriptId) => new(Counters: 0u, Flags: 0, ScriptId: scriptId);

    private static ScrFile MakeScriptFile(string description = "Test script", int conditionCount = 1) =>
        new()
        {
            HeaderFlags = 0,
            HeaderCounters = 0,
            Description = description,
            Flags = 0,
            Entries = Enumerable
                .Range(0, conditionCount)
                .Select(static _ => new ScriptConditionData(
                    1,
                    default,
                    default,
                    new ScriptActionData(0, default, default),
                    new ScriptActionData(0, default, default)
                ))
                .ToArray(),
        };

    private static ObjectProperty MakeScriptProperty(params int[] scriptIds) =>
        new ObjectProperty { Field = ObjectField.ObjFScriptsIdx, RawBytes = [0] }.WithScriptArray([
            .. scriptIds.Select(scriptId => new ObjectPropertyScript(0u, 0u, scriptId)),
        ]);

    private static ObjectProperty MakeArtProperty(ObjectField field, uint artId) =>
        ObjectPropertyFactory.ForInt32(field, unchecked((int)artId));

    private static ObjectProperty MakeColorProperty(ObjectField field, byte r, byte g, byte b) =>
        new() { Field = field, RawBytes = [r, g, b] };

    private static MobData WithProperties(MobData mob, params ObjectProperty[] properties)
    {
        byte[] bitmap = [.. mob.Header.Bitmap];
        foreach (var property in properties)
            bitmap.SetField(property.Field, true);

        return new MobData
        {
            Header = new GameObjectHeader
            {
                Version = mob.Header.Version,
                ProtoId = mob.Header.ProtoId,
                ObjectId = mob.Header.ObjectId,
                GameObjectType = mob.Header.GameObjectType,
                PropCollectionItems = mob.Header.PropCollectionItems,
                Bitmap = bitmap,
            },
            Properties = [.. mob.Properties.Concat(properties).OrderBy(static property => property.Field)],
        };
    }

    private static ProtoData WithProperties(ProtoData proto, params ObjectProperty[] properties)
    {
        byte[] bitmap = [.. proto.Header.Bitmap];
        foreach (var property in properties)
            bitmap.SetField(property.Field, true);

        return new ProtoData
        {
            Header = new GameObjectHeader
            {
                Version = proto.Header.Version,
                ProtoId = proto.Header.ProtoId,
                ObjectId = proto.Header.ObjectId,
                GameObjectType = proto.Header.GameObjectType,
                PropCollectionItems = proto.Header.PropCollectionItems,
                Bitmap = bitmap,
            },
            Properties = [.. proto.Properties.Concat(properties).OrderBy(static property => property.Field)],
        };
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
            SoundList = SectorSoundList.Default,
            BlockMask = new uint[128],
            Objects = objects,
        };

    private static EditorMapObjectPreview CreateSceneObjectHit(MobData mob, int tileX, int tileY) =>
        new()
        {
            ObjectId = mob.Header.ObjectId,
            ProtoId = mob.Header.ProtoId,
            ObjectType = mob.Header.GameObjectType,
            CurrentArtId = new ArtId(0),
            Location = new Location(checked((short)tileX), checked((short)tileY)),
            RotationPitch = 0f,
        };

    private static SectorLight MakeSectorLight(
        int tileX,
        int tileY,
        uint artId = 0,
        int offsetX = 0,
        int offsetY = 0,
        SectorLightFlags flags = SectorLightFlags.None,
        byte red = 0,
        byte green = 0,
        byte blue = 0,
        uint tintColor = 0,
        int palette = 0
    ) =>
        new()
        {
            ObjHandle = -1,
            TileLoc = (long)(uint)tileX | ((long)tileY << 32),
            OffsetX = offsetX,
            OffsetY = offsetY,
            Flags = flags,
            ArtId = artId,
            R = red,
            B = blue,
            G = green,
            TintColor = tintColor,
            Palette = palette,
            Padding2C = 0,
        };

    private static TileScript MakeTileScript(
        uint tileId,
        int scriptId,
        uint nodeFlags = 0,
        uint scriptFlags = 0,
        uint scriptCounters = 0
    ) =>
        new()
        {
            NodeFlags = nodeFlags,
            TileId = tileId,
            ScriptFlags = scriptFlags,
            ScriptCounters = scriptCounters,
            ScriptNum = scriptId,
        };

    private static Sector MakeSectorWithScriptRefs(int scriptId, params MobData[] objects) =>
        new()
        {
            Lights = [],
            Tiles = new uint[4096],
            HasRoofs = false,
            Roofs = null,
            SectorScript = MakeScript(scriptId),
            TileScripts =
            [
                new TileScript
                {
                    NodeFlags = 0,
                    TileId = 7,
                    ScriptFlags = 0,
                    ScriptCounters = 0,
                    ScriptNum = scriptId,
                },
            ],
            TownmapInfo = 0,
            AptitudeAdjustment = 0,
            LightSchemeIdx = 0,
            SoundList = SectorSoundList.Default,
            BlockMask = new uint[128],
            Objects = objects,
        };

    private static Sector MakeSectorWithArtRefs(uint artId, params MobData[] objects)
    {
        var tiles = new uint[4096];
        tiles[7] = artId;

        var roofs = new uint[256];
        roofs[3] = artId;

        return new Sector
        {
            Lights =
            [
                new SectorLight
                {
                    ObjHandle = -1,
                    TileLoc = 0,
                    OffsetX = 0,
                    OffsetY = 0,
                    Flags = 0,
                    ArtId = artId,
                    R = 0,
                    B = 0,
                    G = 0,
                    TintColor = 0,
                    Palette = 0,
                    Padding2C = 0,
                },
            ],
            Tiles = tiles,
            HasRoofs = true,
            Roofs = roofs,
            SectorScript = null,
            TileScripts = [],
            TownmapInfo = 0,
            AptitudeAdjustment = 0,
            LightSchemeIdx = 0,
            SoundList = SectorSoundList.Default,
            BlockMask = new uint[128],
            Objects = objects,
        };
    }

    private static EditorWorkspace CloneWorkspaceWithSave(EditorWorkspace workspace, LoadedSave save) =>
        new()
        {
            ContentDirectory = workspace.ContentDirectory,
            GameDirectory = workspace.GameDirectory,
            InstallationType = workspace.InstallationType,
            GameData = workspace.GameData,
            Assets = workspace.Assets,
            AudioAssets = workspace.AudioAssets,
            Index = workspace.Index,
            LoadReport = workspace.LoadReport,
            Validation = workspace.Validation,
            Save = save,
            SaveFolder = Path.Combine("game-root", "save"),
            SaveSlotName = "slot0001",
        };

    private static ArtFile CreateArtFile(int width, int height, byte[] pixels, uint frameRate = 8, uint actionFrame = 0)
    {
        ArtPaletteEntry[] CreatePalette(params (byte Blue, byte Green, byte Red)[] colors)
        {
            var palette = new ArtPaletteEntry[256];
            for (var index = 0; index < colors.Length; index++)
                palette[index + 1] = new(colors[index].Blue, colors[index].Green, colors[index].Red);

            return palette;
        }

        ArtPaletteEntry[]?[] palettes = [CreatePalette((0, 0, 255)), null, null, null];
        var paletteIds = new int[4];
        for (var index = 0; index < palettes.Length; index++)
            paletteIds[index] = palettes[index] is null ? 0 : index + 1;

        return new ArtFile
        {
            Flags = ArtFlags.Static,
            FrameRate = frameRate,
            ActionFrame = actionFrame,
            FrameCount = 1,
            DataSizes = new uint[8],
            PaletteData1 = new uint[8],
            PaletteData2 = new uint[8],
            PaletteIds = paletteIds,
            Palettes = palettes,
            Frames =
            [
                [
                    new ArtFrame
                    {
                        Header = new ArtFrameHeader((uint)width, (uint)height, (uint)pixels.Length, 0, 0, 0, 0),
                        Pixels = pixels,
                    },
                ],
            ],
        };
    }

    private sealed class TestSpriteSource : IEditorMapRenderSpriteSource
    {
        public List<ArtId> ResolvedArtIds { get; } = [];

        public EditorMapRenderSprite? Resolve(ArtId artId, EditorMapRenderSpriteRequest? request = null)
        {
            if (artId.Value == 0)
                return null;

            ResolvedArtIds.Add(artId);
            return new EditorMapRenderSprite
            {
                ArtId = artId,
                AssetPath = $"art/{artId.Value}.art",
                RotationIndex = request?.RotationIndex ?? 0,
                FrameIndex = request?.FrameIndex ?? 0,
                Width = 1,
                Height = 1,
                Stride = 4,
                CenterX = 0,
                CenterY = 0,
                FrameRate = 1,
                PixelFormat = EditorArtPreviewPixelFormat.Bgra32,
                PixelData = [0x10, 0x20, 0x30, 0xFF],
            };
        }
    }

    private static int[] GetScriptIds(IReadOnlyList<ObjectProperty> properties) =>
        properties
            .Where(static property => property.Field == ObjectField.ObjFScriptsIdx)
            .SelectMany(static property => property.GetScriptArray())
            .Select(static script => script.ScriptId)
            .ToArray();

    private static int[] GetInt32Array(IReadOnlyList<ObjectProperty> properties, ObjectField field) =>
        properties.Single(property => property.Field == field).GetInt32Array();

    private static Color GetColor(IReadOnlyList<ObjectProperty> properties, ObjectField field)
    {
        var bytes = properties.Single(property => property.Field == field).RawBytes;
        return new Color(bytes[0], bytes[1], bytes[2]);
    }

    private static uint[] GetArtIds(IReadOnlyList<ObjectProperty> properties) =>
        properties
            .Where(static property =>
                property.Field
                    is ObjectField.ObjFCurrentAid
                        or ObjectField.ObjFShadow
                        or ObjectField.ObjFLightAid
                        or ObjectField.ObjFAid
                        or ObjectField.ObjFDestroyedAid
            )
            .Select(static property => unchecked((uint)property.GetInt32()))
            .ToArray();

    private static LoadedSave CreateLoadedSave(SaveInfo info) =>
        new()
        {
            Info = info,
            Index = new SaveIndex { Root = [] },
            Files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase),
            RawFiles = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase),
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

    private static LoadedSave CreateLoadedSaveWithMob(LoadedSave baseSave, string mobPath, MobData mob)
    {
        var mobBytes = MobFormat.WriteToArray(mob);
        var files = new Dictionary<string, byte[]>(baseSave.Files, StringComparer.OrdinalIgnoreCase)
        {
            [mobPath] = mobBytes,
        };
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
                                    Children =
                                    [
                                        new TfaiFileEntry { Name = Path.GetFileName(mobPath), Size = mobBytes.Length },
                                    ],
                                },
                                new TfaiFileEntry { Name = "map.jmp", Size = jmpBytes.Length },
                            ],
                        },
                    ],
                },
            ],
        };

        return SaveGameLoader.LoadFromParsed(baseSave.Info, index, TfafFormat.Pack(index, files));
    }
}
