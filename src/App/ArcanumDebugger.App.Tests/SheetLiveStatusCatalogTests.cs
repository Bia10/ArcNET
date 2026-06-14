using ArcanumDebugger.App.ViewModels;
using ArcNET.Diagnostics;

namespace ArcanumDebugger.App.Tests;

public sealed class SheetLiveStatusCatalogTests
{
    [Test]
    public async Task Describe_WhenGenderStatUsesMeaning_ReturnsCanonicalEditorToken()
    {
        var result = SheetLiveStatusCatalog.Describe(
            new SheetReference(SheetRoute.Stat, 26, "Gender"),
            CreateSnapshot(
                "Companion Sogg Mead Mug",
                SheetRoute.Stat,
                [
                    new ReadValueSnapshot("id", "Stat Id", "26"),
                    new ReadValueSnapshot("name", "Stat", "Gender"),
                    new ReadValueSnapshot("value", "Value", "1"),
                    new ReadValueSnapshot("value_name", "Meaning", "Female"),
                ]
            )
        );

        await Assert.That(result.StatusText).IsEqualTo("Current field state");
        await Assert.That(result.ValueTokenText).IsEqualTo("Female");
        await Assert.That(result.SummaryText).Contains("Female");
        await Assert.That(result.SummaryText).Contains("Companion Sogg Mead Mug");
    }

    [Test]
    public async Task Describe_WhenSkillRouteIncludesTraining_ReturnsPointsAndTrainingTokens()
    {
        var result = SheetLiveStatusCatalog.Describe(
            new SheetReference(SheetRoute.BasicSkill, 9, "Haggle"),
            CreateSnapshot(
                "Virgil",
                SheetRoute.BasicSkill,
                [
                    new ReadValueSnapshot("id", "Basic Skill Id", "9"),
                    new ReadValueSnapshot("name", "Basic Skill", "Haggle"),
                    new ReadValueSnapshot("value", "Value", "6"),
                    new ReadValueSnapshot("training", "Training", "2"),
                    new ReadValueSnapshot("training_name", "Training Name", "Expert"),
                    new ReadValueSnapshot("encoded", "Encoded", "134"),
                ]
            )
        );

        await Assert.That(result.StatusText).IsEqualTo("Current skill state");
        await Assert.That(result.ValueTokenText).IsEqualTo("6");
        await Assert.That(result.TrainingTokenText).IsEqualTo("expert");
        await Assert.That(result.SummaryText).Contains("expert");
    }

    [Test]
    public async Task Describe_WhenSpellMasteryIsCleared_UsesNoneToken()
    {
        var result = SheetLiveStatusCatalog.Describe(
            new SheetReference(SheetRoute.SpellMastery, 16, "Spell Mastery"),
            CreateSnapshot(
                "Player",
                SheetRoute.SpellMastery,
                [
                    new ReadValueSnapshot("id", "Spell-Tech Slot", "16"),
                    new ReadValueSnapshot("name", "Field", "Spell Mastery"),
                    new ReadValueSnapshot("value", "Value", "-1"),
                    new ReadValueSnapshot("value_name", "College", "None"),
                ]
            )
        );

        await Assert.That(result.StatusText).IsEqualTo("Current spell mastery");
        await Assert.That(result.ValueTokenText).IsEqualTo("none");
        await Assert.That(result.TrainingTokenText).IsEqualTo(string.Empty);
    }

    private static SheetSnapshot CreateSnapshot(
        string targetText,
        SheetRoute route,
        IReadOnlyList<ReadValueSnapshot> values
    ) =>
        new(
            DateTimeOffset.UtcNow,
            IsAvailable: true,
            "Sheet read completed",
            "Read sheet value.",
            "0x0000000201234567",
            targetText,
            "field",
            route,
            values,
            []
        );
}
