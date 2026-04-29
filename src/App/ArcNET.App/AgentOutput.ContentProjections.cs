using ArcNET.Formats;

namespace ArcNET.App;

internal static partial class AgentOutput
{
    internal static object Project(ArtFile art) =>
        new
        {
            flags = art.Flags.ToString(),
            frameRate = art.FrameRate,
            frameCount = art.FrameCount,
            actionFrame = art.ActionFrame,
            rotations = art.EffectiveRotationCount,
            palettes = art
                .Palettes.Select(
                    (palette, index) =>
                        new
                        {
                            slot = index,
                            id = art.PaletteIds[index],
                            entryCount = palette?.Length ?? 0,
                        }
                )
                .Where(palette => palette.id != 0 || palette.entryCount > 0)
                .ToArray(),
            frames = art
                .Frames.SelectMany(
                    (rotation, rotationIndex) =>
                        rotation.Select(
                            (frame, frameIndex) =>
                                new
                                {
                                    rotation = rotationIndex,
                                    frame = frameIndex,
                                    width = frame.Header.Width,
                                    height = frame.Header.Height,
                                    dataSize = frame.Header.DataSize,
                                    compressed = frame.Header.DataSize < frame.Header.Width * frame.Header.Height,
                                    centerX = frame.Header.CenterX,
                                    centerY = frame.Header.CenterY,
                                    deltaX = frame.Header.DeltaX,
                                    deltaY = frame.Header.DeltaY,
                                }
                        )
                )
                .ToArray(),
        };

    internal static object Project(DlgFile dlg) =>
        new
        {
            entryCount = dlg.Entries.Count,
            entries = dlg
                .Entries.Select(entry => new
                {
                    num = entry.Num,
                    iq = entry.Iq,
                    conditions = entry.Conditions,
                    text = entry.Text,
                    genderField = string.IsNullOrEmpty(entry.GenderField) ? null : entry.GenderField,
                    actions = entry.Actions,
                    responseVal = entry.ResponseVal,
                })
                .ToArray(),
        };

    internal static object Project(FacadeWalk fac) =>
        new
        {
            header = new
            {
                terrain = fac.Header.Terrain,
                outdoor = fac.Header.Outdoor,
                flippable = fac.Header.Flippable,
                width = fac.Header.Width,
                height = fac.Header.Height,
            },
            entryCount = fac.Entries.Length,
            entries = fac
                .Entries.Select(entry => new
                {
                    entry.X,
                    entry.Y,
                    entry.Walkable,
                })
                .ToArray(),
        };

    internal static object Project(JmpFile jmp) =>
        new
        {
            jumpCount = jmp.Jumps.Count,
            jumps = jmp
                .Jumps.Select(jump => new
                {
                    sourceX = jump.SourceX,
                    sourceY = jump.SourceY,
                    destX = jump.DestX,
                    destY = jump.DestY,
                    destinationMapId = jump.DestinationMapId,
                    flags = jump.Flags,
                })
                .ToArray(),
        };

    internal static object Project(MapProperties mapProperties) =>
        new
        {
            artId = mapProperties.ArtId,
            unused = mapProperties.Unused,
            limitX = mapProperties.LimitX,
            limitY = mapProperties.LimitY,
        };

    internal static object Project(MesFile mes) =>
        new
        {
            entryCount = mes.Entries.Count,
            entries = mes
                .Entries.Select(entry => new
                {
                    index = entry.Index,
                    text = entry.Text,
                    soundId = entry.SoundId,
                })
                .ToArray(),
        };

    internal static object Project(ScrFile scr) =>
        new
        {
            description = scr.Description,
            flags = (uint)scr.Flags,
            flagNames = scr.Flags.ToString(),
            entryCount = scr.Entries.Count,
            activeEntries = scr.Entries.Count(entry =>
            {
                if (entry.Type != (int)ScriptConditionType.True)
                    return true;
                if (entry.Action.Type != (int)ScriptActionType.DoNothing)
                    return true;
                var opValues = entry.OpValues;
                for (var index = 0; index < 8; index++)
                    if (opValues[index] != 0)
                        return true;
                return false;
            }),
            entries = scr
                .Entries.Select(
                    (entry, index) =>
                        new
                        {
                            slot = index,
                            conditionType = Enum.IsDefined((ScriptConditionType)entry.Type)
                                ? ((ScriptConditionType)entry.Type).ToString()
                                : entry.Type.ToString(),
                            actionType = Enum.IsDefined((ScriptActionType)entry.Action.Type)
                                ? ((ScriptActionType)entry.Action.Type).ToString()
                                : entry.Action.Type.ToString(),
                        }
                )
                .ToArray(),
        };

    internal static object Project(TerrainData terrain) =>
        new
        {
            version = terrain.Version,
            baseTerrainType = terrain.BaseTerrainType.ToString(),
            width = terrain.Width,
            height = terrain.Height,
            compressed = terrain.Compressed,
            tileCount = terrain.Tiles.Length,
        };

    internal static object Project(TextDataFile textData) =>
        new
        {
            entryCount = textData.Entries.Count,
            entries = textData.Entries.Select(entry => new { key = entry.Key, value = entry.Value }).ToArray(),
        };
}
