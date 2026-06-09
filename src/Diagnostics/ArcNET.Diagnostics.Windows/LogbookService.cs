using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

public sealed class LogbookService(ILogbookBackend backend)
{
    [SupportedOSPlatform("windows")]
    public static LogbookService Default { get; } = new(new LogbookBackend());

    public LogbookSnapshot Read(LogbookRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanReadLogbook(request.Session))
        {
            return CreateUnavailableSnapshot(
                request,
                "Logbook unavailable",
                "Logbook diagnostics require a validated runtime profile with live function invocation support.",
                page: null
            );
        }

        try
        {
            var page = ResolvePage(request.PageToken);
            var target = TargetResolver.Resolve(backend, request.Session, request.HandleToken, "logbook target");
            var result = backend.ReadLogbook(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                page
            );
            IReadOnlyList<string> notes = [.. target.Notes, .. result.Notes];
            return new LogbookSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                "Logbook read completed",
                $"Read {DescribePage(page)} logbook data for {target.TargetText}.",
                request.PageToken,
                page,
                target.HandleText,
                target.TargetText,
                result.Data,
                notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableSnapshot(request, "Invalid logbook request", ex.Message, page: null);
        }
    }

    private static bool CanReadLogbook(AttachedSessionSnapshot session) =>
        !session.HasExited
        && session.RuntimeProfile.SupportsCatalogRvas
        && session.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions);

    private static LogbookPage ResolvePage(string pageToken) =>
        Normalize(pageToken) switch
        {
            "all" => LogbookPage.All,
            "rumors" or "rumorsandnotes" or "notes" => LogbookPage.RumorsAndNotes,
            "quests" => LogbookPage.Quests,
            "reputations" or "reputation" => LogbookPage.Reputations,
            "blessings" or "blessingsandcurses" or "curses" => LogbookPage.BlessingsAndCurses,
            "kills" or "killsandinjuries" or "injuries" => LogbookPage.KillsAndInjuries,
            "background" => LogbookPage.Background,
            "keys" or "keyring" or "keyringcontents" => LogbookPage.KeyringContents,
            _ => throw new InvalidOperationException(
                $"Unknown logbook page '{pageToken}'. Expected one of: all, rumors, quests, reputations, blessings, kills, background, keys."
            ),
        };

    private static string DescribePage(LogbookPage page) =>
        page switch
        {
            LogbookPage.All => "all",
            LogbookPage.RumorsAndNotes => "rumors-and-notes",
            LogbookPage.Quests => "quest",
            LogbookPage.Reputations => "reputation",
            LogbookPage.BlessingsAndCurses => "blessing-and-curse",
            LogbookPage.KillsAndInjuries => "kills-and-injuries",
            LogbookPage.Background => "background",
            LogbookPage.KeyringContents => "keyring",
            _ => "logbook",
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

    private static LogbookSnapshot CreateUnavailableSnapshot(
        LogbookRequest request,
        string status,
        string summary,
        LogbookPage? page
    ) =>
        new(
            DateTimeOffset.UtcNow,
            IsAvailable: false,
            status,
            summary,
            request.PageToken,
            page,
            string.Empty,
            string.Empty,
            EmptyData,
            []
        );

    private static readonly LogbookPayload EmptyData = new(null, null, null, null, null, null, null);
}
