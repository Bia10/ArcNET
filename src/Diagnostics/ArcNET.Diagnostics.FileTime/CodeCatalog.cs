using System.Reflection;
using System.Text;

namespace ArcNET.Diagnostics;

public static class CodeCatalog
{
    public static string DefaultModuleFileName => RuntimeOffsets.ModuleName;

    public static bool TryResolveAnchor(uint rva, out ResolvedCodeAnchor resolvedAnchor)
    {
        resolvedAnchor = default;
        if (s_anchors.Length == 0)
            return false;

        CodeAnchor? nearestAnchor = null;
        foreach (var anchor in s_anchors)
        {
            if ((uint)anchor.Rva > rva)
                break;

            nearestAnchor = anchor;
        }

        if (nearestAnchor is not { } value)
            return false;

        var delta = rva - (uint)value.Rva;
        if (delta > MaximumNearestAnchorDelta)
            return false;

        resolvedAnchor = new ResolvedCodeAnchor(value, delta);
        return true;
    }

    public static string FormatModuleOffset(uint rva) => FormatModuleOffset(DefaultModuleFileName, rva);

    public static string FormatModuleOffset(string moduleFileName, uint rva) =>
        TryResolveAnchor(rva, out var resolvedAnchor)
            ? resolvedAnchor.DisplayLabel
            : ModuleAddressFormatter.FormatModuleOffset(moduleFileName, rva);

    public static string FormatModuleAddress(uint rva) => FormatModuleAddress(DefaultModuleFileName, rva);

    public static string FormatModuleAddress(string moduleFileName, uint rva)
    {
        var offsetText = ModuleAddressFormatter.FormatModuleOffset(moduleFileName, rva);
        return TryResolveAnchor(rva, out var resolvedAnchor)
            ? $"{resolvedAnchor.DisplayLabel} ({offsetText})"
            : offsetText;
    }

    private static CodeAnchor[] BuildAnchors() =>
        [
            .. typeof(RuntimeOffsets)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(static field =>
                    field.IsLiteral
                    && field.FieldType == typeof(int)
                    && field.Name.EndsWith("Rva", StringComparison.Ordinal)
                    && !field.Name.EndsWith("EndExclusiveRva", StringComparison.Ordinal)
                )
                .Select(static field => new CodeAnchor((int)field.GetRawConstantValue()!, FormatKey(field.Name[..^3])))
                .OrderBy(static anchor => anchor.Rva),
        ];

    private static string FormatKey(string rawName)
    {
        if (rawName.Length == 0)
            return string.Empty;

        var builder = new StringBuilder(rawName.Length + 8);
        for (var index = 0; index < rawName.Length; index++)
        {
            var current = rawName[index];
            var previous = index > 0 ? rawName[index - 1] : '\0';
            var next = index + 1 < rawName.Length ? rawName[index + 1] : '\0';
            if (
                index > 0
                && char.IsUpper(current)
                && (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && char.IsLower(next)))
            )
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }

    private const uint MaximumNearestAnchorDelta = 0x1000;

    private static readonly CodeAnchor[] s_anchors = BuildAnchors();
}
