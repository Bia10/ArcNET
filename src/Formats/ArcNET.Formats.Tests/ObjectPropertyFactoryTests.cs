using System.Buffers.Binary;
using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Formats.Tests;

public class ObjectPropertyFactoryTests
{
    // ── ForInt32 ──────────────────────────────────────────────────────────────

    [Test]
    public async Task ForInt32_RoundTrips()
    {
        var prop = ObjectPropertyFactory.ForInt32(ObjectField.HpPts, 42);
        await Assert.That(prop.Field).IsEqualTo(ObjectField.HpPts);
        await Assert.That(prop.GetInt32()).IsEqualTo(42);
    }

    [Test]
    public async Task ForInt32_NegativeValue_RoundTrips()
    {
        var prop = ObjectPropertyFactory.ForInt32(ObjectField.HpAdj, -100);
        await Assert.That(prop.GetInt32()).IsEqualTo(-100);
    }

    // ── ForInt64 ──────────────────────────────────────────────────────────────

    [Test]
    public async Task ForInt64_RoundTrips()
    {
        var prop = ObjectPropertyFactory.ForInt64(ObjectField.PadI64As1, 0x1234567890ABCDEFL);
        await Assert.That(prop.Field).IsEqualTo(ObjectField.PadI64As1);
        await Assert.That(prop.GetInt64()).IsEqualTo(0x1234567890ABCDEFL);
    }

    // ── ForFloat ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ForFloat_RoundTrips()
    {
        var prop = ObjectPropertyFactory.ForFloat(ObjectField.SpeedRun, 3.14f);
        await Assert.That(prop.GetFloat()).IsEqualTo(3.14f);
    }

    // ── ForString ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ForString_RoundTrips()
    {
        var prop = ObjectPropertyFactory.ForString(ObjectField.PcPlayerName, "Roberta");
        await Assert.That(prop.Field).IsEqualTo(ObjectField.PcPlayerName);
        await Assert.That(prop.GetString()).IsEqualTo("Roberta");
    }

    [Test]
    public async Task ForString_EmptyString_RoundTrips()
    {
        var prop = ObjectPropertyFactory.ForString(ObjectField.PcPlayerName, string.Empty);
        await Assert.That(prop.GetString()).IsEqualTo(string.Empty);
    }

    // ── ForLocation ───────────────────────────────────────────────────────────

    [Test]
    public async Task ForLocation_RoundTrips()
    {
        var prop = ObjectPropertyFactory.ForLocation(ObjectField.Location, 512, 1024);
        var (x, y) = prop.GetLocation();
        await Assert.That(x).IsEqualTo(512);
        await Assert.That(y).IsEqualTo(1024);
    }

