using System.Buffers.Binary;
using System.Reflection;
using System.Text.Json;
using ArcNET.BinaryPatch.Patches;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.BinaryPatch.Json;

/// <summary>
/// Builds a <see cref="BinaryPatchSet"/> from a JSON patch file.
/// </summary>
/// <remarks>
/// Compatible with .NET Native AOT via <see cref="PatchSetJsonContext"/> source generation.
/// The JSON schema is described by <see cref="PatchSetDescriptor"/> and
/// <see cref="PatchDescriptor"/>.
/// </remarks>
public static class JsonPatchLoader
{
    /// <summary>
    /// Deserialises <paramref name="jsonText"/> and builds a <see cref="BinaryPatchSet"/>.
    /// </summary>
    /// <param name="jsonText">UTF-8 JSON content of a patch file.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required JSON fields are missing or a patch type is unrecognised.
    /// </exception>
    public static BinaryPatchSet Load(string jsonText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonText);

        var descriptor =
            JsonSerializer.Deserialize(jsonText, PatchSetJsonContext.Default.PatchSetDescriptor)
            ?? throw new InvalidOperationException("Failed to deserialize patch set: root object was null.");

        var patches = new IBinaryPatch[descriptor.Patches.Count];
        for (var i = 0; i < descriptor.Patches.Count; i++)
            patches[i] = BuildPatch(descriptor.Patches[i]);

        return new BinaryPatchSet
        {
            Name = descriptor.Name,
            Version = descriptor.Version,
            Patches = patches,
        };
    }

    /// <summary>
    /// Reads <paramref name="path"/> from the file system and delegates to <see cref="Load"/>.
    /// </summary>
    /// <param name="path">Path to a <c>.json</c> patch file.</param>
    public static BinaryPatchSet LoadFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Load(File.ReadAllText(path, System.Text.Encoding.UTF8));
    }

    /// <summary>
    /// Loads a <c>.json</c> patch file embedded as a manifest resource in <paramref name="assembly"/>.
    /// </summary>
    /// <param name="assembly">The assembly that contains the embedded resource.</param>
    /// <param name="resourceName">
    /// The fully-qualified manifest resource name.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no manifest resource with <paramref name="resourceName"/> is found.
    /// </exception>
    public static BinaryPatchSet LoadEmbedded(Assembly assembly, string resourceName)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        using var stream =
            assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found in assembly '{assembly.GetName().Name}'."
            );

        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        return Load(reader.ReadToEnd());
    }

    private static IBinaryPatch BuildPatch(PatchDescriptor d) =>
        d.Type switch
        {
            "ProtoFieldSetInt32" => ProtoFieldPatch.SetInt32(
                d.Id,
                d.Description,
                d.RelativePath,
                RequireField(d),
                RequireExpectedValue(d),
                RequireNewValue(d)
            ),
            "ProtoFieldClearInt32" => ProtoFieldPatch.Custom(
                d.Id,
                d.Description,
                d.RelativePath,
                RequireField(d),
                needsApplyPredicate: prop => prop.GetInt32() != RequireNewValue(d),
                transform: prop => BuildInt32Property(prop, RequireNewValue(d))
            ),
            "MobFieldSetInt32" => MobFieldPatch.SetInt32(
                d.Id,
                d.Description,
                d.RelativePath,
                RequireField(d),
                RequireExpectedValue(d),
                RequireNewValue(d)
            ),
            "MobFieldClearInt32" => MobFieldPatch.Custom(
                d.Id,
                d.Description,
                d.RelativePath,
                RequireField(d),
                needsApplyPredicate: prop => prop.GetInt32() != RequireNewValue(d),
                transform: prop => BuildInt32Property(prop, RequireNewValue(d))
            ),
            "RawAtOffset" => BuildRawAtOffset(d),
            _ => throw new NotSupportedException($"Unknown patch type: '{d.Type}'."),
        };

    private static ObjectField RequireField(PatchDescriptor d)
    {
        if (d.Field is null)
            throw new InvalidOperationException($"Patch '{d.Id}': 'field' is required for type '{d.Type}'.");

        if (!Enum.TryParse<ObjectField>(d.Field, ignoreCase: false, out var result))
            throw new InvalidOperationException($"Patch '{d.Id}': unknown ObjectField '{d.Field}'.");

        return result;
    }

    private static int RequireExpectedValue(PatchDescriptor d) =>
        d.ExpectedValue
        ?? throw new InvalidOperationException($"Patch '{d.Id}': 'expectedValue' is required for type '{d.Type}'.");

    private static int RequireNewValue(PatchDescriptor d) =>
        d.NewValue
        ?? throw new InvalidOperationException($"Patch '{d.Id}': 'newValue' is required for type '{d.Type}'.");

    private static RawBinaryPatch BuildRawAtOffset(PatchDescriptor d)
    {
        var offset = d.Offset ?? Throw<int>($"Patch '{d.Id}': 'offset' is required for type '{d.Type}'.");
        var expected = Convert.FromHexString(
            d.ExpectedHex ?? Throw<string>($"Patch '{d.Id}': 'expectedHex' is required for type '{d.Type}'.")
        );
        var replacement = Convert.FromHexString(
            d.NewHex ?? Throw<string>($"Patch '{d.Id}': 'newHex' is required for type '{d.Type}'.")
        );

        var target = new PatchTarget(d.RelativePath, PatchTargetFormat.Raw)
        {
            SourceDatPath = d.SourceDatPath,
            DatEntryPath = d.DatEntryPath,
        };

        return RawBinaryPatch.AtOffset(d.Id, d.Description, target, offset, expected, replacement);
    }

    private static ObjectProperty BuildInt32Property(ObjectProperty source, int value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        return new ObjectProperty { Field = source.Field, RawBytes = bytes };
    }

    /// <summary>
    /// Helper that satisfies the compiler's non-nullable constraint inside <c>??</c> expressions
    /// without a separate local variable.
    /// </summary>
    private static T Throw<T>(string message) => throw new InvalidOperationException(message);
}
