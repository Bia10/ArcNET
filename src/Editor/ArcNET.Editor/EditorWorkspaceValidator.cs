using ArcNET.Core;
using ArcNET.Formats;
using static ArcNET.Editor.EditorWorkspaceValidationIssue;

namespace ArcNET.Editor;

internal sealed class EditorWorkspaceValidator
{
    private const string DescriptionMesAssetPath = "mes/description.mes";
    private const string ProtoNameOverrideAssetPath = "oemes/oname.mes";

    public EditorWorkspaceValidationReport Build(
        IReadOnlyDictionary<int, EditorAssetEntry> protoDefinitionsByNumber,
        IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> scriptDefinitionsById,
        IReadOnlyDictionary<int, IReadOnlyList<EditorScriptDefinition>> scriptDetailsById,
        IReadOnlyDictionary<int, IReadOnlyList<EditorDialogDefinition>> dialogDetailsById,
        IReadOnlyDictionary<int, IReadOnlyList<EditorProtoReference>> protoReferencesByNumber,
        IReadOnlyDictionary<int, IReadOnlyList<EditorScriptReference>> scriptReferencesById,
        IReadOnlySet<int> protoDisplayNameMessageIndices,
        ArcanumInstallationType? installationType
    )
    {
        var issues = new List<EditorWorkspaceValidationIssue>();

        ValidateProtoReferences(issues, protoDefinitionsByNumber, protoReferencesByNumber);
        ValidateProtoDisplayNames(issues, protoDefinitionsByNumber, protoDisplayNameMessageIndices, installationType);
        ValidateScriptReferences(issues, scriptDefinitionsById, scriptReferencesById);
        ValidateScripts(issues, scriptDetailsById);
        ValidateDialogs(issues, dialogDetailsById);

        return issues.Count == 0 ? EditorWorkspaceValidationReport.Empty : new() { Issues = [.. issues] };
    }

    public static bool IsProtoDisplayNameAssetPath(string assetPath) =>
        assetPath.Equals(DescriptionMesAssetPath, StringComparison.OrdinalIgnoreCase)
        || assetPath.Equals(ProtoNameOverrideAssetPath, StringComparison.OrdinalIgnoreCase);

    private static void ValidateProtoReferences(
        List<EditorWorkspaceValidationIssue> issues,
        IReadOnlyDictionary<int, EditorAssetEntry> protoDefinitionsByNumber,
        IReadOnlyDictionary<int, IReadOnlyList<EditorProtoReference>> protoReferencesByNumber
    )
    {
        foreach (var (protoNumber, references) in protoReferencesByNumber.OrderBy(static pair => pair.Key))
        {
            if (protoDefinitionsByNumber.ContainsKey(protoNumber))
                continue;

            foreach (var reference in references)
            {
                issues.Add(
                    Error(
                        reference.Asset.AssetPath,
                        $"References proto {protoNumber} {reference.Count} time(s), but no matching proto asset was indexed."
                    )
                );
            }
        }
    }

    private static void ValidateProtoDisplayNames(
        List<EditorWorkspaceValidationIssue> issues,
        IReadOnlyDictionary<int, EditorAssetEntry> protoDefinitionsByNumber,
        IReadOnlySet<int> protoDisplayNameMessageIndices,
        ArcanumInstallationType? installationType
    )
    {
        if (!installationType.HasValue || protoDisplayNameMessageIndices.Count == 0)
            return;

        foreach (var pair in protoDefinitionsByNumber.OrderBy(static pair => pair.Key))
        {
            if (HasProtoDisplayName(pair.Key, protoDisplayNameMessageIndices, installationType.Value))
                continue;

            issues.Add(
                Warning(
                    pair.Value.AssetPath,
                    $"Proto {pair.Key} has no display-name entry in {DescriptionMesAssetPath} or {ProtoNameOverrideAssetPath} for {FormatProtoDisplayNameLookup(pair.Key, installationType.Value)}."
                )
            );
        }
    }

