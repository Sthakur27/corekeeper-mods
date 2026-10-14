from pathlib import Path
import subprocess,json,tempfile,urllib.request,zipfile,io,os
root=Path(__file__).resolve().parents[2]
managed=Path(r'C:/Program Files (x86)/Steam/steamapps/common/Core Keeper/CoreKeeper_Data/Managed')
work=root/'build/magicmirror';work.mkdir(parents=True,exist_ok=True)
scratch=Path(tempfile.gettempdir())
csc=next(scratch.glob('claude/**/csc.dll'),None)
if csc is None:
 tool=scratch/'magicmirror-compiler'
 csc=tool/'tasks/netcore/bincore/csc.dll'
 if not csc.exists():
  data=urllib.request.urlopen('https://www.nuget.org/api/v2/package/Microsoft.Net.Compilers.Toolset').read()
  with zipfile.ZipFile(io.BytesIO(data)) as archive:archive.extractall(tool)
refs=['-nologo','-target:library','-langversion:latest','-unsafe']+['-r:"'+str(p)+'"' for p in managed.glob('*.dll')]
refs+=['-out:"'+str(work/'MagicMirror.dll')+'"']+['"'+str(p)+'"' for p in (root/'MagicMirror/Scripts').rglob('*.cs')]
rsp=work/'compile.rsp';rsp.write_text('\n'.join(refs))
raise SystemExit(subprocess.run(['dotnet',str(csc),'@'+str(rsp)]).returncode)
