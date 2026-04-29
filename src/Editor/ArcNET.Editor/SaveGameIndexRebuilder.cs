using ArcNET.Formats;

namespace ArcNET.Editor;

internal static class SaveGameIndexRebuilder
{
    public static SaveIndex Rebuild(SaveIndex original, IReadOnlyDictionary<string, byte[]> files) =>
        new() { Root = RebuildEntries(original.Root, string.Empty, files) };

    private static IReadOnlyList<TfaiEntry> RebuildEntries(
        IReadOnlyList<TfaiEntry> entries,
        string pathPrefix,
        IReadOnlyDictionary<string, byte[]> files
    )
    {
        var result = new List<TfaiEntry>(entries.Count);
        foreach (var entry in entries)
        {
            switch (entry)
            {
                case TfaiFileEntry file:
                {
                    var key = pathPrefix.Length == 0 ? file.Name : $"{pathPrefix}/{file.Name}";
                    var newSize = files.TryGetValue(key, out var payload) ? payload.Length : file.Size;
                    result.Add(new TfaiFileEntry { Name = file.Name, Size = newSize });
                    break;
                }

                case TfaiDirectoryEntry dir:
                {
                    var childPrefix = pathPrefix.Length == 0 ? dir.Name : $"{pathPrefix}/{dir.Name}";
                    result.Add(
                        new TfaiDirectoryEntry
                        {
                            Name = dir.Name,
                            Children = RebuildEntries(dir.Children, childPrefix, files),
                        }
                    );
                    break;
                }
            }
        }

        return result;
    }
}
