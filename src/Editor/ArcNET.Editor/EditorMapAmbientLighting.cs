using System.Globalization;
using ArcNET.Core.Primitives;
using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Ambient indoor/outdoor colors resolved for one CE light scheme hour.
/// </summary>
public readonly record struct EditorMapAmbientLightColors(Color Indoor, Color Outdoor);

/// <summary>
/// Current ambient-lighting context used while projecting one map render.
/// </summary>
public sealed class EditorMapAmbientLightingState
{
    public int CurrentHour { get; init; } = EditorMapAmbientLightingBuilder.DefaultHour;

    public int MapDefaultLightSchemeIndex { get; init; } = EditorMapAmbientLightingBuilder.DefaultMapLightSchemeIndex;

    public Color FallbackIndoorColor { get; init; } = new(128, 128, 128);

    public Color FallbackOutdoorColor { get; init; } = new(255, 255, 255);

    public IReadOnlyDictionary<int, EditorMapAmbientLightColors> SchemeColorsByIndex { get; init; } =
        new Dictionary<int, EditorMapAmbientLightColors>();

    public EditorMapAmbientLightColors ResolveForSector(int lightSchemeIndex)
    {
        var resolvedSchemeIndex = lightSchemeIndex == 0 ? MapDefaultLightSchemeIndex : lightSchemeIndex;
        return resolvedSchemeIndex > 0 && SchemeColorsByIndex.TryGetValue(resolvedSchemeIndex, out var colors)
            ? colors
            : new EditorMapAmbientLightColors(FallbackIndoorColor, FallbackOutdoorColor);
    }
}

internal static class EditorMapAmbientLightingBuilder
{
    internal const int DefaultHour = 12;
    internal const int DefaultMapLightSchemeIndex = 1;
    private const int HoursPerDay = 24;
    private const int MillisecondsPerHour = 3_600_000;
    private const int HoursPerGameDay = 24;

    public static EditorMapAmbientLightingState Build(
        Func<string, MesFile?> messageResolver,
        int currentHour,
        int mapDefaultLightSchemeIndex = DefaultMapLightSchemeIndex
    )
    {
        ArgumentNullException.ThrowIfNull(messageResolver);

        var normalizedHour = NormalizeHour(currentHour);
        var schemeColorsByIndex = new Dictionary<int, EditorMapAmbientLightColors>();
        var lightingSchemes = messageResolver("Rules/Lighting Schemes.mes");
        if (lightingSchemes is not null)
        {
            for (var entryIndex = 0; entryIndex < lightingSchemes.Entries.Count; entryIndex++)
            {
                var entry = lightingSchemes.Entries[entryIndex];
                if (!TryResolveLightSchemeAssetPath(entry.Text, out var schemeAssetPath))
                    continue;

                var schemeFile = messageResolver(schemeAssetPath);
                if (schemeFile is null || !TryResolveHourColors(schemeFile, normalizedHour, out var hourColors))
                    continue;

                schemeColorsByIndex[entry.Index] = hourColors;
            }
        }

        return new EditorMapAmbientLightingState
        {
            CurrentHour = normalizedHour,
            MapDefaultLightSchemeIndex =
                mapDefaultLightSchemeIndex > 0 ? mapDefaultLightSchemeIndex : DefaultMapLightSchemeIndex,
            SchemeColorsByIndex = schemeColorsByIndex,
        };
    }

    public static int GetHourOfDay(SaveInfo? saveInfo, int fallbackHour = DefaultHour)
    {
        if (saveInfo is null)
            return NormalizeHour(fallbackHour);

        var totalMilliseconds = ((long)saveInfo.GameTimeDays * 86_400_000L) + saveInfo.GameTimeMs;
        var totalHours = totalMilliseconds / MillisecondsPerHour;
        return NormalizeHour((int)(totalHours % HoursPerGameDay));
    }

    internal static bool AreEquivalent(EditorMapAmbientLightingState? left, EditorMapAmbientLightingState? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        if (
            left.CurrentHour != right.CurrentHour
            || left.MapDefaultLightSchemeIndex != right.MapDefaultLightSchemeIndex
            || left.FallbackIndoorColor != right.FallbackIndoorColor
            || left.FallbackOutdoorColor != right.FallbackOutdoorColor
            || left.SchemeColorsByIndex.Count != right.SchemeColorsByIndex.Count
        )
        {
            return false;
        }

        foreach (var (schemeIndex, colors) in left.SchemeColorsByIndex)
        {
            if (!right.SchemeColorsByIndex.TryGetValue(schemeIndex, out var otherColors) || colors != otherColors)
                return false;
        }

        return true;
    }

