using System;
using System.Collections.Generic;
using System.Linq;
enum ObjectID { None, Bar, Sword, Copper, Tin }
readonly record struct Entity(int Index);
struct ContainedObjectsBuffer { public ObjectID objectID; public int amount,variation,auxDataIndex; }
struct ObjectDataCD { public ObjectID objectID; }
struct InventoryBuffer { public int startIndex,sizeX,sizeY,extraSize; public int size=>sizeX*sizeY+extraSize; }
class Buffer<T>:List<T> { public int Length=>Count; }
class EM {
 public Dictionary<Entity,Buffer<ContainedObjectsBuffer>> items=new();
 public Dictionary<Entity,Buffer<InventoryBuffer>> ranges=new();
 public Dictionary<Entity,ObjectID> types=new();
 public HashSet<(Entity,int)> locks=new();
 public bool Exists(Entity e)=>items.ContainsKey(e);
 public bool HasBuffer<T>(Entity e)=>items.ContainsKey(e);
 public void CompleteAllTrackedJobs(){}
 public Buffer<T> GetBuffer<T>(Entity e)=>(Buffer<T>)(object)(typeof(T)==typeof(ContainedObjectsBuffer)?(object)items[e]:ranges[e]);
 public T GetComponentData<T>(Entity e)=>(T)(object)new ObjectDataCD{objectID=types[e]};
 public void Add(Entity e,int count,ObjectID type=ObjectID.None){types[e]=type;items[e]=new();for(int i=0;i<count;i++)items[e].Add(default);ranges[e]=new(){new(){sizeX=count,sizeY=1}};}
}
static class StorageRemoval {public static Entity Active;public static bool IsEvacuating(W w,Entity e)=>e==Active;}
class W { public bool IsServer()=>true; }
enum Allocator { Temp }
class EntityCommandBuffer:IDisposable { public EntityCommandBuffer(Allocator a){} public void Playback(EM em){} public void Dispose(){} }
struct SkillTalentsTableCD {} struct UpgradeCostsTableCD {} struct InventoryAuxDataSystemDataCD {} struct NetworkTime {}
static class PugDatabase {
 public struct DatabaseBankCD { public object databaseBankBlob; }
 public class Info {public bool isStackable;}
 public static Info GetObjectInfo(ObjectID id,int v=0)=>new(){isStackable=id==ObjectID.Bar};
 public static Info GetEntityObjectInfo(ObjectID id,object b,int v=0)=>GetObjectInfo(id,v);
}
static class PlayerController { public class Name{public string text;} public static Name GetObjectName(ContainedObjectsBuffer i,bool b)=>new(){text=i.objectID.ToString()}; }
static class StorageCatalog {public static int Capacity(ObjectID id)=>id==ObjectID.Copper?20:id==ObjectID.Tin?40:0;}
class InventoryHandlerShared {
 public static EM Current;
 public EM em;
 public PugDatabase.DatabaseBankCD databaseBankCD;
 public Dictionary<Entity,Buffer<ContainedObjectsBuffer>> containedObjectsBufferLookup=>em.items;
 public InventoryHandlerShared(ref int state,params object[] args){em=Current;}
 public void Update(ref int state,EntityCommandBuffer ecb,NetworkTime time){}
}
// Adapter models the native metadata overwrite rule deliberately, so the mod
// must prevent unsafe merges rather than being rescued by a forgiving fake.
static class InventoryUtility {
 public static int TryFindSlotToAddTo(in InventoryHandlerShared s,ObjectID id,Entity e,int hint,int end,int variation){
  var r=s.em.ranges[e][0];var items=s.em.items[e];
  bool Fits(int i)=>items[i].objectID==ObjectID.None||(id==ObjectID.Bar&&items[i].objectID==id&&items[i].variation==variation&&items[i].amount<9999);
  if(hint>=r.startIndex&&hint<r.startIndex+r.size&&Fits(hint))return hint;
  for(int i=r.startIndex;i<r.startIndex+r.size;i++)if(Fits(i))return i;
  return -1;
 }
 public static void MoveAmount(in InventoryHandlerShared s,Entity from,int index,Entity to,int hint,int end,int amount){
  var a=s.em.items[from][index];int dest=TryFindSlotToAddTo(in s,a.objectID,to,hint,end,a.variation);if(dest<0||from==to&&index==dest)return;
  var b=s.em.items[to][dest];int n=a.objectID==ObjectID.Bar?Math.Min(Math.Min(amount,a.amount),9999-b.amount):a.amount;
  b.objectID=a.objectID;b.variation=a.variation;b.auxDataIndex=a.auxDataIndex;b.amount+=n;a.amount-=n;
  s.em.items[from][index]=a.amount==0?default:a;s.em.items[to][dest]=b;
 }
 public static void Swap(in InventoryHandlerShared s,Entity a,Entity b,int i,int j){var x=s.em.items[a][i];s.em.items[a][i]=s.em.items[b][j];s.em.items[b][j]=x;}
}
class Shipping {
 public EM EntityManager=new(); public W World=new();int CheckedStateRef;
 T Singleton<T>()=>default;
 bool Available(Entity e)=>EntityManager.items.ContainsKey(e);
 static bool Locked(EM em,Entity e,int i)=>em.locks.Contains((e,i));
 public string Transfer(Entity p,Entity s,int slot,List<Entity> targets,int amount)=>TransferCore(p,s,slot,targets,amount,false);
 public string Evacuate(Entity s,int slot,List<Entity> targets)=>TransferCore(s,s,slot,targets,int.MaxValue,true);
 // SHIPPING_METHODS
}
class Test {
 static ContainedObjectsBuffer Item(int n,int aux=0,ObjectID id=ObjectID.Bar,int variation=0)=>new(){objectID=id,amount=n,auxDataIndex=aux,variation=variation};
 static string Totals(EM em)=>string.Join(";",em.items.Values.SelectMany(x=>x).Where(x=>x.objectID!=ObjectID.None).GroupBy(x=>(x.objectID,x.variation,x.auxDataIndex)).OrderBy(x=>x.Key).Select(g=>$"{g.Key}:{g.Sum(x=>x.amount)}"));
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static Shipping Setup(){var s=new Shipping();s.EntityManager.Add(new(1),20,ObjectID.Copper);s.EntityManager.Add(new(2),40,ObjectID.Tin);s.EntityManager.Add(new(3),31);InventoryHandlerShared.Current=s.EntityManager;return s;}
 static void Main(){
  var random=new Random(642);var units=new List<Entity>{new(1),new(2)};
  for(int trial=0;trial<1000;trial++){
   var s=Setup();var em=s.EntityManager;
   foreach(var e in units)for(int i=0;i<em.items[e].Count;i++)if(random.Next(3)!=0){em.items[e][i]=Item(random.Next(1,10000),random.Next(3),random.Next(4)==0?ObjectID.Sword:ObjectID.Bar,random.Next(2));if(random.Next(8)==0)em.locks.Add((e,i));}
   var before=Totals(em);var locked=em.locks.ToDictionary(k=>k,k=>em.items[k.Item1][k.Item2]);
   s.SortNetwork(units,trial%2==0);Check(Totals(em)==before,"sort conservation");foreach(var x in locked)Check(em.items[x.Key.Item1][x.Key.Item2].Equals(x.Value),"locked slot changed");
   s.SortNetwork(units,trial%2!=0);Check(Totals(em)==before,"repeat sort conservation");
  }
  {var s=Setup();var em=s.EntityManager;em.items[new(1)][0]=Item(9990);em.items[new(2)][3]=Item(20);s.SortNetwork(units,false);Check(em.items[new(1)][0].amount==9999&&em.items[new(1)][1].amount==11,"merge overflow");}
  for(int mode=0;mode<4;mode++){
   var s=Setup();var em=s.EntityManager;em.items[new(1)][0]=Item(25,7);string before=Totals(em);s.CursorTransfer(new(3),new(1),30,0,mode);Check(Totals(em)==before,"cursor conservation");Check(em.items[new(3)][30].amount==(mode==0?25:mode==1?1:mode==2?13:10),"cursor amount");Check(em.ranges[new(3)][0].size==31,"range not restored");
  }
  {var s=Setup();var em=s.EntityManager;em.items[new(3)][10]=Item(30,1);em.items[new(1)][0]=Item(10,2);string before=Totals(em);s.Transfer(new(3),new(3),10,units,int.MaxValue);Check(Totals(em)==before,"aux identity lost on transfer");Check(em.items[new(1)][0].amount==10&&em.items[new(1)][1].amount==30,"distinct aux must not merge");}
  {var s=Setup();var em=s.EntityManager;foreach(var e in units)for(int i=0;i<em.items[e].Count;i++)em.items[e][i]=Item(9999);em.items[new(1)][0]=Item(9990);em.items[new(3)][10]=Item(30);string before=Totals(em);s.Transfer(new(3),new(3),10,units,int.MaxValue);Check(Totals(em)==before&&em.items[new(3)][10].amount==21,"partial/full transfer loss");}
  {var s=Setup();var em=s.EntityManager;StorageRemoval.Active=new(1);em.items[new(1)][0]=Item(30,7);em.locks.Add((new(1),0));em.items[new(2)][0]=Item(9999,8);string before=Totals(em);s.Evacuate(new(1),0,new(){new(2)});Check(Totals(em)==before&&em.items[new(2)][1].amount==30,"evacuation metadata conservation");StorageRemoval.Active=default;}
  Console.WriteLine("PASS: 1000 randomized production sort cases, repeat sorts/locks/metadata; cursor full/one/half/ten; partial/full transfers; auxiliary identity separation. Adapter tests, not live ECS.");
 }
}