    [Test]
    public async Task ForObjectId_RoundTrips()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 123, Guid.NewGuid());
        var prop = ObjectPropertyFactory.ForObjectId(ObjectField.CritterGold, objectId);

        await Assert.That(prop.Field).IsEqualTo(ObjectField.CritterGold);
        await Assert.That(prop.GetObjectId()).IsEqualTo(objectId);
    }

    // ── ForInt32Array ─────────────────────────────────────────────────────────

    [Test]
    public async Task ForInt32Array_RoundTrips()
    {
        int[] stats = [10, 12, 8, 14, 10, 9];
        var prop = ObjectPropertyFactory.ForInt32Array(ObjectField.CritterStatBaseIdx, stats);
        await Assert.That(prop.Field).IsEqualTo(ObjectField.CritterStatBaseIdx);
        await Assert.That(prop.GetInt32Array().SequenceEqual(stats)).IsTrue();
    }

    [Test]
    public async Task ForInt32Array_EmptyArray_RoundTrips()
    {
        var prop = ObjectPropertyFactory.ForInt32Array(ObjectField.CritterBasicSkillIdx, []);
        await Assert.That(prop.GetInt32Array().Length).IsEqualTo(0);
    }

    // ── ForInt64Array ─────────────────────────────────────────────────────────

    [Test]
    public async Task ForInt64Array_RoundTrips()
    {
        long[] values = [100L, 200L, 300L];
        var prop = ObjectPropertyFactory.ForInt64Array(ObjectField.CritterPadI64As1, values);
        await Assert.That(prop.GetInt64Array().SequenceEqual(values)).IsTrue();
    }

    [Test]
    public async Task GetScriptArray_AbsentSar_ReturnsEmptyArray()
    {
        var prop = new ObjectProperty { Field = ObjectField.ScriptsIdx, RawBytes = [0] };

        await Assert.That(prop.GetScriptArray().Length).IsEqualTo(0);
    }

    [Test]
    public async Task GetScriptArray_LegacyScriptIdOnlySar_MapsPositiveIdsAndClearsNegativeSentinels()
    {
        var prop = new ObjectProperty { Field = ObjectField.ScriptsIdx, RawBytes = [0] }.WithInt32Array([
            77,
            -1,
            0,
            88,
        ]);

        var scripts = prop.GetScriptArray();

        await Assert.That(scripts.Length).IsEqualTo(4);
        await Assert.That(scripts[0]).IsEqualTo(new ObjectPropertyScript(0u, 0u, 77));
        await Assert.That(scripts[1]).IsEqualTo(default(ObjectPropertyScript));
        await Assert.That(scripts[2]).IsEqualTo(default(ObjectPropertyScript));
        await Assert.That(scripts[3]).IsEqualTo(new ObjectPropertyScript(0u, 0u, 88));
    }

    [Test]
    public async Task GetScriptArray_MalformedSar_ReturnsEmptyArray()
    {
        var prop = new ObjectProperty
        {
            Field = ObjectField.ScriptsIdx,
            RawBytes =
            [
                1,
                0xFF,
                0xFF,
                0xFF,
                0xFF,
                0xFF,
                0xFF,
                0xFF,
                0xFF,
                0xFF,
                0xFF,
                0xFF,
                0xFF,
                0xFF,
                0xFF,
                0x02,
                0,
                0,
                0,
                0x7F,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
            ],
        };

        await Assert.That(prop.GetScriptArray().Length).IsEqualTo(0);
    }

    [Test]
    public async Task ForInt32Array_EmptyArray_UsesCeCompatibleMinimumBitsetStorage()
    {
        var prop = ObjectPropertyFactory.ForInt32Array(ObjectField.CritterBasicSkillIdx, []);

        await Assert.That(BinaryPrimitives.ReadInt32LittleEndian(prop.RawBytes.AsSpan(13))).IsEqualTo(2);
        await Assert.That(BinaryPrimitives.ReadUInt32LittleEndian(prop.RawBytes.AsSpan(17))).IsEqualTo(0u);
        await Assert.That(BinaryPrimitives.ReadUInt32LittleEndian(prop.RawBytes.AsSpan(21))).IsEqualTo(0u);
    }

    // ── ForObjectIdArray ──────────────────────────────────────────────────────

    [Test]
    public async Task ForObjectIdArray_RoundTrips()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var prop = ObjectPropertyFactory.ForObjectIdArray(ObjectField.CritterInventoryListIdx, [id1, id2]);
        await Assert.That(prop.Field).IsEqualTo(ObjectField.CritterInventoryListIdx);
        var decoded = prop.GetObjectIdArray();
        await Assert.That(decoded.Length).IsEqualTo(2);
        await Assert.That(decoded[0]).IsEqualTo(id1);
        await Assert.That(decoded[1]).IsEqualTo(id2);
    }

    [Test]
    public async Task ForEmptyObjectIdArray_ProducesZeroElements()
    {
        var prop = ObjectPropertyFactory.ForEmptyObjectIdArray(ObjectField.CritterInventoryListIdx);
        await Assert.That(prop.GetObjectIdArray().Length).IsEqualTo(0);
    }
}
