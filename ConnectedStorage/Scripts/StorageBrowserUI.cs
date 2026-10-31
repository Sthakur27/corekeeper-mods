using System;
using System.Collections.Generic;
using System.Linq;
using ConnectedStorage.Browser;
using CoreLib.Submodule.Command;
using CoreLib.Submodule.Command.Data;
using HarmonyLib;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Events;

namespace ConnectedStorage
{
    // Authored Item Browser visuals, with storage-specific bindings. No catalogue
    // controllers, global Item Browser settings, or spawn actions are loaded.
    [DefaultExecutionOrder(10000)]
    public sealed class StorageBrowserUI : MonoBehaviour
    {
        public sealed class Address { public Entity Entity; public int Index; public ContainedObjectsBuffer Item; }
        public static StorageBrowserUI Instance;
        public static GameObject Prefab;
        public static bool Active => Instance != null && Instance.terminal != null;
        private Chest terminal;
        private GameObject screen;
        private TextInputField search;
        private UIScrollWindow scroll;
        private StorageBrowserModel<Address> model;
        private readonly List<StorageBrowserItem> grid = new List<StorageBrowserItem>();
        private readonly List<StorageBrowserItem> list = new List<StorageBrowserItem>();
        private BrowserLayout layout;
        private BrowserSort sort = BrowserSort.Physical;
        private bool descending;
        private int filter;
        private GameObject tabColumn;
        private readonly List<ButtonUIElement> tabButtons=new List<ButtonUIElement>();
        private readonly List<GameObject> tabToggles=new List<GameObject>();
        private readonly List<List<SpriteRenderer>> tabIcons=new List<List<SpriteRenderer>>();
        private readonly List<GameObject> tabHighlights=new List<GameObject>();
        private static readonly Color Gold=new Color(1f,.8f,.25f,1);
        // The chest's art fills its whole canvas, unlike most item sprites.
        private static float IconScale(int category)=>category==0?.6f:1f;
        private int hoveredTab=-1, tabSelectedFrame=-1;
        private bool tabsPlaced;
        // Index order is also the physical sort order (StorageTransfer.SortNetwork).
        private static readonly string[] Categories={"All","Materials","Weapons","Tools","Armor","Accessories","Food","Blocks","Furniture","Pets","Valuables","Other"};
        // Each tab borrows a familiar item as its icon.
        private static readonly ObjectID[] CategoryIcons={ObjectID.InventoryChest,ObjectID.CopperBar,ObjectID.CopperSword,ObjectID.CopperMiningPick,
            ObjectID.CopperHelm,ObjectID.CopperCrossNecklace,ObjectID.HeartBerry,ObjectID.GroundDirtBlock,ObjectID.WoodenWorkBench,
            ObjectID.PetCatEgg,ObjectID.AncientCoin,ObjectID.CopperKey};
        private const int Materials=1,Weapons=2,Tools=3,Armor=4,Accessories=5,Food=6,Blocks=7,Furniture=8,Pets=9,Valuables=10,Other=11;
        private static bool PlacesTile(PugTilemap.TileType tile) {
            switch(tile) {
                case PugTilemap.TileType.wall: case PugTilemap.TileType.thinWall: case PugTilemap.TileType.greatWall:
                case PugTilemap.TileType.ground: case PugTilemap.TileType.floor: case PugTilemap.TileType.litFloor: case PugTilemap.TileType.looseFlooring:
                case PugTilemap.TileType.bridge: case PugTilemap.TileType.fence: case PugTilemap.TileType.rug: case PugTilemap.TileType.rail:
                case PugTilemap.TileType.circuitPlate: case PugTilemap.TileType.ancientCircuitPlate: case PugTilemap.TileType.roofHole:return true;
                default:return false;
            }
        }
        internal static int Category(ObjectInfo info) {
            if(info==null)return Other;
            bool Tagged(ObjectCategoryTag tag)=>info.tags!=null && info.tags.Contains(tag);
            switch(info.objectType) {
                case ObjectType.MeleeWeapon: case ObjectType.RangeWeapon: case ObjectType.SummoningWeapon: case ObjectType.ThrowingWeapon: case ObjectType.BeamWeapon: case ObjectType.CastingItem:return Weapons;
                case ObjectType.Shovel: case ObjectType.Hoe: case ObjectType.MiningPick: case ObjectType.PaintTool: case ObjectType.FishingRod: case ObjectType.BugNet: case ObjectType.Sledge: case ObjectType.RoofingTool: case ObjectType.DrillTool: case ObjectType.WaterCan: case ObjectType.Bucket: case ObjectType.Seeder:return Tools;
                case ObjectType.Helm: case ObjectType.BreastArmor: case ObjectType.PantsArmor:return Armor;
                case ObjectType.Necklace: case ObjectType.Ring: case ObjectType.Offhand: case ObjectType.Bag: case ObjectType.Lantern: case ObjectType.Pouch:return Accessories;
                case ObjectType.Eatable:return Food;
                case ObjectType.Pet: case ObjectType.Critter:return Pets;
                case ObjectType.Valuable:return Valuables;
                case ObjectType.PlaceablePrefab: case ObjectType.TrainingDummy: case ObjectType.NonUsable: case ObjectType.UniqueCraftingComponent:
                    if(PlacesTile(info.tileType))return Blocks;
                    if(Tagged(ObjectCategoryTag.PetEgg))return Pets;
                    if(Tagged(ObjectCategoryTag.Seed))return Materials;
                    return info.objectType==ObjectType.PlaceablePrefab || info.objectType==ObjectType.TrainingDummy ? Furniture : Materials;
                default:return Other;
            }
        }
        private float spread, nextScan, nextNetworkScan;
        private Address pendingCursorSlot;
        private float pendingCursorUntil;
        private StorageNetwork.Snapshot cachedNetwork;
        private World expansionWorld;
        private StorageExpansionTracker<int2> expansions = new StorageExpansionTracker<int2>();
        private void FitInventoryToScreen() {
            var camera=Manager.ui.UICamera.GetComponent<Camera>();
            if(camera==null)return;
            bool first=true;var bounds=new Bounds();
            foreach(var slot in Manager.ui.playerInventoryUI.itemSlots) {
                if(slot==null || !slot.gameObject.activeInHierarchy || slot.background==null)continue;
                // Rows scrolled out of the inventory's window (bag rows above, pouches below) stay
                // active; measuring them would drag the whole panel along with the scroll.
                var window=slot.uiScrollWindow;
                if(window!=null && !window.IsShowingPosition(slot.localScrollPosition))continue;
                if(first){bounds=slot.background.bounds;first=false;}else bounds.Encapsulate(slot.background.bounds);
            }
            if(first)return;
            var low=camera.WorldToViewportPoint(bounds.min);var high=camera.WorldToViewportPoint(bounds.max);
            float dx=high.x>.98f?.98f-high.x:low.x<.02f?.02f-low.x:0;
            float dy=low.y<.02f?.02f-low.y:high.y>.98f?.98f-high.y:0;
            var origin=camera.WorldToViewportPoint(Manager.ui.playerInventoryUI.transform.position);
            Manager.ui.playerInventoryUI.transform.position=camera.ViewportToWorldPoint(origin+new Vector3(dx,dy,0));
        }
        private Vector3 bagPosition, barPosition;
        private string lastSearch = "";
        private PugText heading;
        private PugText emptyMessage;
        private int used,capacity;
        private bool closing;
        private readonly Dictionary<SpriteRenderer,(int layer,int order,SpriteMaskInteraction mask)> cursorLayers=new Dictionary<SpriteRenderer,(int,int,SpriteMaskInteraction)>();
        private readonly Dictionary<PugText,PugTextStyle> cursorTextStyles=new Dictionary<PugText,PugTextStyle>();
        private void RaiseCursor() {
            var mouse=Manager.ui.mouse;int order=100;
            foreach(var renderer in new[]{mouse.grabbedItemUnderlaySR,mouse.grabbedItemSR,mouse.grabbedItemOverlaySR}) {
                if(renderer==null)continue;
                if(!cursorLayers.ContainsKey(renderer))cursorLayers.Add(renderer,(renderer.sortingLayerID,renderer.sortingOrder,renderer.maskInteraction));
                renderer.sortingLayerID=grid[0].icon.sortingLayerID;renderer.sortingOrder=order++;renderer.maskInteraction=SpriteMaskInteraction.None;
            }
            foreach(var text in new[]{mouse.amountGrabbedNumberOutline,mouse.amountGrabbedNumber}) {
                if(text==null)continue;
                if(!cursorTextStyles.ContainsKey(text))cursorTextStyles.Add(text,text.style.GetCopy());
                text.style.sortingLayer=grid[0].icon.sortingLayerID;text.style.orderInLayer=order++;text.style.maskInteraction=SpriteMaskInteraction.None;
                foreach(var renderer in text.GetComponentsInChildren<Renderer>(true)){renderer.sortingLayerID=text.style.sortingLayer;renderer.sortingOrder=text.style.orderInLayer;}
            }
        }
        private void RestoreCursor() {
            foreach(var entry in cursorLayers)if(entry.Key!=null){entry.Key.sortingLayerID=entry.Value.layer;entry.Key.sortingOrder=entry.Value.order;entry.Key.maskInteraction=entry.Value.mask;}
            foreach(var entry in cursorTextStyles)if(entry.Key!=null){entry.Key.style=entry.Value;entry.Key.Render(entry.Key.displayedTextString,false,true,false);}
            cursorLayers.Clear();cursorTextStyles.Clear();
        }
        private void Awake() { Instance=this; StorageUIDev.Init(); }
        private void OnDestroy() { Close(); if(screen!=null) Destroy(screen); if(Instance==this) Instance=null; }
        private Transform Find(string name) => screen.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
        public static void Open(Chest chest)
        {
            if(Instance==null || Prefab==null) { Debug.LogError("[ConnectedStorage] Browser layout has not loaded."); return; }
            Close(); StorageTerminalUI.Close();
            Manager.ui.TryHideAllInventoryAndCraftingUI();
            var self=Instance;
            try {
                self.terminal=chest;self.cachedNetwork=null;self.nextNetworkScan=0;
                self.bagPosition=Manager.ui.playerInventoryUI.transform.position;
                self.barPosition=Manager.ui.itemSlotsBar.transform.position;
                if(self.screen==null) self.Build();
                Manager.ui.mouse.ReleaseGrabbedItemBackToInventory();
                // InventoryUI already includes its own hotbar row. The gameplay
                // bar normally hides in Update, which may have run before Use.
                Manager.ui.itemSlotsBar.HideContainerUI();
                Manager.ui.playerInventoryUI.ShowContainerUI();
                self.ApplyLayout();
                self.screen.SetActive(true);
                self.search.SetInputText(""); self.lastSearch="";
                if(!self.tabsPlaced)self.PlaceTabs();
                self.Refresh(); self.Configure(); self.nextScan=Time.unscaledTime+0.1f;
                StorageUIDev.Opened();
                Debug.Log("[ConnectedStorage] Browser opened; camera depth="+Manager.ui.UICamera.transform.position.z+", panel depth="+self.screen.transform.position.z);
            } catch(Exception ex) { Debug.LogError("[ConnectedStorage] Browser open: "+ex); Close(); }
        }
        public static void Close(string reason="explicit close")
        {
            var self=Instance;
            if(self==null || self.terminal==null || self.closing) return;
            Debug.Log("[ConnectedStorage] Browser closed: "+reason);
            self.closing=true; self.terminal=null; self.pendingCursorSlot=null;
            self.RestoreCursor();
            self.cachedNetwork=null;
            if(self.search!=null && self.search.inputIsActive) self.search.Deactivate(false);
            if(self.screen!=null) self.screen.SetActive(false);
            if(Manager.ui!=null) {
                Manager.ui.mouse.ReleaseGrabbedItemBackToInventory();
                Manager.ui.playerInventoryUI.transform.position=self.bagPosition;
                Manager.ui.itemSlotsBar.transform.position=self.barPosition;
                Manager.ui.TryHideAllInventoryAndCraftingUI();
            }
            self.closing=false;
        }
        private void Build()
        {
            screen=Instantiate(Prefab,Manager.ui.UICamera.transform,false);
            screen.SetActive(false);
            Find("TabsAnchor").gameObject.SetActive(false);
            foreach(var t in screen.GetComponentsInChildren<Transform>(true))
                if(t.name=="FiltersPanel" || t.name=="FakeScrollBar" || t.name=="Toggled") t.gameObject.SetActive(false);
            search=Find("StorageSearch").GetComponent<TextInputField>();
            search.hintString="Search";
            search.hintText.localize=false;
            search.hintText.Render("Search",false,true);
            search.dontAllowNewLines=true;
            search.onInputFieldDone=new UnityEvent();
            var metrics=Find("StorageMetrics").localPosition;
            spread=metrics.z;
            scroll=Find("StorageGrid").GetComponent<UIScrollWindow>();
            model=new StorageBrowserModel<Address>((int)metrics.x,Math.Max(1,Mathf.FloorToInt(scroll.windowHeight/spread)));
            var scrollable=scroll.gameObject.AddComponent<StorageBrowserScroll>();
            scroll.scrollable=scrollable;
            scroll.cursorMustBeInsideWindowToScroll=true;
            emptyMessage=Instantiate(search.hintText,screen.transform,false);
            emptyMessage.style.maskInteraction=SpriteMaskInteraction.None;
            emptyMessage.localize=false;
            emptyMessage.transform.localPosition=new Vector3(-5.4f,2.5f,0);
            emptyMessage.maxWidth=10;
            // Slot count sits right-aligned inside the search bar, clear of the HUD bars above the panel.
            heading=Instantiate(emptyMessage,screen.transform,false);
            heading.transform.localPosition=new Vector3(1.9f,3.88f,0);
            heading.style.horizontalAlignment=PugTextStyle.HorizontalAlignment.right;
            heading.maxWidth=4;
            heading.gameObject.SetActive(true);
            emptyMessage.gameObject.SetActive(false);
            CreateSlots("StorageGridSlot",grid,model.GridColumns*model.ViewportRows);
            CreateSlots("StorageListSlot",list,model.ViewportRows);
            Bind("StorageLayout",()=>{layout=layout==BrowserLayout.Grid?BrowserLayout.List:BrowserLayout.Grid;Configure();},"Grid / list");
            Bind("StorageSort",()=>Send("sort "+(descending?1:0)),"Sort storage by category, then name");
            Bind("StorageOrder",()=>{descending=!descending;Send("sort "+(descending?1:0));},"Sort ascending / descending");
            // Category tabs replace the old Filter popup: one click per category.
            Find("StorageFilters").gameObject.SetActive(false);
            BuildTabs();
        }
        private const float TabSize=.9f;
        private void BuildTabs() {
            tabColumn=new GameObject("Storage category tabs");tabColumn.transform.SetParent(screen.transform,false);
            var template=Find("StorageFilters").gameObject;
            for(int i=0;i<Categories.Length;i++) {
                int selected=i;
                // Clone the working toolbar button so its input layer, collider and
                // initialized native button fields exactly match the game's UI.
                var go=Instantiate(template,tabColumn.transform,false);
                go.SetActive(true);
                go.name="Category "+Categories[i];
                go.transform.localScale=Vector3.one;
                foreach(var text in go.GetComponentsInChildren<PugText>(true))Destroy(text.gameObject);
                // The button has an Icon in its normal state and another inside
                // Toggled (its selected state); both get the category's item.
                GameObject toggled=null;var icons=new List<SpriteRenderer>();
                foreach(var t in go.GetComponentsInChildren<Transform>(true)) {
                    if(t.name=="IconShadow")t.gameObject.SetActive(false);
                    if(t.name=="Toggled"){toggled=t.gameObject;toggled.SetActive(false);}
                    if(t.name=="Icon" && t.TryGetComponent<SpriteRenderer>(out var r))icons.Add(r);
                }
                tabToggles.Add(toggled);tabIcons.Add(icons);
                var collider=go.GetComponent<BoxCollider>();
                collider.enabled=true;collider.isTrigger=true;collider.center=Vector3.zero;collider.size=new Vector3(TabSize,TabSize,1);
                var button=go.GetComponent<ButtonUIElement>();
                button.canBeClicked=true;button.adjustSpritesToFitTextSize=false;
                button.onLeftClick=new UnityEvent();button.onRightClick=new UnityEvent();
                button.onSelected=new UnityEvent();button.onDeselected=new UnityEvent();
                button.optionToSelectOnHover=null;
                button.onLeftClick.AddListener(()=>SelectCategory(selected));
                button.showHoverTitle=true;button.showHoverDesc=false;
                go.AddComponent<StorageBrowserTooltip>().Title=Categories[i];
                tabButtons.Add(button);
                // Active tab: a gold frame drawn over the brown one. It copies the
                // tab's own frame renderer (known to draw here) and swaps in the
                // item slot's light hover border, which tints cleanly to gold.
                var background=go.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.name=="Background");
                var border=grid[0].hoverBorder!=null?grid[0].hoverBorder.sprite:null;
                GameObject highlight=null;
                if(background!=null && border!=null) {
                    highlight=Instantiate(background.gameObject,background.transform.parent,false);
                    highlight.name="Active highlight";
                    foreach(Transform child in highlight.transform)Destroy(child.gameObject);
                    var frame=highlight.GetComponent<SpriteRenderer>();
                    var size=background.drawMode==SpriteDrawMode.Simple?(Vector2)background.sprite.bounds.size:background.size;
                    frame.sprite=border;frame.color=Gold;frame.sortingOrder=background.sortingOrder+1;
                    if(border.border!=Vector4.zero){frame.drawMode=SpriteDrawMode.Sliced;frame.size=size;}
                    else {
                        frame.drawMode=SpriteDrawMode.Simple;
                        var native=(Vector2)border.bounds.size;var scale=background.transform.localScale;
                        highlight.transform.localScale=new Vector3(scale.x*size.x/native.x,scale.y*size.y/native.y,scale.z);
                    }
                    foreach(var icon in icons)if(icon.sortingOrder<=frame.sortingOrder)icon.sortingOrder=frame.sortingOrder+1;
                    highlight.SetActive(false);
                }
                tabHighlights.Add(highlight);
                if(i==0)Debug.Log("[ConnectedStorage] Tab highlight: "+(highlight!=null?"gold frame, sliced="+(border.border!=Vector4.zero):"unavailable (background="+(background!=null)+", border="+(border!=null)+")"));
                // If an item icon fails to load, the tab keeps the native filter icon.
                // Sizing happens in PlaceTabs, matched to the grid's item icons.
                var sprite=PugDatabase.GetObjectInfo(CategoryIcons[i])?.icon;
                if(sprite==null)continue;
                foreach(var icon in icons) {
                    icon.sprite=sprite;
                    icon.transform.localPosition=new Vector3(0,0,icon.transform.localPosition.z);
                }
            }
            UpdateTabs();
        }
        // Runs on the first open, when the panel's renderers have real bounds:
        // a column hugging the panel's left edge, spread over its height.
        private void PlaceTabs() {
            bool first=true;var bounds=new Bounds();
            foreach(var sr in screen.GetComponentsInChildren<SpriteRenderer>()) {
                if(!sr.enabled || sr.sprite==null || sr.transform.IsChildOf(tabColumn.transform))continue;
                if(first){bounds=sr.bounds;first=false;}else bounds.Encapsulate(sr.bounds);
            }
            if(first)return;
            var low=screen.transform.InverseTransformPoint(bounds.min);var high=screen.transform.InverseTransformPoint(bounds.max);
            float step=Mathf.Min(1.05f,(high.y-low.y)/Categories.Length);
            for(int i=0;i<tabButtons.Count;i++)
                tabButtons[i].transform.localPosition=new Vector3(low.x-TabSize*.5f-.1f,high.y-step*(i+.5f),-.2f);
            // Item sprites have uneven padding (tools and weapons are mostly empty
            // canvas), so match the grid's icon scale instead of the sprite bounds.
            float slotScale=grid[0].icon.transform.lossyScale.x*.72f;
            for(int i=0;i<tabIcons.Count;i++)
                foreach(var icon in tabIcons[i])
                    if(icon.sprite==PugDatabase.GetObjectInfo(CategoryIcons[i])?.icon && icon.transform.parent.lossyScale.x>0)
                        icon.transform.localScale=Vector3.one*(slotScale*IconScale(i)/icon.transform.parent.lossyScale.x);
            Physics.SyncTransforms();
            tabsPlaced=true;
        }
        // Inactive tabs are dimmed; the active one is full brightness on the
        // native selected background. Re-applied each frame in case the button
        // tints its sprites on hover.
        private static readonly Color DimTab=new Color(.5f,.5f,.5f,1);
        private void UpdateTabs() {
            for(int i=0;i<tabToggles.Count;i++) {
                // The native Toggled state (an underline) stays off; the gold frame replaces it.
                if(tabToggles[i]!=null && tabToggles[i].activeSelf)tabToggles[i].SetActive(false);
                if(tabHighlights[i]!=null && tabHighlights[i].activeSelf!=(i==filter))tabHighlights[i].SetActive(i==filter);
                var tint=i==filter||i==hoveredTab?Color.white:DimTab;
                foreach(var icon in tabIcons[i])if(icon.color!=tint)icon.color=tint;
            }
        }
        private void SelectCategory(int selected) {
            // The button's own click and the pointer fallback can both fire in one frame.
            if(!Active || Time.frameCount==tabSelectedFrame)return;
            tabSelectedFrame=Time.frameCount;
            // Clicking the active tab again goes back to everything.
            filter=selected==filter?0:selected;
            Refresh();Configure();UpdateTabs();
        }
        private void UpdateTabPointer() {
            int hovered=-1;
            var pointer=Manager.ui.mouse.pointer.position;
            for(int i=0;i<tabButtons.Count;i++) {
                var button=tabButtons[i];
                var collider=button.GetComponent<BoxCollider>();
                var local=button.transform.InverseTransformPoint(pointer)-collider.center;
                if(Mathf.Abs(local.x)<=collider.size.x*.5f && Mathf.Abs(local.y)<=collider.size.y*.5f) {
                    hovered=i;break;
                }
            }
            if(hovered!=hoveredTab) {
                if(hoveredTab>=0)tabButtons[hoveredTab].OnDeselected(false);
                hoveredTab=hovered;
                if(hovered>=0)tabButtons[hovered].OnSelected();
            }
            if(hovered>=0 && Manager.input.singleplayerInputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.UI_INTERACT))
                SelectCategory(hovered);
            UpdateTabs();
        }
        private void Bind(string name,Action action,string title)
        {
            var button=Find(name).GetComponent<ButtonUIElement>();
            button.onLeftClick=new UnityEvent(); button.onLeftClick.AddListener(()=>action());
            button.onRightClick=new UnityEvent();
            button.showHoverTitle=true; button.showHoverDesc=false;
            button.gameObject.AddComponent<StorageBrowserTooltip>().Title=title;
        }
        private void CreateSlots(string templateName,List<StorageBrowserItem> slots,int count)
        {
            var template=Find(templateName).GetComponent<SlotUIBase>();
            for(int i=0;i<count;i++) {
                var clone=Instantiate(template,scroll.scrollingContent.GetChild(0),false);
                clone.gameObject.SetActive(false);
                var item=clone.gameObject.AddComponent<StorageBrowserItem>();
                item.icon=clone.icon; item.background=clone.background; item.darkBackground=clone.darkBackground;
                item.hoverBorder=clone.hoverBorder; item.activeBorder=clone.activeBorder;
                item.amountNumber=clone.amountNumber; item.amountNumberShadow=clone.amountNumberShadow;
                item.overlayIcon=clone.overlayIcon; item.underlayIcon=clone.underlayIcon;
                item.upgradeIcon=clone.upgradeIcon; item.upgradeSprites=clone.upgradeSprites;
                item.Window=scroll;
                if(item.amountNumber==null) {
                    var native=Manager.ui.playerInventoryUI.itemSlots.FirstOrDefault(x=>x.amountNumber!=null);
                    if(native!=null) {
                        item.amountNumber=Instantiate(native.amountNumber,item.transform,false);
                        item.amountNumber.transform.localPosition=new Vector3(.5f,-.4f,-.2f);
                        item.amountNumber.gameObject.SetActive(true);
                    }
                }
                item.NameText=clone.GetComponentsInChildren<PugText>(true).FirstOrDefault(t=>t.name=="NameText");
                foreach(var t in clone.GetComponentsInChildren<Transform>(true))
                    if(t.name=="FavoritedBorder" || t.name=="MissingIcon" || t.name=="PlusIcon" || t.name=="SortingValueText") t.gameObject.SetActive(false);
                Destroy(clone); slots.Add(item);
            }
        }
        private void Refresh()
        {
            var em=terminal.world.EntityManager;
            var entries=new List<StorageBrowserModel<Address>.Entry>();
            used=capacity=0;
            if(cachedNetwork==null || Time.unscaledTime>=nextNetworkScan) {
                cachedNetwork=StorageNetwork.Scan(terminal.world,terminal.entity);
                if(expansionWorld!=terminal.world) {
                    expansionWorld=terminal.world;
                    expansions=new StorageExpansionTracker<int2>();
                }
                var memberTiles=new List<int2>();
                foreach(var unit in cachedNetwork.Chests) memberTiles.Add(StorageNetwork.Tile(em,unit));
                if(expansions.Observe(memberTiles)) Send("sort "+(descending?1:0));
                nextNetworkScan=Time.unscaledTime+2f;
            }
            foreach(var entity in cachedNetwork.Chests) {
                if(!em.Exists(entity) || !em.HasBuffer<ContainedObjectsBuffer>(entity)) {nextNetworkScan=0;continue;}
                var items=em.GetBuffer<ContainedObjectsBuffer>(entity);
                int limit=StorageCatalog.Capacity(em.GetComponentData<ObjectDataCD>(entity).objectID);
                capacity+=limit;
                int occupied=0;
                for(int n=0;n<items.Length;n++)if(items[n].objectID!=ObjectID.None)occupied++;
                int emptyBudget=Math.Max(0,limit-occupied);
                for(int i=0;i<items.Length;i++) {
                    var item=items[i]; bool empty=item.objectID==ObjectID.None;
                    if(empty && (i>=limit || emptyBudget--<=0))continue;
                    if(!empty) used++;
                    if(filter!=0 && (empty || Category(PugDatabase.GetObjectInfo(item.objectID,item.variation))!=filter)) continue;
                    entries.Add(new StorageBrowserModel<Address>.Entry {
                        Address=new Address {Entity=entity,Index=i,Item=item},
                        Name=empty ? "" : PlayerController.GetObjectName(item,true)?.text ?? item.objectID.ToString(),Amount=item.amount,Empty=empty
                    });
                }
            }
            model.SetEntries(entries); Draw();
        }
        private void Configure()
        {
            model.Configure(lastSearch,sort,descending,layout); scroll.ResetScroll(); Draw();
        }
        private void LateUpdate()
        {
            if(!Active) { StorageUIDev.Tick(screen); return; }
            var p=Manager.main?.player; var em=terminal.world.EntityManager;
            if(p==null || !em.Exists(terminal.entity) || Manager.menu.IsAnyMenuActive()
                || math.distancesq(p.WorldPosition,em.GetComponentData<LocalTransform>(terminal.entity).Position)>9f)
            { Close("player/terminal unavailable, menu opened, or out of range"); return; }
            if(cachedNetwork!=null && cachedNetwork.Terminals.Count==0) { Close("network has no terminal"); return; }
            if(!search.inputIsActive && Input.GetKeyDown(KeyCode.Escape)) { Close("Escape"); return; }
            var next=search.GetInputText()??"";
            if(Manager.ui.currentSelectedUIElement==search && Input.GetMouseButtonDown(1)) {search.SetInputText("");next="";}
            if(next!=lastSearch) {lastSearch=next;Configure();}
            // Only watch the clicked slot while awaiting its server update (at most one second).
            // Avoid waiting for the 100 ms list refresh after the mouse inventory has updated.
            bool cursorSlotChanged=false;
            if(pendingCursorSlot!=null) {
                var a=pendingCursorSlot;
                if(Time.unscaledTime>=pendingCursorUntil || !em.Exists(a.Entity) || !em.HasBuffer<ContainedObjectsBuffer>(a.Entity)) pendingCursorSlot=null;
                else {
                    var contents=em.GetBuffer<ContainedObjectsBuffer>(a.Entity);
                    if(a.Index<contents.Length) {
                        var item=contents[a.Index];
                        cursorSlotChanged=item.objectID!=a.Item.objectID || item.variation!=a.Item.variation || item.amount!=a.Item.amount || item.auxDataIndex!=a.Item.auxDataIndex;
                    }
                    if(cursorSlotChanged)pendingCursorSlot=null;
                }
            }
            if(cursorSlotChanged || Time.unscaledTime>=nextScan) {Refresh();nextScan=Time.unscaledTime+0.1f;}
            // Hide the count once a long search would run into it.
            heading.gameObject.SetActive(lastSearch.Length<=12);
            if(lastSearch.Length<=12) heading.Render(used+" / "+capacity);
            ApplyLayout();
            UpdateTabPointer();
            StorageUIDev.Tick(screen);
        }
        private void ApplyLayout()
        {
            RaiseCursor();
            var camera=Manager.ui.UICamera.transform;
            var scale=Manager.ui.CalcGameplayUITargetScaleMultiplier();
            // A child at local z=0 sits on the camera, outside its clipping range.
            // Use the genuine inventory's UI plane, including before the first render.
            var bagDepth=camera.InverseTransformPoint(bagPosition).z;
            screen.transform.localScale=scale*StorageUIDev.PanelScale;
            heading.transform.localScale=Vector3.one*StorageUIDev.TextScale;
            emptyMessage.transform.localScale=Vector3.one*StorageUIDev.TextScale;
            var browser=StorageUIDev.Browser; var bag=StorageUIDev.Bag;
            screen.transform.localPosition=new Vector3(browser.x*scale.x,browser.y*scale.y,bagDepth);
            // Keep the player's genuine inventory, separated beside the browser.
            Manager.ui.playerInventoryUI.transform.position=camera.TransformPoint(new Vector3(bag.x*scale.x,(bag.y+3f)*scale.y,bagDepth));
            FitInventoryToScreen();
            // Do not reposition the separate gameplay hotbar. Suppress it in
            // the same frame, regardless of native Update ordering.
            Manager.ui.itemSlotsBar.HideContainerUI();
        }
        private void Draw()
        {
            bool hasItems=model.TryGetVisible(0,out var first);
            emptyMessage.gameObject.SetActive(!hasItems);
            if(!hasItems) emptyMessage.Render(used==0 ? "Storage is empty.\nPick up an inventory stack, then click an empty slot." : "No items match this search or filter.");
            var active=layout==BrowserLayout.Grid?grid:list;
            foreach(var slot in layout==BrowserLayout.Grid?list:grid) slot.gameObject.SetActive(false);
            for(int i=0;i<active.Count;i++) {
                var slot=active[i]; bool valid=model.TryGetVisible(i,out var entry);
                slot.gameObject.SetActive(valid); if(!valid) continue;
                slot.Address=entry.Address; slot.visibleSlotIndex=i;
                // Slot pool is repositioned into the scroll window's visible rows.
                slot.transform.localPosition=new Vector3(layout==BrowserLayout.Grid?(-(model.Columns-1)*0.5f+i%model.Columns)*spread:0,
                    -(model.FirstRow+i/model.Columns)*spread,0);
                slot.UpdateSlot();
            }
        }
        internal static float Height => Active?Instance.model.TotalRows*Instance.spread:0;
        internal static void Scroll(float value) { if(!Active)return;Instance.model.ScrollToRow(Mathf.FloorToInt(value/Instance.spread));Instance.Draw(); }
        internal static void Withdraw(Address a,bool one)
        {
            if(!Active || a==null) return;
            var pos=StorageNetwork.Tile(Instance.terminal.world.EntityManager,a.Entity);
            Instance.Send("take "+pos.x+" "+pos.y+" "+a.Index+" "+(int)a.Item.objectID+" "+a.Item.variation+" "+(one?1:0));
        }
        internal static void CursorClick(Address a,bool one,int mode=0)
        {
            if(!Active || a==null)return;
            var pos=StorageNetwork.Tile(Instance.terminal.world.EntityManager,a.Entity);
            var held=Manager.main.player.mouseInventoryHandler.GetObjectData(0);
            Instance.pendingCursorSlot=new Address {Entity=a.Entity,Index=a.Index,Item=a.Item};
            Instance.pendingCursorUntil=Time.unscaledTime+1f;
            Instance.Send("cursor "+pos.x+" "+pos.y+" "+a.Index+" "+(int)a.Item.objectID+" "+a.Item.variation+" "+(int)held.objectID+" "+held.variation+" "+(mode!=0?mode:one?1:0));
        }
        internal static bool QuickMoveHeld => Manager.input.singleplayerInputModule.IsButtonCurrentlyDown(PlayerInput.InputType.QUICK_MOVE_ITEMS);
        internal static bool QuickAction(bool depositAll)
        {
            if(!Active)return false;
            // One explicit click, one authoritative command. Never send the
            // vanilla nearby-chest request as well: it bypasses network capacity.
            Instance.Send(depositAll?"deposit":"stack");
            return true;
        }
        internal static bool IsQuickActionButton(ButtonUIElement button)
        {
            if(!Active || button==null)return false;
            var inventory=Manager.ui.playerInventoryUI;
            if(inventory is InventoryUI native &&
                (button==native.quickStackToNearbyButton || button==native.optionalQuickStackButton))return true;
            // Some inventory layouts display a separately wired button rather
            // than either InventoryUI field. Match its native action and owner,
            // never its translated label or a generic button name.
            var action=button.onLeftClick;
            if(action==null)return false;
            for(int i=0;i<action.GetPersistentEventCount();i++) {
                if(action.GetPersistentTarget(i)!=inventory)continue;
                string method=action.GetPersistentMethodName(i);
                if(method==nameof(ItemSlotsUIContainer.QuickStackToNearbyChests) ||
                    method==nameof(ItemSlotsUIContainer.QuickStack))return true;
            }
            return false;
        }
        internal static bool Deposit(InventorySlotUI slot,bool quick)
        {
            if(!Active || !quick || (!slot.isPlayerInventorySlot && !slot.isPlayerPouchSlot))return false;
            if(Manager.main.player.mouseInventoryHandler.HasObject(0)) return false;
            var item=slot.GetObjectData();
            if(item.objectID!=ObjectID.None) Instance.Send("put "+(slot.GetInventoryHandler().startPosInBuffer+slot.inventorySlotIndex)+" "+(int)item.objectID+" "+item.variation);
            return true;
        }
        private void Send(string command)
        {
            var pos=StorageNetwork.Tile(terminal.world.EntityManager,terminal.entity);
            CommandModule.ClientCommSystem?.SendCommand("/connectedstorage "+pos.x+" "+pos.y+" "+command,CommandFlags.None);
        }
    }
    public sealed class StorageBrowserScroll : UIelement,IScrollable
    {
        public override bool isShowing=>StorageBrowserUI.Active;
        public void UpdateContainingElements(float value)=>StorageBrowserUI.Scroll(value);
        public float GetCurrentWindowHeight()=>StorageBrowserUI.Height;
        public bool IsBottomElementSelected()=>false;
        public bool IsTopElementSelected()=>false;
    }
    public sealed class StorageBrowserTooltip : MonoBehaviour { public string Title; }
    [HarmonyPatch(typeof(ButtonUIElement),nameof(ButtonUIElement.GetHoverTitle))]
    public static class BrowserTooltipPatch
    {
        public static bool Prefix(ButtonUIElement __instance,ref TextAndFormatFields __result)
        {
            if(StorageBrowserUI.IsQuickActionButton(__instance)) {
                __result=new TextAndFormatFields {text="Quick stack to storage (left-click) / Deposit bag (right-click)",dontLocalize=true};return false;
            }
            if(!StorageBrowserUI.Active || !__instance.TryGetComponent<StorageBrowserTooltip>(out var hint))return true;
            __result=new TextAndFormatFields {text=hint.Title,dontLocalize=true};return false;
        }
    }
    [HarmonyPatch(typeof(ItemSlotsUIContainer),nameof(ItemSlotsUIContainer.QuickStackToNearbyChests))]
    public static class BrowserQuickStackNearbyPatch
    {
        public static bool Prefix(ItemSlotsUIContainer __instance) =>
            __instance!=Manager.ui.playerInventoryUI || !StorageBrowserUI.QuickAction(false);
    }
    [HarmonyPatch(typeof(ItemSlotsUIContainer),nameof(ItemSlotsUIContainer.QuickStack))]
    public static class BrowserQuickStackPatch
    {
        public static bool Prefix() => !StorageBrowserUI.QuickAction(false);
    }
    [HarmonyPatch(typeof(ButtonUIElement),nameof(ButtonUIElement.OnRightClicked))]
    public static class BrowserDepositBagPatch
    {
        public static bool Prefix(ButtonUIElement __instance) =>
            !StorageBrowserUI.IsQuickActionButton(__instance) || !__instance.canBeClicked || !StorageBrowserUI.QuickAction(true);
    }
    public sealed class StorageBrowserItem : SlotUIBase
    {
        public StorageBrowserUI.Address Address;
        public UIScrollWindow Window;
        public PugText NameText;
        private bool hasRendered;
        private ContainedObjectsBuffer renderedItem;
        private void OnEnable() { hasRendered=false; }
        protected override void Awake() { OnDeselectSlot(); if(activeBorder!=null) activeBorder.gameObject.SetActive(false); }
        protected override void LateUpdate() { base.LateUpdate(); transform.localScale=Vector3.one; }
        public override void OnSelected()=>OnSelectSlot();
        public override void OnDeselected(bool playEffect=true)=>OnDeselectSlot();
        public override float localScrollPosition=>Window==null?0:transform.localPosition.y+transform.parent.localPosition.y;
        public override bool isVisibleOnScreen=>base.isVisibleOnScreen && (Window==null || Window.IsShowingPosition(localScrollPosition));
        protected override ContainedObjectsBuffer GetSlotObject()=>Address?.Item ?? default;
        public override UIScrollWindow uiScrollWindow=>Window;
        public override void OnLeftClicked(bool mod1,bool mod2) { if(StorageBrowserUI.QuickMoveHeld)StorageBrowserUI.Withdraw(Address,false);else StorageBrowserUI.CursorClick(Address,false); }
        public override void OnRightClicked(bool mod1,bool mod2)=>StorageBrowserUI.CursorClick(Address,true,mod2?2:mod1?3:1);
        public override void UpdateSlot()
        {
            if(Address==null)return;
            var item=Address.Item;
            if(hasRendered && item.objectID==renderedItem.objectID && item.variation==renderedItem.variation
                && item.amount==renderedItem.amount && item.auxDataIndex==renderedItem.auxDataIndex)return;
            renderedItem=item;hasRendered=true;
            var info=Address.Item.objectID==ObjectID.None ? null : PugDatabase.GetObjectInfo(Address.Item.objectID,Address.Item.variation);
            icon.sprite=info?.icon; icon.enabled=icon.sprite!=null;
            if(amountNumber!=null) {
                amountNumber.style.sortingLayer=icon.sortingLayerID;
                amountNumber.style.orderInLayer=60;
                amountNumber.style.maskInteraction=SpriteMaskInteraction.None;
                string count=info!=null && info.isStackable && Address.Item.amount>1 ? Address.Item.amount.ToString():"";
                amountNumber.Render(count,false,true,true);
            }
            UpdateUpgradeIcon(Address.Item);
            if(NameText!=null) NameText.Render(PlayerController.GetObjectName(Address.Item,true)?.text??"");
        }
    }
    [HarmonyPatch(typeof(InventorySlotUI),nameof(InventorySlotUI.OnLeftClicked))]
    public static class BrowserDepositPatch
    { public static bool Prefix(InventorySlotUI __instance,bool mod1)=>!StorageBrowserUI.Deposit(__instance,StorageBrowserUI.QuickMoveHeld); }
    [HarmonyPatch(typeof(InventorySlotUI),nameof(InventorySlotUI.TryToSendItemToOtherInventoryOrEquip))]
    public static class BrowserQuickMovePatch {
        public static bool Prefix(InventorySlotUI __instance,ref bool __result) {
            if(!StorageBrowserUI.Deposit(__instance,true))return true;
            __result=true;return false;
        }
    }
    [HarmonyPatch(typeof(UIManager),nameof(UIManager.TryHideAllInventoryAndCraftingUI))]
    public static class BrowserClosePatch
    { public static void Postfix(bool __result) { if(__result) StorageBrowserUI.Close("native inventory close"); } }
    [HarmonyPatch(typeof(SendClientInputSystem),"PlayerInteractionBlocked")]
    public static class BrowserInteractionPatch
    { public static void Postfix(ref bool __result) { if(StorageBrowserUI.Active) __result=true; } }
}

