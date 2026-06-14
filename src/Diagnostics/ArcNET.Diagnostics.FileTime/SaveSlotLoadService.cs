using ArcNET.GameData.SaveGames;

namespace ArcNET.Diagnostics;

public static class SaveSlotLoadService
{
    public static LoadedSave LoadFiles(string gsiPath, string tfaiPath, string tfafPath) =>
        SaveGameLoader.Load(gsiPath, tfaiPath, tfafPath);

    public static SaveSlotLoadSnapshot Load(string saveDir, int slot)
    {
        var slotStem = $"Slot{slot:D4}";
        var gsiPath =
            Directory.GetFiles(saveDir, slotStem + "*.gsi").FirstOrDefault()
            ?? throw new FileNotFoundException(slotStem);

        return new SaveSlotLoadSnapshot(
            slot,
            slotStem,
            LoadFiles(gsiPath, Path.Combine(saveDir, slotStem + ".tfai"), Path.Combine(saveDir, slotStem + ".tfaf"))
        );
    }

    public static SaveSlotLoadSnapshot Load(string saveDir, string slotText)
    {
        var normalized = slotText.PadLeft(4, '0');
        if (!int.TryParse(normalized, out var slot))
            throw new FormatException($"Invalid slot number: {slotText}");

        return Load(saveDir, slot);
    }
}
