"""Bounded log watch: no input injection, background service, or game restarts."""
import argparse,json,pathlib,re,time,subprocess,sys
p=argparse.ArgumentParser();p.add_argument('--seconds',type=float,default=60);p.add_argument('--log',type=pathlib.Path,default=pathlib.Path.home()/'AppData/LocalLow/Pugstorm/Core Keeper/Player.log');a=p.parse_args()
out=pathlib.Path(__file__).resolve().parents[2]/'build/connectedstorage';out.mkdir(parents=True,exist_ok=True)
end=time.monotonic()+max(0,a.seconds);offset=0;events=[];pending=b''
while True:
 if a.log.exists():
  if a.log.stat().st_size<offset:offset=0;pending=b''
  with a.log.open('rb') as f:f.seek(offset);chunk=f.read();offset=f.tell()
  lines=(pending+chunk).split(b'\n');pending=lines.pop()
  for raw in lines:
   line=raw.decode('utf-8',errors='replace').strip()
   if re.search(r'ConnectedStorage|StorageUIDev|Exception|error CS\d+|CompileFailed',line):
    events.append(line);print(line,flush=True)
  (out/'log-watch.json').write_text(json.dumps({'updatedAt':time.time(),'events':events[-200:]},indent=2))
 if time.monotonic()>=end:break
 time.sleep(.5)
subprocess.run([sys.executable,str(pathlib.Path(__file__).with_name('capture_ui.py')),'--log',str(a.log)],check=True)
