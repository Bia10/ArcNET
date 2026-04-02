using System.Buffers;
using System.Collections;
using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.GameObjects.Types;

namespace ArcNET.GameObjects.Tests;

/// <summary>
/// Round-trip tests for all 22 concrete game object type classes.
/// Uses prototype mode (<c>isPrototype = true</c>) so every field is written/read
/// regardless of the bitmap, giving full coverage of every Write → Read path.
/// </summary>
public class ObjectTypeRoundTripTests
{
    // A bitmap large enough for all ObjectField values (max = 152).
    private static BitArray EmptyBitmap => new(160);

    private static readonly ArtId TestArtId = new(0xABCDEF01u);
    private static readonly Location TestLocation = new(10, 20);
    private static readonly Color TestColor = new(255, 128, 64);
    private static readonly GameObjectGuid TestGuid = new(1u, 2u, 3u, 4u);

    private static void PopulateCommon(ObjectCommon obj)
    {
        obj.CurrentAid = TestArtId;
        obj.Location = TestLocation;
        obj.OffsetX = 1;
        obj.OffsetY = 2;
        obj.Shadow = TestArtId;
        obj.OverlayFore = [10, 20];
        obj.OverlayBack = [30, 40];
        obj.Underlay = [50];
        obj.BlitFlags = 7;
        obj.BlitColor = TestColor;
        obj.BlitAlpha = 200;
        obj.BlitScale = 100;
        obj.LightFlags = 3;
        obj.LightAid = TestArtId;
        obj.LightColor = TestColor;
        obj.OverlayLightFlags = 1;
        obj.OverlayLightAid = [11, 22];
        obj.OverlayLightColor = 0x112233;
        obj.Flags = 0xF0F0;
        obj.SpellFlags = 0x0F0F;
        obj.BlockingMask = 0xAA;
        obj.Name = 42;
        obj.Description = 99;
        obj.Aid = TestArtId;
        obj.DestroyedAid = TestArtId;
        obj.Ac = 5;
        obj.HpPts = 100;
        obj.HpAdj = 10;
        obj.HpDamage = 0;
        obj.Material = 3;
        obj.ResistanceIdx = [1, 2, 3];
        obj.ScriptsIdx =
        [
            new GameObjectScript
            {
                Counters = [0x01, 0x02, 0x03, 0x04],
                Flags = 1,
                ScriptId = 777,
            },
        ];
        obj.SoundEffect = 55;
        obj.Category = 7;
        obj.PadIas1 = 0xDEAD;
        obj.PadI64As1 = 0x123456789ABCDE0L;
    }

    private static void PopulateItem(ObjectItem obj)
    {
        PopulateCommon(obj);
        obj.ItemFlags = 5;
        obj.ItemParent = TestGuid;
        obj.ItemWeight = 10;
        obj.ItemMagicWeightAdj = 2;
        obj.ItemWorth = 500;
        obj.ItemManaStore = 0;
        obj.ItemInvAid = 3;
        obj.ItemInvLocation = 4;
        obj.ItemUseAidFragment = 1;
        obj.ItemMagicTechComplexity = 6;
        obj.ItemDiscipline = 2;
        obj.ItemDescriptionUnknown = 0;
        obj.ItemDescriptionEffects = 1;
        obj.ItemSpell1 = 11;
        obj.ItemSpell2 = 22;
        obj.ItemSpell3 = 33;
        obj.ItemSpell4 = 44;
        obj.ItemSpell5 = 55;
        obj.ItemSpellManaStore = 99;
        obj.ItemAiAction = 7;
        obj.ItemPadI1 = 0xBEEF;
        obj.ItemPadIas1 = 0xC0DE;
        obj.ItemPadI64As1 = 0xFEDCBA987654320L;
    }

    private static byte[] WriteAndCapture(Action<SpanWriter> write)
    {
        var buf = new ArrayBufferWriter<byte>(512);
        var writer = new SpanWriter(buf);
        write(writer);
        return buf.WrittenMemory.ToArray();
    }

