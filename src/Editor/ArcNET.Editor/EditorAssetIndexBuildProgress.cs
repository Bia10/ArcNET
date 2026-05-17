namespace ArcNET.Editor;

internal readonly record struct EditorAssetIndexBuildProgress(
    string Activity,
    float Progress,
    int CompletedPhases,
    int TotalPhases
);
