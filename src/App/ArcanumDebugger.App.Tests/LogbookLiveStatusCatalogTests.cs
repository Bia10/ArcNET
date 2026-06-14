using ArcanumDebugger.App.ViewModels;
using ArcNET.Diagnostics;

namespace ArcanumDebugger.App.Tests;

public sealed class LogbookLiveStatusCatalogTests
{
    [Test]
    public async Task DescribeRead_WhenQuestUsesBotchedVariant_ReturnsPrefillTokenAndLiveSummary()
    {
        var snapshot = new ReadSnapshot(
            DateTimeOffset.UtcNow,
            IsAvailable: true,
            "Read completed",
            "Read quest state.",
            "quest",
            [],
            "0x0000000201234567",
            "Virgil",
            [new("quest_state", "Quest State", "Accepted [Botched]"), new("raw_state", "Raw State", "258")],
            NativeRead: null,
            Notes: []
        );

        var result = LogbookLiveStatusCatalog.DescribeRead(
            LogbookMutationKind.SetQuestState,
            "Find the bridge",
            snapshot
        );

        await Assert.That(result.StatusText).IsEqualTo("Current PC quest state");
        await Assert.That(result.PrefillValueToken).IsEqualTo("accepted-botched");
        await Assert.That(result.SummaryText).Contains("Virgil");
        await Assert.That(result.SummaryText).Contains("Accepted [Botched]");
    }

    [Test]
    public async Task DescribeRead_WhenRumorIsAlreadyKnown_ExplainsNoOp()
    {
        var snapshot = new ReadSnapshot(
            DateTimeOffset.UtcNow,
            IsAvailable: true,
            "Read completed",
            "Read rumor-known flag.",
            "rumorknown",
            [],
            "0x0000000201234567",
            "Sogg Mead Mugg",
            [new("known", "Known", "Yes"), new("raw_state", "Raw State", "1")],
            NativeRead: null,
            Notes: []
        );

        var result = LogbookLiveStatusCatalog.DescribeRead(
            LogbookMutationKind.SetRumorKnown,
            "Whispers in Tarant",
            snapshot
        );

        await Assert.That(result.StatusText).IsEqualTo("Current rumor-known flag");
        await Assert.That(result.PrefillValueToken).IsNull();
        await Assert.That(result.SummaryText).Contains("already known");
    }

    [Test]
    public async Task DescribeLogbook_WhenReputationIsPresent_DistinguishesAddAndRemoveBehavior()
    {
        var snapshot = CreateSnapshot(
            new LogbookPayload(
                RumorsAndNotes: null,
                Quests: null,
                new ReputationLogbookPageSnapshot(
                    [new ReputationLogbookEntrySnapshot(12, new GameDateTimeSnapshot(10, 2000), "Notorious")],
                    CreateNativeRead()
                ),
                BlessingsAndCurses: null,
                KillsAndInjuries: null,
                Background: null,
                KeyringContents: null
            ),
            "Virgil"
        );

        var addResult = LogbookLiveStatusCatalog.DescribeLogbook(
            LogbookMutationKind.AddReputation,
            "Notorious",
            12,
            0,
            null,
            snapshot
        );
        var removeResult = LogbookLiveStatusCatalog.DescribeLogbook(
            LogbookMutationKind.RemoveReputation,
            "Notorious",
            12,
            0,
            null,
            snapshot
        );

        await Assert.That(addResult.SummaryText).Contains("likely no-op");
        await Assert.That(removeResult.SummaryText).Contains("can clear it");
    }

    [Test]
    public async Task DescribeLogbook_WhenBackgroundDiffers_ExplainsReplaceOrClear()
    {
        var snapshot = CreateSnapshot(
            new LogbookPayload(
                RumorsAndNotes: null,
                Quests: null,
                Reputations: null,
                BlessingsAndCurses: null,
                KillsAndInjuries: null,
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
                KeyringContents: null
            ),
            "Virgil"
        );

        var setResult = LogbookLiveStatusCatalog.DescribeLogbook(
            LogbookMutationKind.SetBackground,
            "Bred in the Wild",
            9,
            1009,
            null,
            snapshot
        );
        var clearResult = LogbookLiveStatusCatalog.DescribeLogbook(
            LogbookMutationKind.ClearBackground,
            "Current background",
            0,
            0,
            null,
            snapshot
        );

        await Assert.That(setResult.SummaryText).Contains("will replace it");
        await Assert.That(clearResult.SummaryText).Contains("will remove it");
    }

