using System.Collections.Frozen;

namespace ArcNET.Core;

/// <summary>
/// Fast name-to-value and value-to-name lookups for an <see langword="enum"/> type.
/// Built once at first access; all subsequent lookups are O(1) via <see cref="FrozenDictionary{TKey,TValue}"/>.
/// </summary>
/// <typeparam name="TEnum">The enum type to index.</typeparam>
public static class EnumLookup<TEnum>
    where TEnum : struct, Enum
{
    /// <summary>Maps enum member names (case-insensitive) to their values.</summary>
    public static readonly FrozenDictionary<string, TEnum> ByName = Enum.GetValues<TEnum>()
        .ToFrozenDictionary(v => v.ToString(), StringComparer.OrdinalIgnoreCase);

    /// <summary>Maps enum values to their canonical string names.</summary>
    public static readonly FrozenDictionary<TEnum, string> ToName = Enum.GetValues<TEnum>()
        .ToFrozenDictionary(v => v, v => v.ToString());

    /// <summary>
    /// Tries to parse <paramref name="name"/> (case-insensitive) into a <typeparamref name="TEnum"/> value.
    /// </summary>
    public static bool TryGetByName(string name, out TEnum value) => ByName.TryGetValue(name, out value);

    /// <summary>
    /// Returns the canonical string name of <paramref name="value"/>, or its numeric representation
    /// if no name is defined.
    /// </summary>
    public static string GetName(TEnum value) => ToName.TryGetValue(value, out var name) ? name : value.ToString();
}
