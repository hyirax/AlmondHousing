using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using AlmondHousing.Util;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace AlmondHousing.Gui
{
    public static class CustomUI
    {
        private static Dictionary<string, float> _animStates = new Dictionary<string, float>();
        private static float _timeCounter = 0f;

        private static Vector4 Lerp(Vector4 a, Vector4 b, float t)
        {
            return new Vector4(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t,
                a.W + (b.W - a.W) * t
            );
        }

        public static bool AnimatedButton(string id, string text, Vector2 size, FontAwesomeIcon icon = FontAwesomeIcon.None)
        {
            if (!_animStates.ContainsKey(id)) _animStates[id] = 0f;

            var pos = ImGui.GetCursorScreenPos();
            float deltaTime = ImGui.GetIO().DeltaTime;
            _timeCounter += deltaTime;

            bool isHovered = ImGui.IsMouseHoveringRect(pos, pos + size);
            bool isActive = isHovered && ImGui.IsMouseDown(ImGuiMouseButton.Left);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0,0,0,0));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0,0,0,0));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0,0,0,0));
            bool clicked = ImGui.Button($"###{id}", size);
            ImGui.PopStyleColor(3);

            float targetState = isHovered ? 1f : 0f;
            _animStates[id] += (targetState - _animStates[id]) * (deltaTime * 12.0f);
            float animProgress = _animStates[id]; 

            Vector4 colorNormal = Constants.Colors.ThemeBase;
            Vector4 colorHover = Constants.Colors.AccentGold; 
            Vector4 colorClick = new Vector4(1.0f, 0.85f, 0.4f, 1.0f); 
            Vector4 textColorNormal = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);
            Vector4 textColorHover = new Vector4(0.1f, 0.1f, 0.1f, 1.0f); 

            Vector4 currentColor = Lerp(colorNormal, colorHover, animProgress);
            Vector4 currentTextColor = Lerp(textColorNormal, textColorHover, animProgress);

            Vector2 renderPos = pos;
            if (isActive) 
            {
                currentColor = colorClick;
                renderPos.Y += 2; 
            }

            var drawList = ImGui.GetWindowDrawList();

            if (animProgress > 0.05f)
            {
                float breathingPulse = (float)(Math.Sin(_timeCounter * 4.0f) + 1.0f) / 2.0f; 
                float glowSpread = 2.0f + (breathingPulse * 3.0f * animProgress); 
                Vector4 glowColor = new Vector4(colorHover.X, colorHover.Y, colorHover.Z, animProgress * 0.4f); 
                drawList.AddRectFilled(renderPos - new Vector2(glowSpread), renderPos + size + new Vector2(glowSpread), ImGui.ColorConvertFloat4ToU32(glowColor), 6.0f);
            }

            drawList.AddRectFilled(renderPos, renderPos + size, ImGui.ColorConvertFloat4ToU32(currentColor), 6.0f);

            // ==========================================
            // 🚀【修复文字居中】将图标和文字的垂直坐标剥离，各自绝对居中！
            // ==========================================
            Vector2 iconSize = Vector2.Zero;
            string iconString = "";
            
            if (icon != FontAwesomeIcon.None)
            {
                iconString = icon.ToIconString();
                ImGui.PushFont(UiBuilder.IconFont);
                iconSize = ImGui.CalcTextSize(iconString);
                ImGui.PopFont();
            }

            Vector2 textSize = ImGui.CalcTextSize(text);
            float spacing = icon != FontAwesomeIcon.None ? 8.0f : 0.0f;
            float totalWidth = iconSize.X + spacing + textSize.X;
            
            // X 轴起始位置计算
            float startX = renderPos.X + (size.X - totalWidth) / 2;
            
            // Y 轴各自独立计算，完美居中！
            float iconY = renderPos.Y + (size.Y - iconSize.Y) / 2;
            float textY = renderPos.Y + (size.Y - textSize.Y) / 2;

            if (icon != FontAwesomeIcon.None)
            {
                ImGui.PushFont(UiBuilder.IconFont);
                drawList.AddText(new Vector2(startX, iconY), ImGui.ColorConvertFloat4ToU32(currentTextColor), iconString);
                ImGui.PopFont();
                startX += iconSize.X + spacing; 
            }

            drawList.AddText(new Vector2(startX, textY), ImGui.ColorConvertFloat4ToU32(currentTextColor), text);

            return clicked;
        }
    }
}