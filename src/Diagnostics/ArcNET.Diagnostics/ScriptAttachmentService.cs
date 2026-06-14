using System.Globalization;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed class ScriptAttachmentService(IScriptAttachmentBackend backend)
{
    public ScriptAttachmentSnapshot Read(ScriptAttachmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!CanReadAttachments(request.Session))
        {
            return CreateUnavailableSnapshot(
                request,
                "Script attachment unavailable",
                "Script attachment diagnostics require a validated runtime profile with live function invocation support.",
                attachmentPoint: null,
                attachmentPointName: string.Empty
            );
        }

        try
        {
            var attachmentPoint = ParseAttachmentPointId(request.AttachmentPointText);
            var target = TargetResolver.Resolve(
                backend,
                request.Session,
                request.HandleToken,
                "script-attachment target"
            );
            var payload = backend.ReadAttachment(
                request.Session.ProcessId,
                request.Session.RuntimeProfile,
                target.Handle,
                attachmentPoint
            );
            return new ScriptAttachmentSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                "Script attachment read completed",
                $"Read attachment point {RuntimeSemanticCatalog.AttachmentPointName(attachmentPoint)} for {target.TargetText}.",
                request.AttachmentPointText,
                attachmentPoint,
                RuntimeSemanticCatalog.AttachmentPointName(attachmentPoint),
                target.HandleText,
                target.TargetText,
                payload.Script,
                payload.NativeRead,
                target.Notes
            );
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            return CreateUnavailableSnapshot(
                request,
                "Invalid script-attachment request",
                ex.Message,
                attachmentPoint: null,
                attachmentPointName: string.Empty
            );
        }
    }

    private static bool CanReadAttachments(AttachedSessionSnapshot session) =>
        !session.HasExited
        && session.RuntimeProfile.SupportsCatalogRvas
        && session.Capabilities.Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions);

    private static int ParseAttachmentPointId(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericAttachmentPoint))
            return numericAttachmentPoint;

        var normalized = Normalize(value);
        for (var index = 0; index < MaxAttachmentPointScan; index++)
        {
            if (Normalize(RuntimeSemanticCatalog.AttachmentPointName(index)) == normalized)
                return index;
        }

        throw new InvalidOperationException(
            $"Unknown attachment point '{value}'. Example values: dialog, heartbeat, use, first-heartbeat, 9."
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

    private static ScriptAttachmentSnapshot CreateUnavailableSnapshot(
        ScriptAttachmentRequest request,
        string status,
        string summary,
        int? attachmentPoint,
        string attachmentPointName
    ) =>
        new(
            DateTimeOffset.UtcNow,
            IsAvailable: false,
            status,
            summary,
            request.AttachmentPointText,
            attachmentPoint,
            attachmentPointName,
            string.Empty,
            string.Empty,
            Script: null,
            NativeRead: null,
            []
        );

    private const int MaxAttachmentPointScan = 128;
}
