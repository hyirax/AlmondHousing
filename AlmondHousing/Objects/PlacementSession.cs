using System.Collections.Generic;

namespace AlmondHousing.Objects
{
    /// <summary>
    /// 房屋建造会话管理器 (管理排队队列与施工进度)
    /// </summary>
    public class PlacementSession
    {
        // 🚀 高性能队列，替代原有的 List
        public Queue<HousingItem> PendingItems { get; set; } = new Queue<HousingItem>();
        
        // 记录总数，用于计算进度条
        public int TotalCount { get; set; }
        
        // 标记当前是否正在施工中
        public bool IsActive { get; set; }

        /// <summary>
        /// 清空所有状态
        /// </summary>
        public void Clear()
        {
            PendingItems.Clear();
            TotalCount = 0;
            IsActive = false;
        }
    }
}