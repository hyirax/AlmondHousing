using Dalamud.Game.Command;
using Dalamud.Plugin;
using Lumina.Excel.Sheets;
using AlmondHousing.Objects;
using AlmondHousing.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static AlmondHousing.Memory;
using HousingFurniture = Lumina.Excel.Sheets.HousingFurniture;

namespace AlmondHousing
{
    public sealed class AlmondHousing : IDalamudPlugin
    {
        public static LanguageManager Lang { get; private set; } = new LanguageManager();
        public static string PluginDirectory { get; private set; }
        private IDalamudPluginInterface PluginInterface { get; init; } = null!;
        
        public string Name => "AlmondHousing";
        public PluginUi Gui { get; private set; }
        public Configuration Config { get; private set; }

        // 高性能状态机与队列
        public static PlacementSession Session = new PlacementSession();

        private delegate bool UpdateLayoutDelegate(IntPtr a1);
        public delegate void SelectItemDelegate(IntPtr housingStruct, IntPtr item);
        private static HookWrapper<SelectItemDelegate> SelectItemHook;
        public delegate void ClickItemDelegate(IntPtr housingStruct, IntPtr item);
        private static HookWrapper<ClickItemDelegate> ClickItemHook;

        public static bool ApplyChange = false;
        public static SaveLayoutManager LayoutManager = null!;
        public static bool logHousingDetour = false;
        internal static Location PlotLocation = new Location();

        public Layout Layout = new Layout();
        public List<HousingItem> InteriorItemList = new List<HousingItem>();
        public List<HousingItem> ExteriorItemList = new List<HousingItem>();
        public List<HousingItem> UnusedItemList = new List<HousingItem>();

        public void Dispose()
        {
            HookManager.Dispose();
            try { Memory.Instance.SetPlaceAnywhere(false); }
            catch (Exception ex) { DalamudApi.PluginLog.Error(ex, "Error while calling PluginMemory.Dispose()"); }

            DalamudApi.CommandManager.RemoveHandler("/almond");
            Gui?.Dispose();
        }

        public AlmondHousing(IDalamudPluginInterface pi)
        {
            DalamudApi.Initialize(pi);
            Config = DalamudApi.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Config.Save();

            Initialize();

            DalamudApi.CommandManager.AddHandler("/almond", new CommandInfo(CommandHandler) { HelpMessage = "打开 AlmondHousing 面板。" });
            Gui = new PluginUi(this);

            HousingData.Init(this);
            Memory.Init();
            Memory.Instance.SetPlaceAnywhere(true);
            LayoutManager = new SaveLayoutManager(this, Config);

            DalamudApi.PluginLog.Info("AlmondHousing Plugin initialized");

            PluginDirectory = pi.AssemblyLocation.DirectoryName;
            if (PluginDirectory != null)
            {
                Lang.LoadAllLanguages(PluginDirectory);
                Lang.SetLanguage(Config.UILanguage);
                DalamudApi.PluginLog.Info($"[汉化系统] 翻译官启动！当前语言设定为: {Config.UILanguage}");
            }
        }
        
        public void Initialize()
        {
            SelectItemHook = HookManager.Hook<SelectItemDelegate>("48 85 D2 0F 84 ?? ?? ?? ?? 53 41 56 48 83 EC ?? 48 89 6C 24", SelectItemDetour);
            ClickItemHook = HookManager.Hook<ClickItemDelegate>("48 89 5C 24 10 48 89 74  24 18 57 48 83 EC 20 4c 8B 41 18 33 FF 0F B6 F2", ClickItemDetour);
            GetGameObjectHook = HookManager.Hook<GetObjectDelegate>("E8 ?? ?? ?? ?? EB ?? 48 3D", GetGameObject);
            GetObjectFromIndexHook = HookManager.Hook<GetActiveObjectDelegate>("E8 ?? ?? ?? ?? EB ?? 41 0F B7 D0", GetObjectFromIndex);
            GetYardIndexHook = HookManager.Hook<GetIndexDelegate>("E8 ?? ?? ?? ?? 44 0F B7 D8", GetYardIndex);

            MaybePlaceh = HookManager.Hook<MaybePlaced>("40 55 56 57 48 8D AC 24 70 FF FF FF 48 81 EC 90 01 00 00 48 8B", MaybePlace); 
            ResetItemPlacementh = HookManager.Hook<ResetItemPlacementd>("48 89 5C 24 08 57 48 83  EC 20 48 83 79 18 00 0F", Hc1); 
            FinalizeHousingh = HookManager.Hook<FinalizeHousingd>("40 55 56 41 56 48 83 EC 20 48 63 EA 48 8B F1 8B", FinalizeHousing); 
            PlaceCallh = HookManager.Hook<PlaceCalld>("40 53 48 83 Ec 20 48 8B 51 18 48 8B D9 48 85 D2 0F 84 B1 00 00 00", PlaceCall); 
        }

