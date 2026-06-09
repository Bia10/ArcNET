using ArcNET.Editor;

namespace ArcNET.Diagnostics;

public sealed record SaveSlotLoadSnapshot(int Slot, string SlotStem, LoadedSave Save);
