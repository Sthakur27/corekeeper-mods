import json,pathlib,subprocess,sys,tempfile
script=pathlib.Path(__file__).with_name('capture_ui.py')
with tempfile.TemporaryDirectory() as temp:
    d=pathlib.Path(temp);snapshot={'orthographicSize':5,'width':1000,'height':1000,'camera':{'x':0,'y':0},'nodes':[
      {'name':'Browser','active':True,'bounds':{'m_Center':{'x':0,'y':0},'m_Extents':{'x':2,'y':2}}},
      {'name':'Inventory','active':True,'bounds':{'m_Center':{'x':1,'y':0},'m_Extents':{'x':2,'y':2}}}]}
    log=d/'test.log';log.write_text('irrelevant\n[StorageUIDev] SNAPSHOT '+json.dumps(snapshot)+'\n[StorageUIDev] SNAPSHOT broken\n')
    result=subprocess.check_output([sys.executable,str(script),'--log',str(log),'--out',str(d/'out')],text=True)
    assert 'BOUNDS OVERLAP' in result and 'inside viewport' in result
    assert json.loads((d/'out/latest.json').read_text())==snapshot
    assert len(json.loads((d/'out/captures.json').read_text()))==1
print('Capture parser and geometry report passed.')

# New vector-bounds schema and rejected incomplete captures.
with tempfile.TemporaryDirectory() as temp:
    d=pathlib.Path(temp);log=d/'test.log';out=d/'out'
    sample={'orthographicSize':5,'width':1000,'height':1000,'camera':{'x':0,'y':0},'nodes':[{'name':'Browser','active':True,'center':{'x':0,'y':0},'size':{'x':4,'y':4}}]}
    log.write_text('[StorageUIDev] SNAPSHOT '+json.dumps(sample)+'\n')
    subprocess.run([sys.executable,str(script),'--log',str(log),'--out',str(out)],check=True,capture_output=True)
    assert json.loads((out/'latest.json').read_text())==sample
    log.write_text('[StorageUIDev] SNAPSHOT '+json.dumps({'camera':{'x':0},'frame':1})+'\n')
    subprocess.run([sys.executable,str(script),'--log',str(log),'--out',str(out)],check=True,capture_output=True)
    assert not (out/'latest.json').exists(),'Incomplete capture must not leave stale replay data'
print('Vector bounds and incomplete capture rejection passed.')
