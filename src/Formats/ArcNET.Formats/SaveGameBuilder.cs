namespace ArcNET.Formats;

/// <summary>
/// Factory for constructing brand-new <see cref="SaveGame"/> instances without requiring
/// an existing save slot on disk.
/// <para>
/// Typical flow for creating a new save from scratch:
/// <code>
/// var pc = CharacterMdyRecordBuilder.Create(stats, basicSkills, techSkills, spellTech,
///     gold: 200, name: "Hero", portraitIndex: 3);
///
/// var info = new SaveInfo
/// {
///     ModuleName       = "Arcanum",
///     LeaderName       = pc.Name ?? "Hero",
///     DisplayName      = "New Game",
///     MapId            = 1,
///     GameTimeDays     = 0,
///     GameTimeMs       = 0,
///     LeaderPortraitId = pc.PortraitIndex,
///     LeaderLevel      = 1,
///     LeaderTileX      = 1800,
///     LeaderTileY      = 940,
///     StoryState       = 0,
/// };
///
/// var save = SaveGameBuilder.CreateNew(info, "modules/Arcanum/maps/Map01", pc);
/// SaveGameWriter.Save(save, @"C:\path\Slot0001.tfai");
/// </code>
/// </para>
/// </summary>
public static class SaveGameBuilder
{
    /// <summary>
    /// Creates a minimal valid <see cref="SaveGame"/> containing a single map with the
    /// given player-character record as the only <c>mobile.mdy</c> entry.
    /// </summary>
    /// <param name="info">
    /// Save-slot metadata written to the companion <c>.gsi</c> file.
    /// Construct with <see cref="SaveInfo"/> using the appropriate field values.
    /// </param>
    /// <param name="mapPath">
    /// Archive-relative directory path of the starting map inside the TFAF blob,
    /// e.g. <c>modules/Arcanum/maps/Map01</c>.
    /// Must use forward slashes and must start with <c>modules/</c>.
    /// </param>
    /// <param name="pc">
    /// The player-character record to embed as the sole entry in <c>mobile.mdy</c>.
    /// Construct with <see cref="CharacterMdyRecordBuilder.Create"/>.
    /// </param>
    /// <returns>
    /// A <see cref="SaveGame"/> with one map and no static objects, sectors, or diff records.
    /// Pass to <see cref="SaveGameWriter.Save(SaveGame, string)"/> to write to disk.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="mapPath"/> does not start with <c>modules/</c>.
    /// </exception>
    public static SaveGame CreateNew(SaveInfo info, string mapPath, CharacterMdyRecord pc)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(pc);
        ArgumentException.ThrowIfNullOrWhiteSpace(mapPath);

        if (!mapPath.StartsWith("modules/", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                "mapPath must start with 'modules/' to be a valid TFAF archive path.",
                nameof(mapPath)
            );

        var mdy = new MobileMdyFile { Records = [MobileMdyRecord.FromCharacter(pc)] };

        var map = new SaveMapState
        {
            MapPath = mapPath,
            Sectors = [],
            StaticObjects = [],
            DynamicObjects = mdy,
        };

        return new SaveGame { Info = info, Maps = [map] };
    }

    /// <summary>
    /// Creates a minimal valid <see cref="SaveGame"/> with a pre-built map state.
    /// Use this overload when you need full control over sector data, static objects, or
    /// jump points.
    /// </summary>
    /// <param name="info">Save-slot metadata.</param>
    /// <param name="map">
    /// Fully-constructed map state.  Its <see cref="SaveMapState.MapPath"/> must start
    /// with <c>modules/</c>.
    /// </param>
    /// <returns>A <see cref="SaveGame"/> wrapping the supplied map state.</returns>
    public static SaveGame CreateNew(SaveInfo info, SaveMapState map)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(map);

        if (!map.MapPath.StartsWith("modules/", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                "map.MapPath must start with 'modules/' to be a valid TFAF archive path.",
                nameof(map)
            );

        return new SaveGame { Info = info, Maps = [map] };
    }
}
