using System.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Patch;

/// <summary>
/// Builds launch plans and processes for Arcanum-compatible executables.
/// </summary>
public static class ArcanumLauncher
{
    public const string ClassicExecutableName = RuntimeExecutableCatalog.ClassicModuleFileName;
    public const string CommunityEditionExecutableName = RuntimeExecutableCatalog.CommunityEditionModuleFileName;
    public const string CommunityEditionPosixExecutableName =
        RuntimeExecutableCatalog.CommunityEditionPosixModuleFileName;
    public const string SdlRenderDriverEnvironmentVariable = "SDL_RENDER_DRIVER";

    public static ArcanumLaunchPlan CreatePlan(string gamePath, ArcanumLaunchOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gamePath);
        options ??= new ArcanumLaunchOptions();

        ValidateResolution(options);
        ValidateExecutableCompatibility(options);

        var executablePath = ResolveExecutablePath(gamePath, options);
        var executableKind = DetectExecutableKind(executablePath);
        ValidateResolvedExecutableKind(options, executableKind, executablePath);
        var workingDirectory =
            Path.GetDirectoryName(executablePath)
            ?? throw new InvalidOperationException($"Could not determine working directory for '{executablePath}'.");

        List<string> arguments = [];
        if (executableKind == ArcanumExecutableKind.CommunityEdition)
        {
            if (options.Windowed)
                arguments.Add("-window");

            if (options.Width is { } width && options.Height is { } height)
            {
                arguments.Add("-geometry");
                arguments.Add($"{width}x{height}");
            }
        }

        foreach (var argument in options.AdditionalArguments)
        {
            if (!string.IsNullOrWhiteSpace(argument))
                arguments.Add(argument);
        }

        Dictionary<string, string> environmentVariables = [];
        var renderHintValue = options.RenderDriver.ToHintValue();
        if (executableKind == ArcanumExecutableKind.CommunityEdition && renderHintValue is not null)
            environmentVariables[SdlRenderDriverEnvironmentVariable] = renderHintValue;

        return new ArcanumLaunchPlan(
            executableKind,
            executablePath,
            workingDirectory,
            [.. arguments],
            environmentVariables
        );
    }

    public static ProcessStartInfo CreateStartInfo(ArcanumLaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var startInfo = new ProcessStartInfo(plan.ExecutablePath)
        {
            WorkingDirectory = plan.WorkingDirectory,
            UseShellExecute = false,
        };

        foreach (var argument in plan.Arguments)
            startInfo.ArgumentList.Add(argument);

        foreach (var (name, value) in plan.EnvironmentVariables)
            startInfo.EnvironmentVariables[name] = value;

        return startInfo;
    }

    public static Process Launch(ArcanumLaunchPlan plan) =>
        Process.Start(CreateStartInfo(plan))
        ?? throw new InvalidOperationException($"Failed to launch '{plan.ExecutablePath}'.");

    public static Process Launch(string gamePath, ArcanumLaunchOptions? options = null) =>
        Launch(CreatePlan(gamePath, options));

    public static string ResolveExecutablePath(string gamePath) => ResolveExecutablePath(gamePath, null);

    public static string ResolveExecutablePath(string gamePath, ArcanumLaunchOptions? options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gamePath);

        var fullPath = Path.GetFullPath(gamePath);
        if (!Directory.Exists(fullPath))
        {
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Could not find an executable at '{fullPath}'.", fullPath);

            return fullPath;
        }

        var requestedKind = options?.ExecutableKind ?? ArcanumExecutableKind.Auto;
        var requiresCommunityEdition = RequiresCommunityEdition(options);
        string[] candidateNames = requestedKind switch
        {
            ArcanumExecutableKind.Classic => [ClassicExecutableName],
            ArcanumExecutableKind.CommunityEdition =>
            [
                CommunityEditionExecutableName,
                CommunityEditionPosixExecutableName,
            ],
            _ when requiresCommunityEdition => [CommunityEditionExecutableName, CommunityEditionPosixExecutableName],
            _ => [ClassicExecutableName, CommunityEditionExecutableName, CommunityEditionPosixExecutableName],
        };

        foreach (var candidateName in candidateNames)
        {
            var executablePath = Path.Combine(fullPath, candidateName);
            if (File.Exists(executablePath))
                return executablePath;
        }

        var expectedExecutableNames = string.Join("', '", candidateNames);
        throw new FileNotFoundException(
            $"Could not find a compatible executable in '{fullPath}'. Expected one of '{expectedExecutableNames}'.",
            fullPath
        );
    }

    private static void ValidateResolution(ArcanumLaunchOptions options)
    {
        var hasWidth = options.Width is not null;
        var hasHeight = options.Height is not null;
        if (hasWidth != hasHeight)
            throw new InvalidOperationException("Width and height must either both be specified or both be omitted.");

        if (options.Width is <= 0)
            throw new InvalidOperationException("Width must be greater than zero.");

        if (options.Height is <= 0)
            throw new InvalidOperationException("Height must be greater than zero.");
    }

    private static void ValidateExecutableCompatibility(ArcanumLaunchOptions options)
    {
        if (!RequiresCommunityEdition(options))
            return;

        if (options.ExecutableKind == ArcanumExecutableKind.Classic)
        {
            throw new InvalidOperationException(
                "Renderer, windowed, and geometry launch overrides require the Community Edition executable."
            );
        }
    }

    private static void ValidateResolvedExecutableKind(
        ArcanumLaunchOptions options,
        ArcanumExecutableKind resolvedKind,
        string executablePath
    )
    {
        if (options.ExecutableKind == ArcanumExecutableKind.Auto || options.ExecutableKind == resolvedKind)
            return;

        throw new InvalidOperationException(
            $"Requested runtime '{options.ExecutableKind}' does not match resolved executable '{executablePath}'."
        );
    }

    private static bool RequiresCommunityEdition(ArcanumLaunchOptions? options) =>
        options is not null
        && (
            options.RenderDriver != SdlRenderDriver.Auto
            || options.Windowed
            || options.Width is not null
            || options.Height is not null
        );

    private static ArcanumExecutableKind DetectExecutableKind(string executablePath)
    {
        var fileName = Path.GetFileName(executablePath);
        if (fileName.Equals(ClassicExecutableName, StringComparison.OrdinalIgnoreCase))
            return ArcanumExecutableKind.Classic;

        if (
            fileName.Equals(CommunityEditionExecutableName, StringComparison.OrdinalIgnoreCase)
            || fileName.Equals(CommunityEditionPosixExecutableName, StringComparison.OrdinalIgnoreCase)
        )
        {
            return ArcanumExecutableKind.CommunityEdition;
        }

        return ArcanumExecutableKind.Classic;
    }
}
