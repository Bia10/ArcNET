using ArcNET.Formats;
using ArcNET.GameData.SaveGames;

namespace ArcNET.Diagnostics;

public static class SavePlayerProgressionHistoryService
{
    public const string TrackedFieldsSummary =
        "lv, XP, align, fate, magicPts, techPts, gold, quests, quest-state deltas, rumors, blessings, curses, schematics, hp_dmg, fat_dmg, bullets, powerCells, reputation, SpellTech ranks, base stats, basic skills";

    public static PlayerProgressionHistorySnapshot Create(
        string saveDir,
        int firstSlot,
        int lastSlot,
        QuestLabelCatalogSnapshot? questCatalog = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(saveDir);
        if (lastSlot < firstSlot)
            throw new ArgumentOutOfRangeException(
                nameof(lastSlot),
                "Last slot must be greater than or equal to first slot."
            );

        questCatalog ??= QuestLabelCatalogLoader.TryLoadFromSaveDirectory(saveDir);

        List<PlayerProgressionSlotSnapshot> slots = [];
        PlayerProgressionStateSnapshot? previousState = null;

        foreach (var slot in Enumerable.Range(firstSlot, lastSlot - firstSlot + 1))
        {
            var loaded = TryLoadSlot(saveDir, slot);
            if (loaded is null)
                continue;

            var (save, slotStem) = loaded.Value;
            var resolution = SavePlayerCharacterResolver.Resolve(save);
            var state = resolution is null ? null : CreateState(resolution, questCatalog);
            var isBaseline = state is not null && previousState is null;
            var changes =
                state is null ? [new PlayerProgressionChangeSnapshot("player", "missing", "player record not found")]
                : previousState is null ? []
                : CreateChanges(previousState, state);

            slots.Add(
                new PlayerProgressionSlotSnapshot(
                    slot,
                    slotStem,
                    save.Info.LeaderName,
                    save.Info.LeaderLevel,
                    isBaseline,
                    state,
                    changes
                )
            );

            if (state is not null)
                previousState = state;
        }

        return new PlayerProgressionHistorySnapshot(firstSlot, lastSlot, questCatalog, slots);
    }

    private static (LoadedSave Save, string SlotStem)? TryLoadSlot(string saveDir, int slot)
    {
        try
        {
            var loaded = SaveSlotLoadService.Load(saveDir, slot);
            return (loaded.Save, loaded.SlotStem);
        }
        catch
        {
            return null;
        }
    }

    private static PlayerProgressionStateSnapshot CreateState(
        SavePlayerCharacterResolution resolution,
        QuestLabelCatalogSnapshot? questCatalog
    )
    {
        var record = resolution.Record;
        return new PlayerProgressionStateSnapshot(
            resolution.Path,
            record.Name,
            ReadValue(record.Stats, 17),
            ReadValue(record.Stats, 18),
            ReadValue(record.Stats, 19),
            ReadValue(record.Stats, 20),
            ReadValue(record.Stats, 22),
            ReadValue(record.Stats, 23),
            record.Gold,
            record.QuestCount,
            record.RumorsCount,
            record.BlessingProtoElementCount,
            record.CurseProtoElementCount,
            record.SchematicsElementCount,
            record.HpDamage,
            record.FatigueDamage,
            record.Bullets,
            record.PowerCells,
            CreateQuestEntries(record, questCatalog),
            CreateReputationEntries(record),
            CreateIndexedValues(record.Stats, s_baseStatLabels),
            CreateIndexedValues(record.BasicSkills, s_basicSkillLabels),
            CreateIndexedValues(record.SpellTech, s_spellTechLabels)
        );
    }

