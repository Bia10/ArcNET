using System.Buffers.Binary;

namespace ArcNET.Editor.Tests;

public sealed class EditorAudioPreviewBuilderTests
{
    [Test]
    public async Task BuildWave_ParsesPcmMetadataAndSamplePayload()
    {
        var wave = CreateWaveFileBytes(channelCount: 1, sampleRate: 8000, bitsPerSample: 16, sampleData: [1, 2, 3, 4]);

        var preview = EditorAudioPreviewBuilder.BuildWave(wave, "sound/effect.wav");

        await Assert.That(preview.AssetPath).IsEqualTo("sound/effect.wav");
        await Assert.That(preview.Encoding).IsEqualTo(EditorAudioSampleEncoding.Pcm);
        await Assert.That(preview.ChannelCount).IsEqualTo(1);
        await Assert.That(preview.SampleRate).IsEqualTo(8000);
        await Assert.That(preview.BitsPerSample).IsEqualTo(16);
        await Assert.That(preview.BlockAlign).IsEqualTo(2);
        await Assert.That(preview.ByteRate).IsEqualTo(16000);
        await Assert.That(preview.SampleFrameCount).IsEqualTo(2L);
        await Assert.That(preview.Duration).IsEqualTo(TimeSpan.FromSeconds(4d / 16000d));
        await Assert.That(preview.SampleData.SequenceEqual(new byte[] { 1, 2, 3, 4 })).IsTrue();
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
}
