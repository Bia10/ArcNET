namespace ArcNET.Diagnostics;

public sealed record class GuidedActionRequest(
    AttachedSessionSnapshot Session,
    string ActionKey,
    string TravelerToken,
    string TileXText,
    string TileYText,
    string MapIdText,
    string FlagsText,
    string TimeoutMillisecondsText
);
