using Dalamud.Game.Command;
using Dalamud.Plugin;
using Lumina.Excel.Sheets;
using AlmondHousing.Objects;
using AlmondHousing.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using static AlmondHousing.Memory;
using HousingFurniture = Lumina.Excel.Sheets.HousingFurniture;

namespace AlmondHousing
{
    public sealed unsafe class AlmondHousing : IDalamudPlugin
    {
        public static LanguageManager Lang { get; private set; } = new LanguageManager();
        public static string PluginDirectory { get; private set; } = string.Empty;
        private IDalamudPluginInterface PluginInterface { get; init; }

        public string Name => "AlmondHousing";
        public PluginUi Gui { get; private set; }
        public Configuration Config { get; private set; }
        public static AlmondHousing Instance { get; private set; } = null!;

        public static PlacementSession Session = new PlacementSession();

        // 🚀 控制内嵌 BDTH 全区放置的大总管开关
        public static bool UseEmbeddedBDTH { get; set; } = true;

        private delegate bool UpdateLayoutDelegate(HousingStructure* layoutWorld);
        public delegate void SelectItemDelegate(HousingStructure* housingStruct, nint item);
        private static HookWrapper<SelectItemDelegate> SelectItemHook = null!;
        public delegate void ClickItemDelegate(HousingStructure* housingStruct, nint item);
        private static HookWrapper<ClickItemDelegate> ClickItemHook = null!;

        internal delegate nint GetObjectDelegate(nint objList, ushort index);
        internal delegate nint GetActiveObjectDelegate(nint objList, uint index);
        internal delegate ushort GetIndexDelegate(byte plotNumber, ushort inventoryIndex);

        internal static HookWrapper<GetObjectDelegate> GetGameObjectHook = null!;
        internal static HookWrapper<GetActiveObjectDelegate> GetObjectFromIndexHook = null!;
        internal static HookWrapper<GetIndexDelegate> GetYardIndexHook = null!;

        internal delegate void MaybePlaced(HousingStructure* housingPtr, nint itemPtr, long a); 
        internal delegate void ResetItemPlacementd(HousingStructure* housingPtr, long a); 
        internal delegate void FinalizeHousingd(HousingStructure* housingPtr, long a, nint b); 
        internal delegate void PlaceCalld(HousingStructure* housingPtr, long a, nint b); 

        internal static HookWrapper<MaybePlaced> MaybePlaceh = null!;
        internal static HookWrapper<ResetItemPlacementd> ResetItemPlacementh = null!;
        internal static HookWrapper<FinalizeHousingd> FinalizeHousingh = null!;
        internal static HookWrapper<PlaceCalld> PlaceCallh = null!;

        public static bool ApplyChange = false;
        public static SaveLayoutManager LayoutManager = null!;
        public static bool logHousingDetour = false;
        internal static Location PlotLocation = new Location();

        public Layout Layout = new Layout();
        public List<HousingItem> InteriorItemList = new List<HousingItem>();
        public List<HousingItem> ExteriorItemList = new List<HousingItem>();
        public List<HousingItem> UnusedItemList = new List<HousingItem>();

        public AlmondHousing(IDalamudPluginInterface pi)
        {
            Instance = this;
            PluginInterface = pi;
            DalamudApi.Initialize(pi);
            Config = DalamudApi.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Config.Save();

            InitializeHooks();

            DalamudApi.CommandManager.AddHandler("/almond", new CommandInfo(CommandHandler) { HelpMessage = "打开 AlmondHousing 面板。" });
            Gui = new PluginUi(this);

            HousingData.Init(this);
            Memory.Init();

            // 🚀 主动开启一次全盘动态内存幽灵扫描
            CheckBDTHCompatibility();
            if (UseEmbeddedBDTH && Instance.Config.EnableQuantumPlace)
            {
                Memory.Instance.SetPlaceAnywhere(true);
            }

            // ⚡⚡⚡ 注入高阶 3D 微调 UI ⚡⚡⚡
            DalamudApi.PluginInterface.UiBuilder.Draw += AdvancedTuningUI.Draw;

            LayoutManager = new SaveLayoutManager(this, Config);
            DalamudApi.PluginLog.Info("AlmondHousing Plugin initialized");

            PluginDirectory = pi.AssemblyLocation.DirectoryName ?? string.Empty;
            if (!string.IsNullOrEmpty(PluginDirectory))
            {
                Lang.LoadAllLanguages(PluginDirectory);
                Lang.SetLanguage(Config.UILanguage);
                DalamudApi.PluginLog.Info($"[汉化系统] 翻译官启动！当前语言设定为: {Config.UILanguage}");
            }
        }

