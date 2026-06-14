using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public interface IScriptAttachmentBackend : IHandleBackend
{
    ScriptAttachmentPayload ReadAttachment(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int attachmentPoint
    );
}
