using ArcNET.GameObjects;

namespace ArcNET.GameObjects.Tests;

public class BitArrayObjectExtensionsTests
{
    private static byte[] MakeBitmap(int byteLength) => new byte[byteLength];

    [Test]
    public async Task HasField_UnsetBit_ReturnsFalse()
    {
        var bitmap = MakeBitmap(16);
        await Assert.That(bitmap.HasField(ObjectField.Name)).IsFalse();
    }

    [Test]
    public async Task SetField_True_MakesBitVisible()
    {
        var bitmap = MakeBitmap(16);
        bitmap.SetField(ObjectField.Name, true);
        await Assert.That(bitmap.HasField(ObjectField.Name)).IsTrue();
    }

    [Test]
    public async Task SetField_False_ClearsPreviouslySetBit()
    {
        var bitmap = MakeBitmap(16);
        bitmap.SetField(ObjectField.HpPts, true);
        bitmap.SetField(ObjectField.HpPts, false);
        await Assert.That(bitmap.HasField(ObjectField.HpPts)).IsFalse();
    }

    [Test]
    public async Task HasField_IndexZero_WorksForFirstCommonField()
    {
        var bitmap = MakeBitmap(16);
        bitmap.SetField(ObjectField.CurrentAid, true);
        await Assert.That(bitmap.HasField(ObjectField.CurrentAid)).IsTrue();
    }

    [Test]
    public async Task SetField_MultipleFields_IndependentBits()
    {
        var bitmap = MakeBitmap(20);
        bitmap.SetField(ObjectField.Name, true);
        bitmap.SetField(ObjectField.HpPts, true);

        await Assert.That(bitmap.HasField(ObjectField.Name)).IsTrue();
        await Assert.That(bitmap.HasField(ObjectField.HpPts)).IsTrue();
        await Assert.That(bitmap.HasField(ObjectField.Material)).IsFalse();
    }

    [Test]
    public async Task HasField_TypeSpecificField_ReflectsCorrectBit()
    {
        // Weapon fields start at bit 96 — needs 16-byte bitmap (128 bits)
        var bitmap = MakeBitmap(16);
        bitmap.SetField(ObjectField.WeaponFlags, true);
        await Assert.That(bitmap.HasField(ObjectField.WeaponFlags)).IsTrue();
        await Assert.That(bitmap.HasField(ObjectField.WeaponRange)).IsFalse();
    }

    [Test]
    public async Task SetField_HighBitIndex_WorksForPcType()
    {
        // PC fields at bit 128–152 — needs 20-byte bitmap (160 bits)
        var bitmap = MakeBitmap(20);
        bitmap.SetField(ObjectField.PcFlags, true);
        bitmap.SetField(ObjectField.PcPlayerName, true);

        await Assert.That(bitmap.HasField(ObjectField.PcFlags)).IsTrue();
        await Assert.That(bitmap.HasField(ObjectField.PcPlayerName)).IsTrue();
        await Assert.That(bitmap.HasField(ObjectField.PcBankMoney)).IsFalse();
    }
}
