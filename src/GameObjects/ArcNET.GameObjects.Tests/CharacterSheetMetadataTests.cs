using ArcNET.GameObjects.Metadata;

namespace ArcNET.GameObjects.Tests;

public sealed class CharacterSheetMetadataTests
{
    [Test]
    public async Task CharacterLabels_ExposeRepresentativeStatSkillAndSpellNames()
    {
        await Assert.That(CharacterSheetMetadata.StatName(0)).IsEqualTo("Strength");
        await Assert.That(CharacterSheetMetadata.BasicSkillName(0)).IsEqualTo("Bow");
        await Assert.That(CharacterSheetMetadata.TechSkillName(3)).IsEqualTo("Disarm Traps");
        await Assert.That(CharacterSheetMetadata.SpellCollegeName(11)).IsEqualTo("Necromantic Black");
        await Assert.That(CharacterSheetMetadata.SpellTechSlotName(16)).IsEqualTo("Spell Mastery");
        await Assert.That(CharacterSheetMetadata.SpellTechSlotName(24)).IsEqualTo("Therapeutics Discipline");
        await Assert.That(CharacterSheetMetadata.TrainingName(3)).IsEqualTo("Master");
        await Assert.That(CharacterSheetMetadata.RaceName(9)).IsEqualTo("Elf");
        await Assert.That(CharacterSheetMetadata.GenderName(1)).IsEqualTo("Female");
    }
}
