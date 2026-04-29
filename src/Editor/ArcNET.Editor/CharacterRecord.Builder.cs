namespace ArcNET.Editor;

public sealed partial class CharacterRecord
{
    /// <summary>Returns a builder pre-populated with this record's values.</summary>
    public Builder ToBuilder() => new(this);

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
        private int _bullets;
        private int _powerCells;
        private int _portraitIndex = -1;
        private string? _name;
        private int[]? _positionAiRaw;
        private int[]? _hpDamageRaw;
        private int[]? _fatigueDamageRaw;
        private int _questCount;
        private byte[]? _questDataRaw;
        private int[]? _questBitsetRaw;
        private int[]? _reputationRaw;
        private int[]? _blessingRaw;
        private byte[]? _blessingTsRaw;
        private int[]? _curseRaw;
        private byte[]? _curseTsRaw;
        private int[]? _schematicsRaw;
        private int _rumorsCount;
        private byte[]? _rumorsRaw;
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
            _bullets = from.Bullets;
            _powerCells = from.PowerCells;
            _portraitIndex = from.PortraitIndex;
            _name = from.Name;
            _positionAiRaw = from.PositionAiRaw;
            _hpDamageRaw = from.HpDamageRaw;
            _fatigueDamageRaw = from.FatigueDamageRaw;
            _questCount = from.QuestCount;
            _questDataRaw = from.QuestDataRaw;
            _questBitsetRaw = from.QuestBitsetRaw;
            _reputationRaw = from.ReputationRaw;
            _blessingRaw = from.BlessingRaw;
            _blessingTsRaw = from.BlessingTsRaw;
            _curseRaw = from.CurseRaw;
            _curseTsRaw = from.CurseTsRaw;
            _schematicsRaw = from.SchematicsRaw;
            _rumorsCount = from.RumorsCount;
            _rumorsRaw = from.RumorsRaw;
        }

        public Builder WithCarryWeight(int v)
        {
            _carryWt = v;
            return this;
        }

        public Builder WithDamageBonus(int v)
        {
            _dmgBonus = v;
            return this;
        }

        public Builder WithAcAdjustment(int v)
        {
            _acAdj = v;
            return this;
        }

        public Builder WithSpeed(int v)
        {
            _spd = v;
            return this;
        }

        public Builder WithHealRate(int v)
        {
            _healRate = v;
            return this;
        }

        public Builder WithPoisonRecovery(int v)
        {
            _poisonRec = v;
            return this;
        }

        public Builder WithReactionModifier(int v)
        {
            _reactMod = v;
            return this;
        }

        public Builder WithMaxFollowers(int v)
        {
            _maxFoll = v;
            return this;
        }

        public Builder WithMagickTechAptitude(int v)
        {
            _magTechApt = v;
            return this;
        }

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

        /// <summary>
        /// Sets the bullet count. Only written to the save when the underlying
        /// GameStats SAR has at least 12 elements (tech-focused characters).
        /// </summary>
        public Builder WithBullets(int v)
        {
            _bullets = v;
            return this;
        }

        /// <summary>
        /// Sets the power-cell count. Only written to the save when the underlying
        /// GameStats SAR has 13 elements.
        /// </summary>
        public Builder WithPowerCells(int v)
        {
            _powerCells = v;
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

        /// <summary>
        /// Sets only the HP damage taken (element [3] of the HP SAR),
        /// preserving the other three elements from the current raw value.
        /// </summary>
        public Builder WithHpDamage(int damage)
        {
            var raw = _hpDamageRaw ?? [0, 0, 0, 0];
            _hpDamageRaw = [raw[0], raw[1], raw[2], damage];
            return this;
        }

        public Builder WithFatigueDamageRaw(int[] v)
        {
            _fatigueDamageRaw = v;
            return this;
        }

        /// <summary>
        /// Sets only the fatigue damage taken (element [2] of the fatigue SAR),
        /// preserving the other three elements from the current raw value.
        /// </summary>
        public Builder WithFatigueDamage(int damage)
        {
            var raw = _fatigueDamageRaw ?? [0, 0, 0, 0];
            _fatigueDamageRaw = [raw[0], raw[1], damage, raw[3]];
            return this;
        }

        public Builder WithName(string? v)
        {
            _name = v;
            return this;
        }

        /// <summary>
        /// Replaces the raw quest-log entry bytes. The count is derived from
        /// <paramref name="data"/>.Length / 16. Also accepts the original bitset
        /// separately via <see cref="WithQuestBitsetRaw"/>.
        /// </summary>
        public Builder WithQuestDataRaw(byte[] data)
        {
            _questDataRaw = data;
            _questCount = data.Length / 16;
            return this;
        }

        /// <summary>Replaces the 37-word quest bitset (slot-ID encoding).</summary>
        public Builder WithQuestBitsetRaw(int[] bitset)
        {
            _questBitsetRaw = bitset;
            return this;
        }

        /// <summary>Replaces the raw 19-element faction-reputation array.</summary>
        public Builder WithReputationRaw(int[] rep)
        {
            _reputationRaw = rep;
            return this;
        }

        /// <summary>Replaces the blessing prototype-ID array.</summary>
        public Builder WithBlessingRaw(int[] bless)
        {
            _blessingRaw = bless;
            return this;
        }

        /// <summary>Replaces the raw blessing timestamp data (8 bytes per entry).</summary>
        public Builder WithBlessingTsRaw(byte[] ts)
        {
            _blessingTsRaw = ts;
            return this;
        }

        /// <summary>Replaces the curse prototype-ID array.</summary>
        public Builder WithCurseRaw(int[] curse)
        {
            _curseRaw = curse;
            return this;
        }

        /// <summary>Replaces the raw curse timestamp data (8 bytes per entry).</summary>
        public Builder WithCurseTsRaw(byte[] ts)
        {
            _curseTsRaw = ts;
            return this;
        }

        /// <summary>Replaces the tech schematics prototype-ID array.</summary>
        public Builder WithSchematicsRaw(int[] sch)
        {
            _schematicsRaw = sch;
            return this;
        }

        /// <summary>Replaces the raw rumors data (8 bytes per entry).</summary>
        public Builder WithRumorsRaw(byte[] rumors)
        {
            _rumorsRaw = rumors;
            _rumorsCount = rumors.Length / 8;
            return this;
        }

        internal Builder WithHasCompleteData(bool v)
        {
            _hasCompleteData = v;
            return this;
        }

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
                Bullets = _bullets,
                PowerCells = _powerCells,
                PortraitIndex = _portraitIndex,
                Name = _name,
                PositionAiRaw = _positionAiRaw,
                HpDamageRaw = _hpDamageRaw,
                FatigueDamageRaw = _fatigueDamageRaw,
                QuestCount = _questCount,
                QuestDataRaw = _questDataRaw,
                QuestBitsetRaw = _questBitsetRaw,
                ReputationRaw = _reputationRaw,
                BlessingRaw = _blessingRaw,
                BlessingTsRaw = _blessingTsRaw,
                CurseRaw = _curseRaw,
                CurseTsRaw = _curseTsRaw,
                SchematicsRaw = _schematicsRaw,
                RumorsCount = _rumorsCount,
                RumorsRaw = _rumorsRaw,
            };
    }
}
