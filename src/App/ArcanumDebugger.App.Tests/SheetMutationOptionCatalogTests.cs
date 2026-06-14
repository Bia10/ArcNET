using System.Linq;
using ArcanumDebugger.App.ViewModels;
using ArcNET.Diagnostics;

namespace ArcanumDebugger.App.Tests;

public sealed class SheetMutationOptionCatalogTests
{
    [Test]
    public async Task Describe_WhenSpellMastery_ReturnsCollegeSelectorIncludingNone()
    {
        var descriptor = SheetMutationOptionCatalog.Describe(SheetRoute.SpellMastery);

        await Assert.That(descriptor.ShowsValueSelector).IsTrue();
        await Assert.That(descriptor.ShowsValueInput).IsFalse();
        await Assert.That(descriptor.ValueOptions[0].Token).IsEqualTo("none");
        await Assert.That(descriptor.ValueOptions.Any(option => option.Token == "Fire")).IsTrue();
    }

    [Test]
    public async Task ResolveValueOption_WhenSpellMasteryUsesNumericId_ReturnsCanonicalCollegeOption()
    {
        var option = SheetMutationOptionCatalog.ResolveValueOption(SheetRoute.SpellMastery, "0");

        await Assert.That(option).IsNotNull();
        await Assert.That(option!.Token).IsEqualTo(SpellTechCatalog.SpellCollegeName(0));
    }

    [Test]
    public async Task Describe_WhenSkillRoute_ReturnsTrainingSelectorWithKeepCurrent()
    {
        var descriptor = SheetMutationOptionCatalog.Describe(SheetRoute.BasicSkill);

        await Assert.That(descriptor.ShowsTrainingSelector).IsTrue();
        await Assert.That(descriptor.ShowsTrainingInput).IsFalse();
        await Assert.That(descriptor.TrainingOptions[0].Label).IsEqualTo("Keep Current");
        await Assert
            .That(SheetMutationOptionCatalog.ResolveTrainingOption(""))
            .IsEqualTo(descriptor.TrainingOptions[0]);
    }

    [Test]
    public async Task Describe_WhenStatRoute_UsesManualValueInputAndHidesTraining()
    {
        var descriptor = SheetMutationOptionCatalog.Describe(SheetRoute.Stat);

        await Assert.That(descriptor.ShowsValueSelector).IsFalse();
        await Assert.That(descriptor.ShowsValueInput).IsTrue();
        await Assert.That(descriptor.ShowsTrainingSelector).IsFalse();
        await Assert.That(descriptor.ShowsTrainingInput).IsFalse();
    }

    [Test]
    public async Task Describe_WhenGenderReference_ReturnsNamedSelector()
    {
        var descriptor = SheetMutationOptionCatalog.Describe(new SheetReference(SheetRoute.Stat, 26, "Gender"));

        await Assert.That(descriptor.ShowsValueSelector).IsTrue();
        await Assert.That(descriptor.ShowsValueInput).IsFalse();
        await Assert.That(descriptor.ValueOptions.Select(static option => option.Token)).Contains("Male");
        await Assert.That(descriptor.ValueOptions.Select(static option => option.Token)).Contains("Female");
    }

    [Test]
    public async Task ResolveValueOption_WhenRaceUsesNumericId_ReturnsCanonicalRaceOption()
    {
        var option = SheetMutationOptionCatalog.ResolveValueOption(
            new SheetReference(SheetRoute.Stat, 27, "Race"),
            "9"
        );

        await Assert.That(option).IsNotNull();
        await Assert.That(option!.Token).IsEqualTo("Elf");
    }
}
