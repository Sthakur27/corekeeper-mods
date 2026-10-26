from pathlib import Path
import subprocess,json,tempfile
root=Path(__file__).resolve().parents[2]
work=root/'build/magicmirror/recipe-tests';work.mkdir(parents=True,exist_ok=True)
# Test the production authoring hook with a small model of the converter inputs.
runtime=Path(r'C:/Program Files/dotnet/shared/Microsoft.NETCore.App/10.0.11')
csc=next(Path(tempfile.gettempdir()).glob('claude/**/csc.dll'))
rsp=work/'tests.rsp'
lines=['-nologo','-target:exe','-langversion:latest','-out:"'+str(work/'RecipeTests.dll')+'"']
lines+=['-r:"'+str(runtime/p)+'"' for p in ('System.Private.CoreLib.dll','System.Runtime.dll','System.Collections.dll','System.Linq.dll','System.Console.dll')]
lines+=['"'+str(root/p)+'"' for p in ('MagicMirror/Tests/RecipeAuthoringTests.cs','MagicMirror/Scripts/MirrorRecipeAuthoring.cs')]
rsp.write_text('\n'.join(lines)+'\n')
subprocess.run(['dotnet',str(csc),'@'+str(rsp)],check=True)
(work/'RecipeTests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net10.0','framework':{'name':'Microsoft.NETCore.App','version':'10.0.11'}}}))
subprocess.run(['dotnet',str(work/'RecipeTests.dll')],check=True)
