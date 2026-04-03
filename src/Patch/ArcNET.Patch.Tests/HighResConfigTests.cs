using ArcNET.Patch;

namespace ArcNET.Patch.Tests;

public class HighResConfigTests
{
    [Test]
    public async Task ParseFile_ValidIni_ParsesCorrectly()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(
                tempFile,
                [
                    "//Arcanum High Resolution Patch Settings",
                    "Width = 1920 // full HD",
                    "Height = 1080",
                    "BitDepth = 32",
                    "DialogFont = 1",
                    "LogbookFont = 0",
                    "MenuPosition = 1",
                    "MainMenuArt = 1",
                    "Borders = 1",
                    "Language = 0",
                    "Windowed = 0",
                    "Renderer = 0",
                    "DoubleBuffer = 0",
                    "DDrawWrapper = 0",
                    "DxWrapper = 1",
                    "ShowFPS = 1",
                    "ScrollFPS = 60",
                    "ScrollDist = 30",
                    "PreloadLimit = 60",
                    "BroadcastLimit = 20",
                    "Logos = 0",
                    "Intro = 0",
                ]
            );

            var config = HighResConfig.ParseFile(tempFile);
            await Assert.That(config.Width).IsEqualTo(1920);
            await Assert.That(config.Height).IsEqualTo(1080);
            await Assert.That(config.BitDepth).IsEqualTo(32);
            await Assert.That(config.DxWrapper).IsEqualTo(1);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void ParseFile_NonExistentFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => HighResConfig.ParseFile("/nonexistent/config.ini"));
    }
}
