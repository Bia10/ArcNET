using System.Linq;
using ArcanumDebugger.App.ViewModels;
using ArcNET.Diagnostics;

namespace ArcanumDebugger.App.Tests;

public sealed class LogbookLiveEntryCatalogTests
{
    [Test]
    public async Task BuildEntries_WhenQuestUsesBotchedVariant_ReturnsQuestPrefillWithCanonicalStateToken()
    {
        var snapshot = CreateSnapshot(
            new LogbookPayload(
                RumorsAndNotes: null,
                Quests: new QuestLogbookPageSnapshot(
                    Intelligence: 12,
                    UsesDumbText: false,
                    [
                        new QuestLogbookEntrySnapshot(
                            42,
                            new GameDateTimeSnapshot(15, 2500),
                            RuntimeWatchValueCatalog.QuestBotchedModifier | 2,
                            "Accepted [Botched]",
                            "Find the bridge",
                            "Cross the river.",
                            "Cross the river.",
                            null
                        ),
                    ],
                    CreateNativeRead()
                ),
                Reputations: null,
                BlessingsAndCurses: null,
                KillsAndInjuries: null,
                Background: null,
                KeyringContents: null
            )
        );

        var entries = LogbookLiveEntryCatalog.BuildEntries(snapshot);
        var questEntry = entries.Single(entry => entry.MutationKind == LogbookMutationKind.SetQuestState);
        var globalEntry = entries.Single(entry => entry.MutationKind == LogbookMutationKind.SetQuestGlobalState);

        await Assert.That(questEntry.MutationKind).IsEqualTo(LogbookMutationKind.SetQuestState);
        await Assert.That(questEntry.EntryText).IsEqualTo("42");
        await Assert.That(questEntry.ValueText).IsEqualTo("accepted-botched");
        await Assert.That(questEntry.CurrentValueText).IsEqualTo("Accepted [Botched]");
        await Assert.That(globalEntry.EntryText).IsEqualTo("42");
        await Assert.That(globalEntry.ValueText).IsEqualTo(string.Empty);
        await Assert.That(globalEntry.DisplayName).Contains("Global");
        await Assert.That(globalEntry.DetailText).Contains("Accepted [Botched]");
    }

