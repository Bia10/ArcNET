namespace ArcNET.Formats;

internal static class SaveGameEngineVersionDetector
{
    public static SaveEngineVersion Detect(IReadOnlyList<SaveMapState> maps)
    {
        foreach (var map in maps)
        {
            foreach (var (_, mob) in map.StaticObjects)
                if (mob.Header.Version == 0x77)
                    return SaveEngineVersion.ArcanumCE;

            if (map.StaticDiffs is { } md)
                foreach (var record in md.Records)
                    if (record.Version == 0x77)
                        return SaveEngineVersion.ArcanumCE;

            if (map.DynamicObjects is { } mdy)
                foreach (var record in mdy.Records)
                    if (record.IsMob && record.Mob!.Header.Version == 0x77)
                        return SaveEngineVersion.ArcanumCE;
        }

        return SaveEngineVersion.Vanilla;
    }
}
