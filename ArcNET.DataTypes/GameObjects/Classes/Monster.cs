using System;
using ArcNET.DataTypes.GameObjects.Flags;

namespace ArcNET.DataTypes.GameObjects.Classes
{
    public class Monster //TODO: abstract Entity class
    {
        public enum BasicStats
        {
            Strength,
            Dexterity,
            Constitution,
            Beauty,
            Intelligence,
            Willpower,
            Charisma,
            Perception,
            TechPoints,
            MagickPoints
        }

        //TODO: some variables could be compacted later
        public Tuple<int, string> Description;
        //public int InternalName; belongs to Entity not monsters.
        public int Level;
        public Tuple<int, int> ArtNumberAndPalette;
        public int Scale;
        public int Alignment;
        public ObjFCritterFlags CritterFlag1; 
        public ObjFCritterFlags CritterFlag2;
        public ObjFCritterFlags CritterFlag3;
        public ObjFCritterFlags CritterFlag4;
        public ObjFCritterFlags2 CritterFlag2_1;
        public ObjFCritterFlags2 CritterFlag2_2;
        public ObjFCritterFlags2 CritterFlag2_3;
        public ObjFCritterFlags2 CritterFlag2_4;
        public ObjFNpcFlags NpcFlag1;
        public ObjFNpcFlags NpcFlag2;
        public ObjFNpcFlags NpcFlag3;
        public ObjFBlitFlag BlitFlag1;
        public ObjFSpellFlags SpellFlag1;
        public int HitChart;
        public Tuple<BasicStats, int> Strength;
        public Tuple<BasicStats, int> Dexterity;
        public Tuple<BasicStats, int> Constitution;
        public Tuple<BasicStats, int> Beauty;
        public Tuple<BasicStats, int> Intelligence;
        public Tuple<BasicStats, int> Willpower; 
        public Tuple<BasicStats, int> Charisma; 
        public Tuple<BasicStats, int> Perception;
        public Tuple<BasicStats, int> TechPoints;
        public Tuple<BasicStats, int> MagickPoints;
        public Tuple<int, int, int, int, int, int> Script1;
        public Tuple<int, int, int, int, int, int> Script2;
        public Tuple<int, int, int, int, int, int> Script3;
        public Tuple<int, int, int, int, int, int> Script4;
        public Tuple<int, int, int, int, int, int> Script5;
        public Tuple<int, int, int, int, int, int> Script6;
        public int Faction;
        public int AIPacket;
        public int Material;
        public int HitPoints;
        public int DamageResistance;
        public int FireResistance;
        public int ElectricalResistance;
        public int PoisonResistance;
        public int MagicResistance;
        public Tuple<int, int> NormalDamage;
        public Tuple<int, int> FatigueDamage;
        public Tuple<int, int> PoisonDamage;
        public Tuple<int, int> ElectricalDamage;
        public Tuple<int, int> FireDamage;
        public int SoundBank;
        public int Category;
        public int InventorySource;
    }
}