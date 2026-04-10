namespace Probe;

internal static class ProbeConfig
{
    private const string SaveDirArg = "--save-dir";

    public static string ResolveSaveDir(string[] args)
    {
        var index = Array.IndexOf(args, SaveDirArg);
        if (index >= 0 && index + 1 < args.Length)
            return args[index + 1];

        var env = Environment.GetEnvironmentVariable("ARCNET_SAVE_DIR");
        if (!string.IsNullOrEmpty(env))
            return env;

        return @"C:\Games\Arcanum\ArcanumCleanUAPnohighres - Copy\modules\Arcanum\Save";
    }

    public static string[] StripSaveDirArg(string[] args)
    {
        var index = Array.IndexOf(args, SaveDirArg);
        if (index < 0)
            return args;

        if (index + 1 >= args.Length)
            return [.. args[..index]];

        return [.. args[..index], .. args[(index + 2)..]];
    }
}