        internal delegate ushort GetIndexDelegate(byte plotNumber, ushort inventoryIndex);
        internal static HookWrapper<GetIndexDelegate> GetYardIndexHook;
        internal static ushort GetYardIndex(byte plotNumber, ushort inventoryIndex) => GetYardIndexHook.Original(plotNumber, inventoryIndex);

        internal delegate IntPtr GetActiveObjectDelegate(IntPtr ObjList, uint index);
        internal static IntPtr GetObjectFromIndex(IntPtr ObjList, uint index) => GetObjectFromIndexHook.Original(ObjList, index);

        internal delegate IntPtr GetObjectDelegate(IntPtr ObjList, ushort index);
        internal static HookWrapper<GetObjectDelegate> GetGameObjectHook;
        internal static HookWrapper<GetActiveObjectDelegate> GetObjectFromIndexHook;

        internal static IntPtr GetGameObject(IntPtr ObjList, ushort index) => GetGameObjectHook.Original(ObjList, index);

        unsafe static public void SelectItemDetour(IntPtr housing, IntPtr item) { SelectItemHook.Original(housing, item); }
        unsafe static public void SelectItem(IntPtr item) { SelectItemDetour((IntPtr)Memory.Instance.HousingStructure, item); }

        internal delegate void MaybePlaced(IntPtr housingPtr, IntPtr itemPtr, Int64 a); 
        internal static HookWrapper<MaybePlaced> MaybePlaceh;
        unsafe static public void MaybePlacedt(IntPtr housingPtr, IntPtr itemPtr, Int64 a) { MaybePlaceh.Original(housingPtr, itemPtr, a); }
        unsafe static public void MaybePlace(IntPtr housingPtr, IntPtr itemPtr, Int64 a) { MaybePlacedt(housingPtr, itemPtr, a); }

        internal delegate void ResetItemPlacementd(IntPtr housingPtr, Int64 a); 
        internal static HookWrapper<ResetItemPlacementd> ResetItemPlacementh;
        unsafe static public void ResetItemPlacementdt(IntPtr housingPtr, Int64 a) { ResetItemPlacementh.Original(housingPtr, a); }
        unsafe static public void Hc1(IntPtr housingPtr, Int64 a) { ResetItemPlacementdt(housingPtr, a); }

        internal delegate void FinalizeHousingd(IntPtr housingPtr, Int64 a, IntPtr b); 
        internal static HookWrapper<FinalizeHousingd> FinalizeHousingh;
        unsafe static public void FinalizeHousingdt(IntPtr housingPtr, Int64 a, IntPtr b) { FinalizeHousingh.Original(housingPtr, a, b); }
        unsafe static public void FinalizeHousing(IntPtr housingPtr, Int64 a, IntPtr b) { FinalizeHousingdt(housingPtr, a, b); }

        internal delegate void PlaceCalld(IntPtr housingPtr, Int64 a, IntPtr b); 
        internal static HookWrapper<PlaceCalld> PlaceCallh;
        unsafe static public void PlaceCalldt(IntPtr housingPtr, Int64 a, IntPtr b) { PlaceCallh.Original(housingPtr, a, b); }
        unsafe static public void PlaceCall(IntPtr housingPtr, Int64 a, IntPtr b) { PlaceCalldt(housingPtr, a, b); }

        unsafe static public void ClickItemDetour(IntPtr housing, IntPtr item) { ClickItemHook.Original(housing, item); }
        unsafe static public void ClickItem(IntPtr item) { ClickItemDetour((IntPtr)Memory.Instance.HousingStructure, item); }

