using System;
using System.Collections.Generic;
using System.Linq;
using ConnectedStorage;
using ConnectedStorage.Browser;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

namespace ItemSpawner
{
    /// <summary>One spawnable item: the database key plus what the panel needs to filter and level it.</summary>
    public sealed class SpawnEntry
    {
        public ObjectID Id;
        public int Variation;
        public int Category;
        public bool Stackable;
        public bool Upgradeable;
        public int BaseLevel;
    }

    // Runic Storage's browser panel (vanilla MonoBehaviours only) bound to the item catalogue instead of a network.
    [DefaultExecutionOrder(10000)]
    public sealed class SpawnerUI : MonoBehaviour
    {
        public static SpawnerUI Instance;
        public static GameObject Prefab;
        public static bool Active => Instance != null && Instance.open;
        public static bool Typing => Active && Instance.search != null && Instance.search.inputIsActive;
        public const int MaxStack = 9999;

        private static readonly string[] Categories={"All","Materials","Weapons","Tools","Armor","Accessories","Food","Blocks","Furniture","Pets","Valuables","Other"};
        private static readonly ObjectID[] CategoryIcons={ObjectID.InventoryChest,ObjectID.CopperBar,ObjectID.CopperSword,ObjectID.CopperMiningPick,
            ObjectID.CopperHelm,ObjectID.CopperCrossNecklace,ObjectID.HeartBerry,ObjectID.GroundDirtBlock,ObjectID.WoodenWorkBench,
            ObjectID.PetCatEgg,ObjectID.AncientCoin,ObjectID.CopperKey};
        private static readonly Vector2 BrowserPos=new Vector2(-7.5f,.5f), BagPos=new Vector2(6.75f,-1f);
        private static readonly Color Gold=new Color(1f,.8f,.25f,1), DimTab=new Color(.5f,.5f,.5f,1);
        private const float TabSize=.9f;

        private bool open, closing;
        private GameObject screen;
        private TextInputField search;
        private UIScrollWindow scroll;
        private StorageBrowserModel<SpawnEntry> model;
        private readonly List<SpawnerSlot> grid=new List<SpawnerSlot>();
        private readonly List<SpawnerSlot> list=new List<SpawnerSlot>();
        private BrowserLayout layout;
        private float spread;
        private int filter;
        private string lastSearch="";
        private PugText heading, emptyMessage;
        private Vector3 bagPosition, barPosition;
        private List<SpawnEntry> catalogue;
        private readonly Dictionary<SpawnEntry,string> names=new Dictionary<SpawnEntry,string>();

        // Levels added on top of each gear/tool's base level (0 = base), capped at the game's max level.
        private static int bonus;
        private float nextStackSpawn;

        private GameObject tabColumn;
        private readonly List<ButtonUIElement> tabButtons=new List<ButtonUIElement>();
        private readonly List<GameObject> tabToggles=new List<GameObject>();
        private readonly List<List<SpriteRenderer>> tabIcons=new List<List<SpriteRenderer>>();
        private readonly List<GameObject> tabHighlights=new List<GameObject>();
        private int hoveredTab=-1, tabSelectedFrame=-1;
        private bool tabsPlaced;
        private readonly Dictionary<SpriteRenderer,(int layer,int order,SpriteMaskInteraction mask)> cursorLayers=new Dictionary<SpriteRenderer,(int,int,SpriteMaskInteraction)>();
        private readonly Dictionary<PugText,PugTextStyle> cursorTextStyles=new Dictionary<PugText,PugTextStyle>();

        private void Awake() { Instance=this; }
        private void OnDestroy() { Close(); if(screen!=null) Destroy(screen); if(Instance==this) Instance=null; }
        private Transform Find(string name) => screen.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);

        internal static bool CanSpawn
        {
            get {
                var p=Manager.main?.player;
                return p!=null && (p.adminPrivileges>=1 || Manager.saves.IsCreativeModeCharacter());
            }
        }

