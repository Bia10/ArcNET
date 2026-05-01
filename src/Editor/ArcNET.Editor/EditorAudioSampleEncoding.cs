namespace ArcNET.Editor;

/// <summary>
/// Encoded sample representation of an audio preview payload.
/// </summary>
public enum EditorAudioSampleEncoding
{
    /// <summary>
    /// Pulse-code modulation sample data.
    /// </summary>
    Pcm,

    /// <summary>
    /// IEEE 754 floating-point sample data.
    /// </summary>
    IeeeFloat,
}
