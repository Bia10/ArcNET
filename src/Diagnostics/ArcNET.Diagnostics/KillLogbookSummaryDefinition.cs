namespace ArcNET.Diagnostics;

public sealed record class KillLogbookSummaryDefinition(
    LogbookMutationKind MutationKind,
    string Key,
    string SummaryLabel,
    string OperationLabel,
    string CatalogCategoryToken,
    string ValueLabel,
    string ValuePlaceholderText,
    int? DescriptionIndex,
    int ValueIndex
)
{
    public bool RequiresDescription => DescriptionIndex.HasValue;
}
