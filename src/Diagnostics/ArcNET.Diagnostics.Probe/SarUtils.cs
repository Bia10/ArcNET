using ArcNET.Core;
using ArcNET.Diagnostics;
using Bia.ValueBuffers;

namespace Probe;

internal static class SarUtils
{
    public static string FormatSlotList(IReadOnlyList<int> slots, int maxShow = 12) =>
        CharacterSarDiagnostics.FormatSlotList(slots, maxShow);

    public static string FormatSlotList(IEnumerable<int> slots, int maxShow) =>
        slots is IReadOnlyList<int> list
            ? FormatSlotList(list, maxShow)
            : CharacterSarDiagnostics.FormatSlotList([.. slots], maxShow);
}
