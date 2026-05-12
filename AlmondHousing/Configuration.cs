using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace AlmondHousing
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        // 【新增的这一行】：用于保存玩家选择的语言，默认设为 "zh" (中文)
        public string UILanguage { get; set; } = "zh"; // 默认使用简体中文
        public bool ShowTooltips = true;
        public bool DrawScreen = false;
        public float DrawDistance = 0;
        public List<int> HiddenScreenItemHistory = new List<int>();
        public List<int> GroupingList = new List<int>();
        
        public bool Basement = true;
        public bool GroundFloor = true;
        public bool UpperFloor = true;

        public bool EnableDyeSyncPlaced { get; set; } = true;
        public bool EnableDyeOnPlacement { get; set; } = true;

        public int LoadInterval = 400;
        public bool ApplyLayout = true;

        public string SaveLocation = null;

        public void Save()
        {
            DalamudApi.PluginInterface.SavePluginConfig(this);
        }

        public void ResetRecord()
        {
            HiddenScreenItemHistory.Clear();
            GroupingList.Clear();
            Save();
        }

    }
}