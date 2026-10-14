using System;
using System.Collections.Generic;
using System.Linq;

// Lightweight host for the ACTUAL MirrorRecipeAuthoring source. The production
// compile separately checks these signatures against the game DLLs.
namespace UnityEngine { }
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)]
    public class HarmonyPatch : Attribute { public HarmonyPatch(Type t, string method) { } }
}
public enum ObjectID { None, SolariteWorkbench, OtherWorkbench, ExistingRecipe }
public class InventoryConverter { }
public class EntityMonoBehaviourData
{
    public class Info { public ObjectID objectID; }
    public Info objectInfo = new Info();
}
public class CraftingAuthoring
{
    public struct CraftableObject
    {
        public ObjectID objectID;
        public string moddedObjectID;
        public int amount;
        public bool hasPrerequisites;
    }
    public EntityMonoBehaviourData Data = new EntityMonoBehaviourData();
    public List<CraftableObject> canCraftObjects = new List<CraftableObject>();
    public List<CraftingAuthoring> includeCraftedObjectsFromBuildings;
    public T GetComponent<T>() where T : class => Data as T;
}
namespace MagicMirror
{
    public static class MagicMirrorMod { public const string ObjectName = "MagicMirror_Mirror"; }
}
public static class RecipeAuthoringTests
{
    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        Console.WriteLine("PASS " + label);
    }
    static CraftingAuthoring Bench(ObjectID id)
    {
        var b = new CraftingAuthoring(); b.Data.objectInfo.objectID = id;
        for (int i = 0; i < 18; i++) b.canCraftObjects.Add(new CraftingAuthoring.CraftableObject {
            objectID = i >= 2 && i < 6 ? ObjectID.None : ObjectID.ExistingRecipe,
            amount = 1, hasPrerequisites = i == 6
        });
        return b;
    }
    public static void Main()
    {
        var solarite = Bench(ObjectID.SolariteWorkbench);
        MagicMirror.MirrorRecipeAuthoring.Prefix(solarite);
        Check(solarite.canCraftObjects[2].moddedObjectID == MagicMirror.MagicMirrorMod.ObjectName,
            "recipe is in authoring source before vanilla caches the unfiltered list");
        Check(solarite.canCraftObjects[2].amount == 1 && !solarite.canCraftObjects[2].hasPrerequisites,
            "one mirror with no boss/content prerequisite");
        var master = solarite.canCraftObjects.ToArray();
        var live = new CraftingAuthoring.CraftableObject[18];
        for (int refresh = 0; refresh < 20; refresh++)
        {
            Array.Copy(master, live, master.Length); // periodic game's master-list restore
            Check(live[2].moddedObjectID == MagicMirror.MagicMirrorMod.ObjectName,
                "master-list refresh " + refresh + " preserves mirror");
        }
        MagicMirror.MirrorRecipeAuthoring.Prefix(solarite);
        Check(solarite.canCraftObjects.Count == 18 && solarite.canCraftObjects.Count(x => x.moddedObjectID == MagicMirror.MagicMirrorMod.ObjectName) == 1,
            "conversion across worlds is idempotent without shifting indices");
        Check(solarite.canCraftObjects[6].objectID == ObjectID.ExistingRecipe && solarite.canCraftObjects[6].hasPrerequisites,
            "vanilla gated recipe and its prerequisites are preserved");
        var higher = Bench(ObjectID.OtherWorkbench);
        var inherited = Bench(ObjectID.SolariteWorkbench);
        higher.includeCraftedObjectsFromBuildings = new List<CraftingAuthoring> { null, inherited };
        MagicMirror.MirrorRecipeAuthoring.Prefix(higher);
        Check(inherited.canCraftObjects[2].moddedObjectID == MagicMirror.MagicMirrorMod.ObjectName && higher.canCraftObjects[2].moddedObjectID == null,
            "higher bench inherits Solarite source without modifying its own category");
        var occupied = Bench(ObjectID.SolariteWorkbench);
        occupied.canCraftObjects[2] = new CraftingAuthoring.CraftableObject { moddedObjectID = "OtherMod_Recipe", amount = 2 };
        MagicMirror.MirrorRecipeAuthoring.Prefix(occupied);
        Check(occupied.canCraftObjects[2].moddedObjectID == "OtherMod_Recipe" && occupied.canCraftObjects[3].moddedObjectID == MagicMirror.MagicMirrorMod.ObjectName,
            "another mod's slot is preserved");
        var full = Bench(ObjectID.SolariteWorkbench);
        for (int i = 2; i < 6; i++) full.canCraftObjects[i] = new CraftingAuthoring.CraftableObject { objectID = ObjectID.ExistingRecipe };
        MagicMirror.MirrorRecipeAuthoring.Prefix(full);
        Check(full.canCraftObjects.Count(x => x.moddedObjectID == MagicMirror.MagicMirrorMod.ObjectName) == 0,
            "full category never overwrites other recipes");
    }
}