    private static void ValidateScriptReferences(
        List<EditorWorkspaceValidationIssue> issues,
        IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> scriptDefinitionsById,
        IReadOnlyDictionary<int, IReadOnlyList<EditorScriptReference>> scriptReferencesById
    )
    {
        foreach (var (scriptId, references) in scriptReferencesById.OrderBy(static pair => pair.Key))
        {
            if (scriptDefinitionsById.ContainsKey(scriptId))
                continue;

            foreach (var reference in references)
            {
                issues.Add(
                    Warning(
                        reference.Asset.AssetPath,
                        $"References script {scriptId} {reference.Count} time(s), but no matching script asset was indexed."
                    )
                );
            }
        }
    }

    private static void ValidateScripts(
        List<EditorWorkspaceValidationIssue> issues,
        IReadOnlyDictionary<int, IReadOnlyList<EditorScriptDefinition>> scriptDetailsById
    )
    {
        foreach (var (_, definitions) in scriptDetailsById.OrderBy(static pair => pair.Key))
        {
            foreach (var definition in definitions)
            {
                foreach (var issue in ScriptValidator.Validate(definition))
                {
                    issues.Add(
                        issue.Severity switch
                        {
                            ScriptValidationSeverity.Error => Error(
                                definition.Asset.AssetPath,
                                FormatScriptIssue(definition.ScriptId, issue)
                            ),
                            ScriptValidationSeverity.Warning => Warning(
                                definition.Asset.AssetPath,
                                FormatScriptIssue(definition.ScriptId, issue)
                            ),
                            _ => Info(definition.Asset.AssetPath, FormatScriptIssue(definition.ScriptId, issue)),
                        }
                    );
                }
            }
        }
    }

    private static string FormatScriptIssue(int scriptId, ScriptValidationIssue issue)
    {
        return issue.AttachmentSlotIndex.HasValue
            ? $"Script {scriptId} slot {issue.AttachmentSlotIndex.Value}: {issue.Message}"
            : $"Script {scriptId}: {issue.Message}";
    }

    private static void ValidateDialogs(
        List<EditorWorkspaceValidationIssue> issues,
        IReadOnlyDictionary<int, IReadOnlyList<EditorDialogDefinition>> dialogDetailsById
    )
    {
        foreach (var (_, definitions) in dialogDetailsById.OrderBy(static pair => pair.Key))
        {
            foreach (var definition in definitions)
            {
                foreach (var issue in DialogValidator.Validate(definition))
                {
                    issues.Add(
                        issue.Severity switch
                        {
                            DialogValidationSeverity.Error => Error(
                                definition.Asset.AssetPath,
                                FormatDialogIssue(definition.DialogId, issue)
                            ),
                            DialogValidationSeverity.Warning => Warning(
                                definition.Asset.AssetPath,
                                FormatDialogIssue(definition.DialogId, issue)
                            ),
                            _ => Info(definition.Asset.AssetPath, FormatDialogIssue(definition.DialogId, issue)),
                        }
                    );
                }
            }
        }
    }

    private static string FormatDialogIssue(int dialogId, DialogValidationIssue issue)
    {
        return issue.EntryNumber.HasValue
            ? $"Dialog {dialogId} entry {issue.EntryNumber.Value}: {issue.Message}"
            : $"Dialog {dialogId}: {issue.Message}";
    }

    private static bool HasProtoDisplayName(
        int protoNumber,
        IReadOnlySet<int> protoDisplayNameMessageIndices,
        ArcanumInstallationType installationType
    )
    {
        var translatedKey = ArcanumInstallation.ToVanillaProtoId(protoNumber, installationType);
        if (translatedKey > 0 && protoDisplayNameMessageIndices.Contains(translatedKey))
            return true;

        return translatedKey != protoNumber && protoDisplayNameMessageIndices.Contains(protoNumber);
    }

    private static string FormatProtoDisplayNameLookup(int protoNumber, ArcanumInstallationType installationType)
    {
        var translatedKey = ArcanumInstallation.ToVanillaProtoId(protoNumber, installationType);
        return translatedKey > 0 && translatedKey != protoNumber
            ? $"lookup key {translatedKey} or raw fallback {protoNumber}"
            : $"lookup key {protoNumber}";
    }
}
