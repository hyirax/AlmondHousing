using System.Numerics;

namespace AlmondHousing.Util
{
    /// <summary>
    /// 全局静态常量库 (消灭代码里的魔法数字)
    /// </summary>
    public static class Constants
    {
        public static class Housing
        {
            // 🏠 地板高度判定阈值
            public const float BasementThreshold = -0.001f;
            public const float UpperFloorThreshold = 6.999f;
            public const float MaxExteriorHeight = 7.0f;

            // 🏡 房屋庭院尺寸边界 (X 和 Z)
            public const double LargePlotX = 20.5;
            public const double LargePlotZ = 24.5;
            public const double MediumPlotX = 16.5;
            public const double MediumPlotZ = 16.5;
            public const double SmallPlotX = 12.5;
            public const double SmallPlotZ = 12.5;

            // 📐 坐标与旋转误差容忍度 (用于判断家具是否已经摆放完美)
            public const double LocationTolerance = 0.0001;
            public const float RotationTolerance = 0.001f;
        }

        public static class Colors
        {
            // 🎨 UI 主题颜色字典
            public static readonly Vector4 ThemeBase = new Vector4(0.20f, 0.20f, 0.20f, 1f);
            public static readonly Vector4 ThemeHover = new Vector4(0.35f, 0.35f, 0.35f, 1f);
            public static readonly Vector4 ThemeActive = new Vector4(0.45f, 0.45f, 0.45f, 1f);
            public static readonly Vector4 ThemeHeader = new Vector4(0.15f, 0.15f, 0.15f, 0.8f);
            
            public static readonly Vector4 AccentGold = new Vector4(0.9f, 0.7f, 0.3f, 1.0f);
            public static readonly Vector4 WarningRed = new Vector4(1.0f, 0.3f, 0.3f, 1.0f);
            public static readonly Vector4 SuccessGreen = new Vector4(0.4f, 1.0f, 0.4f, 1.0f);
            public static readonly Vector4 TextDisabled = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
        }
    }
}