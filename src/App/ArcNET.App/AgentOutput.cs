using System.Text.Json;
using System.Text.Json.Serialization;
using ArcNET.Formats;

namespace ArcNET.App;

/// <summary>
/// Produces structured JSON output for machine/AI consumers of the CLI.
/// Every dump command that accepts <c>--json</c> routes through this class.
/// <para>
/// Convention: each method returns an envelope with three guaranteed fields:
/// <list type="bullet">
///   <item><c>format</c> — the format name (e.g. <c>"mob"</c>).</item>
///   <item><c>source</c> — path to the file or directory that was parsed.</item>
///   <item><c>data</c> — format-specific structured payload.</item>
/// </list>
/// Parse errors are written to <see cref="Console.Error"/> as
/// <c>{ "error": "...", "source": "..." }</c> and the process exits with code 1.
/// </para>
/// </summary>
internal static class AgentOutput
{
    private static readonly JsonSerializerOptions s_opts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── Emit helpers ─────────────────────────────────────────────────────────

    internal static void Write(string format, string source, object data) =>
        Console.WriteLine(
            JsonSerializer.Serialize(
                new
                {
                    format,
                    source,
                    data,
                },
                s_opts
            )
        );

    internal static void WriteError(string source, Exception ex)
    {
        Console.Error.WriteLine(JsonSerializer.Serialize(new { error = ex.Message, source }, s_opts));
    }

