using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Tests;

public sealed class LogbookEditorServiceTests
{
    [Test]
    public async Task LoadCatalog_WhenBackendReturnsEntries_ExposesCatalogSnapshot()
    {
        var service = new LogbookEditorService(
            new FakeLogbookEditorBackend
            {
                CatalogEntries =
                [
                    new LogbookCatalogEntrySnapshot("quest", 42, 0, "Find the bridge", "Quest 42"),
                    new LogbookCatalogEntrySnapshot("background", 4, 1004, "Raised by Scholars", "Text 1004"),
                ],
            }
        );

        var snapshot = await service.LoadCatalogAsync(new LogbookEditorCatalogRequest(CreateSession()));

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Status).IsEqualTo("Logbook catalog loaded");
        await Assert.That(snapshot.Entries.Count).IsEqualTo(2);
        await Assert.That(snapshot.Entries.Any(static entry => entry.CategoryToken == "background")).IsTrue();
    }

    [Test]
    public async Task Write_SetQuestState_ParsesStateTokenAndUsesResolvedPlayerHandle()
    {
        var backend = CreateBackendWithPlayer();
        var service = new LogbookEditorService(backend);

        var snapshot = await service.WriteAsync(
            new LogbookMutationRequest(
                CreateSession(),
                "player",
                LogbookMutationKind.SetQuestState,
                "42",
                "accepted",
                string.Empty,
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Status).IsEqualTo("Quest State applied");
        await Assert.That(snapshot.ValueText).IsEqualTo("Accepted");
        await Assert.That(snapshot.TargetHandleText).IsEqualTo("0x0000000201234567");
        await Assert.That(backend.LastQuestStateCall).IsNotNull();
        await Assert.That(backend.LastQuestStateCall!.Value.Handle).IsEqualTo(0x0000000201234567UL);
        await Assert.That(backend.LastQuestStateCall!.Value.QuestId).IsEqualTo(42);
        await Assert.That(backend.LastQuestStateCall!.Value.State).IsEqualTo(2);
    }

    [Test]
    public async Task Write_SetBackground_UsesCatalogAuxiliaryIdWhenTextIdIsBlank()
    {
        var backend = CreateBackendWithPlayer();
        backend.CatalogEntries =
        [
            new LogbookCatalogEntrySnapshot("background", 4, 1004, "Raised by Scholars", "Text 1004"),
        ];
        var service = new LogbookEditorService(backend);

        var snapshot = await service.WriteAsync(
            new LogbookMutationRequest(
                CreateSession(),
                "player",
                LogbookMutationKind.SetBackground,
                "4",
                string.Empty,
                string.Empty,
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.AuxiliaryText).IsEqualTo("Text 1004");
        await Assert.That(backend.LastBackgroundCall).IsNotNull();
        await Assert.That(backend.LastBackgroundCall!.Value.BackgroundId).IsEqualTo(4);
        await Assert.That(backend.LastBackgroundCall!.Value.BackgroundTextId).IsEqualTo(1004);
    }

    [Test]
    public async Task Write_SetBackground_WhenManualTextIdOverrideIsProvided_UsesAndReportsThatOverride()
    {
        var backend = CreateBackendWithPlayer();
        backend.CatalogEntries =
        [
            new LogbookCatalogEntrySnapshot("background", 4, 1004, "Raised by Scholars", "Text 1004"),
        ];
        var service = new LogbookEditorService(backend);

        var snapshot = await service.WriteAsync(
            new LogbookMutationRequest(
                CreateSession(),
                "player",
                LogbookMutationKind.SetBackground,
                "4",
                string.Empty,
                "1005",
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.AuxiliaryText).IsEqualTo("Text 1005");
        await Assert.That(backend.LastBackgroundCall).IsNotNull();
        await Assert.That(backend.LastBackgroundCall!.Value.BackgroundId).IsEqualTo(4);
        await Assert.That(backend.LastBackgroundCall!.Value.BackgroundTextId).IsEqualTo(1005);
    }

    [Test]
    public async Task Write_SetBackground_WhenWorkspacePathOverrideIsProvided_UsesOverrideCatalog()
    {
        var backend = CreateBackendWithPlayer();
        backend.CatalogEntries =
        [
            new LogbookCatalogEntrySnapshot("background", 4, 1004, "Raised by Scholars", "Text 1004"),
        ];
        var service = new LogbookEditorService(backend);
        var workspacePath = @"C:\Games\Arcanum\modules\test-module";

        var snapshot = await service.WriteAsync(
            new LogbookMutationRequest(
                CreateSession() with
                {
                    Fingerprint = new RuntimeFingerprint(
                        "Arcanum",
                        4242,
                        RuntimeKind.Classic,
                        "Arcanum.exe",
                        "",
                        "0x00400000",
                        3_538_944,
                        2_048_000,
                        DateTime.UtcNow
                    ),
                },
                "player",
                LogbookMutationKind.SetBackground,
                "4",
                string.Empty,
                string.Empty,
                "1000",
                workspacePath
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.AuxiliaryText).IsEqualTo("Text 1004");
        await Assert.That(backend.LastCatalogWorkspacePath).IsEqualTo(workspacePath);
        await Assert.That(backend.LastBackgroundCall).IsNotNull();
        await Assert.That(backend.LastBackgroundCall!.Value.BackgroundTextId).IsEqualTo(1004);
    }

    [Test]
    public async Task Write_AddInjury_UsesCatalogSourceAndParsesInjurySelector()
    {
        var backend = CreateBackendWithPlayer();
        backend.CatalogEntries = [new LogbookCatalogEntrySnapshot("injury", 30512, 0, "Bandit", "Description 30512")];
        var service = new LogbookEditorService(backend);

        var snapshot = await service.WriteAsync(
            new LogbookMutationRequest(
                CreateSession(),
                "player",
                LogbookMutationKind.AddInjury,
                "30512",
                "crippled-leg",
                string.Empty,
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Status).IsEqualTo("Add Injury History applied");
        await Assert.That(snapshot.ValueText).IsEqualTo("Crippled leg");
        await Assert.That(snapshot.SubjectText).IsEqualTo("Bandit [30512]");
        await Assert.That(backend.LastInjuryCall).IsNotNull();
        await Assert.That(backend.LastInjuryCall!.Value.Handle).IsEqualTo(0x0000000201234567UL);
        await Assert.That(backend.LastInjuryCall!.Value.DescriptionId).IsEqualTo(30512);
        await Assert.That(backend.LastInjuryCall!.Value.InjuryType).IsEqualTo(2);
    }

    [Test]
    public async Task Write_RemoveInjury_UsesCatalogSourceInjurySelectorAndHistorySlot()
    {
        var backend = CreateBackendWithPlayer();
        backend.CatalogEntries = [new LogbookCatalogEntrySnapshot("injury", 30512, 0, "Bandit", "Description 30512")];
        var service = new LogbookEditorService(backend);

        var snapshot = await service.WriteAsync(
            new LogbookMutationRequest(
                CreateSession(),
                "player",
                LogbookMutationKind.RemoveInjury,
                "30512",
                "crippled-leg",
                "67",
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Status).IsEqualTo("Remove Injury History applied");
        await Assert.That(snapshot.ValueText).IsEqualTo("Crippled leg");
        await Assert.That(snapshot.SubjectText).IsEqualTo("Bandit [30512]");
        await Assert.That(snapshot.AuxiliaryText).IsEqualTo("Slot 67");
        await Assert.That(backend.LastRemoveInjuryCall).IsNotNull();
        await Assert.That(backend.LastRemoveInjuryCall!.Value.Handle).IsEqualTo(0x0000000201234567UL);
        await Assert.That(backend.LastRemoveInjuryCall!.Value.DescriptionId).IsEqualTo(30512);
        await Assert.That(backend.LastRemoveInjuryCall!.Value.InjuryType).IsEqualTo(2);
        await Assert.That(backend.LastRemoveInjuryCall!.Value.SlotIndex).IsEqualTo(67);
    }

    [Test]
    public async Task Write_AddKill_UsesResolvedVictimHandleAndPlayerTarget()
    {
        const ulong victimHandle = 0x00000002089ABCDE;
        var backend = CreateBackendWithPlayer();
        backend.SetInspection(
            victimHandle,
            CreateIdentity(
                victimHandle,
                objectTypeName: "Npc",
                objectIdLabel: "mob:victim-a",
                prototypeLabel: "proto#24001"
            )
        );
        var service = new LogbookEditorService(backend);

        var snapshot = await service.WriteAsync(
            new LogbookMutationRequest(
                CreateSession(),
                "player",
                LogbookMutationKind.AddKill,
                string.Empty,
                string.Empty,
                Handle(victimHandle),
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Status).IsEqualTo("Record Kill applied");
        await Assert.That(snapshot.SubjectText).IsEqualTo("Npc mob:victim-a from proto#24001");
        await Assert.That(snapshot.AuxiliaryText).IsEqualTo(Handle(victimHandle));
        await Assert.That(backend.LastKillCall).IsNotNull();
        await Assert.That(backend.LastKillCall!.Value.Handle).IsEqualTo(0x0000000201234567UL);
        await Assert.That(backend.LastKillCall!.Value.VictimHandle).IsEqualTo(victimHandle);
    }

    [Test]
    public async Task Write_AddKill_WhenVictimHandleIsBlank_ReturnsUnavailableSnapshot()
    {
        var backend = CreateBackendWithPlayer();
        var service = new LogbookEditorService(backend);

        var snapshot = await service.WriteAsync(
            new LogbookMutationRequest(
                CreateSession(),
                "player",
                LogbookMutationKind.AddKill,
                string.Empty,
                string.Empty,
                string.Empty,
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Status).IsEqualTo("Invalid logbook edit request");
        await Assert.That(snapshot.Summary).Contains("Record Kill requires one live victim handle");
    }

    [Test]
    public async Task Write_SetTotalKills_UsesResolvedPlayerHandleAndNumericValue()
    {
        var backend = CreateBackendWithPlayer();
        var service = new LogbookEditorService(backend);

        var snapshot = await service.WriteAsync(
            new LogbookMutationRequest(
                CreateSession(),
                "player",
                LogbookMutationKind.SetTotalKills,
                string.Empty,
                "18",
                string.Empty,
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Status).IsEqualTo("Set Total Kills applied");
        await Assert.That(snapshot.SubjectText).IsEqualTo("Total Kills");
        await Assert.That(snapshot.ValueText).IsEqualTo("18");
        await Assert.That(backend.LastKillSummaryCall).IsNotNull();
        await Assert.That(backend.LastKillSummaryCall!.Value.Kind).IsEqualTo(LogbookMutationKind.SetTotalKills);
        await Assert.That(backend.LastKillSummaryCall!.Value.Handle).IsEqualTo(0x0000000201234567UL);
        await Assert.That(backend.LastKillSummaryCall!.Value.DescriptionId).IsEqualTo(0);
        await Assert.That(backend.LastKillSummaryCall!.Value.Value).IsEqualTo(18);
    }

    [Test]
    public async Task Write_SetMostPowerfulKill_UsesDescriptionCatalogEntryAndNumericLevel()
    {
        var backend = CreateBackendWithPlayer();
        backend.CatalogEntries =
        [
            new LogbookCatalogEntrySnapshot("description", 30512, 0, "Bandit", "Description 30512"),
        ];
        var service = new LogbookEditorService(backend);

        var snapshot = await service.WriteAsync(
            new LogbookMutationRequest(
                CreateSession(),
                "player",
                LogbookMutationKind.SetMostPowerfulKill,
                "30512",
                "12",
                string.Empty,
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Status).IsEqualTo("Set Most Powerful Kill applied");
        await Assert.That(snapshot.SubjectText).IsEqualTo("Bandit [30512]");
        await Assert.That(snapshot.ValueText).IsEqualTo("Level 12");
        await Assert.That(backend.LastKillSummaryCall).IsNotNull();
        await Assert.That(backend.LastKillSummaryCall!.Value.Kind).IsEqualTo(LogbookMutationKind.SetMostPowerfulKill);
        await Assert.That(backend.LastKillSummaryCall!.Value.DescriptionId).IsEqualTo(30512);
        await Assert.That(backend.LastKillSummaryCall!.Value.Value).IsEqualTo(12);
    }

    [Test]
    public async Task Write_SetMostTechKill_WhenValueIsInvalid_ReturnsUnavailableSnapshot()
    {
        var backend = CreateBackendWithPlayer();
        var service = new LogbookEditorService(backend);

        var snapshot = await service.WriteAsync(
            new LogbookMutationRequest(
                CreateSession(),
                "player",
                LogbookMutationKind.SetMostTechKill,
                "30512",
                "-1",
                string.Empty,
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Status).IsEqualTo("Invalid logbook edit request");
        await Assert.That(snapshot.Summary).Contains("Tech aptitude");
    }

    [Test]
    public async Task Write_AddKey_UsesCatalogEntryAndResolvedPlayerHandle()
    {
        var backend = CreateBackendWithPlayer();
        backend.CatalogEntries = [new LogbookCatalogEntrySnapshot("key", 55, 0, "Tarant Sewer Key", "Key 55")];
        var service = new LogbookEditorService(backend);

        var snapshot = await service.WriteAsync(
            new LogbookMutationRequest(
                CreateSession(),
                "player",
                LogbookMutationKind.AddKey,
                "55",
                string.Empty,
                string.Empty,
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Status).IsEqualTo("Add Key applied");
        await Assert.That(snapshot.SubjectText).IsEqualTo("Tarant Sewer Key [55]");
        await Assert.That(backend.LastKeyCall).IsNotNull();
        await Assert.That(backend.LastKeyCall!.Value.Handle).IsEqualTo(0x0000000201234567UL);
        await Assert.That(backend.LastKeyCall!.Value.KeyId).IsEqualTo(55);
        await Assert.That(backend.LastKeyCall!.Value.Add).IsTrue();
    }

    [Test]
    public async Task Write_RemoveKey_UsesCatalogEntryAndResolvedPlayerHandle()
    {
        var backend = CreateBackendWithPlayer();
        backend.CatalogEntries = [new LogbookCatalogEntrySnapshot("key", 77, 0, "Black Root Cellar Key", "Key 77")];
        var service = new LogbookEditorService(backend);

        var snapshot = await service.WriteAsync(
            new LogbookMutationRequest(
                CreateSession(),
                "player",
                LogbookMutationKind.RemoveKey,
                "77",
                string.Empty,
                string.Empty,
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.Status).IsEqualTo("Remove Key applied");
        await Assert.That(snapshot.SubjectText).IsEqualTo("Black Root Cellar Key [77]");
        await Assert.That(backend.LastKeyCall).IsNotNull();
        await Assert.That(backend.LastKeyCall!.Value.Handle).IsEqualTo(0x0000000201234567UL);
        await Assert.That(backend.LastKeyCall!.Value.KeyId).IsEqualTo(77);
        await Assert.That(backend.LastKeyCall!.Value.Add).IsFalse();
    }

    [Test]
    public async Task Write_SetQuestState_WhenBotchedVariantIsRequested_UsesRawPcQuestState()
    {
        var backend = CreateBackendWithPlayer();
        var service = new LogbookEditorService(backend);

        var snapshot = await service.WriteAsync(
            new LogbookMutationRequest(
                CreateSession(),
                "player",
                LogbookMutationKind.SetQuestState,
                "42",
                "accepted-botched",
                string.Empty,
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsTrue();
        await Assert.That(snapshot.ValueText).IsEqualTo("Accepted [Botched]");
        await Assert.That(backend.LastQuestStateCall).IsNotNull();
        await Assert
            .That(backend.LastQuestStateCall!.Value.State)
            .IsEqualTo(RuntimeWatchValueCatalog.QuestBotchedModifier | 2);
    }

    [Test]
    public async Task Write_WhenSessionCannotInvokeFunctions_ReturnsUnavailableSnapshot()
    {
        var service = new LogbookEditorService(new FakeLogbookEditorBackend());

        var snapshot = await service.WriteAsync(
            new LogbookMutationRequest(
                CreateSession() with
                {
                    Capabilities = new RuntimeCapabilityReport(
                        RuntimeSupportLevel.Exploratory,
                        DiagnosticsCapability.ReadMemory,
                        []
                    ),
                },
                "player",
                LogbookMutationKind.AddReputation,
                "12",
                string.Empty,
                string.Empty,
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Status).IsEqualTo("Logbook editor unavailable");
    }

    [Test]
    public async Task Write_SetQuestGlobalState_WhenBotchedVariantIsRequested_ReturnsUnavailableSnapshot()
    {
        var backend = CreateBackendWithPlayer();
        var service = new LogbookEditorService(backend);

        var snapshot = await service.WriteAsync(
            new LogbookMutationRequest(
                CreateSession(),
                "player",
                LogbookMutationKind.SetQuestGlobalState,
                "42",
                "accepted-botched",
                string.Empty,
                "1000"
            )
        );

        await Assert.That(snapshot.IsAvailable).IsFalse();
        await Assert.That(snapshot.Status).IsEqualTo("Invalid logbook edit request");
        await Assert
            .That(snapshot.Summary)
            .Contains("Global quest state does not support explicit botched-base variants");
    }

    private static AttachedSessionSnapshot CreateSession() =>
        new(
            DateTimeOffset.UtcNow,
            SessionOrigin.Attach,
            "Arcanum.exe (PID 4242)",
            "Attached live session",
            @"C:\Games\Arcanum\Arcanum.exe @ 0x00400000",
            "Arcanum",
            4242,
            HasExited: false,
            new RuntimeFingerprint(
                "Arcanum",
                4242,
                RuntimeKind.Classic,
                "Arcanum.exe",
                @"C:\Games\Arcanum\Arcanum.exe",
                "0x00400000",
                3_538_944,
                2_048_000,
                DateTime.UtcNow
            ),
            new RuntimeProfileSnapshot(
                "validated-classic",
                "Arcanum.exe validated runtime profile",
                RuntimeKind.Classic,
                RuntimeSupportLevel.Validated,
                SupportsCatalogRvas: true,
                "Validated classic profile.",
                ModuleSha256: null,
                HashError: null
            ),
            new RuntimeCapabilityReport(
                RuntimeSupportLevel.Validated,
                DiagnosticsCapability.ReadMemory
                    | DiagnosticsCapability.ResolveRuntimeProfile
                    | DiagnosticsCapability.ReadStructuredState
                    | DiagnosticsCapability.InvokeFunctions,
                []
            ),
            LaunchPreview: null,
            Notes: []
        );

    private static FakeLogbookEditorBackend CreateBackendWithPlayer()
    {
        var backend = new FakeLogbookEditorBackend
        {
            PlayerResolution = new LivePlayerLocatorResult(
                0x0000000201234567,
                "SingleLivePcInstance",
                "Resolved one player.",
                [],
                [],
                []
            ),
        };
        backend.SetInspection(0x0000000201234567, CreateIdentity(0x0000000201234567));
        return backend;
    }

    private static LiveObjectIdentity CreateIdentity(
        ulong handle,
        string objectTypeName = "Pc",
        string objectIdLabel = "mob:guid-a",
        string prototypeLabel = "proto#1000"
    ) =>
        new(
            Handle(handle),
            LooksLikeHandle: true,
            "PoolEntry",
            PoolIndex: 1,
            BucketIndex: 0,
            SlotIndex: 1,
            EntryAddress: "0x00001000",
            ObjectAddress: "0x00001004",
            Status: (byte)'H',
            Sequence: 1,
            ExpectedSequence: 1,
            new LiveObjectHeader(
                ObjectTypeRaw: 15,
                ObjectTypeName: objectTypeName,
                new LiveOid(2, null, "guid-a", objectIdLabel, objectIdLabel),
                new LiveOid(1, 1000, "guid-b", prototypeLabel, prototypeLabel),
                Handle(handle)
            )
        );

    private static string Handle(ulong handle) => RuntimeSemanticCatalog.FormatHandle(handle);

    private sealed class FakeLogbookEditorBackend : ILogbookEditorBackend
    {
        private readonly Dictionary<ulong, LiveObjectIdentity> _inspections = [];

        public LivePlayerLocatorResult PlayerResolution { get; init; }
        public IReadOnlyList<LogbookCatalogEntrySnapshot> CatalogEntries { get; set; } = [];
        public QuestStateCall? LastQuestStateCall { get; private set; }
        public BackgroundCall? LastBackgroundCall { get; private set; }
        public InjuryCall? LastInjuryCall { get; private set; }
        public RemoveInjuryCall? LastRemoveInjuryCall { get; private set; }
        public KillCall? LastKillCall { get; private set; }
        public KillSummaryCall? LastKillSummaryCall { get; private set; }
        public KeyCall? LastKeyCall { get; private set; }
        public string? LastCatalogWorkspacePath { get; private set; }

        public void SetInspection(ulong handle, LiveObjectIdentity identity) => _inspections[handle] = identity;

        public Task<IReadOnlyList<LogbookCatalogEntrySnapshot>> LoadCatalogAsync(string workspacePath)
        {
            LastCatalogWorkspacePath = workspacePath;
            return Task.FromResult(CatalogEntries);
        }

        public LivePlayerLocatorResult LocatePlayers(int processId) => PlayerResolution;

        public LiveObjectIdentity InspectHandle(int processId, ulong handle) => _inspections[handle];

        public LogbookMutationExecutionResult SetQuestState(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int questId,
            int state,
            TimeSpan timeout
        )
        {
            LastQuestStateCall = new QuestStateCall(handle, questId, state);
            return new LogbookMutationExecutionResult(
                "dispatcher-mode",
                "dispatcher-site",
                "quest_state_set",
                "quest updated"
            );
        }

        public LogbookMutationExecutionResult SetBackground(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int backgroundId,
            int backgroundTextId,
            TimeSpan timeout
        )
        {
            LastBackgroundCall = new BackgroundCall(handle, backgroundId, backgroundTextId);
            return new LogbookMutationExecutionResult(
                "dispatcher-mode",
                "dispatcher-site",
                "background_set",
                "background updated"
            );
        }

        public LogbookMutationExecutionResult SetQuestGlobalState(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            int questId,
            int state,
            TimeSpan timeout
        ) => new("dispatcher-mode", "dispatcher-site", "quest_global_state_set", "quest global updated");

        public LogbookMutationExecutionResult SetRumorKnown(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int rumorId,
            TimeSpan timeout
        ) => new("dispatcher-mode", "dispatcher-site", "rumor_known_set", "rumor updated");

        public LogbookMutationExecutionResult QuellRumor(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            int rumorId,
            TimeSpan timeout
        ) => new("dispatcher-mode", "dispatcher-site", "rumor_qstate_set", "rumor quelled");

        public LogbookMutationExecutionResult AddReputation(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int reputationId,
            TimeSpan timeout
        ) => new("dispatcher-mode", "dispatcher-site", "reputation_add", "reputation updated");

        public LogbookMutationExecutionResult RemoveReputation(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int reputationId,
            TimeSpan timeout
        ) => new("dispatcher-mode", "dispatcher-site", "reputation_remove", "reputation updated");

        public LogbookMutationExecutionResult AddBlessing(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int blessingId,
            TimeSpan timeout
        ) => new("dispatcher-mode", "dispatcher-site", "bless_add", "blessing updated");

        public LogbookMutationExecutionResult RemoveBlessing(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int blessingId,
            TimeSpan timeout
        ) => new("dispatcher-mode", "dispatcher-site", "bless_remove", "blessing updated");

        public LogbookMutationExecutionResult AddCurse(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int curseId,
            TimeSpan timeout
        ) => new("dispatcher-mode", "dispatcher-site", "curse_add", "curse updated");

        public LogbookMutationExecutionResult RemoveCurse(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int curseId,
            TimeSpan timeout
        ) => new("dispatcher-mode", "dispatcher-site", "curse_remove", "curse updated");

        public LogbookMutationExecutionResult AddKey(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int keyId,
            TimeSpan timeout
        )
        {
            LastKeyCall = new KeyCall(handle, keyId, Add: true);
            return new("dispatcher-mode", "dispatcher-site", "key_add", "key updated");
        }

        public LogbookMutationExecutionResult RemoveKey(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int keyId,
            TimeSpan timeout
        )
        {
            LastKeyCall = new KeyCall(handle, keyId, Add: false);
            return new("dispatcher-mode", "dispatcher-site", "key_remove", "key updated");
        }

        public LogbookMutationExecutionResult AddInjury(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int descriptionId,
            int injuryType,
            TimeSpan timeout
        )
        {
            LastInjuryCall = new InjuryCall(handle, descriptionId, injuryType);
            return new("dispatcher-mode", "dispatcher-site", "injury_add", "injury updated");
        }

        public LogbookMutationExecutionResult RemoveInjury(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            int descriptionId,
            int injuryType,
            int slotIndex,
            TimeSpan timeout
        )
        {
            LastRemoveInjuryCall = new RemoveInjuryCall(handle, descriptionId, injuryType, slotIndex);
            return new("dispatcher-mode", "dispatcher-site", "injury_remove", "injury updated");
        }

        public LogbookMutationExecutionResult AddKill(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            ulong victimHandle,
            TimeSpan timeout
        )
        {
            LastKillCall = new KillCall(handle, victimHandle);
            return new("dispatcher-mode", "dispatcher-site", "logbook_add_kill", "kill updated");
        }

        public LogbookMutationExecutionResult SetKillSummary(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            LogbookMutationKind kind,
            int descriptionId,
            int value,
            TimeSpan timeout
        )
        {
            LastKillSummaryCall = new KillSummaryCall(handle, kind, descriptionId, value);
            return new("dispatcher-mode", "dispatcher-site", "kill_summary_set", "kill summary updated");
        }

        public LogbookMutationExecutionResult ClearBackground(
            int processId,
            RuntimeProfileSnapshot runtimeProfile,
            ulong handle,
            TimeSpan timeout
        ) => new("dispatcher-mode", "dispatcher-site", "background_clear", "background cleared");
    }

    private readonly record struct QuestStateCall(ulong Handle, int QuestId, int State);

    private readonly record struct BackgroundCall(ulong Handle, int BackgroundId, int BackgroundTextId);

    private readonly record struct KeyCall(ulong Handle, int KeyId, bool Add);

    private readonly record struct InjuryCall(ulong Handle, int DescriptionId, int InjuryType);

    private readonly record struct RemoveInjuryCall(ulong Handle, int DescriptionId, int InjuryType, int SlotIndex);

    private readonly record struct KillCall(ulong Handle, ulong VictimHandle);

    private readonly record struct KillSummaryCall(
        ulong Handle,
        LogbookMutationKind Kind,
        int DescriptionId,
        int Value
    );
}
