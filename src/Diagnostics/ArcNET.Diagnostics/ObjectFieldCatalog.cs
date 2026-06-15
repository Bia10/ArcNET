using ArcNET.GameObjects;
using ArcNET.GameObjects.Metadata;

namespace ArcNET.Diagnostics;

public static class ObjectFieldCatalog
{
    public static IReadOnlyList<ObjectFieldDescriptor> Fields => s_fields;

    public static string RawName(int fieldId) => ObjectFieldMetadataCatalog.RawName(fieldId);

    public static string RawName(ObjectField field) => ObjectFieldMetadataCatalog.RawName(field);

    public static bool TryGetFieldId(string rawName, out int fieldId) =>
        ObjectFieldMetadataCatalog.TryGetFieldId(rawName, out fieldId);

    public static bool TryGetField(string rawName, out ObjectField field) =>
        ObjectFieldMetadataCatalog.TryGetField(rawName, out field);

    public static string DisplayName(int fieldId) => ObjectFieldMetadataCatalog.DisplayName(fieldId);

    public static string DisplayName(ObjectField field) => ObjectFieldMetadataCatalog.DisplayName(field);

    public static string CollectionName(int fieldId) => ObjectFieldMetadataCatalog.CollectionName(fieldId);

    public static string CollectionName(ObjectField field) => ObjectFieldMetadataCatalog.CollectionName(field);

    public static string ArrayElementName(int fieldId, int index) =>
        ObjectFieldMetadataCatalog.ArrayElementName(fieldId, index);

    public static bool IsNoiseField(int fieldId) => ObjectFieldMetadataCatalog.IsNoiseField(fieldId);

    public static string ResistanceName(int index) => CharacterSheetMetadata.ResistanceName(index);

    public static string BasicSkillName(int index) => CharacterSheetMetadata.BasicSkillName(index);

    public static string TechSkillName(int index) => CharacterSheetMetadata.TechSkillName(index);

    public static string SpellCollegeName(int index) => CharacterSheetMetadata.SpellCollegeName(index);

    public static string TrainingName(int training) => CharacterSheetMetadata.TrainingName(training);

    private static readonly ObjectFieldDescriptor[] s_fields =
    [
        .. ObjectFieldMetadataCatalog.Fields.Select(static field => new ObjectFieldDescriptor(
            field.FieldId,
            field.RawName,
            field.DisplayName,
            field.CollectionName,
            field.IsNoise
        )),
    ];
}
