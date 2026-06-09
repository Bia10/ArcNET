namespace ArcNET.Diagnostics.Contracts;

public static class RuntimeExecutableCatalog
{
    public const string ClassicProcessName = "Arcanum";
    public const string ClassicModuleFileName = "Arcanum.exe";
    public const string CommunityEditionProcessName = "arcanum-ce";
    public const string CommunityEditionModuleFileName = "arcanum-ce.exe";
    public const string CommunityEditionPosixModuleFileName = "arcanum-ce";

    public static IReadOnlyList<string> DefaultProcessNames => s_defaultProcessNames;

    public static IReadOnlyList<string> DefaultModuleFileNames => s_defaultModuleFileNames;

    public static bool IsClassicModuleFileName(string fileName) =>
        fileName.Equals(ClassicModuleFileName, StringComparison.OrdinalIgnoreCase);

    public static bool IsCommunityEditionModuleFileName(string fileName) =>
        fileName.Equals(CommunityEditionModuleFileName, StringComparison.OrdinalIgnoreCase)
        || fileName.Equals(CommunityEditionPosixModuleFileName, StringComparison.OrdinalIgnoreCase);

    private static readonly string[] s_defaultProcessNames = [ClassicProcessName, CommunityEditionProcessName];

    private static readonly string[] s_defaultModuleFileNames =
    [
        ClassicModuleFileName,
        CommunityEditionModuleFileName,
        CommunityEditionPosixModuleFileName,
    ];
}
