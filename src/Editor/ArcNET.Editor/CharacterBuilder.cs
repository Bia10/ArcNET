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

    /// <summary>Sets the tile position of the character (<c>ObjFLocation</c>).</summary>
    public CharacterBuilder WithLocation(int tileX, int tileY)
    {
        _inner.WithLocation(tileX, tileY);
        return this;
    }

    /// <summary>
    /// Sets hit-point values.
    /// <para><c>pts</c> → <c>ObjFHpPts</c> (base maximum HP).</para>
    /// <para><c>adj</c> → <c>ObjFHpAdj</c> (adjustment/current damage offset; default 0).</para>
    /// </summary>
    public CharacterBuilder WithHitPoints(int pts, int adj = 0)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFHpPts, pts));
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFHpAdj, adj));
        return this;
    }

    /// <summary>Sets the amount of damage the character has taken (<c>ObjFHpDamage</c>).</summary>
    public CharacterBuilder WithHpDamage(int damage)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFHpDamage, damage));
        return this;
    }

    // ── Critter base fields (shared by PC and NPC) ────────────────────────────

    /// <summary>
    /// Sets ability-score base values (<c>ObjFCritterStatBaseIdx</c>, Int32 array, 28 elements).
    /// Arcanum's stat array contains all 28 critter stats in this order:
    /// STR, DEX, CON, BEAUT, INT, PERC, WILL, CHA, CARRY_WT, DMG_BONUS, AC_ADJ, SPEED,
    /// HEAL_RATE, POISON_REC, REACT_MOD, MAX_FOLL, MAGIC_TECH_APT, LEVEL, XP_TOTAL,
    /// ALIGNMENT, FATE_PTS, UNSPENT_PTS, MAGIC_PTS, TECH_PTS, POISON_LVL, AGE, GENDER, RACE.
    /// For PC records stored in the v2 mobile.mdy format, use <see cref="SaveGameEditor"/> instead.
    /// </summary>
    public CharacterBuilder WithBaseStats(ReadOnlySpan<int> stats)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32Array(ObjectField.ObjFCritterStatBaseIdx, stats));
        return this;
    }

    /// <summary>Sets basic-skill ranks (<c>ObjFCritterBasicSkillIdx</c>, Int32 array).</summary>
    public CharacterBuilder WithBasicSkills(ReadOnlySpan<int> skills)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32Array(ObjectField.ObjFCritterBasicSkillIdx, skills));
        return this;
    }

    /// <summary>Sets tech-skill ranks (<c>ObjFCritterTechSkillIdx</c>, Int32 array).</summary>
    public CharacterBuilder WithTechSkills(ReadOnlySpan<int> skills)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32Array(ObjectField.ObjFCritterTechSkillIdx, skills));
        return this;
    }

    /// <summary>Sets spell/tech discipline ranks (<c>ObjFCritterSpellTechIdx</c>, Int32 array).</summary>
    public CharacterBuilder WithSpellTech(ReadOnlySpan<int> ranks)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32Array(ObjectField.ObjFCritterSpellTechIdx, ranks));
        return this;
    }

    /// <summary>
    /// Sets fatigue values.
    /// <para><c>pts</c> → <c>ObjFCritterFatiguePts</c> (base maximum fatigue).</para>
    /// <para><c>adj</c> → <c>ObjFCritterFatigueAdj</c> (adjustment; default 0).</para>
    /// </summary>
    public CharacterBuilder WithFatigue(int pts, int adj = 0)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFCritterFatiguePts, pts));
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFCritterFatigueAdj, adj));
        return this;
    }

    /// <summary>Sets the portrait art ID (<c>ObjFCritterPortrait</c>).</summary>
    public CharacterBuilder WithPortrait(int portraitId)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFCritterPortrait, portraitId));
        return this;
    }

    /// <summary>
    /// Sets the character's carried gold link (<c>ObjFCritterGold</c>).
    /// In the compact mob format gold is stored as a handle (OID) pointing to a gold-object;
    /// this setter writes the raw handle field for use with v1 records.
    /// For PC records in the v2 mobile.mdy format the gold handle is separate;
    /// edit the linked gold object's quantity directly via the raw bytes.
    /// </summary>
    public CharacterBuilder WithGold(int amount)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFCritterGold, amount));
        return this;
    }

    /// <summary>
    /// Replaces the character's inventory with the given item GUIDs
    /// (<c>ObjFCritterInventoryListIdx</c>, HandleArray).
    /// Also sets <c>ObjFCritterInventoryNum</c> to match the count.
    /// </summary>
    public CharacterBuilder WithInventory(ReadOnlySpan<Guid> itemIds)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForObjectIdArray(ObjectField.ObjFCritterInventoryListIdx, itemIds));
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFCritterInventoryNum, itemIds.Length));
        return this;
    }

    /// <summary>
    /// Replaces the character's follower list with the given critter GUIDs
    /// (<c>ObjFCritterFollowerIdx</c>, HandleArray).
    /// </summary>
    public CharacterBuilder WithFollowers(ReadOnlySpan<Guid> followerIds)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForObjectIdArray(ObjectField.ObjFCritterFollowerIdx, followerIds));
        return this;
    }

    // ── PC-only fields ────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the player-visible character name (<c>ObjFPcPlayerName</c>, String).
    /// Only meaningful for <see cref="ObjectType.Pc"/> objects.
    /// </summary>
    public CharacterBuilder WithPlayerName(string name)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForString(ObjectField.ObjFPcPlayerName, name));
        return this;
    }

    /// <summary>
    /// Sets the bank balance (<c>ObjFPcBankMoney</c>).
    /// Only meaningful for <see cref="ObjectType.Pc"/> objects.
    /// </summary>
    public CharacterBuilder WithBankMoney(int amount)
    {
        _inner.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFPcBankMoney, amount));
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
