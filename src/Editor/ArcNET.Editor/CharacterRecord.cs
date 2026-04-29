using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Immutable domain model holding a complete character's stat, skill, and spell state.
/// Construct through <see cref="Builder"/> or by calling <see cref="ToBuilder"/> on an
/// existing instance.  Binary encoding and decoding is handled internally by
/// <see cref="SaveGameEditor"/>.
/// </summary>
public sealed partial class CharacterRecord
{
    // ── Primary attributes ────────────────────────────────────────────────────

    public int Strength { get; private init; }
    public int Dexterity { get; private init; }
    public int Constitution { get; private init; }
    public int Beauty { get; private init; }
    public int Intelligence { get; private init; }
    public int Perception { get; private init; }
    public int Willpower { get; private init; }
    public int Charisma { get; private init; }

    // ── Derived / combat stats ─────────────────────────────────────────────────

    public int CarryWeight { get; private init; }
    public int DamageBonus { get; private init; }
    public int AcAdjustment { get; private init; }
    public int Speed { get; private init; }
    public int HealRate { get; private init; }
    public int PoisonRecovery { get; private init; }
    public int ReactionModifier { get; private init; }
    public int MaxFollowers { get; private init; }
    public int MagickTechAptitude { get; private init; }

    // ── Progression ───────────────────────────────────────────────────────────

    public int Level { get; private init; }
    public int ExperiencePoints { get; private init; }

    /// <summary>
    /// Moral alignment on the game's internal scale (0 = full good, 100 = neutral, 200 = full evil).
    /// </summary>
    public int Alignment { get; private init; }

    public int FatePoints { get; private init; }
    public int UnspentPoints { get; private init; }
    public int MagickPoints { get; private init; }
    public int TechPoints { get; private init; }
    public int PoisonLevel { get; private init; }
    public int Age { get; private init; }
    public int Gender { get; private init; }
    public int Race { get; private init; }

    // ── Basic skills (OBJ_F_CRITTER_BASIC_SKILL_IDX, 12 elements) ────────────

    public int SkillBow { get; private init; }
    public int SkillDodge { get; private init; }
    public int SkillMelee { get; private init; }
    public int SkillThrowing { get; private init; }
    public int SkillBackstab { get; private init; }
    public int SkillPickPocket { get; private init; }
    public int SkillProwling { get; private init; }
    public int SkillSpotTrap { get; private init; }
    public int SkillGambling { get; private init; }
    public int SkillHaggle { get; private init; }
    public int SkillHeal { get; private init; }
    public int SkillPersuasion { get; private init; }

    // ── Tech skills (OBJ_F_CRITTER_TECH_SKILL_IDX, 4 elements) ──────────────

    public int SkillRepair { get; private init; }
    public int SkillFirearms { get; private init; }
    public int SkillPickLocks { get; private init; }
    public int SkillDisarmTraps { get; private init; }

    // ── Spell colleges (OBJ_F_CRITTER_SPELL_TECH_IDX, indices 0–15) ──────────

    public int SpellConveyance { get; private init; }
    public int SpellDivination { get; private init; }
    public int SpellAir { get; private init; }
    public int SpellEarth { get; private init; }
    public int SpellFire { get; private init; }
    public int SpellWater { get; private init; }
    public int SpellForce { get; private init; }
    public int SpellMental { get; private init; }
    public int SpellMeta { get; private init; }
    public int SpellMorph { get; private init; }
    public int SpellNature { get; private init; }
    public int SpellNecroBlack { get; private init; }
    public int SpellNecroWhite { get; private init; }
    public int SpellPhantasm { get; private init; }
    public int SpellSummoning { get; private init; }
    public int SpellTemporal { get; private init; }

    /// <summary>Magical mastery rank (index 16 in the spell array).</summary>
    public int SpellMastery { get; private init; }

    // ── Tech disciplines (OBJ_F_CRITTER_SPELL_TECH_IDX, indices 17–24) ───────

    public int TechHerbology { get; private init; }
    public int TechChemistry { get; private init; }
    public int TechElectric { get; private init; }
    public int TechExplosives { get; private init; }
    public int TechGun { get; private init; }
    public int TechMechanical { get; private init; }
    public int TechSmithy { get; private init; }
    public int TechTherapeutics { get; private init; }

    // ── Other character data ──────────────────────────────────────────────────

    /// <summary>The player's current gold amount.</summary>
    public int Gold { get; private init; }

