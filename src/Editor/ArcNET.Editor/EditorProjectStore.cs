using System.Text.Json;

namespace ArcNET.Editor;

/// <summary>
/// Persists and reloads <see cref="EditorProject"/> metadata as JSON.
/// </summary>
public static class EditorProjectStore
{
    /// <summary>
    /// Serializes <paramref name="project"/> to JSON.
    /// </summary>
    public static string Serialize(EditorProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return JsonSerializer.Serialize(project, EditorProjectJsonContext.Default.EditorProject);
    }

    /// <summary>
    /// Deserializes an <see cref="EditorProject"/> from JSON.
    /// </summary>
    public static EditorProject Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize(json, EditorProjectJsonContext.Default.EditorProject)
            ?? throw new InvalidDataException("Editor project JSON did not produce a project model.");
    }

    /// <summary>
    /// Loads a persisted editor project from <paramref name="path"/>.
    /// </summary>
    public static EditorProject Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Deserialize(File.ReadAllText(path));
    }

    /// <summary>
    /// Loads a persisted editor project from <paramref name="path"/> asynchronously.
    /// </summary>
    public static async Task<EditorProject> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Deserialize(await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Saves <paramref name="project"/> to <paramref name="path"/>.
    /// </summary>
    public static void Save(string path, EditorProject project)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(project);

        EnsureDirectoryExists(path);
        File.WriteAllText(path, Serialize(project));
    }

    /// <summary>
    /// Saves <paramref name="project"/> to <paramref name="path"/> asynchronously.
    /// </summary>
    public static Task SaveAsync(string path, EditorProject project, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(project);

        EnsureDirectoryExists(path);
        return File.WriteAllTextAsync(path, Serialize(project), cancellationToken);
    }

    private static void EnsureDirectoryExists(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }
}
