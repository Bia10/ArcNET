using ArcNET.DataTypes.GameObjects.Flags;
using Spectre.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Utils.Console;

namespace ArcNET.DataTypes.GameObjects.Classes
{
    public class NPC : Entity
    {
        public int Portrait;
        public int RetailPriceMultiplier;
        public int SocialClass;

        private static ObjFFlags ParseObjFFlags(string paramValue)
        {
            var flag = (ObjFFlags)0;
            var trimmedFlag = paramValue.TrimStart().TrimEnd();

            if (!((IList)Enum.GetNames(typeof(ObjFFlags))).Contains(trimmedFlag))
                ConsoleExtensions.Log($"Unrecognized ObjFFlags param:|{trimmedFlag}|", "warn");

            foreach (var objFlag in (ObjFFlags[])Enum.GetValues(typeof(ObjFFlags)))
            {
                if (!Enum.GetName(typeof(ObjFFlags), objFlag).Equals(trimmedFlag)) continue;
                //ConsoleExtensions.Log($"Recognized ObjFFlags param:|{trimmedFlag}|", "success");
                flag = objFlag;
            }
            return flag;
        }

        private static ObjFCritterFlags ParseObjFCritterFlags(string paramValue)
        {
            var flag = (ObjFCritterFlags)0;
            var trimmedFlag = paramValue.TrimStart().TrimEnd();

            if (!((IList)Enum.GetNames(typeof(ObjFCritterFlags))).Contains(trimmedFlag))
                ConsoleExtensions.Log($"Unrecognized ObjFCritterFlags param:|{trimmedFlag}|", "warn");

            foreach (var critterFlag in (ObjFCritterFlags[])Enum.GetValues(typeof(ObjFCritterFlags)))
            {
                if (!Enum.GetName(typeof(ObjFCritterFlags), critterFlag).Equals(trimmedFlag)) continue;
                //ConsoleExtensions.Log($"Recognized ObjFCritterFlags param:|{trimmedFlag}|", "success");
                flag = critterFlag;
            }
            return flag;
        }

        private static ObjFCritterFlags2 ParseObjFCritterFlags2(string paramValue)
        {
            var flag = (ObjFCritterFlags2)0;
            var trimmedFlag = paramValue.TrimStart().TrimEnd();

            if (!((IList)Enum.GetNames(typeof(ObjFCritterFlags2))).Contains(trimmedFlag))
                ConsoleExtensions.Log($"Unrecognized ObjFCritterFlags2 param:|{trimmedFlag}|", "warn");

            foreach (var critterFlag2 in (ObjFCritterFlags2[])Enum.GetValues(typeof(ObjFCritterFlags2)))
            {
                if (!Enum.GetName(typeof(ObjFCritterFlags2), critterFlag2).Equals(trimmedFlag)) continue;
                //ConsoleExtensions.Log($"Recognized ObjFCritterFlags2 param:|{trimmedFlag}|", "success");
                flag = critterFlag2;
            }
            return flag;
        }

        private static ObjFNpcFlags ParseObjFNpcFlags(string paramValue)
        {
            var flag = (ObjFNpcFlags)0;
            var trimmedFlag = paramValue.TrimStart().TrimEnd();

            if (!((IList)Enum.GetNames(typeof(ObjFNpcFlags))).Contains(trimmedFlag))
                ConsoleExtensions.Log($"Unrecognized ObjFNpcFlags param:|{trimmedFlag}|", "warn");

            foreach (var npcFlag in (ObjFNpcFlags[])Enum.GetValues(typeof(ObjFNpcFlags)))
            {
                if (!Enum.GetName(typeof(ObjFNpcFlags), npcFlag).Equals(trimmedFlag)) continue;
                //ConsoleExtensions.Log($"Recognized ObjFNpcFlags param:|{trimmedFlag}|", "success");
                flag = npcFlag;
            }
            return flag;
        }

        private static ObjFBlitFlag ParseObjFBlitFlag(string paramValue)
        {
            var flag = (ObjFBlitFlag)0;
            var trimmedFlag = paramValue.TrimStart().TrimEnd();

            if (!((IList)Enum.GetNames(typeof(ObjFBlitFlag))).Contains(trimmedFlag))
                ConsoleExtensions.Log($"Unrecognized ObjFBlitFlag param:|{trimmedFlag}|", "warn");

            foreach (var blitFlags in (ObjFBlitFlag[])Enum.GetValues(typeof(ObjFBlitFlag)))
            {
                if (!Enum.GetName(typeof(ObjFBlitFlag), blitFlags).Equals(trimmedFlag)) continue;
                //ConsoleExtensions.Log($"Recognized ObjFBlitFlag param:|{trimmedFlag}|", "success");
                flag = blitFlags;
            }
            return flag;
        }