    /// <summary>The player's current arrow count (bsId=0x4D68[8]).</summary>
    public int Arrows { get; private init; }

    /// <summary>The player's total kill count (bsId=0x4D68[0]).</summary>
    public int TotalKills { get; private init; }

    /// <summary>
    /// Bullet count (bsId=0x4D68[11]). Only present for tech-focused characters (GameStats eCnt >= 12).
    /// 0 when absent (magic characters).
    /// </summary>
    public int Bullets { get; private init; }

    /// <summary>
    /// Power-cell count (bsId=0x4D68[12]). Only present for tech-focused characters (GameStats eCnt = 13).
    /// 0 when absent.
    /// </summary>
    public int PowerCells { get; private init; }

    /// <summary>
    /// The character's portrait index (bsId=0x4DA4[1]).
    /// −1 when the portrait SAR is absent from the source record.
    /// </summary>
    public int PortraitIndex { get; private init; } = -1;

    /// <summary>
    /// The PC's name as stored in the save (non-SAR length-prefixed ASCII field).
    /// <see langword="null"/> when absent (NPC records).
    /// </summary>
    public string? Name { get; private init; }

    /// <summary>
    /// Raw three-element position / AI SAR values (bsId=0x4DA3: CurrentAid, Location, OffsetX).
    /// Preserved verbatim through the round-trip so the game can locate the PC.
    /// Null when the SAR is absent (NPC records, incomplete parses).
    /// </summary>
    public int[]? PositionAiRaw { get; private init; }

    /// <summary>
    /// Raw four-element HP SAR values (bsId=0x4046: AcBonus, HpPtsBonus, HpAdj, HpDamage).
    /// All zeros at full health.  Element [3] is the HP damage taken.
    /// Null when the SAR is absent.
    /// </summary>
    public int[]? HpDamageRaw { get; private init; }

    /// <summary>HP damage taken (bsId=0x4046[3]).  0 at full health.</summary>
    public int HpDamage => HpDamageRaw is { } r ? r[3] : 0;

    /// <summary>
    /// Raw four-element Fatigue SAR values (bsId=0x423E: FatiguePtsBonus, FatigueAdj, FatigueDamage, ?).
    /// All zeros at full fatigue.  Element [2] is the fatigue damage taken.
    /// Null when the SAR is absent.
    /// </summary>
    public int[]? FatigueDamageRaw { get; private init; }

    /// <summary>Fatigue damage taken (bsId=0x423E[2]).  0 at full fatigue.</summary>
    public int FatigueDamage => FatigueDamageRaw is { } r ? r[2] : 0;

    // ── Quest log ─────────────────────────────────────────────────────────────

    /// <summary>Number of quest-log entries.  0 when the quest SAR is absent (very early saves).</summary>
    public int QuestCount { get; private init; }

    /// <summary>
    /// Raw quest-log entry bytes (16 bytes per entry), or <see langword="null"/> when absent.
    /// Each 16-byte block: INT32 context, INT32 timestamp, INT32 state, INT32 reserved.
    /// The corresponding quest proto IDs are in <see cref="QuestBitsetRaw"/>.
    /// </summary>
    public byte[]? QuestDataRaw { get; private init; }

    /// <summary>
    /// The 37-word bitset encoding which quest-slot IDs are active, or <see langword="null"/> when absent.
    /// Bit N set → quest proto ID N has an entry in <see cref="QuestDataRaw"/>.
    /// </summary>
    public int[]? QuestBitsetRaw { get; private init; }

    // ── Reputation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Raw 19-element faction-reputation array, or <see langword="null"/> when absent (early saves).
    /// Element order matches the SAR bitset slot IDs; see <see cref="CharacterMdyRecord.ReputationFactionSlots"/>.
    /// </summary>
    public int[]? ReputationRaw { get; private init; }

    // ── Blessings / Curses ────────────────────────────────────────────────────

    /// <summary>Number of active divine blessings.  0 when no blessings have been received.</summary>
    public int BlessingProtoElementCount => BlessingRaw?.Length ?? 0;

    /// <summary>
    /// Divine blessing prototype IDs (one per god who blessed this character), or <see langword="null"/> when absent.
    /// </summary>
    public int[]? BlessingRaw { get; private init; }

    /// <summary>
    /// Raw blessing timestamp data (8 bytes per entry, parallel to <see cref="BlessingRaw"/>),
    /// or <see langword="null"/> when absent.
    /// </summary>
    public byte[]? BlessingTsRaw { get; private init; }

