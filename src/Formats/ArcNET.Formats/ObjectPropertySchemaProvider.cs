using System.Collections.Frozen;
using ArcNET.GameObjects;

namespace ArcNET.Formats;

internal interface IObjectPropertySchemaProvider
{
    ObjectWireType ResolveWireType(ObjectType objectType, int bit);
}

internal static class ObjectPropertySchemaProvider
{
    public static IObjectPropertySchemaProvider Default { get; } =
        new TableBackedObjectPropertySchemaProvider(
            ObjectPropertyWireTypeTables.Common,
            ObjectPropertyWireTypeTables.ByObjectType
        );

    private sealed class TableBackedObjectPropertySchemaProvider(
        FrozenDictionary<int, ObjectWireType> commonWireTypes,
        FrozenDictionary<ObjectType, FrozenDictionary<int, ObjectWireType>> objectTypeWireTypes
    ) : IObjectPropertySchemaProvider
    {
        public ObjectWireType ResolveWireType(ObjectType objectType, int bit)
        {
            if (commonWireTypes.TryGetValue(bit, out var common))
                return common;

            if (
                objectTypeWireTypes.TryGetValue(objectType, out var specific)
                && specific.TryGetValue(bit, out var wireType)
            )
            {
                return wireType;
            }

            throw new NotSupportedException(
                $"Unknown wire type for ObjectType={objectType}, bit={bit}. "
                    + "Cross-reference object_fields[] to determine the type "
                    + "and add it to ObjectPropertySchemaProvider.Default."
            );
        }
    }
}
