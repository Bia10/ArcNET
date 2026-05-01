namespace ArcNET.Editor;

/// <summary>
/// Builds host-neutral text previews for projected editor maps.
/// </summary>
public static class EditorMapPreviewBuilder
{
    /// <summary>
    /// Builds a dense text preview for <paramref name="projection"/> using <paramref name="mode"/>.
    /// </summary>
    public static EditorMapPreview Build(EditorMapProjection projection, EditorMapPreviewMode mode)
    {
        ArgumentNullException.ThrowIfNull(projection);

        var rows = new char[projection.Height][];
        for (var row = 0; row < rows.Length; row++)
            rows[row] = new string('.', projection.Width).ToCharArray();

        foreach (var sector in projection.Sectors)
            rows[sector.LocalY][sector.LocalX] = GetGlyph(sector, mode);

        var textRows = new string[projection.Height];
        for (var row = 0; row < projection.Height; row++)
            textRows[row] = new string(rows[projection.Height - 1 - row]);

        return new EditorMapPreview
        {
            MapName = projection.MapName,
            Mode = mode,
            Width = projection.Width,
            Height = projection.Height,
            Legend = GetLegend(mode),
            Rows = textRows,
        };
    }

    private static string GetLegend(EditorMapPreviewMode mode) =>
        mode switch
        {
            EditorMapPreviewMode.Occupancy => "#=sector .=gap",
            EditorMapPreviewMode.Objects => "4=peak 3=high 2=medium 1=low 0=empty sector .=gap",
            EditorMapPreviewMode.Combined => "S=scripted B=blocked L=lit R=roof #=other sector .=gap",
            EditorMapPreviewMode.Roofs => "R=roof sector #=other sector .=gap",
            EditorMapPreviewMode.Lights => "L=lit sector #=other sector .=gap",
            EditorMapPreviewMode.Blocked => "B=blocked sector #=other sector .=gap",
            EditorMapPreviewMode.Scripts => "S=scripted sector #=other sector .=gap",
            _ => "#=sector .=gap",
        };

    private static char GetGlyph(EditorMapSectorProjection sector, EditorMapPreviewMode mode)
    {
        var previewFlags = sector.PreviewFlags;

        return mode switch
        {
            EditorMapPreviewMode.Occupancy => '#',
            EditorMapPreviewMode.Objects => GetDensityBandGlyph(sector.ObjectDensityBand),
            EditorMapPreviewMode.Combined => GetCombinedGlyph(previewFlags),
            EditorMapPreviewMode.Roofs => HasPreviewFlag(previewFlags, EditorMapSectorPreviewFlags.HasRoofs)
                ? 'R'
                : '#',
            EditorMapPreviewMode.Lights => HasPreviewFlag(previewFlags, EditorMapSectorPreviewFlags.HasLights)
                ? 'L'
                : '#',
            EditorMapPreviewMode.Blocked => HasPreviewFlag(previewFlags, EditorMapSectorPreviewFlags.HasBlockedTiles)
                ? 'B'
                : '#',
            EditorMapPreviewMode.Scripts => HasPreviewFlag(previewFlags, EditorMapSectorPreviewFlags.HasScripts)
                ? 'S'
                : '#',
            _ => '#',
        };
    }

    private static bool HasPreviewFlag(EditorMapSectorPreviewFlags actual, EditorMapSectorPreviewFlags expected) =>
        (actual & expected) == expected;

    private static char GetCombinedGlyph(EditorMapSectorPreviewFlags previewFlags)
    {
        if (HasPreviewFlag(previewFlags, EditorMapSectorPreviewFlags.HasScripts))
            return 'S';

        if (HasPreviewFlag(previewFlags, EditorMapSectorPreviewFlags.HasBlockedTiles))
            return 'B';

        if (HasPreviewFlag(previewFlags, EditorMapSectorPreviewFlags.HasLights))
            return 'L';

        if (HasPreviewFlag(previewFlags, EditorMapSectorPreviewFlags.HasRoofs))
            return 'R';

        return '#';
    }

    private static char GetDensityBandGlyph(EditorMapSectorDensityBand band) =>
        band switch
        {
            EditorMapSectorDensityBand.None => '0',
            EditorMapSectorDensityBand.Low => '1',
            EditorMapSectorDensityBand.Medium => '2',
            EditorMapSectorDensityBand.High => '3',
            EditorMapSectorDensityBand.Peak => '4',
            _ => '0',
        };
}
