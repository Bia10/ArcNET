using System.Globalization;
using ArcNET.Diagnostics;

namespace ArcanumDebugger.App.ViewModels;

public static class LogbookQuestStateTokenCatalog
{
    public static string CreatePcMutationToken(int rawState)
    {
        var baseToken = CreateBaseToken(RuntimeWatchValueCatalog.QuestBaseState(rawState), rawState);
        return
            RuntimeWatchValueCatalog.QuestHasBotchedModifier(rawState)
            && RuntimeWatchValueCatalog.QuestBaseState(rawState) is >= 1 and <= 5
            ? $"{baseToken}-botched"
            : baseToken;
    }

    public static string CreateGlobalMutationToken(int rawState) =>
        CreateBaseToken(RuntimeWatchValueCatalog.QuestBaseState(rawState), rawState);

    private static string CreateBaseToken(int baseState, int rawState) =>
        baseState switch
        {
            0 => "unknown",
            1 => "mentioned",
            2 => "accepted",
            3 => "achieved",
            4 => "completed",
            5 => "other-completed",
            6 => "botched",
            _ => rawState.ToString(CultureInfo.InvariantCulture),
        };
}
