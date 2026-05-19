using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Fluent builder for PC and NPC <see cref="MobData"/> instances.
/// Provides named, type-safe setters for every commonly edited character field
/// so that save-editor code reads like a description of the desired state:
/// <code>
/// var mob = new CharacterBuilder(existing)
///     .WithHitPoints(100)
///     .WithBaseStats([10, 10, 10, 10, 10, 10])
///     .WithPlayerName("Roberta")
///     .Build();
/// </code>
/// Construct from an existing <see cref="MobData"/> to edit a saved character, or from
/// the type-constructor to create a new critter from scratch.
/// Call <see cref="Build"/> to produce an immutable <see cref="MobData"/> with a
/// freshly rebuilt header bitmap.
/// </summary>
public sealed class CharacterBuilder
{
    private readonly MobDataBuilder _inner;

    /// <summary>
    /// Starts a builder from an existing character's <see cref="MobData"/>.
    /// All properties are copied; modifications do not affect the original.
    /// </summary>
    public CharacterBuilder(MobData existing)
    {
        _inner = new MobDataBuilder(existing);
    }

    /// <summary>
    /// Starts a builder for a brand-new character with no properties set.
    /// </summary>
    /// <param name="type"><see cref="ObjectType.Pc"/> or <see cref="ObjectType.Npc"/>.</param>
    /// <param name="objectId">Unique instance identifier.</param>
    /// <param name="protoId">Prototype reference identifier.</param>
    public CharacterBuilder(ObjectType type, GameObjectGuid objectId, GameObjectGuid protoId)
    {
        _inner = new MobDataBuilder(type, objectId, protoId);
    }

    // ── Common object fields ──────────────────────────────────────────────────

    /// <summary>Sets the tile position of the character (<c>Location</c>).</summary>
    public CharacterBuilder WithLocation(int tileX, int tileY)
    {
        _inner.WithLocation(tileX, tileY);
        return this;
    }

