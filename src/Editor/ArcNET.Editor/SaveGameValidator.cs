using ArcNET.Formats;
using ArcNET.GameObjects;
using static ArcNET.Editor.SaveValidationIssue;

namespace ArcNET.Editor;

/// <summary>
/// A single validation finding produced by <see cref="SaveGameValidator"/>.
/// </summary>
public sealed record SaveValidationIssue
{
    /// <summary>Severity of the finding.</summary>
    public required SaveValidationSeverity Severity { get; init; }

    /// <summary>
    /// Virtual path of the embedded file the issue relates to,
    /// or <see langword="null"/> for top-level save-slot issues.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>Human-readable description of the problem.</summary>
    public required string Message { get; init; }

    /// <inheritdoc/>
    public override string ToString() =>
        FilePath is null ? $"[{Severity}] {Message}" : $"[{Severity}] {FilePath}: {Message}";

    // ── Severity-typed factories ──────────────────────────────────────────────

    internal static SaveValidationIssue Error(string? path, string message) =>
        new()
        {
            Severity = SaveValidationSeverity.Error,
            FilePath = path,
            Message = message,
        };

    internal static SaveValidationIssue Warning(string? path, string message) =>
        new()
        {
            Severity = SaveValidationSeverity.Warning,
            FilePath = path,
            Message = message,
        };

    internal static SaveValidationIssue Info(string? path, string message) =>
        new()
        {
            Severity = SaveValidationSeverity.Info,
            FilePath = path,
            Message = message,
        };
}

/// <summary>Severity levels for <see cref="SaveValidationIssue"/>.</summary>
public enum SaveValidationSeverity
{
    /// <summary>Informational — save is still loadable.</summary>
    Info,

    /// <summary>Potential data-loss risk; the game may behave unexpectedly.</summary>
    Warning,

    /// <summary>The save is likely to crash the game or refuse to load.</summary>
    Error,
}

/// <summary>
/// Validates a <see cref="LoadedSave"/> (or individual updated collections) for
/// structural and semantic correctness before writing to disk.
/// <para>
/// Call <see cref="Validate(LoadedSave)"/> to check an entire loaded save, or call
/// the targeted overloads to check only the parts you have changed.
/// </para>
/// </summary>
public static class SaveGameValidator
{
    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the entire <see cref="LoadedSave"/>, including all parsed inner files.
    /// Returns all findings; an empty list means the save is structurally clean.
    /// </summary>
    public static IReadOnlyList<SaveValidationIssue> Validate(LoadedSave save) =>
        CollectIssues(issues =>
        {
            ValidateInfo(save.Info, issues);

            foreach (var (path, md) in save.MobileMds)
                ValidateMobileMd(path, md, issues);

            foreach (var (path, mdy) in save.MobileMdys)
                ValidateMobileMdy(path, mdy, issues);

            foreach (var (path, mob) in save.Mobiles)
                ValidateMob(path, mob, issues);
        });

    /// <summary>
    /// Validates a single <see cref="MobileMdFile"/> that is about to replace the
    /// embedded file at <paramref name="virtualPath"/>.
    /// </summary>
    public static IReadOnlyList<SaveValidationIssue> ValidateMobileMd(string virtualPath, MobileMdFile md) =>
        CollectIssues(issues => ValidateMobileMd(virtualPath, md, issues));

    /// <summary>
    /// Validates a single <see cref="MobileMdyFile"/> that is about to replace the
    /// embedded file at <paramref name="virtualPath"/>.
    /// </summary>
    public static IReadOnlyList<SaveValidationIssue> ValidateMobileMdy(string virtualPath, MobileMdyFile mdy) =>
        CollectIssues(issues => ValidateMobileMdy(virtualPath, mdy, issues));

    /// <summary>
    /// Validates a single <see cref="MobData"/> that is about to replace the
    /// embedded file at <paramref name="virtualPath"/>.
    /// </summary>
    public static IReadOnlyList<SaveValidationIssue> ValidateMob(string virtualPath, MobData mob) =>
        CollectIssues(issues => ValidateMob(virtualPath, mob, issues));

    // ── Internal validators ───────────────────────────────────────────────────

    private static void ValidateInfo(SaveInfo info, List<SaveValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(info.LeaderName))
            issues.Add(Error(null, "SaveInfo.LeaderName is empty — the game will show a blank character name."));

        if (info.LeaderLevel is < 1 or > 100)
            issues.Add(Warning(null, $"SaveInfo.LeaderLevel={info.LeaderLevel} is outside the expected range 1–100."));