        public unsafe void RecursivelyPlaceItems()
        {
            if (!Memory.Instance.CanEditItem() || Session.PendingItems.Count == 0)
            {
                Cleanup();
                return;
            }

            try
            {
                if (Memory.Instance.GetCurrentTerritory() == Memory.HousingArea.Outdoors) GetPlotLocation();

                while (Session.PendingItems.Count > 0)
                {
                    var item = Session.PendingItems.Dequeue();
                    if (item.ItemStruct == IntPtr.Zero) continue;

                    if (item.CorrectLocation && item.CorrectRotation)
                    {
                        Log(string.Format(Lang.GetText("{0} is already correctly placed"), item.Name));
                        continue;
                    }

                    SetItemPosition(item);
                    DalamudApi.Framework.RunOnTick(RecursivelyPlaceItems, TimeSpan.FromMilliseconds(Config.LoadInterval));
                    return;
                }

                if (Session.PendingItems.Count == 0) Log(Lang.GetText("Finished applying layout"));
            }
            catch (Exception e) { LogError(string.Format(Lang.GetText("Error: {0}"), e.Message), e.StackTrace ?? ""); }

            Cleanup();
            void Cleanup()
            {
                Memory.Instance.SetPlaceAnywhere(false);
                Session.IsActive = false; 
            }
        }

        unsafe public static void SetItemPosition(HousingItem rowItem)
        {
            if (!Memory.Instance.CanEditItem())
            {
                LogError(Lang.GetText("Unable to set position outside of Rotate Layout mode"));
                return;
            }

            if (rowItem.ItemStruct == IntPtr.Zero) return;

            var MemInstance = Memory.Instance;
            logHousingDetour = true;
            ApplyChange = true;

            SelectItem(rowItem.ItemStruct);

            var thisItem = MemInstance.HousingStructure->ActiveItem;
            if (thisItem == null)
            {
                LogError(Lang.GetText("Error occured while writing position! Item Was null"));
                return;
            }

            Vector3 position = new Vector3(rowItem.X, rowItem.Y, rowItem.Z);
            Vector3 rotation = new Vector3();
            rotation.Y = (float)(rowItem.Rotate * 180 / Math.PI);

            if (MemInstance.GetCurrentTerritory() == Memory.HousingArea.Outdoors)
            {
                var rotateVector = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -PlotLocation.rotation);
                position = Vector3.Transform(position, rotateVector) + PlotLocation.ToVector();
                rotation.Y = (float)((rowItem.Rotate - PlotLocation.rotation) * 180 / Math.PI);
            }
            MemInstance.WritePosition(position);
            MemInstance.WriteRotation(rotation);

            ClickItem(nint.Zero);

            rowItem.CorrectLocation = true;
            rowItem.CorrectRotation = true;
        }

        public void ApplyLayout()
        {
            if (Session.IsActive)
            {
                LogError(Lang.GetText("Already placing items"));
                return;
            }

            Session.IsActive = true;
            Log(string.Format(Lang.GetText("Applying layout with interval of {0}ms"), Config.LoadInterval));
            Session.PendingItems.Clear();

            List<HousingItem> placedLast = new List<HousingItem>();
            List<HousingItem> toBePlaced;

            if (Memory.Instance.GetCurrentTerritory() == Memory.HousingArea.Indoors)
            {
                toBePlaced = new List<HousingItem>();
                foreach (var houseItem in InteriorItemList)
                {
                    if (IsSelectedFloor(houseItem.Y)) toBePlaced.Add(houseItem);
                }
            }
            else toBePlaced = new List<HousingItem>(ExteriorItemList);

            foreach (var item in toBePlaced)
            {
                if (item.IsTableOrWallMounted) placedLast.Add(item);
                else Session.PendingItems.Enqueue(item);
            }

            foreach (var item in placedLast) Session.PendingItems.Enqueue(item);
            Session.TotalCount = Session.PendingItems.Count;
            RecursivelyPlaceItems();
        }

        public bool MatchItem(HousingItem item, uint itemKey)
        {
            if (item.ItemStruct != IntPtr.Zero) return false;       
            return item.ItemKey == itemKey && IsSelectedFloor(item.Y);
        }

