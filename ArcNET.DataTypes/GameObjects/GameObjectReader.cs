using ArcNET.DataTypes.Common;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Utils.Console;

namespace ArcNET.DataTypes.GameObjects;

public static class GameObjectReader
{
    public static GameObject GetGameObject(this BinaryReader reader)
    {
        var gameObject = new GameObject();
        var prototypeGameObject = new GameObject();

        gameObject.Header = GameObjectHeaderReader.Read(reader);

        if (!gameObject.Header.IsPrototype())
            prototypeGameObject = GameObjectManager.ObjectList.Find(x =>
                x.Header.ObjectId.GetId().CompareTo(gameObject.Header.ProtoId.GetId()) == 0);

        const string pathToTypes = "ArcNET.DataTypes.GameObjects.Types.";
        var gameObjectObjType = Type.GetType(pathToTypes + gameObject.Header.GameObjectType);
        const string pathToCustomReader = "ArcNET.DataTypes.BinaryReaderExtensions";
        var binaryReader = Type.GetType(pathToCustomReader);
        gameObject.Obj = Activator.CreateInstance(gameObjectObjType ?? throw new InvalidOperationException());

        IOrderedEnumerable<PropertyInfo> props = from p in gameObjectObjType.GetProperties()
            where p.CanWrite
            orderby p.PropertyOrder()
            select p;

        PropertyInfo[] propertyArray = props.ToArray();

        foreach (PropertyInfo propertyInfo in propertyArray)
        {
            if (binaryReader == null)
                throw new InvalidOperationException("binaryReader is null");

            MethodInfo readMethod = binaryReader.GetMethod(propertyInfo.PropertyType.Name.Contains("Tuple")
                ? "ReadArray"
                : "Read" + propertyInfo.PropertyType.Name);

            if (readMethod is not null && readMethod.IsGenericMethod)
                if (propertyInfo.PropertyType.FullName != null)
                {
                    string genericTypeName = propertyInfo.PropertyType.FullName
                        .Replace("System.Tuple`2[[", "").Split(new[] { ',' })[0]
                        .Replace("[]", "");
                    readMethod = readMethod.MakeGenericMethod(Type.GetType(genericTypeName) ?? throw new InvalidOperationException());
                }

            var parameters = new List<object>
            {
                reader,
            };

            var bit = (int)Enum.Parse(typeof(Enums.ObjectField), propertyInfo.Name);

            foreach (object param in parameters)
                ConsoleExtensions.Log($"Parameters: {param} Bit: {bit}", "debug");

            if (gameObject.Header.Bitmap.Get(bit, gameObject.Header.IsPrototype()))
            {
                if (readMethod is null) continue;

                try
                {
                    propertyInfo.SetValue(gameObject.Obj, readMethod.Invoke(binaryReader, parameters.ToArray()));
                }
                catch (Exception e)
                {
                    AnsiConsole.WriteException(e);
                    throw;
                }
            }
            else
            {
                if (prototypeGameObject == null || prototypeGameObject.Header.GameObjectType.ToString()
                    != gameObjectObjType.ToString()) continue;

                Type tempType = prototypeGameObject.Obj.GetType();
                PropertyInfo tempProperty = tempType.GetProperty(propertyInfo.Name);
                object tempObj = tempProperty?.GetValue(prototypeGameObject.Obj);

                propertyInfo.SetValue(gameObject.Obj, tempObj);
            }
        }

        IEnumerable<string> artIds = props
            .Where(item =>
                item.PropertyType.ToString() == "ArcNET.DataTypes.Common.ArtId" &&
                item.GetValue(gameObject.Obj) != null).Select(item => ((ArtId)item.GetValue(gameObject.Obj))?.Path);

        foreach (string artId in artIds)
            ArtId.ArtIds.Add(artId);

        return gameObject;
    }
}