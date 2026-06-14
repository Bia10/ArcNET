namespace ArcanumDebugger.App.ViewModels;

internal sealed record class LogbookLoadedSnapshotInvalidation(string DisplaySummaryText, string EditorSummaryText);

internal static class LogbookLoadedSnapshotStateCatalog
{
    public static bool TryCreateInvalidation(
        string? loadedHandleTokenText,
        string? loadedPageTokenText,
        string? currentHandleTokenText,
        string? currentPageTokenText,
        out LogbookLoadedSnapshotInvalidation invalidation
    )
    {
        invalidation = default!;

        var loadedHandle = Normalize(loadedHandleTokenText);
        var loadedPage = Normalize(loadedPageTokenText);
        if (loadedHandle.Length == 0 || loadedPage.Length == 0)
            return false;

        var currentHandle = Normalize(currentHandleTokenText);
        var currentPage = Normalize(currentPageTokenText);
        var targetChanged = !loadedHandle.Equals(currentHandle, StringComparison.OrdinalIgnoreCase);
        var pageChanged = !loadedPage.Equals(currentPage, StringComparison.OrdinalIgnoreCase);
        if (!targetChanged && !pageChanged)
            return false;

        var pagePhrase = ResolvePageReloadPhrase(currentPageTokenText, loadedPageTokenText);
        invalidation = new(
            CreateDisplaySummary(targetChanged, pageChanged, pagePhrase),
            CreateEditorSummary(targetChanged, pageChanged, pagePhrase)
        );
        return true;
    }

    private static string CreateDisplaySummary(bool targetChanged, bool pageChanged, string pagePhrase) =>
        (targetChanged, pageChanged) switch
        {
            (true, true) =>
                $"The loaded journal view belonged to a different player or companion and a different journal page. Read {pagePhrase} again for the current target.",
            (true, false) =>
                $"The loaded journal view belonged to a different player or companion. Read {pagePhrase} again for the current target.",
            (false, true) =>
                $"The loaded journal view belonged to a different journal page. Read {pagePhrase} again for the current target.",
            _ => string.Empty,
        };

    private static string CreateEditorSummary(bool targetChanged, bool pageChanged, string pagePhrase) =>
        (targetChanged, pageChanged) switch
        {
            (true, true) =>
                $"The loaded live editor shortcuts belonged to a different target and page. Read {pagePhrase} again to rebuild companion-safe journal shortcuts.",
            (true, false) =>
                $"The loaded live editor shortcuts belonged to a different player or companion. Read {pagePhrase} again to rebuild journal shortcuts for this target.",
            (false, true) =>
                $"The loaded live editor shortcuts belonged to a different journal page. Read {pagePhrase} again to rebuild the current shortcut list.",
            _ => string.Empty,
        };

    private static string ResolvePageReloadPhrase(string? currentPageTokenText, string? loadedPageTokenText)
    {
        var preferredToken = string.IsNullOrWhiteSpace(currentPageTokenText)
            ? loadedPageTokenText
            : currentPageTokenText;
        var label = ResolvePageLabel(preferredToken).Trim();
        return label.Equals("All pages", StringComparison.OrdinalIgnoreCase)
            ? "all pages"
            : $"the {label.ToLowerInvariant()} page";
    }

    private static string ResolvePageLabel(string? token) =>
        token?.Trim().ToLowerInvariant() switch
        {
            "all" => "All pages",
            "rumors" or "rumorsandnotes" or "notes" => "Rumors and notes",
            "quests" => "Quest journal",
            "reputations" or "reputation" => "Reputations",
            "blessings" or "blessingsandcurses" or "curses" => "Blessings and curses",
            "kills" or "killsandinjuries" or "injuries" => "Kills and injuries",
            "background" => "Background",
            "keys" or "keyring" or "keyringcontents" => "Keyring",
            _ => string.IsNullOrWhiteSpace(token) ? "selected journal page" : token.Trim(),
        };

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
}
