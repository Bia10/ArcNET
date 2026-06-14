using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class AuditBackend : IAuditBackend
{
    public DispatcherAuditSnapshot AuditDispatcher(int processId, RuntimeProfileSnapshot runtimeProfile)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Debugger audit currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        try
        {
            using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
            return new DispatcherAuditSnapshot(true, dispatcher.ModeDescription, dispatcher.SiteDescription, null);
        }
        catch (Exception ex)
        {
            return new DispatcherAuditSnapshot(false, null, null, ex.Message);
        }
    }

    public FunctionAuditSnapshot AuditFunctions(int processId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Debugger audit currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        var results = FunctionCatalog
            .KnownFunctionKeys.Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static key => key, StringComparer.OrdinalIgnoreCase)
            .Select(key => AuditFunction(memory, key))
            .ToArray();

        return new FunctionAuditSnapshot(
            results.Length,
            results.Count(static result => result.Success),
            results.Count(static result => !result.Success),
            results
        );
    }

    public HookAuditSnapshot AuditHooks(
        int processId,
        IReadOnlyList<string> selectors,
        TimeSpan duration,
        bool includeWatch,
        bool includeIntercept,
        int stackCaptureDwordCount,
        bool stopOnFailure
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(selectors);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Debugger audit currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        var selectedHooks = RuntimeWatchCatalog.ResolveSelectors(selectors);
        var moduleFileName = Path.GetFileName(memory.ModulePath);
        List<HookAuditResultSnapshot> hookResults = new(selectedHooks.Count);
        string? abortedAtHook = null;
        var processExited = false;

        foreach (var hook in selectedHooks)
        {
            var bind = BindHook(memory, hook);
            HookPassAuditSnapshot? watch = null;
            HookPassAuditSnapshot? intercept = null;

            if (bind.BoundHook is { } boundHook)
            {
                if (includeWatch)
                {
                    watch = AuditWatchPass(memory, boundHook, duration, moduleFileName);
                    if (!watch.Success && stopOnFailure)
                    {
                        hookResults.Add(
                            new HookAuditResultSnapshot(hook.Key, hook.Area, bind.Snapshot, watch, intercept)
                        );
                        abortedAtHook = hook.Key;
                        break;
                    }
                }

                if (includeIntercept)
                {
                    intercept = AuditInterceptPass(memory, boundHook, duration, stackCaptureDwordCount, moduleFileName);
                    if (!intercept.Success && stopOnFailure)
                    {
                        hookResults.Add(
                            new HookAuditResultSnapshot(hook.Key, hook.Area, bind.Snapshot, watch, intercept)
                        );
                        abortedAtHook = hook.Key;
                        break;
                    }
                }
            }

            hookResults.Add(new HookAuditResultSnapshot(hook.Key, hook.Area, bind.Snapshot, watch, intercept));
            if (!memory.HasExited)
                continue;

            processExited = true;
            abortedAtHook = hook.Key;
            break;
        }

        var results = hookResults.ToArray();
        return new HookAuditSnapshot(
            [.. selectors],
            checked((int)duration.TotalMilliseconds),
            includeWatch,
            includeIntercept,
            stackCaptureDwordCount,
            results.Length,
            results.Count(static result => result.Bind.Success),
            results.Count(static result => !result.Bind.Success),
            results.Count(static result => result.Watch?.Success == true),
            results.Count(static result => result.Watch?.Success == false),
            results.Count(static result => result.Intercept?.Success == true),
            results.Count(static result => result.Intercept?.Success == false),
            results.Count(static result => result.Watch?.ObservedEvents == true),
            results.Count(static result => result.Intercept?.ObservedEvents == true),
            results.Count(static result => (result.Watch?.DroppedEvents ?? 0) > 0),
            results.Count(static result => (result.Intercept?.DroppedEvents ?? 0) > 0),
            processExited,
            abortedAtHook,
            results
        );
    }

    private static FunctionAuditResultSnapshot AuditFunction(ProcessMemory memory, string key)
    {
        try
        {
            var function = FunctionCatalog.GetDefinition(key);
            var address = RuntimeCatalogAddressResolver.Resolve(
                memory,
                function.Key,
                function.Rva,
                $"Debugger function '{function.Key}'"
            );
            return new FunctionAuditResultSnapshot(key, true, address.Site, address.Resolution, function.Summary, null);
        }
        catch (Exception ex)
        {
            return new FunctionAuditResultSnapshot(key, false, null, null, null, ex.Message);
        }
    }

    private static BoundHookResult BindHook(ProcessMemory memory, RuntimeWatchHookDefinition hook)
    {
        try
        {
            var boundHook = RuntimeCatalogAddressResolver.BindHooks(memory, [hook], "Debugger hook audit")[0];
            return new BoundHookResult(new HookBindAuditSnapshot(true, boundHook.Site, null), boundHook);
        }
        catch (Exception ex)
        {
            return new BoundHookResult(new HookBindAuditSnapshot(false, hook.Site, ex.Message), null);
        }
    }

    private static HookPassAuditSnapshot AuditWatchPass(
        ProcessMemory memory,
        RuntimeWatchHookDefinition hook,
        TimeSpan duration,
        string moduleFileName
    )
    {
        try
        {
            using var session = RuntimeWatchSession.Install(memory, [hook]);
            Thread.Sleep(duration);
            var poll = session.ReadSince(0);
            return new HookPassAuditSnapshot(
                true,
                poll.Events.Count > 0,
                poll.Events.Count,
                poll.DroppedEvents,
                poll.InconsistentRecords,
                poll.ContentionDrops,
                poll.Events.Count > 0
                    ? CodeCatalog.FormatModuleAddress(moduleFileName, poll.Events[0].CallerRva)
                    : null,
                null
            );
        }
        catch (Exception ex)
        {
            return new HookPassAuditSnapshot(false, false, 0, 0, 0, 0, null, ex.Message);
        }
    }

    private static HookPassAuditSnapshot AuditInterceptPass(
        ProcessMemory memory,
        RuntimeWatchHookDefinition hook,
        TimeSpan duration,
        int stackCaptureDwordCount,
        string moduleFileName
    )
    {
        try
        {
            var definition = new RuntimeInterceptionDefinition(
                hook.Key,
                memory.ToUInt32Address(memory.ResolveRva(hook.Rva)),
                unchecked((uint)hook.Rva),
                hook.Site,
                stackCaptureDwordCount,
                CreatePassthroughMutation()
            );
            using var session = RuntimeInterceptionSession.Install(memory, definition);
            Thread.Sleep(duration);
            var poll = session.ReadSince(0);
            return new HookPassAuditSnapshot(
                true,
                poll.Events.Count > 0,
                poll.Events.Count,
                poll.DroppedEvents,
                poll.InconsistentRecords,
                poll.ContentionDrops,
                poll.Events.Count > 0
                    ? CodeCatalog.FormatModuleAddress(moduleFileName, poll.Events[0].CallerRva)
                    : null,
                null
            );
        }
        catch (Exception ex)
        {
            return new HookPassAuditSnapshot(false, false, 0, 0, 0, 0, null, ex.Message);
        }
    }

    private static RuntimeInterceptionMutation CreatePassthroughMutation() =>
        new(
            RuntimeInterceptionExecutionMode.ContinueOriginal,
            0,
            0,
            0,
            default,
            0,
            new uint[RuntimeInterceptionLimits.MaximumStackCaptureDwordCount],
            0
        );

    private readonly record struct BoundHookResult(
        HookBindAuditSnapshot Snapshot,
        RuntimeWatchHookDefinition? BoundHook
    );
}
