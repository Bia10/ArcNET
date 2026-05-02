namespace ArcNET.Editor;

/// <summary>
/// Browser-friendly metadata for one loaded audio asset.
/// </summary>
public sealed class EditorAudioDefinition
{
    /// <summary>
    /// Asset entry that produced this detail.
    /// </summary>
    public required EditorAudioAssetEntry Asset { get; init; }

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
    /// Number of sample frames in the asset data chunk.
    /// </summary>
    public required long SampleFrameCount { get; init; }

    /// <summary>
    /// Raw byte length of the extracted sample payload.
    /// </summary>
    public required int SampleByteLength { get; init; }

    /// <summary>
    /// Total playback duration derived from the sample payload and byte rate.
    /// </summary>
    public required TimeSpan Duration { get; init; }
}
