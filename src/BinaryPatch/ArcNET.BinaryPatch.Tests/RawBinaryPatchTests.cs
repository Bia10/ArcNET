using ArcNET.BinaryPatch.Patches;

namespace ArcNET.BinaryPatch.Tests;

public sealed class RawBinaryPatchTests
{
    // ── NeedsApply ─────────────────────────────────────────────────────────

    [Test]
    public async Task NeedsApply_ReturnsTrueWhenBytesMatchAtOffset()
    {
        var patch = RawBinaryPatch.AtOffset(
            "test",
            "test",
            "some/file.bin",
            offset: 4,
            expectedBytes: [0xAA, 0xBB],
            newBytes: [0x00, 0x00]
        );

        byte[] file = [0x01, 0x02, 0x03, 0x04, 0xAA, 0xBB, 0x05];

        await Assert.That(patch.NeedsApply(file)).IsTrue();
    }

    [Test]
    public async Task NeedsApply_ReturnsFalseWhenBytesDiffer()
    {
        var patch = RawBinaryPatch.AtOffset(
            "test",
            "test",
            "some/file.bin",
            offset: 4,
            expectedBytes: [0xAA, 0xBB],
            newBytes: [0x00, 0x00]
        );

        byte[] file = [0x01, 0x02, 0x03, 0x04, 0xFF, 0xFF, 0x05];

        await Assert.That(patch.NeedsApply(file)).IsFalse();
    }

    [Test]
    public async Task NeedsApply_ReturnsFalseWhenFileTooShort()
    {
        var patch = RawBinaryPatch.AtOffset(
            "test",
            "test",
            "some/file.bin",
            offset: 10,
            expectedBytes: [0x01, 0x02],
            newBytes: [0x00, 0x00]
        );

        byte[] file = [0x01, 0x02, 0x03];

        await Assert.That(patch.NeedsApply(file)).IsFalse();
    }

    // ── Apply ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Apply_ReplacesCorrectBytesAtOffset()
    {
        var patch = RawBinaryPatch.AtOffset(
            "test",
            "test",
            "some/file.bin",
            offset: 2,
            expectedBytes: [0xDE, 0xAD],
            newBytes: [0xBE, 0xEF]
        );

        byte[] file = [0x01, 0x02, 0xDE, 0xAD, 0x05];
        var result = patch.Apply(file);

        await Assert.That(result.SequenceEqual(new byte[] { 0x01, 0x02, 0xBE, 0xEF, 0x05 })).IsTrue();
    }

    [Test]
    public async Task Apply_DoesNotMutateOriginal()
    {
        var patch = RawBinaryPatch.AtOffset(
            "test",
            "test",
            "some/file.bin",
            offset: 0,
            expectedBytes: [0xAA],
            newBytes: [0xBB]
        );

        byte[] file = [0xAA, 0x00];
        _ = patch.Apply(file);

        await Assert.That(file[0]).IsEqualTo((byte)0xAA);
    }

    [Test]
    public async Task AtOffset_ThrowsWhenLengthsMismatch()
    {
        await Assert
            .That(() =>
                RawBinaryPatch.AtOffset(
                    "test",
                    "test",
                    "some/file.bin",
                    offset: 0,
                    expectedBytes: [0x01],
                    newBytes: [0x01, 0x02]
                )
            )
            .ThrowsException();
    }
}
