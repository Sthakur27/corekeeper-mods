using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class StorageUIPreviewFixtures {
 const string Output="C:/Users/Sid/CoreKeeperMods/build/connectedstorage/previews";
 [MenuItem("Connected Storage/Render UI fixtures")]
 public static void BuildAll(){
  Directory.CreateDirectory(Output);StorageUIPreview.Build();
  var root=GameObject.Find("Browser Preview");
  var template=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="StorageGridSlot");
  var listTemplate=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="StorageListSlot");
  var sprites=AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/ConnectedStorage/Textures"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith("_icon.png")).SelectMany(p=>AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>()).Take(24).ToArray();
  if(sprites.Length==0)throw new Exception("No imported sample sprites for fixtures.");
  var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();
  foreach(var mode in new[]{"empty","counts","full-grid","long-names","no-results"}) {
   var fixture=new GameObject("Fixture "+mode);fixture.transform.SetParent(root.transform,false);
   Text(fixture.transform,"VISUAL FIXTURE: "+mode+" (preview font)",new Vector3(-5.4f,5.6f, -.5f),.10f);
   bool list=mode=="long-names";int slots=mode=="no-results"?0:list?7:56;
   for(int i=0;i<slots;i++){
    var slot=UnityEngine.Object.Instantiate(list?listTemplate:template,fixture.transform,false);slot.name="Fixture slot "+i;
    slot.localPosition=new Vector3(list?0:(i%8-3.5f)*1.375f,2.5f-(list?i:i/8)*1.375f,0);slot.gameObject.SetActive(true);
    foreach(var child in slot.GetComponentsInChildren<Transform>(true))if(child.name.Contains("Border")||child.name.Contains("Amount")||child.name=="NameText"||child.name=="MissingIcon"||child.name=="PlusIcon")child.gameObject.SetActive(false);
    var icon=slot.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.name=="Icon");
    bool populated=mode!="empty"&&(mode!="counts"||i<8);
    if(icon!=null){icon.gameObject.SetActive(populated);icon.enabled=populated;if(populated){icon.sprite=sprites[i%sprites.Length];icon.transform.localPosition=new Vector3(list?-4.8f:0,0,-.1f);icon.transform.localScale=Vector3.one;}}
    if(populated){Text(slot,(new[]{1,2,10,99,999,9999,20,500})[i%8].ToString(),new Vector3(list?4.5f:.5f,-.55f,-.5f),.065f,TextAnchor.LowerRight);
     if(list)Text(slot,"Long item name / variant "+i,new Vector3(-3.5f,.1f,-.5f),.16f);}
   }
   if(mode=="no-results")Text(fixture.transform,"No items match this search.",new Vector3(-5.2f,2.5f,-.5f),.2f);
   Render(camera,mode,1920,1080);UnityEngine.Object.DestroyImmediate(fixture);
  }
  // Exercise the same schema and transform math used by capture replay.
  var sample="{\"width\":2560,\"height\":1440,\"orthographicSize\":8.4375,\"camera\":{\"z\":-10},\"nodes\":[{\"name\":\"Browser\",\"active\":true,\"position\":{\"x\":-6,\"y\":1},\"scale\":{\"x\":0.9,\"y\":0.9,\"z\":0.9},\"center\":{\"x\":-6},\"size\":{\"x\":10,\"y\":12}}]}";
  var capture=JsonUtility.FromJson<StorageUIPreview.Snapshot>(sample);
  if(capture.nodes==null||capture.nodes.Length!=1||capture.nodes[0].size.x!=10)throw new Exception("Replay schema failed");
  root.transform.position=capture.nodes[0].position;root.transform.localScale=capture.nodes[0].scale;
  camera.orthographicSize=capture.orthographicSize;camera.transform.position=capture.camera;camera.aspect=(float)capture.width/capture.height;
  if(root.transform.position.x!=-6||Mathf.Abs(camera.aspect-16f/9)>0.001)throw new Exception("Replay positioning failed");
  Render(camera,"replay-contract",capture.width,capture.height);
  File.WriteAllText(Output+"/result.json","{\"fixtures\":5,\"replayContract\":true,\"liveCaptureVerified\":false,\"nativeInputVerified\":false}");
  Debug.Log("[StorageUIPreview] FIXTURES PASS: five populated/empty states and replay schema/position contract; live game acceptance remains pending.");
 }
 static void Text(Transform parent,string value,Vector3 position,float size,TextAnchor anchor=TextAnchor.UpperLeft){
  var go=new GameObject("Fixture label");go.transform.SetParent(parent,false);go.transform.localPosition=position;
  var text=go.AddComponent<TextMesh>();text.text=value;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=48;text.characterSize=size;text.anchor=anchor;text.color=Color.white;
  go.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;var renderer=go.GetComponent<MeshRenderer>();var reference=parent.GetComponentInChildren<SpriteRenderer>(true);if(reference!=null)renderer.sortingLayerID=reference.sortingLayerID;renderer.sortingOrder=32760;
 }
 static void Render(Camera camera,string name,int width,int height){
  var rt=new RenderTexture(width,height,24);var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
  var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());
  RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
 }
}
