using System.Globalization;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed class InventoryEditorService(IInventoryEditorBackend backend)
{
    public InventoryEditorSnapshot CreateItem(InventoryCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanEditInventory(request.Session))
            return CreateUnavailableSnapshot(
                "Inventory editor unavailable",
                CreateAvailabilitySummary(request.Session)
            );

        if (!RuntimeSemanticCatalog.LooksLikeObjectHandle(request.PrototypeHandle))
        {
            return CreateUnavailableSnapshot(
                "Invalid prototype handle",
                $"Prototype handle {RuntimeSemanticCatalog.FormatHandle(request.PrototypeHandle)} is not a live handle-shaped runtime value."
            );
        }

        try
        {
            var timeout = ParseTimeout(request.TimeoutMillisecondsText);
            var inventoryLocation = ParseInventoryLocation(request.InventoryLocationText);
            var owner = TargetResolver.Resolve(backend, request.Session, request.OwnerToken, "inventory owner");
            var execution = backend.CreateItem(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                owner.Handle,
                request.PrototypeHandle,
                inventoryLocation,
                timeout
            );
            return new InventoryEditorSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                "Inventory item created",
                $"Inserted {RuntimeSemanticCatalog.FormatHandle(execution.ItemHandle)} into {owner.TargetText} at {FormatInventoryLocation(inventoryLocation)}.",
                owner.HandleText,
                owner.TargetText,
                RuntimeSemanticCatalog.FormatHandle(execution.ItemHandle),
                RuntimeSemanticCatalog.FormatHandle(request.PrototypeHandle),
                FormatInventoryLocation(inventoryLocation),
                $"{execution.DispatcherMode} · {execution.DispatcherSite}",
                execution.ExecutionDetailText,
                execution.ResultText,
                owner.Notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableSnapshot("Invalid inventory create request", ex.Message);
        }
    }

    public InventoryEditorSnapshot DestroyItem(InventoryDestroyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanEditInventory(request.Session))
            return CreateUnavailableSnapshot(
                "Inventory editor unavailable",
                CreateAvailabilitySummary(request.Session)
            );

        if (string.IsNullOrWhiteSpace(request.ItemHandleToken))
            return CreateUnavailableSnapshot(
                "Item handle required",
                "Enter one runtime item handle before removing it."
            );

        if (TargetResolver.IsPlayerToken(request.ItemHandleToken))
        {
            return CreateUnavailableSnapshot(
                "Item handle required",
                "Destroy expects one explicit item handle, not the player token."
            );
        }

        try
        {
            var timeout = ParseTimeout(request.TimeoutMillisecondsText);
            var item = TargetResolver.Resolve(backend, request.Session, request.ItemHandleToken, "inventory item");
            var execution = backend.DestroyItem(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                item.Handle,
                timeout
            );
            var owner = RuntimeSemanticCatalog.LooksLikeObjectHandle(execution.ParentHandle)
                ? TargetResolver.Resolve(
                    backend,
                    request.Session,
                    RuntimeSemanticCatalog.FormatHandle(execution.ParentHandle),
                    "inventory owner"
                )
                : default(ResolvedTarget);
            List<string> notes = [.. item.Notes];
            if (RuntimeSemanticCatalog.LooksLikeObjectHandle(execution.ParentHandle))
                notes.AddRange(owner.Notes);

            return new InventoryEditorSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                "Inventory item destroyed",
                string.IsNullOrWhiteSpace(owner.TargetText)
                    ? $"Destroyed {item.TargetText}."
                    : $"Destroyed {item.TargetText} after detaching it from {owner.TargetText}.",
                owner.HandleText ?? string.Empty,
                owner.TargetText ?? string.Empty,
                item.HandleText,
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
            return CreateUnavailableSnapshot("Invalid inventory destroy request", ex.Message);
        }
    }

    private static bool CanEditInventory(AttachedSessionSnapshot session) =>
        !session.HasExited
        && session.RuntimeProfile.SupportsCatalogRvas
        && session.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions);

    private static string CreateAvailabilitySummary(AttachedSessionSnapshot session)
    {
        if (session.HasExited)
            return "The attached process has exited, so live inventory edits are unavailable until a new session is attached.";

        return "Inventory edits require a validated runtime profile with live function invocation support.";
    }

    private static InventoryEditorSnapshot CreateUnavailableSnapshot(string status, string summary) =>
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
            "Target address and hook details will appear here after a live inventory mutation.",
            "Mutation result values will appear here after a live inventory mutation.",
            []
        );

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

    private static int ParseInventoryLocation(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return 0;

        var trimmed = token.Trim();
        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericValue))
            return numericValue;

        var normalized = Normalize(trimmed);
        return InventoryLocationAliases.TryGetValue(normalized, out var resolved)
            ? resolved
            : throw new InvalidOperationException(
                $"Unknown inventory slot '{token}'. Use 0 for general inventory or names like weapon, armor, helmet, ring-left, or boots."
            );
    }

    private static string FormatInventoryLocation(int inventoryLocation) =>
        $"{RuntimeSemanticCatalog.InventoryLocationName(inventoryLocation)} ({inventoryLocation.ToString(CultureInfo.InvariantCulture)})";

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

    private static readonly Dictionary<string, int> InventoryLocationAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["inventory"] = 0,
        ["general"] = 0,
        ["bag"] = 0,
        ["backpack"] = 0,
        ["helmet"] = 1000,
        ["ringleft"] = 1001,
        ["leftring"] = 1001,
        ["ringright"] = 1002,
        ["rightring"] = 1002,
        ["medallion"] = 1003,
        ["weapon"] = 1004,
        ["shield"] = 1005,
        ["armor"] = 1006,
        ["gauntlet"] = 1007,
        ["gloves"] = 1007,
        ["boots"] = 1008,
    };
}