    private static bool TryResolveHourColors(MesFile schemeFile, int hour, out EditorMapAmbientLightColors colors)
    {
        ArgumentNullException.ThrowIfNull(schemeFile);

        colors = default;
        var entriesByIndex = schemeFile.Entries.ToDictionary(static entry => entry.Index);
        var resolvedColorsByHour = new EditorMapAmbientLightColors[HoursPerDay];
        var interpolate = false;
        var prevIndex = 0;
        var foundAny = false;
        var outdoor = new RgbColor();
        var indoor = new RgbColor();
        var prevOutdoor = new RgbColor();
        var prevIndoor = new RgbColor();

        for (var index = 0; index < HoursPerDay; index++)
        {
            if (
                entriesByIndex.TryGetValue(index, out var entry)
                && !string.IsNullOrWhiteSpace(entry.Text)
                && TryParseSchemeEntry(entry.Text, out outdoor, out indoor)
            )
            {
                foundAny = true;
                resolvedColorsByHour[index] = CreateAmbientColors(outdoor, indoor);

                if (interpolate)
                {
                    var outdoorRedStep = (outdoor.Red - prevOutdoor.Red) / (index - prevIndex);
                    var outdoorGreenStep = (outdoor.Green - prevOutdoor.Green) / (index - prevIndex);
                    var outdoorBlueStep = (outdoor.Blue - prevOutdoor.Blue) / (index - prevIndex);
                    var indoorRedStep = (indoor.Red - prevIndoor.Red) / (index - prevIndex);
                    var indoorGreenStep = (indoor.Green - prevIndoor.Green) / (index - prevIndex);
                    var indoorBlueStep = (indoor.Blue - prevIndoor.Blue) / (index - prevIndex);
                    var interpolatedOutdoor = prevOutdoor;
                    var interpolatedIndoor = prevIndoor;

                    for (var previous = prevIndex; previous < index; previous++)
                    {
                        resolvedColorsByHour[previous] = CreateAmbientColors(interpolatedOutdoor, interpolatedIndoor);
                        interpolatedOutdoor = new RgbColor(
                            interpolatedOutdoor.Red + outdoorRedStep,
                            interpolatedOutdoor.Green + outdoorGreenStep,
                            interpolatedOutdoor.Blue + outdoorBlueStep
                        );
                        interpolatedIndoor = new RgbColor(
                            interpolatedIndoor.Red + indoorRedStep,
                            interpolatedIndoor.Green + indoorGreenStep,
                            interpolatedIndoor.Blue + indoorBlueStep
                        );
                    }

                    interpolate = false;
                }

                prevIndex = index;
                continue;
            }

            prevOutdoor = outdoor;
            prevIndoor = indoor;
            interpolate = true;
        }

        if (!foundAny)
            return false;

        colors = resolvedColorsByHour[NormalizeHour(hour)];
        return true;
    }

    private static bool TryResolveLightSchemeAssetPath(string? entryText, out string assetPath)
    {
        assetPath = string.Empty;
        if (string.IsNullOrWhiteSpace(entryText))
            return false;

        var trimmed = entryText.Trim().Replace('/', '\\');
        assetPath = trimmed.StartsWith("Rules\\", StringComparison.OrdinalIgnoreCase) ? trimmed : $"Rules\\{trimmed}";
        if (!assetPath.EndsWith(".mes", StringComparison.OrdinalIgnoreCase))
            assetPath += ".mes";
        return true;
    }

    private static bool TryParseSchemeEntry(string text, out RgbColor outdoor, out RgbColor indoor)
    {
        outdoor = default;
        indoor = default;

        var tokens = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 6)
            return false;

        if (
            !TryParseChannel(tokens[0], out var outdoorRed)
            || !TryParseChannel(tokens[1], out var outdoorGreen)
            || !TryParseChannel(tokens[2], out var outdoorBlue)
            || !TryParseChannel(tokens[3], out var indoorRed)
            || !TryParseChannel(tokens[4], out var indoorGreen)
            || !TryParseChannel(tokens[5], out var indoorBlue)
        )
        {
            return false;
        }

        outdoor = new RgbColor(outdoorRed, outdoorGreen, outdoorBlue);
        indoor = new RgbColor(indoorRed, indoorGreen, indoorBlue);
        return true;
    }

    private static bool TryParseChannel(string token, out int value) =>
        int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    private static int NormalizeHour(int hour)
    {
        var normalizedHour = hour % HoursPerDay;
        return normalizedHour < 0 ? normalizedHour + HoursPerDay : normalizedHour;
    }

    private static EditorMapAmbientLightColors CreateAmbientColors(RgbColor outdoor, RgbColor indoor) =>
        new(
            new Color(ToByte(indoor.Red), ToByte(indoor.Green), ToByte(indoor.Blue)),
            new Color(ToByte(outdoor.Red), ToByte(outdoor.Green), ToByte(outdoor.Blue))
        );

    private static byte ToByte(int value) => (byte)Math.Clamp(value, byte.MinValue, byte.MaxValue);

    private readonly record struct RgbColor(int Red, int Green, int Blue);
}