        public static void Open()
        {
            if(Instance==null || Prefab==null) return;
            var self=Instance;
            Manager.ui.TryHideAllInventoryAndCraftingUI();
            try {
                self.bagPosition=Manager.ui.playerInventoryUI.transform.position;
                self.barPosition=Manager.ui.itemSlotsBar.transform.position;
                if(self.screen==null) self.Build();
                if(self.catalogue==null) self.BuildCatalogue();
                Manager.ui.itemSlotsBar.HideContainerUI();
                Manager.ui.playerInventoryUI.ShowContainerUI();
                self.open=true;
                self.ApplyLayout();
                self.screen.SetActive(true);
                self.search.SetInputText(""); self.lastSearch="";
                if(!self.tabsPlaced) self.PlaceTabs();
                self.Refresh();
                Debug.Log($"[{ItemSpawnerMod.Name}] Opened; {self.catalogue.Count} items, admin={Manager.main.player.adminPrivileges}, can spawn={CanSpawn}.");
            } catch(Exception ex) { Debug.LogError($"[{ItemSpawnerMod.Name}] Open: "+ex); Close(); }
        }

        public static void Close(string reason="explicit close")
        {
            var self=Instance;
            if(self==null || !self.open || self.closing) return;
            self.closing=true; self.open=false;
            self.RestoreCursor();
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

        // ---------------------------------------------------------------- catalogue

        private void BuildCatalogue()
        {
            catalogue=new List<SpawnEntry>();
            var seen=new HashSet<(ObjectID,Sprite)>();
            foreach(var pair in PugDatabase.objectsByType.OrderBy(p=>p.Key.objectID).ThenBy(p=>p.Key.variation)) {
                var key=pair.Key; var info=pair.Value;
                if(key.objectID==ObjectID.None || info==null || info.icon==null) continue;
                if(info.objectType==ObjectType.NonObtainable || info.objectType==ObjectType.Creature || info.objectType==ObjectType.PlayerType) continue;
                // Variations that only differ internally share an icon; list each look once.
                if(!seen.Add((key.objectID,info.icon))) continue;
                bool upgradeable=info.tags!=null && info.tags.Contains(ObjectCategoryTag.CanBeUpgraded);
                if(upgradeable && key.variation!=0) continue; // for gear the variation IS the level
                var entry=new SpawnEntry {
                    Id=key.objectID, Variation=key.variation, Category=StorageBrowserUI.Category(info),
                    Stackable=info.isStackable, Upgradeable=upgradeable, BaseLevel=BaseLevelOf(key,info)
                };
                string name=PlayerController.GetObjectName(new ContainedObjectsBuffer { objectData=new ObjectDataCD { objectID=key.objectID, amount=1, variation=key.variation } },true)?.text;
                names[entry]=string.IsNullOrWhiteSpace(name) ? key.objectID.ToString() : name;
                catalogue.Add(entry);
            }
        }

        // The prefab's LevelCD is the level vanilla upgrades from; ObjectInfo.level is often unset.
        private static int BaseLevelOf(ObjectDataCD key,ObjectInfo info)
        {
            if(PugDatabase.TryGetComponent<LevelCD>(key,out var levelCD) && levelCD.level>0) return levelCD.level;
            return Math.Max(1,info.level);
        }

        // Base level + bonus, capped at the game's max. Bonus 0 (or an item already at max) keeps the item as is.
        internal static int VariationFor(SpawnEntry e)
        {
            if(!e.Upgradeable || bonus<=0) return e.Variation;
            int target=Math.Min(e.BaseLevel+bonus,LevelScaling.GetMaxLevel());
            return target>e.BaseLevel ? target : e.Variation;
        }

        internal static ContainedObjectsBuffer ItemFor(SpawnEntry e) =>
            new ContainedObjectsBuffer { objectData=new ObjectDataCD { objectID=e.Id, amount=1, variation=VariationFor(e) } };

        // ---------------------------------------------------------------- spawning

        // Left-click: cursor (Ctrl = full stack). Right-click: bag. Shift-click: full stack into the bag.
        internal static void Click(SpawnEntry e,bool toBag)
        {
            if(!Active || e==null) return;
            if(!CanSpawn) { Instance.Flash("Only the host or an admin can spawn items."); return; }
            var p=Manager.main.player;
            bool shift=Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool ctrl=Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            int variation=VariationFor(e);
            int stack=e.Stackable ? MaxStack : 1;
            // Holding the mouse repeats clicks; full stacks only once per press.
            if(shift || ctrl) {
                if(Time.unscaledTime<Instance.nextStackSpawn) return;
                Instance.nextStackSpawn=Time.unscaledTime+.3f;
            }
            if(shift || toBag) {
                // Same request vanilla uses for starter items: first free or matching bag slot, else dropped at your feet.
                p.playerCommandSystem.CreateItem(p.entity,0,e.Id,shift || ctrl ? stack : 1,p.WorldPosition,variation);
                return;
            }
            var mouse=p.mouseInventoryHandler;
            if(mouse==null) return;
            var held=mouse.GetObjectData(0);
            int amount=ctrl ? stack : 1;
            if(held.objectID!=ObjectID.None) {
                if(held.objectID!=e.Id || held.variation!=variation || !e.Stackable) {
                    Instance.Flash("Put down the item on your cursor first.");
                    return;
                }
                amount=Math.Min(amount,MaxStack-held.amount);
                if(amount<=0) return;
            }
            p.playerCommandSystem.CreateItem(mouse.inventoryEntity,mouse.startPosInBuffer,e.Id,amount,p.WorldPosition,variation);
        }

        private float flashUntil;
        private void Flash(string text)
        {
            emptyMessage.gameObject.SetActive(true);
            emptyMessage.Render(text);
            flashUntil=Time.unscaledTime+2f;
        }

        // ---------------------------------------------------------------- level

        private void ChangeLevel(int delta)
        {
            if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) delta*=5;
            // No item needs more than max-1 levels on top of its base.
            bonus=Mathf.Clamp(bonus+delta,0,LevelScaling.GetMaxLevel()-1);
            Draw();
        }

