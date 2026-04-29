using ArcNET.GameObjects;

namespace ArcNET.Formats.Tests;

public class ObjectPropertyFactoryTests
{
    // ── ForInt32 ──────────────────────────────────────────────────────────────

    [Test]
    public async Task ForInt32_RoundTrips()
    {
        var prop = ObjectPropertyFactory.ForInt32(ObjectField.ObjFHpPts, 42);
        await Assert.That(prop.Field).IsEqualTo(ObjectField.ObjFHpPts);
        await Assert.That(prop.GetInt32()).IsEqualTo(42);
    }

    [Test]
    public async Task ForInt32_NegativeValue_RoundTrips()
    {
        var prop = ObjectPropertyFactory.ForInt32(ObjectField.ObjFHpAdj, -100);
        await Assert.That(prop.GetInt32()).IsEqualTo(-100);
    }

    // ── ForInt64 ──────────────────────────────────────────────────────────────

    [Test]
    public async Task ForInt64_RoundTrips()
    {
        var prop = ObjectPropertyFactory.ForInt64(ObjectField.ObjFPadI64As1, 0x1234567890ABCDEFL);
        await Assert.That(prop.Field).IsEqualTo(ObjectField.ObjFPadI64As1);
        await Assert.That(prop.GetInt64()).IsEqualTo(0x1234567890ABCDEFL);
    }

    // ── ForFloat ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ForFloat_RoundTrips()
    {
        var prop = ObjectPropertyFactory.ForFloat(ObjectField.ObjFSpeedRun, 3.14f);
        await Assert.That(prop.GetFloat()).IsEqualTo(3.14f);
    }

    // ── ForString ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ForString_RoundTrips()
    {
        var prop = ObjectPropertyFactory.ForString(ObjectField.ObjFPcPlayerName, "Roberta");
        await Assert.That(prop.Field).IsEqualTo(ObjectField.ObjFPcPlayerName);
        await Assert.That(prop.GetString()).IsEqualTo("Roberta");
    }

    [Test]
    public async Task ForString_EmptyString_RoundTrips()
    {
        var prop = ObjectPropertyFactory.ForString(ObjectField.ObjFPcPlayerName, string.Empty);
        await Assert.That(prop.GetString()).IsEqualTo(string.Empty);
    }

    // ── ForLocation ───────────────────────────────────────────────────────────

    [Test]
    public async Task ForLocation_RoundTrips()
    {
        var prop = ObjectPropertyFactory.ForLocation(ObjectField.ObjFLocation, 512, 1024);
        var (x, y) = prop.GetLocation();
        await Assert.That(x).IsEqualTo(512);
        await Assert.That(y).IsEqualTo(1024);
    }

    // ── ForInt32Array ─────────────────────────────────────────────────────────

    [Test]
    public async Task ForInt32Array_RoundTrips()
    {
        int[] stats = [10, 12, 8, 14, 10, 9];
        var prop = ObjectPropertyFactory.ForInt32Array(ObjectField.ObjFCritterStatBaseIdx, stats);
        await Assert.That(prop.Field).IsEqualTo(ObjectField.ObjFCritterStatBaseIdx);
        await Assert.That(prop.GetInt32Array().SequenceEqual(stats)).IsTrue();
    }

    [Test]
    public async Task ForInt32Array_EmptyArray_RoundTrips()
    {
        var prop = ObjectPropertyFactory.ForInt32Array(ObjectField.ObjFCritterBasicSkillIdx, []);
        await Assert.That(prop.GetInt32Array().Length).IsEqualTo(0);
    }

    // ── ForInt64Array ─────────────────────────────────────────────────────────

    [Test]
    public async Task ForInt64Array_RoundTrips()
    {
        long[] values = [100L, 200L, 300L];
        var prop = ObjectPropertyFactory.ForInt64Array(ObjectField.ObjFCritterPadI64As1, values);
        await Assert.That(prop.GetInt64Array().SequenceEqual(values)).IsTrue();
    }

    [Test]
    public async Task GetScriptArray_AbsentSar_ReturnsEmptyArray()
    {
        var prop = new ObjectProperty { Field = ObjectField.ObjFScriptsIdx, RawBytes = [0] };

        await Assert.That(prop.GetScriptArray().Length).IsEqualTo(0);
    }

    // ── ForObjectIdArray ──────────────────────────────────────────────────────

    [Test]
    public async Task ForObjectIdArray_RoundTrips()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var prop = ObjectPropertyFactory.ForObjectIdArray(ObjectField.ObjFCritterInventoryListIdx, [id1, id2]);
        await Assert.That(prop.Field).IsEqualTo(ObjectField.ObjFCritterInventoryListIdx);
        var decoded = prop.GetObjectIdArray();
        await Assert.That(decoded.Length).IsEqualTo(2);
        await Assert.That(decoded[0]).IsEqualTo(id1);
        await Assert.That(decoded[1]).IsEqualTo(id2);
    }

    [Test]
    public async Task ForEmptyObjectIdArray_ProducesZeroElements()
    {
        var prop = ObjectPropertyFactory.ForEmptyObjectIdArray(ObjectField.ObjFCritterInventoryListIdx);
        await Assert.That(prop.GetObjectIdArray().Length).IsEqualTo(0);
    }
}
