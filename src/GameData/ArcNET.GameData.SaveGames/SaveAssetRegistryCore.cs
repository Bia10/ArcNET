using ArcNET.Formats;

namespace ArcNET.Editor;

internal static class SaveAssetRegistryCore
{
    private static readonly IReadOnlyList<ISaveAssetRegistration> s_registrations =
    [
        SaveAssetRegistration<DataSavFile>.ForFileName(
            "data.sav",
            static (builder, path, value) => builder.DataSavFiles[path] = value,
            static memory => DataSavFormat.ParseMemory(memory),
            static updates => updates?.UpdatedDataSavFiles,
            static value => DataSavFormat.WriteToArray(value)
        ),
        SaveAssetRegistration<Data2SavFile>.ForFileName(
            "data2.sav",
            static (builder, path, value) => builder.Data2SavFiles[path] = value,
            static memory => Data2SavFormat.ParseMemory(memory),
            static updates => updates?.UpdatedData2SavFiles,
            static value => Data2SavFormat.WriteToArray(value)
        ),
        SaveAssetRegistration<MobileMdFile>.ForFileName(
            "mobile.md",
            static (builder, path, value) => builder.MobileMds[path] = value,
            static memory => MobileMdFormat.ParseMemory(memory),
            static updates => updates?.UpdatedMobileMds,
            static value => MobileMdFormat.WriteToArray(value)
        ),
        SaveAssetRegistration<MobileMdyFile>.ForFileName(
            "mobile.mdy",
            static (builder, path, value) => builder.MobileMdys[path] = value,
            static memory => MobileMdyFormat.ParseMemory(memory),
            static updates => updates?.UpdatedMobileMdys,
            static value => MobileMdyFormat.WriteToArray(value)
        ),
        SaveAssetRegistration<MobData>.ForFormat(
            FileFormat.Mob,
            static (builder, path, value) => builder.Mobiles[path] = value,
            static memory => MobFormat.ParseMemory(memory),
            static updates => updates?.UpdatedMobiles,
            static value => MobFormat.WriteToArray(value)
        ),
        SaveAssetRegistration<Sector>.ForFormat(
            FileFormat.Sector,
            static (builder, path, value) => builder.Sectors[path] = value,
            static memory => SectorFormat.ParseMemory(memory),
            static updates => updates?.UpdatedSectors,
            static value => SectorFormat.WriteToArray(value)
        ),
        SaveAssetRegistration<JmpFile>.ForFormat(
            FileFormat.Jmp,
            static (builder, path, value) => builder.JumpFiles[path] = value,
            static memory => JmpFormat.ParseMemory(memory),
            static updates => updates?.UpdatedJumpFiles,
            static value => JmpFormat.WriteToArray(value)
        ),
        SaveAssetRegistration<MapProperties>.ForFormat(
            FileFormat.MapProperties,
            static (builder, path, value) => builder.MapPropertiesList[path] = value,
            static memory => MapPropertiesFormat.ParseMemory(memory),
            static updates => updates?.UpdatedMapProperties,
            static value => MapPropertiesFormat.WriteToArray(value)
        ),
        SaveAssetRegistration<MesFile>.ForFormat(
            FileFormat.Message,
            static (builder, path, value) => builder.Messages[path] = value,
            static memory => MessageFormat.ParseMemory(memory),
            static updates => updates?.UpdatedMessages,
            static value => MessageFormat.WriteToArray(value)
        ),
        SaveAssetRegistration<TownMapFog>.ForFormat(
            FileFormat.TownMapFog,
            static (builder, path, value) => builder.TownMapFogs[path] = value,
            static memory => TownMapFogFormat.ParseMemory(memory),
            static updates => updates?.UpdatedTownMapFogs,
            static value => TownMapFogFormat.WriteToArray(value)
        ),
        SaveAssetRegistration<ScrFile>.ForFormat(
            FileFormat.Script,
            static (builder, path, value) => builder.Scripts[path] = value,
            static memory => ScriptFormat.ParseMemory(memory),
            static updates => updates?.UpdatedScripts,
            static value => ScriptFormat.WriteToArray(value)
        ),
        SaveAssetRegistration<DlgFile>.ForFormat(
            FileFormat.Dialog,
            static (builder, path, value) => builder.Dialogs[path] = value,
            static memory => DialogFormat.ParseMemory(memory),
            static updates => updates?.UpdatedDialogs,
            static value => DialogFormat.WriteToArray(value)
        ),
        SaveAssetRegistration<byte[]>.ForRawBytes(static updates => updates?.RawFileUpdates),
    ];

