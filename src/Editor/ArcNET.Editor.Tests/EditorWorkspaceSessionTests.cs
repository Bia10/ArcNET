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
            await Assert.That(session.GetRedoHistory().Count).IsEqualTo(0);

            session.Undo();

            var redoHistory = session.GetRedoHistory();

            await Assert.That(redoHistory.Count).IsEqualTo(1);
            await Assert.That(redoHistory[0].Label).IsEqualTo("Guard touch-up");
            await Assert.That(redoHistory[0].PersistedToDisk).IsFalse();
            await Assert.That(redoHistory[0].Changes.Count).IsEqualTo(2);
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

    private static int[] GetScriptIds(IReadOnlyList<ObjectProperty> properties) =>
        properties
            .Where(static property => property.Field == ObjectField.ObjFScriptsIdx)
            .SelectMany(static property => property.GetScriptArray())
            .Select(static script => script.ScriptId)
            .ToArray();

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
}
