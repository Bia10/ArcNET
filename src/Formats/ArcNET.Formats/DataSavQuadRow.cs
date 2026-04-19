namespace ArcNET.Formats;

/// <summary>
/// One aligned INT32[4] row from the verified structural surface of <c>data.sav</c>.
/// The fields are intentionally positional because their semantics remain unresolved.
/// </summary>
public readonly record struct DataSavQuadRow(int A, int B, int C, int D);
