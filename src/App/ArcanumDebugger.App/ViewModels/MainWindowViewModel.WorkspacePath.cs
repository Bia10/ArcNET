using ArcNET.Diagnostics;
using CommunityToolkit.Mvvm.Input;

namespace ArcanumDebugger.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    partial void OnWorkspaceOverridePathTextChanged(string value)
    {
        if (_activeSessionHandle is not null)
        {
            ApplySessionSnapshot(
                _sessionService.SetWorkspacePathHint(_activeSessionHandle, ResolveWorkspacePathOverride())
            );
            return;
        }

        if (ActiveSession is not { } session)
            return;

        WorkspaceSourceText = CreateWorkspaceSourceText(session);
        InvalidateLoadedLogbookSnapshotIfRequestChanged();
        ApplySessionLogbookEditorState(session);
        ApplySessionGameDataCatalogState(session);
    }

    [RelayCommand]
    private void ClearWorkspaceOverridePath() => WorkspaceOverridePathText = string.Empty;

    private string? ResolveWorkspacePathOverride()
    {
        var trimmed = WorkspaceOverridePathText.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private string ResolveEffectiveWorkspacePath(AttachedSessionSnapshot session) =>
        ResolveWorkspacePathOverride() ?? session.LocalWorkspacePath;

    private string CreateWorkspaceDisplayName(AttachedSessionSnapshot session)
    {
        var workspacePath = ResolveEffectiveWorkspacePath(session);
        if (!string.IsNullOrWhiteSpace(workspacePath))
        {
            var trimmedPath = workspacePath.TrimEnd(
                global::System.IO.Path.DirectorySeparatorChar,
                global::System.IO.Path.AltDirectorySeparatorChar
            );
            var pathName = global::System.IO.Path.GetFileName(trimmedPath);
            if (!string.IsNullOrWhiteSpace(pathName))
                return pathName;
        }

        return CreateModuleCatalogDisplayName(session.Fingerprint);
    }

    private string CreateWorkspaceSourceText(AttachedSessionSnapshot session) =>
        ResolveWorkspacePathOverride() is null
            ? $"Local workspace: {CreateWorkspaceDisplayName(session)}"
            : $"Local workspace override: {CreateWorkspaceDisplayName(session)}";

    private static string NormalizeWorkspacePathKey(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
