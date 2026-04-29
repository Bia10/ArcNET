using System.Buffers;
using System.Reflection;
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
    // A bitmap large enough for all ObjectField values (max = 152) — 20 bytes = 160 bits.
    private static byte[] EmptyBitmap => new byte[20];

    private static readonly ArtId TestArtId = new(0xABCDEF01u);
    private static readonly Location TestLocation = new(10, 20);
    private static readonly Color TestColor = new(255, 128, 64);
    private static readonly GameObjectGuid TestGuid = new(
        (short)1,
        (short)2,
        3,
        new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4)
    );

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
        obj.ObjectFlags = ObjFFlags.Flat | ObjFFlags.Translucent | ObjFFlags.Inventory;
        obj.SpellFlags = ObjFSpellFlags.Invisible | ObjFSpellFlags.DetectingMagic | ObjFSpellFlags.Shielded;
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
            // 0x04030201u = bytes [0x01, 0x02, 0x03, 0x04] in LE layout
            new GameObjectScript(Counters: 0x04030201u, Flags: 1, ScriptId: 777),
        ];
        obj.SoundEffect = 55;
        obj.Category = 7;
        obj.CommonPadIas1Reserved = 0xDEAD;
        obj.CommonPadI64As1Reserved = 0x123456789ABCDE0L;
    }

    private static void PopulateItem(ObjectItem obj)
    {
        PopulateCommon(obj);
        obj.ItemFlags = ObjFItemFlags.Identified | ObjFItemFlags.IsMagical;
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
        SetReserved<ObjectItem>(obj, "_itemPadI1Reserved", 0xBEEF);
        SetReserved<ObjectItem>(obj, "_itemPadIas1Reserved", 0xC0DE);
        SetReserved<ObjectItem>(obj, "_itemPadI64As1Reserved", 0xFEDCBA987654320L);
    }

    private static byte[] WriteAndCapture(Action<SpanWriter> write)
    {
        var buf = new ArrayBufferWriter<byte>(512);
        var writer = new SpanWriter(buf);
        write(writer);
        return buf.WrittenMemory.ToArray();
    }

    private static GameObject RoundTripGameObject(ObjectType objectType, ObjectCommon common)
    {
        var gameObject = new GameObject
        {
            Header = new GameObjectHeader
            {
                Version = 0x77,
                ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeBlocked, 0, 0, Guid.Empty),
                ObjectId = TestGuid,
                GameObjectType = objectType,
                Bitmap = new byte[ObjectFieldBitmapSize.For(objectType)],
            },
            Common = common,
        };

        var bytes = gameObject.WriteToArray();
        var reader = new SpanReader(bytes);
        return GameObject.Read(ref reader);
    }

    private static void PopulateCritterReserved(ObjectCritter obj)
    {
        obj.CritterPadI1Reserved = 0x1111;
        obj.CritterPadI2Reserved = 0x2222;
        obj.CritterPadI3Reserved = 0x3333;
        obj.CritterPadIas1Reserved = 0x4444;
        obj.CritterPadI64As1Reserved = 0x5555666677778888L;
    }

    private static void PopulatePcReserved(ObjectPc obj)
    {
        obj.PcPadIas2Reserved = 0x9090;
        obj.PcPadI1Reserved = 0xA0A0;
        obj.PcPadI2Reserved = 0xB0B0;
        obj.PcPadIas1Reserved = 0xC0C0;
        obj.PcPadI64As1Reserved = 0x1111222233334444L;
    }

    private static void PopulateNpcReserved(ObjectNpc obj) => obj.NpcPadI1Reserved = 0x8181;

    private static void SetReserved<TDeclaring>(object target, string fieldName, object value)
    {
        var field =
            typeof(TDeclaring).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"Missing reserved field '{fieldName}' on {typeof(TDeclaring).Name}."
            );
        field.SetValue(target, value);
    }

    // ── ObjectWall ───────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectWall_RoundTrips()
    {
        var original = new ObjectWall();
        PopulateCommon(original);
        original.WallFlags = 0xFF;
        SetReserved<ObjectWall>(original, "_wallPadI1Reserved", 1);
        SetReserved<ObjectWall>(original, "_wallPadI2Reserved", 2);
        SetReserved<ObjectWall>(original, "_wallPadIas1Reserved", 3);
        SetReserved<ObjectWall>(original, "_wallPadI64As1Reserved", 999L);

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
        original.PortalFlags = ObjFPortalFlags.Locked;
        original.LockDifficulty = 50;
        original.KeyId = 7;
        original.NotifyNpc = 1;
        SetReserved<ObjectPortal>(original, "_portalPadI1Reserved", 2);
        SetReserved<ObjectPortal>(original, "_portalPadI2Reserved", 3);
        SetReserved<ObjectPortal>(original, "_portalPadIas1Reserved", 4);
        SetReserved<ObjectPortal>(original, "_portalPadI64As1Reserved", 888L);

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
        original.ContainerFlags = ObjFContainerFlags.Jammed;
        original.LockDifficulty = 30;
        original.KeyId = 5;
        original.InventoryList =
        [
            TestGuid,
            new GameObjectGuid((short)5, (short)6, 7, new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 8)),
        ];
        original.InventorySource = 1;
        original.NotifyNpc = 0;
        original.ContainerPadI1Reserved = 1;
        original.ContainerPadI2Reserved = 2;
        original.ContainerPadIas1Reserved = 3;
        original.ContainerPadI64As1Reserved = 777L;

        var bitmap = EmptyBitmap;
        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectContainer.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    [Test]
    public async Task ObjectContainer_SeparatesObjectFlagsAndContainerFlags()
    {
        ObjectCommon common = new ObjectContainer();
        common.ObjectFlags = ObjFFlags.Dynamic | ObjFFlags.Inventory;

        var container = (ObjectContainer)common;
        container.ContainerFlags = ObjFContainerFlags.Locked | ObjFContainerFlags.Jammed;

        await Assert.That(common.ObjectFlags).IsEqualTo(ObjFFlags.Dynamic | ObjFFlags.Inventory);
        await Assert.That(container.ContainerFlags).IsEqualTo(ObjFContainerFlags.Locked | ObjFContainerFlags.Jammed);
    }

    // ── ObjectScenery ────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectScenery_RoundTrips()
    {
        var original = new ObjectScenery();
        PopulateCommon(original);
        original.SceneryFlags = ObjFSceneryFlags.Busted | ObjFSceneryFlags.MarksTownmap;
        original.WhosInMe = TestGuid;
        original.RespawnDelay = 120;
        SetReserved<ObjectScenery>(original, "_sceneryPadI2Reserved", 1);
        SetReserved<ObjectScenery>(original, "_sceneryPadIas1Reserved", 2);
        SetReserved<ObjectScenery>(original, "_sceneryPadI64As1Reserved", 555L);

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
        original.CombatFlags = 3;
        original.CombatDamageFlags = 15;
        original.HitLoc = TestLocation;
        original.ParentWeapon = 42;
        SetReserved<ObjectProjectile>(original, "_projectilePadI1Reserved", 1);
        SetReserved<ObjectProjectile>(original, "_projectilePadI2Reserved", 2);
        SetReserved<ObjectProjectile>(original, "_projectilePadIas1Reserved", 3);
        SetReserved<ObjectProjectile>(original, "_projectilePadI64As1Reserved", 444L);

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
        original.Difficulty = 80;
        SetReserved<ObjectTrap>(original, "_trapPadI2Reserved", 1);
        SetReserved<ObjectTrap>(original, "_trapPadIas1Reserved", 2);
        SetReserved<ObjectTrap>(original, "_trapPadI64As1Reserved", 333L);

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
        original.WeaponFlags = ObjFWeaponFlags.Loud;
        original.PaperDollAid = 2;
        original.BonusToHit = 3;
        original.MagicHitAdj = 1;
        original.DamageLower = [5, 6];
        original.DamageUpper = [12, 15];
        original.MagicDamageAdj = [2];
        original.SpeedFactor = 4;
        original.MagicSpeedAdj = 1;
        original.Range = 2;
        original.MagicRangeAdj = 0;
        original.MinStrength = 6;
        original.MagicMinStrengthAdj = 0;
        original.AmmoType = 1;
        original.AmmoConsumption = 1;
        original.MissileAid = 0;
        original.VisualEffectAid = 0;
        original.CritHitChart = 1;
        original.MagicCritHitChance = 5;
        original.MagicCritHitEffect = 2;
        original.CritMissChart = 1;
        original.MagicCritMissChance = 3;
        original.MagicCritMissEffect = 1;
        SetReserved<ObjectWeapon>(original, "_weaponPadI1Reserved", 0x5151);
        SetReserved<ObjectWeapon>(original, "_weaponPadI2Reserved", 0x5252);
        SetReserved<ObjectWeapon>(original, "_weaponPadIas1Reserved", 0x5353);
        SetReserved<ObjectWeapon>(original, "_weaponPadI64As1Reserved", 0x5454545454545454L);

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
        original.Quantity = 50;
        original.Type = 2;
        SetReserved<ObjectAmmo>(original, "_ammoPadI1Reserved", 1);
        SetReserved<ObjectAmmo>(original, "_ammoPadI2Reserved", 2);
        SetReserved<ObjectAmmo>(original, "_ammoPadIas1Reserved", 3);
        SetReserved<ObjectAmmo>(original, "_ammoPadI64As1Reserved", 111L);

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
        original.ArmorFlags = ObjFArmorFlags.SizeLarge;
        original.PaperDollAid = 7;
        original.AcAdj = 5;
        original.MagicAcAdj = 2;
        original.ResistanceAdj = [1, 2, 3];
        original.MagicResistanceAdj = [4, 5];
        original.SilentMoveAdj = -2;
        original.MagicSilentMoveAdj = 0;
        original.UnarmedBonusDamage = 1;
        SetReserved<ObjectArmor>(original, "_armorPadI2Reserved", 0x6161);
        SetReserved<ObjectArmor>(original, "_armorPadIas1Reserved", 0x6262);
        SetReserved<ObjectArmor>(original, "_armorPadI64As1Reserved", 0x6363636363636363L);

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
        original.Quantity = 100;
        SetReserved<ObjectGold>(original, "_goldPadI1Reserved", 1);
        SetReserved<ObjectGold>(original, "_goldPadI2Reserved", 2);
        SetReserved<ObjectGold>(original, "_goldPadIas1Reserved", 3);
        SetReserved<ObjectGold>(original, "_goldPadI64As1Reserved", 0x6464646464646464L);

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
        SetReserved<ObjectFood>(original, "_foodPadI1Reserved", 1);
        SetReserved<ObjectFood>(original, "_foodPadI2Reserved", 2);
        SetReserved<ObjectFood>(original, "_foodPadIas1Reserved", 3);
        SetReserved<ObjectFood>(original, "_foodPadI64As1Reserved", 0x6565656565656565L);

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
        SetReserved<ObjectScroll>(original, "_scrollPadI1Reserved", 1);
        SetReserved<ObjectScroll>(original, "_scrollPadI2Reserved", 2);
        SetReserved<ObjectScroll>(original, "_scrollPadIas1Reserved", 3);
        SetReserved<ObjectScroll>(original, "_scrollPadI64As1Reserved", 0x6666666666666666L);

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
        original.KeyId = 13;
        SetReserved<ObjectKey>(original, "_keyPadI1Reserved", 1);
        SetReserved<ObjectKey>(original, "_keyPadI2Reserved", 2);
        SetReserved<ObjectKey>(original, "_keyPadIas1Reserved", 3);
        SetReserved<ObjectKey>(original, "_keyPadI64As1Reserved", 0x6767676767676767L);

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
        original.List = [1, 5, 13];
        SetReserved<ObjectKeyRing>(original, "_keyRingPadI1Reserved", 1);
        SetReserved<ObjectKeyRing>(original, "_keyRingPadI2Reserved", 2);
        SetReserved<ObjectKeyRing>(original, "_keyRingPadIas1Reserved", 3);
        SetReserved<ObjectKeyRing>(original, "_keyRingPadI64As1Reserved", 0x6868686868686868L);

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
        original.Subtype = 1;
        original.TextStartLine = 10;
        original.TextEndLine = 20;
        SetReserved<ObjectWritten>(original, "_writtenPadI1Reserved", 1);
        SetReserved<ObjectWritten>(original, "_writtenPadI2Reserved", 2);
        SetReserved<ObjectWritten>(original, "_writtenPadIas1Reserved", 3);
        SetReserved<ObjectWritten>(original, "_writtenPadI64As1Reserved", 0x6969696969696969L);

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
        original.UsageBonus = 1;
        original.UsageCountRemaining = 5;
        SetReserved<ObjectGeneric>(original, "_genericPadIas1Reserved", 3);
        SetReserved<ObjectGeneric>(original, "_genericPadI64As1Reserved", 0x7070707070707070L);

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
        original.CritterFlags = ObjFCritterFlags.Undead | ObjFCritterFlags.Animal | ObjFCritterFlags.Sleeping;
        original.CritterFlags2 = ObjFCritterFlags2.AutoAnimates | ObjFCritterFlags2.UsingBoomerang;
        original.CritterStatBase = [10, 11, 12];
        original.CritterInventoryList = [TestGuid];
        original.CritterFollowers =
        [
            new GameObjectGuid((short)9, (short)8, 7, new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6)),
        ];
        original.CritterTeleportDest = TestLocation;
        PopulateCritterReserved(original);
        original.NpcFlags = ObjFNpcFlags.Fighting;
        original.Leader = TestGuid;
        original.ExperienceWorth = 250;
        original.Waypoints = [TestLocation, new Location(30, 40)];
        original.WaypointCurrent = 0;
        original.StandpointDay = TestLocation;
        original.StandpointNight = new Location(5, 5);
        original.ReactionPc = [100, 50];
        PopulateNpcReserved(original);

        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectNpc.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    [Test]
    public async Task ObjectCritter_RestoresSemanticFieldsAfterRoundTrip()
    {
        var bitmap = EmptyBitmap;
        var original = new ObjectCritter();
        PopulateCommon(original);
        original.CritterFlags = ObjFCritterFlags.Undead | ObjFCritterFlags.NoFlee;
        original.CritterFlags2 = ObjFCritterFlags2.DarkSight | ObjFCritterFlags2.NoDecay;
        original.CritterStatBase = [10, 11, 12];
        original.CritterEffects = [5, 6];
        original.CritterFleeingFrom = TestGuid;
        original.CritterGold = 77;
        original.CritterInventoryList = [TestGuid];
        original.CritterFollowers =
        [
            new GameObjectGuid((short)9, (short)8, 7, new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6)),
        ];
        original.CritterTeleportDest = TestLocation;
        original.CritterTeleportMap = 5;

        var bytes = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes);
        var restored = ObjectCritter.Read(ref reader, bitmap, isPrototype: true);

        await Assert.That(restored.CurrentAid).IsEqualTo(TestArtId);
        await Assert.That(restored.ObjectFlags).IsEqualTo(ObjFFlags.Flat | ObjFFlags.Translucent | ObjFFlags.Inventory);
        await Assert.That(restored.CritterFlags).IsEqualTo(ObjFCritterFlags.Undead | ObjFCritterFlags.NoFlee);
        await Assert.That(restored.CritterFlags2).IsEqualTo(ObjFCritterFlags2.DarkSight | ObjFCritterFlags2.NoDecay);
        await Assert.That(restored.CritterStatBase).IsEquivalentTo([10, 11, 12]);
        await Assert.That(restored.CritterEffects).IsEquivalentTo([5, 6]);
        await Assert.That(restored.CritterFleeingFrom).IsEqualTo(TestGuid);
        await Assert.That(restored.CritterGold).IsEqualTo(77);
        await Assert.That(restored.CritterInventoryList).IsEquivalentTo([TestGuid]);
        await Assert.That(restored.CritterFollowers.Length).IsEqualTo(1);
        await Assert.That(restored.CritterTeleportDest).IsEqualTo(TestLocation);
        await Assert.That(restored.CritterTeleportMap).IsEqualTo(5);
    }

    [Test]
    public async Task GameObject_NpcRoundTrip_RestoresSemanticFields()
    {
        var original = new ObjectNpc();
        PopulateCommon(original);
        original.CritterFlags = ObjFCritterFlags.Undead | ObjFCritterFlags.Animal;
        original.CritterInventoryList = [TestGuid];
        original.CritterFollowers = [];
        original.CritterTeleportDest = TestLocation;
        original.NpcFlags = ObjFNpcFlags.Fighting;
        original.Leader = TestGuid;
        original.ExperienceWorth = 250;
        original.Waypoints = [TestLocation, new Location(30, 40)];
        original.ReactionPc = [100, 50];

        var restoredGameObject = RoundTripGameObject(ObjectType.Npc, original);

        await Assert.That(restoredGameObject.Common).IsTypeOf<ObjectNpc>();
        var restored = (ObjectNpc)restoredGameObject.Common;

        await Assert.That(restoredGameObject.Type).IsEqualTo(ObjectType.Npc);
        await Assert.That(restored.CurrentAid).IsEqualTo(TestArtId);
        await Assert.That(restored.CritterFlags).IsEqualTo(ObjFCritterFlags.Undead | ObjFCritterFlags.Animal);
        await Assert.That(restored.CritterInventoryList).IsEquivalentTo([TestGuid]);
        await Assert.That(restored.NpcFlags).IsEqualTo(ObjFNpcFlags.Fighting);
        await Assert.That(restored.Leader).IsEqualTo(TestGuid);
        await Assert.That(restored.ExperienceWorth).IsEqualTo(250);
        await Assert.That(restored.Waypoints).IsEquivalentTo([TestLocation, new Location(30, 40)]);
        await Assert.That(restored.ReactionPc).IsEquivalentTo([100, 50]);
    }

    // ── ObjectPc ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ObjectPc_RoundTrips()
    {
        var bitmap = EmptyBitmap;
        var original = new ObjectPc();
        PopulateCommon(original);
        original.CritterFlags = ObjFCritterFlags.IsConcealed | ObjFCritterFlags.Undead;
        original.CritterGold = 166;
        original.CritterArrows = 60;
        original.CritterBullets = 100;
        original.CritterPowerCells = 4;
        original.CritterFuel = 2;
        original.CritterInventoryList = [];
        original.CritterFollowers = [];
        PopulateCritterReserved(original);
        original.PcFlags = 2;
        original.PlayerName = new PrefixedString("TestChar");
        original.BankMoney = 9999;
        original.Reputation = [1, 2];
        original.Quest = [10, 20, 30];
        original.GlobalFlags = [1, 0, 1];
        original.GlobalVariables = [42];
        PopulatePcReserved(original);

        var bytes1 = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: true));
        var reader = new SpanReader(bytes1);
        var restored = ObjectPc.Read(ref reader, bitmap, isPrototype: true);
        var bytes2 = WriteAndCapture(w => restored.Write(ref w, bitmap, isPrototype: true));
        await Assert.That(bytes1.SequenceEqual(bytes2)).IsTrue();
    }

    [Test]
    public async Task GameObject_PcRoundTrip_RestoresSemanticFields()
    {
        var original = new ObjectPc();
        PopulateCommon(original);
        original.CritterFlags = ObjFCritterFlags.IsConcealed | ObjFCritterFlags.Undead;
        original.CritterGold = 166;
        original.CritterArrows = 60;
        original.CritterBullets = 100;
        original.CritterPowerCells = 4;
        original.CritterFuel = 2;
        original.CritterInventoryList = [];
        original.CritterFollowers = [];
        original.PcFlags = 2;
        original.PlayerName = new PrefixedString("TestChar");
        original.BankMoney = 9999;
        original.Reputation = [1, 2];
        original.Quest = [10, 20, 30];
        original.GlobalFlags = [1, 0, 1];
        original.GlobalVariables = [42];

        var restoredGameObject = RoundTripGameObject(ObjectType.Pc, original);

        await Assert.That(restoredGameObject.Common).IsTypeOf<ObjectPc>();
        var restored = (ObjectPc)restoredGameObject.Common;

        await Assert.That(restoredGameObject.Type).IsEqualTo(ObjectType.Pc);
        await Assert.That(restored.CurrentAid).IsEqualTo(TestArtId);
        await Assert.That(restored.CritterFlags).IsEqualTo(ObjFCritterFlags.IsConcealed | ObjFCritterFlags.Undead);
        await Assert.That(restored.CritterGold).IsEqualTo(166);
        await Assert.That(restored.CritterArrows).IsEqualTo(60);
        await Assert.That(restored.PcFlags).IsEqualTo(2);
        await Assert.That(restored.PlayerName).IsEqualTo(new PrefixedString("TestChar"));
        await Assert.That(restored.BankMoney).IsEqualTo(9999);
        await Assert.That(restored.Reputation).IsEquivalentTo([1, 2]);
        await Assert.That(restored.Quest).IsEquivalentTo([10, 20, 30]);
        await Assert.That(restored.GlobalFlags).IsEquivalentTo([1, 0, 1]);
        await Assert.That(restored.GlobalVariables).IsEquivalentTo([42]);
    }

    [Test]
    public async Task ObjectTypeRegistry_Read_RejectsUnsupportedObjectType()
    {
        var threw = false;

        try
        {
            var reader = new SpanReader(Array.Empty<byte>());
            ObjectTypeRegistry.Read((ObjectType)255, ref reader, Array.Empty<byte>(), isPrototype: true);
        }
        catch (InvalidDataException)
        {
            threw = true;
        }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task GameObject_RejectsSupportedHeaderBodyMismatch()
    {
        var body = new ObjectNpc();
        PopulateCommon(body);

        await Assert
            .That(() =>
                new GameObject
                {
                    Header = new GameObjectHeader
                    {
                        Version = 0x77,
                        ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeBlocked, 0, 0, Guid.Empty),
                        ObjectId = TestGuid,
                        GameObjectType = ObjectType.Pc,
                        Bitmap = new byte[ObjectFieldBitmapSize.For(ObjectType.Pc)],
                    },
                    Common = body,
                }
            )
            .Throws<InvalidDataException>();
    }

    [Test]
    public async Task GameObject_RejectsUnsupportedCommonType()
    {
        await Assert
            .That(() =>
                new GameObject
                {
                    Header = new GameObjectHeader
                    {
                        Version = 0x77,
                        ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeBlocked, 0, 0, Guid.Empty),
                        ObjectId = TestGuid,
                        GameObjectType = ObjectType.Generic,
                        Bitmap = new byte[ObjectFieldBitmapSize.For(ObjectType.Generic)],
                    },
                    Common = new ObjectUnknown(),
                }
            )
            .Throws<InvalidDataException>();
    }

    [Test]
    public async Task GameObject_WriteToArray_AllowsMatchingHeaderBodyType()
    {
        var body = new ObjectPc();
        PopulateCommon(body);

        var gameObject = new GameObject
        {
            Header = new GameObjectHeader
            {
                Version = 0x77,
                ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeBlocked, 0, 0, Guid.Empty),
                ObjectId = TestGuid,
                GameObjectType = ObjectType.Pc,
                Bitmap = new byte[ObjectFieldBitmapSize.For(ObjectType.Pc)],
            },
            Common = body,
        };

        var threw = false;

        try
        {
            _ = gameObject.WriteToArray();
        }
        catch (InvalidDataException)
        {
            threw = true;
        }

        await Assert.That(threw).IsFalse();
    }

    [Test]
    public async Task GameObject_WriteToArray_AllowsSupportedCommonType()
    {
        var body = new ObjectGeneric();
        PopulateCommon(body);

        var gameObject = new GameObject
        {
            Header = new GameObjectHeader
            {
                Version = 0x77,
                ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeBlocked, 0, 0, Guid.Empty),
                ObjectId = TestGuid,
                GameObjectType = ObjectType.Generic,
                Bitmap = new byte[ObjectFieldBitmapSize.For(ObjectType.Generic)],
            },
            Common = body,
        };

        var threw = false;

        try
        {
            _ = gameObject.WriteToArray();
        }
        catch (InvalidDataException)
        {
            threw = true;
        }

        await Assert.That(threw).IsFalse();
    }

    [Test]
    public async Task ObjectContainer_UsesInventoryListLength_WhenInventoryListIsSerialized()
    {
        var bitmap = EmptyBitmap;
        bitmap.SetField(ObjectField.ObjFContainerInventoryNum, true);
        bitmap.SetField(ObjectField.ObjFContainerInventoryListIdx, true);

        var original = new ObjectContainer();
        original.InventoryList = [TestGuid, TestGuid];
        original.InventoryCountReserved = 99;

        var bytes = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: false));
        var reader = new SpanReader(bytes);
        var restored = ObjectContainer.Read(ref reader, bitmap, isPrototype: false);

        await Assert.That(restored.InventoryList.Length).IsEqualTo(2);
    }

    [Test]
    public async Task ObjectCritter_UsesInventoryListLength_WhenInventoryListIsSerialized()
    {
        var bitmap = EmptyBitmap;
        bitmap.SetField(ObjectField.ObjFCritterInventoryNum, true);
        bitmap.SetField(ObjectField.ObjFCritterInventoryListIdx, true);

        var original = new ObjectCritter();
        original.CritterInventoryList = [TestGuid];
        original.InventoryCountReserved = 99;

        var bytes = WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: false));
        var reader = new SpanReader(bytes);
        var restored = ObjectCritter.Read(ref reader, bitmap, isPrototype: false);

        await Assert.That(restored.CritterInventoryList.Length).IsEqualTo(1);
    }

    [Test]
    public async Task ObjectContainer_RejectsInventoryListBitmapWithoutCount()
    {
        var bitmap = EmptyBitmap;
        bitmap.SetField(ObjectField.ObjFContainerInventoryListIdx, true);

        var original = new ObjectContainer();
        original.InventoryList = [TestGuid];

        await Assert
            .That(() => WriteAndCapture(w => original.Write(ref w, bitmap, isPrototype: false)))
            .Throws<InvalidOperationException>();
    }
}