    [Test]
    public async Task BuildEntries_WhenPayloadContainsMutableLiveEntries_ReturnsPrefillsForRemoveAndCloneFlows()
    {
        var snapshot = CreateSnapshot(
            new LogbookPayload(
                new RumorLogbookPageSnapshot(
                    Intelligence: 12,
                    UsesDumbText: false,
                    [
                        new RumorLogbookEntrySnapshot(
                            7,
                            new GameDateTimeSnapshot(5, 1000),
                            false,
                            "Whispers",
                            null,
                            null
                        ),
                    ],
                    CreateNativeRead()
                ),
                Quests: null,
                new ReputationLogbookPageSnapshot(
                    [new ReputationLogbookEntrySnapshot(12, new GameDateTimeSnapshot(10, 2000), "Notorious")],
                    CreateNativeRead()
                ),
                new BlessingCurseLogbookPageSnapshot(
                    [
                        new BlessingCurseLogbookEntrySnapshot(
                            "blessing",
                            2,
                            new GameDateTimeSnapshot(12, 3000),
                            "Still Water"
                        ),
                        new BlessingCurseLogbookEntrySnapshot(
                            "curse",
                            9,
                            new GameDateTimeSnapshot(13, 3500),
                            "Black Bile"
                        ),
                    ],
                    []
                ),
                new KillsAndInjuriesLogbookPageSnapshot(
                    Summary:
                    [
                        new KillLogbookSummaryEntrySnapshot("total_kills", "Total Kills", 0, null, 9),
                        new KillLogbookSummaryEntrySnapshot("most_powerful", "Most Powerful", 30512, "Bandit", 12),
                    ],
                    Injuries:
                    [
                        new InjuryLogbookEntrySnapshot(
                            1,
                            30512,
                            "Bandit",
                            2,
                            "Crippled leg",
                            Active: true,
                            "Active",
                            "Bandit :: Crippled leg :: Active"
                        ),
                        new InjuryLogbookEntrySnapshot(
                            3,
                            41200,
                            "Ogre",
                            0,
                            "Blinded",
                            Active: false,
                            "Healed",
                            "Ogre :: Blinded :: Healed"
                        ),
                    ],
                    CreateNativeRead()
                ),
                new BackgroundLogbookPageSnapshot(
                    4,
                    1004,
                    "Raised by Scholars",
                    "Great library upbringing.",
                    "Raised by Scholars",
                    "Great library upbringing.",
                    CreateNativeRead(),
                    CreateNativeRead()
                ),
                new KeyringLogbookPageSnapshot([new KeyringLogbookEntrySnapshot(3, 55, "Tarant Sewer Key")])
            )
        );

        var entries = LogbookLiveEntryCatalog.BuildEntries(snapshot);

        await Assert.That(entries.Any(static entry => entry.MutationKind == LogbookMutationKind.QuellRumor)).IsTrue();
        await Assert
            .That(entries.Any(static entry => entry.MutationKind == LogbookMutationKind.RemoveReputation))
            .IsTrue();
        await Assert
            .That(entries.Any(static entry => entry.MutationKind == LogbookMutationKind.SetQuestGlobalState))
            .IsFalse();
        await Assert
            .That(entries.Any(static entry => entry.MutationKind == LogbookMutationKind.RemoveBlessing))
            .IsTrue();
        await Assert.That(entries.Any(static entry => entry.MutationKind == LogbookMutationKind.RemoveCurse)).IsTrue();
        await Assert.That(entries.Any(static entry => entry.MutationKind == LogbookMutationKind.AddKill)).IsTrue();
        await Assert
            .That(entries.Any(static entry => entry.MutationKind == LogbookMutationKind.SetTotalKills))
            .IsTrue();
        await Assert
            .That(entries.Any(static entry => entry.MutationKind == LogbookMutationKind.SetMostPowerfulKill))
            .IsTrue();
        await Assert.That(entries.Any(static entry => entry.MutationKind == LogbookMutationKind.AddInjury)).IsTrue();
        await Assert.That(entries.Any(static entry => entry.MutationKind == LogbookMutationKind.RemoveInjury)).IsTrue();
        await Assert
            .That(entries.Any(static entry => entry.MutationKind == LogbookMutationKind.SetBackground))
            .IsTrue();
        await Assert
            .That(entries.Any(static entry => entry.MutationKind == LogbookMutationKind.ClearBackground))
            .IsTrue();
        await Assert.That(entries.Any(static entry => entry.MutationKind == LogbookMutationKind.RemoveKey)).IsTrue();

        var backgroundEntry = entries.Single(static entry => entry.MutationKind == LogbookMutationKind.SetBackground);
        await Assert.That(backgroundEntry.EntryText).IsEqualTo("4");
        await Assert.That(backgroundEntry.AuxiliaryText).IsEqualTo("1004");

        var clearBackgroundEntry = entries.Single(static entry =>
            entry.MutationKind == LogbookMutationKind.ClearBackground
        );
        await Assert.That(clearBackgroundEntry.EntryText).IsEqualTo(string.Empty);
        await Assert.That(clearBackgroundEntry.AuxiliaryText).IsEqualTo(string.Empty);
        await Assert.That(clearBackgroundEntry.DisplayName).Contains("Clear");

        var removeInjuryEntry = entries.Single(static entry => entry.MutationKind == LogbookMutationKind.RemoveInjury);
        await Assert.That(removeInjuryEntry.EntryText).IsEqualTo("41200");
        await Assert.That(removeInjuryEntry.ValueText).IsEqualTo("blinded");
        await Assert.That(removeInjuryEntry.AuxiliaryText).IsEqualTo("3");

        var addKillEntry = entries.Single(static entry => entry.MutationKind == LogbookMutationKind.AddKill);
        await Assert.That(addKillEntry.EntryText).IsEqualTo(string.Empty);
        await Assert.That(addKillEntry.CurrentValueText).IsEqualTo("Total kills 9");
        await Assert.That(addKillEntry.SuggestedOperationText).Contains("Prefills Record Kill");

        var totalKillsEntry = entries.Single(static entry => entry.MutationKind == LogbookMutationKind.SetTotalKills);
        await Assert.That(totalKillsEntry.DisplayName).IsEqualTo("Total Kills");
        await Assert.That(totalKillsEntry.ValueText).IsEqualTo("9");

        var mostPowerfulEntry = entries.Single(static entry =>
            entry.MutationKind == LogbookMutationKind.SetMostPowerfulKill
        );
        await Assert.That(mostPowerfulEntry.DisplayName).Contains("Most Powerful");
        await Assert.That(mostPowerfulEntry.DisplayName).Contains("Bandit");
        await Assert.That(mostPowerfulEntry.EntryText).IsEqualTo("30512");
        await Assert.That(mostPowerfulEntry.ValueText).IsEqualTo("12");
        await Assert.That(mostPowerfulEntry.CatalogCategoryToken).IsEqualTo("description");
    }

    [Test]
    public async Task BuildEntries_WhenRumorIsAlreadyQuelled_OmitsNoOpRumorShortcut()
    {
        var snapshot = CreateSnapshot(
            new LogbookPayload(
                new RumorLogbookPageSnapshot(
                    Intelligence: 12,
                    UsesDumbText: false,
                    [new RumorLogbookEntrySnapshot(7, new GameDateTimeSnapshot(5, 1000), true, "Whispers", null, null)],
                    CreateNativeRead()
                ),
                Quests: null,
                Reputations: null,
                BlessingsAndCurses: null,
                KillsAndInjuries: null,
                Background: null,
                KeyringContents: null
            )
        );

        var entries = LogbookLiveEntryCatalog.BuildEntries(snapshot);

        await Assert.That(entries.Count).IsEqualTo(0);
    }

    private static LogbookSnapshot CreateSnapshot(LogbookPayload payload) =>
        new(
            DateTimeOffset.UtcNow,
            IsAvailable: true,
            "Logbook read completed",
            "Read logbook data.",
            "all",
            LogbookPage.All,
            "0x0000000201234567",
            "Pc mob:guid-a from proto#1000",
            payload,
            []
        );

    private static NativeReadSnapshot CreateNativeRead() =>
        new("logbook_read", "0x00000000", "Test native read.", "dispatcher", "0x00000000", "Completed", 0, "0", "0");
}
