namespace ArcNET.Formats;

/// <summary>
/// One decoded entry from the verified alternating <c>[state, 50000+ id]</c> table
/// inside <c>data2.sav</c>.
/// </summary>
public readonly record struct Data2SavIdPairEntry(int Id, int Value);
