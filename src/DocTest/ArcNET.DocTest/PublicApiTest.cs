using System.Runtime.CompilerServices;
using System.Text;
using ArcNET.Archive;
using ArcNET.BinaryPatch;
using ArcNET.Core;
using ArcNET.Dumpers;
using ArcNET.Formats;
using ArcNET.GameData;
using ArcNET.GameObjects;
using ArcNET.Patch;
using PublicApiGenerator;

namespace ArcNET.DocTest;

[NotInParallel]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class PublicApiTest
{
    private static readonly string s_testSourceFilePath = SourceFile();

    private static readonly string s_publicApiFilePath = Path.GetFullPath(
        Path.Combine(Path.GetDirectoryName(s_testSourceFilePath)!, "..", "..", "..", "docs", "PublicApi.md")
    );

    [Test]
    public void PublicApi_GenerateAndWrite()
    {
        var assemblies = new[]
        {
            ("ArcNET.Core", typeof(SpanReader).Assembly.GeneratePublicApi()),
            ("ArcNET.Archive", typeof(DatArchive).Assembly.GeneratePublicApi()),
            ("ArcNET.Formats", typeof(MessageFormat).Assembly.GeneratePublicApi()),
            ("ArcNET.GameObjects", typeof(GameObject).Assembly.GeneratePublicApi()),
            ("ArcNET.GameData", typeof(GameDataStore).Assembly.GeneratePublicApi()),
            ("ArcNET.Patch", typeof(PatchInstaller).Assembly.GeneratePublicApi()),
            ("ArcNET.BinaryPatch", typeof(BinaryPatcher).Assembly.GeneratePublicApi()),
            ("ArcNET.Dumpers", typeof(SectorDumper).Assembly.GeneratePublicApi()),
        };

        var sb = new StringBuilder();
        sb.AppendLine("# ArcNET Public API");

        foreach (var (name, api) in assemblies)
        {
            sb.AppendLine();
            sb.AppendLine($"## {name}");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.Append(api);
            sb.AppendLine("```");
        }

        File.WriteAllText(s_publicApiFilePath, sb.ToString(), Encoding.UTF8);
    }

    private static string SourceFile([CallerFilePath] string path = "") => path;
}
