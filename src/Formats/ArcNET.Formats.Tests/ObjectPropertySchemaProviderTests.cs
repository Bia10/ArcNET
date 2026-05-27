using ArcNET.GameObjects;

namespace ArcNET.Formats.Tests;

public sealed class ObjectPropertySchemaProviderTests
{
    [Test]
    public async Task ResolveWireType_CommonBit_UsesSharedTable()
    {
        var wireType = ObjectPropertySchemaProvider.Default.ResolveWireType(ObjectType.Wall, 31);

        await Assert.That(wireType).IsEqualTo(ObjectWireType.ScriptArray);
    }

    [Test]
    public async Task ResolveWireType_ItemDerivedType_ReusesItemBaseBits()
    {
        var wireType = ObjectPropertySchemaProvider.Default.ResolveWireType(ObjectType.Weapon, 85);

        await Assert.That(wireType).IsEqualTo(ObjectWireType.Int32Array);
    }

    [Test]
    public async Task ResolveWireType_ItemDerivedType_UsesTypeSpecificTailBits()
    {
        var wireType = ObjectPropertySchemaProvider.Default.ResolveWireType(ObjectType.Weapon, 121);

        await Assert.That(wireType).IsEqualTo(ObjectWireType.Int32Array);
    }

    [Test]
    public async Task ResolveWireType_Pc_ComposesCritterAndPcBits()
    {
        var critterWireType = ObjectPropertySchemaProvider.Default.ResolveWireType(ObjectType.Pc, 74);
        var pcWireType = ObjectPropertySchemaProvider.Default.ResolveWireType(ObjectType.Pc, 145);

        await Assert.That(critterWireType).IsEqualTo(ObjectWireType.Int32Array);
        await Assert.That(pcWireType).IsEqualTo(ObjectWireType.String);
    }

    [Test]
    public async Task ResolveWireType_Npc_ComposesCritterAndNpcBits()
    {
        var critterWireType = ObjectPropertySchemaProvider.Default.ResolveWireType(ObjectType.Npc, 87);
        var npcWireType = ObjectPropertySchemaProvider.Default.ResolveWireType(ObjectType.Npc, 129);

        await Assert.That(critterWireType).IsEqualTo(ObjectWireType.HandleArray);
        await Assert.That(npcWireType).IsEqualTo(ObjectWireType.ObjectId);
    }

    [Test]
    public async Task ResolveWireType_CommonLightingAndBlitBits_MatchCePackedStorage()
    {
        var blitColor = ObjectPropertySchemaProvider.Default.ResolveWireType(ObjectType.Scenery, 9);
        var lightColor = ObjectPropertySchemaProvider.Default.ResolveWireType(ObjectType.Scenery, 14);
        var overlayLightFlags = ObjectPropertySchemaProvider.Default.ResolveWireType(ObjectType.Scenery, 15);
        var overlayLightAid = ObjectPropertySchemaProvider.Default.ResolveWireType(ObjectType.Scenery, 16);
        var overlayLightColor = ObjectPropertySchemaProvider.Default.ResolveWireType(ObjectType.Scenery, 17);

        await Assert.That(blitColor).IsEqualTo(ObjectWireType.Int32);
        await Assert.That(lightColor).IsEqualTo(ObjectWireType.Int32);
        await Assert.That(overlayLightFlags).IsEqualTo(ObjectWireType.UInt32Array);
        await Assert.That(overlayLightAid).IsEqualTo(ObjectWireType.UInt32Array);
        await Assert.That(overlayLightColor).IsEqualTo(ObjectWireType.UInt32Array);
    }

    [Test]
    public async Task ResolveWireType_UnknownBit_Throws()
    {
        await Assert
            .That(() => ObjectPropertySchemaProvider.Default.ResolveWireType(ObjectType.Wall, 999))
            .Throws<NotSupportedException>();
    }
}
