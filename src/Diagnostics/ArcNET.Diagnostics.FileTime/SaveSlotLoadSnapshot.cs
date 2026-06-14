using ArcNET.GameData.SaveGames;

namespace ArcNET.Diagnostics;

public sealed record SaveSlotLoadSnapshot(int Slot, string SlotStem, LoadedSave Save);
