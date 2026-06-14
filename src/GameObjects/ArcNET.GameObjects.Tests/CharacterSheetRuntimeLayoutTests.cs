using ArcNET.GameObjects.Runtime;

namespace ArcNET.GameObjects.Tests;

public sealed class CharacterSheetRuntimeLayoutTests
{
    [Test]
    public async Task MainStatsFields_ExposeExpectedAnchors()
    {
        await Assert.That(CharacterSheetRuntimeLayout.MainStatsFields.Count).IsEqualTo(13);
        await Assert
            .That(CharacterSheetRuntimeLayout.MainStatsFields[0])
            .IsEqualTo(new RuntimeFieldDescriptor("Strength", 0x0C));
        await Assert
            .That(CharacterSheetRuntimeLayout.MainStatsFields[8])
            .IsEqualTo(new RuntimeFieldDescriptor("Level", 0x50));
        await Assert
            .That(CharacterSheetRuntimeLayout.MainStatsFields[^1])
            .IsEqualTo(new RuntimeFieldDescriptor("SkillPoints", 0x60));
    }

    [Test]
    public async Task SkillFields_ExposeExpectedAnchors()
    {
        await Assert.That(CharacterSheetRuntimeLayout.BasicSkillsFields.Count).IsEqualTo(12);
        await Assert
            .That(CharacterSheetRuntimeLayout.BasicSkillsFields[0])
            .IsEqualTo(new RuntimeFieldDescriptor("Bow", 0x0C));
        await Assert
            .That(CharacterSheetRuntimeLayout.BasicSkillsFields[^1])
            .IsEqualTo(new RuntimeFieldDescriptor("Persuasion", 0x38));
        await Assert
            .That(CharacterSheetRuntimeLayout.TechSkillsFields)
            .IsEquivalentTo([
                new RuntimeFieldDescriptor("Repair", 0x0C),
                new RuntimeFieldDescriptor("Firearms", 0x10),
                new RuntimeFieldDescriptor("PickLocks", 0x14),
                new RuntimeFieldDescriptor("DisarmTraps", 0x18),
            ]);
        await Assert.That(CharacterSheetRuntimeLayout.SpellAndTechFields.Count).IsEqualTo(24);
        await Assert
            .That(CharacterSheetRuntimeLayout.SpellAndTechFields[0])
            .IsEqualTo(new RuntimeFieldDescriptor("Conveyance", 0x0C));
        await Assert
            .That(CharacterSheetRuntimeLayout.SpellAndTechFields[^1])
            .IsEqualTo(new RuntimeFieldDescriptor("Therapeutics", 0x6C));
    }
}
