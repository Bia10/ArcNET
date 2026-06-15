using ArcNET.GameObjects.Metadata;

namespace ArcNET.GameObjects.Tests;

public sealed class ObjectFieldMetadataCatalogTests
{
    [Test]
    public async Task TryGetFieldId_WhenRawNameExists_ResolvesKnownField()
    {
        var resolved = ObjectFieldMetadataCatalog.TryGetFieldId("OBJ_F_LOCATION", out var fieldId);

        await Assert.That(resolved).IsTrue();
        await Assert.That(fieldId).IsEqualTo(2);
        await Assert.That(ObjectFieldMetadataCatalog.DisplayName(fieldId)).IsEqualTo("Location");
        await Assert.That(ObjectFieldMetadataCatalog.TryGetField("OBJ_F_LOCATION", out var field)).IsTrue();
        await Assert.That(field).IsEqualTo(ObjectField.Location);
    }

    [Test]
    public async Task ArrayElementName_WhenSkillFieldIsUsed_ResolvesSemanticLabel()
    {
        _ = ObjectFieldMetadataCatalog.TryGetFieldId("OBJ_F_CRITTER_BASIC_SKILL_IDX", out var fieldId);
        _ = ObjectFieldMetadataCatalog.TryGetFieldId("OBJ_F_RENDER_COLOR", out var renderFieldId);

        await Assert.That(ObjectFieldMetadataCatalog.ArrayElementName(fieldId, 0)).IsEqualTo("Bow");
        await Assert.That(ObjectFieldMetadataCatalog.IsNoiseField(renderFieldId)).IsTrue();
    }
}
