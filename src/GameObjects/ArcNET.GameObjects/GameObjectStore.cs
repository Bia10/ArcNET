namespace ArcNET.GameObjects;

/// <summary>
/// Injectable data store for loaded game objects.
/// Replaces the old static <c>GameObjectManager</c>.
/// </summary>
public sealed class GameObjectStore
{
    private readonly List<GameObjectHeader> _headers = [];

    /// <summary>Gets a read-only view of all loaded object headers.</summary>
    /// <remarks>
    /// The returned <see cref="IReadOnlyList{T}"/> is a <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}"/>
    /// wrapper and cannot be downcast to the underlying <see cref="List{T}"/> to bypass read-only semantics.
    /// </remarks>
    public IReadOnlyList<GameObjectHeader> Headers => _headers.AsReadOnly();

    /// <summary>Adds a header to the store.</summary>
    public void Add(GameObjectHeader header) => _headers.Add(header);

    /// <summary>Clears all stored objects.</summary>
    public void Clear() => _headers.Clear();
}
