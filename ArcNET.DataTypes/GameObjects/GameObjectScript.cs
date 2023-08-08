namespace ArcNET.DataTypes.GameObjects;

public class GameObjectScript
{
    public byte[] Counters;
    public int Flags;
    public int ScriptId;

    public GameObjectScript()
    {
        Counters = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        Flags = 0x00;
        ScriptId = 0x00;
    }

    public GameObjectScript(byte c0, byte c1, byte c2, byte c3, int f0, int id)
    {
        Counters = new[] { c0, c1, c2, c3 };
        Flags = f0;
        ScriptId = id;
    }
}