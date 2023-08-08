using Spectre.Console;
using System;
using System.Collections.Generic;

namespace ArcNET.DataTypes.GameObjects.Classes;

public class Faction
{
    public static Faction LoadedFactions = new();

    public class FactionEntry
    {
        public int Id;
        public string FactionName;

        public FactionEntry(int id, string factionName)
        {
            Id = id;
            FactionName = factionName;
        }
    }

    public List<FactionEntry> Entries = new();

    public static void InitFromText(IEnumerable<string> textData)
    {
        try
        {
            foreach (string line in textData)
            {
                string[] idAndFaction = line.Split("}", 2);
                idAndFaction[0] = idAndFaction[0].Replace("{", "");
                var id = int.Parse(idAndFaction[0]);
                idAndFaction[1] = idAndFaction[1].Replace("{", "");
                idAndFaction[1] = idAndFaction[1].Replace("}", "");
                idAndFaction[1] = idAndFaction[1].TrimEnd();
                string faction = idAndFaction[1];

                LoadedFactions.Entries.Add(new FactionEntry(id, faction));
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            throw;
        }
    }
}