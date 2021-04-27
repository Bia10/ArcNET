using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ArcNET.DataTypes.GameObjects.Classes
{
    public class AutoLevelSchemes
    {
        public static AutoLevelSchemes LoadedAutoLevelSchemes = new();

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum Abbreviations
        {
            //Stats
            st,
            dx,
            cn,
            be,
            @in,
            pe,
            wp,
            ch,
            //Skills
            bow,
            dodge,
            melee,
            throwing,
            backstab,
            pickpocket,
            prowling,
            spottrap,
            gambling,
            haggle,
            heal,
            persuasion,
            repair,
            firearms,
            picklock,
            armtrap,
            //Spells
            conveyance,
            divination,
            air,
            earth,
            fire,
            water,
            force,
            mental,
            meta,
            morph,
            nature,
            necro_evil,
            necro_good,
            phantasm,
            summoning,
            temporal,
            //Tech
            anatomical,
            chemistry,
            electric,
            explosives,
            gun_smithy,
            mechanical,
            smithy,
            therapeutics,
            //Misc
            maxhps,
            maxfatigue
        }

        public class AutoLevelSchemeEntry
        {
            public int Id;
            public string Name;
            public List<Tuple<Abbreviations, int>> Data;

            public AutoLevelSchemeEntry(int id, string name)
            {
                Id = id;
                Name = name;
                Data = new List<Tuple<Abbreviations, int>>();
            }
        }

        public List<AutoLevelSchemeEntry> Entries = new();

        public static void InitFromText(IEnumerable<string> textData)
        {
            try
            {
                foreach (var line in textData)
                {
                    var idNameData = line.Split("}", 2);
                    idNameData[0] = idNameData[0].Replace("{", "");
                    var id = int.Parse(idNameData[0]);

                    var nameAndData = idNameData[1];
                    if (nameAndData.StartsWith("-")) nameAndData.Remove(0);

                    var nameAndDataSplit = nameAndData.Split("{");
                    var name = nameAndDataSplit[0].Trim();
                    var data = nameAndDataSplit[1];

                    var schemeEntry = new AutoLevelSchemeEntry(id, name);

                    data = data.Replace("}", "");

                    //bad data cleanup
                    if (data.EndsWith(",")) data = data.Remove(data.Length - 1, 1);
                    if (data.EndsWith("\t// default level scheme")) data = data.Replace("\t// default level scheme", "");
                    if (data.Contains("  ")) data = data.Replace("  ", " ");

                    var dataArray = data.Split(",");
                    foreach (var dataValueTuple in dataArray)
                    {
                        var abbreviationAndValue = dataValueTuple.TrimStart().Split(" ");
                        var abbreviation = abbreviationAndValue[0];
                        var value = int.Parse(abbreviationAndValue[1]);

                        Tuple<Abbreviations, int> dataTuple = null;
                        foreach (var abrev in (Abbreviations[])Enum.GetValues(typeof(Abbreviations)))
                        {
                            var enumValueName = Enum.GetName(typeof(Abbreviations), abrev);
                            if (enumValueName != null && !enumValueName.Equals(abbreviation)) continue;

                            dataTuple = new Tuple<Abbreviations, int>(abrev, value);
                        }
                        schemeEntry.Data.Add(dataTuple);
                    }
                    LoadedAutoLevelSchemes.Entries.Add(schemeEntry);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                throw;
            }
        }
    }
}