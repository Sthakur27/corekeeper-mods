"""Test the shipping recipe migration against existing and new benches.
Usage: python test_station_recipes.py CSC_DLL REFS_RSP OUTPUT_DIR
"""
from pathlib import Path
import subprocess,sys,json
root=Path(__file__).resolve().parents[1]
source=(root/'Scripts/ConnectedStorageMod.cs').read_text()
def block(marker):
 start=source.index(marker); i=source.index('{',start)+1; depth=1
 while depth: depth+=(source[i]=='{')-(source[i]=='}');i+=1
 return source[start:i]
migration=block('internal static void EnsureRecipes')+block('private static void AddRecipe')
source=(root/'Scripts/StorageRecipeTabs.cs').read_text()
authoring=block('public static void Prefix(CraftingAuthoring')+block('internal static void EnsureWoodRecipe')
network=(root/'Scripts/StorageNetwork.cs').read_text()
catalog=network[network.index('    public static class StorageCatalog'):network.index('    // Items remain')]
code=r"""using System; using System.Collections.Generic;
public enum ObjectID { None, WoodenWorkBench=4003, CopperWorkbench=4043, TinWorkbench=4019, IronWorkBench=4020, ScarletWorkBench=4021, OctarineWorkbench=4027, GalaxiteWorkbench=4033, SolariteWorkbench=4049, HydraWorkbench=4051 }
public struct Entity { }
public struct ObjectDataCD { public ObjectID objectID; }
public struct CanCraftObjectsBuffer { public ObjectID objectID; public int amount; }
public struct IncludedCraftingBuildingsBuffer { public ObjectID objectID; public int amountOfCraftingOptions; }
public class Buffer<T> { public List<T> data=new List<T>(); public int Length=>data.Count; public T this[int i]{get=>data[i];set=>data[i]=value;} public void Add(T v)=>data.Add(v); public void Insert(int i,T v)=>data.Insert(i,v); }
public class FakeWorld {public string Name=>"Test";}
public class EntityManager { public ObjectDataCD item; public Buffer<CanCraftObjectsBuffer> recipes=new Buffer<CanCraftObjectsBuffer>(); public Buffer<IncludedCraftingBuildingsBuffer> included; public FakeWorld World=new FakeWorld(); public bool HasComponent<T>(Entity e)=>true; public bool HasBuffer<T>(Entity e)=>typeof(T)==typeof(CanCraftObjectsBuffer)||included!=null; public T GetComponentData<T>(Entity e)=>(T)(object)item; public Buffer<T> GetBuffer<T>(Entity e,bool readOnly=false)=>(Buffer<T>)(object)(typeof(T)==typeof(CanCraftObjectsBuffer)?(object)recipes:included); }
public class ObjectInfo { public ObjectID objectID; }
public class EntityMonoBehaviourData { public ObjectInfo objectInfo; }
public class CraftingAuthoring {
 public struct CraftableObject { public ObjectID objectID; public string moddedObjectID; public int amount; }
 public EntityMonoBehaviourData component;
 public T GetComponent<T>() where T:class => component as T;
 public List<CraftableObject> canCraftObjects=new List<CraftableObject>();
 public List<CraftingAuthoring> includeCraftedObjectsFromBuildings=new List<CraftingAuthoring>();
}
public static class Debug {public static void Log(string s){} }
static class API { public static class Authoring { static Dictionary<string,ObjectID> ids=new Dictionary<string,ObjectID>(); public static ObjectID GetObjectID(string k) {if(!ids.ContainsKey(k))ids[k]=(ObjectID)(33000+ids.Count); return ids[k];} } }
"""+catalog+'class Test {\n'.replace('\\n','\n')+migration+authoring+r"""
 static void Check(bool ok,string n){if(!ok)throw new Exception(n);Console.WriteLine("PASS "+n);}
 static CanCraftObjectsBuffer R(ObjectID id)=>new CanCraftObjectsBuffer{objectID=id,amount=1};
 static EntityManager Bench(ObjectID id,int slots){var em=new EntityManager();em.item.objectID=id;for(int i=0;i<slots;i++)em.recipes.Add(R((ObjectID)(100+i)));return em;}
 static void Main(){var e=new Entity();
 var wood=Bench(ObjectID.WoodenWorkBench,17);EnsureRecipes(e,wood);
 Check(wood.recipes.Length==18&&wood.recipes[17].objectID==StorageCatalog.Workbench,"station occupies sixth Base slot");
 for(int i=0;i<17;i++)Check(wood.recipes[i].objectID==(ObjectID)(100+i),"preserved vanilla slot "+i);
 EnsureRecipes(e,wood);Check(wood.recipes.Length==18,"repeated registration is idempotent");
 var metal=Bench((ObjectID)4043,18);metal.recipes.Add(R(StorageCatalog.Terminal));metal.recipes.Add(R(StorageCatalog.Gem(1)));EnsureRecipes(e,metal);
 Check(metal.recipes.Length==20&&metal.recipes[18].objectID==ObjectID.None&&metal.recipes[19].objectID==ObjectID.None,"retire old recipes without shifting indices");
 var station=Bench(StorageCatalog.Workbench,0);station.recipes.Add(R(StorageCatalog.Terminal));station.recipes.Add(R(StorageCatalog.Gem(7)));EnsureRecipes(e,station);
 Check(station.recipes[0].objectID==StorageCatalog.Terminal&&station.recipes[1].objectID==StorageCatalog.Gem(7),"dedicated station retains recipes");
 var other=Bench((ObjectID)9999,0);other.recipes.Add(R(StorageCatalog.Gem(1)));EnsureRecipes(e,other);
 Check(other.recipes[0].objectID==StorageCatalog.Gem(1),"unrelated mod crafting station preserved");
 var rawWood=new CraftingAuthoring { component=new EntityMonoBehaviourData { objectInfo=new ObjectInfo {objectID=ObjectID.WoodenWorkBench} } };
 for(int i=0;i<17;i++)rawWood.canCraftObjects.Add(new CraftingAuthoring.CraftableObject {objectID=(ObjectID)(100+i),amount=1});
 var rawSolar=new CraftingAuthoring { component=new EntityMonoBehaviourData { objectInfo=new ObjectInfo {objectID=ObjectID.SolariteWorkbench} } };
 rawSolar.includeCraftedObjectsFromBuildings.Add(rawWood);
 Prefix(rawSolar);
 Check(rawWood.canCraftObjects.Count==18&&rawWood.canCraftObjects[17].moddedObjectID=="ConnectedStorage_Workbench","wood source padded before tier copy and prerequisite generation");
 Prefix(rawSolar);Prefix(rawWood);Check(rawWood.canCraftObjects.Count==18,"multiple world conversions do not duplicate station");
 var solar=Bench(ObjectID.SolariteWorkbench,18);solar.included=new Buffer<IncludedCraftingBuildingsBuffer>();
 solar.included.Add(new IncludedCraftingBuildingsBuffer{objectID=ObjectID.SolariteWorkbench,amountOfCraftingOptions=18});
 solar.included.Add(new IncludedCraftingBuildingsBuffer{objectID=ObjectID.WoodenWorkBench,amountOfCraftingOptions=rawWood.canCraftObjects.Count});
 foreach(var recipe in rawWood.canCraftObjects)solar.recipes.Add(R(recipe.moddedObjectID=="ConnectedStorage_Workbench"?StorageCatalog.Workbench:recipe.objectID));
 solar.included.Add(new IncludedCraftingBuildingsBuffer{objectID=ObjectID.CopperWorkbench,amountOfCraftingOptions=6});
 for(int i=0;i<6;i++)solar.recipes.Add(R((ObjectID)(200+i)));
 var master=new List<CanCraftObjectsBuffer>(solar.recipes.data);
 EnsureRecipes(e,solar);
 // Vanilla unlock refresh restores its master list, which defeated the old live insertion.
 for(int i=0;i<master.Count;i++)solar.recipes[i]=master[i];
 EnsureRecipes(e,solar);
 Check(solar.recipes[35].objectID==StorageCatalog.Workbench,"station survives vanilla prerequisite refresh");
 Check(solar.recipes[36].objectID==(ObjectID)200&&solar.recipes[41].objectID==(ObjectID)205,"copper recipes remain on copper page");
 Check(solar.recipes.Length==42&&solar.included[1].amountOfCraftingOptions==18,"recipe buffer and category counts stay aligned");
 var oldCopy=Bench(ObjectID.SolariteWorkbench,35);oldCopy.included=new Buffer<IncludedCraftingBuildingsBuffer>();
 oldCopy.included.Add(new IncludedCraftingBuildingsBuffer{objectID=ObjectID.SolariteWorkbench,amountOfCraftingOptions=18});
 oldCopy.included.Add(new IncludedCraftingBuildingsBuffer{objectID=ObjectID.WoodenWorkBench,amountOfCraftingOptions=17});EnsureRecipes(e,oldCopy);
 Check(oldCopy.recipes.Length==35&&oldCopy.included[1].amountOfCraftingOptions==17,"runtime code cannot grow counts independently of prerequisite master list");
 var occupied=Bench(ObjectID.WoodenWorkBench,18);EnsureRecipes(e,occupied);
 Check(occupied.recipes[17].objectID==(ObjectID)117&&occupied.recipes[18].objectID==StorageCatalog.Workbench,"other mod's occupied Base slot preserved");
 }
}
"""
out=Path(sys.argv[3]).resolve();out.mkdir(parents=True,exist_ok=True)
(out/'station.cs').write_text(code)
subprocess.run(['dotnet',sys.argv[1],'-nologo','-nowarn:0436','@'+str(Path(sys.argv[2]).resolve()),'-target:exe','-out:'+str(out/'station.dll'),str(out/'station.cs')],check=True)
(out/'station.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net10.0','framework':{'name':'Microsoft.NETCore.App','version':'10.0.0'}}}))
subprocess.run(['dotnet',str(out/'station.dll')],check=True)
