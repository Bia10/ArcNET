namespace ArcNET.Patch;

/// <summary>Display resolution settings for the HighRes patch config.ini.</summary>
public sealed class HighResConfig
{
    // Basic
    /// <summary>Screen width in pixels.</summary>
    public int Width { get; init; } = 800;

    /// <summary>Screen height in pixels.</summary>
    public int Height { get; init; } = 600;

    /// <summary>Bit depth (typically 16 or 32).</summary>
    public int BitDepth { get; init; } = 16;

    /// <summary>Dialog font size index (0=12pt, 1=14pt, 2=18pt).</summary>
    public int DialogFont { get; init; }

    /// <summary>Logbook font size index (0=12pt, 1=14pt).</summary>
    public int LogbookFont { get; init; }

    /// <summary>Main menu position (0=top, 1=center, 2=bottom).</summary>
    public int MenuPosition { get; init; } = 1;

    /// <summary>Main menu art style (0=black, 1=fade, 2=wood).</summary>
    public int MainMenuArt { get; init; } = 1;

    /// <summary>Whether to add borders to most UI graphics.</summary>
    public int Borders { get; init; } = 1;

    /// <summary>Language index (0=English, 1=German, 2=French, 3=Russian).</summary>
    public int Language { get; init; }

    // Graphics
    /// <summary>Windowed mode (0=fullscreen, 1=windowed).</summary>
    public int Windowed { get; init; }

    /// <summary>Renderer (0=software, 1=hardware).</summary>
    public int Renderer { get; init; }

    /// <summary>Double-buffer mode.</summary>
    public int DoubleBuffer { get; init; }

    /// <summary>DDrawCompat wrapper.</summary>
    public int DDrawWrapper { get; init; }

    /// <summary>DxWrapper DDrawCompat.</summary>
    public int DxWrapper { get; init; }

    /// <summary>Show FPS counter.</summary>
    public int ShowFPS { get; init; } = 1;

    // Advanced
    /// <summary>Scroll frame rate (original 35, max 255).</summary>
    public int ScrollFPS { get; init; } = 60;

    /// <summary>Scroll distance (original 10, 0=infinite).</summary>
    public int ScrollDist { get; init; } = 30;

    /// <summary>Tile preload limit.</summary>
    public int PreloadLimit { get; init; } = 60;

    /// <summary>Broadcast limit.</summary>
    public int BroadcastLimit { get; init; } = 20;

    /// <summary>Skip logos (0=skip Sierra/Troika logos).</summary>
    public int Logos { get; init; }

    /// <summary>Skip intro clip.</summary>
    public int Intro { get; init; }

    /// <summary>Parses a config.ini file into a <see cref="HighResConfig"/>.</summary>
    public static HighResConfig ParseFile(string iniPath)
    {
        if (!File.Exists(iniPath))
            throw new FileNotFoundException("Unable to find config file", iniPath);

        var kvps = ParseIni(iniPath);
        return FromDictionary(kvps);
    }

    /// <summary>Writes the configuration back to a config.ini file.</summary>
    public void WriteFile(string iniPath)
    {
        using var writer = new StreamWriter(iniPath, append: false);
        writer.WriteLine("//Arcanum High Resolution Patch Settings");
        writer.WriteLine();
        writer.WriteLine("//Basic:");
        writer.WriteLine($"Width = {Width}");
        writer.WriteLine($"Height = {Height}");
        writer.WriteLine($"BitDepth = {BitDepth}");
        writer.WriteLine($"DialogFont = {DialogFont}");
        writer.WriteLine($"LogbookFont = {LogbookFont}");
        writer.WriteLine($"MenuPosition = {MenuPosition}");
        writer.WriteLine($"MainMenuArt = {MainMenuArt}");
        writer.WriteLine($"Borders = {Borders}");
        writer.WriteLine($"Language = {Language}");
        writer.WriteLine();
        writer.WriteLine("//Graphics:");
        writer.WriteLine($"Windowed = {Windowed}");
        writer.WriteLine($"Renderer = {Renderer}");
        writer.WriteLine($"DoubleBuffer = {DoubleBuffer}");
        writer.WriteLine($"DDrawWrapper = {DDrawWrapper}");
        writer.WriteLine($"DxWrapper = {DxWrapper}");
        writer.WriteLine($"ShowFPS = {ShowFPS}");
        writer.WriteLine();
        writer.WriteLine("//Advanced:");
        writer.WriteLine($"ScrollFPS = {ScrollFPS}");
        writer.WriteLine($"ScrollDist = {ScrollDist}");
        writer.WriteLine($"PreloadLimit = {PreloadLimit}");
        writer.WriteLine($"BroadcastLimit = {BroadcastLimit}");
        writer.WriteLine($"Logos = {Logos}");
        writer.WriteLine($"Intro = {Intro}");
    }

    private static IReadOnlyDictionary<string, string> ParseIni(string iniPath)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in File.ReadAllLines(iniPath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("//", StringComparison.Ordinal))
                continue;

            var eqIdx = line.IndexOf('=', StringComparison.Ordinal);
            if (eqIdx < 0)
                continue;

            var key = line[..eqIdx].Trim();
            var value = line[(eqIdx + 1)..].Trim();

            // Strip inline comment
            var commentIdx = value.IndexOf("//", StringComparison.Ordinal);
            if (commentIdx >= 0)
                value = value[..commentIdx].Trim();

            result[key] = value;
        }

        return result;
    }

    private static HighResConfig FromDictionary(IReadOnlyDictionary<string, string> kvps)
    {
        static int Get(IReadOnlyDictionary<string, string> d, string key, int @default = 0) =>
            d.TryGetValue(key, out var v) && int.TryParse(v, out var n) ? n : @default;

        return new HighResConfig
        {
            Width = Get(kvps, "Width", 800),
            Height = Get(kvps, "Height", 600),
            BitDepth = Get(kvps, "BitDepth", 16),
            DialogFont = Get(kvps, "DialogFont"),
            LogbookFont = Get(kvps, "LogbookFont"),
            MenuPosition = Get(kvps, "MenuPosition", 1),
            MainMenuArt = Get(kvps, "MainMenuArt", 1),
            Borders = Get(kvps, "Borders", 1),
            Language = Get(kvps, "Language"),
            Windowed = Get(kvps, "Windowed"),
            Renderer = Get(kvps, "Renderer"),
            DoubleBuffer = Get(kvps, "DoubleBuffer"),
            DDrawWrapper = Get(kvps, "DDrawWrapper"),
            DxWrapper = Get(kvps, "DxWrapper"),
            ShowFPS = Get(kvps, "ShowFPS", 1),
            ScrollFPS = Get(kvps, "ScrollFPS", 60),
            ScrollDist = Get(kvps, "ScrollDist", 30),
            PreloadLimit = Get(kvps, "PreloadLimit", 60),
            BroadcastLimit = Get(kvps, "BroadcastLimit", 20),
            Logos = Get(kvps, "Logos"),
            Intro = Get(kvps, "Intro"),
        };
    }
}
