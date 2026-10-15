"""Execute the shipping pure planner with lightweight types, without starting Unity.
Usage: python test_removal_planner.py CSC_DLL REFS_RSP [output_directory]
The planner method and its slot/move records are extracted verbatim from production.
"""
import pathlib, re, subprocess, sys, json
root = pathlib.Path(__file__).resolve().parents[1]
source = (root / 'Scripts/StorageRemoval.cs').read_text()
def block(marker):
    start = source.index(marker)
    opening = source.index('{', start)
    depth = 1
    i = opening + 1
    while depth:
        depth += (source[i] == '{') - (source[i] == '}')
        i += 1
    return source[start:i]
out = pathlib.Path(sys.argv[3] if len(sys.argv)>3 else root / '../../build/connectedstorage').resolve()
out.mkdir(parents=True, exist_ok=True)
header = '''using System; using System.Collections.Generic;
enum ObjectID { None, Bar, Sword }
struct Entity { public int Index; }
struct ObjectDataCD { public ObjectID objectID; public int amount; public int variation; }
class Test {
'''
tests = r'''
static ObjectDataCD Item(ObjectID id,int amount,int variation=0)=>new ObjectDataCD{objectID=id,amount=amount,variation=variation};
static Slot Dest(ObjectID id,int amount,int variation=0,int chest=1)=>new Slot{Chest=new Entity{Index=chest},Index=chest,Item=Item(id,amount,variation)};
static bool Stack(ObjectID id)=>id==ObjectID.Bar;
static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
static void Main(){
 List<Move> m;
 Check(!TryPlan(new List<ObjectDataCD>{Item(ObjectID.Sword,150),Item(ObjectID.Bar,5)},new List<Slot>{Dest(ObjectID.Bar,9994)},Stack,out m)&&m.Count==1&&m[0].Source==1&&m[0].Amount==5,"later item fits despite earlier overflow");
 Check(TryPlan(new List<ObjectDataCD>{Item(ObjectID.Bar,50)},new List<Slot>{Dest(ObjectID.Bar,9949)},Stack,out m)&&m.Count==1&&m[0].Amount==50,"exact existing stack capacity");
 Check(!TryPlan(new List<ObjectDataCD>{Item(ObjectID.Bar,51)},new List<Slot>{Dest(ObjectID.Bar,9949)},Stack,out m)&&m.Count==1&&m[0].Amount==50,"partial capacity preserves moves");
 Check(TryPlan(new List<ObjectDataCD>{Item(ObjectID.Bar,51)},new List<Slot>{Dest(ObjectID.Bar,9949),Dest(ObjectID.None,0,0,2)},Stack,out m)&&m.Count==2&&m[1].Amount==1&&m[1].Target.Index==2,"overflow across physical chests");
 Check(!TryPlan(new List<ObjectDataCD>{Item(ObjectID.Bar,1,2)},new List<Slot>{Dest(ObjectID.Bar,1,3)},Stack,out m),"different variants cannot merge");
 Check(!TryPlan(new List<ObjectDataCD>{Item(ObjectID.Sword,150)},new List<Slot>{Dest(ObjectID.Sword,20)},Stack,out m),"nonstackable durability cannot merge");
 Check(TryPlan(new List<ObjectDataCD>{Item(ObjectID.Sword,150)},new List<Slot>{Dest(ObjectID.None,0)},Stack,out m)&&m.Count==1&&m[0].Amount==150,"nonstackable durability preserved as whole item");
 Check(TryPlan(new List<ObjectDataCD>{Item(ObjectID.Bar,9998),Item(ObjectID.Bar,1)},new List<Slot>{Dest(ObjectID.None,0)},Stack,out m)&&m.Count==2,"planned reservations allow compatible merge");
 Check(!TryPlan(new List<ObjectDataCD>{Item(ObjectID.Bar,9999),Item(ObjectID.Bar,1)},new List<Slot>{Dest(ObjectID.None,0)},Stack,out m)&&m.Count==1&&m[0].Amount==9999,"planned reservations prevent overcommit");
 Check(TryPlan(new List<ObjectDataCD>(),new List<Slot>(),Stack,out m)&&m.Count==0,"empty source succeeds without capacity");
 Check(!TryPlan(new List<ObjectDataCD>{Item(ObjectID.Bar,1)},new List<Slot>(),Stack,out m),"occupied source without destinations fails");
 Check(TryPlan(new List<ObjectDataCD>{Item(ObjectID.Bar,3)},new List<Slot>{Dest(ObjectID.None,0),Dest(ObjectID.Bar,9996,0,2)},Stack,out m)&&m.Count==1&&m[0].Target.Index==2,"existing stacks preferred to empty slots");
}
}
'''
code = header + block('private struct Slot') + block('private struct Move') + block('private static bool TryPlan') + tests
(out/'planner_test.cs').write_text(code)
subprocess.run(['dotnet',sys.argv[1],'-nologo','-nowarn:0436','-langversion:latest','@'+str(pathlib.Path(sys.argv[2]).resolve()),'-target:exe','-out:'+str(out/'planner_test.dll'),str(out/'planner_test.cs')],check=True)
(out/'planner_test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net10.0','framework':{'name':'Microsoft.NETCore.App','version':'10.0.0'}}}))
subprocess.run(['dotnet',str(out/'planner_test.dll')],check=True)
