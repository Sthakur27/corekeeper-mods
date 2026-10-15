using System;
using System.Collections.Generic;
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
    // The screen and item interactions are vanilla. Only the visible-slot address is mapped.
    public sealed class StorageTerminalUI : MonoBehaviour
    {
        private sealed class Address { public Entity Entity; public int Index; public InventoryHandler Handler; }
        public static StorageTerminalUI Instance;
        public static bool Active => Instance != null && Instance.terminal != null;
        private Chest terminal;
        private InventoryHandler viewHandler;
        private readonly Dictionary<InventoryHandler, Entity> entities = new Dictionary<InventoryHandler, Entity>();
        private readonly Dictionary<InventorySlotUI, Address> bindings = new Dictionary<InventorySlotUI, Address>();
        private readonly List<Address> addresses = new List<Address>();
        private readonly List<Address> filtered = new List<Address>();
        private readonly Dictionary<Entity, InventoryHandler> handlers = new Dictionary<Entity, InventoryHandler>();
        private readonly List<InventorySlotUI> visible = new List<InventorySlotUI>();
        private GameObject root;
        private InventorySlotUI template;
        private UIManager.CraftingUITheme woodTheme;
        private Texture2D fillTexture;
        private Sprite fillSprite;
        private TextInputField searchField;
        private PugText status;
        private UIScrollWindow scrollWindow;
        private GameObject scrollRoot;
        private string search = "", notice = "";
        private int row, capacity, occupied;
        private float scanAt;
        private bool closing;
        private bool oldAuto;
        private readonly List<GameObject> hiddenButtons = new List<GameObject>();
        private const int Columns = 10, Rows = 3;
        private const float LayoutScale = 1f;
        private static float Pixel(float value) => Mathf.Round(value * 16f) / 16f;
        internal static bool CanClick => Active;
        private void Awake() { Instance = this; }
        private void OnDisable() { Close(); }
        private void OnDestroy() { Close(); if(root != null) Destroy(root); if(scrollRoot != null) Destroy(scrollRoot); if(fillSprite != null) Destroy(fillSprite); if(fillTexture != null) Destroy(fillTexture); if(Instance == this) Instance = null; }
        private static InventoryHandler CreateHandler(Chest chest, int count)
        {
            var handler = new InventoryHandler(chest, chest.world);
            handler.size = handler.maxSize = count;

            return handler;
        }
        public static void Open(Chest chest)
        {
            if (Instance == null || Manager.main?.player == null) return;
            Close();
            Manager.ui.TryHideAllInventoryAndCraftingUI();
            var self = Instance;
            try
            {
                self.terminal = chest;
                self.row = 0; self.search = self.notice = "";
                self.viewHandler = CreateHandler(chest, Columns * Rows);
                Manager.main.player.SetActiveInventoryHandler(self.viewHandler);
                Manager.main.player.SetActiveWorldLabel(null);
                var ui = Manager.ui.chestInventoryUI;
                self.oldAuto = ui.autoPositionSlots;
                ui.autoPositionSlots = true;
                Manager.ui.OnChestInventoryOpen();
                // Freeze the native layout; addresses change independently of visual positions.
                ui.autoPositionSlots = false;
                self.visible.Clear();
                foreach (var slot in ui.itemSlots)
                    if(slot.uiSlotXPosition < Columns && slot.uiSlotYPosition < Rows && slot is InventorySlotUI inventorySlot) self.visible.Add(inventorySlot);
                ui.labelRoot.gameObject.SetActive(false);
                self.hiddenButtons.Clear();
                foreach(var button in ui.GetComponentsInChildren<ButtonUIElement>(true))
                    if(button.gameObject.activeSelf) { self.hiddenButtons.Add(button.gameObject); button.gameObject.SetActive(false); }
                self.RefreshNetwork(); self.BindSlots();
                if(self.root == null) self.Build();
                self.root.SetActive(true); self.scrollRoot.SetActive(true); self.searchField.SetInputText(""); self.scrollWindow.ResetScroll();
                self.scanAt = Time.unscaledTime + 0.5f;
                if(ui.optionalSortButton != null) ui.optionalSortButton.gameObject.SetActive(false);
                if(ui.optionalQuickStackButton != null) ui.optionalQuickStackButton.gameObject.SetActive(false);
            }
            catch(Exception ex) { Debug.LogError("[ConnectedStorage] Native chest open failed: " + ex); Close(); }
        }
        public static void Close()
        {
            var self = Instance;
            if(self == null || self.terminal == null || self.closing) return;
            self.closing = true;
            self.terminal = null;
            if(self.searchField != null && self.searchField.inputIsActive) self.searchField.Deactivate(false);
            if(self.root != null) self.root.SetActive(false);
            if(self.scrollRoot != null) self.scrollRoot.SetActive(false);
            foreach(var button in self.hiddenButtons) if(button != null) button.SetActive(true);
            self.hiddenButtons.Clear();
            foreach(var slot in self.visible) if(slot != null) slot.gameObject.SetActive(true);
            self.bindings.Clear(); self.entities.Clear(); self.handlers.Clear(); self.addresses.Clear();
            if(Manager.ui != null)
            {
                Manager.ui.chestInventoryUI.autoPositionSlots = self.oldAuto;
                Manager.ui.TryHideAllInventoryAndCraftingUI();
            }
            self.closing = false;
        }
        public static void CloseIfFrom(Chest chest) { if(Active && Instance.terminal == chest) Close(); }
        public static void Feedback(string message) { if(Active && message != null && message.StartsWith("[Connected Storage]")) Instance.notice = message; }
        private void Update()
        {
            if(!Active) return;
            var p = Manager.main?.player;
            var em = terminal.world.EntityManager;
            if(p == null || !em.Exists(terminal.entity) || !Manager.ui.isChestInventoryUIShowing || Manager.menu.IsAnyMenuActive()
                || math.distancesq(em.GetComponentData<LocalTransform>(terminal.entity).Position, em.GetComponentData<LocalTransform>(p.entity).Position) > 9f)
            { Close(); return; }
            if(Time.unscaledTime >= scanAt) { RefreshNetwork(); scanAt = Time.unscaledTime + 0.5f; }
            var next = searchField.GetInputText() ?? "";
            if(next != search) { search = next; row = 0; scrollWindow.ResetScroll(); }
            // Native slots refresh every frame. Preserve their physical addresses while clicking;
            // empty slots never collapse or reorder underneath the cursor.
            BindSlots();
            var bg = Manager.ui.chestInventoryUI.backgroundSR;
            root.transform.localScale = Manager.ui.CalcGameplayUITargetScaleMultiplier();
            root.transform.position = new Vector3(bg.bounds.center.x, bg.bounds.max.y + 1.25f, bg.transform.position.z - 0.2f);
            scrollRoot.transform.position = bg.bounds.center;
            scrollRoot.transform.localScale = Manager.ui.CalcGameplayUITargetScaleMultiplier();
            status.Render(string.IsNullOrEmpty(notice) ? "Storage  " + occupied + "/" + capacity + "   Rows " + (row+1) + "-" + (row+Rows) : notice);
        }
        private void RefreshNetwork()
        {
            var em = terminal.world.EntityManager;
            var net = StorageNetwork.Scan(terminal.world, terminal.entity);
            addresses.Clear(); capacity = 0;
            foreach(var entity in net.Chests)
            {
                int count = em.GetBuffer<ContainedObjectsBuffer>(entity).Length;
                if(!handlers.TryGetValue(entity, out var handler))
                {
                    // A handler only needs the world's context from the terminal. Its inventory
                    // entity is mapped below; it never stores a second copy of the items.
                    Chest unit = null;
                    foreach(var candidate in UnityEngine.Object.FindObjectsOfType<Chest>())
                        if(candidate.entity == entity && candidate.world == terminal.world) { unit = candidate; break; }
                    if(unit == null) continue;
                    handler = CreateHandler(unit, count);
                    handlers.Add(entity, handler); entities.Add(handler, entity);
                }
                handler.size = handler.maxSize = count;
                for(int i=0;i<count;i++) addresses.Add(new Address { Entity=entity, Index=i, Handler=handler });
                capacity += count;
            }
        }
        private void BindSlots()
        {
            filtered.Clear(); occupied = 0;
            var em = terminal.world.EntityManager;
            foreach(var a in addresses)
            {
                if(!em.Exists(a.Entity)) continue;
                var data = em.GetBuffer<ContainedObjectsBuffer>(a.Entity)[a.Index];
                if(data.objectID != ObjectID.None) occupied++;
                if(search.Length == 0 || (data.objectID != ObjectID.None && (PlayerController.GetObjectName(data,true)?.text ?? "").IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0)) filtered.Add(a);
            }
            row = Mathf.Clamp(row,0,Math.Max(0,(filtered.Count+Columns-1)/Columns-Rows));
            bindings.Clear();
            for(int i=0;i<visible.Count;i++)
            {
                var slot=visible[i]; int index=row*Columns+i;
                bool valid=index<filtered.Count;
                slot.gameObject.SetActive(valid);
                if(valid) { bindings[slot]=filtered[index]; slot.UpdateSlot(); }
            }
        }
        private void Scroll(int delta) { scrollWindow.MoveScroll(delta * Manager.ui.chestInventoryUI.spread); }
        internal static float ContentHeight => Active ? Math.Max(Rows,(Instance.filtered.Count+Columns-1)/Columns)*Manager.ui.chestInventoryUI.spread : 0;
        internal static void SetScroll(float value) { if(!Active) return; Instance.row=Mathf.RoundToInt(value/Manager.ui.chestInventoryUI.spread); Instance.BindSlots(); }
        internal static bool Resolve(InventorySlotUI slot, out InventoryHandler handler)
        { handler=null; if(!Active || !Instance.bindings.TryGetValue(slot,out var a)) return false; handler=a.Handler; return true; }
        internal static bool ResolveIndex(SlotUIBase slot,out int index)
        { index=0; if(!Active || !(slot is InventorySlotUI s) || !Instance.bindings.TryGetValue(s,out var a)) return false; index=a.Index; return true; }
        internal static bool IsStorageHandler(InventoryHandler handler) => Active && (Instance.viewHandler == handler || Instance.entities.ContainsKey(handler));
        internal static bool ResolveEntity(InventoryHandler handler,out Entity entity)
        { entity=Entity.Null; return Active && Instance.entities.TryGetValue(handler,out entity); }
        internal static bool Deposit(InventorySlotUI slot)
        {
            if(!Active || (!slot.isPlayerInventorySlot && !slot.isPlayerPouchSlot)) return false;
            var data=slot.GetObjectData();
            if(data.objectID!=ObjectID.None) Instance.Send("put " + (slot.GetInventoryHandler().startPosInBuffer+slot.inventorySlotIndex) + " " + (int)data.objectID + " " + data.variation);
            return true;
        }
        private void Send(string action)
        {
            var tile=StorageNetwork.Tile(terminal.world.EntityManager,terminal.entity);
            CommandModule.ClientCommSystem?.SendCommand("/connectedstorage " + tile.x + " " + tile.y + " " + action,CommandFlags.None);
        }
        private GameObject Node(string name, Transform parent, float x, float y, float z = 0)
        {
            var go = new GameObject(name);
            go.layer = template.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(Pixel(x), Pixel(y), z);
            return go;
        }
        private void Solid(Transform parent, float x, float y, float width, float height, int order, Color color)
        {
            if (fillSprite == null)
            {
                fillTexture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                fillTexture.SetPixel(0, 0, Color.white); fillTexture.Apply();
                fillSprite = Sprite.Create(fillTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            var go = Node("Opaque backing", parent, x, y);
            go.transform.localScale = new Vector3(Pixel(width), Pixel(height), 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = fillSprite; sr.sharedMaterial = template.background.sharedMaterial;
            sr.sortingLayerID = template.background.sortingLayerID; sr.sortingOrder = order;
            color.a = 1f; sr.color = color;
        }
        private SpriteRenderer Picture(Transform parent, SpriteRenderer source, float x, float y, float width, float height, int order)
        {
            var go = Node("Frame", parent, x, y);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite; sr.sharedMaterial = source.sharedMaterial;
            sr.sortingLayerID = source.sortingLayerID; sr.sortingOrder = order;
            sr.color = Color.white;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(Pixel(width), Pixel(height));
            return sr;
        }
        private PugText Label(Transform parent, float x, float y, string value, float width = 22)
        {
            var go = Node("Text", parent, x, y, -0.02f);
            var text = go.AddComponent<PugText>();
            // Unlike serialized prefab text, AddComponent leaves this list null.
            // PugText.ShouldForceLatinFont enumerates it unconditionally.
            text.languagesToForceLatinFont = new List<SystemLanguage>();
            text.styleOverrides = new List<PugText.StyleOverride>();
            text.style = new PugTextStyle();
            text.style.horizontalAlignment = PugTextStyle.HorizontalAlignment.left;
            text.style.verticalAlignment = PugTextStyle.VerticalAlignment.center;
            text.style.maskInteraction = SpriteMaskInteraction.None;
            text.style.sortingLayer = template.background.sortingLayerID;
            text.style.color = Color.white;
            text.style.orderInLayer = template.background.sortingOrder + 108;
            text.style.fontFace = TextManager.FontFace.thinSmall;
            text.localize = false; text.renderOnStart = true; text.keepEnabledOnStart = true;
            text.maxWidth = Pixel(width);
            text.SetText(value);
            return text;
        }
        private StorageControl Control(string title, float x, float y, float width, float height, Action<bool> action, bool slot = false)
        {
            var go = Node(title, root.transform, x, y, -0.2f);
            go.AddComponent<BoxCollider>().size = new Vector3(Pixel(width), Pixel(height), 0.1f);
            var control = go.AddComponent<StorageControl>();
            control.Click = action; control.Title = title;
            // Vanilla background is just a border; its filled layer is a separate renderer.
            Solid(go.transform, 0, 0, width, height, template.background.sortingOrder + 101,
                slot ? new Color(0.28f, 0.18f, 0.09f) : new Color(0.51f, 0.31f, 0.13f));
            control.Frame = Picture(go.transform, template.background, 0, 0, width, height, template.background.sortingOrder + 102);
            if (woodTheme != null) control.Frame.sprite = woodTheme.inventorySlotBackground;
            if (slot)
            {
                control.Icon = Node("Item", go.transform, 0, 0.1f, -0.02f).AddComponent<SpriteRenderer>();
                control.Icon.sharedMaterial = template.icon.sharedMaterial;
                control.Icon.sortingLayerID = template.icon.sortingLayerID;
                control.Icon.sortingOrder = template.background.sortingOrder + 106;
                control.Text = Label(go.transform, width / 2 - 0.1f, -height / 2 + 0.3f, "", width);
                control.Text.style.horizontalAlignment = PugTextStyle.HorizontalAlignment.right;
            }
            else control.Text = Label(go.transform, -width / 2 + 0.15f, 0, title, width - 0.3f);
            return control;
        }
        private void Build()
        {
            template = Manager.ui.playerInventoryUI.GetComponentInChildren<InventorySlotUI>(true);
            woodTheme = Manager.ui.craftingUIThemes.Find(t=>t.craftingUIThemeType==UIManager.CraftingUIThemeType.Wood);
            root=new GameObject("Connected Storage Search"); root.SetActive(false);
            root.layer=template.gameObject.layer; root.transform.SetParent(Manager.ui.UICamera.transform,false);
            scrollRoot=new GameObject("Connected Storage Scroll"); scrollRoot.SetActive(false);
            scrollRoot.layer=template.gameObject.layer; scrollRoot.transform.SetParent(Manager.ui.UICamera.transform,false);
            var scrollView=scrollRoot.AddComponent<StorageScrollView>();
            scrollWindow=scrollRoot.AddComponent<UIScrollWindow>();
            scrollWindow.scrollable=scrollView;
            scrollWindow.scrollingContent=new GameObject("Scroll position").transform;
            scrollWindow.scrollingContent.SetParent(scrollRoot.transform,false);
            scrollWindow.windowWidth=Columns*Manager.ui.chestInventoryUI.spread;
            scrollWindow.windowHeight=Rows*Manager.ui.chestInventoryUI.spread;
            scrollWindow.cursorMustBeInsideWindowToScroll=true;
            scrollWindow.scrollMultiplierSpeedMouse=Manager.ui.chestInventoryUI.spread;
            var sourceBar=Manager.ui.playerInventoryUI.scrollWindow?.scrollBar;
            if(sourceBar != null)
            {
                var bar=Instantiate(sourceBar,scrollRoot.transform);
                bar.scrollWindow=scrollWindow; scrollWindow.scrollBar=bar;
                bar.transform.localPosition=new Vector3(scrollWindow.windowWidth/2+0.7f,0,-0.1f);
                bar.background.size=new Vector2(bar.background.size.x,scrollWindow.windowHeight);
            }
            Solid(root.transform,0,0.8f,14,0.8f,template.background.sortingOrder+101,new Color(0.28f,0.18f,0.09f));
            status=Label(root.transform,0,0.8f,"",14);
            status.style.horizontalAlignment=PugTextStyle.HorizontalAlignment.center;
            var go=Node("Search",root.transform,0,0,-0.2f);
            go.AddComponent<BoxCollider>().size=new Vector3(14,0.9f,0.1f);
            Solid(go.transform,0,0,14,0.9f,template.background.sortingOrder+101,new Color(0.28f,0.18f,0.09f));
            Picture(go.transform,template.background,0,0,14,0.9f,template.background.sortingOrder+102);
            searchField=go.AddComponent<TextInputField>();
            searchField.pugText=Label(go.transform,-6.8f,0,"",13.6f);
            searchField.pugText.isWrittenToByUser=true;
            searchField.hintText=Label(go.transform,-6.8f,0,"Search items...",13.6f);
            searchField.hintString="Search items...";searchField.maxWidth=13.6f;searchField.dontAllowNewLines=true;
            searchField.onInputFieldDone=new UnityEvent();
            searchField.selectedMarker=Node("Selected",go.transform,0,0);
            Picture(searchField.selectedMarker.transform,template.hoverBorder,0,0,14,0.9f,template.background.sortingOrder+104);
            var caret=Node("Caret",go.transform,-6.8f,0,-0.03f);
            searchField.characterMarkBlinker=caret.AddComponent<CharacterMarkBlinker>();
            searchField.characterMarkBlinker.sr=Picture(caret.transform,template.background,0,0,0.0625f,0.5f,template.background.sortingOrder+109);
        }
    }
    public sealed class StorageScrollView : UIelement, IScrollable
    {
        public override bool isShowing => StorageTerminalUI.Active;
        public void UpdateContainingElements(float scroll) => StorageTerminalUI.SetScroll(scroll);
        public float GetCurrentWindowHeight() => StorageTerminalUI.ContentHeight;
        public bool IsBottomElementSelected() => false;
        public bool IsTopElementSelected() => false;
    }
    [HarmonyPatch(typeof(InventoryHandler),nameof(InventoryHandler.Swap))]
    public static class StorageSwapTrace
    {
        public static void Prefix(InventoryHandler __instance, int index, InventoryHandler other, int otherIndex)
        {
            if(!StorageTerminalUI.Active) return;
            var em=Manager.main.player.world.EntityManager;
            Debug.Log("[StorageNative] swap " + __instance.inventoryEntity + ":" + index + " -> " + other.inventoryEntity + ":" + otherIndex + " mapped=" + StorageTerminalUI.IsStorageHandler(other) + " buffer=" + em.HasBuffer<InventoryBuffer>(other.inventoryEntity));
        }
    }
    [HarmonyPatch(typeof(InventorySlotUI),nameof(InventorySlotUI.GetInventoryHandler))]
    public static class StorageSlotHandlerPatch
    { public static bool Prefix(InventorySlotUI __instance,ref InventoryHandler __result) { if(!StorageTerminalUI.Resolve(__instance,out var handler))return true;__result=handler;return false; } }
    [HarmonyPatch(typeof(SlotUIBase),"get_inventorySlotIndex")]
    public static class StorageSlotIndexPatch
    { public static bool Prefix(SlotUIBase __instance,ref int __result) { if(!StorageTerminalUI.ResolveIndex(__instance,out int index))return true;__result=index;return false; } }
    [HarmonyPatch(typeof(InventoryHandler),nameof(InventoryHandler.ObjectIsValidToPutInInventory))]
    public static class StorageHandlerValidationPatch
    { public static bool Prefix(InventoryHandler __instance,int checkSpecificIndexOnly,ref bool __result) { if(!StorageTerminalUI.ResolveEntity(__instance,out var entity))return true; __result=checkSpecificIndexOnly<__instance.size && checkSpecificIndexOnly>=-1; return false; } }
    [HarmonyPatch(typeof(InventoryHandler),"get_columns")]
    public static class StorageHandlerColumnsPatch
    { public static bool Prefix(InventoryHandler __instance,ref int __result) { if(!StorageTerminalUI.IsStorageHandler(__instance))return true;__result=10;return false; } }
    [HarmonyPatch(typeof(InventoryHandler),"get_inventoryEntity")]
    public static class StorageHandlerEntityPatch
    { public static bool Prefix(InventoryHandler __instance,ref Entity __result) { if(!StorageTerminalUI.ResolveEntity(__instance,out var entity))return true;__result=entity;return false; } }
    [HarmonyPatch(typeof(InventorySlotUI),nameof(InventorySlotUI.TryToSendItemToOtherInventoryOrEquip))]
    public static class StorageQuickTransferPatch
    { public static bool Prefix(InventorySlotUI __instance) => !StorageTerminalUI.Deposit(__instance); }
    [HarmonyPatch(typeof(UIManager),nameof(UIManager.TryHideAllInventoryAndCraftingUI))]
    public static class StorageClosePatch
    { public static void Postfix() { StorageTerminalUI.Close(); } }
    public sealed class StorageControl : UIelement
    {
        public Action<bool> Click;
        public string Title;
        public SpriteRenderer Frame, Icon;
        public PugText Text;
        public override void OnLeftClicked(bool mod1, bool mod2)
        {
            if (StorageTerminalUI.CanClick) Click?.Invoke(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        }
        public override void OnSelected()
        {
            base.OnSelected();
            if (Frame != null) Frame.color = new Color(1f, 0.9f, 0.65f);
        }
        public override void OnDeselected(bool playEffect = true)
        {
            base.OnDeselected(playEffect);
            if (Frame != null) Frame.color = Color.white;
        }
        public override TextAndFormatFields GetHoverTitle() => string.IsNullOrEmpty(Title) || Title == "Empty" ? null : new TextAndFormatFields { text = Title, dontLocalize = true };
    }
    [HarmonyPatch(typeof(Chest), nameof(Chest.Use))]
    public static class StorageTerminalUsePatch
    {
        public static bool Prefix(Chest __instance)
        {
            if (StorageCatalog.IsChest(__instance.objectData.objectID))
            {
                var player = Manager.main.player;
                var comm = CommandModule.ClientCommSystem;
                if (player != null && comm != null)
                {
                    var em=player.world.EntityManager;
                    var items=em.GetBuffer<ContainedObjectsBuffer>(player.entity);
                    int slot=player.equippedSlotIndex;
                    if(slot<0 || slot>=items.Length){StorageBrowserUI.Open(__instance);return false;}
                    bool holdingGem=false;
                    for(int tier=1;tier<StorageCatalog.Tiers.Length;tier++)
                        if(items[slot].objectID!=ObjectID.None && items[slot].objectID==StorageCatalog.Gem(tier))holdingGem=true;
                    if(!holdingGem){StorageBrowserUI.Open(__instance);return false;}
                    var tile = StorageNetwork.Tile(__instance.world.EntityManager, __instance.entity);
                    comm.SendCommand("/connectedstorage " + tile.x + " " + tile.y + " upgrade "
                        + player.equippedSlotIndex + " " + (int)__instance.objectData.objectID, CommandFlags.None);
                }
                return false;
            }
            if (!StorageCatalog.IsTerminal(__instance.objectData.objectID)) return true;
            StorageBrowserUI.Open(__instance); return false;
        }
    }
    [HarmonyPatch(typeof(Chest), nameof(Chest.OnPlayerLeftChest))]
    public static class StorageTerminalLeftPatch
    {
        public static void Postfix(Chest __instance) => StorageTerminalUI.CloseIfFrom(__instance);
    }
    [HarmonyPatch(typeof(PugText), nameof(PugText.Render), new[] { typeof(string), typeof(bool), typeof(bool), typeof(bool) })]
    public static class StorageFeedbackPatch
    {
        public static void Postfix(string text) => StorageTerminalUI.Feedback(text);
    }
}











