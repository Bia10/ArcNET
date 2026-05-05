using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameData;

namespace ArcNET.Editor.Tests;

public sealed class EditorArtResolverTests
{
    [Test]
    public async Task CreateArtResolver_ArcanumMessageTables_ResolvesBindingsLazily()
    {
        var contentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(contentDir, "art", "scenery"));

        try
        {
            var sceneryMes = new MesFile { Entries = [new MessageEntry(1, "barbarian")] };
            var artFile = MakeArtFile();
            MessageFormat.WriteToFile(in sceneryMes, Path.Combine(contentDir, "art", "scenery", "scenery.mes"));
            ArtFormat.WriteToFile(in artFile, Path.Combine(contentDir, "art", "scenery", "barbarian.art"));

            var workspace = await EditorWorkspaceLoader.LoadAsync(contentDir);
            var resolver = workspace.CreateArtResolver(EditorArtResolverBindingStrategy.ArcanumMessageTables);

            await Assert.That(resolver.BindingCount).IsEqualTo(0);
            await Assert.That(resolver.FindAssetPath(new ArtId(0x40000100u))).IsEqualTo("art/scenery/barbarian.art");
            await Assert.That(resolver.BindingCount).IsEqualTo(1);
        }
        finally
        {
            if (Directory.Exists(contentDir))
                Directory.Delete(contentDir, recursive: true);
        }
    }

    private static ArtFile MakeArtFile(byte paletteIndex = 1, uint frameRate = 8) =>
        new()
        {
            Flags = ArtFlags.Static,
            FrameRate = frameRate,
            ActionFrame = 0,
            FrameCount = 1,
            DataSizes = new uint[8],
            PaletteData1 = new uint[8],
            PaletteData2 = new uint[8],
            PaletteIds = [1, 0, 0, 0],
            Palettes = [CreatePalette(), null, null, null],
            Frames =
            [
                [new ArtFrame { Header = new ArtFrameHeader(1u, 1u, 1u, 0, 0, 0, 0), Pixels = [paletteIndex] }],
            ],
        };

    private static ArtPaletteEntry[] CreatePalette()
    {
        var palette = new ArtPaletteEntry[256];
        palette[1] = new ArtPaletteEntry(1, 2, 3);
        return palette;
    }
}
