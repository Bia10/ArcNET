using System.Buffers.Binary;
using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;
using static ArcNET.Formats.Tests.SpanWriterTestHelpers;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="MobFormat"/>.</summary>
public sealed class MobFormatTests
{
    // ── ObjectID wire helpers (24 bytes each) ────────────────────────────────────
    // struct ObjectID { int16_t type; int16_t pad2; int pad4; TigGuid g; }  sizeof=0x18
    // OID_TYPE_BLOCKED = -1  (marks a prototype definition)
    // OID_TYPE_GUID    =  2  (GUID-based instance OID)

    private static void WriteOidBlocked(SpanWriter w)
    {
        w.WriteInt16(-1); // OID_TYPE_BLOCKED
        w.WriteInt16(0); // padding_2
        w.WriteInt32(0); // padding_4
        w.WriteBytes(new byte[16]); // TigGuid = zeros
    }

    private static void WriteOidGuid(SpanWriter w, Guid g)
    {
        w.WriteInt16(2); // OID_TYPE_GUID
        w.WriteInt16(0);
        w.WriteInt32(0);
        w.WriteBytes(g.ToByteArray());
    }

    private static void WriteOidRef(SpanWriter w, int protoIndex = 1)
    {
        // OID_TYPE_A = 1 — references a prototype by art-based ID
        w.WriteInt16(1);
        w.WriteInt16(0);
        w.WriteInt32(protoIndex);
        w.WriteBytes(new byte[16]);
    }

    private static ObjectProperty CreateScalarObjectIdProperty(ObjectField field, Guid guid) =>
        new() { Field = field, RawBytes = BuildBytes(w => WriteOidGuid(w, guid)) };

