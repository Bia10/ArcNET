using System.Buffers.Binary;
using ArcNET.Archive;

namespace ArcNET.Editor.Tests;

public sealed class EditorWorkspaceAudioPreviewTests
{
    [Test]
    public async Task LoadAsync_LoadsLooseWaveAssetsAndBuildsAudioPreview()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "sound"));

        try
        {
            var wavePath = Path.Combine(contentDir, "sound", "effect.wav");
            await File.WriteAllBytesAsync(
                wavePath,
                CreateWaveFileBytes(channelCount: 1, sampleRate: 8000, bitsPerSample: 16, sampleData: [1, 2, 3, 4])
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var asset = workspace.FindAudioAsset("sound/effect.wav");
            var preview = workspace.CreateAudioPreview("sound/effect.wav");

            await Assert.That(workspace.AudioAssets.Count).IsEqualTo(1);
            await Assert.That(asset).IsNotNull();
            await Assert.That(asset!.SourceKind).IsEqualTo(EditorAssetSourceKind.LooseFile);
            await Assert.That(asset.SourcePath).IsEqualTo(wavePath);
            await Assert.That(preview.ChannelCount).IsEqualTo(1);
            await Assert.That(preview.SampleRate).IsEqualTo(8000);
            await Assert.That(preview.SampleFrameCount).IsEqualTo(2L);
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadFromGameInstallAsync_LoadsWaveAssetsFromArchiveAndBuildsAudioPreview()
    {
        var gameDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(gameDir);
        Directory.CreateDirectory(Path.Combine(gameDir, "modules"));

        try
        {
            var archivePath = Path.Combine(gameDir, "modules", "Arcanum.dat");
            await WriteDatAsync(
                archivePath,
                new Dictionary<string, byte[]>
                {
                    ["sound\\effect.wav"] = CreateWaveFileBytes(
                        channelCount: 2,
                        sampleRate: 11025,
                        bitsPerSample: 16,
                        sampleData: [1, 2, 3, 4, 5, 6, 7, 8]
                    ),
                }
            );

            var workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(gameDir);
            var asset = workspace.FindAudioAsset("sound/effect.wav");
            var preview = workspace.CreateAudioPreview("sound/effect.wav");

            await Assert.That(workspace.AudioAssets.Count).IsEqualTo(1);
            await Assert.That(asset).IsNotNull();
            await Assert.That(asset!.SourceKind).IsEqualTo(EditorAssetSourceKind.DatArchive);
            await Assert.That(asset.SourcePath).IsEqualTo(archivePath);
            await Assert.That(asset.SourceEntryPath).IsEqualTo("sound/effect.wav");
            await Assert.That(preview.ChannelCount).IsEqualTo(2);
            await Assert.That(preview.SampleRate).IsEqualTo(11025);
            await Assert.That(preview.SampleFrameCount).IsEqualTo(2L);
        }
        finally
        {
            Directory.Delete(gameDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_IndexesAudioDetails_ForBrowserWorkflows()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "sound"));
        Directory.CreateDirectory(Path.Combine(contentDir, "music"));

        try
        {
            await File.WriteAllBytesAsync(
                Path.Combine(contentDir, "sound", "effect.wav"),
                CreateWaveFileBytes(channelCount: 1, sampleRate: 8000, bitsPerSample: 16, sampleData: [1, 2, 3, 4])
            );
            await File.WriteAllBytesAsync(
                Path.Combine(contentDir, "music", "theme.wav"),
                CreateWaveFileBytes(
                    channelCount: 2,
                    sampleRate: 11025,
                    bitsPerSample: 16,
                    sampleData: [1, 2, 3, 4, 5, 6, 7, 8]
                )
            );

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var detail = workspace.FindAudioDetail("sound/effect.wav");
            var search = workspace.SearchAudioDetails("theme");

            await Assert.That(detail).IsNotNull();
            await Assert.That(detail!.Asset.AssetPath).IsEqualTo("sound/effect.wav");
            await Assert.That(detail.ChannelCount).IsEqualTo(1);
            await Assert.That(detail.SampleRate).IsEqualTo(8000);
            await Assert.That(detail.SampleFrameCount).IsEqualTo(2L);
            await Assert.That(detail.SampleByteLength).IsEqualTo(4);
            await Assert.That(detail.Duration).IsGreaterThan(TimeSpan.Zero);

            await Assert.That(search.Count).IsEqualTo(1);
            await Assert.That(search[0].Asset.AssetPath).IsEqualTo("music/theme.wav");
            await Assert.That(search[0].ChannelCount).IsEqualTo(2);
            await Assert.That(search[0].SampleRate).IsEqualTo(11025);
        }
        finally
        {
            Directory.Delete(contentDir, recursive: true);
        }
    }

    private static byte[] CreateWaveFileBytes(int channelCount, int sampleRate, int bitsPerSample, byte[] sampleData)
    {
        var blockAlign = checked((ushort)((channelCount * bitsPerSample) / 8));
        var byteRate = checked(sampleRate * blockAlign);
        var riffSize = 36 + sampleData.Length;
        var bytes = new byte[44 + sampleData.Length];

        "RIFF"u8.CopyTo(bytes.AsSpan(0, 4));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4, 4), riffSize);
        "WAVE"u8.CopyTo(bytes.AsSpan(8, 4));
        "fmt "u8.CopyTo(bytes.AsSpan(12, 4));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(16, 4), 16);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(20, 2), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(22, 2), checked((ushort)channelCount));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(24, 4), sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(28, 4), byteRate);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(32, 2), blockAlign);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(34, 2), checked((ushort)bitsPerSample));
        "data"u8.CopyTo(bytes.AsSpan(36, 4));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(40, 4), sampleData.Length);
        sampleData.CopyTo(bytes, 44);
        return bytes;
    }

    private static async Task WriteDatAsync(string archivePath, IReadOnlyDictionary<string, byte[]> entries)
    {
        var inputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(inputDir);

        try
        {
            foreach (var (virtualPath, bytes) in entries)
            {
                var fullPath = Path.Combine(
                    inputDir,
                    virtualPath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)
                );
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                await File.WriteAllBytesAsync(fullPath, bytes);
            }

            await DatPacker.PackAsync(inputDir, archivePath);
        }
        finally
        {
            if (Directory.Exists(inputDir))
                Directory.Delete(inputDir, recursive: true);
        }
    }
}
