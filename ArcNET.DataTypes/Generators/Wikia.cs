using System.Linq;
using ArcNET.DataTypes.GameObjects.Classes;

namespace ArcNET.DataTypes.Generators;

public class Wikia
{
    public static string Header = "{{EntityInfobox";

    public static string[] EntityInfoboxElements =
    {
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
        string result = string.Empty;
        result += Header;
        foreach (string infoboxElement in EntityInfoboxElements)
            switch (infoboxElement)
            {
                case "| image = ":
                    result += infoboxElement;
                    break;
                case "| race = ":
                    var raceStr = "";
                    bool race = entity.BasicStats.Any(stat => stat.Item1 == Entity.BasicStatType.Race);
                    if (race) raceStr = entity.BasicStats.First(stat => stat.Item1 == Entity.BasicStatType.Race).Item2.ToString();

                    result = result + infoboxElement + raceStr;
                    break;
                case "| gender = ":
                    var genderStr = "";
                    bool gender = entity.BasicStats.Any(stat => stat.Item1 == Entity.BasicStatType.Gender);
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
            }

        result += Footer;
        return result;
    }
}