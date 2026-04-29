using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectNpc : ObjectCritter
{
    private int _npcPadI1Reserved;

    public ObjFNpcFlags NpcFlags { get; internal set; }
    public GameObjectGuid Leader { get; internal set; }
    public int AiData { get; internal set; }
    public GameObjectGuid CombatFocus { get; internal set; }
    public GameObjectGuid WhoHitMeLast { get; internal set; }
    public int ExperienceWorth { get; internal set; }
    public int ExperiencePool { get; internal set; }
    public Location[] Waypoints { get; internal set; } = [];
    public int WaypointCurrent { get; internal set; }
    public Location StandpointDay { get; internal set; }
    public Location StandpointNight { get; internal set; }
    public int Origin { get; internal set; }
    public int Faction { get; internal set; }
    public int RetailPriceMultiplier { get; internal set; }
    public GameObjectGuid SubstituteInventory { get; internal set; }
    public int ReactionBase { get; internal set; }
    public int SocialClass { get; internal set; }
    public int[] ReactionPc { get; internal set; } = [];
    public int[] ReactionLevel { get; internal set; } = [];
    public int[] ReactionTime { get; internal set; } = [];
    public int Wait { get; internal set; }
    public int GeneratorData { get; internal set; }
    public int[] Damage { get; internal set; } = [];
    public int[] HostileList { get; internal set; } = [];

    internal int NpcPadI1Reserved
    {
        get => _npcPadI1Reserved;
        set => _npcPadI1Reserved = value;
    }

    internal static new ObjectNpc Read(ref SpanReader reader, byte[] bitmap, bool isPrototype) =>
        ObjectNpcCodec.Read(ref reader, bitmap, isPrototype);

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype) =>
        ObjectNpcCodec.Write(this, ref writer, bitmap, isPrototype);

    private void WriteNpcFields(ref SpanWriter writer, byte[] bitmap, bool isPrototype) =>
        ObjectNpcCodec.WriteFields(this, ref writer, bitmap, isPrototype);

    private void ReadNpcFields(ref SpanReader reader, byte[] bitmap, bool isPrototype) =>
        ObjectNpcCodec.ReadFields(this, ref reader, bitmap, isPrototype);
}
