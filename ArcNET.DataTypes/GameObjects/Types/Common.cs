using System;
using ArcNET.DataTypes.Common;

namespace ArcNET.DataTypes.GameObjects.Types
{
	public partial class Common
	{
		[Order(01)] public ArtId ObjFCurrentAid { get; set; }
		[Order(02)] public Location ObjFLocation { get; set; }
		[Order(03)] public int ObjFOffsetX { get; set; }
		[Order(04)] public int ObjFOffsetY { get; set; }
		[Order(05)] public ArtId ObjFShadow { get; set; } //may be bool/int
		[Order(06)] public Tuple<int[], int[]> ObjFOverlayFore { get; set; }
		[Order(07)] public Tuple<int[], int[]> ObjFOverlayBack { get; set; }
		[Order(08)] public Tuple<int[], int[]> ObjFUnderlay { get; set; }
		[Order(09)] public int ObjFBlitFlags { get; set; }
		[Order(10)] public Color ObjFBlitColor { get; set; }
		[Order(11)] public int ObjFBlitAlpha { get; set; }
		[Order(12)] public int ObjFBlitScale { get; set; }
		[Order(13)] public int ObjFLightFlags { get; set; }
		[Order(14)] public ArtId ObjFLightAid { get; set; }
		[Order(15)] public Color ObjFLightColor { get; set; }
		[Order(16)] public Unknown ObjFOverlayLightFlags { get; set; }
		[Order(17)] public Tuple<int[], int[]> ObjFOverlayLightAid { get; set; }
		[Order(18)] public Unknown ObjFOverlayLightColor { get; set; }
		[Order(19)] public int ObjFFlags { get; set; }
		[Order(20)] public int ObjFSpellFlags { get; set; }
		[Order(21)] public int ObjFBlockingMask { get; set; }
		[Order(22)] public int ObjFName { get; set; }
		[Order(23)] public int ObjFDescription { get; set; }
		[Order(24)] public ArtId ObjFAid { get; set; }
		[Order(25)] public ArtId ObjFDestroyedAid { get; set; }
		[Order(26)] public int ObjFAc { get; set; }
		[Order(27)] public int ObjFHpPts { get; set; }
		[Order(28)] public int ObjFHpAdj { get; set; }
		[Order(29)] public int ObjFHpDamage { get; set; }
		[Order(30)] public int ObjFMaterial { get; set; }
		[Order(31)] public Tuple<int[], int[]> ObjFResistanceIdx { get; set; }
		[Order(32)] public Tuple<GameObjectScript[], int[]> ObjFScriptsIdx { get; set; }
		[Order(33)] public int ObjFSoundEffect { get; set; }
		[Order(34)] public int ObjFCategory { get; set; }
		[Order(35)] public Unknown ObjFPadIas1 { get; set; }
		[Order(36)] public Unknown ObjFPadI64As1 { get; set; }
	}
}