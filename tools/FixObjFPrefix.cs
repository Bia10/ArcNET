#! "nuget: Spectre.Console, 0.55.0"

using System.Collections.Concurrent;
using Spectre.Console;

var repoRoot = args.Length > 0 ? args[0] : Environment.CurrentDirectory;
AnsiConsole.MarkupLine($"[bold]Repo root:[/] {repoRoot}");

// The ONE exception: ObjectFlags maps to ObjectFlags not Flags
var specialMappings = new Dictionary<string, string>(StringComparer.Ordinal)
{
    ["ObjectField.ObjectFlags"] = "ObjectField.ObjectFlags",
};

var csFiles = Directory.GetFiles(repoRoot, "*.cs", SearchOption.AllDirectories)
    .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\artifacts\\"))
    .ToArray();

AnsiConsole.MarkupLine($"Found [green]{csFiles.Length}[/] .cs files to scan.");

var filesToFix = new ConcurrentBag<string>();
var totalReplacements = 0;

Parallel.ForEach(csFiles, file =>
{
    var content = File.ReadAllText(file);
    var original = content;
    var changed = false;

    // Special case first: ObjectFlags → ObjectFlags
    if (content.Contains("ObjectField.ObjectFlags"))
    {
        content = content.Replace("ObjectField.ObjectFlags", "ObjectField.ObjectFlags");
        changed = true;
    }

    // General case: find all ObjectField.ObjF* references
    var matches = System.Text.RegularExpressions.Regex.Matches(content, @"ObjectField\.ObjF(\w+)");
    foreach (System.Text.RegularExpressions.Match match in matches)
    {
        var oldRef = match.Value;
        // Skip if already handled by special mapping
        if (specialMappings.ContainsKey(oldRef))
            continue;
        
        var suffix = match.Groups[1].Value;
        var newRef = "ObjectField." + suffix;
        
        // Only replace if the new name is different
        if (oldRef != newRef)
        {
            content = content.Replace(oldRef, newRef);
            changed = true;
        }
    }

    if (changed)
    {
        File.WriteAllText(file, content);
        filesToFix.Add(file);
        Interlocked.Increment(ref totalReplacements);
    }
});

AnsiConsole.MarkupLine($"[bold green]Done![/] Fixed [yellow]{filesToFix.Count}[/] files with ~{totalReplacements} replacement groups.");
foreach (var f in filesToFix.OrderBy(x => x))
    AnsiConsole.MarkupLine($"  [grey]{Path.GetRelativePath(repoRoot, f)}[/]");
