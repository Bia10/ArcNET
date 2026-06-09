using ArcNET.Diagnostics;

namespace ArcNET.Diagnostics.Windows;

internal static class PrototypeResolutionRequestExtensions
{
    public static string TokenOrFallback(this PrototypeResolutionRequest request) =>
        string.IsNullOrWhiteSpace(request.PrototypeText) ? string.Empty : request.PrototypeText.Trim();
}