    private static IReadOnlyList<PlayerProgressionChangeSnapshot> CreateChanges(
        PlayerProgressionStateSnapshot previous,
        PlayerProgressionStateSnapshot current
    )
    {
        List<PlayerProgressionChangeSnapshot> changes = [];

        AddScalarChange(changes, "scalar", "lv", previous.Level, current.Level);
        AddScalarChange(changes, "scalar", "XP", previous.Xp, current.Xp);
        AddScalarChange(changes, "scalar", "align", previous.Alignment, current.Alignment);
        AddScalarChange(changes, "scalar", "fate", previous.Fate, current.Fate);
        AddScalarChange(changes, "scalar", "magicPts", previous.MagicPoints, current.MagicPoints);
        AddScalarChange(changes, "scalar", "techPts", previous.TechPoints, current.TechPoints);
        AddScalarChange(changes, "scalar", "gold", previous.Gold, current.Gold);
        AddScalarChange(changes, "scalar", "quests", previous.QuestCount, current.QuestCount);
        AddScalarChange(changes, "scalar", "rumors", previous.RumorsCount, current.RumorsCount);
        AddScalarChange(changes, "scalar", "bless", previous.BlessingCount, current.BlessingCount);
        AddScalarChange(changes, "scalar", "curse", previous.CurseCount, current.CurseCount);
        AddScalarChange(changes, "scalar", "schematics", previous.SchematicsCount, current.SchematicsCount);
        AddScalarChange(changes, "scalar", "hp_dmg", previous.HpDamage, current.HpDamage);
        AddScalarChange(changes, "scalar", "fat_dmg", previous.FatigueDamage, current.FatigueDamage);
        AddScalarChange(changes, "scalar", "bullets", previous.Bullets, current.Bullets);
        AddScalarChange(changes, "scalar", "powerCells", previous.PowerCells, current.PowerCells);

        AddQuestChanges(changes, previous.Quests, current.Quests);
        AddReputationChanges(changes, previous.Reputation, current.Reputation);
        AddIndexedChanges(changes, "stat", previous.BaseStats, current.BaseStats);
        AddIndexedChanges(changes, "skill", previous.BasicSkills, current.BasicSkills);
        AddIndexedChanges(changes, "spell", previous.SpellTech, current.SpellTech);

        return changes;
    }

    private static void AddScalarChange(
        List<PlayerProgressionChangeSnapshot> changes,
        string category,
        string key,
        int previous,
        int current
    )
    {
        if (previous != current)
            changes.Add(new PlayerProgressionChangeSnapshot(category, key, $"{key}:{previous}->{current}"));
    }

    private static void AddQuestChanges(
        List<PlayerProgressionChangeSnapshot> changes,
        IReadOnlyList<PlayerQuestEntrySnapshot> previous,
        IReadOnlyList<PlayerQuestEntrySnapshot> current
    )
    {
        var previousByProto = previous.ToDictionary(static entry => entry.ProtoId);
        var currentByProto = current.ToDictionary(static entry => entry.ProtoId);

        foreach (var protoId in previousByProto.Keys.Union(currentByProto.Keys).Order())
        {
            var hasPrevious = previousByProto.TryGetValue(protoId, out var previousEntry);
            var hasCurrent = currentByProto.TryGetValue(protoId, out var currentEntry);

            if (!hasPrevious && hasCurrent)
            {
                var addedEntry = currentEntry!;
                changes.Add(
                    new PlayerProgressionChangeSnapshot(
                        "quest",
                        $"q{protoId}",
                        $"quest+:{FormatQuestRef(addedEntry)}={addedEntry.StateDescription}"
                    )
                );
                continue;
            }

            if (hasPrevious && !hasCurrent)
            {
                var removedEntry = previousEntry!;
                changes.Add(
                    new PlayerProgressionChangeSnapshot(
                        "quest",
                        $"q{protoId}",
                        $"quest-:{FormatQuestRef(removedEntry)} (was {removedEntry.StateDescription})"
                    )
                );
                continue;
            }

            if (previousEntry!.State != currentEntry!.State)
            {
                changes.Add(
                    new PlayerProgressionChangeSnapshot(
                        "quest",
                        $"q{protoId}",
                        $"quest:{FormatQuestRef(currentEntry)}:{previousEntry.StateDescription}->{currentEntry.StateDescription}"
                    )
                );
            }
        }
    }

    private static void AddReputationChanges(
        List<PlayerProgressionChangeSnapshot> changes,
        IReadOnlyList<PlayerReputationEntrySnapshot> previous,
        IReadOnlyList<PlayerReputationEntrySnapshot> current
    )
    {
        var previousBySlot = previous.ToDictionary(static entry => entry.Slot, static entry => entry.Value);
        var currentBySlot = current.ToDictionary(static entry => entry.Slot, static entry => entry.Value);

        foreach (var slot in previousBySlot.Keys.Union(currentBySlot.Keys).Order())
        {
            previousBySlot.TryGetValue(slot, out var previousValue);
            currentBySlot.TryGetValue(slot, out var currentValue);
            if (previousValue == currentValue)
                continue;

            var prefix =
                previousValue == 0 ? "rep+"
                : currentValue == 0 ? "rep-"
                : "rep";
            changes.Add(
                new PlayerProgressionChangeSnapshot(
                    "reputation",
                    $"slot{slot}",
                    $"{prefix}:slot{slot}:{previousValue}->{currentValue}"
                )
            );
        }
    }

