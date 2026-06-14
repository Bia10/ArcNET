using ArcanumDebugger.App.ViewModels;
using ArcNET.Diagnostics;

namespace ArcanumDebugger.App.Tests;

public sealed class LogbookQuestStateTokenCatalogTests
{
    [Test]
    public async Task CreatePcMutationToken_WhenQuestUsesBotchedVariant_PreservesBaseStateToken()
    {
        var token = LogbookQuestStateTokenCatalog.CreatePcMutationToken(
            RuntimeWatchValueCatalog.QuestBotchedModifier | 2
        );

        await Assert.That(token).IsEqualTo("accepted-botched");
    }

    [Test]
    public async Task CreateGlobalMutationToken_WhenQuestUsesBotchedVariant_FallsBackToCanonicalGlobalToken()
    {
        var token = LogbookQuestStateTokenCatalog.CreateGlobalMutationToken(
            RuntimeWatchValueCatalog.QuestBotchedModifier | 2
        );

        await Assert.That(token).IsEqualTo("accepted");
    }
}
