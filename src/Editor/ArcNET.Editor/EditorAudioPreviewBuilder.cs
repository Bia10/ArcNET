using System.Buffers.Binary;

namespace ArcNET.Editor;

/// <summary>
/// Builds preview-ready metadata and sample payloads for supported audio assets.
/// </summary>
public static class EditorAudioPreviewBuilder
{
    /// <summary>
    /// Builds an audio preview from a RIFF/WAVE payload.
    /// </summary>
    public static EditorAudioPreview BuildWave(ReadOnlyMemory<byte> waveData, string? assetPath = null)
    {
        var data = waveData.Span;
        if (data.Length < 12)
            throw new InvalidDataException("WAV payload was too short to contain a RIFF header.");

        if (!data[..4].SequenceEqual("RIFF"u8) || !data.Slice(8, 4).SequenceEqual("WAVE"u8))
            throw new InvalidDataException("Audio payload was not a RIFF/WAVE file.");

        WaveFormatHeader? format = null;
        ReadOnlySpan<byte> sampleData = default;
        var offset = 12;

        while (offset + 8 <= data.Length)
        {
            var chunkId = data.Slice(offset, 4);
            var chunkSize = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset + 4, 4));
            if (chunkSize < 0)
                throw new InvalidDataException("WAV chunk size was negative.");

            var chunkDataOffset = offset + 8;
            if (chunkDataOffset + chunkSize > data.Length)
                throw new InvalidDataException("WAV chunk extended past the end of the payload.");

            var chunkData = data.Slice(chunkDataOffset, chunkSize);
            if (chunkId.SequenceEqual("fmt "u8))
            {
                format = ParseFormatChunk(chunkData);
            }
            else if (chunkId.SequenceEqual("data"u8))
            {
                sampleData = chunkData;
            }

            offset = chunkDataOffset + chunkSize + (chunkSize & 1);
        }

        if (format is null)
            throw new InvalidDataException("WAV payload did not contain a fmt chunk.");

        if (sampleData.IsEmpty)
            throw new InvalidDataException("WAV payload did not contain a data chunk.");

        var header = format.Value;
        var encoding = header.FormatTag switch
        {
            0x0001 => EditorAudioSampleEncoding.Pcm,
            0x0003 => EditorAudioSampleEncoding.IeeeFloat,
            _ => throw new NotSupportedException($"WAV format tag 0x{header.FormatTag:X4} is not supported."),
        };

        if (header.ChannelCount <= 0)
            throw new InvalidDataException("WAV channel count must be greater than zero.");

        if (header.SampleRate <= 0)
            throw new InvalidDataException("WAV sample rate must be greater than zero.");

        if (header.BlockAlign <= 0)
            throw new InvalidDataException("WAV block-align must be greater than zero.");

        var sampleFrameCount = sampleData.Length / header.BlockAlign;
        return new EditorAudioPreview
        {
            AssetPath = assetPath,
            Encoding = encoding,
            ChannelCount = header.ChannelCount,
            SampleRate = header.SampleRate,
            BitsPerSample = header.BitsPerSample,
            BlockAlign = header.BlockAlign,
            ByteRate = header.ByteRate,
            SampleFrameCount = sampleFrameCount,
            SampleData = sampleData.ToArray(),
        };
    }

    private static WaveFormatHeader ParseFormatChunk(ReadOnlySpan<byte> chunkData)
    {
        if (chunkData.Length < 16)
            throw new InvalidDataException("WAV fmt chunk was too short.");

        return new WaveFormatHeader(
            BinaryPrimitives.ReadUInt16LittleEndian(chunkData),
            BinaryPrimitives.ReadUInt16LittleEndian(chunkData.Slice(2, 2)),
            checked((int)BinaryPrimitives.ReadUInt32LittleEndian(chunkData.Slice(4, 4))),
            checked((int)BinaryPrimitives.ReadUInt32LittleEndian(chunkData.Slice(8, 4))),
            BinaryPrimitives.ReadUInt16LittleEndian(chunkData.Slice(12, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(chunkData.Slice(14, 2))
        );
    }

    private readonly record struct WaveFormatHeader(
        ushort FormatTag,
        int ChannelCount,
        int SampleRate,
        int ByteRate,
        ushort BlockAlign,
        ushort BitsPerSample
    );
}
