namespace ArcNET.Formats;

internal static class TfaiTreeTraversal
{
    public static void Traverse(
        IReadOnlyList<TfaiEntry> entries,
        string pathPrefix,
        Action<string, TfaiFileEntry>? onFile = null,
        Action<string, TfaiDirectoryEntry>? onDirectoryEnter = null,
        Action<string, TfaiDirectoryEntry>? onDirectoryExit = null
    )
    {
        foreach (var entry in entries)
        {
            switch (entry)
            {
                case TfaiFileEntry file:
                {
                    var path = pathPrefix.Length == 0 ? file.Name : $"{pathPrefix}/{file.Name}";
                    onFile?.Invoke(path, file);
                    break;
                }

                case TfaiDirectoryEntry dir:
                {
                    var path = pathPrefix.Length == 0 ? dir.Name : $"{pathPrefix}/{dir.Name}";
                    onDirectoryEnter?.Invoke(path, dir);
                    Traverse(dir.Children, path, onFile, onDirectoryEnter, onDirectoryExit);
                    onDirectoryExit?.Invoke(path, dir);
                    break;
                }
            }
        }
    }

    public static IReadOnlyList<TfaiEntry> MapEntries(
        IReadOnlyList<TfaiEntry> entries,
        string pathPrefix,
        Func<string, TfaiFileEntry, TfaiFileEntry> mapFile
    )
    {
        var result = new List<TfaiEntry>(entries.Count);
        foreach (var entry in entries)
        {
            switch (entry)
            {
                case TfaiFileEntry file:
                {
                    var path = pathPrefix.Length == 0 ? file.Name : $"{pathPrefix}/{file.Name}";
                    result.Add(mapFile(path, file));
                    break;
                }

                case TfaiDirectoryEntry dir:
                {
                    var path = pathPrefix.Length == 0 ? dir.Name : $"{pathPrefix}/{dir.Name}";
                    result.Add(
                        new TfaiDirectoryEntry { Name = dir.Name, Children = MapEntries(dir.Children, path, mapFile) }
                    );
                    break;
                }
            }
        }

        return result;
    }
}
