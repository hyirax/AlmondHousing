using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Lumina.Excel.Sheets;

namespace AlmondHousing
{
    public unsafe class Memory
    {
        public static GetInventoryContainerDelegate GetInventoryContainer;
        public delegate InventoryContainer* GetInventoryContainerDelegate(IntPtr inventoryManager, InventoryType inventoryType);

        // 🚀 整合 BDTH 核心：全套内存断脉补丁地址
        public IntPtr placeAnywhere;
        public IntPtr wallAnywhere;
        public IntPtr wallmountAnywhere;
        public IntPtr showcasePlaceAddress;
        public IntPtr showcaseRotateAddress;

        // 🚀 修复报错方案：绝对路径强制引用，无视警告检查
#pragma warning disable CS8500
        public static unsafe FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera* Camera => &FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager.Instance()->GetActiveCamera()->CameraBase.SceneCamera;
#pragma warning restore CS8500

        // 🚀 BDTH 底裤 2：家具模型瞬间强刷委托 (让微调不用重新选中就能看到移动)
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void HousingLayoutModelUpdateDelegate(nint item);
        public HousingLayoutModelUpdateDelegate HousingLayoutModelUpdate = null!;

        // 🚀 整合 BDTH 核心：备份原厂机器码，供还原使用
        private byte[] showcasePlaceOriginal;
        private byte[] showcaseRotateOriginal;
        private byte[] showcasePlaceNoop;
        private byte[] showcaseRotateNoop;

        private Memory()
        {
            try
            {
                // 1. 普通家具与墙壁限制解除
                placeAnywhere = DalamudApi.SigScanner.ScanText("C6 83 ?? ?? ?? ?? ?? 0F 29 44 24") + 6;
                wallAnywhere = DalamudApi.SigScanner.ScanText("48 85 C0 74 ?? C6 87 ?? ?? 00 00 00") + 11;
                wallmountAnywhere = DalamudApi.SigScanner.ScanText("c6 87 83 01 00 00 00 48 83 c4 ??") + 6;

                // 2. 展柜与特殊摆件限制解除
                showcasePlaceAddress = DalamudApi.SigScanner.ScanText("C6 87 ?? ?? 00 00 00 48 8B BC 24 ?? ?? ?? ?? 48");
                showcaseRotateAddress = DalamudApi.SigScanner.ScanText("88 87 ?? ?? 00 00 0F 28 74 24 ?? 48 8B");

                if (showcasePlaceAddress != IntPtr.Zero && showcaseRotateAddress != IntPtr.Zero)
                {
                    showcasePlaceOriginal = ReadBytes(showcasePlaceAddress, 7);
                    showcaseRotateOriginal = ReadBytes(showcaseRotateAddress, 6);
                    
                    showcasePlaceNoop = Enumerable.Repeat((byte)0x90, showcasePlaceOriginal.Length).ToArray();
                    showcaseRotateNoop = Enumerable.Repeat((byte)0x90, showcaseRotateOriginal.Length).ToArray();
                }

                housingModulePtr = DalamudApi.SigScanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 8B 52");
                LayoutWorldPtr = DalamudApi.SigScanner.GetStaticAddressFromSig("48 8B D1 48 8B 0D ?? ?? ?? ?? 48 85 C9 74 0A", 3);

                var getInventoryContainerPtr = DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? 40 38 78 18");
                GetInventoryContainer = Marshal.GetDelegateForFunctionPointer<GetInventoryContainerDelegate>(getInventoryContainerPtr);

                // 🚀 扫描并绑定瞬间强刷函数
                var housingLayoutModelUpdateAddress = DalamudApi.SigScanner.ScanText("48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 50 48 8B E9 48 8B 49");
                HousingLayoutModelUpdate = Marshal.GetDelegateForFunctionPointer<HousingLayoutModelUpdateDelegate>(housingLayoutModelUpdateAddress);
            }
            catch (Exception e)
            {
                DalamudApi.PluginLog.Error(e, "Could not load housing memory!!");
            }
        }

        public static Memory Instance { get; private set; }
        public IntPtr housingModulePtr { get; }
        public IntPtr LayoutWorldPtr { get; }

        public unsafe HousingModule* HousingModule => housingModulePtr != IntPtr.Zero ? (HousingModule*)Marshal.ReadIntPtr(housingModulePtr) : null;
        public unsafe LayoutWorld* LayoutWorld => LayoutWorldPtr != IntPtr.Zero ? (LayoutWorld*)Marshal.ReadIntPtr(LayoutWorldPtr) : null;
        public unsafe HousingObjectManager* CurrentManager => HousingModule->currentTerritory;
        public unsafe HousingStructure* HousingStructure => LayoutWorld->HousingStruct;

