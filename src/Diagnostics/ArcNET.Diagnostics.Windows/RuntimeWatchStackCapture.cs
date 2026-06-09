namespace ArcNET.Diagnostics.Windows;

public struct RuntimeWatchStackCapture
{
    public uint D0;
    public uint D1;
    public uint D2;
    public uint D3;
    public uint D4;
    public uint D5;
    public uint D6;
    public uint D7;

    public readonly uint Get(int index) =>
        index switch
        {
            0 => D0,
            1 => D1,
            2 => D2,
            3 => D3,
            4 => D4,
            5 => D5,
            6 => D6,
            7 => D7,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };

    public void Set(int index, uint value)
    {
        switch (index)
        {
            case 0:
                D0 = value;
                break;
            case 1:
                D1 = value;
                break;
            case 2:
                D2 = value;
                break;
            case 3:
                D3 = value;
                break;
            case 4:
                D4 = value;
                break;
            case 5:
                D5 = value;
                break;
            case 6:
                D6 = value;
                break;
            case 7:
                D7 = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
