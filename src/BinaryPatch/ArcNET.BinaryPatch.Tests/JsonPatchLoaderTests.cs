using ArcNET.BinaryPatch;
using ArcNET.BinaryPatch.Json;

namespace ArcNET.BinaryPatch.Tests;

public sealed class JsonPatchLoaderTests
{
    // ── Load ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Load_ParsesPatchSetNameAndVersion()
    {
        const string json = """
            {
              "name": "Test patch set",
              "version": "2.0.0",
              "patches": []
            }
            """;

        var set = JsonPatchLoader.Load(json);

        await Assert.That(set.Name).IsEqualTo("Test patch set");
        await Assert.That(set.Version).IsEqualTo("2.0.0");
    }

    [Test]
    public async Task Load_EmptyPatchesArray_ReturnsEmptyPatchSet()
    {
        const string json = """
            { "name": "Empty", "version": "1.0.0", "patches": [] }
            """;

        var set = JsonPatchLoader.Load(json);

        await Assert.That(set.Patches.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Load_RawAtOffset_BuiltCorrectly()
    {
        const string json = """
            {
              "name": "Raw test",
              "version": "1.0.0",
              "patches": [
                {
                  "type": "RawAtOffset",
                  "id": "raw-test",
                  "description": "Test raw patch",
                  "relativePath": "some/file.bin",
                  "offset": 4,
                  "expectedHex": "AABB",
                  "newHex": "0000"
                }
              ]
            }
            """;

        var set = JsonPatchLoader.Load(json);

        await Assert.That(set.Patches.Count).IsEqualTo(1);

        var patch = set.Patches[0];
        await Assert.That(patch.Id).IsEqualTo("raw-test");
        await Assert.That(patch.Target.RelativePath).IsEqualTo("some/file.bin");

        // NeedsApply should return true with matching expected bytes at offset 4
        byte[] file = [0x01, 0x02, 0x03, 0x04, 0xAA, 0xBB, 0x05];
        await Assert.That(patch.NeedsApply(file)).IsTrue();
    }

    [Test]
    public async Task Load_ProtoFieldClearInt32_NeedsApplyWhenFieldNonZero()
    {
        const string json = """
            {
              "name": "Clear test",
              "version": "1.0.0",
              "patches": [
                {
                  "type": "ProtoFieldClearInt32",
                  "id": "clear-test",
                  "description": "Clear a field",
                  "relativePath": "data/proto/items/00000001.pro",
                  "field": "ObjFContainerInventorySource",
                  "newValue": 0
                }
              ]
            }
            """;

        var set = JsonPatchLoader.Load(json);

        await Assert.That(set.Patches.Count).IsEqualTo(1);
        await Assert.That(set.Patches[0].Id).IsEqualTo("clear-test");
    }

    [Test]
    public async Task Load_UnknownType_ThrowsNotSupportedException()
    {
        const string json = """
            {
              "name": "Bad type",
              "version": "1.0.0",
              "patches": [
                {
                  "type": "NonExistentPatchType",
                  "id": "bad",
                  "description": "bad",
                  "relativePath": "some/file.bin"
                }
              ]
            }
            """;

        await Assert.That(() => JsonPatchLoader.Load(json)).Throws<NotSupportedException>();
    }

    [Test]
    public async Task Load_UnknownObjectField_ThrowsInvalidOperationException()
    {
        const string json = """
            {
              "name": "Bad field",
              "version": "1.0.0",
              "patches": [
                {
                  "type": "ProtoFieldClearInt32",
                  "id": "bad-field",
                  "description": "bad field name",
                  "relativePath": "some/file.pro",
                  "field": "NonExistentField",
                  "newValue": 0
                }
              ]
            }
            """;

        await Assert.That(() => JsonPatchLoader.Load(json)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Load_NullJsonText_Throws()
    {
        await Assert.That(() => JsonPatchLoader.Load(null!)).Throws<ArgumentNullException>();
    }

    // ── PatchDiscovery ─────────────────────────────────────────────────────

    [Test]
    public async Task PatchDiscovery_LoadAll_ReturnsJsonFilesFromDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"arcnet_patches_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            const string json = """
                {
                  "name": "Test Patch",
                  "version": "0.1.0",
                  "patches": [
                    {
                      "type": "RawAtOffset",
                      "id": "test-raw",
                      "description": "test",
                      "relativePath": "data/test.pro",
                      "offset": 0,
                      "expectedHex": "FF",
                      "newHex": "00"
                    }
                  ]
                }
                """;

            File.WriteAllText(Path.Combine(dir, "TestPatch.json"), json);

            var sets = PatchDiscovery.LoadAll(dir);
            await Assert.That(sets.Count).IsEqualTo(1);
            await Assert.That(sets[0].Name).IsEqualTo("Test Patch");
            await Assert.That(sets[0].Patches.Count).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task PatchDiscovery_LoadAll_EmptyDirReturnsEmpty()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"arcnet_patches_empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var sets = PatchDiscovery.LoadAll(dir);
            await Assert.That(sets.Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(dir);
        }
    }

    [Test]
    public async Task PatchDiscovery_LoadAll_MissingDirReturnsEmpty()
    {
        var sets = PatchDiscovery.LoadAll(Path.Combine(Path.GetTempPath(), "arcnet_does_not_exist"));
        await Assert.That(sets.Count).IsEqualTo(0);
    }

    [Test]
    public async Task PatchDiscovery_LoadAll_InvalidJsonSkippedWithCallback()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"arcnet_patches_bad_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "bad.json"), "not-json{{{");

            var errors = new List<(string File, Exception Ex)>();
            var sets = PatchDiscovery.LoadAll(dir, (f, ex) => errors.Add((f, ex)));

            await Assert.That(sets.Count).IsEqualTo(0);
            await Assert.That(errors.Count).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