        private static ObjFSpellFlags ParseObjFSpellFlags(string paramValue)
        {
            var flag = (ObjFSpellFlags)0;
            var trimmedFlag = paramValue.TrimStart().TrimEnd();

            if (!((IList)Enum.GetNames(typeof(ObjFSpellFlags))).Contains(trimmedFlag))
                ConsoleExtensions.Log($"Unrecognized ObjFSpellFlags param:|{trimmedFlag}|", "warn");

            foreach (var spellFlags in (ObjFSpellFlags[])Enum.GetValues(typeof(ObjFSpellFlags)))
            {
                if (!Enum.GetName(typeof(ObjFSpellFlags), spellFlags).Equals(trimmedFlag)) continue;
                //ConsoleExtensions.Log($"Recognized ObjFSpellFlags param:|{trimmedFlag}|", "success");
                flag = spellFlags;
            }
            return flag;
        }

        private static Tuple<BasicStatType, int> GetBasicStat(string paramValue)
        {
            Tuple<BasicStatType, int> basicStatTuple = null;
            var trimmedStats = paramValue.TrimStart();
            var statAndValue = trimmedStats.Split(" ", 2);
            var statType = statAndValue[0].Trim();

            if (!((IList)Enum.GetNames(typeof(BasicStatType))).Contains(statType))
                ConsoleExtensions.Log($"unrecognized Entity.BasicStatType param:|{statType}|", "warn");

            var statValue = statAndValue[1].Trim();
            //ConsoleExtensions.Log($"statType:|{statType}| value:|{statValue}|", "warn");

            foreach (var basicStatType in (BasicStatType[])Enum.GetValues(typeof(BasicStatType)))
            {
                var enumValueName = Enum.GetName(typeof(BasicStatType), basicStatType);
                if (!enumValueName.Equals(statType))
                {
                    //ConsoleExtensions.Log($"Failed to match enumValueName:|{enumValueName}| vs statType:|{statType}|", "warn");
                    continue;
                }
                basicStatTuple = new Tuple<BasicStatType, int>(basicStatType, int.Parse(statValue));
            }

            return basicStatTuple;
        }

        private static Tuple<ResistanceType, int> GetResistTuple(string paramName, string paramValue)
        {
            var trimmedResist = paramName.TrimStart();
            var resist = trimmedResist.Split(" ", 2);
            var resistTypeStr = resist[0];
            var resistanceTypes = (ResistanceType[])Enum.GetValues(typeof(ResistanceType));

            return (from resistType in resistanceTypes
                    let resistTypeName = Enum.GetName(typeof(ResistanceType), resistType)
                    where resistTypeName.Equals(resistTypeStr)
                    select new Tuple<ResistanceType, int>(resistType, int.Parse(paramValue)))
                    .FirstOrDefault();
        }

        private static Tuple<DamageType, int, int> GetDmgTuple(string paramName, string paramValue)
        {
            var trimmedDmg = paramValue.TrimStart();
            var dmgRange = trimmedDmg.Split(" ", 2);
            var min = int.Parse(dmgRange[0]);
            var max = int.Parse(dmgRange[1]);

            return paramName switch
            {
                "Normal Damage" => new Tuple<DamageType, int, int>(DamageType.Normal, min, max),
                "Fatigue Damage" => new Tuple<DamageType, int, int>(DamageType.Fatigue, min, max),
                "Poison Damage" => new Tuple<DamageType, int, int>(DamageType.Poison, min, max),
                "Electrical Damage" => new Tuple<DamageType, int, int>(DamageType.Electrical, min, max),
                "Fire Damage" => new Tuple<DamageType, int, int>(DamageType.Fire, min, max),
                _ => null
            };
        }

