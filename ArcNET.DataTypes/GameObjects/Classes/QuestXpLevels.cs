using Spectre.Console;
using System;
using System.Collections.Generic;

namespace ArcNET.DataTypes.GameObjects.Classes
{
    public class QuestXpLevels
    {
        public static QuestXpLevels LoadedXpQuestLevels = new();

        public class QuestXpLevelEntry
        {
            public int Level;
            public int Experience;

            public QuestXpLevelEntry(int level, int experience)
            {
                Level = level;
                Experience = experience;
            }
        }

        public List<QuestXpLevelEntry> Entries = new();

        public static void InitFromText(IEnumerable<string> textData)
        {
            try
            {
                foreach (var line in textData)
                {
                    var levelAndXp = line.Split("}", 2);
                    levelAndXp[0] = levelAndXp[0].Replace("{", "");
                    var level = int.Parse(levelAndXp[0]);
                    levelAndXp[1] = levelAndXp[1].Replace("{", "");
                    levelAndXp[1] = levelAndXp[1].Replace("}", "");
                    levelAndXp[1] = levelAndXp[1].TrimEnd();
                    var xp = int.Parse(levelAndXp[1]);

                    LoadedXpQuestLevels.Entries.Add(new QuestXpLevelEntry(level, xp));
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