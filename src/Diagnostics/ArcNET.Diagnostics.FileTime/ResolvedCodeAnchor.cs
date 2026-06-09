namespace ArcNET.Diagnostics;

public readonly record struct ResolvedCodeAnchor(CodeAnchor Anchor, uint Delta)
{
    public string DisplayLabel => Delta == 0 ? Anchor.Key : $"{Anchor.Key}+0x{Delta:X}";
}
