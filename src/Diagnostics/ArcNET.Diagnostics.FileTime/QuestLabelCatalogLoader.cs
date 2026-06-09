using ArcNET.Archive;
using ArcNET.Formats;

namespace ArcNET.Diagnostics;

public static class QuestLabelCatalogLoader
{
    public static QuestLabelCatalogSnapshot? TryLoadFromSaveDirectory(string saveDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(saveDir);

        var gameDir = Path.GetFullPath(Path.Combine(saveDir, "..", "..", ".."));

        foreach (var path in EnumerateLooseQuestFiles(gameDir))
        {
            try
            {
                var mes = MessageFormat.ParseFile(path);
                var catalog = BuildCatalog(mes, path);
                if (catalog is not null)
                    return catalog;
            }
            catch
            {
                // Keep probing other candidates.
            }
        }

        foreach (var datPath in EnumerateDatArchives(gameDir))
        {
            try
            {
                using var archive = DatArchive.Open(datPath);
                var entry = archive.Entries.FirstOrDefault(candidate =>
                    IsQuestLookupCandidate(Path.GetFileName(candidate.Path))
                );
                if (entry is null)
                    continue;

                var mes = MessageFormat.ParseMemory(archive.ReadEntry(entry));
                var source = $"{Path.GetFileName(datPath)}:{entry.Path.Replace('\\', '/')}";
                var catalog = BuildCatalog(mes, source);
                if (catalog is not null)
                    return catalog;
            }
            catch
            {
                // Keep probing other archives.
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateLooseQuestFiles(string gameDir)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        string[] roots =
        [
            Path.Combine(gameDir, "data"),
            Path.Combine(gameDir, "modules"),
            Path.Combine(gameDir, "modules", "Arcanum"),
        ];

        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
                continue;

            foreach (var fileName in s_candidateFileNames)
            {
                foreach (var path in Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories))
                {
                    if (seen.Add(path))
                        yield return path;
                }
            }
        }
    }

    private static IEnumerable<string> EnumerateDatArchives(string gameDir)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        string[] roots = [gameDir, Path.Combine(gameDir, "modules")];

        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
                continue;

            foreach (var path in Directory.EnumerateFiles(root, "*.dat", SearchOption.TopDirectoryOnly).Order())
            {
                if (seen.Add(path))
                    yield return path;
            }
        }
    }

    private static QuestLabelCatalogSnapshot? BuildCatalog(MesFile mes, string source)
    {
        Dictionary<int, string> labels = [];
        foreach (var entry in mes.Entries)
        {
            if (entry.Index < 1000 || string.IsNullOrWhiteSpace(entry.Text) || labels.ContainsKey(entry.Index))
                continue;

            var normalized = string.Join(
                ' ',
                entry
                    .Text.Replace('\r', ' ')
                    .Replace('\n', ' ')
                    .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
            );

            if (normalized.Length > 0)
                labels[entry.Index] = normalized;
        }

        if (source.Contains("Module template", StringComparison.OrdinalIgnoreCase) && labels.Count <= 1)
            return null;

        return labels.Count == 0 ? null : new QuestLabelCatalogSnapshot(source, labels);
    }

    private static bool IsQuestLookupCandidate(string fileName) =>
        s_candidateFileNames.Contains(fileName, StringComparer.OrdinalIgnoreCase);

    private static readonly string[] s_candidateFileNames =
    [
        "quests.mes",
        "gamequestlog.mes",
        "gamequestlogdumb.mes",
        "gamequest.mes",
    ];
}
