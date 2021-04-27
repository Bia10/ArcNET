using ArcNET.DataTypes.GameObjects.Classes;
using System.Collections.Generic;

namespace ArcNET.DataTypes.GameObjects
{
    public class GameObjectManager
    {
        //Lists
        public static List<GameObject> ObjectList;
        public static List<Monster> Monsters;

        public static void Init()
        {
            ObjectList = new List<GameObject>();
            Monsters = new List<Monster>();
        }
    }
}