        // 👑【动态内存扫描内核】：彻底删除对 IPluginManager 的编译期依赖，100% 防报错
        public static void CheckBDTHCompatibility()
        {
            try
            {
                // 🛰️ 扫描当前游戏进程执行上下文里加载的所有托管程序集
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                
                // 如果发现名叫 BDTHPlugin 的程序集存在，说明原版 BDTH 正在运行
                bool isOriginalBDTHActive = assemblies.Any(a => a.GetName().Name == "BDTHPlugin");

                if (isOriginalBDTHActive)
                {
                    UseEmbeddedBDTH = false;
                    // 原版已加载，静默将让出内存修改权以防冲突
                    try { Memory.Instance.SetPlaceAnywhere(false); } catch { }
                }
                else
                {
                    UseEmbeddedBDTH = true;
                }
            }
            catch
            {
                // 异常保底，默认允许内嵌版运行
                UseEmbeddedBDTH = true;
            }
        }

        public void InitializeHooks()
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

        internal static ushort GetYardIndex(byte plotNumber, ushort inventoryIndex) => GetYardIndexHook.Original(plotNumber, inventoryIndex);
        internal static nint GetObjectFromIndex(nint objList, uint index) => GetObjectFromIndexHook.Original(objList, index);
        internal static nint GetGameObject(nint objList, ushort index) => GetGameObjectHook.Original(objList, index);

        public static void SelectItemDetour(HousingStructure* housing, nint item) => SelectItemHook.Original(housing, item);
        public static void SelectItem(nint item) => SelectItemDetour(Memory.Instance.HousingStructure, item);

        public static void MaybePlacedt(HousingStructure* housingPtr, nint itemPtr, long a) => MaybePlaceh.Original(housingPtr, itemPtr, a);
        public static void MaybePlace(HousingStructure* housingPtr, nint itemPtr, long a) => MaybePlacedt(housingPtr, itemPtr, a);

        public static void ResetItemPlacementdt(HousingStructure* housingPtr, long a) => ResetItemPlacementh.Original(housingPtr, a);
        public static void Hc1(HousingStructure* housingPtr, long a) => ResetItemPlacementdt(housingPtr, a);

        public static void FinalizeHousingdt(HousingStructure* housingPtr, long a, nint b) => FinalizeHousingh.Original(housingPtr, a, b);
        public static void FinalizeHousing(HousingStructure* housingPtr, long a, nint b) => FinalizeHousingdt(housingPtr, a, b);

        public static void PlaceCalldt(HousingStructure* housingPtr, long a, nint b) => PlaceCallh.Original(housingPtr, a, b);
        public static void PlaceCall(HousingStructure* housingPtr, long a, nint b) => PlaceCalldt(housingPtr, a, b);

        public static void ClickItemDetour(HousingStructure* housing, nint item) => ClickItemHook.Original(Memory.Instance.HousingStructure, item);
        public static void ClickItem(nint item) => ClickItemDetour(Memory.Instance.HousingStructure, item);

