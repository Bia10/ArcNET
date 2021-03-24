using System;
using ArcNET.DataTypes.Common;

namespace ArcNET.DataTypes.GameObjects.Types
{
	public partial class Common
	{
		[Order(01)] public ArtId ObjFCurrentAid { get; set; }
		[Order(02)] public BinaryReaderExtensions.Location ObjFLocation { get; set; }
		[Order(03)] public Int32 ObjFOffsetX { get; set; }
		[Order(04)] public Int32 ObjFOffsetY { get; set; }
		[Order(05)] public ArtId ObjFShadow { get; set; } //may be bool/int
		[Order(06)] public Tuple<Int32[], Int32[]> ObjFOverlayFore { get; set; }
		[Order(07)] public Tuple<Int32[], Int32[]> ObjFOverlayBack { get; set; }
		[Order(08)] public Tuple<Int32[], Int32[]> ObjFUnderlay { get; set; }
		[Order(09)] public Int32 ObjFBlitFlags { get; set; }
		[Order(10)] public Color ObjFBlitColor { get; set; }
		[Order(11)] public Int32 ObjFBlitAlpha { get; set; }
		[Order(12)] public Int32 ObjFBlitScale { get; set; }
		[Order(13)] public Int32 ObjFLightFlags { get; set; }
		[Order(14)] public ArtId ObjFLightAid { get; set; }
		[Order(15)] public Color ObjFLightColor { get; set; }
		[Order(16)] public Unknown ObjFOverlayLightFlags { get; set; }
		[Order(17)] public Tuple<Int32[], Int32[]> ObjFOverlayLightAid { get; set; }
		[Order(18)] public Unknown ObjFOverlayLightColor { get; set; }
		[Order(19)] public Int32 ObjFFlags { get; set; }
		[Order(20)] public Int32 ObjFSpellFlags { get; set; }
		[Order(21)] public Int32 ObjFBlockingMask { get; set; }
		[Order(22)] public Int32 ObjFName { get; set; }
		[Order(23)] public Int32 ObjFDescription { get; set; }
		[Order(24)] public ArtId ObjFAid { get; set; }
		[Order(25)] public ArtId ObjFDestroyedAid { get; set; }
		[Order(26)] public Int32 ObjFAc { get; set; }
		[Order(27)] public Int32 ObjFHpPts { get; set; }
		[Order(28)] public Int32 ObjFHpAdj { get; set; }
		[Order(29)] public Int32 ObjFHpDamage { get; set; }
		[Order(30)] public Int32 ObjFMaterial { get; set; }
		[Order(31)] public Tuple<Int32[], Int32[]> ObjFResistanceIdx { get; set; }
		[Order(32)] public Tuple<GameObjectScript[], Int32[]> ObjFScriptsIdx { get; set; }
		[Order(33)] public Int32 ObjFSoundEffect { get; set; }
		[Order(34)] public Int32 ObjFCategory { get; set; }
		[Order(35)] public Unknown ObjFPadIas1 { get; set; }
		[Order(36)] public Unknown ObjFPadI64As1 { get; set; }
	}
}