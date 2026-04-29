using ArcNET.Formats;

namespace ArcNET.Editor;

internal static class SaveAssetRegistry
{
    private static readonly IReadOnlyList<ISaveAssetRegistration> s_registrations =
    [
        SaveAssetRegistration<DataSavFile>.ForFileName(
            "data.sav",
            static (builder, path, value) => builder.DataSavFiles[path] = value,
            static memory => DataSavFormat.ParseMemory(memory),
            static updates => updates?.UpdatedDataSavFiles,
            static value => DataSavFormat.WriteToArray(value),
            static pending => pending.DataSavFiles.PendingOrNull,
            static (builder, values) => builder.UpdatedDataSavFiles = values
        ),
        SaveAssetRegistration<Data2SavFile>.ForFileName(
            "data2.sav",
            static (builder, path, value) => builder.Data2SavFiles[path] = value,
            static memory => Data2SavFormat.ParseMemory(memory),
            static updates => updates?.UpdatedData2SavFiles,
            static value => Data2SavFormat.WriteToArray(value),
            static pending => pending.Data2SavFiles.PendingOrNull,
            static (builder, values) => builder.UpdatedData2SavFiles = values
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
            static value => MobileMdyFormat.WriteToArray(value),
            static pending => pending.MobileMdys.PendingOrNull,
            static (builder, values) => builder.UpdatedMobileMdys = values
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
            static value => MessageFormat.WriteToArray(value),
            static pending => pending.Messages.PendingOrNull,
            static (builder, values) => builder.UpdatedMessages = values
        ),
        SaveAssetRegistration<TownMapFog>.ForFormat(
            FileFormat.TownMapFog,
            static (builder, path, value) => builder.TownMapFogs[path] = value,
            static memory => TownMapFogFormat.ParseMemory(memory),
            static updates => updates?.UpdatedTownMapFogs,
            static value => TownMapFogFormat.WriteToArray(value),
            static pending => pending.TownMapFogs.PendingOrNull,
            static (builder, values) => builder.UpdatedTownMapFogs = values
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
        SaveAssetRegistration<byte[]>.ForRawBytes(
            static updates => updates?.RawFileUpdates,
            static pending => pending.RawFiles.PendingOrNull,
            static (builder, values) => builder.RawFileUpdates = values
        ),
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

    public static bool HasPendingUpdates(PendingGameUpdates pending)
    {
        foreach (var registration in s_registrations)
            if (registration.HasPendingUpdates(pending))
                return true;

        return false;
    }

    public static SaveGameUpdates ToSaveGameUpdates(PendingGameUpdates pending, SaveInfo? updatedInfo)
    {
        var builder = new SaveGameUpdatesBuilder { UpdatedInfo = updatedInfo };

        foreach (var registration in s_registrations)
            registration.ApplyPendingUpdates(pending, builder);

        return builder.Build();
    }

    private interface ISaveAssetRegistration
    {
        bool CanParse(string path, string fileName, FileFormat format);

        void Parse(string path, ReadOnlyMemory<byte> memory, LoadedSaveBuilder builder);

        void ApplyUpdates(SaveGameUpdates? updates, Dictionary<string, byte[]> files);

        bool HasPendingUpdates(PendingGameUpdates pending);

        void ApplyPendingUpdates(PendingGameUpdates pending, SaveGameUpdatesBuilder builder);
    }

    private sealed class SaveAssetRegistration<T> : ISaveAssetRegistration
        where T : class
    {
        private readonly Func<string, string, FileFormat, bool>? _canParse;
        private readonly Action<LoadedSaveBuilder, string, T>? _store;
        private readonly Func<ReadOnlyMemory<byte>, T>? _parse;
        private readonly Func<SaveGameUpdates?, IReadOnlyDictionary<string, T>?> _getUpdates;
        private readonly Func<T, byte[]> _serialize;
        private readonly Func<PendingGameUpdates, IReadOnlyDictionary<string, T>?>? _getPending;
        private readonly Action<SaveGameUpdatesBuilder, IReadOnlyDictionary<string, T>?>? _setPending;

        private SaveAssetRegistration(
            Func<string, string, FileFormat, bool>? canParse,
            Action<LoadedSaveBuilder, string, T>? store,
            Func<ReadOnlyMemory<byte>, T>? parse,
            Func<SaveGameUpdates?, IReadOnlyDictionary<string, T>?> getUpdates,
            Func<T, byte[]> serialize,
            Func<PendingGameUpdates, IReadOnlyDictionary<string, T>?>? getPending = null,
            Action<SaveGameUpdatesBuilder, IReadOnlyDictionary<string, T>?>? setPending = null
        )
        {
            _canParse = canParse;
            _store = store;
            _parse = parse;
            _getUpdates = getUpdates;
            _serialize = serialize;
            _getPending = getPending;
            _setPending = setPending;
        }

        public static SaveAssetRegistration<T> ForFileName(
            string fileName,
            Action<LoadedSaveBuilder, string, T> store,
            Func<ReadOnlyMemory<byte>, T> parse,
            Func<SaveGameUpdates?, IReadOnlyDictionary<string, T>?> getUpdates,
            Func<T, byte[]> serialize,
            Func<PendingGameUpdates, IReadOnlyDictionary<string, T>?>? getPending = null,
            Action<SaveGameUpdatesBuilder, IReadOnlyDictionary<string, T>?>? setPending = null
        ) =>
            new(
                (_, currentFileName, _) => currentFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase),
                store,
                parse,
                getUpdates,
                serialize,
                getPending,
                setPending
            );

        public static SaveAssetRegistration<T> ForFormat(
            FileFormat format,
            Action<LoadedSaveBuilder, string, T> store,
            Func<ReadOnlyMemory<byte>, T> parse,
            Func<SaveGameUpdates?, IReadOnlyDictionary<string, T>?> getUpdates,
            Func<T, byte[]> serialize,
            Func<PendingGameUpdates, IReadOnlyDictionary<string, T>?>? getPending = null,
            Action<SaveGameUpdatesBuilder, IReadOnlyDictionary<string, T>?>? setPending = null
        ) =>
            new(
                (_, _, currentFormat) => currentFormat == format,
                store,
                parse,
                getUpdates,
                serialize,
                getPending,
                setPending
            );

        public static SaveAssetRegistration<T> ForRawBytes(
            Func<SaveGameUpdates?, IReadOnlyDictionary<string, T>?> getUpdates,
            Func<PendingGameUpdates, IReadOnlyDictionary<string, T>?>? getPending = null,
            Action<SaveGameUpdatesBuilder, IReadOnlyDictionary<string, T>?>? setPending = null
        ) => new(null, null, null, getUpdates, static value => (byte[])(object)value, getPending, setPending);

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

        public bool HasPendingUpdates(PendingGameUpdates pending) => _getPending?.Invoke(pending) is { Count: > 0 };

        public void ApplyPendingUpdates(PendingGameUpdates pending, SaveGameUpdatesBuilder builder)
        {
            if (_getPending is null || _setPending is null)
                return;

            var typedPending = _getPending(pending);
            if (typedPending is not null)
                _setPending(builder, typedPending);
        }
    }

    private sealed class SaveGameUpdatesBuilder
    {
        public SaveInfo? UpdatedInfo { get; init; }

        public IReadOnlyDictionary<string, MobData>? UpdatedMobiles { get; set; }

        public IReadOnlyDictionary<string, Sector>? UpdatedSectors { get; set; }

        public IReadOnlyDictionary<string, JmpFile>? UpdatedJumpFiles { get; set; }

        public IReadOnlyDictionary<string, MapProperties>? UpdatedMapProperties { get; set; }

        public IReadOnlyDictionary<string, MesFile>? UpdatedMessages { get; set; }

        public IReadOnlyDictionary<string, TownMapFog>? UpdatedTownMapFogs { get; set; }

        public IReadOnlyDictionary<string, DataSavFile>? UpdatedDataSavFiles { get; set; }

        public IReadOnlyDictionary<string, Data2SavFile>? UpdatedData2SavFiles { get; set; }

        public IReadOnlyDictionary<string, ScrFile>? UpdatedScripts { get; set; }

        public IReadOnlyDictionary<string, DlgFile>? UpdatedDialogs { get; set; }

        public IReadOnlyDictionary<string, MobileMdFile>? UpdatedMobileMds { get; set; }

        public IReadOnlyDictionary<string, MobileMdyFile>? UpdatedMobileMdys { get; set; }

        public IReadOnlyDictionary<string, byte[]>? RawFileUpdates { get; set; }

        public SaveGameUpdates Build() =>
            new()
            {
                UpdatedInfo = UpdatedInfo,
                UpdatedMobiles = UpdatedMobiles,
                UpdatedSectors = UpdatedSectors,
                UpdatedJumpFiles = UpdatedJumpFiles,
                UpdatedMapProperties = UpdatedMapProperties,
                UpdatedMessages = UpdatedMessages,
                UpdatedTownMapFogs = UpdatedTownMapFogs,
                UpdatedDataSavFiles = UpdatedDataSavFiles,
                UpdatedData2SavFiles = UpdatedData2SavFiles,
                UpdatedScripts = UpdatedScripts,
                UpdatedDialogs = UpdatedDialogs,
                UpdatedMobileMds = UpdatedMobileMds,
                UpdatedMobileMdys = UpdatedMobileMdys,
                RawFileUpdates = RawFileUpdates,
            };
    }
}
