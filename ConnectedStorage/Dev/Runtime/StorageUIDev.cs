using System;
using System.Linq;
using CoreLib.Data.Configuration;
using UnityEngine;
namespace ConnectedStorage {
    // Development tooling; no inventory/save mutations. CoreLib owns file access.
    public static class StorageUIDev {
        static ConfigFile config;
        static ConfigEntry<bool> enabled;
        static ConfigEntry<float> browserX,browserY,bagX,bagY,barY,panelScale,textScale;
        static float nextPoll;
        static int captureFrames;
        public static void Init() {
            config=new ConfigFile("ConnectedStorage/ui-dev.cfg",true);
            enabled=config.Bind("Development","Enabled",true);
            browserX=config.Bind("Layout","BrowserX",-7.5f);
            browserY=config.Bind("Layout","BrowserY",0.5f);
            bagX=config.Bind("Layout","InventoryX",6.75f);
            bagY=config.Bind("Layout","InventoryY",-1f);
            barY=config.Bind("Layout","HotbarY",2f);
            panelScale=config.Bind("Layout","PanelScale",1f);
            textScale=config.Bind("Layout","TextScale",1f);
            config.SaveOnConfigSet=false;
        }
        static float Valid(float x,float fallback) => float.IsNaN(x)||float.IsInfinity(x)||Mathf.Abs(x)>30 ? fallback:x;
        public static Vector2 Browser => enabled!=null && enabled.Value ? new Vector2(Valid(browserX.Value,-7.5f),Valid(browserY.Value,.5f)):new Vector2(-7.5f,.5f);
        public static Vector2 Bag => enabled!=null && enabled.Value ? new Vector2(Valid(bagX.Value,6.75f),Valid(bagY.Value,-1)):new Vector2(6.75f,-1);
        public static float BarY => enabled!=null && enabled.Value ? Valid(barY.Value,2):2;
        public static float PanelScale => enabled!=null&&enabled.Value ? Mathf.Clamp(Valid(panelScale.Value,1),.6f,1.4f):1;
        public static float TextScale => enabled!=null&&enabled.Value ? Mathf.Clamp(Valid(textScale.Value,1),.75f,1.5f):1;
        public static void Opened() { captureFrames=12; }
        public static void Tick(GameObject browser) {
            if(config==null)return;
            if(Time.unscaledTime>=nextPoll) {
                nextPoll=Time.unscaledTime+1;
                try { config.Reload(); } catch(Exception ex) { Debug.LogWarning("[StorageUIDev] Reload: "+ex.Message); }
            }
            if(!enabled.Value)return;
            if(Input.GetKeyDown(KeyCode.F8)) Capture(browser,"F8");
            if(captureFrames>0) { if(captureFrames==12||captureFrames==11||captureFrames==1)Capture(browser,"open+"+(12-captureFrames));captureFrames--; }
        }
        [Serializable] public class Node { public string name;public Vector3 position,scale;public Bounds bounds; public bool active; }
        [Serializable] public class Snapshot { public string reason; public int frame,width,height;public float time,orthographicSize,near,far;public Vector3 camera; }
        static Node Read(string name,GameObject go) {
            var node=new Node {name=name};if(go==null)return node;
            node.position=go.transform.position;node.scale=go.transform.lossyScale;node.active=go.activeInHierarchy;
            bool first=true;
            foreach(var r in go.GetComponentsInChildren<SpriteRenderer>())if(r.enabled&&r.gameObject.activeInHierarchy) {if(first){node.bounds=r.bounds;first=false;}else node.bounds.Encapsulate(r.bounds);}
            node.active = node.active && !first;
            return node;
        }
        static string NodeJson(Node n) => "{\"name\":\""+n.name+"\",\"active\":"+(n.active?"true":"false")+",\"position\":"+JsonUtility.ToJson(n.position)+",\"scale\":"+JsonUtility.ToJson(n.scale)+",\"center\":"+JsonUtility.ToJson(n.bounds.center)+",\"size\":"+JsonUtility.ToJson(n.bounds.size)+"}";
        public static void Capture(GameObject browser,string reason) {
            if(Manager.ui==null)return;
            var c=Manager.ui.UICamera.GetComponent<Camera>(); if(c==null)return;
            var itemBrowser=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)
                .FirstOrDefault(m=>m!=null && m.GetType().FullName=="ItemBrowser.Common.UserInterface.Browser.ItemBrowserUI");
            var snapshot=new Snapshot {reason=reason,frame=Time.frameCount,time=Time.unscaledTime,width=Screen.width,height=Screen.height,orthographicSize=c.orthographicSize,near=c.nearClipPlane,far=c.farClipPlane,camera=c.transform.position};
            var nodes=new[]{Read("Browser",browser),Read("Inventory",Manager.ui.playerInventoryUI.gameObject),Read("Hotbar",Manager.ui.itemSlotsBar.gameObject),Read("NativeChest",Manager.ui.chestInventoryUI.gameObject),Read("ItemBrowser",itemBrowser==null?null:itemBrowser.gameObject)};
            var header=JsonUtility.ToJson(snapshot);
            Debug.Log("[StorageUIDev] SNAPSHOT "+header.Substring(0,header.Length-1)+",\"nodes\":["+string.Join(",",nodes.Select(NodeJson))+"]}");
        }
    }
}

