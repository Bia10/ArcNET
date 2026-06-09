namespace ArcNET.Diagnostics.Windows;

public readonly record struct ResolvedCatalogAddress(uint Address, uint? Rva, string Site, string Resolution);
