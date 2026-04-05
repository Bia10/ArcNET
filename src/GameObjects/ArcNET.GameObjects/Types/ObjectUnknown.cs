using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

/// <summary>
/// Placeholder for game objects whose type is not recognized by this version of the library.
/// Fields are intentionally absent — the raw bytes are not consumed.
/// </summary>
public sealed class ObjectUnknown : ObjectCommon
{
    internal void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype) =>
        WriteCommonFields(ref writer, bitmap, isPrototype);
}
