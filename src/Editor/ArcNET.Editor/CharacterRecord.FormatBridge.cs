using ArcNET.Formats;

namespace ArcNET.Editor;

public sealed partial class CharacterRecord
{
    /// <summary>
    /// Constructs a <see cref="CharacterRecord"/> from a format-layer
    /// <see cref="CharacterMdyRecord"/> decoded from a <c>mobile.mdy</c> file.
    /// </summary>
    public static CharacterRecord From(CharacterMdyRecord rec) => CharacterRecordMapper.From(rec);

    /// <summary>
    /// Produces a new <see cref="CharacterMdyRecord"/> derived from
    /// <paramref name="original"/> with all four SAR arrays replaced by the
    /// values in this record. All other bytes in the original are preserved.
    /// </summary>
    public CharacterMdyRecord ApplyTo(CharacterMdyRecord original) => CharacterRecordMapper.ApplyTo(this, original);
}