        public unsafe bool MatchExactItem(HousingItem item, uint itemKey, HousingGameObject obj)
        {
            if (!MatchItem(item, itemKey)) return false;
            if (item.Stain != obj.color) return false;

            var matNumber = obj.Item->MaterialManager->MaterialSlot1;
            if (item.MaterialItemKey == 0 && matNumber == 0) return true;
            else if (item.MaterialItemKey != 0 && matNumber == 0) return false;

            var matItemKey = HousingData.Instance.GetMaterialItemKey(item.ItemKey, matNumber);
            if (matItemKey == 0) return true;

            return matItemKey == item.MaterialItemKey;
        }

        // 🚀【消灭魔法数字】使用 Constants 里的精度定义
        private unsafe void BindItemToGameObject(HousingItem houseItem, HousingGameObject gameObject, Vector3 localPosition, float localRotation)
        {
            if (houseItem == null) return;
            var locationError = houseItem.GetLocation() - localPosition;
            houseItem.CorrectLocation = locationError.LengthSquared() < Constants.Housing.LocationTolerance;
            houseItem.CorrectRotation = localRotation - houseItem.Rotate < Constants.Housing.RotationTolerance;
            houseItem.ItemStruct = (IntPtr)gameObject.Item;
        }

        public unsafe void MatchLayout()
        {
            List<HousingGameObject> allObjects = null;
            Memory Mem = Memory.Instance;
            Quaternion rotateVector = new();
            var currentTerritory = Mem.GetCurrentTerritory();

            switch (currentTerritory)
            {
                case HousingArea.Indoors:
                    Mem.TryGetNameSortedHousingGameObjectList(out allObjects);
                    InteriorItemList.ForEach(item => { item.ItemStruct = IntPtr.Zero; });
                    break;
                case HousingArea.Outdoors:
                    GetPlotLocation();
                    Mem.TryGetNameSortedHousingGameObjectList(out allObjects);
                    ExteriorItemList.ForEach(item => { item.ItemStruct = IntPtr.Zero; });
                    rotateVector = Quaternion.CreateFromAxisAngle(Vector3.UnitY, PlotLocation.rotation);
                    break;
                case HousingArea.Island:
                    Mem.TryGetIslandGameObjectList(out allObjects);
                    ExteriorItemList.ForEach(item => { item.ItemStruct = IntPtr.Zero; });
                    break;
            }

            List<HousingGameObject> unmatched = new List<HousingGameObject>();

            foreach (var gameObject in allObjects)
            {
                if (!IsSelectedFloor(gameObject.Y)) continue;

                uint furnitureKey = gameObject.housingRowId;
                HousingItem houseItem = null;

                Vector3 localPosition = new Vector3(gameObject.X, gameObject.Y, gameObject.Z);
                float localRotation = gameObject.rotation;

                if (currentTerritory == HousingArea.Indoors)
                {
                    var furniture = DalamudApi.DataManager.GetExcelSheet<HousingFurniture>().GetRow(furnitureKey);
                    var itemKey = furniture.Item.Value.RowId;
                    houseItem = Utils.GetNearestHousingItem(
                        InteriorItemList.Where(item => MatchExactItem(item, itemKey, gameObject)), localPosition);
                }
                else
                {
                    if (currentTerritory == HousingArea.Outdoors)
                    {
                        localPosition = Vector3.Transform(localPosition - PlotLocation.ToVector(), rotateVector);
                        localRotation += PlotLocation.rotation;
                    }
                    var furniture = DalamudApi.DataManager.GetExcelSheet<HousingYardObject>().GetRow(furnitureKey);
                    var itemKey = furniture.Item.Value.RowId;
                    houseItem = Utils.GetNearestHousingItem(
                        ExteriorItemList.Where(item => MatchExactItem(item, itemKey, gameObject)), localPosition);
                }

                if (houseItem == null)
                {
                    unmatched.Add(gameObject);
                    continue;
                }
                BindItemToGameObject(houseItem, gameObject, localPosition, localRotation);
            }

            UnusedItemList.Clear();

            foreach (var gameObject in unmatched)
            {
                uint furnitureKey = gameObject.housingRowId;
                HousingItem houseItem = null;
                Item item;
                Vector3 localPosition = new Vector3(gameObject.X, gameObject.Y, gameObject.Z);
                float localRotation = gameObject.rotation;

                if (currentTerritory == HousingArea.Indoors)
                {
                    var furniture = DalamudApi.DataManager.GetExcelSheet<HousingFurniture>().GetRow(furnitureKey);
                    item = furniture.Item.Value;
                    houseItem = Utils.GetNearestHousingItem(
                        InteriorItemList.Where(hItem => MatchItem(hItem, item.RowId)), new Vector3(gameObject.X, gameObject.Y, gameObject.Z));
                }
                else
                {
                    if (currentTerritory == HousingArea.Outdoors)
                    {
                        localPosition = Vector3.Transform(localPosition - PlotLocation.ToVector(), rotateVector);
                        localRotation += PlotLocation.rotation;
                    }
                    var furniture = DalamudApi.DataManager.GetExcelSheet<HousingYardObject>().GetRow(furnitureKey);
                    item = furniture.Item.Value;
                    houseItem = Utils.GetNearestHousingItem(
                        ExteriorItemList.Where(hItem => MatchItem(hItem, item.RowId)), localPosition);
                }
                
                if (houseItem == null)
                {
                    var unmatchedItem = new HousingItem(item, gameObject.color, gameObject.X, gameObject.Y, gameObject.Z, gameObject.rotation);
                    UnusedItemList.Add(unmatchedItem);
                    continue;
                }

                BindItemToGameObject(houseItem, gameObject, localPosition, localRotation);
                houseItem.DyeMatch = false;
            }
        }

