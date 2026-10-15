import argparse,json,pathlib
p=argparse.ArgumentParser();p.add_argument('--log',default=str(pathlib.Path.home()/'AppData/LocalLow/Pugstorm/Core Keeper/Player.log'));p.add_argument('--out',default='build/connectedstorage/ui-captures');a=p.parse_args()
out=pathlib.Path(a.out);out.mkdir(parents=True,exist_ok=True)
rows=[]
for line in pathlib.Path(a.log).read_text(errors='replace').splitlines():
    if '[StorageUIDev] SNAPSHOT ' in line:
        try: rows.append(json.loads(line.split('SNAPSHOT ',1)[1]))
        except json.JSONDecodeError: pass
(out/'captures.json').write_text(json.dumps(rows,indent=2))
valid=[s for s in rows if s.get('nodes') and all('name' in n and ('center' in n or 'bounds' in n) for n in s['nodes'])]
if valid:
    (out/'latest.json').write_text(json.dumps(valid[-1],indent=2))
    print(f'Exported {len(rows)} captures to {out.resolve()}')
else:
    (out/'latest.json').unlink(missing_ok=True)
    print('No complete captures yet. Load the development build, open a terminal or press F8 with a native chest open.')

# Geometry report: deterministic viewport clipping and browser/inventory overlap.
if valid:
    s=valid[-1];half_y=s['orthographicSize'];half_x=half_y*s['width']/s['height'];cam=s['camera']
    boxes={}
    for n in s.get('nodes',[]):
        if not n['active']: continue
        b=n.get('bounds',{});c=n.get('center',b.get('m_Center',b.get('center',{})));e={k:v/2 for k,v in n['size'].items()} if 'size' in n else b.get('m_Extents',b.get('extents',{}))
        if not c or not e: continue
        box=(c['x']-e['x'],c['y']-e['y'],c['x']+e['x'],c['y']+e['y']);boxes[n['name']]=box
        clipped=box[0]<cam['x']-half_x or box[2]>cam['x']+half_x or box[1]<cam['y']-half_y or box[3]>cam['y']+half_y
        print(n['name'], 'OUTSIDE VIEWPORT' if clipped else 'inside viewport',box)
    if 'Browser' in boxes:
        a=boxes['Browser']
        for key in ('Inventory','Hotbar'):
            if key not in boxes: continue
            b=boxes[key];overlap=min(a[2],b[2])>max(a[0],b[0]) and min(a[3],b[3])>max(a[1],b[1])
            print('Browser / '+key+': '+('BOUNDS OVERLAP' if overlap else 'separated'))
