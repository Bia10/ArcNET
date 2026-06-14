namespace ArcNET.Diagnostics;

public static class KillLogbookSummaryCatalog
{
    public static IReadOnlyList<KillLogbookSummaryDefinition> Definitions { get; } =
    [
        new(
            LogbookMutationKind.SetTotalKills,
            "total_kills",
            "Total Kills",
            "Set Total Kills",
            string.Empty,
            "Total kills",
            "Total kills",
            null,
            0
        ),
        new(
            LogbookMutationKind.SetMostPowerfulKill,
            "most_powerful",
            "Most Powerful",
            "Set Most Powerful Kill",
            "description",
            "Level",
            "Level",
            1,
            2
        ),
        new(
            LogbookMutationKind.SetLeastPowerfulKill,
            "least_powerful",
            "Least Powerful",
            "Set Least Powerful Kill",
            "description",
            "Level",
            "Level",
            3,
            4
        ),
        new(
            LogbookMutationKind.SetMostGoodKill,
            "most_good",
            "Most Good",
            "Set Most Good Kill",
            "description",
            "Good rating",
            "Good rating",
            5,
            6
        ),
        new(
            LogbookMutationKind.SetMostEvilKill,
            "most_evil",
            "Most Evil",
            "Set Most Evil Kill",
            "description",
            "Evil rating",
            "Evil rating",
            7,
            8
        ),
        new(
            LogbookMutationKind.SetMostMagicalKill,
            "most_magical",
            "Most Magical",
            "Set Most Magical Kill",
            "description",
            "Magick aptitude",
            "Magick aptitude",
            9,
            10
        ),
        new(
            LogbookMutationKind.SetMostTechKill,
            "most_tech",
            "Most Tech",
            "Set Most Tech Kill",
            "description",
            "Tech aptitude",
            "Tech aptitude",
            11,
            12
        ),
    ];

    public static bool TryGetDefinition(LogbookMutationKind kind, out KillLogbookSummaryDefinition definition)
    {
        var match = Definitions.FirstOrDefault(candidate => candidate.MutationKind == kind);
        definition = match!;
        return match is not null;
    }

    public static bool TryGetDefinition(string key, out KillLogbookSummaryDefinition definition)
    {
        var match = Definitions.FirstOrDefault(candidate =>
            candidate.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
        );
        definition = match!;
        return match is not null;
    }
}
