using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Immutable domain model holding a complete character's stat, skill, and spell state.
/// Construct through <see cref="Builder"/> or by calling <see cref="ToBuilder"/> on an
/// existing instance.  Binary encoding and decoding is handled internally by
/// <see cref="SaveGameEditor"/>.
/// </summary>
public sealed class CharacterRecord
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

    // ── Metadata ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <see langword="true"/> when the source v2 record contained all four SAR arrays:
    /// stats, basic skills, tech skills, and spell / tech colleges.
    /// PC records always have all four; NPCs may only have the stat array.
    /// </summary>
    public bool HasCompleteData { get; private init; }

    // ── Construction (Builder / codec only) ───────────────────────────────────

    private CharacterRecord() { }

    /// <summary>Returns a builder pre-populated with this record's values.</summary>
    public Builder ToBuilder() => new(this);

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
        int[]? fatigueDamageRaw = null
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
        };
    }

    // ── Format-layer bridge ───────────────────────────────────────────────────

    /// <summary>
    /// Constructs a <see cref="CharacterRecord"/> from a format-layer
    /// <see cref="CharacterMdyRecord"/> decoded from a <c>mobile.mdy</c> file.
    /// </summary>
    public static CharacterRecord From(CharacterMdyRecord rec) =>
        FromArrays(
            rec.Stats,
            rec.BasicSkills,
            rec.TechSkills,
            rec.SpellTech,
            rec.HasCompleteData,
            rec.Gold,
            rec.Arrows,
            rec.TotalKills,
            rec.PortraitIndex,
            rec.Name,
            rec.PositionAiRaw,
            rec.HpDamageRaw,
            rec.FatigueDamageRaw
        );

    /// <summary>
    /// Produces a new <see cref="CharacterMdyRecord"/> derived from
    /// <paramref name="original"/> with all four SAR arrays replaced by the
    /// values in this record.  All other bytes in the original are preserved.
    /// </summary>
    public CharacterMdyRecord ApplyTo(CharacterMdyRecord original) =>
        original
            .WithStats(ToStatArray())
            .WithBasicSkills(ToBasicSkillArray())
            .WithTechSkills(ToTechSkillArray())
            .WithSpellTech(ToSpellTechArray())
            .WithGold(Gold)
            .WithArrows(Arrows)
            .WithTotalKills(TotalKills)
            .WithPortraitIndex(
                PortraitIndex >= 0 ? PortraitIndex
                : original.PortraitIndex >= 0 ? original.PortraitIndex
                : 0
            )
            .WithPositionAi(PositionAiRaw ?? original.PositionAiRaw ?? [0, 0, 0])
            .WithHpDamage(HpDamageRaw ?? original.HpDamageRaw ?? [0, 0, 0, 0])
            .WithFatigueDamage(FatigueDamageRaw ?? original.FatigueDamageRaw ?? [0, 0, 0, 0])
            .WithName(Name ?? original.Name);

    // ── Nested builder ────────────────────────────────────────────────────────

    /// <summary>
    /// Fluent builder for <see cref="CharacterRecord"/>.
    /// Construct via <c>new CharacterRecord.Builder()</c> for a blank character
    /// or via <see cref="CharacterRecord.ToBuilder"/> to copy an existing one.
    /// </summary>
    public sealed class Builder
    {
        private int _str,
            _dex,
            _con,
            _bea,
            _int,
            _per,
            _wil,
            _cha;
        private int _carryWt,
            _dmgBonus,
            _acAdj,
            _spd,
            _healRate,
            _poisonRec,
            _reactMod,
            _maxFoll,
            _magTechApt;
        private int _lvl,
            _xp,
            _align,
            _fate,
            _unspent,
            _magPts,
            _techPts,
            _poisonLvl,
            _age,
            _gender,
            _race;
        private int _bow,
            _dodge,
            _melee,
            _throw,
            _back,
            _pp,
            _prowl,
            _spot,
            _gamble,
            _haggle,
            _heal,
            _persuade;
        private int _repair,
            _firearms,
            _pickLocks,
            _disarmTraps;
        private int _sConv,
            _sDiv,
            _sAir,
            _sEarth,
            _sFire,
            _sWater,
            _sForce,
            _sMental,
            _sMeta,
            _sMorph,
            _sNature,
            _sNecroBlk,
            _sNecroWht,
            _sPhant,
            _sSum,
            _sTemp,
            _sMastery;
        private int _tHerb,
            _tChem,
            _tElec,
            _tExp,
            _tGun,
            _tMech,
            _tSmith,
            _tTherap;
        private int _gold;
        private int _arrows;
        private int _totalKills;
        private int _portraitIndex = -1;
        private string? _name;
        private int[]? _positionAiRaw;
        private int[]? _hpDamageRaw;
        private int[]? _fatigueDamageRaw;
        private bool _hasCompleteData = true;

        /// <summary>Creates an empty builder; all values default to zero.</summary>
        public Builder() { }

        /// <summary>Creates a builder pre-populated from an existing record.</summary>
        public Builder(CharacterRecord from)
        {
            _str = from.Strength;
            _dex = from.Dexterity;
            _con = from.Constitution;
            _bea = from.Beauty;
            _int = from.Intelligence;
            _per = from.Perception;
            _wil = from.Willpower;
            _cha = from.Charisma;
            _carryWt = from.CarryWeight;
            _dmgBonus = from.DamageBonus;
            _acAdj = from.AcAdjustment;
            _spd = from.Speed;
            _healRate = from.HealRate;
            _poisonRec = from.PoisonRecovery;
            _reactMod = from.ReactionModifier;
            _maxFoll = from.MaxFollowers;
            _magTechApt = from.MagickTechAptitude;
            _lvl = from.Level;
            _xp = from.ExperiencePoints;
            _align = from.Alignment;
            _fate = from.FatePoints;
            _unspent = from.UnspentPoints;
            _magPts = from.MagickPoints;
            _techPts = from.TechPoints;
            _poisonLvl = from.PoisonLevel;
            _age = from.Age;
            _gender = from.Gender;
            _race = from.Race;
            _bow = from.SkillBow;
            _dodge = from.SkillDodge;
            _melee = from.SkillMelee;
            _throw = from.SkillThrowing;
            _back = from.SkillBackstab;
            _pp = from.SkillPickPocket;
            _prowl = from.SkillProwling;
            _spot = from.SkillSpotTrap;
            _gamble = from.SkillGambling;
            _haggle = from.SkillHaggle;
            _heal = from.SkillHeal;
            _persuade = from.SkillPersuasion;
            _repair = from.SkillRepair;
            _firearms = from.SkillFirearms;
            _pickLocks = from.SkillPickLocks;
            _disarmTraps = from.SkillDisarmTraps;
            _sConv = from.SpellConveyance;
            _sDiv = from.SpellDivination;
            _sAir = from.SpellAir;
            _sEarth = from.SpellEarth;
            _sFire = from.SpellFire;
            _sWater = from.SpellWater;
            _sForce = from.SpellForce;
            _sMental = from.SpellMental;
            _sMeta = from.SpellMeta;
            _sMorph = from.SpellMorph;
            _sNature = from.SpellNature;
            _sNecroBlk = from.SpellNecroBlack;
            _sNecroWht = from.SpellNecroWhite;
            _sPhant = from.SpellPhantasm;
            _sSum = from.SpellSummoning;
            _sTemp = from.SpellTemporal;
            _sMastery = from.SpellMastery;
            _tHerb = from.TechHerbology;
            _tChem = from.TechChemistry;
            _tElec = from.TechElectric;
            _tExp = from.TechExplosives;
            _tGun = from.TechGun;
            _tMech = from.TechMechanical;
            _tSmith = from.TechSmithy;
            _tTherap = from.TechTherapeutics;
            _gold = from.Gold;
            _arrows = from.Arrows;
            _totalKills = from.TotalKills;
            _portraitIndex = from.PortraitIndex;
            _name = from.Name;
            _positionAiRaw = from.PositionAiRaw;
            _hpDamageRaw = from.HpDamageRaw;
            _fatigueDamageRaw = from.FatigueDamageRaw;
        }

        // ── Attributes ────────────────────────────────────────────────────────

        public Builder WithStrength(int v)
        {
            _str = v;
            return this;
        }

        public Builder WithDexterity(int v)
        {
            _dex = v;
            return this;
        }

        public Builder WithConstitution(int v)
        {
            _con = v;
            return this;
        }

        public Builder WithBeauty(int v)
        {
            _bea = v;
            return this;
        }

        public Builder WithIntelligence(int v)
        {
            _int = v;
            return this;
        }

        public Builder WithPerception(int v)
        {
            _per = v;
            return this;
        }

        public Builder WithWillpower(int v)
        {
            _wil = v;
            return this;
        }

        public Builder WithCharisma(int v)
        {
            _cha = v;
            return this;
        }

        // ── Progression ───────────────────────────────────────────────────────

        public Builder WithLevel(int v)
        {
            _lvl = v;
            return this;
        }

        public Builder WithExperiencePoints(int v)
        {
            _xp = v;
            return this;
        }

        public Builder WithAlignment(int v)
        {
            _align = v;
            return this;
        }

        public Builder WithFatePoints(int v)
        {
            _fate = v;
            return this;
        }

        public Builder WithUnspentPoints(int v)
        {
            _unspent = v;
            return this;
        }

        public Builder WithMagickPoints(int v)
        {
            _magPts = v;
            return this;
        }

        public Builder WithTechPoints(int v)
        {
            _techPts = v;
            return this;
        }

        public Builder WithPoisonLevel(int v)
        {
            _poisonLvl = v;
            return this;
        }

        public Builder WithAge(int v)
        {
            _age = v;
            return this;
        }

        public Builder WithGender(int v)
        {
            _gender = v;
            return this;
        }

        public Builder WithRace(int v)
        {
            _race = v;
            return this;
        }

        // ── Basic Skills ──────────────────────────────────────────────────────

        public Builder WithSkillBow(int v)
        {
            _bow = v;
            return this;
        }

        public Builder WithSkillDodge(int v)
        {
            _dodge = v;
            return this;
        }

        public Builder WithSkillMelee(int v)
        {
            _melee = v;
            return this;
        }

        public Builder WithSkillThrowing(int v)
        {
            _throw = v;
            return this;
        }

        public Builder WithSkillBackstab(int v)
        {
            _back = v;
            return this;
        }

        public Builder WithSkillPickPocket(int v)
        {
            _pp = v;
            return this;
        }

        public Builder WithSkillProwling(int v)
        {
            _prowl = v;
            return this;
        }

        public Builder WithSkillSpotTrap(int v)
        {
            _spot = v;
            return this;
        }

        public Builder WithSkillGambling(int v)
        {
            _gamble = v;
            return this;
        }

        public Builder WithSkillHaggle(int v)
        {
            _haggle = v;
            return this;
        }

        public Builder WithSkillHeal(int v)
        {
            _heal = v;
            return this;
        }

        public Builder WithSkillPersuasion(int v)
        {
            _persuade = v;
            return this;
        }

        // ── Tech Skills ───────────────────────────────────────────────────────

        public Builder WithSkillRepair(int v)
        {
            _repair = v;
            return this;
        }

        public Builder WithSkillFirearms(int v)
        {
            _firearms = v;
            return this;
        }

        public Builder WithSkillPickLocks(int v)
        {
            _pickLocks = v;
            return this;
        }

        public Builder WithSkillDisarmTraps(int v)
        {
            _disarmTraps = v;
            return this;
        }

        // ── Spell Colleges ────────────────────────────────────────────────────

        public Builder WithSpellConveyance(int v)
        {
            _sConv = v;
            return this;
        }

        public Builder WithSpellDivination(int v)
        {
            _sDiv = v;
            return this;
        }

        public Builder WithSpellAir(int v)
        {
            _sAir = v;
            return this;
        }

        public Builder WithSpellEarth(int v)
        {
            _sEarth = v;
            return this;
        }

        public Builder WithSpellFire(int v)
        {
            _sFire = v;
            return this;
        }

        public Builder WithSpellWater(int v)
        {
            _sWater = v;
            return this;
        }

        public Builder WithSpellForce(int v)
        {
            _sForce = v;
            return this;
        }

        public Builder WithSpellMental(int v)
        {
            _sMental = v;
            return this;
        }

        public Builder WithSpellMeta(int v)
        {
            _sMeta = v;
            return this;
        }

        public Builder WithSpellMorph(int v)
        {
            _sMorph = v;
            return this;
        }

        public Builder WithSpellNature(int v)
        {
            _sNature = v;
            return this;
        }

        public Builder WithSpellNecroBlack(int v)
        {
            _sNecroBlk = v;
            return this;
        }

        public Builder WithSpellNecroWhite(int v)
        {
            _sNecroWht = v;
            return this;
        }

        public Builder WithSpellPhantasm(int v)
        {
            _sPhant = v;
            return this;
        }

        public Builder WithSpellSummoning(int v)
        {
            _sSum = v;
            return this;
        }

        public Builder WithSpellTemporal(int v)
        {
            _sTemp = v;
            return this;
        }

        public Builder WithSpellMastery(int v)
        {
            _sMastery = v;
            return this;
        }

        // ── Tech Disciplines ──────────────────────────────────────────────────

        public Builder WithTechHerbology(int v)
        {
            _tHerb = v;
            return this;
        }

        public Builder WithTechChemistry(int v)
        {
            _tChem = v;
            return this;
        }

        public Builder WithTechElectric(int v)
        {
            _tElec = v;
            return this;
        }

        public Builder WithTechExplosives(int v)
        {
            _tExp = v;
            return this;
        }

        public Builder WithTechGun(int v)
        {
            _tGun = v;
            return this;
        }

        public Builder WithTechMechanical(int v)
        {
            _tMech = v;
            return this;
        }

        public Builder WithTechSmithy(int v)
        {
            _tSmith = v;
            return this;
        }

        public Builder WithTechTherapeutics(int v)
        {
            _tTherap = v;
            return this;
        }

        public Builder WithGold(int v)
        {
            _gold = v;
            return this;
        }

        public Builder WithArrows(int v)
        {
            _arrows = v;
            return this;
        }

        public Builder WithTotalKills(int v)
        {
            _totalKills = v;
            return this;
        }

        public Builder WithPortraitIndex(int v)
        {
            _portraitIndex = v;
            return this;
        }

        public Builder WithPositionAiRaw(int[] v)
        {
            _positionAiRaw = v;
            return this;
        }

        public Builder WithHpDamageRaw(int[] v)
        {
            _hpDamageRaw = v;
            return this;
        }

        public Builder WithFatigueDamageRaw(int[] v)
        {
            _fatigueDamageRaw = v;
            return this;
        }

        public Builder WithName(string? v)
        {
            _name = v;
            return this;
        }

        internal Builder WithHasCompleteData(bool v)
        {
            _hasCompleteData = v;
            return this;
        }

        // ── Build ─────────────────────────────────────────────────────────────

        public CharacterRecord Build() =>
            new()
            {
                Strength = _str,
                Dexterity = _dex,
                Constitution = _con,
                Beauty = _bea,
                Intelligence = _int,
                Perception = _per,
                Willpower = _wil,
                Charisma = _cha,
                CarryWeight = _carryWt,
                DamageBonus = _dmgBonus,
                AcAdjustment = _acAdj,
                Speed = _spd,
                HealRate = _healRate,
                PoisonRecovery = _poisonRec,
                ReactionModifier = _reactMod,
                MaxFollowers = _maxFoll,
                MagickTechAptitude = _magTechApt,
                Level = _lvl,
                ExperiencePoints = _xp,
                Alignment = _align,
                FatePoints = _fate,
                UnspentPoints = _unspent,
                MagickPoints = _magPts,
                TechPoints = _techPts,
                PoisonLevel = _poisonLvl,
                Age = _age,
                Gender = _gender,
                Race = _race,
                SkillBow = _bow,
                SkillDodge = _dodge,
                SkillMelee = _melee,
                SkillThrowing = _throw,
                SkillBackstab = _back,
                SkillPickPocket = _pp,
                SkillProwling = _prowl,
                SkillSpotTrap = _spot,
                SkillGambling = _gamble,
                SkillHaggle = _haggle,
                SkillHeal = _heal,
                SkillPersuasion = _persuade,
                SkillRepair = _repair,
                SkillFirearms = _firearms,
                SkillPickLocks = _pickLocks,
                SkillDisarmTraps = _disarmTraps,
                SpellConveyance = _sConv,
                SpellDivination = _sDiv,
                SpellAir = _sAir,
                SpellEarth = _sEarth,
                SpellFire = _sFire,
                SpellWater = _sWater,
                SpellForce = _sForce,
                SpellMental = _sMental,
                SpellMeta = _sMeta,
                SpellMorph = _sMorph,
                SpellNature = _sNature,
                SpellNecroBlack = _sNecroBlk,
                SpellNecroWhite = _sNecroWht,
                SpellPhantasm = _sPhant,
                SpellSummoning = _sSum,
                SpellTemporal = _sTemp,
                SpellMastery = _sMastery,
                TechHerbology = _tHerb,
                TechChemistry = _tChem,
                TechElectric = _tElec,
                TechExplosives = _tExp,
                TechGun = _tGun,
                TechMechanical = _tMech,
                TechSmithy = _tSmith,
                TechTherapeutics = _tTherap,
                HasCompleteData = _hasCompleteData,
                Gold = _gold,
                Arrows = _arrows,
                TotalKills = _totalKills,
                PortraitIndex = _portraitIndex,
                Name = _name,
                PositionAiRaw = _positionAiRaw,
                HpDamageRaw = _hpDamageRaw,
                FatigueDamageRaw = _fatigueDamageRaw,
            };
    }
}
