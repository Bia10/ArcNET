using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Typed script-attachments pane contract for one object/proto inspector target.
/// Known attachment points are always projected in stable <see cref="ScriptAttachmentPoint"/> order.
/// </summary>
public sealed class EditorObjectInspectorScriptAttachmentsSummary
{
    /// <summary>
    /// Top-level inspector summary that owns this script-attachments contract.
    /// </summary>
    public required EditorObjectInspectorSummary Inspector { get; init; }

    /// <summary>
    /// Known attachment points in stable <see cref="ScriptAttachmentPoint"/> order.
    /// </summary>
    public required IReadOnlyList<EditorObjectInspectorScriptAttachment> Attachments { get; init; }

    /// <summary>
    /// Unknown non-empty script attachment slots beyond the named <see cref="ScriptAttachmentPoint"/> range.
    /// </summary>
    public required IReadOnlyList<EditorObjectInspectorUnknownScriptAttachment> UnknownAttachments { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the target uses one or more non-empty unknown attachment slots.
    /// </summary>
    public bool HasUnknownAttachmentSlots => UnknownAttachments.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when one or more known or unknown slots reference scripts that are not loaded.
    /// </summary>
    public bool HasMissingScripts =>
        Attachments.Any(static attachment => attachment.IsMissingScript)
        || UnknownAttachments.Any(static attachment => attachment.IsMissingScript);

    internal static EditorObjectInspectorScriptAttachmentsSummary Create(
        EditorObjectInspectorSummary inspector,
        IReadOnlyList<ObjectProperty> properties,
        Func<int, EditorObjectInspectorScriptReference?> resolveScript
    )
    {
        ArgumentNullException.ThrowIfNull(inspector);
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentNullException.ThrowIfNull(resolveScript);

        var scripts = GetScripts(properties);
        var attachments = Enum.GetValues<ScriptAttachmentPoint>()
            .Select(point => CreateKnownAttachment(point, scripts, resolveScript))
            .ToArray();
        var unknownAttachments = new List<EditorObjectInspectorUnknownScriptAttachment>();

        for (var slotIndex = 0; slotIndex < scripts.Length; slotIndex++)
        {
            if (ScriptValidator.IsKnownAttachmentSlot(slotIndex) || IsEmptyScriptAttachment(scripts[slotIndex]))
                continue;

            var script = scripts[slotIndex];
            unknownAttachments.Add(
                new EditorObjectInspectorUnknownScriptAttachment
                {
                    SlotIndex = slotIndex,
                    Flags = script.Flags,
                    Counters = script.Counters,
                    ScriptId = script.ScriptId,
                    Script = script.ScriptId > 0 ? resolveScript(script.ScriptId) : null,
                }
            );
        }

        return new EditorObjectInspectorScriptAttachmentsSummary
        {
            Inspector = inspector,
            Attachments = attachments,
            UnknownAttachments = unknownAttachments,
        };
    }

    private static EditorObjectInspectorScriptAttachment CreateKnownAttachment(
        ScriptAttachmentPoint attachmentPoint,
        IReadOnlyList<ObjectPropertyScript> scripts,
        Func<int, EditorObjectInspectorScriptReference?> resolveScript
    )
    {
        var slotIndex = (int)attachmentPoint;
        var script = slotIndex < scripts.Count ? scripts[slotIndex] : default;

        return new EditorObjectInspectorScriptAttachment
        {
            AttachmentPoint = attachmentPoint,
            Flags = script.Flags,
            Counters = script.Counters,
            ScriptId = script.ScriptId,
            Script = script.ScriptId > 0 ? resolveScript(script.ScriptId) : null,
        };
    }

    private static ObjectPropertyScript[] GetScripts(IReadOnlyList<ObjectProperty> properties)
    {
        for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
        {
            var property = properties[propertyIndex];
            if (property.Field == ObjectField.ObjFScriptsIdx)
                return property.GetScriptArray();
        }

        return [];
    }

    private static bool IsEmptyScriptAttachment(ObjectPropertyScript script) =>
        script.ScriptId == 0 && script.Flags == 0 && script.Counters == 0;
}