        private string LevelLabel => bonus==0 ? "Lv base" : "Lv +"+bonus;

        // ---------------------------------------------------------------- panel

        private void Build()
        {
            screen=Instantiate(Prefab,Manager.ui.UICamera.transform,false);
            screen.name="Item Spawner";
            screen.SetActive(false);
            Find("TabsAnchor").gameObject.SetActive(false);
            foreach(var t in screen.GetComponentsInChildren<Transform>(true))
                if(t.name=="FiltersPanel" || t.name=="FakeScrollBar" || t.name=="Toggled") t.gameObject.SetActive(false);
            search=Find("StorageSearch").GetComponent<TextInputField>();
            search.hintString="Search items";
            search.hintText.localize=false;
            search.hintText.Render("Search items",false,true);
            search.dontAllowNewLines=true;
            search.onInputFieldDone=new UnityEvent();
            var metrics=Find("StorageMetrics").localPosition;
            spread=metrics.z;
            scroll=Find("StorageGrid").GetComponent<UIScrollWindow>();
            model=new StorageBrowserModel<SpawnEntry>((int)metrics.x,Math.Max(1,Mathf.FloorToInt(scroll.windowHeight/spread)));
            var scrollable=scroll.gameObject.AddComponent<SpawnerScroll>();
            scroll.scrollable=scrollable;
            scroll.cursorMustBeInsideWindowToScroll=true;
            emptyMessage=Instantiate(search.hintText,screen.transform,false);
            emptyMessage.style.maskInteraction=SpriteMaskInteraction.None;
            emptyMessage.localize=false;
            emptyMessage.transform.localPosition=new Vector3(-5.4f,2.5f,0);
            emptyMessage.maxWidth=10;
            heading=Instantiate(emptyMessage,screen.transform,false);
            heading.transform.localPosition=new Vector3(1.9f,3.88f,0);
            heading.style.horizontalAlignment=PugTextStyle.HorizontalAlignment.right;
            heading.maxWidth=4;
            heading.gameObject.SetActive(true);
            emptyMessage.gameObject.SetActive(false);
            CreateSlots("StorageGridSlot",grid,model.GridColumns*model.ViewportRows);
            CreateSlots("StorageListSlot",list,model.ViewportRows);
            Bind("StorageLayout",()=>{layout=layout==BrowserLayout.Grid?BrowserLayout.List:BrowserLayout.Grid;Configure();},null,"Grid / list");
            // The list is always sorted by name, so the sort button is not needed.
            Bind("StorageSort",()=>ChangeLevel(-1),null,"Gear and tool level: -1 (Shift: -5). Never below the item's base level. Key [");
            Bind("StorageOrder",()=>ChangeLevel(1),null,"Gear and tool level: +1 (Shift: +5). Stops at the max level. Key ]");
            GlyphIcons.Apply(Find("StorageSort").gameObject,false);
            GlyphIcons.Apply(Find("StorageOrder").gameObject,true);
            Find("StorageFilters").gameObject.SetActive(false);
            BuildTabs();
        }