        public static NPC GetFromText(IEnumerable<string> npcText)
        {
            var npc = new NPC
            {
                ObjectFlags = new List<ObjFFlags>(),
                CritterFlags = new List<ObjFCritterFlags>(),
                CritterFlags2 = new List<ObjFCritterFlags2>(),
                NpcFlags = new List<ObjFNpcFlags>(),
                BlitFlags = new List<ObjFBlitFlag>(),
                SpellFlags = new List<ObjFSpellFlags>(),
                BasicStats = new List<Tuple<BasicStatType, int>>(),
                Spells = new List<string>(),
                Scripts = new List<Tuple<int, int, int, int, int, int>>(),
                Resistances = new List<Tuple<ResistanceType, int>>(),
                Damages = new List<Tuple<DamageType, int, int>>()
            };

            foreach (var curLine in npcText)
            {
                if (string.IsNullOrWhiteSpace(curLine)) continue;

                var lines = curLine.Split(":", 2);
                var paramName = lines[0];
                var paramValue = lines[1];

                try
                {
                    switch (paramName)
                    {
                        case "Description" when paramValue.Contains(@"//"):
                            {
                                var idAndName = paramValue.Split(@"//", 2);
                                var monsterId = idAndName[0];
                                var monsterName = idAndName[1];

                                npc.Description = new Tuple<int, string>(int.Parse(monsterId), monsterName);
                                break;
                            }
                        case "Description":
                            break;
                        case "Internal Name" or "internal name":
                            npc.InternalName = int.Parse(paramValue);
                            break;
                        case "Level":
                            npc.Level = int.Parse(paramValue);
                            break;
                        case "Art Number and Palette":
                            var trimmed = paramValue.TrimStart();
                            var artNumberAndPalette = trimmed.Split(" ", 2);
                            var artNumber = artNumberAndPalette[0];
                            var paletteNumber = artNumberAndPalette[1];

                            npc.ArtNumberAndPalette = new Tuple<int, int>(int.Parse(artNumber), int.Parse(paletteNumber));
                            break;
                        case "Scale":
                            npc.Scale = int.Parse(paramValue);
                            break;
                        case "Alignment":
                            npc.Alignment = int.Parse(paramValue);
                            break;
                        case "Object Flag":
                            npc.ObjectFlags.Add(ParseObjFFlags(paramValue));
                            break;
                        case "Critter Flag":
                            npc.CritterFlags.Add(ParseObjFCritterFlags(paramValue));
                            break;
                        case "Critter2 Flag":
                            npc.CritterFlags2.Add(ParseObjFCritterFlags2(paramValue));
                            break;
                        case "NPC Flag":
                            npc.NpcFlags.Add(ParseObjFNpcFlags(paramValue));
                            break;
                        case "Blit Flag":
                            npc.BlitFlags.Add(ParseObjFBlitFlag(paramValue));
                            break;
                        case "Spell Flag":
                            npc.SpellFlags.Add(ParseObjFSpellFlags(paramValue));
                            break;
                        case "Hit Chart":
                            npc.HitChart = int.Parse(paramValue);
                            break;
                        case "Basic Stat" or "basic stat":
                            npc.BasicStats.Add(GetBasicStat(paramValue));
                            break;
                        case "Spell" or "spell":
                            npc.Spells.Add(paramValue);
                            break;
                        case "Script":
                            var trimmedScript = paramValue.TrimStart();
                            var scriptParams = trimmedScript.Split(" ", 6);
                            var paramValues = scriptParams.Select(int.Parse).ToList();

                            npc.Scripts.Add(new Tuple<int, int, int, int, int, int>(paramValues[0], paramValues[1], paramValues[2], paramValues[3], paramValues[4], paramValues[5]));
                            break;
                        case "Faction":
                            npc.Faction = int.Parse(paramValue);
                            break;
                        case "AI Packet":
                            if (paramValue.Contains("//"))
                                paramValue = paramValue.Split("//")[0].Trim();

                            npc.AIPacket = int.Parse(paramValue);
                            break;
                        case "Material":
                            if (paramValue.Contains("//"))
                                paramValue = paramValue.Split("//")[0].Trim();

                            npc.Material = int.Parse(paramValue);
                            break;
                        case "Hit Points":
                            npc.HitPoints = int.Parse(paramValue);
                            break;
                        case "Fatigue":
                            npc.Fatigue = int.Parse(paramValue);
                            break;
                        case "Damage Resistance" or "damage resistance":
                            npc.Resistances.Add(GetResistTuple(paramName, paramValue));
                            break;
                        case "Fire Resistance":
                            npc.Resistances.Add(GetResistTuple(paramName, paramValue));
                            break;
                        case "Electrical Resistance":
                            npc.Resistances.Add(GetResistTuple(paramName, paramValue));
                            break;
                        case "Poison Resistance":
                            npc.Resistances.Add(GetResistTuple(paramName, paramValue));
                            break;
                        case "Magic Resistance":
                            npc.Resistances.Add(GetResistTuple(paramName, paramValue));
                            break;
                        case "Normal Damage":
                            npc.Damages.Add(GetDmgTuple(paramName, paramValue));
                            break;
                        case "Fatigue Damage":
                            npc.Damages.Add(GetDmgTuple(paramName, paramValue));
                            break;
                        case "Poison Damage":
                            npc.Damages.Add(GetDmgTuple(paramName, paramValue));
                            break;
                        case "Electrical Damage":
                            npc.Damages.Add(GetDmgTuple(paramName, paramValue));
                            break;
                        case "Fire Damage":
                            npc.Damages.Add(GetDmgTuple(paramName, paramValue));
                            break;
                        case "Sound Bank" or "sound bank":
                            npc.SoundBank = int.Parse(paramValue);
                            break;
                        case "Portrait":
                            npc.Portrait = int.Parse(paramValue);
                            break;
                        case "Retail Price Multiplier":
                            npc.RetailPriceMultiplier = int.Parse(paramValue);
                            break;
                        case "Social Class":
                            npc.SocialClass = int.Parse(paramValue);
                            break;
                        case "Category":
                            npc.Category = int.Parse(paramValue);
                            break;
                        case "Auto Level Scheme":
                            npc.AutoLevelScheme = int.Parse(paramValue);
                            break;
                        case "Inventory Source":
                            npc.InventorySource = int.Parse(paramValue);
                            break;

                        default:
                            ConsoleExtensions.Log($"unrecognized entity param:|{paramName}|", "error");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                    throw;
                }
            }
            return npc;
        }
    }
}