using Newtonsoft.Json;
using Spectre.Console;
using System;
using System.IO;

namespace ArcNET.Utilities
{
    public static class FileWriter
    {
        public static void ToJson<T>(string filePath, T objectToWrite, bool append = false) 
            where T : new()
        {
            if (string.IsNullOrEmpty(filePath))
                return;
            if (objectToWrite == null)
                return;
            if (!filePath.EndsWith(".json"))
                filePath += ".json";

            TextWriter writer = null;
            try
            {
                var contentsToWriteToFile = JsonConvert.SerializeObject(objectToWrite);
                writer = new StreamWriter(filePath, append);
                writer.Write(contentsToWriteToFile);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                throw;
            }
            finally
            {
                writer?.Close();
            }
        }

        public static void ToJson(string filePath, string serializedData, bool append = false)
        {
            if (string.IsNullOrEmpty(filePath))
                return;
            if (string.IsNullOrEmpty(serializedData))
                return;
            if (!filePath.EndsWith(".json"))
                filePath += ".json";

            TextWriter writer = null;
            try
            {
                writer = new StreamWriter(filePath, append);
                writer.Write(serializedData);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                throw;
            }
            finally
            {
                writer?.Close();
            }
        }
    }
}