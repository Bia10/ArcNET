using ArcNET.App.Output;
using ArcNET.Editor;

namespace ArcNET.App.Rendering;

internal static class EditorOutlineRenderer
{
    public static void Render(EditorMapProjection projection, EditorMapPreviewMode mode, IEditorOutput output)
    {
        var preview = EditorMapPreviewBuilder.Build(projection, mode);

        output.WriteLine($"Legend: {preview.Legend}");

        foreach (var line in preview.Rows)
            output.WriteLine(line);
    }
}
