using ArcNET.Core.Primitives;
using ArcNET.Formats;

namespace ArcNET.Editor.Tests;

public sealed class EditorMapAmbientLightingBuilderTests
{
    [Test]
    public async Task Build_ResolvesCurrentHourColorsFromLightingSchemeFiles()
    {
        var messages = new Dictionary<string, MesFile>(StringComparer.OrdinalIgnoreCase)
        {
            ["Rules\\Lighting Schemes.mes"] = CreateMes(new MessageEntry(3, "castle_hall")),
            ["Rules\\castle_hall.mes"] = CreateMes(new MessageEntry(18, "10,20,30,40,50,60")),
        };

        var state = EditorMapAmbientLightingBuilder.Build(
            assetPath => ResolveMessage(messages, assetPath),
            currentHour: 18
        );

        await Assert.That(state.CurrentHour).IsEqualTo(18);
        await Assert.That(state.ResolveForSector(3).Outdoor).IsEqualTo(new Color(10, 20, 30));
        await Assert.That(state.ResolveForSector(3).Indoor).IsEqualTo(new Color(40, 50, 60));
    }

    [Test]
    public async Task Build_InterpolatesMissingHoursLikeCe()
    {
        var messages = new Dictionary<string, MesFile>(StringComparer.OrdinalIgnoreCase)
        {
            ["Rules\\Lighting Schemes.mes"] = CreateMes(new MessageEntry(7, "bridge")),
            ["Rules\\bridge.mes"] = CreateMes(
                new MessageEntry(0, "0,0,0,0,0,0"),
                new MessageEntry(2, "20,40,60,10,20,30")
            ),
        };

        var state = EditorMapAmbientLightingBuilder.Build(
            assetPath => ResolveMessage(messages, assetPath),
            currentHour: 1
        );
        var colors = state.ResolveForSector(7);

        await Assert.That(colors.Outdoor).IsEqualTo(new Color(10, 20, 30));
        await Assert.That(colors.Indoor).IsEqualTo(new Color(5, 10, 15));
    }

    [Test]
    public async Task GetHourOfDay_UsesSaveGameTimeModuloTwentyFour()
    {
        var saveInfo = new SaveInfo
        {
            ModuleName = "arcanum",
            LeaderName = "Virgil",
            DisplayName = "slot01",
            MapId = 1,
            GameTimeDays = 2,
            GameTimeMs = (27 * 3_600_000) + 1234,
            LeaderPortraitId = 0,
            LeaderLevel = 1,
            LeaderTileX = 0,
            LeaderTileY = 0,
            StoryState = 0,
        };

        var hour = EditorMapAmbientLightingBuilder.GetHourOfDay(saveInfo);

        await Assert.That(hour).IsEqualTo(3);
    }

    private static MesFile CreateMes(params MessageEntry[] entries) => new() { Entries = entries };

    private static MesFile? ResolveMessage(IReadOnlyDictionary<string, MesFile> messages, string assetPath)
    {
        var normalizedPath = assetPath.Replace('/', '\\');
        return messages.TryGetValue(normalizedPath, out var message) ? message : null;
    }
}
