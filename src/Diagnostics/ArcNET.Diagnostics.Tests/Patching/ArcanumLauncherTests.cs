using ArcNET.Patch;

namespace ArcNET.Patch.Tests;

public class ArcanumLauncherTests
{
    [Test]
    public async Task CreatePlan_WithRendererWindowAndResolution_BuildsExpectedLaunchPlan()
    {
        var gameDir = Directory.CreateTempSubdirectory();
        try
        {
            var executablePath = Path.Combine(gameDir.FullName, ArcanumLauncher.CommunityEditionExecutableName);
            await File.WriteAllTextAsync(executablePath, "stub");

            var plan = ArcanumLauncher.CreatePlan(
                gameDir.FullName,
                new ArcanumLaunchOptions
                {
                    ExecutableKind = ArcanumExecutableKind.CommunityEdition,
                    RenderDriver = SdlRenderDriver.Direct3D12,
                    Windowed = true,
                    Width = 1920,
                    Height = 1080,
                    AdditionalArguments = ["-nologo"],
                }
            );

            await Assert.That(plan.ExecutableKind).IsEqualTo(ArcanumExecutableKind.CommunityEdition);
            await Assert.That(plan.ExecutablePath).IsEqualTo(executablePath);
            await Assert.That(plan.WorkingDirectory).IsEqualTo(gameDir.FullName);
            await Assert.That(plan.Arguments.Count).IsEqualTo(4);
            await Assert.That(plan.Arguments[0]).IsEqualTo("-window");
            await Assert.That(plan.Arguments[1]).IsEqualTo("-geometry");
            await Assert.That(plan.Arguments[2]).IsEqualTo("1920x1080");
            await Assert.That(plan.Arguments[3]).IsEqualTo("-nologo");
            await Assert
                .That(plan.EnvironmentVariables[ArcanumLauncher.SdlRenderDriverEnvironmentVariable])
                .IsEqualTo("direct3d12");
        }
        finally
        {
            gameDir.Delete(recursive: true);
        }
    }

    [Test]
    public async Task CreateStartInfo_AppliesArgumentListAndEnvironmentVariables()
    {
        var plan = new ArcanumLaunchPlan(
            ArcanumExecutableKind.CommunityEdition,
            @"C:\Games\Arcanum\Arcanum.exe",
            @"C:\Games\Arcanum",
            ["-window", "-geometry", "1280x720"],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ArcanumLauncher.SdlRenderDriverEnvironmentVariable] = "software",
            }
        );

        var startInfo = ArcanumLauncher.CreateStartInfo(plan);

        await Assert.That(startInfo.FileName).IsEqualTo(plan.ExecutablePath);
        await Assert.That(startInfo.WorkingDirectory).IsEqualTo(plan.WorkingDirectory);
        await Assert.That(startInfo.UseShellExecute).IsFalse();
        await Assert.That(startInfo.ArgumentList.Count).IsEqualTo(3);
        await Assert.That(startInfo.ArgumentList[0]).IsEqualTo("-window");
        await Assert.That(startInfo.ArgumentList[1]).IsEqualTo("-geometry");
        await Assert.That(startInfo.ArgumentList[2]).IsEqualTo("1280x720");
        await Assert
            .That(startInfo.EnvironmentVariables[ArcanumLauncher.SdlRenderDriverEnvironmentVariable])
            .IsEqualTo("software");
    }

    [Test]
    public async Task CreatePlan_WithPartialResolution_Throws()
    {
        var gameDir = Directory.CreateTempSubdirectory();
        try
        {
            var executablePath = Path.Combine(gameDir.FullName, ArcanumLauncher.CommunityEditionExecutableName);
            await File.WriteAllTextAsync(executablePath, "stub");

            await Assert
                .That(() => ArcanumLauncher.CreatePlan(gameDir.FullName, new ArcanumLaunchOptions { Width = 1600 }))
                .Throws<InvalidOperationException>();
        }
        finally
        {
            gameDir.Delete(recursive: true);
        }
    }

    [Test]
    public async Task CreatePlan_WithoutCommunityEditionOverrides_PrefersClassicExecutable()
    {
        var gameDir = Directory.CreateTempSubdirectory();
        try
        {
            var classicExecutablePath = Path.Combine(gameDir.FullName, ArcanumLauncher.ClassicExecutableName);
            var ceExecutablePath = Path.Combine(gameDir.FullName, ArcanumLauncher.CommunityEditionExecutableName);
            await File.WriteAllTextAsync(classicExecutablePath, "classic");
            await File.WriteAllTextAsync(ceExecutablePath, "ce");

            var plan = ArcanumLauncher.CreatePlan(gameDir.FullName);

            await Assert.That(plan.ExecutableKind).IsEqualTo(ArcanumExecutableKind.Classic);
            await Assert.That(plan.ExecutablePath).IsEqualTo(classicExecutablePath);
            await Assert.That(plan.Arguments).IsEmpty();
            await Assert.That(plan.EnvironmentVariables).IsEmpty();
        }
        finally
        {
            gameDir.Delete(recursive: true);
        }
    }

    [Test]
    public async Task CreatePlan_WithCommunityEditionOverrideAndMissingCeExecutable_Throws()
    {
        var gameDir = Directory.CreateTempSubdirectory();
        try
        {
            var classicExecutablePath = Path.Combine(gameDir.FullName, ArcanumLauncher.ClassicExecutableName);
            await File.WriteAllTextAsync(classicExecutablePath, "classic");

            await Assert
                .That(() => ArcanumLauncher.CreatePlan(gameDir.FullName, new ArcanumLaunchOptions { Windowed = true }))
                .Throws<FileNotFoundException>();
        }
        finally
        {
            gameDir.Delete(recursive: true);
        }
    }

    [Test]
    public void CreatePlan_WithClassicExecutableAndCommunityEditionOverrides_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ArcanumLauncher.CreatePlan(
                @"C:\Games\Arcanum",
                new ArcanumLaunchOptions
                {
                    ExecutableKind = ArcanumExecutableKind.Classic,
                    RenderDriver = SdlRenderDriver.Direct3D11,
                }
            )
        );
    }
}
