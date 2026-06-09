namespace ArcNET.Diagnostics;

public sealed record SaveBinaryHexRowSnapshot(
    int AbsoluteOffset,
    string BeforeHex,
    string BeforeAscii,
    string AfterHex,
    string AfterAscii
);