        private SpawnerTooltip Bind(string name,Action left,Action right,string title)
        {
            var button=Find(name).GetComponent<ButtonUIElement>();
            button.onLeftClick=new UnityEvent(); button.onLeftClick.AddListener(()=>left());
            button.onRightClick=new UnityEvent(); if(right!=null) button.onRightClick.AddListener(()=>right());
            button.showHoverTitle=true; button.showHoverDesc=false;
            var tip=button.gameObject.AddComponent<SpawnerTooltip>(); tip.Title=title;
            return tip;
        }

        private void CreateSlots(string templateName,List<SpawnerSlot> slots,int count)
        {
            var template=Find(templateName).GetComponent<SlotUIBase>();
            for(int i=0;i<count;i++) {
                var clone=Instantiate(template,scroll.scrollingContent.GetChild(0),false);
                clone.gameObject.SetActive(false);
                var item=clone.gameObject.AddComponent<SpawnerSlot>();
                item.icon=clone.icon; item.background=clone.background; item.darkBackground=clone.darkBackground;
                item.hoverBorder=clone.hoverBorder; item.activeBorder=clone.activeBorder;
                item.amountNumber=clone.amountNumber; item.amountNumberShadow=clone.amountNumberShadow;
                item.overlayIcon=clone.overlayIcon; item.underlayIcon=clone.underlayIcon;
                item.upgradeIcon=clone.upgradeIcon; item.upgradeSprites=clone.upgradeSprites;
                item.Window=scroll;
                item.NameText=clone.GetComponentsInChildren<PugText>(true).FirstOrDefault(t=>t.name=="NameText");
                foreach(var t in clone.GetComponentsInChildren<Transform>(true))
                    if(t.name=="FavoritedBorder" || t.name=="MissingIcon" || t.name=="PlusIcon" || t.name=="SortingValueText") t.gameObject.SetActive(false);
                Destroy(clone); slots.Add(item);
            }
        }

        private void BuildTabs()
        {
            tabColumn=new GameObject("Spawner category tabs");tabColumn.transform.SetParent(screen.transform,false);
            var template=Find("StorageFilters").gameObject;
            for(int i=0;i<Categories.Length;i++) {
                int selected=i;
                var go=Instantiate(template,tabColumn.transform,false);
                go.SetActive(true);
                go.name="Category "+Categories[i];
                go.transform.localScale=Vector3.one;
                foreach(var text in go.GetComponentsInChildren<PugText>(true))Destroy(text.gameObject);
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
                go.AddComponent<SpawnerTooltip>().Title=Categories[i];
                tabButtons.Add(button);
                // Active tab: a gold frame over the brown one, same as Runic Storage.
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
                var sprite=PugDatabase.GetObjectInfo(CategoryIcons[i])?.icon;
                if(sprite==null)continue;
                foreach(var icon in icons) {
                    icon.sprite=sprite;
                    icon.transform.localPosition=new Vector3(0,0,icon.transform.localPosition.z);
                }
            }
            UpdateTabs();
        }

        private void PlaceTabs()
        {
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
            float slotScale=grid[0].icon.transform.lossyScale.x*.72f;
            for(int i=0;i<tabIcons.Count;i++)
                foreach(var icon in tabIcons[i])
                    if(icon.sprite==PugDatabase.GetObjectInfo(CategoryIcons[i])?.icon && icon.transform.parent.lossyScale.x>0)
                        icon.transform.localScale=Vector3.one*(slotScale*(i==0?.6f:1f)/icon.transform.parent.lossyScale.x);
            Physics.SyncTransforms();
            tabsPlaced=true;
        }

