using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public class StorageUIPreview : EditorWindow {
    const string Prefab="Assets/ConnectedStorageBrowser/ConnectedStorageBrowser.prefab";
    const string Scene="Assets/ConnectedStorageBrowser/StorageUIPreview.unity";
    const string Captures="C:/Users/Sid/CoreKeeperMods/build/connectedstorage/ui-captures/latest.json";
    const string Config="C:/Users/Sid/AppData/LocalLow/Pugstorm/Core Keeper/Steam/138826622/mods/ConnectedStorage/ui-dev.cfg";
    [Serializable] public class Node { public string name;public Vector3 position,scale,center,size;public Bounds bounds;public bool active; }
    [Serializable] public class Snapshot { public string reason;public int frame,width,height;public float time,orthographicSize,near,far;public Vector3 camera;public Node[] nodes; }
    Snapshot snapshot; Vector2 browser=new Vector2(-7.5f,.5f),bag=new Vector2(6.75f,-1);float panelScale=1,textScale=1;
    [MenuItem("Connected Storage/UI Development Preview")]
    public static void Open() { GetWindow<StorageUIPreview>("Storage UI Preview"); }
    void OnEnable(){SceneView.duringSceneGui+=DrawBounds;}
    void OnDisable(){SceneView.duringSceneGui-=DrawBounds;}
    void OnGUI(){
        EditorGUILayout.HelpBox("Edit-mode visual preview. Browser uses its real prefab; native inventory is captured geometry, not a simulated inventory. Game interactions still require Test world.",MessageType.Info);
        if(GUILayout.Button("Open / create preview scene")) { if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Build(); }
        if(GUILayout.Button("Load latest game capture")) {if(File.Exists(Captures)){snapshot=JsonUtility.FromJson<Snapshot>(File.ReadAllText(Captures));if(snapshot.nodes==null)throw new Exception("Capture has no panel data; collect a new capture.");foreach(var n in snapshot.nodes)if(n.size!=Vector3.zero)n.bounds=new Bounds(n.center,n.size);ApplyCapture();}else Debug.LogWarning("Run ConnectedStorage/Dev/capture_ui.py after an in-game capture.");}
        browser=EditorGUILayout.Vector2Field("Browser",browser);bag=EditorGUILayout.Vector2Field("Inventory",bag);panelScale=EditorGUILayout.Slider("Panel scale",panelScale,.6f,1.4f);textScale=EditorGUILayout.Slider("Heading / hint scale",textScale,.75f,1.5f);
        if(GUILayout.Button("Write live layout (game polls every second)")) WriteLayout();
        EditorGUILayout.LabelField("F8 in game captures current UI; terminal opening captures three frames.",EditorStyles.wordWrappedLabel);
        if(snapshot!=null)EditorGUILayout.LabelField($"Capture: {snapshot.reason}, frame {snapshot.frame}, {snapshot.width} x {snapshot.height}",EditorStyles.wordWrappedLabel);
    }
    void WriteLayout(){
        if(!File.Exists(Config)){Debug.LogWarning("Start the new mod build once to create ui-dev.cfg.");return;}
        var text=File.ReadAllText(Config);
        foreach(var pair in new[]{("BrowserX",browser.x),("BrowserY",browser.y),("InventoryX",bag.x),("InventoryY",bag.y),("PanelScale",panelScale),("TextScale",textScale)})
            text=System.Text.RegularExpressions.Regex.Replace(text,@"(?m)^"+pair.Item1+@"\s*=.*$",pair.Item1+" = "+pair.Item2.ToString(System.Globalization.CultureInfo.InvariantCulture));
        File.WriteAllText(Config,text);Debug.Log("[StorageUIPreview] Live layout written.");
    }
    public static void Build(){
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Preview Camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=9;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.09f,.1f);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);if(source==null)throw new Exception("Browser prefab missing; run build_browser.py first.");
        var root=(GameObject)PrefabUtility.InstantiatePrefab(source);root.name="Browser Preview";
        // Strip gameplay scripts from this scene instance: they require Manager singletons.
        foreach(var script in root.GetComponentsInChildren<MonoBehaviour>(true))DestroyImmediate(script);
        foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name=="TabsAnchor"||t.name=="FiltersPanel"||t.name=="FakeScrollBar"||t.name=="Toggled")t.gameObject.SetActive(false);
        root.transform.position=new Vector3(-7.5f,.5f,0);root.SetActive(true);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Scene);
        if(SceneView.lastActiveSceneView!=null){SceneView.lastActiveSceneView.in2DMode=true;SceneView.lastActiveSceneView.LookAt(Vector3.zero,Quaternion.identity,15);}
        Debug.Log("[StorageUIPreview] Scene saved: "+Scene);
        var target=new RenderTexture(1920,1080,24);camera.targetTexture=target;camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
        File.WriteAllBytes("C:/Users/Sid/CoreKeeperMods/build/connectedstorage/ui-preview.png",image.EncodeToPNG());
        RenderTexture.active=previous;camera.targetTexture=null;DestroyImmediate(image);DestroyImmediate(target);
    }
    void ApplyCapture(){
        var c=FindFirstObjectByType<Camera>();if(c!=null){c.aspect=(float)snapshot.width/snapshot.height;c.transform.position=snapshot.camera;c.orthographicSize=snapshot.orthographicSize;c.nearClipPlane=snapshot.near;c.farClipPlane=snapshot.far;}
        var root=GameObject.Find("Browser Preview");var node=snapshot.nodes.FirstOrDefault(n=>n.name=="Browser");
        if(root!=null&&node!=null&&node.active){root.transform.position=node.position;root.transform.localScale=node.scale;}
        SceneView.RepaintAll();
    }
    void DrawBounds(SceneView view){
        if(snapshot==null)return;
        foreach(var n in snapshot.nodes.Where(n=>n.active)) {Handles.color=n.name=="Browser"?Color.cyan:Color.yellow;Handles.DrawWireCube(n.bounds.center,n.bounds.size);Handles.Label(n.bounds.max,n.name+" (captured bounds)");}
    }
}
