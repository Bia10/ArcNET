﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcNET.DataTypes.Common;
using Spectre.Console;
using AnsiConsoleExtensions = ArcNET.Utilities.AnsiConsoleExtensions;

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
            var gameObjectObjType = Type.GetType(pathToTypes + gameObject.Header.GameObjectType);
            const string pathToCustomReader = "ArcNET.DataTypes.BinaryReaderExtensions";
            var binaryReader = Type.GetType(pathToCustomReader);
            gameObject.Obj = Activator.CreateInstance(gameObjectObjType ?? throw new InvalidOperationException());

            var props = from p in gameObjectObjType.GetProperties() 
                where p.CanWrite
                orderby p.PropertyOrder()
                select p;

            var propertyArray = props.ToArray();

            foreach (var propertyInfo in propertyArray)
            {
                if (binaryReader == null)
                    throw new InvalidOperationException("binaryReader is null");
                
                var readMethod = binaryReader.GetMethod(propertyInfo.PropertyType.Name.Contains("Tuple")
                    ? "ReadArray"
                    : "Read" + propertyInfo.PropertyType.Name);

                if (readMethod is not null && readMethod.IsGenericMethod)
                {
                    if (propertyInfo.PropertyType.FullName != null)
                    {
                        var genericTypeName = propertyInfo.PropertyType.FullName
                            .Replace("System.Tuple`2[[", "").Split(new[] {','})[0]
                            .Replace("[]", "");
                        readMethod = readMethod.MakeGenericMethod(Type.GetType(genericTypeName) ?? throw new InvalidOperationException());
                    }
                }

                var parameters = new List<object>() {reader};
                var bit = (int)Enum.Parse(typeof(Enums.ObjectField), propertyInfo.Name);

                foreach (var param in parameters)
                {
                    AnsiConsoleExtensions.Log($"Parameters: {param} Bit: {bit}", "debug");
                }

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

                    var tempType = prototypeGameObject.Obj.GetType();
                    var tempProperty = tempType.GetProperty(propertyInfo.Name);
                    var tempObj = tempProperty?.GetValue(prototypeGameObject.Obj);

                    propertyInfo.SetValue(gameObject.Obj, tempObj);
                }
            }

            var artIds = props
                .Where(item =>
                    item.PropertyType.ToString() == "ArcNET.DataTypes.Common.ArtId" &&
                    item.GetValue(gameObject.Obj) != null).Select(item => ((ArtId)item.GetValue(gameObject.Obj))?.Path);

            foreach (var artId in artIds)
            {
                ArtId.ArtIds.Add(artId);
            }

            return gameObject;
        }
    }
}