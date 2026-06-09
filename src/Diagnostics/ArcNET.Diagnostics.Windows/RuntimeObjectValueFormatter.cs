namespace ArcNET.Diagnostics.Windows;

internal static class RuntimeObjectValueFormatter
{
    public static string FormatFieldInt32(int fieldId, int value) =>
        ObjectValueFormatter.FormatFieldInt32(fieldId, value);
}