    /// <summary>Number of active divine curses.  0 when no curses are present.</summary>
    public int CurseProtoElementCount => CurseRaw?.Length ?? 0;

    /// <summary>
    /// Divine curse prototype IDs (one per god who cursed this character), or <see langword="null"/> when absent.
    /// </summary>
    public int[]? CurseRaw { get; private init; }

    /// <summary>
    /// Raw curse timestamp data (8 bytes per entry, parallel to <see cref="CurseRaw"/>),
    /// or <see langword="null"/> when absent.
    /// </summary>
    public byte[]? CurseTsRaw { get; private init; }

    // ── Schematics ────────────────────────────────────────────────────────────

    /// <summary>Number of tech schematics found.  0 for pure magic builds.</summary>
    public int SchematicsElementCount => SchematicsRaw?.Length ?? 0;

    /// <summary>
    /// Tech schematic prototype IDs found by this character, or <see langword="null"/> when absent.
    /// </summary>
    public int[]? SchematicsRaw { get; private init; }

    // ── Rumors ────────────────────────────────────────────────────────────────

    /// <summary>Number of known rumors.  0 when the rumors SAR is absent.</summary>
    public int RumorsCount { get; private init; }

    /// <summary>
    /// Raw rumors data (8 bytes per entry), or <see langword="null"/> when absent.
    /// Count is given by <see cref="RumorsCount"/>.
    /// </summary>
    public byte[]? RumorsRaw { get; private init; }

    // ── Metadata ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <see langword="true"/> when the source v2 record contained all four SAR arrays:
    /// stats, basic skills, tech skills, and spell / tech colleges.
    /// PC records always have all four; NPCs may only have the stat array.
    /// </summary>
    public bool HasCompleteData { get; private init; }

    // ── Construction (Builder / codec only) ───────────────────────────────────

    private CharacterRecord() { }

    // ── Internal array views — used only by V2MdyCharacterCodec ──────────────

    internal int[] ToStatArray() =>
        [
            Strength,
            Dexterity,
            Constitution,
            Beauty,
            Intelligence,
            Perception,
            Willpower,
            Charisma,
            CarryWeight,
            DamageBonus,
            AcAdjustment,
            Speed,
            HealRate,
            PoisonRecovery,
            ReactionModifier,
            MaxFollowers,
            MagickTechAptitude,
            Level,
            ExperiencePoints,
            Alignment,
            FatePoints,
            UnspentPoints,
            MagickPoints,
            TechPoints,
            PoisonLevel,
            Age,
            Gender,
            Race,
        ];

    internal int[] ToBasicSkillArray() =>
        [
            SkillBow,
            SkillDodge,
            SkillMelee,
            SkillThrowing,
            SkillBackstab,
            SkillPickPocket,
            SkillProwling,
            SkillSpotTrap,
            SkillGambling,
            SkillHaggle,
            SkillHeal,
            SkillPersuasion,
        ];

    internal int[] ToTechSkillArray() => [SkillRepair, SkillFirearms, SkillPickLocks, SkillDisarmTraps];

    internal int[] ToSpellTechArray() =>
        [
            SpellConveyance,
            SpellDivination,
            SpellAir,
            SpellEarth,
            SpellFire,
            SpellWater,
            SpellForce,
            SpellMental,
            SpellMeta,
            SpellMorph,
            SpellNature,
            SpellNecroBlack,
            SpellNecroWhite,
            SpellPhantasm,
            SpellSummoning,
            SpellTemporal,
            SpellMastery,
            TechHerbology,
            TechChemistry,
            TechElectric,
            TechExplosives,
            TechGun,
            TechMechanical,
            TechSmithy,
            TechTherapeutics,
        ];

