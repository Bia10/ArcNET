namespace ArcNET.DataTypes.Common;

public class PrefixedString
{
    private readonly string _value;

    public PrefixedString()
    {
        _value = string.Empty;
    }

    public PrefixedString(string value)
    {
        _value = value;
    }

    public override string ToString()
        => _value;
}