        private void UpdateTabs()
        {
            for(int i=0;i<tabToggles.Count;i++) {
                if(tabToggles[i]!=null && tabToggles[i].activeSelf)tabToggles[i].SetActive(false);
                if(tabHighlights[i]!=null && tabHighlights[i].activeSelf!=(i==filter))tabHighlights[i].SetActive(i==filter);
                var tint=i==filter||i==hoveredTab?Color.white:DimTab;
                foreach(var icon in tabIcons[i])if(icon.color!=tint)icon.color=tint;
            }
        }

        private void SelectCategory(int selected)
        {
            if(!Active || Time.frameCount==tabSelectedFrame)return;
            tabSelectedFrame=Time.frameCount;
            filter=selected==filter?0:selected;
            Refresh();UpdateTabs();
        }

        private void UpdateTabPointer()
        {
            int hovered=-1;
            var pointer=Manager.ui.mouse.pointer.position;
            for(int i=0;i<tabButtons.Count;i++) {
                var button=tabButtons[i];
                var collider=button.GetComponent<BoxCollider>();
                var local=button.transform.InverseTransformPoint(pointer)-collider.center;
                if(Mathf.Abs(local.x)<=collider.size.x*.5f && Mathf.Abs(local.y)<=collider.size.y*.5f) { hovered=i;break; }
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

        private void Refresh()
        {
            model.SetEntries(catalogue.Where(e=>filter==0 || e.Category==filter).Select(e=>new StorageBrowserModel<SpawnEntry>.Entry {
                Address=e, Name=names[e], Amount=1, Empty=false
            }));
            Configure();
        }

        private void Configure() { model.Configure(lastSearch,BrowserSort.Name,false,layout); scroll.ResetScroll(); Draw(); }

        private void LateUpdate()
        {
            if(!Active) return;
            var p=Manager.main?.player;
            if(p==null || Manager.menu.IsAnyMenuActive()) { Close("player unavailable or menu opened"); return; }
            if(!search.inputIsActive) {
                if(Input.GetKeyDown(KeyCode.Escape)) { Close("Escape"); return; }
                if(Input.GetKeyDown(KeyCode.RightBracket)) ChangeLevel(1);
                if(Input.GetKeyDown(KeyCode.LeftBracket)) ChangeLevel(-1);
            }
            var next=search.GetInputText()??"";
            if(Manager.ui.currentSelectedUIElement==search && Input.GetMouseButtonDown(1)) {search.SetInputText("");next="";}
            if(next!=lastSearch) {lastSearch=next;Configure();}
            heading.gameObject.SetActive(lastSearch.Length<=12);
            if(lastSearch.Length<=12) heading.Render(CanSpawn ? LevelLabel : "View only");
            if(flashUntil>0 && Time.unscaledTime>=flashUntil) { flashUntil=0; Draw(); }
            ApplyLayout();
            UpdateTabPointer();
        }

        private void ApplyLayout()
        {
            RaiseCursor();
            var camera=Manager.ui.UICamera.transform;
            var scale=Manager.ui.CalcGameplayUITargetScaleMultiplier();
            var bagDepth=camera.InverseTransformPoint(bagPosition).z;
            screen.transform.localScale=scale;
            screen.transform.localPosition=new Vector3(BrowserPos.x*scale.x,BrowserPos.y*scale.y,bagDepth);
            Manager.ui.playerInventoryUI.transform.position=camera.TransformPoint(new Vector3(BagPos.x*scale.x,(BagPos.y+3f)*scale.y,bagDepth));
            FitInventoryToScreen();
            Manager.ui.itemSlotsBar.HideContainerUI();
        }

        private void FitInventoryToScreen()
        {
            var camera=Manager.ui.UICamera.GetComponent<Camera>();
            if(camera==null)return;
            bool first=true;var bounds=new Bounds();
            foreach(var slot in Manager.ui.playerInventoryUI.itemSlots) {
                if(slot==null || !slot.gameObject.activeInHierarchy || slot.background==null)continue;
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

        // The grabbed item draws above the panel instead of behind it.
        private void RaiseCursor()
        {
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

        private void RestoreCursor()
        {
            foreach(var entry in cursorLayers)if(entry.Key!=null){entry.Key.sortingLayerID=entry.Value.layer;entry.Key.sortingOrder=entry.Value.order;entry.Key.maskInteraction=entry.Value.mask;}
            foreach(var entry in cursorTextStyles)if(entry.Key!=null){entry.Key.style=entry.Value;entry.Key.Render(entry.Key.displayedTextString,false,true,false);}
            cursorLayers.Clear();cursorTextStyles.Clear();
        }

        private void Draw()
        {
            bool hasItems=model.TryGetVisible(0,out _);
            if(flashUntil<=0) {
                emptyMessage.gameObject.SetActive(!hasItems);
                if(!hasItems) emptyMessage.Render("No items match this search or tab.");
            }
            var active=layout==BrowserLayout.Grid?grid:list;
            foreach(var slot in layout==BrowserLayout.Grid?list:grid) slot.gameObject.SetActive(false);
            for(int i=0;i<active.Count;i++) {
                var slot=active[i]; bool valid=model.TryGetVisible(i,out var entry);
                slot.gameObject.SetActive(valid); if(!valid) continue;
                slot.Entry=entry.Address; slot.visibleSlotIndex=i;
                slot.transform.localPosition=new Vector3(layout==BrowserLayout.Grid?(-(model.Columns-1)*0.5f+i%model.Columns)*spread:0,
                    -(model.FirstRow+i/model.Columns)*spread,0);
                slot.UpdateSlot();
            }
        }

        internal static float Height => Active?Instance.model.TotalRows*Instance.spread:0;
        internal static void Scroll(float value) { if(!Active)return;Instance.model.ScrollToRow(Mathf.FloorToInt(value/Instance.spread));Instance.Draw(); }
    }

    public sealed class SpawnerScroll : UIelement,IScrollable
    {
        public override bool isShowing=>SpawnerUI.Active;
        public void UpdateContainingElements(float value)=>SpawnerUI.Scroll(value);
        public float GetCurrentWindowHeight()=>SpawnerUI.Height;
        public bool IsBottomElementSelected()=>false;
        public bool IsTopElementSelected()=>false;
    }

    public sealed class SpawnerTooltip : MonoBehaviour { public string Title; }

    public sealed class SpawnerSlot : SlotUIBase
    {
        public SpawnEntry Entry;
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
        protected override ContainedObjectsBuffer GetSlotObject()=>Entry==null ? default : SpawnerUI.ItemFor(Entry);
        public override UIScrollWindow uiScrollWindow=>Window;
        public override void OnLeftClicked(bool mod1,bool mod2)=>SpawnerUI.Click(Entry,false);
        public override void OnRightClicked(bool mod1,bool mod2)=>SpawnerUI.Click(Entry,true);
        public override void UpdateSlot()
        {
            if(Entry==null)return;
            var item=SpawnerUI.ItemFor(Entry);
            if(hasRendered && item.objectID==renderedItem.objectID && item.variation==renderedItem.variation)return;
            renderedItem=item;hasRendered=true;
            var info=PugDatabase.GetObjectInfo(Entry.Id,Entry.Variation);
            icon.sprite=info?.icon; icon.enabled=icon.sprite!=null;
            if(amountNumber!=null) amountNumber.Render("",false,true,true);
            UpdateUpgradeIcon(item);
            if(NameText!=null) NameText.Render(PlayerController.GetObjectName(item,true)?.text??"");
        }
    }

    [HarmonyPatch(typeof(ButtonUIElement),nameof(ButtonUIElement.GetHoverTitle))]
    public static class SpawnerTooltipPatch
    {
        public static bool Prefix(ButtonUIElement __instance,ref TextAndFormatFields __result)
        {
            if(!SpawnerUI.Active || !__instance.TryGetComponent<SpawnerTooltip>(out var hint))return true;
            __result=new TextAndFormatFields {text=hint.Title,dontLocalize=true};return false;
        }
    }

    [HarmonyPatch(typeof(UIManager),nameof(UIManager.TryHideAllInventoryAndCraftingUI))]
    public static class SpawnerClosePatch
    {
        public static void Postfix(bool __result) { if(__result) SpawnerUI.Close("native inventory close"); }
    }

    [HarmonyPatch(typeof(SendClientInputSystem),"PlayerInteractionBlocked")]
    public static class SpawnerInteractionPatch
    {
        public static void Postfix(ref bool __result) { if(SpawnerUI.Active) __result=true; }
    }
}