    /// <summary>
    /// Sets hit-point values.
    /// <para><c>pts</c> → <c>HpPts</c> (base maximum HP).</para>
    /// <para><c>adj</c> → <c>HpAdj</c> (adjustment/current damage offset; default 0).</para>
    /// </summary>
    public CharacterBuilder WithHitPoints(int pts, int adj = 0)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.HpPts, pts));
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.HpAdj, adj));
        return this;
    }

    /// <summary>Sets the amount of damage the character has taken (<c>HpDamage</c>).</summary>
    public CharacterBuilder WithHpDamage(int damage)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.HpDamage, damage));
        return this;
    }

    // ── Critter base fields (shared by PC and NPC) ────────────────────────────

    /// <summary>
    /// Sets ability-score base values (<c>CritterStatBaseIdx</c>, Int32 array, 28 elements).
    /// Arcanum's stat array contains all 28 critter stats in this order:
    /// STR, DEX, CON, BEAUT, INT, PERC, WILL, CHA, CARRY_WT, DMG_BONUS, AC_ADJ, SPEED,
    /// HEAL_RATE, POISON_REC, REACT_MOD, MAX_FOLL, MAGIC_TECH_APT, LEVEL, XP_TOTAL,
    /// ALIGNMENT, FATE_PTS, UNSPENT_PTS, MAGIC_PTS, TECH_PTS, POISON_LVL, AGE, GENDER, RACE.
    /// For PC records stored in the v2 mobile.mdy format, use <see cref="SaveGameEditor"/> instead.
    /// </summary>
    public CharacterBuilder WithBaseStats(ReadOnlySpan<int> stats)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32Array(ObjectField.CritterStatBaseIdx, stats));
        return this;
    }

    /// <summary>Sets basic-skill ranks (<c>CritterBasicSkillIdx</c>, Int32 array).</summary>
    public CharacterBuilder WithBasicSkills(ReadOnlySpan<int> skills)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32Array(ObjectField.CritterBasicSkillIdx, skills));
        return this;
    }

    /// <summary>Sets tech-skill ranks (<c>CritterTechSkillIdx</c>, Int32 array).</summary>
    public CharacterBuilder WithTechSkills(ReadOnlySpan<int> skills)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32Array(ObjectField.CritterTechSkillIdx, skills));
        return this;
    }

    /// <summary>Sets spell/tech discipline ranks (<c>CritterSpellTechIdx</c>, Int32 array).</summary>
    public CharacterBuilder WithSpellTech(ReadOnlySpan<int> ranks)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32Array(ObjectField.CritterSpellTechIdx, ranks));
        return this;
    }

    /// <summary>
    /// Sets fatigue values.
    /// <para><c>pts</c> → <c>CritterFatiguePts</c> (base maximum fatigue).</para>
    /// <para><c>adj</c> → <c>CritterFatigueAdj</c> (adjustment; default 0).</para>
    /// </summary>
    public CharacterBuilder WithFatigue(int pts, int adj = 0)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.CritterFatiguePts, pts));
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.CritterFatigueAdj, adj));
        return this;
    }

    /// <summary>Sets the portrait art ID (<c>CritterPortrait</c>).</summary>
    public CharacterBuilder WithPortrait(int portraitId)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.CritterPortrait, portraitId));
        return this;
    }

    /// <summary>
    /// Sets the character's carried gold counter (<c>CritterGold</c>).
    /// This writes the raw critter scalar field used by the compact mob property layer.
    /// For PC records in the v2 <c>mobile.mdy</c> format, use <see cref="SaveGameEditor"/>
    /// or <see cref="CharacterRecord"/> for the player-character save-global gold surface.
    /// </summary>
    public CharacterBuilder WithGold(int amount)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.CritterGold, amount));
        return this;
    }

    /// <summary>
    /// Replaces the character's inventory with the given item GUIDs
    /// (<c>CritterInventoryListIdx</c>, HandleArray).
    /// Also sets <c>CritterInventoryNum</c> to match the count.
    /// </summary>
    public CharacterBuilder WithInventory(ReadOnlySpan<Guid> itemIds)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForObjectIdArray(ObjectField.CritterInventoryListIdx, itemIds));
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.CritterInventoryNum, itemIds.Length));
        return this;
    }

    /// <summary>
    /// Replaces the character's follower list with the given critter GUIDs
    /// (<c>CritterFollowerIdx</c>, HandleArray).
    /// </summary>
    public CharacterBuilder WithFollowers(ReadOnlySpan<Guid> followerIds)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForObjectIdArray(ObjectField.CritterFollowerIdx, followerIds));
        return this;
    }

    // ── PC-only fields ────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the player-visible character name (<c>PcPlayerName</c>, String).
    /// Only meaningful for <see cref="ObjectType.Pc"/> objects.
    /// </summary>
    public CharacterBuilder WithPlayerName(string name)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForString(ObjectField.PcPlayerName, name));
        return this;
    }

    /// <summary>
    /// Sets the bank balance (<c>PcBankMoney</c>).
    /// Only meaningful for <see cref="ObjectType.Pc"/> objects.
    /// </summary>
    public CharacterBuilder WithBankMoney(int amount)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.PcBankMoney, amount));
        return this;
    }

    // ── Escape hatch ──────────────────────────────────────────────────────────

    /// <summary>
    /// Adds or replaces an arbitrary property by raw <see cref="ObjectProperty"/>.
    /// Use this for fields not covered by the named setters above.
    /// </summary>
    public CharacterBuilder WithProperty(ObjectProperty property)
    {
        _inner.WithProperty(property);
        return this;
    }

    /// <summary>Removes a property by field, if present.</summary>
    public CharacterBuilder WithoutProperty(ObjectField field)
    {
        _inner.WithoutProperty(field);
        return this;
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Produces a new <see cref="MobData"/> with a freshly rebuilt header bitmap
    /// matching the current property list.
    /// </summary>
    public MobData Build() => _inner.Build();
}
