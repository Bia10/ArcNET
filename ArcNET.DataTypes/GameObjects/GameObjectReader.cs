using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcNET.DataTypes.Common;

namespace ArcNET.DataTypes.GameObjects
{
    public static class GameObjectReader
    {
        public static GameObject GetGameObject(this BinaryReader reader)
        {
            var gameObject = new GameObject();
            var prototypeGameObject = new GameObject();

            gameObject.Header = GameObjectHeaderReader.Read(reader);

            if (!gameObject.Header.IsPrototype())
            {
                prototypeGameObject = GameObjectManager.ObjectList.Find(x =>
                    x.Header.ObjectId.GetId().CompareTo(gameObject.Header.ProtoId.GetId()) == 0);
            }

            const string pathToTypes = "ArcNET.DataTypes.GameObjects.Types.";
            var gameObjectObjType = Type.GetType(pathToTypes + gameObject.Header.GameObjectType); //TODO: See Key_Ring
            const string pathToCustomReader = "ArcNET.DataTypes.BinaryReaderExtensions";
            var binaryReader = Type.GetType(pathToCustomReader); //TODO: See Key_Ring
            gameObject.Obj = Activator.CreateInstance(gameObjectObjType ?? throw new InvalidOperationException());

            var props = from p in gameObjectObjType.GetProperties()
                where p.CanWrite
                orderby p.PropertyOrder()
                select p;

            var propertyArray = props.ToArray();

            foreach (var propertyInfo in propertyArray)
            {
                var readMethod = binaryReader.GetMethod(propertyInfo.PropertyType.Name.Contains("Tuple")
                    ? "ReadArray"
                    : "Read" + propertyInfo.PropertyType.Name);

                if (readMethod.IsGenericMethod)
                {
                    var genericTypeName = propertyInfo.PropertyType.FullName
                        .Replace("System.Tuple`2[[", "").Split(new[] {','})[0]
                        .Replace("[]", "");
                    readMethod = readMethod.MakeGenericMethod(Type.GetType(genericTypeName) ?? throw new InvalidOperationException());
                }

                var parameters = new List<object>() {reader};
                var bit = (int)Enum.Parse(typeof(Enums.ObjectField), propertyInfo.Name);
                if (gameObject.Header.Bitmap.Get(bit, gameObject.Header.IsPrototype()))
                {
                    propertyInfo.SetValue(gameObject.Obj, readMethod.Invoke(binaryReader, parameters.ToArray()));
                }
                else
                {
                    if (prototypeGameObject == null || prototypeGameObject.Header.GameObjectType.ToString() 
                        != gameObjectObjType.ToString()) continue;

                    var tempType = prototypeGameObject.Obj.GetType();
                    var tempProperty = tempType.GetProperty(propertyInfo.Name);
                    var tempObj = tempProperty?.GetValue(prototypeGameObject.Obj);

                    propertyInfo.SetValue(gameObject.Obj, tempObj);
                }
            }

            var artIds = props
                .Where(item =>
                    item.PropertyType.ToString() == "ArcanumFileFormats.Common.ArtId" &&
                    item.GetValue(gameObject.Obj) != null).Select(item => ((ArtId)item.GetValue(gameObject.Obj))?.Path);

            foreach (var artId in artIds)
            {
                ArtId.ArtIds.Add(artId);
            }

            return gameObject;
        }
    }
}