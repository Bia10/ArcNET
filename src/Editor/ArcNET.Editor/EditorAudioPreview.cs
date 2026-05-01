namespace ArcNET.Editor;

/// <summary>
/// Preview-ready audio metadata plus the extracted sample payload for one asset.
/// </summary>
public sealed class EditorAudioPreview
{
    /// <summary>
    /// Asset path that produced this preview when known.
    /// </summary>
    public string? AssetPath { get; init; }

    /// <summary>
    /// Encoded sample representation.
    /// </summary>
    public required EditorAudioSampleEncoding Encoding { get; init; }

    /// <summary>
    /// Number of audio channels.
    /// </summary>
    public required int ChannelCount { get; init; }

    /// <summary>
    /// Samples per second.
    /// </summary>
    public required int SampleRate { get; init; }

    /// <summary>
    /// Encoded bits per sample.
    /// </summary>
    public required int BitsPerSample { get; init; }

    /// <summary>
    /// Encoded byte count per sample frame.
    /// </summary>
    public required int BlockAlign { get; init; }

    /// <summary>
    /// Encoded bytes per second.
    /// </summary>
    public required int ByteRate { get; init; }

    /// <summary>
    /// Number of sample frames in <see cref="SampleData"/>.
    /// </summary>
    public required long SampleFrameCount { get; init; }

    /// <summary>
    /// Extracted audio sample payload from the WAV data chunk.
    /// </summary>
    public required byte[] SampleData { get; init; }

    /// <summary>
    /// Total playback duration derived from <see cref="SampleData"/> and <see cref="ByteRate"/>.
    /// </summary>
    public TimeSpan Duration =>
        ByteRate <= 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(SampleData.Length / (double)ByteRate);
}
