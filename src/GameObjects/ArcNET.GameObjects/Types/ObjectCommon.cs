using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

/// <summary>
/// Common fields shared by every game object type.  Field presence is controlled by the
/// <see cref="GameObjectHeader.Bitmap"/>; absent fields retain their default (zero/null) value.
/// </summary>
public abstract class ObjectCommon
{
    private struct VisualFields
    {
        public VisualFields()
        {
            OverlayFore = [];
            OverlayBack = [];
            Underlay = [];
        }

        public ArtId CurrentAid { get; set; }
        public Location? Location { get; set; }
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
        public ArtId Shadow { get; set; }
        public int[] OverlayFore { get; set; } = [];
        public int[] OverlayBack { get; set; } = [];
        public int[] Underlay { get; set; } = [];
        public int BlitFlags { get; set; }
        public Color BlitColor { get; set; }
        public int BlitAlpha { get; set; }
        public int BlitScale { get; set; }
    }

    private struct LightingFields
    {
        public LightingFields() => OverlayLightAid = [];

        public int LightFlags { get; set; }
        public ArtId LightAid { get; set; }
        public Color LightColor { get; set; }
        public int OverlayLightFlags { get; set; }
        public int[] OverlayLightAid { get; set; } = [];
        public int OverlayLightColor { get; set; }
    }

    private struct CombatFields
    {
        public int Ac { get; set; }
        public int HpPts { get; set; }
        public int HpAdj { get; set; }
        public int HpDamage { get; set; }
    }

    private struct StateFields
    {
        public StateFields()
        {
            ResistanceIdx = [];
            ScriptsIdx = [];
        }

        public int Flags { get; set; }
        public int SpellFlags { get; set; }
        public int BlockingMask { get; set; }
        public int Name { get; set; }
        public int Description { get; set; }
        public ArtId Aid { get; set; }
        public ArtId DestroyedAid { get; set; }
        public int Material { get; set; }
        public int[] ResistanceIdx { get; set; } = [];
        public GameObjectScript[] ScriptsIdx { get; set; } = [];
        public int SoundEffect { get; set; }
        public int Category { get; set; }
    }

    private VisualFields _visual = new();
    private LightingFields _lighting = new();
    private CombatFields _combat = new();
    private StateFields _state = new();
    private int _commonPadIas1Reserved;
    private long _commonPadI64As1Reserved;

    internal int CommonPadIas1Reserved
    {
        get => _commonPadIas1Reserved;
        set => _commonPadIas1Reserved = value;
    }

    internal long CommonPadI64As1Reserved
    {
        get => _commonPadI64As1Reserved;
        set => _commonPadI64As1Reserved = value;
    }

    // ── Visual ───────────────────────────────────────────────────────────────
    public ArtId CurrentAid
    {
        get => _visual.CurrentAid;
        internal set => _visual.CurrentAid = value;
    }
    public Location? Location
    {
        get => _visual.Location;
        internal set => _visual.Location = value;
    }
    public int OffsetX
    {
        get => _visual.OffsetX;
        internal set => _visual.OffsetX = value;
    }
    public int OffsetY
    {
        get => _visual.OffsetY;
        internal set => _visual.OffsetY = value;
    }
    public ArtId Shadow
    {
        get => _visual.Shadow;
        internal set => _visual.Shadow = value;
    }

    // Overlay lists — count + data pairs; exact format TBD during reverse engineering
    public int[] OverlayFore
    {
        get => _visual.OverlayFore;
        internal set => _visual.OverlayFore = value;
    }
    public int[] OverlayBack
    {
        get => _visual.OverlayBack;
        internal set => _visual.OverlayBack = value;
    }
    public int[] Underlay
    {
        get => _visual.Underlay;
        internal set => _visual.Underlay = value;
    }

    // ── Rendering ────────────────────────────────────────────────────────────
    public int BlitFlags
    {
        get => _visual.BlitFlags;
        internal set => _visual.BlitFlags = value;
    }
    public Color BlitColor
    {
        get => _visual.BlitColor;
        internal set => _visual.BlitColor = value;
    }
    public int BlitAlpha
    {
        get => _visual.BlitAlpha;
        internal set => _visual.BlitAlpha = value;
    }
    public int BlitScale
    {
        get => _visual.BlitScale;
        internal set => _visual.BlitScale = value;
    }

