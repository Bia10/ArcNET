namespace ArcNET.GameObjects.Types;

internal static class ObjectBitmap
{
    public static bool IsFieldPresent(byte[] bitmap, ObjectField field, bool isPrototype) =>
        isPrototype || bitmap.HasField(field);
}
