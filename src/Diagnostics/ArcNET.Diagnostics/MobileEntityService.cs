using System.Globalization;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed class MobileEntityService(IMobileEntityBackend backend)
{
    public MobileRosterSnapshot ListMobiles(MobileRosterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanReadRoster(request.Session))
            return CreateUnavailableRoster(
                "Mobile roster unavailable",
                CreateRosterAvailabilitySummary(request.Session)
            );

        try
        {
            var maxEntries = request.MaxEntries > 0 ? request.MaxEntries : DefaultMaxEntries;
            var mobiles = backend.ListLiveMobiles(request.Session.ProcessId, maxEntries).Select(CreateEntry).ToArray();
            return new MobileRosterSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                mobiles.Length == 0 ? "No live mobiles found" : $"Loaded {mobiles.Length} live mobile(s)",
                mobiles.Length == 0
                    ? "The live object pool did not expose any NPC or PC instances with prototype-backed runtime handles."
                    : $"Scanned the live object pool and found {mobiles.Length} PC/NPC instance(s) with runtime prototype handles.",
                mobiles,
                []
            );
        }
        catch (Exception ex) when (ex is InvalidOperationException or OverflowException)
        {
            return CreateUnavailableRoster("Mobile roster failed", ex.Message);
        }
    }

    public MobileMutationSnapshot SetStat(MobileStatWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanMutateMobiles(request.Session))
            return CreateUnavailableMutation(
                "Mobile editor unavailable",
                CreateMutationAvailabilitySummary(request.Session)
            );

        try
        {
            var timeout = ParseTimeout(request.TimeoutMillisecondsText);
            var statId = ParseStatId(request.StatToken);
            var value = ParseValue(request.ValueText);
            var target = TargetResolver.Resolve(backend, request.Session, request.TargetHandleToken, "mobile target");
            var execution = backend.SetMobileStat(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                statId,
                value,
                timeout
            );
            return new MobileMutationSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                "Mobile stat updated",
                $"Set {RuntimeSemanticCatalog.StatName(statId)} on {target.TargetText} to {value.ToString(CultureInfo.InvariantCulture)}.",
                target.HandleText,
                target.TargetText,
                string.Empty,
                RuntimeSemanticCatalog.StatName(statId),
                value.ToString(CultureInfo.InvariantCulture),
                $"{execution.DispatcherMode} · {execution.DispatcherSite}",
                execution.ExecutionDetailText,
                execution.ResultText,
                target.Notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableMutation("Invalid mobile stat request", ex.Message);
        }
    }

    public MobileMutationSnapshot Kill(MobileActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanMutateMobiles(request.Session))
            return CreateUnavailableMutation(
                "Mobile editor unavailable",
                CreateMutationAvailabilitySummary(request.Session)
            );

        try
        {
            var timeout = ParseTimeout(request.TimeoutMillisecondsText);
            var target = TargetResolver.Resolve(backend, request.Session, request.TargetHandleToken, "mobile target");
            var execution = backend.KillMobile(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                timeout
            );
            return new MobileMutationSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                "Mobile kill triggered",
                $"Triggered the native death path for {target.TargetText}.",
                target.HandleText,
                target.TargetText,
                string.Empty,
                string.Empty,
                string.Empty,
                $"{execution.DispatcherMode} · {execution.DispatcherSite}",
                execution.ExecutionDetailText,
                execution.ResultText,
                target.Notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableMutation("Invalid mobile kill request", ex.Message);
        }
    }

    public MobileMutationSnapshot Despawn(MobileActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanMutateMobiles(request.Session))
            return CreateUnavailableMutation(
                "Mobile editor unavailable",
                CreateMutationAvailabilitySummary(request.Session)
            );

        try
        {
            var timeout = ParseTimeout(request.TimeoutMillisecondsText);
            var target = TargetResolver.Resolve(backend, request.Session, request.TargetHandleToken, "mobile target");
            var execution = backend.DespawnMobile(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                timeout
            );
            return new MobileMutationSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                "Mobile despawned",
                $"Destroyed {target.TargetText} through object_destroy.",
                target.HandleText,
                target.TargetText,
                string.Empty,
                string.Empty,
                string.Empty,
                $"{execution.DispatcherMode} · {execution.DispatcherSite}",
                execution.ExecutionDetailText,
                execution.ResultText,
                target.Notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableMutation("Invalid mobile despawn request", ex.Message);
        }
    }

    public MobileMutationSnapshot Spawn(MobileSpawnRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanMutateMobiles(request.Session))
            return CreateUnavailableMutation(
                "Mobile editor unavailable",
                CreateMutationAvailabilitySummary(request.Session)
            );

        if (!RuntimeSemanticCatalog.LooksLikeObjectHandle(request.PrototypeHandle))
        {
            return CreateUnavailableMutation(
                "Invalid prototype handle",
                $"Prototype handle {RuntimeSemanticCatalog.FormatHandle(request.PrototypeHandle)} is not a live handle-shaped runtime value."
            );
        }

        try
        {
            var timeout = ParseTimeout(request.TimeoutMillisecondsText);
            var anchor = TargetResolver.Resolve(backend, request.Session, request.AnchorHandleToken, "spawn anchor");
            var execution = backend.SpawnMobile(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                request.PrototypeHandle,
                anchor.Handle,
                timeout
            );
            var mobileHandleText = RuntimeSemanticCatalog.FormatHandle(execution.RelatedHandle);
            List<string> notes = [.. anchor.Notes];
            return new MobileMutationSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                "Prototype created",
                $"Created {mobileHandleText} at {anchor.TargetText}.",
                mobileHandleText,
                mobileHandleText,
                RuntimeSemanticCatalog.FormatHandle(request.PrototypeHandle),
                string.Empty,
                string.Empty,
                $"{execution.DispatcherMode} · {execution.DispatcherSite}",
                execution.ExecutionDetailText,
                execution.ResultText,
                notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableMutation("Invalid mobile spawn request", ex.Message);
        }
    }

    private static bool CanReadRoster(AttachedSessionSnapshot session) =>
        !session.HasExited && session.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.ReadStructuredState);

    private static bool CanMutateMobiles(AttachedSessionSnapshot session) =>
        !session.HasExited
        && session.RuntimeProfile.SupportsCatalogRvas
        && session.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions);

    private static string CreateRosterAvailabilitySummary(AttachedSessionSnapshot session)
    {
        if (session.HasExited)
            return "The attached process has exited, so the live mobile roster is unavailable until a new session is attached.";

        return "Live mobile scanning requires structured-state capability.";
    }

    private static string CreateMutationAvailabilitySummary(AttachedSessionSnapshot session)
    {
        if (session.HasExited)
            return "The attached process has exited, so live mobile edits are unavailable until a new session is attached.";

        return "Live mobile edits require a validated runtime profile with live function invocation support.";
    }

    private static MobileRosterSnapshot CreateUnavailableRoster(string status, string summary) =>
        new(DateTimeOffset.UtcNow, IsAvailable: false, status, summary, [], []);

    private static MobileMutationSnapshot CreateUnavailableMutation(string status, string summary) =>
        new(
            DateTimeOffset.UtcNow,
            IsAvailable: false,
            status,
            summary,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            "Dispatcher result unavailable.",
            "Target address and hook details will appear here after a live mobile mutation.",
            "Mutation result values will appear here after a live mobile mutation.",
            []
        );

    private static MobileRosterEntrySnapshot CreateEntry(LiveObjectIdentity identity)
    {
        var header = identity.Header;
        var objectTypeText = header?.ObjectTypeName ?? "Mobile";
        var objectIdText = header?.ObjectId.Label ?? identity.HandleHex;
        var prototypeText = header?.PrototypeId.Label ?? "unknown-proto";
        var prototypeHandleText = header?.PrototypeHandle ?? "unknown-prototype-handle";
        var displayText = string.IsNullOrWhiteSpace(header?.PrototypeId.Label)
            ? $"{objectTypeText} {objectIdText}"
            : $"{objectTypeText} {objectIdText} from {prototypeText}";
        var statusText =
            $"{identity.ResolutionSource} · pool index {identity.PoolIndex?.ToString(CultureInfo.InvariantCulture) ?? "?"}";
        return new MobileRosterEntrySnapshot(
            identity.HandleHex,
            displayText,
            objectTypeText,
            objectIdText,
            prototypeText,
            prototypeHandleText,
            statusText,
            header?.PrototypeId.ProtoNumber ?? header?.ObjectId.ProtoNumber,
            identity.ResolutionSource
        );
    }

    private static TimeSpan ParseTimeout(string? timeoutText)
    {
        if (string.IsNullOrWhiteSpace(timeoutText))
            return TimeSpan.FromSeconds(1);

        if (
            int.TryParse(timeoutText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds)
            && milliseconds > 0
        )
        {
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        throw new InvalidOperationException($"Timeout '{timeoutText}' is not a valid positive millisecond value.");
    }

    private static int ParseValue(string? valueText)
    {
        if (string.IsNullOrWhiteSpace(valueText))
            throw new InvalidOperationException("Enter one integer stat value before writing the mobile stat.");

        if (int.TryParse(valueText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return value;

        throw new InvalidOperationException($"Value '{valueText}' is not a valid signed 32-bit integer.");
    }

    private static int ParseStatId(string? statToken)
    {
        if (string.IsNullOrWhiteSpace(statToken))
            throw new InvalidOperationException(
                "Enter one stat id or stat name such as strength, level, or poison-level."
            );

        var normalized = Normalize(statToken);
        if (StatAliases.TryGetValue(normalized, out var statId))
            return statId;

        for (var index = 0; index < RuntimeStatCount; index++)
        {
            if (Normalize(RuntimeSemanticCatalog.StatName(index)) == normalized)
                return index;
        }

        if (int.TryParse(statToken.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out statId))
        {
            if (statId is >= 0 and < RuntimeStatCount)
                return statId;
        }

        throw new InvalidOperationException(
            $"Unknown stat '{statToken}'. Try names like strength, dexterity, level, alignment, poison-level, magick-points, or a numeric id between 0 and 27."
        );
    }

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

    private const int DefaultMaxEntries = 128;
    private const int RuntimeStatCount = 28;

    private static readonly Dictionary<string, int> StatAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["str"] = 0,
        ["dex"] = 1,
        ["con"] = 2,
        ["beauty"] = 3,
        ["int"] = 4,
        ["per"] = 5,
        ["wil"] = 6,
        ["cha"] = 7,
        ["carry"] = 8,
        ["carryweight"] = 8,
        ["damagebonus"] = 9,
        ["dmgbonus"] = 9,
        ["ac"] = 10,
        ["acadjustment"] = 10,
        ["acadj"] = 10,
        ["speed"] = 11,
        ["healrate"] = 12,
        ["poisonrecovery"] = 13,
        ["reactionmodifier"] = 14,
        ["reactionmod"] = 14,
        ["maxfollowers"] = 15,
        ["followers"] = 15,
        ["magicktechaptitude"] = 16,
        ["magickaptitude"] = 16,
        ["techaptitude"] = 16,
        ["level"] = 17,
        ["xp"] = 18,
        ["experience"] = 18,
        ["experiencepoints"] = 18,
        ["alignment"] = 19,
        ["align"] = 19,
        ["fate"] = 20,
        ["fatepoints"] = 20,
        ["skillpoints"] = 21,
        ["unspent"] = 21,
        ["unspentpoints"] = 21,
        ["mp"] = 22,
        ["magick"] = 22,
        ["magickpoints"] = 22,
        ["tp"] = 23,
        ["tech"] = 23,
        ["techpoints"] = 23,
        ["poison"] = 24,
        ["poisonlevel"] = 24,
        ["age"] = 25,
        ["gender"] = 26,
        ["race"] = 27,
    };
}
