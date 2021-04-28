using ArcNET.DataTypes.GameObjects.Flags;
using System;
using System.Collections.Generic;
using System.Linq;

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

    public class Wikia
    {
        public static string Header = "{{EntityInfobox";
        public static string[] EntityInfoboxElements = {
            "| image = ",
            "| race = ",
            "| gender = ",
            "| level = ",
            "| hit points = ",
            "| fatigue = ",
            "| alignment = ",
            "| aptitude = ",
            "| faction = ",
            "| st = ",
            "| cn = ",
            "| dx = ",
            "| be = ",
            "| in = ",
            "| wp = ",
            "| ch = ",
            "| normal = ",
            "| fire =  ",
            "| electrical = ",
            "| poison = ",
            "| magic = ",
            "| normalDmg = ",
            "| fireDmg =  ",
            "| electricalDmg =",
            "| poisonDmg = ",
            "| magicDmg =  ",
        };
        public static string Footer = "}}";

        public static string GetEntityInfobox(Entity entity)
        {
            var result = string.Empty;
            result += Header;
            foreach (var infoboxElement in EntityInfoboxElements)
            {
                switch (infoboxElement)
                {
                    case "| image = ":
                        result += infoboxElement;
                        break;
                    case "| race = ":
                        var raceStr = "";
                        var race = entity.BasicStats.Any(stat => stat.Item1 == Entity.BasicStatType.Race);
                        if (race) raceStr = entity.BasicStats.First(stat => stat.Item1 == Entity.BasicStatType.Race).Item2.ToString();

                        result = result + infoboxElement + raceStr;
                        break;
                    case "| gender = ":
                        var genderStr = "";
                        var gender = entity.BasicStats.Any(stat => stat.Item1 == Entity.BasicStatType.Gender);
                        if (gender) genderStr = entity.BasicStats.First(stat => stat.Item1 == Entity.BasicStatType.Gender).Item2.ToString();

                        result = result + infoboxElement + genderStr;
                        break;
                    case "| level = ":
                        result = result + infoboxElement + entity.Level;
                        break;
                    case "| hit points = ":
                        result = result + infoboxElement + entity.HitPoints;
                        break;
                    case "| fatigue = ":
                        result = result + infoboxElement + entity.Fatigue;
                        break;
                    case "| alignment = ":
                        result = result + infoboxElement + entity.Alignment;
                        break;
                    case "| aptitude = ":
                        result += infoboxElement;
                        break;
                    case "| faction = ":
                        result = result + infoboxElement + entity.Faction;
                        break;

                    default:
                        break;
                }
            }
            result += Footer;

            return result;
        }
    }
}