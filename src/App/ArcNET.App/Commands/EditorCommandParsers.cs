using System.Globalization;
using ArcNET.Editor;
using ArcNET.Formats;

namespace ArcNET.App;

internal static class EditorCommandParsers
{
    internal const string OutlineModesHelpText = "occupancy, objects, combined, roofs, lights, blocked, scripts";

    public static bool TryParseFormatFilter(string? text, out FileFormat? format)
    {
        format = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;

        var normalized = text.Trim().TrimStart('.');
        if (Enum.TryParse<FileFormat>(normalized, ignoreCase: true, out var parsedFormat))
        {
            format = parsedFormat;
            return true;
        }

        format = normalized.ToLowerInvariant() switch
        {
            "mes" => FileFormat.Message,
            "sec" => FileFormat.Sector,
            "pro" => FileFormat.Proto,
            "mob" => FileFormat.Mob,
            "scr" => FileFormat.Script,
            "dlg" => FileFormat.Dialog,
            _ => null,
        };

        return format.HasValue;
    }

    public static bool TryParseValidationSeverity(string? text, out EditorWorkspaceValidationSeverity? severity)
    {
        severity = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;

        if (Enum.TryParse<EditorWorkspaceValidationSeverity>(text.Trim(), ignoreCase: true, out var parsedSeverity))
        {
            severity = parsedSeverity;
            return true;
        }

        return false;
    }

    public static bool TryParseOutlineMode(string text, out EditorMapPreviewMode mode, out string normalizedMode)
    {
        switch (text.Trim().ToLowerInvariant())
        {
            case "occupancy":
            case "":
                mode = EditorMapPreviewMode.Occupancy;
                normalizedMode = "occupancy";
                return true;
            case "objects":
            case "object":
                mode = EditorMapPreviewMode.Objects;
                normalizedMode = "objects";
                return true;
            case "combined":
            case "combo":
            case "all":
                mode = EditorMapPreviewMode.Combined;
                normalizedMode = "combined";
                return true;
            case "roofs":
            case "roof":
                mode = EditorMapPreviewMode.Roofs;
                normalizedMode = "roofs";
                return true;
            case "lights":
            case "light":
                mode = EditorMapPreviewMode.Lights;
                normalizedMode = "lights";
                return true;
            case "blocked":
            case "block":
                mode = EditorMapPreviewMode.Blocked;
                normalizedMode = "blocked";
                return true;
            case "scripts":
            case "script":
                mode = EditorMapPreviewMode.Scripts;
                normalizedMode = "scripts";
                return true;
            default:
                mode = EditorMapPreviewMode.Occupancy;
                normalizedMode = string.Empty;
                return false;
        }
    }

    public static bool TryParseUInt32(string text, out uint value)
    {
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return uint.TryParse(text[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);

        return uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
