using System.Globalization;

namespace ArcNET.Diagnostics;

public static class SheetValueCatalog
{
    public static IReadOnlyList<SheetValueOption> GetValueOptions(SheetReference reference) =>
        reference.Route == SheetRoute.Stat
            ? reference.Id switch
            {
                GenderStatId => s_genderOptions,
                RaceStatId => s_raceOptions,
                _ => [],
            }
            : [];

    public static int ParseValue(SheetReference reference, string? valueText) =>
        reference.Route == SheetRoute.Stat
            ? reference.Id switch
            {
                GenderStatId => ParseGender(valueText),
                RaceStatId => ParseRace(valueText),
                _ => ParseSignedInt32(valueText, reference.DisplayName),
            }
            : ParseSignedInt32(valueText, reference.DisplayName);

    public static string FormatDisplayValue(SheetReference reference, int value) =>
        reference.Route == SheetRoute.Stat
            ? reference.Id switch
            {
                GenderStatId => DescribeGender(value),
                RaceStatId => DescribeRace(value),
                _ => value.ToString(CultureInfo.InvariantCulture),
            }
            : value.ToString(CultureInfo.InvariantCulture);

    public static string FormatEditorValue(SheetReference reference, int value) =>
        reference.Route == SheetRoute.Stat
            ? reference.Id switch
            {
                GenderStatId => GenderToken(value),
                RaceStatId => RaceToken(value),
                _ => value.ToString(CultureInfo.InvariantCulture),
            }
            : value.ToString(CultureInfo.InvariantCulture);

    public static string CreateInputHint(SheetReference reference) =>
        reference.Route == SheetRoute.Stat
            ? reference.Id switch
            {
                GenderStatId => "Use Male or Female.",
                RaceStatId => "Use one race such as Human, Elf, Half-Ogre, or a supported race id.",
                _ => "Enter a signed 32-bit integer value.",
            }
            : "Enter a signed 32-bit integer value.";

    private static int ParseGender(string? valueText)
    {
        if (string.IsNullOrWhiteSpace(valueText))
            throw new InvalidOperationException("Gender value is required. Use Male or Female.");

        return Normalize(valueText) switch
        {
            "0" or "male" or "m" or "masculine" => 0,
            "1" or "female" or "f" or "feminine" => 1,
            _ => throw new InvalidOperationException($"Unknown gender '{valueText}'. Use Male, Female, 0, or 1."),
        };
    }

    private static int ParseRace(string? valueText)
    {
        if (string.IsNullOrWhiteSpace(valueText))
        {
            throw new InvalidOperationException(
                "Race value is required. Use one race name such as Human or Elf, or a numeric race id."
            );
        }

        var normalized = Normalize(valueText);
        if (int.TryParse(valueText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericValue))
        {
            return numericValue is >= 0 and <= 10
                ? numericValue
                : throw new InvalidOperationException($"Race id '{valueText}' is outside the supported range 0-10.");
        }

        return normalized switch
        {
            "human" => 0,
            "halfling" => 1,
            "halfelf" => 2,
            "halfogre" => 3,
            "dwarf" => 4,
            "gnome" => 5,
            "halforc" => 6,
            "race7" or "unknown7" => 7,
            "darkelf" => 8,
            "elf" => 9,
            "race10" or "unknown10" => 10,
            _ => throw new InvalidOperationException(
                $"Unknown race '{valueText}'. Use one race name such as Human or Elf, or a numeric race id."
            ),
        };
    }

    private static int ParseSignedInt32(string? valueText, string label)
    {
        if (string.IsNullOrWhiteSpace(valueText))
            throw new InvalidOperationException($"{label} value is required.");

        return int.TryParse(valueText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new InvalidOperationException($"{label} must be a signed 32-bit integer value.");
    }

    private static string DescribeGender(int value) =>
        value switch
        {
            0 => "Male",
            1 => "Female",
            _ => $"Gender {value.ToString(CultureInfo.InvariantCulture)}",
        };

    private static string DescribeRace(int value) =>
        value switch
        {
            0 => "Human",
            1 => "Halfling",
            2 => "HalfElf",
            3 => "Half-Ogre",
            4 => "Dwarf",
            5 => "Gnome",
            6 => "HalfOrc",
            7 => "Unknown 7",
            8 => "DarkElf",
            9 => "Elf",
            10 => "Unknown 10",
            _ => $"Race {value.ToString(CultureInfo.InvariantCulture)}",
        };

    private static string GenderToken(int value) =>
        value switch
        {
            0 => "Male",
            1 => "Female",
            _ => value.ToString(CultureInfo.InvariantCulture),
        };

    private static string RaceToken(int value) =>
        value switch
        {
            0 => "Human",
            1 => "Halfling",
            2 => "HalfElf",
            3 => "Half-Ogre",
            4 => "Dwarf",
            5 => "Gnome",
            6 => "HalfOrc",
            7 => "7",
            8 => "DarkElf",
            9 => "Elf",
            10 => "10",
            _ => value.ToString(CultureInfo.InvariantCulture),
        };

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

    private const int GenderStatId = 26;
    private const int RaceStatId = 27;

    private static readonly SheetValueOption[] s_genderOptions =
    [
        new("Male", "Male", "Writes male gender."),
        new("Female", "Female", "Writes female gender."),
    ];

    private static readonly SheetValueOption[] s_raceOptions =
    [
        new("Human", "Human", "Writes the Human race."),
        new("Halfling", "Halfling", "Writes the Halfling race."),
        new("HalfElf", "HalfElf", "Writes the Half-Elf race."),
        new("Half-Ogre", "Half-Ogre", "Writes the Half-Ogre race."),
        new("Dwarf", "Dwarf", "Writes the Dwarf race."),
        new("Gnome", "Gnome", "Writes the Gnome race."),
        new("HalfOrc", "HalfOrc", "Writes the Half-Orc race."),
        new("7", "Unknown 7", "Writes race id 7."),
        new("DarkElf", "DarkElf", "Writes the Dark Elf race."),
        new("Elf", "Elf", "Writes the Elf race."),
        new("10", "Unknown 10", "Writes race id 10."),
    ];
}
