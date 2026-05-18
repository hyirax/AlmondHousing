using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using Dalamud.Interface.Utility;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;

namespace AlmondHousing
{
    public static class AdvancedTuningUI
    {
        // 🚀 坐标轴专属红绿蓝
        private static readonly Vector4 RED = new Vector4(1.0f, 0.3f, 0.3f, 1.0f);
        private static readonly Vector4 GREEN = new Vector4(0.3f, 1.0f, 0.3f, 1.0f);
        private static readonly Vector4 BLUE = new Vector4(0.3f, 0.6f, 1.0f, 1.0f);
        
        // 🚀 完美同步主面板的主题色
        private static readonly Vector4 ACCENT_COLOR = new Vector4(0.9f, 0.7f, 0.3f, 1.0f);

        public static unsafe void Draw()
        {
            var plugin = AlmondHousing.Instance;
            if (plugin == null || !plugin.Config.IsActivated) return;

            // 🛡️ 绝对防冲突领域：侦测到外部 BDTH 驱动时，本 UI 直接销毁让权！
            if (!AlmondHousing.UseEmbeddedBDTH) return;

            var mem = Memory.Instance;
            if (mem == null || !mem.CanEditItem()) return;

            var activeItem = mem.HousingStructure->ActiveItem;
            if (activeItem == null) return;

            // ==========================================
            // 🎯 3D 屏幕拖拽轴 (Gizmo)
            // ==========================================
            if (plugin.Config.UseGizmo)
            {
                var sceneCam = Memory.Camera;
                if (sceneCam != null && sceneCam->RenderCamera != null)
                {
                    var renderCam = sceneCam->RenderCamera;
                    var view = sceneCam->ViewMatrix;
                    var proj = renderCam->ProjectionMatrix;

                    var far = renderCam->FarPlane;
                    var near = renderCam->NearPlane;
                    var clip = far / (far - near);
                    proj.M43 = -(clip * near);
                    proj.M33 = -((far + near) / (far - near));
                    view.M44 = 1.0f;

                    ImGuiHelpers.ForceNextWindowMainViewport();
                    ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
                    
                    const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoNavInputs | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs;
                    
                    if (ImGui.Begin("AlmondGizmo", windowFlags))
                    {
                        var io = ImGui.GetIO();
                        ImGui.SetWindowSize(io.DisplaySize);

                        ImGuizmo.BeginFrame();
                        ImGuizmo.SetDrawlist();
                        ImGuizmo.Enable(mem.HousingStructure->Rotating);
                        ImGuizmo.SetID(1337);
                        ImGuizmo.SetOrthographic(false);
                        
                        ImGuizmo.SetRect(0, 0, io.DisplaySize.X, io.DisplaySize.Y);

                        Vector3 translate = activeItem->Position;
                        Vector3 rot = Vector3.Zero;
                        Vector3 scale = Vector3.One;
                        Matrix4x4 matrix = Matrix4x4.Identity;
                        
                        ImGuizmo.RecomposeMatrixFromComponents(ref translate.X, ref rot.X, ref scale.X, ref matrix.M11);

                        float snapVal = plugin.Config.DoSnap ? plugin.Config.Drag : 0f;
                        Vector3 snap = new Vector3(snapVal, snapVal, snapVal);
                        Matrix4x4 deltaMatrix = Matrix4x4.Identity;

                        if (ImGuizmo.Manipulate(ref view.M11, ref proj.M11, ImGuizmoOperation.Translate, ImGuizmoMode.World, ref matrix.M11, ref deltaMatrix.M11, ref snap.X))
                        {
                            ImGuizmo.DecomposeMatrixToComponents(ref matrix.M11, ref translate.X, ref rot.X, ref scale.X);
                            activeItem->Position = translate;
                            if (mem.HousingLayoutModelUpdate != null) mem.HousingLayoutModelUpdate((nint)activeItem + 0x80);
                        }
                    }
                    ImGui.End();
                    ImGui.PopStyleVar();
                }
            }

            // ==========================================
            // 🎯 多语言适配版：量子悬浮微调面板
            // ==========================================
            ImGui.SetNextWindowBgAlpha(0.95f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8.0f);
            
            // 🚀 颜色大一统：完美继承你主控制台的碳灰高级感
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.05f, 0.05f, 0.05f, 0.95f)); 
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.15f, 0.15f, 0.15f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.22f, 0.22f, 0.22f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.28f, 0.28f, 0.28f, 1.0f));
            
            if (ImGui.Begin("AlmondTuningPanel", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoDocking))
            {
                // 图标前缀
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ACCENT_COLOR, FontAwesomeIcon.Crosshairs.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine();

                // 🌐 多语言获取标题
                string title = AlmondHousing.Lang.GetText("QuantumTuningTitle");
                if (string.IsNullOrEmpty(title) || title == "QuantumTuningTitle") title = "量子微调系统"; 
                ImGui.TextColored(ACCENT_COLOR, title);

                // 🌐 悬浮帮助问号 (彻底干掉底部长文字，完美适配任意语言长度)
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextDisabled(FontAwesomeIcon.QuestionCircle.ToIconString());
                ImGui.PopFont();
                if (ImGui.IsItemHovered())
                {
                    string helpText = AlmondHousing.Lang.GetText("QuantumTuningHelp");
                    if (string.IsNullOrEmpty(helpText) || helpText == "QuantumTuningHelp") 
                        helpText = "💡 按住左右拖拽可微调数值\n💡 【双击】数字区域即可精确输入";
                    ImGui.SetTooltip(helpText);
                }

                ImGui.Separator();
                ImGui.Dummy(new Vector2(0, 3));
                
                float drag = plugin.Config.Drag > 0 ? plugin.Config.Drag : 0.05f;
                Vector3 pos = activeItem->Position;
                bool changed = false;

                void DrawCoord(string label, Vector4 col, ref float val)
                {
                    ImGui.TextColored(col, label);
                    ImGui.SameLine(30f); 
                    
                    ImGui.PushItemWidth(160f); 
                    if (ImGui.DragFloat($"##drag_{label}", ref val, drag, 0, 0, "%.3f")) changed = true;
                    ImGui.PopItemWidth();
                }

                DrawCoord("X", RED, ref pos.X);
                ImGui.Dummy(new Vector2(0, 2)); 
                DrawCoord("Y", GREEN, ref pos.Y);
                ImGui.Dummy(new Vector2(0, 2));
                DrawCoord("Z", BLUE, ref pos.Z);

                if (changed)
                {
                    activeItem->Position = pos;
                    if (mem.HousingLayoutModelUpdate != null)
                        mem.HousingLayoutModelUpdate((nint)activeItem + 0x80);
                }
                ImGui.Dummy(new Vector2(0, 2));
            }
            ImGui.End();
            
            // 弹出全部颜色和样式注入
            ImGui.PopStyleColor(4); 
            ImGui.PopStyleVar(2);
        }
    }
}