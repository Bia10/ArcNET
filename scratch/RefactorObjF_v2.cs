using System;
using System.IO;
using System.Text.RegularExpressions;

var rootPath = @"c:\Users\Bia\source\repos\ArcNET";
var files = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories);

string[] enumsToRename = [
    "ObjFArmorFlags", "ObjFBlitFlags", "ObjFContainerFlags", "ObjFCritterFlags", 
    "ObjFCritterFlags2", "ObjFFlags", "ObjFItemFlags", "ObjFNpcFlags", 
    "ObjFPortalFlags", "ObjFSceneryFlags", "ObjFSpellFlags", "ObjFWeaponFlags",
    "ObjFAmmoFlags", "ObjFGoldFlags", "ObjFFoodFlags", "ObjFScrollFlags",
    "ObjFKeyRingFlags", "ObjFWrittenFlags", "ObjFGenericFlags", "ObjFPcFlags"
];

foreach (var file in files)
{
    if (file.Contains("RefactorObjF")) continue;

    var content = File.ReadAllText(file);
    var originalContent = content;

    // 1. Rename enum types specifically
    foreach (var oldName in enumsToRename)
    {
        var newName = oldName == "ObjFFlags" ? "ObjectFlags" : oldName.Substring(4);
        content = Regex.Replace(content, "\\b" + oldName + "\\b", newName);
    }

    // 2. Rename remaining identifiers starting with ObjF
    // This handles ObjFCurrentAid -> CurrentAid, ObjFLocation -> Location, etc.
    content = Regex.Replace(content, "\\bObjF([A-Z])", "$1");

    if (content != originalContent)
    {
        Console.WriteLine($"Updating {Path.GetRelativePath(rootPath, file)}");
        File.WriteAllText(file, content);
    }
}

Console.WriteLine("Refactoring complete.");
