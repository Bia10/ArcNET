using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

public interface IScriptAttachmentBackend : IHandleBackend
{
    ScriptAttachmentPayload ReadAttachment(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int attachmentPoint
    );
}
