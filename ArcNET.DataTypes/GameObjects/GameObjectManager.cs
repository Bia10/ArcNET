using ArcNET.DataTypes.GameObjects.Classes;
using System.Collections.Generic;

namespace ArcNET.DataTypes.GameObjects;

public class GameObjectManager
{
    public static List<GameObject> ObjectList;
    public static List<Monster> Monsters;
    public static List<NPC> NPCs = new();
    public static List<Unique> Uniques = new();

    public static void Init()
    {
        ObjectList = new List<GameObject>();
        Monsters = new List<Monster>();
        NPCs = new List<NPC>();
        Uniques = new List<Unique>();
    }
}