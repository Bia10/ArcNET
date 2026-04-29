using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

/// <summary>
/// Placeholder for unsupported game object types.
/// The registry no longer produces this type because the original body bytes cannot be preserved safely.
/// </summary>
internal sealed class ObjectUnknown : ObjectCommon
{
    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype) =>
        throw new InvalidDataException("ObjectUnknown cannot be serialized because its body codec is unavailable.");
}
