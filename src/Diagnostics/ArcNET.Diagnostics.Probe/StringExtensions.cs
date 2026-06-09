using ArcNET.Core;
using Bia.ValueBuffers;

namespace Probe;

internal static class StringExtensions
{
    internal static string TruncateAnnotation(this string s) => ValueBufferText.TruncateText(s, 12);
}