        public void RecursivelyPlaceItems()
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
                    if (item.ItemStruct == nint.Zero) continue;

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
                if (UseEmbeddedBDTH && !Instance.Config.EnableQuantumPlace)
                {
                    Memory.Instance.SetPlaceAnywhere(false);
                }
                Session.IsActive = false; 
            }
        }

        public static void SetItemPosition(HousingItem rowItem)
        {
            if (!Memory.Instance.CanEditItem())
            {
                LogError(Lang.GetText("Unable to set position outside of Rotate Layout mode"));
                return;
            }

            if (rowItem.ItemStruct == nint.Zero) return;

            var memInstance = Memory.Instance;
            logHousingDetour = true;
            ApplyChange = true;

            SelectItem(rowItem.ItemStruct);

            var thisItem = memInstance.HousingStructure->ActiveItem;
            if (thisItem == null)
            {
                LogError(Lang.GetText("Error occured while writing position! Item Was null"));
                return;
            }

            Vector3 position = new Vector3(rowItem.X, rowItem.Y, rowItem.Z);
            Vector3 rotation = new Vector3(0, (float)(rowItem.Rotate * 180 / Math.PI), 0);

            if (memInstance.GetCurrentTerritory() == Memory.HousingArea.Outdoors)
            {
                var rotateVector = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -PlotLocation.rotation);
                position = Vector3.Transform(position, rotateVector) + PlotLocation.ToVector();
                rotation.Y = (float)((rowItem.Rotate - PlotLocation.rotation) * 180 / Math.PI);
            }
            
            // 🚀 排布之前实时雷达探针重测，决定是由内嵌解锁还是原版解锁
            CheckBDTHCompatibility();
            if (UseEmbeddedBDTH && Instance.Config.EnableQuantumPlace)
            {
                Memory.Instance.SetPlaceAnywhere(true);
            }

            memInstance.WritePosition(position);
            memInstance.WriteRotation(rotation);

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
            if (item.ItemStruct != nint.Zero) return false;       
            return item.ItemKey == itemKey && IsSelectedFloor(item.Y);
        }

        public bool MatchExactItem(HousingItem item, uint itemKey, HousingGameObject obj)
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

        private void BindItemToGameObject(HousingItem houseItem, HousingGameObject gameObject, Vector3 localPosition, float localRotation)
        {
            if (houseItem == null) return;
            var locationError = houseItem.GetLocation() - localPosition;
            houseItem.CorrectLocation = locationError.LengthSquared() < Constants.Housing.LocationTolerance;
            houseItem.CorrectRotation = localRotation - houseItem.Rotate < Constants.Housing.RotationTolerance;
            houseItem.ItemStruct = (nint)gameObject.Item;
        }

        public void MatchLayout()
        {
            List<HousingGameObject> allObjects = null;
            Memory mem = Memory.Instance;
            Quaternion rotateVector = new();
            var currentTerritory = mem.GetCurrentTerritory();

            switch (currentTerritory)
            {
                case HousingArea.Indoors:
                    mem.TryGetNameSortedHousingGameObjectList(out allObjects);
                    InteriorItemList.ForEach(item => { item.ItemStruct = nint.Zero; });
                    break;
                case HousingArea.Outdoors:
                    GetPlotLocation();
                    mem.TryGetNameSortedHousingGameObjectList(out allObjects);
                    ExteriorItemList.ForEach(item => { item.ItemStruct = nint.Zero; });
                    rotateVector = Quaternion.CreateFromAxisAngle(Vector3.UnitY, PlotLocation.rotation);
                    break;
                case HousingArea.Island:
                    mem.TryGetIslandGameObjectList(out allObjects);
                    ExteriorItemList.ForEach(item => { item.ItemStruct = nint.Zero; });
                    break;
            }

            if (allObjects == null) return;
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

        public void GetPlotLocation()
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

        public void LoadExterior()
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
                housingItem.ItemStruct = (nint)gameObject.Item;

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

            if (y < Constants.Housing.BasementThreshold) return Config.Basement;
            if (y >= Constants.Housing.BasementThreshold && y < Constants.Housing.UpperFloorThreshold) return Config.GroundFloor;

            if (y >= Constants.Housing.UpperFloorThreshold)
            {
                if (Memory.Instance.HasUpperFloor()) return Config.UpperFloor;
                else return Config.GroundFloor;
            }

            return false;
        }

        public void LoadInterior()
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
                housingItem.ItemStruct = (nint)gameObject.Item;

                if (gameObject.Item != null && gameObject.Item->MaterialManager != null)
                {
                    ushort material = gameObject.Item->MaterialManager->MaterialSlot1;
                    housingItem.MaterialItemKey = HousingData.Instance.GetMaterialItemKey(item.RowId, material);
                }

                InteriorItemList.Add(housingItem);
            }
            Config.Save();
        }

        public void LoadIsland()
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
                housingItem.ItemStruct = (nint)gameObject.Item;
                ExteriorItemList.Add(housingItem);
            }
            Config.Save();
        }

        public void GetGameLayout()
        {
            Memory mem = Memory.Instance;
            var currentTerritory = mem.GetCurrentTerritory();
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

        public void CommandHandler(string command, string arguments)
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

        public void Dispose()
        {
            HookManager.Dispose();
            try { Memory.Instance.SetPlaceAnywhere(false); }
            catch (Exception ex) { DalamudApi.PluginLog.Error(ex, "Error while calling PluginMemory.Dispose()"); }

            // ⚡⚡⚡ 断开高阶 UI 电源防止泄漏 ⚡⚡⚡
            DalamudApi.PluginInterface.UiBuilder.Draw -= AdvancedTuningUI.Draw;

            DalamudApi.CommandManager.RemoveHandler("/almond");
            Gui?.Dispose();
        }
    }
}