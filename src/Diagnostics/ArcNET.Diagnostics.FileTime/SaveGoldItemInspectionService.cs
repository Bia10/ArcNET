using System.Buffers.Binary;
using ArcNET.GameData.SaveGames;
using ArcNET.GameObjects;

namespace ArcNET.Diagnostics;

public static class SaveGoldItemInspectionService
{
    public static SaveGoldItemInspectionSnapshot Create(LoadedSave save)
    {
        ArgumentNullException.ThrowIfNull(save);

        var playerCharacterRawBytes = save
            .MobileMdys.SelectMany(static entry => entry.Value.Records)
            .Where(static record => record.IsCharacter)
            .Select(static record => record.Character!.RawBytes)
            .ToList();

        List<SaveGoldItemFileSnapshot> files = [];
        foreach (var (path, mdyFile) in save.MobileMdys.OrderBy(static entry => entry.Key))
        {
            List<SaveGoldItemEntrySnapshot> items = [];
            foreach (
                var goldRecord in mdyFile
                    .Records.Where(static record =>
                        record.IsMob && record.Mob!.Header.GameObjectType == ObjectType.Gold
                    )
                    .Select(static record => record.Mob!)
            )
            {
                var objectIdBytes = new byte[24];
                BinaryPrimitives.WriteInt16LittleEndian(objectIdBytes.AsSpan(0, 2), goldRecord.Header.ObjectId.OidType);
                BinaryPrimitives.WriteInt16LittleEndian(
                    objectIdBytes.AsSpan(2, 2),
                    goldRecord.Header.ObjectId.Padding2
                );
                BinaryPrimitives.WriteInt32LittleEndian(
                    objectIdBytes.AsSpan(4, 4),
                    goldRecord.Header.ObjectId.Padding4
                );
                goldRecord.Header.ObjectId.Id.TryWriteBytes(objectIdBytes.AsSpan(8, 16));

                var quantityProperty = goldRecord.Properties.FirstOrDefault(static property =>
                    (int)(byte)property.Field == 97 && property.RawBytes.Length == 4
                );
                var positiveProperties = goldRecord
                    .Properties.Where(static property => property.RawBytes.Length == 4)
                    .Select(static property => new SaveGoldItemPropertySnapshot(
                        (int)(byte)property.Field,
                        BinaryPrimitives.ReadInt32LittleEndian(property.RawBytes)
                    ))
                    .Where(static property => property.Value > 0)
                    .ToList();

                items.Add(
                    new SaveGoldItemEntrySnapshot(
                        objectIdBytes,
                        quantityProperty is null
                            ? -1
                            : BinaryPrimitives.ReadInt32LittleEndian(quantityProperty.RawBytes),
                        goldRecord.Properties.Any(static property => (int)(byte)property.Field == 65),
                        playerCharacterRawBytes.Any(pc => pc.AsSpan().IndexOf(objectIdBytes) >= 0),
                        positiveProperties
                    )
                );
            }

            if (items.Count == 0)
                continue;

            files.Add(new SaveGoldItemFileSnapshot(path, items));
        }

        return new SaveGoldItemInspectionSnapshot(save.Info.LeaderName, save.Info.LeaderLevel, files);
    }
}