    // ── ObjectWall ───────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectWall_RoundTrips()
    {
        var original = new ObjectWall();
        PopulateCommon(original);
        original.WallFlags = 0xFF;
        original.WallPadI1 = 1;
        original.WallPadI2 = 2;
        original.WallPadIas1 = 3;
        original.WallPadI64As1 = 999L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));

        var reader = new SpanReader(bytes1);
        var restored = ObjectWall.Read(ref reader, bitmap, isPrototype: true);

        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectPortal ─────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectPortal_RoundTrips()
    {
        var original = new ObjectPortal();
        PopulateCommon(original);
        original.PortalFlags = 1;
        original.PortalLockDifficulty = 50;
        original.PortalKeyId = 7;
        original.PortalNotifyNpc = 1;
        original.PortalPadI1 = 2;
        original.PortalPadI2 = 3;
        original.PortalPadIas1 = 4;
        original.PortalPadI64As1 = 888L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectPortal.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectContainer ──────────────────────────────────────────────────────

    [Test]
    public async Task ObjectContainer_RoundTrips()
    {
        var original = new ObjectContainer();
        PopulateCommon(original);
        original.ContainerFlags = 2;
        original.ContainerLockDifficulty = 30;
        original.ContainerKeyId = 5;
        original.ContainerInventoryNum = 2;
        original.ContainerInventoryList = [TestGuid, new GameObjectGuid(5u, 6u, 7u, 8u)];
        original.ContainerInventorySource = 1;
        original.ContainerNotifyNpc = 0;
        original.ContainerPadI1 = 1;
        original.ContainerPadI2 = 2;
        original.ContainerPadIas1 = 3;
        original.ContainerPadI64As1 = 777L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectContainer.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectScenery ────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectScenery_RoundTrips()
    {
        var original = new ObjectScenery();
        PopulateCommon(original);
        original.SceneryFlags = 0xA;
        original.SceneryWhosInMe = TestGuid;
        original.SceneryRespawnDelay = 120;
        original.SceneryPadI2 = 1;
        original.SceneryPadIas1 = 2;
        original.SceneryPadI64As1 = 555L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectScenery.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectProjectile ─────────────────────────────────────────────────────

    [Test]
    public async Task ObjectProjectile_RoundTrips()
    {
        var original = new ObjectProjectile();
        PopulateCommon(original);
        original.ProjectileFlagsCombat = 3;
        original.ProjectileFlagsCombatDamage = 15;
        original.ProjectileHitLoc = TestLocation;
        original.ProjectileParentWeapon = 42;
        original.ProjectilePadI1 = 1;
        original.ProjectilePadI2 = 2;
        original.ProjectilePadIas1 = 3;
        original.ProjectilePadI64As1 = 444L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectProjectile.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectTrap ───────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectTrap_RoundTrips()
    {
        var original = new ObjectTrap();
        PopulateCommon(original);
        original.TrapFlags = 5;
        original.TrapDifficulty = 80;
        original.TrapPadI2 = 1;
        original.TrapPadIas1 = 2;
        original.TrapPadI64As1 = 333L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectTrap.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectWeapon ─────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectWeapon_RoundTrips()
    {
        var original = new ObjectWeapon();
        PopulateItem(original);
        original.WeaponFlags = 1;
        original.WeaponPaperDollAid = 2;
        original.WeaponBonusToHit = 3;
        original.WeaponMagicHitAdj = 1;
        original.WeaponDamageLower = [5, 6];
        original.WeaponDamageUpper = [12, 15];
        original.WeaponMagicDamageAdj = [2];
        original.WeaponSpeedFactor = 4;
        original.WeaponMagicSpeedAdj = 1;
        original.WeaponRange = 2;
        original.WeaponMagicRangeAdj = 0;
        original.WeaponMinStrength = 6;
        original.WeaponMagicMinStrengthAdj = 0;
        original.WeaponAmmoType = 1;
        original.WeaponAmmoConsumption = 1;
        original.WeaponMissileAid = 0;
        original.WeaponVisualEffectAid = 0;
        original.WeaponCritHitChart = 1;
        original.WeaponMagicCritHitChance = 5;
        original.WeaponMagicCritHitEffect = 2;
        original.WeaponCritMissChart = 1;
        original.WeaponMagicCritMissChance = 3;
        original.WeaponMagicCritMissEffect = 1;
        original.WeaponPadI1 = 0;
        original.WeaponPadI2 = 0;
        original.WeaponPadIas1 = 0;
        original.WeaponPadI64As1 = 0L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectWeapon.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectAmmo ───────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectAmmo_RoundTrips()
    {
        var original = new ObjectAmmo();
        PopulateItem(original);
        original.AmmoFlags = 0;
        original.AmmoQuantity = 50;
        original.AmmoType = 2;
        original.AmmoPadI1 = 1;
        original.AmmoPadI2 = 2;
        original.AmmoPadIas1 = 3;
        original.AmmoPadI64As1 = 111L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectAmmo.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectArmor ──────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectArmor_RoundTrips()
    {
        var original = new ObjectArmor();
        PopulateItem(original);
        original.ArmorFlags = 4;
        original.ArmorPaperDollAid = 7;
        original.ArmorAcAdj = 5;
        original.ArmorMagicAcAdj = 2;
        original.ArmorResistanceAdj = [1, 2, 3];
        original.ArmorMagicResistanceAdj = [4, 5];
        original.ArmorSilentMoveAdj = -2;
        original.ArmorMagicSilentMoveAdj = 0;
        original.ArmorUnarmedBonusDamage = 1;
        original.ArmorPadI2 = 0;
        original.ArmorPadIas1 = 0;
        original.ArmorPadI64As1 = 0L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectArmor.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectGold ───────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectGold_RoundTrips()
    {
        var original = new ObjectGold();
        PopulateItem(original);
        original.GoldFlags = 0;
        original.GoldQuantity = 100;
        original.GoldPadI1 = 1;
        original.GoldPadI2 = 2;
        original.GoldPadIas1 = 3;
        original.GoldPadI64As1 = 0L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectGold.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectFood ───────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectFood_RoundTrips()
    {
        var original = new ObjectFood();
        PopulateItem(original);
        original.FoodFlags = 1;
        original.FoodPadI1 = 1;
        original.FoodPadI2 = 2;
        original.FoodPadIas1 = 3;
        original.FoodPadI64As1 = 0L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectFood.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectScroll ─────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectScroll_RoundTrips()
    {
        var original = new ObjectScroll();
        PopulateItem(original);
        original.ScrollFlags = 2;
        original.ScrollPadI1 = 1;
        original.ScrollPadI2 = 2;
        original.ScrollPadIas1 = 3;
        original.ScrollPadI64As1 = 0L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectScroll.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectKey ────────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectKey_RoundTrips()
    {
        var original = new ObjectKey();
        PopulateItem(original);
        original.KeyKeyId = 13;
        original.KeyPadI1 = 1;
        original.KeyPadI2 = 2;
        original.KeyPadIas1 = 3;
        original.KeyPadI64As1 = 0L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectKey.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectKeyRing ────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectKeyRing_RoundTrips()
    {
        var original = new ObjectKeyRing();
        PopulateItem(original);
        original.KeyRingFlags = 0;
        original.KeyRingList = [1, 5, 13];
        original.KeyRingPadI1 = 1;
        original.KeyRingPadI2 = 2;
        original.KeyRingPadIas1 = 3;
        original.KeyRingPadI64As1 = 0L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectKeyRing.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectWritten ────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectWritten_RoundTrips()
    {
        var original = new ObjectWritten();
        PopulateItem(original);
        original.WrittenFlags = 3;
        original.WrittenSubtype = 1;
        original.WrittenTextStartLine = 10;
        original.WrittenTextEndLine = 20;
        original.WrittenPadI1 = 1;
        original.WrittenPadI2 = 2;
        original.WrittenPadIas1 = 3;
        original.WrittenPadI64As1 = 0L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectWritten.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectGeneric ────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectGeneric_RoundTrips()
    {
        var original = new ObjectGeneric();
        PopulateItem(original);
        original.GenericFlags = 6;
        original.GenericUsageBonus = 1;
        original.GenericUsageCountRemaining = 5;
        original.GenericPadIas1 = 3;
        original.GenericPadI64As1 = 0L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectGeneric.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectNpc ────────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectNpc_RoundTrips()
    {
        var bitmap = EmptyBitmap;
        var original = new ObjectNpc();
        PopulateCommon(original);
        original.CritterFlags = 7;
        original.CritterFlags2 = 3;
        original.CritterStatBase = [10, 11, 12];
        original.CritterInventoryNum = 1;
        original.CritterInventoryList = [TestGuid];
        original.CritterFollowers = [new GameObjectGuid(9u, 8u, 7u, 6u)];
        original.CritterTeleportDest = TestLocation;
        original.NpcFlags = 1;
        original.NpcLeader = TestGuid;
        original.NpcExperienceWorth = 250;
        original.NpcWaypoints = [TestLocation, new Location(30, 40)];
        original.NpcWaypointCurrent = 0;
        original.NpcStandpointDay = TestLocation;
        original.NpcStandpointNight = new Location(5, 5);
        original.NpcReactionPc = [100, 50];

        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectNpc.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    // ── ObjectPc ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectPc_RoundTrips()
    {
        var bitmap = EmptyBitmap;
        var original = new ObjectPc();
        PopulateCommon(original);
        original.CritterFlags = 5;
        original.CritterInventoryNum = 0;
        original.CritterInventoryList = [];
        original.CritterFollowers = [];
        original.PcFlags = 2;
        original.PcPlayerName = new PrefixedString("TestChar");
        original.PcBankMoney = 9999;
        original.PcReputation = [1, 2];
        original.PcQuest = [10, 20, 30];
        original.PcGlobalFlags = [1, 0, 1];
        original.PcGlobalVariables = [42];

        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectPc.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }
}
