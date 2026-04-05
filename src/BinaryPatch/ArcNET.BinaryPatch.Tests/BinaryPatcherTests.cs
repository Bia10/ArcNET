using ArcNET.BinaryPatch.Patches;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.BinaryPatch.Tests;

/// <summary>
/// Integration-style tests for <see cref="BinaryPatcher"/> using temp-directory file fixtures.
/// Each test class instance creates a unique temp directory and cleans it up on dispose.
/// No game installation required.
/// </summary>
public sealed class BinaryPatcherTests : IDisposable
{
    private readonly string _gameDir = Path.Combine(Path.GetTempPath(), $"arcnet-binarypatcher-{Guid.NewGuid():N}");

    public BinaryPatcherTests() => Directory.CreateDirectory(_gameDir);

    public void Dispose()
    {
        if (Directory.Exists(_gameDir))
            Directory.Delete(_gameDir, recursive: true);
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private static byte[] BuildContainerProtoBytes(int inventorySource)
    {
        var bitmap = new byte[12]; // 12 bytes = 96 bits
        bitmap[(int)ObjectField.ObjFContainerInventorySource >> 3] |= (byte)(
            1 << ((int)ObjectField.ObjFContainerInventorySource & 7)
        );

        var header = new GameObjectHeader
        {
            Version = 0x77,
            ProtoId = new GameObjectGuid(-1, 0, 0, Guid.Empty),
            ObjectId = new GameObjectGuid(0, 0, 0, Guid.Empty),
            GameObjectType = ObjectType.Container,
            PropCollectionItems = 0,
            Bitmap = bitmap,
        };

        var srcBytes = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(srcBytes, inventorySource);

        var proto = new ProtoData
        {
            Header = header,
            Properties = [new ObjectProperty { Field = ObjectField.ObjFContainerInventorySource, RawBytes = srcBytes }],
        };

        return ProtoFormat.WriteToArray(in proto);
    }

    private string WriteProtoFile(string relativePath, int inventorySource)
    {
        var full = Path.Combine(_gameDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, BuildContainerProtoBytes(inventorySource));
        return full;
    }

    private static ProtoFieldPatch MakePatch(string relativePath, int expectedValue = 42, int newValue = 0) =>
        ProtoFieldPatch.SetInt32(
            "test-patch",
            "test",
            relativePath,
            ObjectField.ObjFContainerInventorySource,
            expectedValue,
            newValue
        );

    private static BinaryPatchSet MakePatchSet(IBinaryPatch patch) =>
        new()
        {
            Name = "Test Set",
            Version = "1.0.0",
            Patches = [patch],
        };

    // ── Apply — happy path ─────────────────────────────────────────────────

    [Test]
    public async Task Apply_ReturnsPatchStatusApplied_AndFileBytesChange()
    {
        const string relative = "data/proto/containers/00000025.pro";
        var path = WriteProtoFile(relative, inventorySource: 42);

        var results = BinaryPatcher.Apply(MakePatchSet(MakePatch(relative)), _gameDir);

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Status).IsEqualTo(PatchStatus.Applied);

        var proto = ProtoFormat.ParseFile(path);
        var src = proto.Properties.First(p => p.Field == ObjectField.ObjFContainerInventorySource);
        await Assert.That(src.GetInt32()).IsEqualTo(0);
    }

    [Test]
    public async Task Apply_CreatesBackupFile_WhenCreateBackupIsTrue()
    {
        const string relative = "data/proto/containers/00000025.pro";
        var path = WriteProtoFile(relative, inventorySource: 42);

        BinaryPatcher.Apply(MakePatchSet(MakePatch(relative)), _gameDir, new PatchOptions { CreateBackup = true });

        await Assert.That(File.Exists(path + ".bak")).IsTrue();
    }

    [Test]
    public async Task Apply_DoesNotCreateBackup_WhenCreateBackupIsFalse()
    {
        const string relative = "data/proto/containers/00000025.pro";
        var path = WriteProtoFile(relative, inventorySource: 42);

        BinaryPatcher.Apply(MakePatchSet(MakePatch(relative)), _gameDir, new PatchOptions { CreateBackup = false });

        await Assert.That(File.Exists(path + ".bak")).IsFalse();
    }

    // ── Apply — idempotency ────────────────────────────────────────────────

    [Test]
    public async Task Apply_ReturnsAlreadyApplied_WhenFileAlreadyPatched()
    {
        const string relative = "data/proto/containers/00000025.pro";
        WriteProtoFile(relative, inventorySource: 0);

        var results = BinaryPatcher.Apply(MakePatchSet(MakePatch(relative, expectedValue: 42, newValue: 0)), _gameDir);

        await Assert.That(results[0].Status).IsEqualTo(PatchStatus.AlreadyApplied);
    }

