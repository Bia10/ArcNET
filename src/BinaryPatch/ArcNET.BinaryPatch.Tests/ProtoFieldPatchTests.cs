using System.Collections;
using ArcNET.BinaryPatch.Patches;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.BinaryPatch.Tests;

/// <summary>
/// Round-trip tests for <see cref="ProtoFieldPatch"/>.
/// Fixtures are built in-memory via <see cref="ProtoFormat.WriteToArray"/> so the tests
/// are fully self-contained and require no game installation.
/// </summary>
public sealed class ProtoFieldPatchTests
{
    // ── Fixture helpers ────────────────────────────────────────────────────

    private static byte[] BuildContainerProtoBytes(int inventorySource)
    {
        var bitmap = new BitArray(12 * 8);
        bitmap[(int)ObjectField.ObjFContainerInventorySource] = true;

        var header = new GameObjectHeader
        {
            Version = 0x77,
            ProtoId = new GameObjectGuid(-1, 0, 0, Guid.Empty),
            ObjectId = new GameObjectGuid(0, 0, 0, Guid.Empty),
            GameObjectType = ObjectType.Container,
            PropCollectionItems = 0,
            Bitmap = bitmap,
        };

        var invSourceBytes = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(invSourceBytes, inventorySource);

        var proto = new ProtoData
        {
            Header = header,
            Properties =
            [
                new ObjectProperty { Field = ObjectField.ObjFContainerInventorySource, RawBytes = invSourceBytes },
            ],
        };

        return ProtoFormat.WriteToArray(in proto);
    }

    // ── NeedsApply ─────────────────────────────────────────────────────────

    [Test]
    public async Task NeedsApply_ReturnsTrueWhenFieldHasExpectedValue()
    {
        var bytes = BuildContainerProtoBytes(inventorySource: 42);

        var patch = ProtoFieldPatch.SetInt32(
            "test-patch",
            "test",
            "data/proto/containers/00000025.pro",
            ObjectField.ObjFContainerInventorySource,
            expectedValue: 42,
            newValue: 0
        );

        await Assert.That(patch.NeedsApply(bytes)).IsTrue();
    }

    [Test]
    public async Task NeedsApply_ReturnsFalseWhenFieldAlreadyHasNewValue()
    {
        var bytes = BuildContainerProtoBytes(inventorySource: 0);

        var patch = ProtoFieldPatch.SetInt32(
            "test-patch",
            "test",
            "data/proto/containers/00000025.pro",
            ObjectField.ObjFContainerInventorySource,
            expectedValue: 42,
            newValue: 0
        );

        await Assert.That(patch.NeedsApply(bytes)).IsFalse();
    }

    [Test]
    public async Task NeedsApply_ReturnsFalseWhenFieldAbsentFromProto()
    {
        var bitmap = new BitArray(12 * 8); // no bits set
        var header = new GameObjectHeader
        {
            Version = 0x77,
            ProtoId = new GameObjectGuid(-1, 0, 0, Guid.Empty),
            ObjectId = new GameObjectGuid(0, 0, 0, Guid.Empty),
            GameObjectType = ObjectType.Container,
            PropCollectionItems = 0,
            Bitmap = bitmap,
        };

        var proto = new ProtoData { Header = header, Properties = [] };
        var bytes = ProtoFormat.WriteToArray(in proto);

        var patch = ProtoFieldPatch.SetInt32(
            "test-patch",
            "test",
            "data/proto/containers/00000001.pro",
            ObjectField.ObjFContainerInventorySource,
            expectedValue: 0,
            newValue: 99
        );

        await Assert.That(patch.NeedsApply(bytes)).IsFalse();
    }

    // ── Apply ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Apply_ChangesTargetField_RoundTrip()
    {
        var bytes = BuildContainerProtoBytes(inventorySource: 42);

        var patch = ProtoFieldPatch.SetInt32(
            "test-patch",
            "test",
            "data/proto/containers/00000025.pro",
            ObjectField.ObjFContainerInventorySource,
            expectedValue: 42,
            newValue: 0
        );

        var patched = patch.Apply(bytes);

        var result = ProtoFormat.ParseMemory(patched);
        var invSource = result.Properties.First(p => p.Field == ObjectField.ObjFContainerInventorySource);

        await Assert.That(invSource.GetInt32()).IsEqualTo(0);
    }

    [Test]
    public async Task Apply_PreservesOtherFields_WhenMultiplePropsPresent()
    {
        var bitmap = new BitArray(12 * 8);
        bitmap[(int)ObjectField.ObjFContainerFlags] = true;
        bitmap[(int)ObjectField.ObjFContainerInventorySource] = true;

        var header = new GameObjectHeader
        {
            Version = 0x77,
            ProtoId = new GameObjectGuid(-1, 0, 0, Guid.Empty),
            ObjectId = new GameObjectGuid(0, 0, 0, Guid.Empty),
            GameObjectType = ObjectType.Container,
            PropCollectionItems = 0,
            Bitmap = bitmap,
        };

        var flagBytes = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(flagBytes, 7);

        var srcBytes = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(srcBytes, 42);

        var proto = new ProtoData
        {
            Header = header,
            Properties =
            [
                new ObjectProperty { Field = ObjectField.ObjFContainerFlags, RawBytes = flagBytes },
                new ObjectProperty { Field = ObjectField.ObjFContainerInventorySource, RawBytes = srcBytes },
            ],
        };

        var bytes = ProtoFormat.WriteToArray(in proto);

        var patch = ProtoFieldPatch.SetInt32(
            "test-patch",
            "test",
            "data/proto/containers/00000025.pro",
            ObjectField.ObjFContainerInventorySource,
            expectedValue: 42,
            newValue: 0
        );

        var patched = patch.Apply(bytes);
        var result = ProtoFormat.ParseMemory(patched);

        var flags = result.Properties.First(p => p.Field == ObjectField.ObjFContainerFlags);
        var invSrc = result.Properties.First(p => p.Field == ObjectField.ObjFContainerInventorySource);

        await Assert.That(flags.GetInt32()).IsEqualTo(7);
        await Assert.That(invSrc.GetInt32()).IsEqualTo(0);
    }

    // ── Custom predicate ───────────────────────────────────────────────────

    [Test]
    public async Task Custom_NullPredicate_AlwaysNeedsApply_WhenFieldPresent()
    {
        var bytes = BuildContainerProtoBytes(inventorySource: 99);

        var patch = ProtoFieldPatch.Custom(
            "test-custom",
            "test",
            "data/proto/containers/00000025.pro",
            ObjectField.ObjFContainerInventorySource,
            needsApplyPredicate: null,
            transform: prop => new ObjectProperty { Field = prop.Field, RawBytes = [0, 0, 0, 0] }
        );

        await Assert.That(patch.NeedsApply(bytes)).IsTrue();
    }
}
