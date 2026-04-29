namespace ArcNET.Editor.Runtime;

/// <summary>
/// Runtime substructure IDs observed when Arcanum reads the character sheet.
/// These IDs were recovered from the supplied Cheat Engine table and line up with
/// ArcNET's typed character save surfaces.
/// </summary>
public enum CharacterSheetSubstructureId
{
    MainStats = 0xDC,
    BasicSkills = 0xDD,
    TechSkills = 0xDE,
    SpellAndTech = 0xDF,
}
