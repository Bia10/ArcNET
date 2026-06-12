namespace ArcNET.Editor;

internal static class EditorParallelism
{
    public static int InteractiveMaxDegreeOfParallelism => Math.Max(1, Environment.ProcessorCount - 1);
}
