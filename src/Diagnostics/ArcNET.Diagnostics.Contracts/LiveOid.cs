namespace ArcNET.Diagnostics.Contracts;

public readonly record struct LiveOid(short OidType, int? ProtoNumber, string Guid, string Label, string Text);