    private static MobData CreateEmptyMob(ObjectType objectType, GameObjectGuid objectId, int protoIndex = 1) =>
        new()
        {
            Header = new GameObjectHeader
            {
                Version = 0x77,
                ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, protoIndex, Guid.Empty),
                ObjectId = objectId,
                GameObjectType = objectType,
                PropCollectionItems = 0,
                Bitmap = new byte[ObjectFieldBitmapSize.For(objectType)],
            },
            Properties = [],
        };

    /// <summary>
    /// Writes a minimal valid MOB header with a Wall object type.
    /// Wall bitmap is 12 bytes. We set bit 21 (Name → Int32).
    /// </summary>
    private static byte[] BuildMinimalWallMob()
    {
        return BuildBytes(w =>
        {
            w.WriteInt32(0x77); // version

            // ProtoId (24 bytes) — non-prototype: OidType != -1.
            WriteOidRef(w, protoIndex: 1);

            // ObjectId (24 bytes) — unique instance GUID
            WriteOidGuid(w, Guid.Parse("00000001-0000-0000-0000-000000000000"));

            // GameObjectType
            w.WriteUInt32((uint)ObjectType.Wall);

            // PropCollectionItems (int16, present because not prototype)
            w.WriteInt16(1);

            // Bitmap — 12 bytes for Wall; set bit 21 (Name)
            var bitmap = new byte[12];
            bitmap[2] = 0x20; // byte 2, bit 5 = bit index 21
            w.WriteBytes(bitmap);

            // Property: Name = Int32 (value = 42)
            w.WriteInt32(42);
        });
    }

    [Test]
    public async Task Parse_MinimalWall_HeaderFieldsCorrect()
    {
        var bytes = BuildMinimalWallMob();
        var mob = MobFormat.ParseMemory(bytes);

        await Assert.That(mob.Header.Version).IsEqualTo(0x77);
        await Assert.That(mob.Header.GameObjectType).IsEqualTo(ObjectType.Wall);
        await Assert.That(mob.Header.IsPrototype).IsFalse();
    }

    [Test]
    public async Task Parse_MinimalWall_OnePropertyRead()
    {
        var bytes = BuildMinimalWallMob();
        var mob = MobFormat.ParseMemory(bytes);

        await Assert.That(mob.Properties.Count).IsEqualTo(1);
        await Assert.That(mob.Properties[0].Field).IsEqualTo(ObjectField.Name);
        await Assert.That(mob.Properties[0].RawBytes.Length).IsEqualTo(4);
    }

    [Test]
    public async Task Parse_BadVersion_Throws()
    {
        var bytes = BuildBytes(w => w.WriteInt32(0x01));
        Assert.Throws<InvalidDataException>(() => MobFormat.ParseMemory(bytes));
    }

    [Test]
    public async Task Parse_TruncatedPresencePrefixedField_SetsParseNoteInsteadOfThrowing()
    {
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0x77);
            WriteOidRef(w, protoIndex: 1);
            WriteOidGuid(w, Guid.Parse("00000003-0000-0000-0000-000000000000"));
            w.WriteUInt32((uint)ObjectType.Wall);
            w.WriteInt16(1);

            var bitmap = new byte[12];
            bitmap[35 / 8] |= (byte)(1 << (35 % 8));
            w.WriteBytes(bitmap);

            w.WriteByte(1);
            w.WriteInt32(1234);
        });

        var mob = MobFormat.ParseMemory(bytes);

        await Assert.That(mob.Properties.Count).IsEqualTo(1);
        await Assert.That(mob.Properties[0].Field).IsEqualTo((ObjectField)35);
        await Assert.That(mob.Properties[0].RawBytes.Length).IsEqualTo(0);
        await Assert.That(mob.Properties[0].ParseNote).IsNotNull();
        await Assert.That(mob.Properties[0].ParseNote!).Contains("fixed-size field data");
    }

    [Test]
    public async Task RoundTrip_MinimalWall_Identical()
    {
        var bytes = BuildMinimalWallMob();
        var original = MobFormat.ParseMemory(bytes);
        var rewritten = MobFormat.WriteToArray(in original);
        var back = MobFormat.ParseMemory(rewritten);

        await Assert.That(back.Header.GameObjectType).IsEqualTo(original.Header.GameObjectType);
        await Assert.That(back.Properties.Count).IsEqualTo(original.Properties.Count);
        await Assert.That(back.Properties[0].Field).IsEqualTo(original.Properties[0].Field);
        await Assert.That(back.Properties[0].RawBytes.SequenceEqual(original.Properties[0].RawBytes)).IsTrue();
    }

    [Test]
    public async Task Parse_EmptyBitmap_NoProperties()
    {
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0x77);
            // ProtoId — non-prototype instance (ref to prototype)
            WriteOidRef(w);
            // ObjectId
            WriteOidGuid(w, Guid.Parse("00000001-0000-0000-0000-000000000000"));
            w.WriteUInt32((uint)ObjectType.Portal);
            w.WriteInt16(0); // PropCollectionItems
            // Portal bitmap — 12 bytes, all zero
            w.WriteBytes(new byte[12]);
        });

        var mob = MobFormat.ParseMemory(bytes);

        await Assert.That(mob.Properties.Count).IsEqualTo(0);
        await Assert.That(mob.Header.GameObjectType).IsEqualTo(ObjectType.Portal);
    }

    [Test]
    public async Task Parse_TwoProperties_BothPresent()
    {
        // Wall MOB with bit 21 (Name, Int32) and bit 23 (Aid, Int32) set.
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0x77);
            WriteOidRef(w);
            WriteOidGuid(w, Guid.Parse("00000002-0000-0000-0000-000000000000"));
            w.WriteUInt32((uint)ObjectType.Wall);
            w.WriteInt16(2); // 2 properties
            // Bitmap 12 bytes: set bits 21 and 23
            var bitmap = new byte[12];
            bitmap[21 / 8] |= (byte)(1 << (21 % 8));
            bitmap[23 / 8] |= (byte)(1 << (23 % 8));
            w.WriteBytes(bitmap);
            w.WriteInt32(999); // Name = 999
            w.WriteInt32(42); // Aid  = 42
        });

        var mob = MobFormat.ParseMemory(bytes);

        await Assert.That(mob.Properties.Count).IsEqualTo(2);
        await Assert.That(mob.Properties[0].Field).IsEqualTo(ObjectField.Name);
        await Assert.That(mob.Properties[0].GetInt32()).IsEqualTo(999);
        await Assert.That(mob.Properties[1].Field).IsEqualTo(ObjectField.Aid);
        await Assert.That(mob.Properties[1].GetInt32()).IsEqualTo(42);
    }

    [Test]
    public async Task Parse_PcReservedScalarPadFields_ParseAsInt32_AndBridgeToGameObject()
    {
        const int padIas2 = 42;
        const int padIas1 = 87;

        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0x77);
            WriteOidRef(w);
            WriteOidGuid(w, Guid.Parse("00000004-0000-0000-0000-000000000000"));
            w.WriteUInt32((uint)ObjectType.Pc);
            w.WriteInt16(2);

            var bitmap = new byte[20];
            bitmap[(int)ObjectField.PcPadIas2 / 8] |= (byte)(1 << ((int)ObjectField.PcPadIas2 % 8));
            bitmap[(int)ObjectField.PcPadIas1 / 8] |= (byte)(1 << ((int)ObjectField.PcPadIas1 % 8));
            w.WriteBytes(bitmap);

            w.WriteInt32(padIas2);
            w.WriteInt32(padIas1);
        });

        var mob = MobFormat.ParseMemory(bytes);
        var bridged = mob.ToGameObject().ToMobData();

        await Assert.That(mob.Properties.Count).IsEqualTo(2);
        await Assert.That(mob.Properties[0].Field).IsEqualTo(ObjectField.PcPadIas2);
        await Assert.That(mob.Properties[0].ParseNote).IsNull();
        await Assert.That(mob.Properties[0].GetInt32()).IsEqualTo(padIas2);
        await Assert.That(mob.Properties[1].Field).IsEqualTo(ObjectField.PcPadIas1);
        await Assert.That(mob.Properties[1].ParseNote).IsNull();
        await Assert.That(mob.Properties[1].GetInt32()).IsEqualTo(padIas1);
        await Assert.That(bridged.GetProperty(ObjectField.PcPadIas2)).IsNotNull();
        await Assert.That(bridged.GetProperty(ObjectField.PcPadIas2)!.GetInt32()).IsEqualTo(padIas2);
        await Assert.That(bridged.GetProperty(ObjectField.PcPadIas1)).IsNotNull();
        await Assert.That(bridged.GetProperty(ObjectField.PcPadIas1)!.GetInt32()).IsEqualTo(padIas1);
    }

    [Test]
    public async Task Parse_PcBackgroundText_MessageIndex_ParsesAsInt32_AndBridgeToGameObject()
    {
        const int backgroundText = 1234;

        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0x77);
            WriteOidRef(w);
            WriteOidGuid(w, Guid.Parse("00000005-0000-0000-0000-000000000000"));
            w.WriteUInt32((uint)ObjectType.Pc);
            w.WriteInt16(1);

            var bitmap = new byte[20];
            bitmap[(int)ObjectField.PcBackgroundText / 8] |= (byte)(1 << ((int)ObjectField.PcBackgroundText % 8));
            w.WriteBytes(bitmap);

            w.WriteInt32(backgroundText);
        });

        var mob = MobFormat.ParseMemory(bytes);
        var bridged = mob.ToGameObject().ToMobData();

        await Assert.That(mob.Properties.Count).IsEqualTo(1);
        await Assert.That(mob.Properties[0].Field).IsEqualTo(ObjectField.PcBackgroundText);
        await Assert.That(mob.Properties[0].ParseNote).IsNull();
        await Assert.That(mob.Properties[0].GetInt32()).IsEqualTo(backgroundText);
        await Assert.That(bridged.GetProperty(ObjectField.PcBackgroundText)).IsNotNull();
        await Assert.That(bridged.GetProperty(ObjectField.PcBackgroundText)!.GetInt32()).IsEqualTo(backgroundText);
    }

    [Test]
    public async Task Parse_NpcLeader_ScalarGuid_ParsesAndBridgeToGameObject()
    {
        var leaderGuid = Guid.Parse("00000006-0000-0000-0000-000000000000");
        var expectedLeaderRaw = BuildBytes(w => WriteOidGuid(w, leaderGuid));

        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0x77);
            WriteOidRef(w);
            WriteOidGuid(w, Guid.Parse("00000007-0000-0000-0000-000000000000"));
            w.WriteUInt32((uint)ObjectType.Npc);
            w.WriteInt16(1);

            var bitmap = new byte[20];
            bitmap[(int)ObjectField.NpcLeader / 8] |= (byte)(1 << ((int)ObjectField.NpcLeader % 8));
            w.WriteBytes(bitmap);

            WriteOidGuid(w, leaderGuid);
        });

        var mob = MobFormat.ParseMemory(bytes);
        var bridged = mob.ToGameObject().ToMobData();

        await Assert.That(mob.Properties.Count).IsEqualTo(1);
        await Assert.That(mob.Properties[0].Field).IsEqualTo(ObjectField.NpcLeader);
        await Assert.That(mob.Properties[0].ParseNote).IsNull();
        await Assert.That(mob.Properties[0].RawBytes.SequenceEqual(expectedLeaderRaw)).IsTrue();
        await Assert.That(bridged.GetProperty(ObjectField.NpcLeader)).IsNotNull();
        await Assert
            .That(bridged.GetProperty(ObjectField.NpcLeader)!.RawBytes.SequenceEqual(expectedLeaderRaw))
            .IsTrue();
    }

    [Test]
    public async Task Parse_NpcSubstituteInventory_ScalarGuid_ParsesAndBridgeToGameObject()
    {
        var substituteInventoryGuid = Guid.Parse("00000008-0000-0000-0000-000000000000");
        var expectedRaw = BuildBytes(w => WriteOidGuid(w, substituteInventoryGuid));

        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0x77);
            WriteOidRef(w);
            WriteOidGuid(w, Guid.Parse("00000009-0000-0000-0000-000000000000"));
            w.WriteUInt32((uint)ObjectType.Npc);
            w.WriteInt16(1);

            var bitmap = new byte[20];
            bitmap[(int)ObjectField.NpcSubstituteInventory / 8] |= (byte)(
                1 << ((int)ObjectField.NpcSubstituteInventory % 8)
            );
            w.WriteBytes(bitmap);

            WriteOidGuid(w, substituteInventoryGuid);
        });

        var mob = MobFormat.ParseMemory(bytes);
        var bridged = mob.ToGameObject().ToMobData();

        await Assert.That(mob.Properties.Count).IsEqualTo(1);
        await Assert.That(mob.Properties[0].Field).IsEqualTo(ObjectField.NpcSubstituteInventory);
        await Assert.That(mob.Properties[0].ParseNote).IsNull();
        await Assert.That(mob.Properties[0].RawBytes.SequenceEqual(expectedRaw)).IsTrue();
        await Assert.That(bridged.GetProperty(ObjectField.NpcSubstituteInventory)).IsNotNull();
        await Assert
            .That(bridged.GetProperty(ObjectField.NpcSubstituteInventory)!.RawBytes.SequenceEqual(expectedRaw))
            .IsTrue();
    }

    [Test]
    public async Task Parse_CritterGold_ScalarGuid_ParsesAndBridgeToGameObject()
    {
        var goldGuid = Guid.Parse("00000018-0000-0000-0000-000000000000");
        var expectedRaw = BuildBytes(w => WriteOidGuid(w, goldGuid));

        var source = CreateEmptyMob(
                ObjectType.Pc,
                new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 0, Guid.Parse("00000017-0000-0000-0000-000000000000"))
            )
            .WithProperty(CreateScalarObjectIdProperty(ObjectField.CritterGold, goldGuid));

        var parsed = MobFormat.ParseMemory(MobFormat.WriteToArray(in source));
        var bridged = parsed.ToGameObject().ToMobData();

        await Assert.That(parsed.GetProperty(ObjectField.CritterGold)).IsNotNull();
        await Assert.That(parsed.GetProperty(ObjectField.CritterGold)!.RawBytes.SequenceEqual(expectedRaw)).IsTrue();
        await Assert.That(parsed.GetProperty(ObjectField.CritterGold)!.GetObjectId().OidType).IsEqualTo((short)2);
        await Assert.That(bridged.GetProperty(ObjectField.CritterGold)).IsNotNull();
        await Assert.That(bridged.GetProperty(ObjectField.CritterGold)!.RawBytes.SequenceEqual(expectedRaw)).IsTrue();
    }

    [Test]
    public async Task Parse_PcQuestPlayerNameAndPadI64_RawSchema_BridgesToGameObject()
    {
        var objectId = new GameObjectGuid(
            GameObjectGuid.OidTypeGuid,
            0,
            0,
            Guid.Parse("00000010-0000-0000-0000-000000000000")
        );
        const long padI64 = 0x1122334455667788L;
        const string playerName = "Virgil";
        var questValues = new[] { 10, 20, 30 };

        var source = CreateEmptyMob(ObjectType.Pc, objectId)
            .WithProperty(ObjectPropertyFactory.ForInt32Array(ObjectField.PcQuestIdx, questValues))
            .WithProperty(ObjectPropertyFactory.ForString(ObjectField.PcPlayerName, playerName))
            .WithProperty(ObjectPropertyFactory.ForInt64(ObjectField.PadI64As1, padI64));

        var parsed = MobFormat.ParseMemory(MobFormat.WriteToArray(in source));
        var bridged = parsed.ToGameObject().ToMobData();

        await Assert.That(parsed.GetProperty(ObjectField.PcQuestIdx)).IsNotNull();
        await Assert
            .That(parsed.GetProperty(ObjectField.PcQuestIdx)!.GetInt32Array().SequenceEqual(questValues))
            .IsTrue();
        await Assert.That(parsed.GetProperty(ObjectField.PcPlayerName)).IsNotNull();
        await Assert.That(parsed.GetProperty(ObjectField.PcPlayerName)!.GetString()).IsEqualTo(playerName);
        await Assert.That(parsed.GetProperty(ObjectField.PadI64As1)).IsNotNull();
        await Assert.That(parsed.GetProperty(ObjectField.PadI64As1)!.GetInt64()).IsEqualTo(padI64);
        await Assert.That(bridged.GetProperty(ObjectField.PcQuestIdx)).IsNotNull();
        var bridgedQuestRaw = bridged.GetProperty(ObjectField.PcQuestIdx)!.RawBytes;
        await Assert.That(bridgedQuestRaw[0]).IsEqualTo((byte)1);
        await Assert.That(BinaryPrimitives.ReadInt32LittleEndian(bridgedQuestRaw.AsSpan(1))).IsEqualTo(16);
        await Assert
            .That(BinaryPrimitives.ReadInt32LittleEndian(bridgedQuestRaw.AsSpan(5)))
            .IsEqualTo(questValues.Length);
        await Assert
            .That(
                questValues
                    .Select(
                        (state, index) =>
                            BinaryPrimitives.ReadInt32LittleEndian(bridgedQuestRaw.AsSpan(21 + (index * 16))) == state
                            && BinaryPrimitives.ReadInt64LittleEndian(bridgedQuestRaw.AsSpan(13 + (index * 16))) == 0
                            && BinaryPrimitives.ReadInt32LittleEndian(bridgedQuestRaw.AsSpan(25 + (index * 16))) == 0
                    )
                    .All(static matches => matches)
            )
            .IsTrue();
        await Assert.That(bridged.GetProperty(ObjectField.PcPlayerName)).IsNotNull();
        await Assert.That(bridged.GetProperty(ObjectField.PcPlayerName)!.GetString()).IsEqualTo(playerName);
        await Assert.That(bridged.GetProperty(ObjectField.PadI64As1)).IsNotNull();
        await Assert.That(bridged.GetProperty(ObjectField.PadI64As1)!.GetInt64()).IsEqualTo(padI64);
    }

    [Test]
    public async Task Parse_NpcFollowerArrayStandpointsAndScalarGuid_RawSchema_BridgesToGameObject()
    {
        var objectId = new GameObjectGuid(
            GameObjectGuid.OidTypeGuid,
            0,
            0,
            Guid.Parse("00000011-0000-0000-0000-000000000000")
        );
        var followerA = Guid.Parse("00000012-0000-0000-0000-000000000000");
        var followerB = Guid.Parse("00000013-0000-0000-0000-000000000000");
        var substituteInventory = Guid.Parse("00000014-0000-0000-0000-000000000000");
        var expectedSubstituteRaw = BuildBytes(w => WriteOidGuid(w, substituteInventory));

        var source = CreateEmptyMob(ObjectType.Npc, objectId)
            .WithProperty(
                ObjectPropertyFactory.ForObjectIdArray(ObjectField.CritterFollowerIdx, [followerA, followerB])
            )
            .WithProperty(ObjectPropertyFactory.ForLocation(ObjectField.NpcStandpointDay, 11, 12))
            .WithProperty(ObjectPropertyFactory.ForLocation(ObjectField.NpcStandpointNight, 13, 14))
            .WithProperty(CreateScalarObjectIdProperty(ObjectField.NpcSubstituteInventory, substituteInventory));

        var parsed = MobFormat.ParseMemory(MobFormat.WriteToArray(in source));
        var bridged = parsed.ToGameObject().ToMobData();

        await Assert.That(parsed.GetProperty(ObjectField.CritterFollowerIdx)).IsNotNull();
        await Assert
            .That(
                parsed
                    .GetProperty(ObjectField.CritterFollowerIdx)!
                    .GetObjectIdArray()
                    .SequenceEqual([followerA, followerB])
            )
            .IsTrue();
        await Assert.That(parsed.GetProperty(ObjectField.NpcStandpointDay)).IsNotNull();
        await Assert.That(parsed.GetProperty(ObjectField.NpcStandpointDay)!.GetLocation()).IsEqualTo((11, 12));
        await Assert.That(parsed.GetProperty(ObjectField.NpcStandpointNight)).IsNotNull();
        await Assert.That(parsed.GetProperty(ObjectField.NpcStandpointNight)!.GetLocation()).IsEqualTo((13, 14));
        await Assert.That(parsed.GetProperty(ObjectField.NpcSubstituteInventory)).IsNotNull();
        await Assert
            .That(parsed.GetProperty(ObjectField.NpcSubstituteInventory)!.RawBytes.SequenceEqual(expectedSubstituteRaw))
            .IsTrue();
        await Assert.That(bridged.GetProperty(ObjectField.CritterFollowerIdx)).IsNotNull();
        await Assert
            .That(
                bridged
                    .GetProperty(ObjectField.CritterFollowerIdx)!
                    .GetObjectIdArray()
                    .SequenceEqual([followerA, followerB])
            )
            .IsTrue();
        await Assert.That(bridged.GetProperty(ObjectField.NpcStandpointDay)).IsNotNull();
        await Assert.That(bridged.GetProperty(ObjectField.NpcStandpointDay)!.GetLocation()).IsEqualTo((11, 12));
        await Assert.That(bridged.GetProperty(ObjectField.NpcStandpointNight)).IsNotNull();
        await Assert.That(bridged.GetProperty(ObjectField.NpcStandpointNight)!.GetLocation()).IsEqualTo((13, 14));
        await Assert.That(bridged.GetProperty(ObjectField.NpcSubstituteInventory)).IsNotNull();
        await Assert
            .That(
                bridged.GetProperty(ObjectField.NpcSubstituteInventory)!.RawBytes.SequenceEqual(expectedSubstituteRaw)
            )
            .IsTrue();
    }

    [Test]
    public async Task Parse_NpcAiData_ScalarInt32_RawSchema_BridgesToGameObject()
    {
        var objectId = new GameObjectGuid(
            GameObjectGuid.OidTypeGuid,
            0,
            0,
            Guid.Parse("00000015-0000-0000-0000-000000000000")
        );
        const int aiData = 314159;

        var source = CreateEmptyMob(ObjectType.Npc, objectId)
            .WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.NpcAiData, aiData));

        var parsed = MobFormat.ParseMemory(MobFormat.WriteToArray(in source));
        var bridged = parsed.ToGameObject().ToMobData();

        await Assert.That(parsed.GetProperty(ObjectField.NpcAiData)).IsNotNull();
        await Assert.That(parsed.GetProperty(ObjectField.NpcAiData)!.ParseNote).IsNull();
        await Assert.That(parsed.GetProperty(ObjectField.NpcAiData)!.GetInt32()).IsEqualTo(aiData);
        await Assert.That(bridged.GetProperty(ObjectField.NpcAiData)).IsNotNull();
        await Assert.That(bridged.GetProperty(ObjectField.NpcAiData)!.GetInt32()).IsEqualTo(aiData);
    }

    [Test]
    public async Task Parse_CritterDeathTime_Int64_RawSchema_BridgesToGameObject()
    {
        var objectId = new GameObjectGuid(
            GameObjectGuid.OidTypeGuid,
            0,
            0,
            Guid.Parse("00000016-0000-0000-0000-000000000000")
        );
        const long deathTime = 0x02E2D5F99C7659E7L;

        var source = CreateEmptyMob(ObjectType.Npc, objectId)
            .WithProperty(ObjectPropertyFactory.ForInt64(ObjectField.CritterDeathTime, deathTime));

        var parsed = MobFormat.ParseMemory(MobFormat.WriteToArray(in source));
        var bridged = parsed.ToGameObject().ToMobData();

        await Assert.That(parsed.GetProperty(ObjectField.CritterDeathTime)).IsNotNull();
        await Assert.That(parsed.GetProperty(ObjectField.CritterDeathTime)!.ParseNote).IsNull();
        await Assert.That(parsed.GetProperty(ObjectField.CritterDeathTime)!.GetInt64()).IsEqualTo(deathTime);
        await Assert.That(bridged.GetProperty(ObjectField.CritterDeathTime)).IsNotNull();
        await Assert.That(bridged.GetProperty(ObjectField.CritterDeathTime)!.GetInt64()).IsEqualTo(deathTime);
    }

    [Test]
    public async Task Container_InventoryListIdx_ParsedAsHandleArray()
    {
        // Container MOB with bit 68 (ContainerInventoryListIdx) holding a 2-item SAR block.
        // Verifies the wire type is HandleArray (SAR of 24-byte ObjectIDs), not Int32.
        var itemGuid1 = Guid.Parse("AAAAAAAA-0000-0000-0000-000000000001");
        var itemGuid2 = Guid.Parse("BBBBBBBB-0000-0000-0000-000000000002");

        // Build the SAR block for 2 ObjectIDs.
        // SAR layout: sarCount(1) + elementSize(4) + elementCount(4) + sarcIndex(4) = 13 bytes header
        //             + 2×24 bytes data + postSize(4) + post(4×postSize) bytes
        var elementSize = (uint)ObjectPropertyExtensions.ObjectIdWireSize; // 24
        const int elementCount = 2;
        const short oidTypeGuid = 2;
        var sarData = new byte[elementCount * (int)elementSize];
        for (var i = 0; i < elementCount; i++)
        {
            var o = i * (int)elementSize;
            System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(sarData.AsSpan(o), oidTypeGuid);
            // padding_2, padding_4 = 0
            (i == 0 ? itemGuid1 : itemGuid2)
                .ToByteArray()
                .CopyTo(sarData, o + 8);
        }

        var sarBytes = new byte[13 + sarData.Length + 4 + 4];
        sarBytes[0] = 1; // sarCount
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(sarBytes.AsSpan(1), elementSize);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(sarBytes.AsSpan(5), elementCount);
        // sarcIndex = 0
        sarData.CopyTo(sarBytes, 13);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(sarBytes.AsSpan(13 + sarData.Length), 1); // postSize = 1
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(
            sarBytes.AsSpan(13 + sarData.Length + 4),
            0xFFFFFFFF
        ); // all bits

        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0x77);
            WriteOidRef(w);
            WriteOidGuid(w, Guid.Parse("CCCCCCCC-0000-0000-0000-000000000003"));
            w.WriteUInt32((uint)ObjectType.Container);
            w.WriteInt16(1); // 1 property

            // Container bitmap (12 bytes): set bit 68 (ContainerInventoryListIdx)
            var bitmap = new byte[12];
            bitmap[68 / 8] |= (byte)(1 << (68 % 8));
            w.WriteBytes(bitmap);

            w.WriteBytes(sarBytes);
        });

        var mob = MobFormat.ParseMemory(bytes);

        await Assert.That(mob.Properties.Count).IsEqualTo(1);
        await Assert.That(mob.Properties[0].Field).IsEqualTo(ObjectField.ContainerInventoryListIdx);

        var ids = mob.Properties[0].GetObjectIdArray();
        await Assert.That(ids.Length).IsEqualTo(2);
        await Assert.That(ids[0]).IsEqualTo(itemGuid1);
        await Assert.That(ids[1]).IsEqualTo(itemGuid2);
    }
}