        if (info.MapId < 0)
            issues.Add(Error(null, $"SaveInfo.MapId={info.MapId} is negative — invalid map reference."));
    }

    private static void ValidateMobileMd(string path, MobileMdFile md, List<SaveValidationIssue> issues) =>
        ValidateMobRecords(
            path,
            md.Records,
            issues,
            _ => false,
            record => record.MapObjectId.ToString(),
            record => record.Data,
            ordinal => $"record {ordinal}",
            (ordinal, oidLabel) =>
                Warning(
                    path,
                    $"Record {ordinal}: duplicate MapObjectId {oidLabel} — engine may use last occurrence only."
                ),
            (ordinal, oidLabel) =>
                Info(
                    path,
                    $"Record {ordinal} ({oidLabel}): mob body could not be fully decoded; raw bytes will be written back verbatim."
                )
        );

    private static void ValidateMobileMdy(string path, MobileMdyFile mdy, List<SaveValidationIssue> issues) =>
        ValidateMobRecords(
            path,
            mdy.Records,
            issues,
            record => record.IsCharacter,
            record => record.Mob!.Header.ObjectId.ToString(),
            record => record.Mob,
            ordinal => $"object {ordinal}",
            (ordinal, oidLabel) =>
                Warning(
                    path,
                    $"Object {ordinal}: duplicate ObjectId {oidLabel} — engine may load both copies, causing corruption."
                ),
            validateBeforeDuplicateCheck: true
        );

    private static void ValidateMob(string path, MobData mob, List<SaveValidationIssue> issues) =>
        ValidateMobData(path, "root", mob, issues);

    private static IReadOnlyList<SaveValidationIssue> CollectIssues(Action<List<SaveValidationIssue>> validate)
    {
        var issues = new List<SaveValidationIssue>();
        validate(issues);
        return issues;
    }

    private static void ValidateMobRecords<TRecord>(
        string path,
        IReadOnlyList<TRecord> records,
        List<SaveValidationIssue> issues,
        Func<TRecord, bool> shouldSkip,
        Func<TRecord, string> getIdLabel,
        Func<TRecord, MobData?> getMob,
        Func<int, string> getLabel,
        Func<int, string, SaveValidationIssue> createDuplicateIssue,
        Func<int, string, SaveValidationIssue>? createOpaqueRecordIssue = null,
        bool validateBeforeDuplicateCheck = false
    )
    {
        var seenIds = new HashSet<string>();
        for (var i = 0; i < records.Count; i++)
        {
            var record = records[i];
            if (shouldSkip(record))
                continue;

            var ordinal = i + 1;
            var idLabel = getIdLabel(record);
            var mob = getMob(record);

            if (validateBeforeDuplicateCheck)
                ValidateMobRecord(path, ordinal, idLabel, mob, issues, getLabel, createOpaqueRecordIssue);

            if (!seenIds.Add(idLabel))
                issues.Add(createDuplicateIssue(ordinal, idLabel));

            if (!validateBeforeDuplicateCheck)
                ValidateMobRecord(path, ordinal, idLabel, mob, issues, getLabel, createOpaqueRecordIssue);
        }
    }

    private static void ValidateMobRecord(
        string path,
        int ordinal,
        string idLabel,
        MobData? mob,
        List<SaveValidationIssue> issues,
        Func<int, string> getLabel,
        Func<int, string, SaveValidationIssue>? createOpaqueRecordIssue
    )
    {
        if (mob is null)
        {
            if (createOpaqueRecordIssue is not null)
                issues.Add(createOpaqueRecordIssue(ordinal, idLabel));

            return;
        }

        ValidateMobData(path, getLabel(ordinal), mob, issues);
    }

    private static void ValidateMobData(string path, string label, MobData mob, List<SaveValidationIssue> issues)
    {
        var hdr = mob.Header;

        if (hdr.Version != 0x08 && hdr.Version != 0x77)
            issues.Add(
                Error(path, $"{label}: unrecognised object version 0x{hdr.Version:X2} (expected 0x08 or 0x77).")
            );

        if (!Enum.IsDefined(hdr.GameObjectType))
            issues.Add(
                Warning(
                    path,
                    $"{label}: ObjectType value {(byte)hdr.GameObjectType} is undefined — engine may crash when processing this object."
                )
            );

        // Bitmap length must match the expected size for this ObjectType.
        // Guard: unknown types already produce a Warning above; For() would throw on them.
        if (Enum.IsDefined(hdr.GameObjectType))
        {
            var expectedBitmapLen = ObjectFieldBitmapSize.For(hdr.GameObjectType);
            if (hdr.Bitmap.Length != expectedBitmapLen)
                issues.Add(
                    Error(
                        path,
                        $"{label}: bitmap length {hdr.Bitmap.Length} does not match expected {expectedBitmapLen} bytes for ObjectType.{hdr.GameObjectType}."
                    )
                );
        }

        // Check for parse-error sentinel properties (unknown wire type stopped the reader).
        foreach (var prop in mob.Properties)
        {
            if (prop.ParseNote is not null)
                issues.Add(
                    Warning(
                        path,
                        $"{label}: property {prop.Field} has a parse note ('{prop.ParseNote}') — subsequent fields may be lost."
                    )
                );
        }

        // For PC objects: specific required fields.
        if (hdr.GameObjectType == ObjectType.Pc)
        {
            if (!HasField(mob, ObjectField.ObjFLocation))
                issues.Add(
                    Warning(path, $"{label}: PC object is missing ObjFLocation — character will spawn at tile (0,0).")
                );

            if (!HasField(mob, ObjectField.ObjFHpPts))
                issues.Add(
                    Warning(path, $"{label}: PC object is missing ObjFHpPts — engine will use prototype max HP.")
                );
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool HasField(MobData mob, ObjectField field)
    {
        var props = mob.Properties;
        for (var i = 0; i < props.Count; i++)
            if (props[i].Field == field && props[i].RawBytes.Length > 0)
                return true;
        return false;
    }
}