    // ── Format-specific projections ───────────────────────────────────────────

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
                    (p, i) =>
                        new
                        {
                            slot = i,
                            id = art.PaletteIds[i],
                            entryCount = p?.Length ?? 0,
                        }
                )
                .Where(p => p.id != 0 || p.entryCount > 0)
                .ToArray(),
            frames = art
                .Frames.SelectMany(
                    (rot, r) =>
                        rot.Select(
                            (f, fi) =>
                                new
                                {
                                    rotation = r,
                                    frame = fi,
                                    width = f.Header.Width,
                                    height = f.Header.Height,
                                    dataSize = f.Header.DataSize,
                                    compressed = f.Header.DataSize < f.Header.Width * f.Header.Height,
                                    centerX = f.Header.CenterX,
                                    centerY = f.Header.CenterY,
                                    deltaX = f.Header.DeltaX,
                                    deltaY = f.Header.DeltaY,
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
                .Entries.Select(e => new
                {
                    num = e.Num,
                    iq = e.Iq,
                    conditions = e.Conditions,
                    text = e.Text,
                    genderField = string.IsNullOrEmpty(e.GenderField) ? null : e.GenderField,
                    actions = e.Actions,
                    responseVal = e.ResponseVal,
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
                .Entries.Select(e => new
                {
                    e.X,
                    e.Y,
                    e.Walkable,
                })
                .ToArray(),
        };

    internal static object Project(JmpFile jmp) =>
        new
        {
            jumpCount = jmp.Jumps.Count,
            jumps = jmp
                .Jumps.Select(e => new
                {
                    sourceX = e.SourceX,
                    sourceY = e.SourceY,
                    destX = e.DestX,
                    destY = e.DestY,
                    destinationMapId = e.DestinationMapId,
                    flags = e.Flags,
                })
                .ToArray(),
        };

    internal static object Project(MapProperties mp) =>
        new
        {
            artId = mp.ArtId,
            unused = mp.Unused,
            limitX = mp.LimitX,
            limitY = mp.LimitY,
        };

    internal static object Project(MesFile mes) =>
        new
        {
            entryCount = mes.Entries.Count,
            entries = mes
                .Entries.Select(e => new
                {
                    index = e.Index,
                    text = e.Text,
                    soundId = e.SoundId,
                })
                .ToArray(),
        };

    internal static object Project(MobData mob)
    {
        var h = mob.Header;
        return new
        {
            version = h.Version,
            objectType = h.GameObjectType.ToString(),
            isPrototype = h.IsPrototype,
            protoId = h.ProtoId.ToString(),
            objectId = h.IsPrototype ? null : h.ObjectId.ToString(),
            propertyCount = mob.Properties.Count,
            properties = mob
                .Properties.Select(p => new
                {
                    bit = (int)p.Field,
                    field = p.Field.ToString(),
                    rawHex = Convert.ToHexString(p.RawBytes),
                })
                .ToArray(),
        };
    }

    internal static object Project(SaveInfo si) =>
        new
        {
            displayName = si.DisplayName,
            moduleName = si.ModuleName,
            leaderName = si.LeaderName,
            leaderLevel = si.LeaderLevel,
            leaderPortraitId = si.LeaderPortraitId,
            mapId = si.MapId,
            leaderTileX = si.LeaderTileX,
            leaderTileY = si.LeaderTileY,
            gameTimeDays = si.GameTimeDays,
            gameTimeMs = si.GameTimeMs,
            storyState = si.StoryState,
        };

    internal static object Project(SaveIndex idx) => ProjectEntries(idx.Root);

    private static object[] ProjectEntries(IReadOnlyList<TfaiEntry> entries) =>
        entries
            .Select(e =>
                e switch
                {
                    TfaiFileEntry f => (object)
                        new
                        {
                            type = "file",
                            name = f.Name,
                            size = f.Size,
                        },
                    TfaiDirectoryEntry d => new
                    {
                        type = "directory",
                        name = d.Name,
                        children = ProjectEntries(d.Children),
                    },
                    _ => new { type = "unknown", name = e.Name },
                }
            )
            .ToArray();

    internal static object Project(ScrFile scr) =>
        new
        {
            description = scr.Description,
            flags = (uint)scr.Flags,
            flagNames = scr.Flags.ToString(),
            entryCount = scr.Entries.Count,
            activeEntries = scr.Entries.Count(e =>
            {
                if (e.Type != (int)ScriptConditionType.True)
                    return true;
                if (e.Action.Type != (int)ScriptActionType.DoNothing)
                    return true;
                var opVals = e.OpValues;
                for (var i = 0; i < 8; i++)
                    if (opVals[i] != 0)
                        return true;
                return false;
            }),
            entries = scr
                .Entries.Select(
                    (e, i) =>
                        new
                        {
                            slot = i,
                            conditionType = Enum.IsDefined((ScriptConditionType)e.Type)
                                ? ((ScriptConditionType)e.Type).ToString()
                                : e.Type.ToString(),
                            actionType = Enum.IsDefined((ScriptActionType)e.Action.Type)
                                ? ((ScriptActionType)e.Action.Type).ToString()
                                : e.Action.Type.ToString(),
                        }
                )
                .ToArray(),
        };

    internal static object Project(Sector sec) =>
        new
        {
            objectCount = sec.Objects.Count,
            lightCount = sec.Lights.Count,
            tileScriptCount = sec.TileScripts.Count,
            hasRoofs = sec.HasRoofs,
            lightSchemeIdx = sec.LightSchemeIdx,
            townmapInfo = sec.TownmapInfo,
            aptitudeAdjustment = sec.AptitudeAdjustment,
            soundList = new
            {
                musicSchemeIdx = sec.SoundList.MusicSchemeIdx,
                ambientSchemeIdx = sec.SoundList.AmbientSchemeIdx,
            },
            blockedTileCount = sec.BlockMask.Sum(m => int.PopCount((int)m)),
            distinctTileArtIds = sec.Tiles.Distinct().Count(),
            lights = sec
                .Lights.Select(l => new
                {
                    tileX = l.TileX,
                    tileY = l.TileY,
                    offsetX = l.OffsetX,
                    offsetY = l.OffsetY,
                    r = l.R,
                    g = l.G,
                    b = l.B,
                    artId = l.ArtId,
                    flags = l.Flags.ToString(),
                    attached = l.ObjHandle != -1L,
                    objHandle = l.ObjHandle == -1L ? null : (long?)l.ObjHandle,
                })
                .ToArray(),
        };

    internal static object Project(TerrainData ter) =>
        new
        {
            version = ter.Version,
            baseTerrainType = ter.BaseTerrainType.ToString(),
            width = ter.Width,
            height = ter.Height,
            compressed = ter.Compressed,
            tileCount = ter.Tiles.Length,
        };

    internal static object Project(TextDataFile td) =>
        new
        {
            entryCount = td.Entries.Count,
            entries = td.Entries.Select(e => new { key = e.Key, value = e.Value }).ToArray(),
        };

    internal static object ProjectSave(SaveInfo info, SaveIndex index, IReadOnlyDictionary<string, byte[]> payloads) =>
        new
        {
            info = Project(info),
            index = Project(index),
            payloadCount = payloads.Count,
            totalBytes = payloads.Values.Sum(p => (long)p.Length),
            files = payloads
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new
                {
                    path = kvp.Key,
                    size = kvp.Value.Length,
                    ext = Path.GetExtension(kvp.Key).ToLowerInvariant(),
                })
                .ToArray(),
        };
}
