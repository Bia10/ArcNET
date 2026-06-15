using System.Globalization;
using System.Linq;
using ArcNET.Diagnostics;
using ArcNET.GameObjects.Metadata;

namespace ArcanumDebugger.App.ViewModels;

public static class SheetMutationOptionCatalog
{
    public static SheetMutationEditorDescriptor Fallback { get; } =
        new(
            ShowsValueSelector: false,
            ShowsValueInput: true,
            ValueOptions: [],
            "Value, rank, degree, or none",
            ShowsTrainingSelector: false,
            ShowsTrainingInput: true,
            TrainingOptions: [],
            "Training optional for skills"
        );

    public static SheetMutationEditorDescriptor Describe(SheetRoute route) =>
        route switch
        {
            SheetRoute.SpellCollege => new(
                ShowsValueSelector: true,
                ShowsValueInput: false,
                ValueOptions: s_spellCollegeRankOptions,
                "Spell-college rank 0-5",
                ShowsTrainingSelector: false,
                ShowsTrainingInput: false,
                TrainingOptions: [],
                string.Empty
            ),
            SheetRoute.TechDiscipline => new(
                ShowsValueSelector: true,
                ShowsValueInput: false,
                ValueOptions: s_techDisciplineDegreeOptions,
                "Tech-discipline degree 0-7",
                ShowsTrainingSelector: false,
                ShowsTrainingInput: false,
                TrainingOptions: [],
                string.Empty
            ),
            SheetRoute.SpellMastery => new(
                ShowsValueSelector: true,
                ShowsValueInput: false,
                ValueOptions: s_spellMasteryOptions,
                "Spell mastery college or none",
                ShowsTrainingSelector: false,
                ShowsTrainingInput: false,
                TrainingOptions: [],
                string.Empty
            ),
            SheetRoute.BasicSkill or SheetRoute.TechSkill => new(
                ShowsValueSelector: false,
                ShowsValueInput: true,
                ValueOptions: [],
                "Skill points 0-63",
                ShowsTrainingSelector: true,
                ShowsTrainingInput: false,
                TrainingOptions: s_trainingOptions,
                "Training tier"
            ),
            _ => new(
                ShowsValueSelector: false,
                ShowsValueInput: true,
                ValueOptions: [],
                "Signed 32-bit value",
                ShowsTrainingSelector: false,
                ShowsTrainingInput: false,
                TrainingOptions: [],
                string.Empty
            ),
        };

    public static SheetMutationEditorDescriptor Describe(SheetReference reference)
    {
        var specialValueOptions = SheetValueCatalog.GetValueOptions(reference);
        if (specialValueOptions.Count == 0)
            return Describe(reference.Route);

        return new SheetMutationEditorDescriptor(
            ShowsValueSelector: true,
            ShowsValueInput: false,
            ValueOptions: [.. specialValueOptions.Select(static option => ToChoiceOption(option))],
            reference.Id == 26 ? "Gender" : "Race",
            ShowsTrainingSelector: false,
            ShowsTrainingInput: false,
            TrainingOptions: [],
            string.Empty
        );
    }