        public unsafe void GetPlotLocation()
        {
            var mgr = Memory.Instance.HousingModule->outdoorTerritory;
            var plotNumber = mgr->Plot + 1;
            if (plotNumber == 256)
            {
                LogError(Lang.GetText("Not inside a valid Plot"));
                PlotLocation = new Location();
                return;
            }

            var territoryId = Memory.Instance.GetTerritoryTypeId();
            TerritoryType row = DalamudApi.DataManager.GetExcelSheet<TerritoryType>().GetRow(territoryId);
            if (row.Equals(null)) return;

            var placeName = row.Name.ToString();
            PlotLocation = Plots.Map[placeName][plotNumber];
        }

        public unsafe void LoadExterior()
        {
            SaveLayoutManager.LoadExteriorFixtures();

            List<HousingGameObject> objects;
            var playerPos = DalamudApi.ObjectTable.LocalPlayer.Position;
            Memory.Instance.GetExteriorPlacedObjects(out objects, playerPos);
            ExteriorItemList.Clear();
            GetPlotLocation();
            
            var rotateVector = Quaternion.CreateFromAxisAngle(Vector3.UnitY, PlotLocation.rotation);

            foreach (var gameObject in objects)
            {
                uint furnitureKey = gameObject.housingRowId;
                var furniture = DalamudApi.DataManager.GetExcelSheet<HousingYardObject>().GetRow(furnitureKey);
                Item? item = furniture.Item.Value;
                if (item == null || item.Equals(0)) continue;

                // 🚀【消灭魔法数字】使用 Constants 里的标准房屋尺寸
                var xMax = 0.0;
                var yMax = Constants.Housing.MaxExteriorHeight;
                var zMax = 0.0;
                switch (PlotLocation.size)
                {
                    case "l": xMax = Constants.Housing.LargePlotX; zMax = Constants.Housing.LargePlotZ; break;
                    case "m": xMax = Constants.Housing.MediumPlotX; zMax = Constants.Housing.MediumPlotZ; break;
                    case "s": xMax = Constants.Housing.SmallPlotX; zMax = Constants.Housing.SmallPlotZ; break;
                    default: yMax = 0; break;
                }

                var housingItem = new HousingItem(item.Value, gameObject);
                housingItem.ItemStruct = (IntPtr)gameObject.Item;

                var location = new Vector3(housingItem.X, housingItem.Y, housingItem.Z);
                var newLocation = Vector3.Transform(location - PlotLocation.ToVector(), rotateVector);

                housingItem.X = newLocation.X;
                housingItem.Y = newLocation.Y;
                housingItem.Z = newLocation.Z;
                housingItem.Rotate += PlotLocation.rotation;

                if (!(Math.Abs(housingItem.X) > xMax || Math.Abs(housingItem.Y) > yMax || Math.Abs(housingItem.Z) > zMax))
                {
                    ExteriorItemList.Add(housingItem);
                }
            }
            Config.Save();
        }