        public static void Init() => Instance = new Memory();
        public static InventoryContainer* GetContainer(InventoryType inventoryType) => InventoryManager.Instance()->GetInventoryContainer(inventoryType);
        public uint GetTerritoryTypeId() => GetActiveLayout(out var manager) ? manager.TerritoryTypeId : 0;
        public bool HasUpperFloor() => GetIndoorHouseSize() == "Medium" || GetIndoorHouseSize() == "Large";

        public string GetIndoorHouseSize()
        {
            var territoryId = GetTerritoryTypeId();
            var row = DalamudApi.DataManager.GetExcelSheet<TerritoryType>().GetRow(territoryId);
            if (row.Equals(null)) return null;
            var placeName = row.Name.ToString();
            if (string.IsNullOrEmpty(placeName) || placeName.Length < 4) return null;
            var sizeName = placeName.Substring(2, 2);
            return sizeName switch { "i1" => "Small", "i2" => "Medium", "i3" => "Large", "i4" => "Apartment", _ => null };
        }

        public float GetInteriorLightLevel() => (GetCurrentTerritory() == HousingArea.Indoors && GetActiveLayout(out var m) && m.IndoorAreaData.HasValue) ? m.IndoorAreaData.Value.LightLevel : 0f;

        public CommonFixture[] GetInteriorCommonFixtures(int floorId)
        {
            if (GetCurrentTerritory() != HousingArea.Indoors || !GetActiveLayout(out var m) || !m.IndoorAreaData.HasValue) return new CommonFixture[0];
            var floor = m.IndoorAreaData.Value.GetFloor(floorId);
            var ret = new CommonFixture[IndoorFloorData.PartsMax];
            for (var i = 0; i < IndoorFloorData.PartsMax; i++)
            {
                var key = floor.GetPart(i);
                if (!HousingData.Instance.TryGetItem(unchecked((uint)key), out var item)) HousingData.Instance.IsUnitedExteriorPart(unchecked((uint)key), out item);
                ret[i] = new CommonFixture(false, i, key, null, item);
            }
            return ret;
        }

        public CommonFixture[] GetExteriorCommonFixtures(int plotId)
        {
            if (GetCurrentTerritory() != HousingArea.Outdoors || !GetHousingController(out var c)) return new CommonFixture[0];
            var home = c.Houses(plotId);
            if (home.Size == -1 || home.GetPart(0).Category == -1) return new CommonFixture[0];
            var ret = new CommonFixture[HouseCustomize.PartsMax];
            for (var i = 0; i < HouseCustomize.PartsMax; i++)
            {
                var colorId = home.GetPart(i).Color;
                HousingData.Instance.TryGetStain(colorId, out var stain);
                HousingData.Instance.TryGetItem(home.GetPart(i).FixtureKey, out var item);
                ret[i] = new CommonFixture(true, home.GetPart(i).Category, home.GetPart(i).FixtureKey, stain, item);
            }
            return ret;
        }
        
        public unsafe bool GetExteriorPlacedObjects(out List<HousingGameObject> objects, Vector3 playerPos) 
        {
            objects = null;
            if (HousingModule == null || HousingModule->GetCurrentManager() == null || HousingModule->GetCurrentManager()->Objects == null) return false;
            var tmpObjects = new List<(HousingGameObject gObject, float distance)>(400);
            objects = new List<HousingGameObject>(400);
            var curMgr = HousingModule->outdoorTerritory;
            nint* objectsArrayPtr = (nint*)curMgr->Objects;
            ReadOnlySpan<nint> pointerSpan = new ReadOnlySpan<nint>(objectsArrayPtr, 400);
            foreach (ref readonly nint oPtr in pointerSpan)
            {
                if (oPtr == 0) continue;
                var o = *(HousingGameObject*)oPtr;
                tmpObjects.Add((o, Utils.DistanceFromPlayer(o, playerPos)));
            }
            tmpObjects.Sort((obj1, obj2) => obj1.distance.CompareTo(obj2.distance));
            foreach (var item in tmpObjects) objects.Add(item.gObject);
            return true;
        }

        public unsafe bool TryGetIslandGameObjectList(out List<HousingGameObject> objects)
        {
            objects = new List<HousingGameObject>(200);
            var manager = (MjiManagerExtended*)MJIManager.Instance();
            var objectManager = manager->ObjectManager;
            var furnManager = objectManager->FurnitureManager;
            nint* objectsArrayPtr = (nint*)furnManager->Objects;
            ReadOnlySpan<nint> pointerSpan = new ReadOnlySpan<nint>(objectsArrayPtr, 200);
            foreach (ref readonly nint objPtr in pointerSpan)
            {
                if (objPtr == 0) continue;
                objects.Add(*(HousingGameObject*)objPtr);
            }
            return true;
        }