    public static DebuggerChoiceOption? ResolveValueOption(SheetRoute route, string? valueText)
    {
        if (string.IsNullOrWhiteSpace(valueText))
            return null;

        try
        {
            return route switch
            {
                SheetRoute.SpellCollege => ResolveNumericOption(
                    s_spellCollegeRankOptions,
                    SpellTechCatalog.ParseLevel(
                        valueText,
                        "Spell-college rank",
                        minimum: 0,
                        maximumInclusive: SpellTechCatalog.SpellMaxLevel
                    )
                ),
                SheetRoute.TechDiscipline => ResolveNumericOption(
                    s_techDisciplineDegreeOptions,
                    SpellTechCatalog.ParseLevel(valueText, "Tech-discipline degree", minimum: 0, maximumInclusive: 7)
                ),
                SheetRoute.SpellMastery => ResolveSpellMasteryOption(valueText),
                _ => null,
            };
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static DebuggerChoiceOption? ResolveValueOption(SheetReference reference, string? valueText)
    {
        var specialValueOptions = SheetValueCatalog.GetValueOptions(reference);
        if (specialValueOptions.Count == 0)
            return ResolveValueOption(reference.Route, valueText);

        if (string.IsNullOrWhiteSpace(valueText))
            return null;

        try
        {
            var canonicalToken = SheetValueCatalog.FormatEditorValue(
                reference,
                SheetValueCatalog.ParseValue(reference, valueText)
            );
            return specialValueOptions
                .Select(static option => ToChoiceOption(option))
                .FirstOrDefault(option => option.Token.Equals(canonicalToken, StringComparison.OrdinalIgnoreCase));
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static DebuggerChoiceOption? ResolveTrainingOption(string? trainingText)
    {
        if (string.IsNullOrWhiteSpace(trainingText))
            return s_trainingOptions[0];

        var normalized = SpellTechCatalog.Normalize(trainingText);
        var token = normalized switch
        {
            "0" or "untrained" or "none" => "untrained",
            "1" or "apprentice" => "apprentice",
            "2" or "expert" => "expert",
            "3" or "master" => "master",
            _ => string.Empty,
        };
        return token.Length == 0
            ? null
            : s_trainingOptions.FirstOrDefault(option =>
                option.Token.Equals(token, StringComparison.OrdinalIgnoreCase)
            );
    }

    private static DebuggerChoiceOption? ResolveNumericOption(
        IReadOnlyList<DebuggerChoiceOption> options,
        int numericValue
    )
    {
        var token = numericValue.ToString(CultureInfo.InvariantCulture);
        return options.FirstOrDefault(option => option.Token.Equals(token, StringComparison.OrdinalIgnoreCase));
    }

    private static DebuggerChoiceOption? ResolveSpellMasteryOption(string valueText)
    {
        var normalized = SpellTechCatalog.Normalize(valueText);
        if (normalized is "none" or "clear" or "unset")
            return s_spellMasteryOptions[0];

        var collegeId = SpellTechCatalog.ParseSpellCollegeId(valueText);
        var token = CharacterSheetMetadata.SpellCollegeName(collegeId);
        return s_spellMasteryOptions.FirstOrDefault(option =>
            option.Token.Equals(token, StringComparison.OrdinalIgnoreCase)
        );
    }

    private static DebuggerChoiceOption ToChoiceOption(SheetValueOption option) =>
        new(option.Token, option.Label, option.Description);

    private static DebuggerChoiceOption[] BuildLevelOptions(int maximumInclusive, string labelPrefix) =>
        [
            .. Enumerable
                .Range(0, maximumInclusive + 1)
                .Select(level => new DebuggerChoiceOption(
                    level.ToString(CultureInfo.InvariantCulture),
                    $"{labelPrefix} {level.ToString(CultureInfo.InvariantCulture)}",
                    $"{labelPrefix} {level.ToString(CultureInfo.InvariantCulture)}"
                )),
        ];

    private static DebuggerChoiceOption[] BuildSpellMasteryOptions() =>
        [
            new("none", "None", "Clears spell mastery on the selected character."),
            .. Enumerable
                .Range(0, SpellTechCatalog.SpellCollegeCount)
                .Select(collegeId =>
                {
                    var collegeName = CharacterSheetMetadata.SpellCollegeName(collegeId);
                    return new DebuggerChoiceOption(
                        collegeName,
                        collegeName,
                        $"{collegeName} becomes the active spell mastery focus."
                    );
                }),
        ];

    private static readonly DebuggerChoiceOption[] s_spellCollegeRankOptions = BuildLevelOptions(
        SpellTechCatalog.SpellMaxLevel,
        "Rank"
    );

    private static readonly DebuggerChoiceOption[] s_techDisciplineDegreeOptions = BuildLevelOptions(7, "Degree");

    private static readonly DebuggerChoiceOption[] s_spellMasteryOptions = BuildSpellMasteryOptions();

    private static readonly DebuggerChoiceOption[] s_trainingOptions =
    [
        new(
            string.Empty,
            "Keep Current",
            "Leaves the current training tier unchanged while only the point value is updated."
        ),
        new("untrained", "Untrained", "Writes untrained skill status."),
        new("apprentice", "Apprentice", "Writes apprentice skill training."),
        new("expert", "Expert", "Writes expert skill training."),
        new("master", "Master", "Writes master skill training."),
    ];
}

public sealed record class SheetMutationEditorDescriptor(
    bool ShowsValueSelector,
    bool ShowsValueInput,
    IReadOnlyList<DebuggerChoiceOption> ValueOptions,
    string ValuePlaceholderText,
    bool ShowsTrainingSelector,
    bool ShowsTrainingInput,
    IReadOnlyList<DebuggerChoiceOption> TrainingOptions,
    string TrainingPlaceholderText
);
