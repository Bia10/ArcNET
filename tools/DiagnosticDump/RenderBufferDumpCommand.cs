using System.Globalization;
using System.Text;
using ArcNET.Core.Primitives;
using ArcNET.Editor;
using ArcNET.GameObjects;

internal static class RenderBufferDumpCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage();
            return 0;
        }

        RenderBufferDumpOptions options;
        try
        {
            options = Parse(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine();
            PrintUsage();
            return 1;
        }

        using var workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(
            options.GameRoot,
            new EditorWorkspaceLoadOptions { ModuleName = options.ModuleName }
        );

        var mapName = options.MapName ?? workspace.ResolveDefaultMap()?.MapName;
        if (string.IsNullOrWhiteSpace(mapName))
            throw new InvalidOperationException("The workspace did not resolve a default map. Pass --map explicitly.");

        if (workspace.Index.FindMapProjection(mapName) is null)
            throw new InvalidOperationException($"No indexed map projection matched '{mapName}'.");

        var session = workspace.CreateSession();
        var mapViewState = CreateMapViewState(session, mapName);
        var scene = await session.CreateMapWorldEditSceneAsync(
            mapViewState,
            new EditorMapWorldEditSceneRequest
            {
                RenderRequest = EditorMapFloorRenderRequest.CreateWorldEditPreset(options.ViewMode),
            }
        );

        var paintableLookup = BuildPaintableLookup(scene.PaintableScene.Items);
        var paintableItems = (
            options.VisibleOnly
                ? scene.PaintableScene.EnumerateVisibleItems(scene.ViewportLayout)
                : scene.PaintableScene.Items
        )
            .Where(item => Matches(item, options))
            .Take(options.Limit)
            .ToArray();
        var queueItems = scene
            .SceneRender.RenderQueue.Where(item => Matches(item, paintableLookup, options))
            .Take(options.Limit)
            .ToArray();

        var writer = CreateWriter(options.OutputPath);
        try
        {
            await writer.WriteLineAsync("=== RENDER BUFFER DUMP ===");
            await writer.WriteLineAsync($"GameRoot\t{options.GameRoot}");
            await writer.WriteLineAsync($"Module\t{options.ModuleName ?? "(none)"}");
            await writer.WriteLineAsync($"Map\t{mapName}");
            await writer.WriteLineAsync($"ViewMode\t{options.ViewMode}");
            await writer.WriteLineAsync($"VisibleOnly\t{options.VisibleOnly}");
            await writer.WriteLineAsync($"QueueItemCount\t{scene.SceneRender.RenderQueue.Count}");
            await writer.WriteLineAsync($"PaintableItemCount\t{scene.PaintableScene.Items.Count}");
            await writer.WriteLineAsync($"FilteredQueueItemCount\t{queueItems.Length}");
            await writer.WriteLineAsync($"FilteredPaintableItemCount\t{paintableItems.Length}");
            await writer.WriteLineAsync();

            await writer.WriteLineAsync("=== QUEUE ITEMS ===");
            await writer.WriteLineAsync(
                "Index\tKind\tLayer\tDrawOrder\tSortKey\tObjectType\tAuxLayer\tTileOrderSecondary\tTypeSortPriority\tBlendMode\tTint\tOpacity\tArtId\tAssetPath\tSectorAssetPath\tMapTile\tAnchor\tSpriteBounds\tRoofCovered\tQueueDetails"
            );
            for (var index = 0; index < queueItems.Length; index++)
                await writer.WriteLineAsync(FormatQueueLine(index, queueItems[index], paintableLookup));

            await writer.WriteLineAsync();
            await writer.WriteLineAsync("=== PAINTABLE ITEMS ===");
            await writer.WriteLineAsync(
                "Index\tKind\tLayer\tDrawOrder\tSortKey\tTint\tOpacity\tBlendMode\tUseLightMaskTint\tUseGrayscalePaletteOverride\tTintIgnoresLightVisibility\tIsRoofCovered\tTileOverlayKind\tArtId\tAssetPath\tLeftTopSize\tAnchor\tSourceRect\tDestinationRect\tGeometryPointCount"
            );
            for (var index = 0; index < paintableItems.Length; index++)
                await writer.WriteLineAsync(FormatPaintableLine(index, paintableItems[index]));
        }
        finally
        {
            if (!ReferenceEquals(writer, Console.Out))
                await writer.DisposeAsync();
        }

        return 0;
    }

    private static StreamWriter CreateWriter(string? outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            return new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };

        var fullPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory());
        return new StreamWriter(fullPath, append: false, Encoding.ASCII);
    }

    private static EditorProjectMapViewState CreateMapViewState(EditorWorkspaceSession session, string mapName)
    {
        var defaultViewState = session.CreateDefaultMapViewState("render-buffer-dump");
        if (string.Equals(defaultViewState.MapName, mapName, StringComparison.OrdinalIgnoreCase))
            return defaultViewState;

        return new EditorProjectMapViewState
        {
            Id = defaultViewState.Id,
            MapName = mapName,
            ViewId = defaultViewState.ViewId,
            Camera = defaultViewState.Camera,
            Selection = new EditorProjectMapSelectionState(),
            Preview = new EditorProjectMapPreviewState(),
            WorldEdit = new EditorProjectMapWorldEditState(),
        };
    }

    private static Dictionary<SceneItemMatchKey, PaintableLookupValue> BuildPaintableLookup(
        IReadOnlyList<EditorMapPaintableSceneItem> items
    )
    {
        var result = new Dictionary<SceneItemMatchKey, PaintableLookupValue>();
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var key = new SceneItemMatchKey(item.Kind, item.DrawOrder, item.SortKey, GetArtId(item));
            result[key] = new PaintableLookupValue(GetAssetPath(item), item);
        }

        return result;
    }

    private static bool Matches(
        EditorMapRenderQueueItem item,
        IReadOnlyDictionary<SceneItemMatchKey, PaintableLookupValue> paintableLookup,
        RenderBufferDumpOptions options
    )
    {
        if (options.Kind is { } kind && item.Kind != kind)
            return false;

        var layer = GetCommittedRenderLayer(item);
        if (options.Layer is { } layerFilter && layer != layerFilter)
            return false;

        var artId = GetArtId(item);
        if (options.ArtId is { } artIdFilter && artId != artIdFilter)
            return false;

        if (!string.IsNullOrWhiteSpace(options.AssetContains))
        {
            var key = new SceneItemMatchKey(item.Kind, item.DrawOrder, item.SortKey, artId);
            if (
                !paintableLookup.TryGetValue(key, out var lookup)
                || lookup.AssetPath is null
                || lookup.AssetPath.IndexOf(options.AssetContains, StringComparison.OrdinalIgnoreCase) < 0
            )
            {
                return false;
            }
        }

        return true;
    }

    private static bool Matches(EditorMapPaintableSceneItem item, RenderBufferDumpOptions options)
    {
        if (options.Kind is { } kind && item.Kind != kind)
            return false;

        if (options.Layer is { } layer && item.CommittedRenderLayer != layer)
            return false;

        var artId = GetArtId(item);
        if (options.ArtId is { } artIdFilter && artId != artIdFilter)
            return false;

        if (
            !string.IsNullOrWhiteSpace(options.AssetContains)
            && (GetAssetPath(item)?.IndexOf(options.AssetContains, StringComparison.OrdinalIgnoreCase) ?? -1) < 0
        )
        {
            return false;
        }

        return true;
    }

    private static string FormatQueueLine(
        int index,
        EditorMapRenderQueueItem item,
        IReadOnlyDictionary<SceneItemMatchKey, PaintableLookupValue> paintableLookup
    )
    {
        var key = new SceneItemMatchKey(item.Kind, item.DrawOrder, item.SortKey, GetArtId(item));
        paintableLookup.TryGetValue(key, out var lookup);

        return string.Join(
            '\t',
            index.ToString(CultureInfo.InvariantCulture),
            item.Kind.ToString(),
            FormatNullable(item.CommittedRenderLayer),
            item.DrawOrder.ToString(CultureInfo.InvariantCulture),
            FormatDouble(item.SortKey),
            GetObjectType(item),
            GetAuxiliaryLayer(item),
            GetTileOrderSecondary(item),
            GetTypeSortPriority(item),
            GetBlendMode(item),
            FormatNullableHex(GetSuggestedTintColor(item)),
            FormatDouble(GetSuggestedOpacity(item)),
            FormatHex(GetArtId(item)),
            lookup.AssetPath ?? "",
            GetSectorAssetPath(item),
            GetMapTile(item),
            GetAnchor(item),
            GetSpriteBounds(item),
            GetIsRoofCovered(item),
            GetQueueDetails(item)
        );
    }

    private static string FormatPaintableLine(int index, EditorMapPaintableSceneItem item)
    {
        return string.Join(
            '\t',
            index.ToString(CultureInfo.InvariantCulture),
            item.Kind.ToString(),
            FormatNullable(item.CommittedRenderLayer),
            item.DrawOrder.ToString(CultureInfo.InvariantCulture),
            FormatDouble(item.SortKey),
            FormatNullableHex(item.SuggestedTintColor),
            FormatDouble(item.SuggestedOpacity),
            item.BlendMode.ToString(),
            item.UseLightMaskTint.ToString(),
            item.UseGrayscalePaletteOverride.ToString(),
            item.TintIgnoresLightVisibility.ToString(),
            item.IsRoofCovered.ToString(),
            FormatNullable(item.TileOverlayKind),
            FormatHex(GetArtId(item)),
            GetAssetPath(item) ?? "",
            FormatRect(item.Left, item.Top, item.Width, item.Height),
            FormatPoint(item.AnchorX, item.AnchorY),
            FormatSourceRect(item.SpriteSourceRect),
            FormatDestinationRect(item.SpriteDestinationRect),
            (item.GeometryPoints?.Count ?? 0).ToString(CultureInfo.InvariantCulture)
        );
    }

    private static RenderBufferDumpOptions Parse(string[] args)
    {
        var options = new RenderBufferDumpOptions();

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--map":
                    options.MapName = ReadRequiredValue(args, ref index, arg);
                    break;
                case "--module":
                    options.ModuleName = ReadRequiredValue(args, ref index, arg);
                    break;
                case "--kind":
                    options.Kind = Enum.TryParse<EditorMapRenderQueueItemKind>(
                        ReadRequiredValue(args, ref index, arg),
                        ignoreCase: true,
                        out var kind
                    )
                        ? kind
                        : throw new ArgumentOutOfRangeException(nameof(args), "Unknown render kind.");
                    break;
                case "--layer":
                    options.Layer = Enum.TryParse<EditorMapCommittedRenderLayer>(
                        ReadRequiredValue(args, ref index, arg),
                        ignoreCase: true,
                        out var layer
                    )
                        ? layer
                        : throw new ArgumentOutOfRangeException(nameof(args), "Unknown committed render layer.");
                    break;
                case "--asset-contains":
                    options.AssetContains = ReadRequiredValue(args, ref index, arg);
                    break;
                case "--art-id":
                    options.ArtId = ParseUInt32(ReadRequiredValue(args, ref index, arg));
                    break;
                case "--limit":
                    options.Limit =
                        int.TryParse(
                            ReadRequiredValue(args, ref index, arg),
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out var limit
                        )
                        && limit > 0
                            ? limit
                            : throw new ArgumentOutOfRangeException(
                                nameof(args),
                                "--limit must be a positive integer."
                            );
                    break;
                case "--out":
                    options.OutputPath = ReadRequiredValue(args, ref index, arg);
                    break;
                case "--view-mode":
                    options.ViewMode = Enum.TryParse<EditorMapSceneViewMode>(
                        ReadRequiredValue(args, ref index, arg),
                        ignoreCase: true,
                        out var viewMode
                    )
                        ? viewMode
                        : throw new ArgumentOutOfRangeException(nameof(args), "Unknown view mode.");
                    break;
                case "--visible-only":
                    options.VisibleOnly = true;
                    break;
                default:
                    if (arg.StartsWith("--", StringComparison.Ordinal))
                        throw new ArgumentOutOfRangeException(nameof(args), $"Unknown option '{arg}'.");

                    if (options.GameRoot.Length != 0)
                        throw new ArgumentException($"Unexpected extra argument '{arg}'.");

                    options.GameRoot = arg;
                    break;
            }
        }

        if (options.GameRoot.Length == 0)
            throw new ArgumentException("Missing required <game-root-dir> argument.");

        return options;
    }

    private static string ReadRequiredValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
            throw new ArgumentException($"Missing value for {option}.");

        index++;
        return args[index];
    }

    private static uint ParseUInt32(string value) =>
        value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? uint.Parse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : uint.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static bool IsHelp(string arg) => arg is "help" or "--help" or "-h" or "/?";

    private static void PrintUsage()
    {
        Console.WriteLine("DiagnosticDump render-buffer - dump render queue and paintable scene items for one map");
        Console.WriteLine();
        Console.WriteLine(
            "Usage: DiagnosticDump render-buffer <game-root-dir> [--map <mapName>] [--module <moduleName>] [--kind <kind>] [--layer <layer>] [--asset-contains <text>] [--art-id <hex|uint>] [--limit <count>] [--view-mode <Isometric|TopDown>] [--visible-only] [--out <path>]"
        );
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  DiagnosticDump render-buffer C:\\Games\\Arcanum --map Arcanum1-024-fixed");
        Console.WriteLine(
            "  DiagnosticDump render-buffer C:\\Games\\Arcanum --kind ObjectAuxiliary --asset-contains light --limit 64"
        );
        Console.WriteLine();
        Console.WriteLine("Notes:");
        Console.WriteLine(
            "  - The queue section exposes sort and object-band metadata from ArcNET render composition."
        );
        Console.WriteLine(
            "  - The paintable section exposes final rect, tint, and mask metadata consumed by the host renderer."
        );
        Console.WriteLine("  - --asset-contains filters against resolved paintable sprite asset paths.");
    }

    private static uint GetArtId(EditorMapPaintableSceneItem item) =>
        item.Sprite?.ArtId.Value ?? item.SpriteReference?.ArtId.Value ?? 0u;

    private static uint GetArtId(EditorMapRenderQueueItem item) =>
        item.Kind switch
        {
            EditorMapRenderQueueItemKind.FloorTile => item.Tile?.ArtId.Value ?? 0u,
            EditorMapRenderQueueItemKind.Object => item.Object?.CurrentArtId.Value ?? 0u,
            EditorMapRenderQueueItemKind.Roof => item.Roof?.ArtId.Value ?? 0u,
            EditorMapRenderQueueItemKind.Light => item.Light?.ArtId.Value ?? 0u,
            EditorMapRenderQueueItemKind.ObjectAuxiliary => item.ObjectAuxiliary?.ArtId.Value ?? 0u,
            EditorMapRenderQueueItemKind.PlacementPreviewObject => item.PlacementPreviewObject?.CurrentArtId.Value
                ?? 0u,
            _ => 0u,
        };

    private static string? GetAssetPath(EditorMapPaintableSceneItem item) =>
        item.Sprite?.AssetPath ?? item.SpriteReference?.ArtId.ToString();

    private static EditorMapCommittedRenderLayer? GetCommittedRenderLayer(EditorMapRenderQueueItem item) =>
        item.CommittedRenderLayer;

    private static string GetObjectType(EditorMapRenderQueueItem item) =>
        item.Kind switch
        {
            EditorMapRenderQueueItemKind.Object => item.Object?.ObjectType.ToString() ?? string.Empty,
            EditorMapRenderQueueItemKind.ObjectAuxiliary => item.ObjectAuxiliary?.ParentObjectType.ToString()
                ?? string.Empty,
            EditorMapRenderQueueItemKind.PlacementPreviewObject => item.PlacementPreviewObject?.ObjectType.ToString()
                ?? string.Empty,
            _ => string.Empty,
        };

    private static string GetAuxiliaryLayer(EditorMapRenderQueueItem item) =>
        item.ObjectAuxiliary?.Layer.ToString() ?? string.Empty;

    private static string GetTileOrderSecondary(EditorMapRenderQueueItem item) =>
        item.Object?.TileOrderSecondary.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string GetTypeSortPriority(EditorMapRenderQueueItem item) =>
        item.Object?.TypeSortPriority.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string GetBlendMode(EditorMapRenderQueueItem item) =>
        item.Kind switch
        {
            EditorMapRenderQueueItemKind.Object => item.Object?.BlendMode.ToString() ?? string.Empty,
            EditorMapRenderQueueItemKind.ObjectAuxiliary => item.ObjectAuxiliary?.BlendMode.ToString() ?? string.Empty,
            _ => string.Empty,
        };

    private static uint? GetSuggestedTintColor(EditorMapRenderQueueItem item) =>
        item.Kind switch
        {
            EditorMapRenderQueueItemKind.FloorTile => item.Tile?.SuggestedTintColor,
            EditorMapRenderQueueItemKind.Object => item.Object?.SuggestedTintColor,
            EditorMapRenderQueueItemKind.ObjectAuxiliary => item.ObjectAuxiliary?.SuggestedTintColor,
            EditorMapRenderQueueItemKind.Roof => item.Roof?.SuggestedTintColor,
            EditorMapRenderQueueItemKind.Light => item.Light?.SuggestedTintColor,
            EditorMapRenderQueueItemKind.TileOverlay => item.TileOverlay?.SuggestedTintColor,
            EditorMapRenderQueueItemKind.PlacementPreviewObject => item.PlacementPreviewObject?.SuggestedTintColor,
            _ => null,
        };

    private static double GetSuggestedOpacity(EditorMapRenderQueueItem item) =>
        item.Kind switch
        {
            EditorMapRenderQueueItemKind.Object => 1d,
            EditorMapRenderQueueItemKind.ObjectAuxiliary => 1d,
            EditorMapRenderQueueItemKind.Roof => item.Roof?.SuggestedOpacity ?? 1d,
            EditorMapRenderQueueItemKind.Light => item.Light?.SuggestedOpacity ?? 1d,
            EditorMapRenderQueueItemKind.TileOverlay => item.TileOverlay?.SuggestedOpacity ?? 1d,
            EditorMapRenderQueueItemKind.PlacementPreviewObject => item.PlacementPreviewObject?.SuggestedOpacity ?? 1d,
            _ => 1d,
        };

    private static string GetSectorAssetPath(EditorMapRenderQueueItem item) =>
        item.Kind switch
        {
            EditorMapRenderQueueItemKind.FloorTile => item.Tile?.SectorAssetPath ?? string.Empty,
            EditorMapRenderQueueItemKind.Object => item.Object?.SectorAssetPath ?? string.Empty,
            EditorMapRenderQueueItemKind.ObjectAuxiliary => item.ObjectAuxiliary?.SectorAssetPath ?? string.Empty,
            EditorMapRenderQueueItemKind.Roof => item.Roof?.SectorAssetPath ?? string.Empty,
            EditorMapRenderQueueItemKind.TileOverlay => item.TileOverlay?.SectorAssetPath ?? string.Empty,
            EditorMapRenderQueueItemKind.Light => item.Light?.SectorAssetPath ?? string.Empty,
            EditorMapRenderQueueItemKind.PlacementPreviewObject => item.PlacementPreviewObject?.SectorAssetPath
                ?? string.Empty,
            _ => string.Empty,
        };

    private static string GetMapTile(EditorMapRenderQueueItem item) =>
        item.Kind switch
        {
            EditorMapRenderQueueItemKind.FloorTile => FormatTile(item.Tile?.MapTileX, item.Tile?.MapTileY),
            EditorMapRenderQueueItemKind.Object => FormatTile(item.Object?.MapTileX, item.Object?.MapTileY),
            EditorMapRenderQueueItemKind.ObjectAuxiliary => FormatTile(
                item.ObjectAuxiliary?.MapTileX,
                item.ObjectAuxiliary?.MapTileY
            ),
            EditorMapRenderQueueItemKind.Roof => FormatTile(item.Roof?.MapTileX, item.Roof?.MapTileY),
            EditorMapRenderQueueItemKind.TileOverlay => FormatTile(
                item.TileOverlay?.MapTileX,
                item.TileOverlay?.MapTileY
            ),
            EditorMapRenderQueueItemKind.Light => FormatTile(item.Light?.MapTileX, item.Light?.MapTileY),
            EditorMapRenderQueueItemKind.PlacementPreviewObject => FormatTile(
                item.PlacementPreviewObject?.MapTileX,
                item.PlacementPreviewObject?.MapTileY
            ),
            _ => string.Empty,
        };

    private static string GetAnchor(EditorMapRenderQueueItem item) =>
        item.Kind switch
        {
            EditorMapRenderQueueItemKind.FloorTile => FormatPoint(item.Tile?.CenterX, item.Tile?.CenterY),
            EditorMapRenderQueueItemKind.Object => FormatPoint(item.Object?.AnchorX, item.Object?.AnchorY),
            EditorMapRenderQueueItemKind.ObjectAuxiliary => FormatPoint(
                item.ObjectAuxiliary?.AnchorX,
                item.ObjectAuxiliary?.AnchorY
            ),
            EditorMapRenderQueueItemKind.Roof => FormatPoint(item.Roof?.AnchorX, item.Roof?.AnchorY),
            EditorMapRenderQueueItemKind.TileOverlay => FormatPoint(
                item.TileOverlay?.CenterX,
                item.TileOverlay?.CenterY
            ),
            EditorMapRenderQueueItemKind.Light => FormatPoint(item.Light?.AnchorX, item.Light?.AnchorY),
            EditorMapRenderQueueItemKind.PlacementPreviewObject => FormatPoint(
                item.PlacementPreviewObject?.AnchorX,
                item.PlacementPreviewObject?.AnchorY
            ),
            _ => string.Empty,
        };

    private static string GetSpriteBounds(EditorMapRenderQueueItem item) =>
        item.Kind switch
        {
            EditorMapRenderQueueItemKind.Object => FormatSpriteBounds(item.Object?.SpriteBounds),
            EditorMapRenderQueueItemKind.PlacementPreviewObject => FormatSpriteBounds(
                item.PlacementPreviewObject?.SpriteBounds
            ),
            _ => string.Empty,
        };

    private static string GetIsRoofCovered(EditorMapRenderQueueItem item) =>
        item.Kind switch
        {
            EditorMapRenderQueueItemKind.FloorTile => item.Tile?.IsRoofCovered.ToString() ?? string.Empty,
            EditorMapRenderQueueItemKind.Object => item.Object?.IsRoofCovered.ToString() ?? string.Empty,
            EditorMapRenderQueueItemKind.ObjectAuxiliary => item.ObjectAuxiliary?.IsRoofCovered.ToString()
                ?? string.Empty,
            _ => string.Empty,
        };

    private static string GetQueueDetails(EditorMapRenderQueueItem item) =>
        item.Kind switch
        {
            EditorMapRenderQueueItemKind.FloorTile => string.Join(
                ';',
                $"blocked={item.Tile?.IsBlocked}",
                $"light={item.Tile?.HasLight}",
                $"script={item.Tile?.HasScript}"
            ),
            EditorMapRenderQueueItemKind.Object => string.Join(
                ';',
                $"proto={item.Object?.ProtoId}",
                $"objectId={item.Object?.ObjectId}",
                $"gridSnapped={item.Object?.IsTileGridSnapped}",
                $"rotation={item.Object?.RotationIndex}",
                $"scale={item.Object?.BlitScale}",
                $"shrunk={item.Object?.IsShrunk}"
            ),
            EditorMapRenderQueueItemKind.ObjectAuxiliary => string.Join(
                ';',
                $"parent={item.ObjectAuxiliary?.ParentObjectId}",
                $"rotation={item.ObjectAuxiliary?.RotationIndex}",
                $"scale={item.ObjectAuxiliary?.ScalePercent}",
                $"shrunk={item.ObjectAuxiliary?.IsShrunk}"
            ),
            EditorMapRenderQueueItemKind.Roof => string.Join(
                ';',
                $"faded={item.Roof?.IsFaded}",
                $"alphaLerp={item.Roof?.AlphaLerp}"
            ),
            EditorMapRenderQueueItemKind.Light => $"flags={item.Light?.Flags}",
            EditorMapRenderQueueItemKind.TileOverlay => $"overlayKind={item.TileOverlay?.Kind}",
            EditorMapRenderQueueItemKind.PlacementPreviewObject => string.Join(
                ';',
                $"proto={item.PlacementPreviewObject?.ProtoId}",
                $"state={item.PlacementPreviewObject?.State}",
                $"gridSnapped={item.PlacementPreviewObject?.IsTileGridSnapped}",
                $"rotation={item.PlacementPreviewObject?.RotationIndex}",
                $"scale={item.PlacementPreviewObject?.BlitScale}",
                $"shrunk={item.PlacementPreviewObject?.IsShrunk}"
            ),
            _ => string.Empty,
        };

    private static string FormatSourceRect(EditorMapPaintableSceneSpriteSourceRect? rect) =>
        rect is null ? string.Empty : FormatRect(rect.Value.X, rect.Value.Y, rect.Value.Width, rect.Value.Height);

    private static string FormatDestinationRect(EditorMapPaintableSceneSpriteDestinationRect? rect) =>
        rect is null ? string.Empty : FormatRect(rect.Value.X, rect.Value.Y, rect.Value.Width, rect.Value.Height);

    private static string FormatSpriteBounds(EditorMapObjectSpriteBounds? bounds) =>
        bounds is null
            ? string.Empty
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{bounds.MaxFrameWidth},{bounds.MaxFrameHeight},{bounds.MaxFrameCenterX},{bounds.MaxFrameCenterY}"
            );

    private static string FormatRect(double left, double top, double width, double height) =>
        string.Create(CultureInfo.InvariantCulture, $"{left:0.###},{top:0.###},{width:0.###},{height:0.###}");

    private static string FormatPoint(double? x, double? y) =>
        x is null || y is null ? string.Empty : string.Create(CultureInfo.InvariantCulture, $"{x:0.###},{y:0.###}");

    private static string FormatTile(int? x, int? y) =>
        x is null || y is null ? string.Empty : string.Create(CultureInfo.InvariantCulture, $"{x},{y}");

    private static string FormatHex(uint value) => value == 0u ? string.Empty : $"0x{value:X8}";

    private static string FormatNullableHex(uint? value) => value is null ? string.Empty : $"0x{value.Value:X8}";

    private static string FormatDouble(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string FormatNullable<T>(T? value)
        where T : struct => value?.ToString() ?? string.Empty;

    private readonly record struct SceneItemMatchKey(
        EditorMapRenderQueueItemKind Kind,
        int DrawOrder,
        double SortKey,
        uint ArtId
    );

    private readonly record struct PaintableLookupValue(string? AssetPath, EditorMapPaintableSceneItem Item);

    private sealed class RenderBufferDumpOptions
    {
        public string GameRoot { get; set; } = string.Empty;
        public string? MapName { get; set; }
        public string? ModuleName { get; set; }
        public EditorMapRenderQueueItemKind? Kind { get; set; }
        public EditorMapCommittedRenderLayer? Layer { get; set; }
        public string? AssetContains { get; set; }
        public uint? ArtId { get; set; }
        public int Limit { get; set; } = 256;
        public string? OutputPath { get; set; }
        public EditorMapSceneViewMode ViewMode { get; set; } = EditorMapSceneViewMode.Isometric;
        public bool VisibleOnly { get; set; }
    }
}