        public bool IsSelectedFloor(float y)
        {
            if (Memory.Instance.GetCurrentTerritory() != Memory.HousingArea.Indoors || Memory.Instance.GetIndoorHouseSize().Equals("Apartment")) return true;

            // 🚀【消灭魔法数字】使用 Constants 里的楼层高度判定
            if (y < Constants.Housing.BasementThreshold) return Config.Basement;
            if (y >= Constants.Housing.BasementThreshold && y < Constants.Housing.UpperFloorThreshold) return Config.GroundFloor;

            if (y >= Constants.Housing.UpperFloorThreshold)
            {
                if (Memory.Instance.HasUpperFloor()) return Config.UpperFloor;
                else return Config.GroundFloor;
            }

            return false;
        }

        public unsafe void LoadInterior()
        {
            SaveLayoutManager.LoadInteriorFixtures();
            List<HousingGameObject> dObjects;
            Memory.Instance.TryGetNameSortedHousingGameObjectList(out dObjects);
            InteriorItemList.Clear();

            foreach (var gameObject in dObjects)
            {
                uint furnitureKey = gameObject.housingRowId;
                var furniture = DalamudApi.DataManager.GetExcelSheet<HousingFurniture>().GetRow(furnitureKey);
                Item item = furniture.Item.Value;

                if (item.Equals(null) || item.RowId == 0) continue;
                if (!IsSelectedFloor(gameObject.Y)) continue;

                var housingItem = new HousingItem(item, gameObject);
                housingItem.ItemStruct = (IntPtr)gameObject.Item;

                if (gameObject.Item != null && gameObject.Item->MaterialManager != null)
                {
                    ushort material = gameObject.Item->MaterialManager->MaterialSlot1;
                    housingItem.MaterialItemKey = HousingData.Instance.GetMaterialItemKey(item.RowId, material);
                }

                InteriorItemList.Add(housingItem);
            }
            Config.Save();
        }

        public unsafe void LoadIsland()
        {
            SaveLayoutManager.LoadIslandFixtures();
            List<HousingGameObject> objects;
            Memory.Instance.TryGetIslandGameObjectList(out objects);
            ExteriorItemList.Clear();

            foreach (var gameObject in objects)
            {
                uint furnitureKey = gameObject.housingRowId;
                var furniture = DalamudApi.DataManager.GetExcelSheet<HousingYardObject>().GetRow(furnitureKey);
                Item item = furniture.Item.Value;

                if (item.Equals(null) || item.RowId == 0) continue;

                var housingItem = new HousingItem(item, gameObject);
                housingItem.ItemStruct = (IntPtr)gameObject.Item;
                ExteriorItemList.Add(housingItem);
            }
            Config.Save();
        }

        public void GetGameLayout()
        {
            Memory Mem = Memory.Instance;
            var currentTerritory = Mem.GetCurrentTerritory();
            var itemList = currentTerritory == HousingArea.Indoors ? InteriorItemList : ExteriorItemList;
            itemList.Clear();

            switch (currentTerritory)
            {
                case HousingArea.Outdoors: LoadExterior(); break;
                case HousingArea.Indoors: LoadInterior(); break;
                case HousingArea.Island: LoadIsland(); break;
            }

            Log(string.Format(Lang.GetText("Loaded {0} furniture items"), itemList.Count));
            Config.HiddenScreenItemHistory = new List<int>();
            Config.Save();
        }

        private void TerritoryChanged(ushort e)
        {
            Config.DrawScreen = false;
            Config.Save();
        }

        public unsafe void CommandHandler(string command, string arguments)
        {
            var args = arguments.Trim().Replace("\"", string.Empty);
            try
            {
                if (string.IsNullOrEmpty(args) || args.Equals("config", StringComparison.OrdinalIgnoreCase))
                {
                    Gui.ConfigWindow.IsOpen = !Gui.ConfigWindow.IsOpen;
                }
            }
            catch (Exception e) { LogError(e.Message, e.StackTrace ?? ""); }
        }

        public static void Log(string message, string detail_message = "")
        {
            var msg = $"{message}";
            DalamudApi.PluginLog.Info(detail_message == "" ? msg : detail_message);
            DalamudApi.ChatGui.Print(msg);
        }

        public static void LogError(string message, string detail_message = "")
        {
            var msg = $"{message}";
            DalamudApi.PluginLog.Error(msg);
            if (detail_message.Length > 0) DalamudApi.PluginLog.Error(detail_message);
            DalamudApi.ChatGui.PrintError(msg);
        }
    }
}