        public unsafe bool TryGetNameSortedHousingGameObjectList(out List<HousingGameObject> objects)
        {
            objects = new List<HousingGameObject>();
            if (HousingModule == null || HousingModule->GetCurrentManager() == null || HousingModule->GetCurrentManager()->Objects == null) return false;
            var manager = HousingModule->GetCurrentManager();
            nint* objectsArrayPtr = (nint*)manager->Objects; 
            ReadOnlySpan<nint> pointerSpan = new ReadOnlySpan<nint>(objectsArrayPtr, 600);
            var tempList = new List<(HousingGameObject Obj, string Name)>(600);
            foreach (ref readonly nint oPtr in pointerSpan)
            {
                if (oPtr == 0) continue;
                var o = *(HousingGameObject*)oPtr;
                string itemName = "";
                if (HousingData.Instance.TryGetFurniture(o.housingRowId, out var furniture)) itemName = furniture.Item.Value.Name.ToString(); 
                else if (HousingData.Instance.TryGetYardObject(o.housingRowId, out var yardObject)) itemName = yardObject.Item.Value.Name.ToString();
                tempList.Add((o, itemName));
            }
            tempList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            objects.Capacity = tempList.Count;
            foreach (var item in tempList) objects.Add(item.Obj);
            return true;
        }

        public unsafe bool GetActiveLayout(out LayoutManager manager)
        {
            manager = new LayoutManager();
            if (LayoutWorld == null || LayoutWorld->ActiveLayout == null) return false;
            manager = *LayoutWorld->ActiveLayout;
            return true;
        }

        public bool GetHousingController(out HousingController controller)
        {
            controller = new HousingController();
            if (!GetActiveLayout(out var manager) || !manager.HousingController.HasValue) return false;
            controller = manager.HousingController.Value;
            return true;
        }

        public enum HousingArea { Indoors, Outdoors, Island, None }

        public unsafe HousingArea GetCurrentTerritory()
        {
            var territoryRow = DalamudApi.DataManager.GetExcelSheet<TerritoryType>().GetRow(GetTerritoryTypeId());
            if (territoryRow.Equals(null) || territoryRow.Name.ToString().Equals("r1i5")) return HousingArea.None;
            if (territoryRow.Name.ToString().Equals("h1m2")) return HousingArea.Island;
            if (HousingModule == null) return HousingArea.None;
            return HousingModule->IsOutdoors() ? HousingArea.Outdoors : HousingArea.Indoors;
        }

        public unsafe bool IsHousingMode() => HousingStructure != null && HousingStructure->Mode != HousingLayoutMode.None;
        public unsafe bool CanEditItem() => HousingStructure != null && HousingStructure->Mode == HousingLayoutMode.Rotate;

        public unsafe void WritePosition(Vector3 newPosition)
        {
            if (!CanEditItem()) return;
            try { var item = HousingStructure->ActiveItem; if (item != null) item->Position = newPosition; }
            catch (Exception ex) { DalamudApi.PluginLog.Error(ex, "Error occured while writing position!"); }
        }

        public unsafe void WriteRotation(Vector3 newRotation)
        {
            if (!CanEditItem()) return;
            try { var item = HousingStructure->ActiveItem; if (item != null) item->Rotation = MoveUtil.ToQ(newRotation); }
            catch (Exception ex) { DalamudApi.PluginLog.Error(ex, "Error occured while writing rotation!"); }
        }

        public void SetPlaceAnywhere(bool state)
        {
            if (placeAnywhere == IntPtr.Zero || wallAnywhere == IntPtr.Zero || wallmountAnywhere == IntPtr.Zero) return;

            var bstate = (byte)(state ? 1 : 0);
            WriteProtectedBytes(placeAnywhere, bstate);
            WriteProtectedBytes(wallAnywhere, bstate);
            WriteProtectedBytes(wallmountAnywhere, bstate);

            if (showcasePlaceAddress != IntPtr.Zero && showcaseRotateAddress != IntPtr.Zero && showcasePlaceOriginal != null)
            {
                WriteProtectedBytes(showcasePlaceAddress, state ? showcasePlaceNoop : showcasePlaceOriginal);
                WriteProtectedBytes(showcaseRotateAddress, state ? showcaseRotateNoop : showcaseRotateOriginal);
            }
        }

        private static byte[] ReadBytes(IntPtr addr, int length)
        {
            var bytes = new byte[length];
            Marshal.Copy(addr, bytes, 0, length);
            return bytes;
        }

        private static void WriteProtectedBytes(IntPtr addr, byte[] b)
        {
            if (addr == IntPtr.Zero) return;
            VirtualProtect(addr, 1, Protection.PAGE_EXECUTE_READWRITE, out var oldProtection);
            Marshal.Copy(b, 0, addr, b.Length);
            VirtualProtect(addr, 1, oldProtection, out _);
        }

        private static void WriteProtectedBytes(IntPtr addr, byte b)
        {
            if (addr == IntPtr.Zero) return;
            WriteProtectedBytes(addr, [b]);
        }

        #region Kernel32
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, Protection flNewProtect, out Protection lpflOldProtect);

        public enum Protection
        {
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400
        }
        #endregion
    }
}