    internal static CharacterRecord FromArrays(
        int[] stats,
        int[] basicSkills,
        int[] techSkills,
        int[] spellTech,
        bool hasCompleteData,
        int gold = 0,
        int arrows = 0,
        int totalKills = 0,
        int portraitIndex = -1,
        string? name = null,
        int[]? positionAiRaw = null,
        int[]? hpDamageRaw = null,
        int[]? fatigueDamageRaw = null,
        int bullets = 0,
        int powerCells = 0,
        int questCount = 0,
        byte[]? questDataRaw = null,
        int[]? questBitsetRaw = null,
        int[]? reputationRaw = null,
        int[]? blessingRaw = null,
        byte[]? blessingTsRaw = null,
        int[]? curseRaw = null,
        byte[]? curseTsRaw = null,
        int[]? schematicsRaw = null,
        int rumorsCount = 0,
        byte[]? rumorsRaw = null
    )
    {
        static int At(int[] arr, int i) => i < arr.Length ? arr[i] : 0;

        return new CharacterRecord
        {
            Strength = At(stats, 0),
            Dexterity = At(stats, 1),
            Constitution = At(stats, 2),
            Beauty = At(stats, 3),
            Intelligence = At(stats, 4),
            Perception = At(stats, 5),
            Willpower = At(stats, 6),
            Charisma = At(stats, 7),
            CarryWeight = At(stats, 8),
            DamageBonus = At(stats, 9),
            AcAdjustment = At(stats, 10),
            Speed = At(stats, 11),
            HealRate = At(stats, 12),
            PoisonRecovery = At(stats, 13),
            ReactionModifier = At(stats, 14),
            MaxFollowers = At(stats, 15),
            MagickTechAptitude = At(stats, 16),
            Level = At(stats, 17),
            ExperiencePoints = At(stats, 18),
            Alignment = At(stats, 19),
            FatePoints = At(stats, 20),
            UnspentPoints = At(stats, 21),
            MagickPoints = At(stats, 22),
            TechPoints = At(stats, 23),
            PoisonLevel = At(stats, 24),
            Age = At(stats, 25),
            Gender = At(stats, 26),
            Race = At(stats, 27),
            SkillBow = At(basicSkills, 0),
            SkillDodge = At(basicSkills, 1),
            SkillMelee = At(basicSkills, 2),
            SkillThrowing = At(basicSkills, 3),
            SkillBackstab = At(basicSkills, 4),
            SkillPickPocket = At(basicSkills, 5),
            SkillProwling = At(basicSkills, 6),
            SkillSpotTrap = At(basicSkills, 7),
            SkillGambling = At(basicSkills, 8),
            SkillHaggle = At(basicSkills, 9),
            SkillHeal = At(basicSkills, 10),
            SkillPersuasion = At(basicSkills, 11),
            SkillRepair = At(techSkills, 0),
            SkillFirearms = At(techSkills, 1),
            SkillPickLocks = At(techSkills, 2),
            SkillDisarmTraps = At(techSkills, 3),
            SpellConveyance = At(spellTech, 0),
            SpellDivination = At(spellTech, 1),
            SpellAir = At(spellTech, 2),
            SpellEarth = At(spellTech, 3),
            SpellFire = At(spellTech, 4),
            SpellWater = At(spellTech, 5),
            SpellForce = At(spellTech, 6),
            SpellMental = At(spellTech, 7),
            SpellMeta = At(spellTech, 8),
            SpellMorph = At(spellTech, 9),
            SpellNature = At(spellTech, 10),
            SpellNecroBlack = At(spellTech, 11),
            SpellNecroWhite = At(spellTech, 12),
            SpellPhantasm = At(spellTech, 13),
            SpellSummoning = At(spellTech, 14),
            SpellTemporal = At(spellTech, 15),
            SpellMastery = At(spellTech, 16),
            TechHerbology = At(spellTech, 17),
            TechChemistry = At(spellTech, 18),
            TechElectric = At(spellTech, 19),
            TechExplosives = At(spellTech, 20),
            TechGun = At(spellTech, 21),
            TechMechanical = At(spellTech, 22),
            TechSmithy = At(spellTech, 23),
            TechTherapeutics = At(spellTech, 24),
            HasCompleteData = hasCompleteData,
            Gold = gold,
            Arrows = arrows,
            TotalKills = totalKills,
            PortraitIndex = portraitIndex,
            Name = name,
            PositionAiRaw = positionAiRaw,
            HpDamageRaw = hpDamageRaw,
            FatigueDamageRaw = fatigueDamageRaw,
            Bullets = bullets,
            PowerCells = powerCells,
            QuestCount = questCount,
            QuestDataRaw = questDataRaw,
            QuestBitsetRaw = questBitsetRaw,
            ReputationRaw = reputationRaw,
            BlessingRaw = blessingRaw,
            BlessingTsRaw = blessingTsRaw,
            CurseRaw = curseRaw,
            CurseTsRaw = curseTsRaw,
            SchematicsRaw = schematicsRaw,
            RumorsCount = rumorsCount,
            RumorsRaw = rumorsRaw,
        };
    }
}