    // ── Lighting ─────────────────────────────────────────────────────────────
    public int LightFlags
    {
        get => _lighting.LightFlags;
        internal set => _lighting.LightFlags = value;
    }
    public ArtId LightAid
    {
        get => _lighting.LightAid;
        internal set => _lighting.LightAid = value;
    }
    public Color LightColor
    {
        get => _lighting.LightColor;
        internal set => _lighting.LightColor = value;
    }
    public int OverlayLightFlags
    {
        get => _lighting.OverlayLightFlags;
        internal set => _lighting.OverlayLightFlags = value;
    }
    public int[] OverlayLightAid
    {
        get => _lighting.OverlayLightAid;
        internal set => _lighting.OverlayLightAid = value;
    }
    public int OverlayLightColor
    {
        get => _lighting.OverlayLightColor;
        internal set => _lighting.OverlayLightColor = value;
    }

    // ── State & stats ─────────────────────────────────────────────────────────
    public ObjFFlags ObjectFlags
    {
        get => unchecked((ObjFFlags)(uint)_state.Flags);
        internal set => _state.Flags = unchecked((int)value);
    }
    public ObjFSpellFlags SpellFlags
    {
        get => unchecked((ObjFSpellFlags)(uint)_state.SpellFlags);
        internal set => _state.SpellFlags = unchecked((int)value);
    }
    public int BlockingMask
    {
        get => _state.BlockingMask;
        internal set => _state.BlockingMask = value;
    }
    public int Name
    {
        get => _state.Name;
        internal set => _state.Name = value;
    }
    public int Description
    {
        get => _state.Description;
        internal set => _state.Description = value;
    }
    public ArtId Aid
    {
        get => _state.Aid;
        internal set => _state.Aid = value;
    }
    public ArtId DestroyedAid
    {
        get => _state.DestroyedAid;
        internal set => _state.DestroyedAid = value;
    }
    public int Ac
    {
        get => _combat.Ac;
        internal set => _combat.Ac = value;
    }
    public int HpPts
    {
        get => _combat.HpPts;
        internal set => _combat.HpPts = value;
    }
    public int HpAdj
    {
        get => _combat.HpAdj;
        internal set => _combat.HpAdj = value;
    }
    public int HpDamage
    {
        get => _combat.HpDamage;
        internal set => _combat.HpDamage = value;
    }
    public int Material
    {
        get => _state.Material;
        internal set => _state.Material = value;
    }

    // ── Indexed arrays ────────────────────────────────────────────────────────
    public int[] ResistanceIdx
    {
        get => _state.ResistanceIdx;
        internal set => _state.ResistanceIdx = value;
    }
    public GameObjectScript[] ScriptsIdx
    {
        get => _state.ScriptsIdx;
        internal set => _state.ScriptsIdx = value;
    }

    // ── Misc ──────────────────────────────────────────────────────────────────
    public int SoundEffect
    {
        get => _state.SoundEffect;
        internal set => _state.SoundEffect = value;
    }
    public int Category
    {
        get => _state.Category;
        internal set => _state.Category = value;
    }

    protected void ReadCommonFields(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        ObjectCommonFieldsCodec.Read(this, ref reader, bitmap, isPrototype);
    }

    // Indexed int array: 4-byte count followed by count × 4-byte values.
    internal static int[] ReadIndexedInts(ref SpanReader reader) =>
        ObjectSerializationHelpers.ReadIndexedInts(ref reader);

    internal static GameObjectScript[] ReadScripts(ref SpanReader reader) =>
        ObjectSerializationHelpers.ReadScripts(ref reader);

    protected void WriteCommonFields(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        ObjectCommonFieldsCodec.Write(this, ref writer, bitmap, isPrototype);
    }

    internal static void WriteIndexedInts(ref SpanWriter writer, int[] values) =>
        ObjectSerializationHelpers.WriteIndexedInts(ref writer, values);

    internal static void WriteScripts(ref SpanWriter writer, GameObjectScript[] scripts) =>
        ObjectSerializationHelpers.WriteScripts(ref writer, scripts);

    internal abstract void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype);
}
