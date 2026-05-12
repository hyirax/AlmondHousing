using System;
using System.Collections.Generic;
using AlmondHousing.Objects;

namespace AlmondHousing.Util
{
    public static class DyeManager
    {
        // ==========================================
        // 🎨 智能分流映射表
        // ==========================================
        
        // 1. 特殊染剂映射 (商城/绿底稀有染剂，依然需要独立道具)
        private static readonly Dictionary<uint, uint> SpecialDyeMapping = new Dictionary<uint, uint>();

        // 2. 整合染剂 B (伊修加德) 的 StainId 集合 (9种)
        private static readonly HashSet<uint> GeneralDyeB_Stains = new HashSet<uint>();

        // 3. 整合染剂 C (宇宙探索) 的 StainId 集合 (11种)
        private static readonly HashSet<uint> GeneralDyeC_Stains = new HashSet<uint>();

        // ==========================================
        // 📦 7.5 版本通用染剂的真实 物品ID (ItemId)
        // ⚠️ 请根据最新的 Lumina / 灰机wiki 数据替换这三个数字！
        // ==========================================
        private const uint GENERAL_DYE_A_ITEM_ID = 40001; // 整合染剂A (2.X)
        private const uint GENERAL_DYE_B_ITEM_ID = 40002; // 整合染剂B (伊修加德)
        private const uint GENERAL_DYE_C_ITEM_ID = 40003; // 整合染剂C (宇宙探索)
        
        // 褪色剂/松节油
        private const uint TEREBINTH_ITEM_ID = 5733;

        static DyeManager()
        {
            InitializeMapping();
        }

        private static void InitializeMapping()
        {
            // 💡 第一类：独立/稀有染剂
            SpecialDyeMapping[101] = 10112; // 无瑕白
            SpecialDyeMapping[102] = 10113; // 煤烟黑
            SpecialDyeMapping[103] = 10114; // 闪耀银
            SpecialDyeMapping[104] = 10115; // 闪耀金
            SpecialDyeMapping[105] = 29648; // 柔彩粉
            SpecialDyeMapping[106] = 29649; // 香草白
            SpecialDyeMapping[107] = 29650; // 黏土白
            SpecialDyeMapping[108] = 29651; // 黑暗红
            SpecialDyeMapping[109] = 29652; // 黑暗棕
            SpecialDyeMapping[110] = 29653; // 黑暗绿
            SpecialDyeMapping[111] = 29654; // 黑暗蓝
            SpecialDyeMapping[112] = 29655; // 黑暗紫
            SpecialDyeMapping[113] = 29656; // 枪铁黑
            SpecialDyeMapping[114] = 29657; // 珍珠白
            SpecialDyeMapping[115] = 29658; // 黄铜金
            SpecialDyeMapping[116] = 36341; // 樱桃粉
            SpecialDyeMapping[117] = 36342; // 香草黄
            SpecialDyeMapping[118] = 36343; // 薄荷绿
            SpecialDyeMapping[119] = 36344; // 柔彩蓝
            SpecialDyeMapping[120] = 36345; // 柔彩紫

            // 💡 第二类：整合染剂 B (伊修加德，9 种)
            // (请查阅数据表，把这 9 个颜色的 StainId 填进来，以下为示例)
            // GeneralDyeB_Stains.Add(80); 
            // GeneralDyeB_Stains.Add(81);

            // 💡 第三类：整合染剂 C (宇宙探索，11 种)
            // (把宇宙探索新增颜色的 StainId 填进来)
            // GeneralDyeC_Stains.Add(90); 
        }

        /// <summary>
        /// 核心“大脑”：根据颜色决定扣除什么道具
        /// </summary>
        public static uint GetRequiredDyeItemId(uint stainId)
        {
            if (stainId == 0) return TEREBINTH_ITEM_ID; 

            // 1. 是不是稀有色？
            if (SpecialDyeMapping.TryGetValue(stainId, out uint itemId)) return itemId;

            // 2. 是不是伊修加德系列？
            if (GeneralDyeB_Stains.Contains(stainId)) return GENERAL_DYE_B_ITEM_ID;

            // 3. 是不是宇宙探索系列？
            if (GeneralDyeC_Stains.Contains(stainId)) return GENERAL_DYE_C_ITEM_ID;

            // 4. 剩下的全部默认兜底为 2.X 基础色
            return GENERAL_DYE_A_ITEM_ID; 
        }

        public static bool IsDyeStockSufficient(uint stainId, int requiredCount)
        {
            if (requiredCount <= 0) return true;
            uint itemId = GetRequiredDyeItemId(stainId);
            
            // 7.5 版所有颜色都对应实体染剂，如果拿不到ID直接报错防呆
            if (itemId == 0) return false; 
            
            return InventoryScanner.GetOwnedCount(itemId) >= requiredCount;
        }

        public static int GetOwnedDyeCount(uint stainId)
        {
            uint itemId = GetRequiredDyeItemId(stainId);
            if (itemId == 0) return 0;
            return InventoryScanner.GetOwnedCount(itemId);
        }

        public static unsafe bool DyePlacedFurniture(HousingItem housingItem, uint targetStainId)
        {
            if (housingItem.ItemStruct == IntPtr.Zero) return false;

            byte bStainId = (byte)targetStainId;
            var itemStruct = (HousingGameObject*)housingItem.ItemStruct;
            
            if (itemStruct->color == bStainId) return true;

            if (!IsDyeStockSufficient(targetStainId, 1))
            {
                AlmondHousing.LogError($"[DyeManager] 染剂不足！无法将 {housingItem.Name} 染成目标颜色。");
                return false;
            }

            try
            {
                Memory.Instance.ColorFurniture(housingItem.ItemStruct, bStainId);
                itemStruct->color = bStainId;
                housingItem.Stain = bStainId; 
                return true;
            }
            catch (Exception ex)
            {
                AlmondHousing.LogError($"[DyeManager] 染色发生异常: {ex.Message}");
                return false;
            }
        }
    }
}