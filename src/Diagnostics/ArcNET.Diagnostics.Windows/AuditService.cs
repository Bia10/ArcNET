using System.Runtime.Versioning;
using ArcNET.Diagnostics;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class AuditService(IAuditBackend backend)
{
    [SupportedOSPlatform("windows")]
    public static AuditService Default { get; } = new(new AuditBackend());

    public AuditSnapshot Run(AuditRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var dispatcher = request.IncludeDispatcher
            ? backend.AuditDispatcher(request.Session.ProcessId, request.Session.RuntimeProfile)
            : null;
        var functions = request.IncludeFunctions ? backend.AuditFunctions(request.Session.ProcessId) : null;
        var hooks = request.IncludeHooks
            ? backend.AuditHooks(
                request.Session.ProcessId,
                request.HookSelectors,
                request.HookDuration,
                request.IncludeWatchPass,
                request.IncludeInterceptPass,
                request.StackCaptureDwordCount,
                request.StopOnFailure
            )
            : null;

        return new AuditSnapshot(
            DateTimeOffset.UtcNow,
            request.Session.Fingerprint,
            request.Session.RuntimeProfile,
            dispatcher,
            functions,
            hooks,
            CreateNotes(request)
        );
    }

    private static void Validate(AuditRequest request)
    {
        ArgumentNullException.ThrowIfNull(request.Session);
        ArgumentNullException.ThrowIfNull(request.HookSelectors);

        if (!request.IncludeHooks)
            return;

        if (request.HookDuration <= TimeSpan.Zero)
            throw new InvalidOperationException("Hook audit duration must be positive.");

        if (request.StackCaptureDwordCount is < 1 or > RuntimeInterceptionSession.MaximumStackCaptureDwordCount)
        {
            throw new InvalidOperationException(
                $"Hook audit stack capture dword count must be between 1 and {RuntimeInterceptionSession.MaximumStackCaptureDwordCount}."
            );
        }

        if (!request.IncludeWatchPass && !request.IncludeInterceptPass)
            throw new InvalidOperationException("Hook audit must enable watch and/or intercept coverage.");
    }

    private static IReadOnlyList<string> CreateNotes(AuditRequest request)
    {
        List<string> notes = [.. request.Session.Notes];
        if (!request.IncludeHooks)
            return notes.Count == 0 ? [] : [.. notes];

        notes.Add(
            "This audit proves resolution plus short install/detach smoke-test behavior on the currently running process. It does not prove arbitrary semantic correctness for every possible argument payload."
        );
        notes.Add(
            "Watch/intercept event counts only reflect activity that actually happened during the short audit window."
        );
        notes.Add("A hook can pass installation even when no runtime event fires during the sample window.");
        return [.. notes];
    }
}
