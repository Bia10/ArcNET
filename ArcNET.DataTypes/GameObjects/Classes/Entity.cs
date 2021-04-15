using ArcNET.DataTypes.GameObjects.Flags;
using System;
using System.Collections.Generic;

namespace ArcNET.DataTypes.GameObjects.Classes
{
    public class Entity
    {
        public enum BasicStatType
        {
            Gender,
            Race,
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

        public enum DamageType
        {
            Normal,
            Fatigue,
            Poison,
            Electrical,
            Fire,
        }

        public enum ResistanceType
        {
            Damage,
            Fire,
            Electrical,
            Poison,
            Magic,
        }

        public Tuple<int, string> Description;
        public int InternalName;
        public int Level;
        public Tuple<int, int> ArtNumberAndPalette;
        public int Scale;
        public int Alignment;
        public List<ObjFFlags> ObjectFlags;
        public List<ObjFCritterFlags> CritterFlags; //4 critter flags
        public List<ObjFCritterFlags2> CritterFlags2; //4 critter flags2
        public List<ObjFNpcFlags> NpcFlags; //4 npc flags?
        public List<ObjFBlitFlag> BlitFlags; //4 blit flags?
        public List<ObjFSpellFlags> SpellFlags; //4 spell flags?
        public int HitChart;
        public List<Tuple<BasicStatType, int>> BasicStats; // max 12 basic stats
        public List<string> Spells; //max 10??
        public List<Tuple<int, int, int, int, int, int>> Scripts; // max ?? scripts
        public int Faction;
        public int AIPacket;
        public int Material;
        public int HitPoints;
        public int Fatigue;
        public List<Tuple<ResistanceType, int>> Resistances; // max 5 resistances
        public List<Tuple<DamageType, int, int>> Damages; // max ?? dmg types
        public int SoundBank;
        public int Category;
        public int AutoLevelScheme;
        public int InventorySource;
    }
}