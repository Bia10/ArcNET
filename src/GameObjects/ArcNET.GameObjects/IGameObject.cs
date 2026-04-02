using ArcNET.Core.Primitives;
using ArcNET.GameObjects.Types;

namespace ArcNET.GameObjects;

/// <summary>Common contract for all game object instances.</summary>
public interface IGameObject
{
    GameObjectHeader Header { get; }
    ObjectCommon Common { get; }
    ObjectType Type { get; }
    GameObjectGuid ObjectId { get; }
    GameObjectGuid ProtoId { get; }
    bool IsPrototype { get; }
}