    // ── Apply — error cases ────────────────────────────────────────────────

    [Test]
    public async Task Apply_ReturnsFailed_WhenFileNotFound()
    {
        var results = BinaryPatcher.Apply(MakePatchSet(MakePatch("data/proto/containers/99999999.pro")), _gameDir);

        await Assert.That(results[0].Status).IsEqualTo(PatchStatus.Failed);
        await Assert.That(results[0].Reason!.Contains("not found")).IsTrue();
    }

    // ── Dry run ────────────────────────────────────────────────────────────

    [Test]
    public async Task Apply_DryRun_ReturnsSkipped_AndWritesNothing()
    {
        const string relative = "data/proto/containers/00000025.pro";
        var path = WriteProtoFile(relative, inventorySource: 42);
        var originalBytes = File.ReadAllBytes(path);

        var results = BinaryPatcher.Apply(
            MakePatchSet(MakePatch(relative)),
            _gameDir,
            new PatchOptions { DryRun = true }
        );

        await Assert.That(results[0].Status).IsEqualTo(PatchStatus.Skipped);
        await Assert.That(File.ReadAllBytes(path).SequenceEqual(originalBytes)).IsTrue();
        await Assert.That(File.Exists(path + ".bak")).IsFalse();
    }

    // ── Revert ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Revert_RestoresOriginalBytes_AfterApply()
    {
        const string relative = "data/proto/containers/00000025.pro";
        var path = WriteProtoFile(relative, inventorySource: 42);
        var originalBytes = File.ReadAllBytes(path);

        var patchSet = MakePatchSet(MakePatch(relative));
        BinaryPatcher.Apply(patchSet, _gameDir, new PatchOptions { CreateBackup = true });

        var revertResults = BinaryPatcher.Revert(patchSet, _gameDir);

        await Assert.That(revertResults[0].Status).IsEqualTo(PatchStatus.Applied);
        await Assert.That(File.ReadAllBytes(path).SequenceEqual(originalBytes)).IsTrue();
        await Assert.That(File.Exists(path + ".bak")).IsFalse();
    }

    [Test]
    public async Task Revert_ReturnsSkipped_WhenNoBackupExists()
    {
        const string relative = "data/proto/containers/00000025.pro";
        WriteProtoFile(relative, inventorySource: 42);

        var results = BinaryPatcher.Revert(MakePatchSet(MakePatch(relative)), _gameDir);

        await Assert.That(results[0].Status).IsEqualTo(PatchStatus.Skipped);
    }

    // ── Verify ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Verify_ReturnsNeedsApplyTrue_ForUnpatchedFile()
    {
        const string relative = "data/proto/containers/00000025.pro";
        WriteProtoFile(relative, inventorySource: 42);

        var results = BinaryPatcher.Verify(MakePatchSet(MakePatch(relative)), _gameDir);

        await Assert.That(results[0].NeedsApply).IsTrue();
        await Assert.That(results[0].FileExists).IsTrue();
    }

    [Test]
    public async Task Verify_ReturnsNeedsApplyFalse_ForPatchedFile()
    {
        const string relative = "data/proto/containers/00000025.pro";
        WriteProtoFile(relative, inventorySource: 0);

        var results = BinaryPatcher.Verify(MakePatchSet(MakePatch(relative, expectedValue: 42)), _gameDir);

        await Assert.That(results[0].NeedsApply).IsFalse();
        await Assert.That(results[0].FileExists).IsTrue();
    }

    [Test]
    public async Task Verify_ReturnsFileExistsFalse_WhenFileMissing()
    {
        var results = BinaryPatcher.Verify(MakePatchSet(MakePatch("data/proto/containers/99999999.pro")), _gameDir);

        await Assert.That(results[0].FileExists).IsFalse();
        await Assert.That(results[0].NeedsApply).IsFalse();
    }

    [Test]
    public async Task Verify_DoesNotModifyFile()
    {
        const string relative = "data/proto/containers/00000025.pro";
        var path = WriteProtoFile(relative, inventorySource: 42);
        var originalBytes = File.ReadAllBytes(path);

        BinaryPatcher.Verify(MakePatchSet(MakePatch(relative)), _gameDir);

        await Assert.That(File.ReadAllBytes(path).SequenceEqual(originalBytes)).IsTrue();
    }
}
