namespace ArcNET.Diagnostics;

public sealed record class LogbookPayload(
    RumorLogbookPageSnapshot? RumorsAndNotes,
    QuestLogbookPageSnapshot? Quests,
    ReputationLogbookPageSnapshot? Reputations,
    BlessingCurseLogbookPageSnapshot? BlessingsAndCurses,
    KillsAndInjuriesLogbookPageSnapshot? KillsAndInjuries,
    BackgroundLogbookPageSnapshot? Background,
    KeyringLogbookPageSnapshot? KeyringContents
);
