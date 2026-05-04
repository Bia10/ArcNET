namespace ArcNET.Editor;

/// <summary>
/// Staged generator-pane update for one object/proto inspector target.
/// Null properties preserve the current value.
/// </summary>
public sealed class EditorObjectInspectorGeneratorUpdate
{
    public int? GeneratorData { get; init; }

    public bool HasChanges => GeneratorData.HasValue;
}