    [Test]
    public async Task DescribeLogbook_WhenActiveInjuryMatches_ExplainsAdditionalHistoryRow()
    {
        var snapshot = CreateSnapshot(
            new LogbookPayload(
                RumorsAndNotes: null,
                Quests: null,
                Reputations: null,
                BlessingsAndCurses: null,
                new KillsAndInjuriesLogbookPageSnapshot(
                    Summary: [],
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
                    ],
                    CreateNativeRead()
                ),
                Background: null,
                KeyringContents: null
            ),
            "Virgil"
        );

        var result = LogbookLiveStatusCatalog.DescribeLogbook(
            LogbookMutationKind.AddInjury,
            "Bandit",
            30512,
            0,
            "crippled-leg",
            snapshot
        );

        await Assert.That(result.StatusText).IsEqualTo("Current injury status");
        await Assert.That(result.SummaryText).Contains("already present");
        await Assert.That(result.SummaryText).Contains("append another history row");
    }

    [Test]
    public async Task DescribeLogbook_WhenRecordingKill_ExplainsCurrentLedgerAndProjectedAdvance()
    {
        var snapshot = CreateSnapshot(
            new LogbookPayload(
                RumorsAndNotes: null,
                Quests: null,
                Reputations: null,
                BlessingsAndCurses: null,
                new KillsAndInjuriesLogbookPageSnapshot(
                    Summary: [new KillLogbookSummaryEntrySnapshot("total_kills", "Total Kills", 0, null, 14)],
                    Injuries:
                    [
                        new InjuryLogbookEntrySnapshot(
                            5,
                            30512,
                            "Bandit",
                            2,
                            "Crippled leg",
                            Active: false,
                            "Healed",
                            "Bandit :: Crippled leg :: Healed"
                        ),
                    ],
                    CreateNativeRead()
                ),
                Background: null,
                KeyringContents: null
            ),
            "Virgil"
        );

        var result = LogbookLiveStatusCatalog.DescribeLogbook(
            LogbookMutationKind.AddKill,
            "Sogg Mead Mugg [0x0000000201234567]",
            0,
            0,
            null,
            snapshot
        );

        await Assert.That(LogbookLiveStatusCatalog.Supports(LogbookMutationKind.AddKill)).IsTrue();
        await Assert.That(result.StatusText).IsEqualTo("Current kill ledger");
        await Assert.That(result.SummaryText).Contains("14 total kills");
        await Assert.That(result.SummaryText).Contains("advance total kills to 15");
        await Assert.That(result.SummaryText).Contains("Sogg Mead Mugg");
    }

    [Test]
    public async Task DescribeLogbook_WhenSettingTotalKills_ExplainsCurrentAndRequestedCount()
    {
        var snapshot = CreateSnapshot(
            new LogbookPayload(
                RumorsAndNotes: null,
                Quests: null,
                Reputations: null,
                BlessingsAndCurses: null,
                new KillsAndInjuriesLogbookPageSnapshot(
                    Summary: [new KillLogbookSummaryEntrySnapshot("total_kills", "Total Kills", 0, null, 14)],
                    Injuries: [],
                    CreateNativeRead()
                ),
                Background: null,
                KeyringContents: null
            ),
            "Virgil"
        );

        var result = LogbookLiveStatusCatalog.DescribeLogbook(
            LogbookMutationKind.SetTotalKills,
            "Total Kills",
            0,
            0,
            "20",
            snapshot
        );

        await Assert.That(result.StatusText).IsEqualTo("Current total kills");
        await Assert.That(result.SummaryText).Contains("currently shows 14 total kills");
        await Assert.That(result.SummaryText).Contains("change it to 20");
    }

    [Test]
    public async Task DescribeLogbook_WhenReplacingMostPowerfulKill_ExplainsCurrentAndReplacementRow()
    {
        var snapshot = CreateSnapshot(
            new LogbookPayload(
                RumorsAndNotes: null,
                Quests: null,
                Reputations: null,
                BlessingsAndCurses: null,
                new KillsAndInjuriesLogbookPageSnapshot(
                    Summary:
                    [
                        new KillLogbookSummaryEntrySnapshot("total_kills", "Total Kills", 0, null, 14),
                        new KillLogbookSummaryEntrySnapshot("most_powerful", "Most Powerful", 30512, "Bandit", 12),
                    ],
                    Injuries: [],
                    CreateNativeRead()
                ),
                Background: null,
                KeyringContents: null
            ),
            "Virgil"
        );

        var result = LogbookLiveStatusCatalog.DescribeLogbook(
            LogbookMutationKind.SetMostPowerfulKill,
            "Ogre",
            41200,
            0,
            "18",
            snapshot
        );

        await Assert.That(result.StatusText).IsEqualTo("Current kill summary");
        await Assert.That(result.SummaryText).Contains("Most Powerful currently records Bandit [30512]");
        await Assert.That(result.SummaryText).Contains("replace it with Ogre [41200]");
        await Assert.That(result.SummaryText).Contains("Level 18");
    }

    [Test]
    public async Task DescribeLogbook_WhenHealedInjuryShortcutMatches_ExplainsSafeRemoval()
    {
        var snapshot = CreateSnapshot(
            new LogbookPayload(
                RumorsAndNotes: null,
                Quests: null,
                Reputations: null,
                BlessingsAndCurses: null,
                new KillsAndInjuriesLogbookPageSnapshot(
                    Summary: [],
                    Injuries:
                    [
                        new InjuryLogbookEntrySnapshot(
                            5,
                            30512,
                            "Bandit",
                            2,
                            "Crippled leg",
                            Active: false,
                            "Healed",
                            "Bandit :: Crippled leg :: Healed"
                        ),
                    ],
                    CreateNativeRead()
                ),
                Background: null,
                KeyringContents: null
            ),
            "Virgil"
        );

        var result = LogbookLiveStatusCatalog.DescribeLogbook(
            LogbookMutationKind.RemoveInjury,
            "Bandit",
            30512,
            5,
            "crippled-leg",
            snapshot
        );

        await Assert.That(result.StatusText).IsEqualTo("Healed injury history row");
        await Assert.That(result.SummaryText).Contains("Remove Injury History will delete it");
        await Assert.That(result.SummaryText).Contains("slot 5");
    }

    [Test]
    public async Task DescribeLogbook_WhenRemoveInjuryTargetsActiveRow_ExplainsProtection()
    {
        var snapshot = CreateSnapshot(
            new LogbookPayload(
                RumorsAndNotes: null,
                Quests: null,
                Reputations: null,
                BlessingsAndCurses: null,
                new KillsAndInjuriesLogbookPageSnapshot(
                    Summary: [],
                    Injuries:
                    [
                        new InjuryLogbookEntrySnapshot(
                            7,
                            30512,
                            "Bandit",
                            2,
                            "Crippled leg",
                            Active: true,
                            "Active",
                            "Bandit :: Crippled leg :: Active"
                        ),
                    ],
                    CreateNativeRead()
                ),
                Background: null,
                KeyringContents: null
            ),
            "Virgil"
        );

        var result = LogbookLiveStatusCatalog.DescribeLogbook(
            LogbookMutationKind.RemoveInjury,
            "Bandit",
            30512,
            7,
            "crippled-leg",
            snapshot
        );

        await Assert.That(result.StatusText).IsEqualTo("Active injury protected");
        await Assert.That(result.SummaryText).Contains("Heal the condition first");
    }

    private static LogbookSnapshot CreateSnapshot(LogbookPayload payload, string targetText) =>
        new(
            DateTimeOffset.UtcNow,
            IsAvailable: true,
            "Logbook read completed",
            "Read logbook data.",
            "all",
            LogbookPage.All,
            "0x0000000201234567",
            targetText,
            payload,
            []
        );

    private static NativeReadSnapshot CreateNativeRead() =>
        new("logbook_read", "0x00000000", "Test native read.", "dispatcher", "0x00000000", "Completed", 0, "0", "0");
}
