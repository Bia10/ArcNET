namespace ArcNET.Diagnostics.Tests;

public sealed class ObjectFieldCatalogTests
{
    [Test]
    public async Task TryGetFieldId_WhenRawNameExists_ResolvesKnownField()
    {
        var resolved = ObjectFieldCatalog.TryGetFieldId("OBJ_F_LOCATION", out var fieldId);

        await Assert.That(resolved).IsTrue();
        await Assert.That(fieldId).IsEqualTo(2);
        await Assert.That(ObjectFieldCatalog.DisplayName(fieldId)).IsEqualTo("Location");
    }

    [Test]
    public async Task ArrayElementName_WhenSkillFieldIsUsed_ResolvesSemanticLabel()
    {
        _ = ObjectFieldCatalog.TryGetFieldId("OBJ_F_CRITTER_BASIC_SKILL_IDX", out var fieldId);
        _ = ObjectFieldCatalog.TryGetFieldId("OBJ_F_RENDER_COLOR", out var renderFieldId);

        await Assert.That(ObjectFieldCatalog.ArrayElementName(fieldId, 0)).IsEqualTo("Bow");
        await Assert.That(ObjectFieldCatalog.IsNoiseField(renderFieldId)).IsTrue();
    }
}