    public static bool TryParse(
        string path,
        string fileName,
        FileFormat format,
        ReadOnlyMemory<byte> memory,
        LoadedSaveBuilder builder
    )
    {
        foreach (var registration in s_registrations)
        {
            if (!registration.CanParse(path, fileName, format))
                continue;

            registration.Parse(path, memory, builder);
            return true;
        }

        return false;
    }

    public static void ApplyUpdates(SaveGameUpdates? updates, Dictionary<string, byte[]> files)
    {
        foreach (var registration in s_registrations)
            registration.ApplyUpdates(updates, files);
    }

    private interface ISaveAssetRegistration
    {
        bool CanParse(string path, string fileName, FileFormat format);

        void Parse(string path, ReadOnlyMemory<byte> memory, LoadedSaveBuilder builder);

        void ApplyUpdates(SaveGameUpdates? updates, Dictionary<string, byte[]> files);
    }

    private sealed class SaveAssetRegistration<T> : ISaveAssetRegistration
        where T : class
    {
        private readonly Func<string, string, FileFormat, bool>? _canParse;
        private readonly Action<LoadedSaveBuilder, string, T>? _store;
        private readonly Func<ReadOnlyMemory<byte>, T>? _parse;
        private readonly Func<SaveGameUpdates?, IReadOnlyDictionary<string, T>?> _getUpdates;
        private readonly Func<T, byte[]> _serialize;

        private SaveAssetRegistration(
            Func<string, string, FileFormat, bool>? canParse,
            Action<LoadedSaveBuilder, string, T>? store,
            Func<ReadOnlyMemory<byte>, T>? parse,
            Func<SaveGameUpdates?, IReadOnlyDictionary<string, T>?> getUpdates,
            Func<T, byte[]> serialize
        )
        {
            _canParse = canParse;
            _store = store;
            _parse = parse;
            _getUpdates = getUpdates;
            _serialize = serialize;
        }

        public static SaveAssetRegistration<T> ForFileName(
            string fileName,
            Action<LoadedSaveBuilder, string, T> store,
            Func<ReadOnlyMemory<byte>, T> parse,
            Func<SaveGameUpdates?, IReadOnlyDictionary<string, T>?> getUpdates,
            Func<T, byte[]> serialize
        ) =>
            new(
                (_, currentFileName, _) => currentFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase),
                store,
                parse,
                getUpdates,
                serialize
            );

        public static SaveAssetRegistration<T> ForFormat(
            FileFormat format,
            Action<LoadedSaveBuilder, string, T> store,
            Func<ReadOnlyMemory<byte>, T> parse,
            Func<SaveGameUpdates?, IReadOnlyDictionary<string, T>?> getUpdates,
            Func<T, byte[]> serialize
        ) => new((_, _, currentFormat) => currentFormat == format, store, parse, getUpdates, serialize);

        public static SaveAssetRegistration<T> ForRawBytes(
            Func<SaveGameUpdates?, IReadOnlyDictionary<string, T>?> getUpdates
        ) => new(null, null, null, getUpdates, static value => (byte[])(object)value);

        public bool CanParse(string path, string fileName, FileFormat format) =>
            _canParse?.Invoke(path, fileName, format) == true;

        public void Parse(string path, ReadOnlyMemory<byte> memory, LoadedSaveBuilder builder)
        {
            if (_store is null || _parse is null)
                throw new InvalidOperationException("This asset registration does not support parsing.");

            _store(builder, path, _parse(memory));
        }

        public void ApplyUpdates(SaveGameUpdates? updates, Dictionary<string, byte[]> files)
        {
            var typedUpdates = _getUpdates(updates);
            if (typedUpdates is null)
                return;

            foreach (var (path, item) in typedUpdates)
                files[path] = _serialize(item);
        }
    }
}
