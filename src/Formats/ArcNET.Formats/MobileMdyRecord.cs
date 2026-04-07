using System.Diagnostics.CodeAnalysis;

namespace ArcNET.Formats;

/// <summary>
/// A single record within a <c>mobile.mdy</c> file.
/// Each entry is either a standard game object (<see cref="MobData"/>) or a v2
/// character record (<see cref="CharacterMdyRecord"/>).
/// Exactly one of <see cref="Mob"/> and <see cref="Character"/> is non-null.
/// </summary>
public sealed class MobileMdyRecord
{
    private MobileMdyRecord() { }

    /// <summary>Standard game object, or <see langword="null"/> when this is a character record.</summary>
    public MobData? Mob { get; private init; }

    /// <summary>V2 character record, or <see langword="null"/> when this is a standard mob record.</summary>
    public CharacterMdyRecord? Character { get; private init; }

    /// <summary><see langword="true"/> when this record wraps a standard <see cref="MobData"/>.</summary>
    [MemberNotNullWhen(true, nameof(Mob))]
    [MemberNotNullWhen(false, nameof(Character))]
    public bool IsMob => Mob is not null;

    /// <summary><see langword="true"/> when this record wraps a v2 <see cref="CharacterMdyRecord"/>.</summary>
    [MemberNotNullWhen(true, nameof(Character))]
    [MemberNotNullWhen(false, nameof(Mob))]
    public bool IsCharacter => Character is not null;

    /// <summary>Wraps a standard game object.</summary>
    public static MobileMdyRecord FromMob(MobData mob) => new() { Mob = mob };

    /// <summary>Wraps a v2 character record.</summary>
    public static MobileMdyRecord FromCharacter(CharacterMdyRecord character) => new() { Character = character };
}
