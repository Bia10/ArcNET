namespace ArcNET.Diagnostics;

public sealed record class LogbookRequest(
    AttachedSessionSnapshot Session,
    string HandleToken,
    string PageToken = "all"
);
