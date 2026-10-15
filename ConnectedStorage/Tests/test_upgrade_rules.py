"""Run the shipping catalog and upgrade rules against all tier/gem combinations.
Usage: python test_upgrade_rules.py CSC_DLL REFS_RSP OUTPUT_DIR
"""
from pathlib import Path
import subprocess,sys,json
source=(Path(__file__).resolve().parents[1]/'Scripts/StorageNetwork.cs').read_text()
a=source.index('    public static class StorageCatalog')
b=source.index('    // Items remain',a)
catalog=source[a:b]
code=r'''using System; using System.Collections.Generic;
public enum ObjectID { None }
static class API { public static class Authoring {
 static Dictionary<string,ObjectID> ids=new Dictionary<string,ObjectID>();
 public static ObjectID GetObjectID(string key) { if(!ids.ContainsKey(key)) ids[key]=(ObjectID)(ids.Count+1); return ids[key]; }
}}
'''+catalog+r'''
class Test {
 static void Check(bool ok,string label) { if(!ok) throw new Exception(label); }
 static void Main() {
  int accepted=0,rejected=0;
  for(int unit=0;unit<8;unit++) for(int gem=1;gem<8;gem++) {
   bool result=StorageCatalog.CanUpgrade(StorageCatalog.Chest(unit),StorageCatalog.Gem(gem));
   Check(result==(gem==unit+1),"tier "+unit+" with gem "+gem);
   if(result) {accepted++;Check(StorageCatalog.Capacities[gem]>StorageCatalog.Capacities[unit],"must grow");} else rejected++;
  }
  Check(!StorageCatalog.CanUpgrade(ObjectID.None,StorageCatalog.Gem(1)),"missing unit");
  Check(!StorageCatalog.CanUpgrade(StorageCatalog.Terminal,StorageCatalog.Gem(1)),"terminal");
  Check(!StorageCatalog.CanUpgrade(StorageCatalog.Chest(0),ObjectID.None),"missing gem");
  Check(!StorageCatalog.CanUpgrade(StorageCatalog.Chest(0),StorageCatalog.Chest(1)),"unit is not gem");
  Console.WriteLine("PASS: "+accepted+" sequential upgrades; "+rejected+" skipped, same-tier or downgrade pairs rejected; 4 invalid-item cases rejected.");
 }
}
'''
out=Path(sys.argv[3]).resolve();out.mkdir(parents=True,exist_ok=True)
(out/'upgrade_rules.cs').write_text(code)
subprocess.run(['dotnet',sys.argv[1],'-nologo','-nowarn:0436','@'+str(Path(sys.argv[2]).resolve()),'-target:exe','-out:'+str(out/'upgrade_rules.dll'),str(out/'upgrade_rules.cs')],check=True)
(out/'upgrade_rules.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net10.0','framework':{'name':'Microsoft.NETCore.App','version':'10.0.0'}}}))
subprocess.run(['dotnet',str(out/'upgrade_rules.dll')],check=True)

