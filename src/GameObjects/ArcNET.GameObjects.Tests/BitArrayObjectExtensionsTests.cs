using ArcNET.GameObjects;

namespace ArcNET.GameObjects.Tests;

public class BitArrayObjectExtensionsTests
{
    private static byte[] MakeBitmap(int byteLength) => new byte[byteLength];

    [Test]
    public async Task HasField_UnsetBit_ReturnsFalse()
    {
        var bitmap = MakeBitmap(16);
        await Assert.That(bitmap.HasField(ObjectField.ObjFName)).IsFalse();
    }

    [Test]
    public async Task SetField_True_MakesBitVisible()
    {
        var bitmap = MakeBitmap(16);
        bitmap.SetField(ObjectField.ObjFName, true);
        await Assert.That(bitmap.HasField(ObjectField.ObjFName)).IsTrue();
    }

    [Test]
    public async Task SetField_False_ClearsPreviouslySetBit()
    {
        var bitmap = MakeBitmap(16);
        bitmap.SetField(ObjectField.ObjFHpPts, true);
        bitmap.SetField(ObjectField.ObjFHpPts, false);
        await Assert.That(bitmap.HasField(ObjectField.ObjFHpPts)).IsFalse();
    }

    [Test]
    public async Task HasField_IndexZero_WorksForFirstCommonField()
    {
        var bitmap = MakeBitmap(16);
        bitmap.SetField(ObjectField.ObjFCurrentAid, true);
        await Assert.That(bitmap.HasField(ObjectField.ObjFCurrentAid)).IsTrue();
    }

    [Test]
    public async Task SetField_MultipleFields_IndependentBits()
    {
        var bitmap = MakeBitmap(20);
        bitmap.SetField(ObjectField.ObjFName, true);
        bitmap.SetField(ObjectField.ObjFHpPts, true);

        await Assert.That(bitmap.HasField(ObjectField.ObjFName)).IsTrue();
        await Assert.That(bitmap.HasField(ObjectField.ObjFHpPts)).IsTrue();
        await Assert.That(bitmap.HasField(ObjectField.ObjFMaterial)).IsFalse();
    }

    [Test]
    public async Task HasField_TypeSpecificField_ReflectsCorrectBit()
    {
        // Weapon fields start at bit 96 — needs 16-byte bitmap (128 bits)
        var bitmap = MakeBitmap(16);
        bitmap.SetField(ObjectField.ObjFWeaponFlags, true);
        await Assert.That(bitmap.HasField(ObjectField.ObjFWeaponFlags)).IsTrue();
        await Assert.That(bitmap.HasField(ObjectField.ObjFWeaponRange)).IsFalse();
    }

    [Test]
    public async Task SetField_HighBitIndex_WorksForPcType()
    {
        // PC fields at bit 128–152 — needs 20-byte bitmap (160 bits)
        var bitmap = MakeBitmap(20);
        bitmap.SetField(ObjectField.ObjFPcFlags, true);
        bitmap.SetField(ObjectField.ObjFPcPlayerName, true);

        await Assert.That(bitmap.HasField(ObjectField.ObjFPcFlags)).IsTrue();
        await Assert.That(bitmap.HasField(ObjectField.ObjFPcPlayerName)).IsTrue();
        await Assert.That(bitmap.HasField(ObjectField.ObjFPcBankMoney)).IsFalse();
    }
}