    private static void AddIndexedChanges(
        List<PlayerProgressionChangeSnapshot> changes,
        string category,
        IReadOnlyList<PlayerIndexedValueSnapshot> previous,
        IReadOnlyList<PlayerIndexedValueSnapshot> current
    )
    {
        for (var index = 0; index < Math.Min(previous.Count, current.Count); index++)
        {
            if (previous[index].Value == current[index].Value)
                continue;

            changes.Add(
                new PlayerProgressionChangeSnapshot(
                    category,
                    $"{category}{current[index].Index}",
                    $"{category}:{current[index].Label}:{previous[index].Value}->{current[index].Value}"
                )
            );
        }
    }

    private static IReadOnlyList<PlayerQuestEntrySnapshot> CreateQuestEntries(
        CharacterMdyRecord record,
        QuestLabelCatalogSnapshot? questCatalog
    ) =>
        record.QuestEntries is { } entries
            ?
            [
                .. entries.Select(entry => new PlayerQuestEntrySnapshot(
                    entry.ProtoId,
                    questCatalog?.Resolve(entry.ProtoId),
                    entry.Context,
                    entry.Timestamp,
                    entry.State,
                    QuestStateFormatter.Format(entry.State)
                )),
            ]
            : [];

    private static IReadOnlyList<PlayerReputationEntrySnapshot> CreateReputationEntries(CharacterMdyRecord record)
    {
        var reputation = record.ReputationRaw;
        if (reputation is null)
            return [];

        var slots = record.ReputationFactionSlots;
        List<PlayerReputationEntrySnapshot> entries = [];
        for (var index = 0; index < reputation.Length; index++)
        {
            entries.Add(
                new PlayerReputationEntrySnapshot(slots is { Length: > 0 } ? slots[index] : index, reputation[index])
            );
        }

        return entries;
    }

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateIndexedValues(
        int[] values,
        IReadOnlyList<string> labels
    )
    {
        List<PlayerIndexedValueSnapshot> snapshots = [];
        for (var index = 0; index < values.Length; index++)
        {
            snapshots.Add(
                new PlayerIndexedValueSnapshot(
                    index,
                    index < labels.Count ? labels[index] : index.ToString(),
                    values[index]
                )
            );
        }

        return snapshots;
    }

    private static string FormatQuestRef(PlayerQuestEntrySnapshot entry) =>
        entry.Label is null ? $"q{entry.ProtoId}" : $"q{entry.ProtoId}[{entry.Label}]";

    private static int ReadValue(int[] values, int index) => values.Length > index ? values[index] : 0;

    private static readonly string[] s_baseStatLabels =
    [
        "STR",
        "DEX",
        "CON",
        "BEA",
        "INT",
        "PER",
        "WIL",
        "CHA",
        "CarryWt",
        "DmgBonus",
        "AcAdj",
        "Speed",
        "HealRate",
        "PoisRec",
        "ReactMod",
        "MaxFoll",
        "MTApt",
        "lv",
        "XP",
        "align",
        "fate",
        "unspent",
        "magicPts",
        "techPts",
        "poisonLvl",
        "age",
        "gender",
        "race",
    ];

    private static readonly string[] s_basicSkillLabels =
    [
        "BOW",
        "DODGE",
        "MELEE",
        "THROW",
        "BKSTB",
        "PPKT",
        "PROWL",
        "STRAP",
        "GAMBL",
        "HAGGL",
        "HEAL",
        "PERS",
    ];

    private static readonly string[] s_spellTechLabels =
    [
        "Conv",
        "Div",
        "Air",
        "Erth",
        "Fire",
        "Watr",
        "Forc",
        "Ment",
        "Meta",
        "Mrph",
        "Natr",
        "NBlk",
        "NWht",
        "Phan",
        "Summ",
        "Temp",
        "MAST",
        "Herb",
        "Chem",
        "Elec",
        "Xpls",
        "Gun",
        "Mech",
        "Smth",
        "Thrp",
    ];
}
