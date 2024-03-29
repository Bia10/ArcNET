﻿using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using Utils.Console;

namespace ArcNET.DataTypes.GameObjects.Classes;

public class Descriptions
{
    public static Descriptions LoadedDescriptions = new();

    public class DescriptionsEntry
    {
        public int Id;
        public string Name;

        public DescriptionsEntry(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public List<DescriptionsEntry> Entries = new();

    public static string GetNameFromId(int id)
    {
        var entries = LoadedDescriptions.Entries.Where(entry => entry.Id.Equals(id)).ToList();
        if (entries.Count == 0) return $"NOT_FOUND id:{id}";

        string name = entries.First().Name;
        ConsoleExtensions.Log($"Entries found: |{entries.Count}| Name: |{name}|", "info");

        return name;
    }

    public static void InitFromText(IEnumerable<string> textData)
    {
        try
        {
            foreach (string line in textData)
            {
                string noFirstBrace = line.Replace("{", "");
                string[] idName = noFirstBrace.Split("}", 2);
                var id = int.Parse(idName[0]);
                idName[1] = idName[1].Replace("}", "");

                LoadedDescriptions.Entries.Add(new DescriptionsEntry(id, idName[1]));
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            throw;
        }
    }
}