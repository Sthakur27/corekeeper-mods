"""Execute shipping transfer/sort/cursor methods against a deterministic inventory adapter.
This checks mod orchestration/conservation, NOT Unity ECS, replication or native lifecycle.
"""
from pathlib import Path
import subprocess, sys, json
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'Scripts/StorageTransfer.cs').read_text()
def method(marker):
 a=s.index(marker); i=s.index('{',a); depth=1; j=i+1
 while depth:
  depth+=(s[j]=='{')-(s[j]=='}');j+=1
 return s[a:j]
out=Path(sys.argv[2]);out.mkdir(parents=True,exist_ok=True)
harness=(ROOT/'Tests/InventorySafetyAdapter.cs').read_text()
harness=harness.replace('// SHIPPING_METHODS', '\n'.join(method(x) for x in ['public string SortNetwork','public string CursorTransfer','private string TransferCore']))
(out/'InventorySafety.cs').write_text(harness)
runtime=sorted(Path('C:/Program Files/dotnet/shared/Microsoft.NETCore.App').glob('8.*'),key=lambda p:tuple(map(int,p.name.split('.'))))[-1]
refs=[f'-r:{runtime}/{name}.dll' for name in ['System.Private.CoreLib','System.Runtime','System.Console','System.Collections','System.Linq']]
subprocess.run(['dotnet',sys.argv[1],'-nologo','-out:'+str(out/'InventorySafety.dll'),*refs,str(out/'InventorySafety.cs')],check=True)
(out/'InventorySafety.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':runtime.name}}}))
subprocess.run(['dotnet',str(out/'InventorySafety.dll')],check=True)
