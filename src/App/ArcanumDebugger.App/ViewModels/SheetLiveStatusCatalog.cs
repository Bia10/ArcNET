using System.Globalization;
using System.Linq;
using ArcNET.Diagnostics;

namespace ArcanumDebugger.App.ViewModels;

public static class SheetLiveStatusCatalog
{
    public static DebuggerSheetLiveStatus Describe(SheetReference reference, SheetSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var targetText = ResolveTargetText(snapshot.TargetText);
        return reference.Route switch
        {
            SheetRoute.BasicSkill or SheetRoute.TechSkill => DescribeSkill(reference, snapshot, targetText),
            SheetRoute.SpellMastery => DescribeSpellMastery(snapshot, targetText),
            _ => DescribeScalar(reference, snapshot, targetText),
        };
    }

    private static DebuggerSheetLiveStatus DescribeScalar(
        SheetReference reference,
        SheetSnapshot snapshot,
        string targetText
    )
    {
        var rawValue = TryReadInt32(snapshot, "value");
        var rawValueText = rawValue.ToString(CultureInfo.InvariantCulture);
        var displayValue = FirstNonEmpty(TryReadValueText(snapshot, "value_name"), rawValueText)!;
        var editorValue = SheetValueCatalog.FormatEditorValue(reference, rawValue);
        return new DebuggerSheetLiveStatus(
            "Current field state",
            displayValue.Equals(rawValueText, StringComparison.OrdinalIgnoreCase)
                ? $"{reference.DisplayName} is currently {displayValue} on {targetText}. Use Current to copy the live value into the editor."
                : $"{reference.DisplayName} is currently {displayValue} ({rawValueText}) on {targetText}. Use Current to copy the live value into the editor.",
            editorValue,
            string.Empty
        );
    }

    private static DebuggerSheetLiveStatus DescribeSkill(
        SheetReference reference,
        SheetSnapshot snapshot,
        string targetText
    )
    {
        var points = TryReadInt32(snapshot, "value");
        var trainingName = FirstNonEmpty(TryReadValueText(snapshot, "training_name"), "Unknown training")!;
        var trainingToken = ResolveTrainingToken(snapshot);
        return new DebuggerSheetLiveStatus(
            "Current skill state",
            $"{reference.DisplayName} is currently {points.ToString(CultureInfo.InvariantCulture)} point(s) with {trainingName.ToLowerInvariant()} on {targetText}. Use Current to copy both the live points and training tier into the editor.",
            points.ToString(CultureInfo.InvariantCulture),
            trainingToken
        );
    }

    private static DebuggerSheetLiveStatus DescribeSpellMastery(SheetSnapshot snapshot, string targetText)
    {
        var masteryName = FirstNonEmpty(TryReadValueText(snapshot, "value_name"), "None")!;
        var masteryToken = Normalize(masteryName) is "none" ? "none" : masteryName;
        return new DebuggerSheetLiveStatus(
            "Current spell mastery",
            $"{masteryName} is currently the active spell mastery focus on {targetText}. Use Current to copy that live focus into the editor.",
            masteryToken,
            string.Empty
        );
    }

    private static string ResolveTrainingToken(SheetSnapshot snapshot)
    {
        var trainingName = Normalize(TryReadValueText(snapshot, "training_name") ?? string.Empty);
        return trainingName switch
        {
            "untrained" => "untrained",
            "apprentice" => "apprentice",
            "expert" => "expert",
            "master" => "master",
            _ => TryReadInt32(snapshot, "training") switch
            {
                0 => "untrained",
                1 => "apprentice",
                2 => "expert",
                3 => "master",
                _ => string.Empty,
            },
        };
    }

    private static int TryReadInt32(SheetSnapshot snapshot, string key)
    {
        var valueText = TryReadValueText(snapshot, key);
        return int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private static string? TryReadValueText(SheetSnapshot snapshot, string key)
    {
        var value = snapshot.Values.FirstOrDefault(candidate => candidate.Key == key);
        return string.IsNullOrWhiteSpace(value.Key) ? null : value.ValueText;
    }

    private static string ResolveTargetText(string? targetText) =>
        string.IsNullOrWhiteSpace(targetText) ? "the selected target" : targetText;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));

    private static string Normalize(string value)
    {
        Span<char> buffer = stackalloc char[value.Length];
        var count = 0;
        foreach (var ch in value)
        {
            if (!char.IsLetterOrDigit(ch))
                continue;

            buffer[count++] = char.ToLowerInvariant(ch);
        }

        return new string(buffer[..count]);
    }
}

public sealed record class DebuggerSheetLiveStatus(
    string StatusText,
    string SummaryText,
    string ValueTokenText,
    string TrainingTokenText
);
