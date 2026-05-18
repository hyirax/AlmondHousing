using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace AlmondHousing
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        // 🚀 【必须新增的这一行】：用于永久保存防倒卖系统的解锁状态
        public bool IsActivated { get; set; } = false;

        // 用于保存玩家选择的语言，默认设为 "zh" (中文)
        public string UILanguage { get; set; } = "zh"; // 默认使用简体中文
        
        public bool ShowTooltips = true;
        public bool DrawScreen = false;
        public float DrawDistance = 0;
        public List<int> HiddenScreenItemHistory = new List<int>();
        public List<int> GroupingList = new List<int>();
        
        public bool Basement = true;
        public bool GroundFloor = true;
        public bool UpperFloor = true;

        public int LoadInterval = 400;
        public bool ApplyLayout = true;
        public bool EnableQuantumPlace = false;
        public bool UseGizmo = false;    // 是否在屏幕上显示 3D 拖拽轴
        public bool DoSnap = false;      // 是否开启拖拽吸附
        public float Drag = 0.05f;       // 鼠标滚轮/拖拽的微调精度

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