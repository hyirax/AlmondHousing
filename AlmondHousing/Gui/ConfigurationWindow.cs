using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Utility;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using AlmondHousing.Objects;
using AlmondHousing.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using static AlmondHousing.AlmondHousing;
using Dalamud.Interface.Textures;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;

namespace AlmondHousing.Gui
{
    // 🚀 partial 关键字让它与 Materials.cs 和 EasterEgg.cs 完美合体
    public partial class ConfigurationWindow : Window
    {
        private AlmondHousing Plugin;
        public Configuration Config => Plugin.Config;

        public bool CanUpload { get; set; }
        public bool CanImport { get; set; }

        private bool showOnlyMisplaced = false;
        private bool showOnlyMissing = false;

        private string CustomTag = string.Empty;
        private readonly Dictionary<uint, uint> iconToFurniture = new() { };

        private readonly Vector4 THEME_BASE = new(0.20f, 0.20f, 0.20f, 1f);     
        private readonly Vector4 THEME_HOVER = new(0.35f, 0.35f, 0.35f, 1f);    
        private readonly Vector4 THEME_ACTIVE = new(0.45f, 0.45f, 0.45f, 1f);   
        private readonly Vector4 THEME_HEADER = new(0.15f, 0.15f, 0.15f, 0.8f);
        private readonly Vector4 ACCENT_COLOR = new(0.9f, 0.7f, 0.3f, 1.0f); 

        private FileDialogManager FileDialogManager { get; }
        private Dalamud.Game.ClientLanguage currentExportLang = Dalamud.Game.ClientLanguage.English;
        private string searchQuery = string.Empty;

        private int selectedTab = 0;
        
        private Dictionary<int, float> _sidebarAnimStates = new Dictionary<int, float>();

        public ConfigurationWindow(AlmondHousing plugin) : base("AlmondHousing", ImGuiWindowFlags.NoScrollWithMouse)
        {
            Plugin = plugin;
            this.SizeCondition = ImGuiCond.FirstUseEver;
            this.Size = new Vector2(800, 550);
            this.FileDialogManager = new FileDialogManager
            {
                AddedWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking,
            };
        }

        public override void PreDraw()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 12.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 8.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 8.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10)); 
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 6));    

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.08f, 0.08f, 0.09f, 0.98f)); 
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.05f, 0.05f, 0.06f, 1.0f));   
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.12f, 0.12f, 0.14f, 1.0f)); 
            
            ImGui.PushStyleColor(ImGuiCol.Button, THEME_BASE);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, THEME_HOVER);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, THEME_ACTIVE);
            
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, new Vector4(0.05f, 0.05f, 0.05f, 0.5f));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, THEME_HOVER);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, ACCENT_COLOR);

            ImGui.PushStyleColor(ImGuiCol.CheckMark, ACCENT_COLOR); 
        }

        public override void PostDraw()
        {
            ImGui.PopStyleVar(6);
            ImGui.PopStyleColor(10);
        }

        public override void Draw()
        {
            // 🚀 暗中监听魂斗罗秘籍 (实现在 ConfigurationWindow.EasterEgg.cs)
            ListenForCheatCode();

            string version = typeof(AlmondHousing).Assembly.GetName().Version?.ToString();
            if (string.IsNullOrEmpty(version)) version = "7.5.1.0";
            if (version.EndsWith(".0")) version = version.Substring(0, version.Length - 2); 
            
            string versionText = $" v{version}";
            string authorText = " By AlmondCookie";

            float maxTextWidth = ImGui.CalcTextSize(Lang.GetText("Home")).X;
            maxTextWidth = Math.Max(maxTextWidth, ImGui.CalcTextSize(Lang.GetText("Interior Furniture")).X);
            maxTextWidth = Math.Max(maxTextWidth, ImGui.CalcTextSize(Lang.GetText("Fixtures")).X);
            maxTextWidth = Math.Max(maxTextWidth, ImGui.CalcTextSize(Lang.GetText("Layout & Export")).X);
            maxTextWidth = Math.Max(maxTextWidth, ImGui.CalcTextSize(Lang.GetText("Settings")).X);
            maxTextWidth = Math.Max(maxTextWidth, ImGui.CalcTextSize(Lang.GetText("Material Audit")).X);
            maxTextWidth = Math.Max(maxTextWidth, ImGui.CalcTextSize(authorText).X);

            float sidebarWidth = maxTextWidth + 65f; 
            if (sidebarWidth < 200f) sidebarWidth = 200f; 

            ImGui.BeginChild("AlmondSidebar", new Vector2(sidebarWidth, 0), true);
            {
                DrawSidebarItem(FontAwesomeIcon.Home, Lang.GetText("Home"), 0);
                DrawSidebarItem(FontAwesomeIcon.Chair, Lang.GetText("Interior Furniture"), 1); 
                DrawSidebarItem(FontAwesomeIcon.PaintRoller, Lang.GetText("Fixtures"), 2);               
                DrawSidebarItem(FontAwesomeIcon.ClipboardList, Lang.GetText("Material Audit"), 5);
                DrawSidebarItem(FontAwesomeIcon.FileExport, Lang.GetText("Layout & Export"), 3);        
                DrawSidebarItem(FontAwesomeIcon.Cog, Lang.GetText("Settings"), 4);               

                ImGui.SetCursorPosY(Math.Max(ImGui.GetCursorPosY() + 20, ImGui.GetWindowHeight() - 60));
                ImGui.Separator();
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1f));
                ImGui.TextUnformatted(versionText);
                ImGui.TextUnformatted(authorText);
                ImGui.PopStyleColor();
            }
            ImGui.EndChild();

            ImGui.SameLine();

            ImGui.BeginChild("AlmondContent", new Vector2(0, 0), false);
            {
                switch (selectedTab)
                {
                    case 0: DrawHomeTab(); break;
                    case 1: DrawFurnitureTab(); break;
                    case 2: DrawFixtureTab(); break;
                    case 3: DrawLayoutFileTab(); break;
                    case 4: DrawSettingsTab(); break;
                    case 5: DrawMaterialTab(); break; // 实现在 ConfigurationWindow.Materials.cs
                }
            }
            ImGui.EndChild();

            this.FileDialogManager.Draw();
        }

        private void DrawSidebarItem(FontAwesomeIcon icon, string label, int index)
        {
            if (!_sidebarAnimStates.ContainsKey(index)) _sidebarAnimStates[index] = 0f;

            float height = 38f;
            Vector2 startPos = ImGui.GetCursorPos(); 
            Vector2 screenPos = ImGui.GetCursorScreenPos(); 
            var drawList = ImGui.GetWindowDrawList();

            bool isSelected = selectedTab == index;
            
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0,0,0,0));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0,0,0,0));
            if (ImGui.Selectable($"##{index}_{label}", isSelected, ImGuiSelectableFlags.None, new Vector2(0, height)))
            {
                selectedTab = index;
            }
            ImGui.PopStyleColor(2);

            bool isHovered = ImGui.IsItemHovered();

            float targetState = isSelected ? 1f : (isHovered ? 0.3f : 0f);
            float deltaTime = ImGui.GetIO().DeltaTime;
            _sidebarAnimStates[index] += (targetState - _sidebarAnimStates[index]) * (deltaTime * 15.0f);
            float progress = _sidebarAnimStates[index];

            Vector4 baseColor = new Vector4(0.55f, 0.55f, 0.55f, 1f); 
            Vector4 activeColor = ACCENT_COLOR;                       
            
            Vector4 currentColor = new Vector4(
                baseColor.X + (activeColor.X - baseColor.X) * progress,
                baseColor.Y + (activeColor.Y - baseColor.Y) * progress,
                baseColor.Z + (activeColor.Z - baseColor.Z) * progress,
                1f
            );

            float currentWidth = ImGui.GetWindowWidth();

            if (progress > 0.01f)
            {
                Vector4 bgColor = new Vector4(ACCENT_COLOR.X, ACCENT_COLOR.Y, ACCENT_COLOR.Z, progress * 0.15f);
                drawList.AddRectFilled(screenPos, screenPos + new Vector2(currentWidth, height), ImGui.ColorConvertFloat4ToU32(bgColor), 6.0f);
            }

            if (progress > 0.01f)
            {
                float lineH = height * progress * 0.6f; 
                float lineY = screenPos.Y + (height - lineH) / 2;
                drawList.AddLine(new Vector2(screenPos.X + 2, lineY), new Vector2(screenPos.X + 2, lineY + lineH), ImGui.ColorConvertFloat4ToU32(ACCENT_COLOR), 4.0f);
            }

            float xOffset = 12f + (progress * 6f);
            
            string iconStr = icon.ToIconString();
            ImGui.PushFont(UiBuilder.IconFont);
            Vector2 iconSize = ImGui.CalcTextSize(iconStr);
            drawList.AddText(new Vector2(screenPos.X + xOffset, screenPos.Y + (height - iconSize.Y) / 2), ImGui.ColorConvertFloat4ToU32(currentColor), iconStr);
            ImGui.PopFont();

            Vector2 textSize = ImGui.CalcTextSize(label);
            drawList.AddText(new Vector2(screenPos.X + xOffset + iconSize.X + 8f, screenPos.Y + (height - textSize.Y) / 2), ImGui.ColorConvertFloat4ToU32(currentColor), label);

            ImGui.SetCursorPos(new Vector2(startPos.X, startPos.Y + height + 2));
        }

        private void DrawInlineIcon(FontAwesomeIcon icon)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text(icon.ToIconString());
            ImGui.PopFont();
        }

        private void DrawInlineIconColored(FontAwesomeIcon icon, Vector4 color)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(color, icon.ToIconString());
            ImGui.PopFont();
        }

        private void DrawHomeTab()
        {
            DrawInlineIconColored(FontAwesomeIcon.Leaf, ACCENT_COLOR); ImGui.SameLine();
            ImGui.TextColored(ACCENT_COLOR, Lang.GetText("Welcome to AlmondHousing!"));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 10));

            ImGui.BeginChild("HomeInfo", new Vector2(0, 0), false);
            {
                DrawInlineIconColored(FontAwesomeIcon.InfoCircle, ACCENT_COLOR); ImGui.SameLine();
                ImGui.TextColored(ACCENT_COLOR, Lang.GetText("About this Plugin"));
                ImGui.TextWrapped(Lang.GetText("HomeDesc1"));
                ImGui.Dummy(new Vector2(0, 10));

                DrawInlineIconColored(FontAwesomeIcon.Star, ACCENT_COLOR); ImGui.SameLine();
                ImGui.TextColored(ACCENT_COLOR, Lang.GetText("Core Features"));
                ImGui.BulletText(Lang.GetText("Feat1"));
                ImGui.BulletText(Lang.GetText("Feat2"));
                ImGui.BulletText(Lang.GetText("Feat3"));
                ImGui.BulletText(Lang.GetText("Feat4"));
                ImGui.Dummy(new Vector2(0, 10));
                
                DrawInlineIconColored(FontAwesomeIcon.Heart, ACCENT_COLOR); ImGui.SameLine();
                ImGui.TextColored(ACCENT_COLOR, Lang.GetText("Credits & Acknowledgements"));
                ImGui.TextWrapped(Lang.GetText("CreditDesc"));
                ImGui.Dummy(new Vector2(0, 5));
                ImGui.Indent(10f);
                ImGui.TextWrapped(Lang.GetText("Credit1"));
                ImGui.TextWrapped(Lang.GetText("Credit2"));
                ImGui.TextWrapped(Lang.GetText("Credit3"));
                ImGui.TextWrapped(Lang.GetText("Credit4"));
                ImGui.Unindent(10f);
                ImGui.Dummy(new Vector2(0, 15));

                DrawInlineIconColored(FontAwesomeIcon.Ban, new Vector4(1.0f, 0.4f, 0.4f, 1.0f)); ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.4f, 0.4f, 1.0f)); 
                ImGui.TextUnformatted(Lang.GetText("Strict Anti-Resell Warning"));
                ImGui.Separator();
                ImGui.TextWrapped(Lang.GetText("Warn1"));
                ImGui.TextWrapped(Lang.GetText("Warn2"));
                ImGui.TextWrapped(Lang.GetText("Warn3"));
                ImGui.PopStyleColor();
            }
            ImGui.EndChild();
        }

        private void DrawFurnitureTab()
        {
            DrawInlineIconColored(FontAwesomeIcon.Search, ACCENT_COLOR); ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##SearchBox", Lang.GetText("Search furniture name..."), ref searchQuery, 256);
            ImGui.Dummy(new Vector2(0, 5));

            DrawInlineIconColored(FontAwesomeIcon.Filter, ACCENT_COLOR); ImGui.SameLine();
            ImGui.TextColored(ACCENT_COLOR, Lang.GetText("Smart Filter:"));
            ImGui.SameLine();

            DrawInlineIconColored(FontAwesomeIcon.ExclamationTriangle, ACCENT_COLOR); ImGui.SameLine();
            ImGui.Checkbox(Lang.GetText("Only show misplaced furniture"), ref showOnlyMisplaced);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.GetText("Check to show only items that are not in the correct position or rotation."));

            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();

            DrawInlineIconColored(FontAwesomeIcon.ShoppingCart, ACCENT_COLOR); ImGui.SameLine();
            ImGui.Checkbox(Lang.GetText("Only show missing furniture"), ref showOnlyMissing);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.GetText("Check to show only items that are missing from your inventory."));

            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 5));

            ImGui.PushStyleColor(ImGuiCol.Header, THEME_HEADER);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, THEME_HOVER);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, THEME_ACTIVE);

            if (ImGui.CollapsingHeader(Lang.GetText("Interior Furniture"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PushID("interior");
                DrawItemList(Plugin.InteriorItemList);
                ImGui.PopID();
            }
            if (ImGui.CollapsingHeader(Lang.GetText("Exterior Furniture"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PushID("exterior");
                DrawItemList(Plugin.ExteriorItemList);
                ImGui.PopID();
            }
            if (ImGui.CollapsingHeader(Lang.GetText("Unused Furniture"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PushID("unused");
                DrawItemList(Plugin.UnusedItemList, true);
                ImGui.PopID();
            }

            ImGui.PopStyleColor(3);
        }

        private void DrawFixtureTab()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, THEME_HEADER);
            if (ImGui.CollapsingHeader(Lang.GetText("Interior Fixtures"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawFixtureList(Plugin.Layout.interiorFixture);
            }
            if (ImGui.CollapsingHeader(Lang.GetText("Exterior Fixtures"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawFixtureList(Plugin.Layout.exteriorFixture);
            }
            ImGui.PopStyleColor();
        }

        private void DrawLayoutFileTab()
        {
            DrawInlineIconColored(FontAwesomeIcon.FolderOpen, ACCENT_COLOR); ImGui.SameLine();
            ImGui.TextColored(ACCENT_COLOR, Lang.GetText("Layout & Export"));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 10));

            if (AlmondHousing.Session.IsActive && AlmondHousing.Session.TotalCount > 0)
            {
                float progress = 1.0f - ((float)AlmondHousing.Session.PendingItems.Count / AlmondHousing.Session.TotalCount);
                
                DrawInlineIconColored(FontAwesomeIcon.Tools, new Vector4(0.2f, 0.8f, 1.0f, 1.0f)); ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.2f, 0.8f, 1.0f, 1.0f), Lang.GetText("Construction in progress..."));
                
                ImGui.ProgressBar(progress, new Vector2(-1, 24), string.Format(Lang.GetText("{0} / {1} Completed"), AlmondHousing.Session.TotalCount - AlmondHousing.Session.PendingItems.Count, AlmondHousing.Session.TotalCount));
                ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
            }

            DrawInlineIconColored(FontAwesomeIcon.Save, ACCENT_COLOR); ImGui.SameLine();
            ImGui.TextColored(ACCENT_COLOR, Lang.GetText("Save Layout"));

            // ==========================================
            // 🚀【权限限制区】检测是否处于布置模式（证明是否拥有该房屋权限）
            // ==========================================
            bool hasPermission = Memory.Instance != null && Memory.Instance.IsHousingMode();

            if (hasPermission)
            {
                if (!Config.SaveLocation.IsNullOrEmpty())
                {
                    ImGui.TextWrapped($"{Lang.GetText("Current Path:")} {Config.SaveLocation}");
                    
                    if (CustomUI.AnimatedButton("btn_save", Lang.GetText("Save"), new Vector2(180, 36), FontAwesomeIcon.Save))
                        SaveLayoutToFile();
                    ImGui.SameLine();
                }
                
                if (CustomUI.AnimatedButton("btn_save_as", Lang.GetText("Save As"), new Vector2(180, 36), FontAwesomeIcon.FolderOpen))
                    ShowSaveDialog();
            }
            else
            {
                // 🔒 如果没权限，就画一把灰色的锁，并调用 7 国语言翻译！
                ImGui.Dummy(new Vector2(0, 5));
                DrawInlineIconColored(FontAwesomeIcon.Lock, new Vector4(0.6f, 0.6f, 0.6f, 1.0f)); ImGui.SameLine();
                ImGui.TextDisabled(Lang.GetText("LockMsg"));
            }

            ImGui.Dummy(new Vector2(0, 10));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 10));

            DrawInlineIconColored(FontAwesomeIcon.FileImport, ACCENT_COLOR); ImGui.SameLine();
            ImGui.TextColored(ACCENT_COLOR, Lang.GetText("Apply & Load Layout"));

            float buttonWidth = 200f; 

            void DrawHelpText(string text)
            {
                ImGui.SameLine(0, 15);
                DrawInlineIconColored(FontAwesomeIcon.InfoCircle, ACCENT_COLOR);
                ImGui.SameLine();
                ImGui.TextDisabled(text);
            }

            if (!Config.SaveLocation.IsNullOrEmpty())
            {
                if (CustomUI.AnimatedButton("btn_apply_curr", Lang.GetText("Apply Current"), new Vector2(buttonWidth, 36), FontAwesomeIcon.PlayCircle)) { CreateAutoBackup(); LoadLayoutFromFile(true); }
                DrawHelpText(Lang.GetText("Read from current path and place immediately"));
            }

            if (CustomUI.AnimatedButton("btn_apply_file", Lang.GetText("Apply from File"), new Vector2(buttonWidth, 36), FontAwesomeIcon.FileImport)) { ShowLoadDialog(true); }
            DrawHelpText(Lang.GetText("Select a file and start placing immediately"));

            if (!Config.SaveLocation.IsNullOrEmpty())
            {
                if (CustomUI.AnimatedButton("btn_load_curr", Lang.GetText("Load Current"), new Vector2(buttonWidth, 36), FontAwesomeIcon.Sync)) { LoadLayoutFromFile(false); }
                DrawHelpText(Lang.GetText("Update list only, do not move furniture"));
            }

            if (CustomUI.AnimatedButton("btn_load_file", Lang.GetText("Load from File"), new Vector2(buttonWidth, 36), FontAwesomeIcon.Folder)) { ShowLoadDialog(false); }
            DrawHelpText(Lang.GetText("Select file and update list, do not move"));

            ImGui.Dummy(new Vector2(0, 20));
            
            DrawInlineIconColored(FontAwesomeIcon.ShoppingCart, ACCENT_COLOR); ImGui.SameLine();
            ImGui.TextColored(ACCENT_COLOR, Lang.GetText("Export Shopping List"));
            ImGui.Separator();

            ImGui.SetNextItemWidth(180); 
            if (ImGui.BeginCombo("##ExportLang", currentExportLang.ToString()))
            {
                foreach (var lang in new[] { Dalamud.Game.ClientLanguage.English, Dalamud.Game.ClientLanguage.Japanese, Dalamud.Game.ClientLanguage.German, Dalamud.Game.ClientLanguage.French })
                {
                    if (ImGui.Selectable(lang.ToString(), currentExportLang == lang)) currentExportLang = lang;
                }
                ImGui.EndCombo();
            }
            ImGui.SameLine();
            if (CustomUI.AnimatedButton("btn_exp_tc", Lang.GetText("Export for Teamcraft"), new Vector2(220, 32), FontAwesomeIcon.Clipboard)) ExportToTeamcraft(currentExportLang);
            ImGui.SameLine();
            if (CustomUI.AnimatedButton("btn_exp_csv", Lang.GetText("Export to CSV"), new Vector2(180, 32), FontAwesomeIcon.FileCsv)) ExportToCSV(currentExportLang);

            // ==========================================
            // 🚨【彩蛋破锁区】全自动 7 国语言支持！
            // ==========================================
            if (_isDeveloperModeUnlocked)
            {
                ImGui.Dummy(new Vector2(0, 20));
                ImGui.Separator();
                ImGui.Dummy(new Vector2(0, 10));

                float pulseAlpha = (float)(Math.Sin(ImGui.GetTime() * 5.0) + 1.0) / 2.0f;
                Vector4 warningColor = new Vector4(1.0f, 0.2f, 0.2f, 0.5f + (pulseAlpha * 0.5f));
                
                DrawInlineIconColored(FontAwesomeIcon.UserSecret, warningColor); ImGui.SameLine();
                ImGui.TextColored(warningColor, Lang.GetText("OverrideActivated"));

                ImGui.Dummy(new Vector2(0, 5));
                ImGui.TextDisabled(Lang.GetText("DevPrivilegesMsg"));

                ImGui.Dummy(new Vector2(0, 5));
                
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.1f, 0.4f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.5f, 0.2f, 0.7f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.7f, 0.3f, 1.0f, 1f));

                if (ImGui.Button(Lang.GetText("ForceExtractSave") + " ##cheat_clone", new Vector2(-1, 40)))
                {
                    Plugin.GetGameLayout(); 
                    ShowSaveDialog();       
                    Log(Lang.GetText("DataExtractedMsg"));
                }
                
                ImGui.PopStyleColor(3);
            }
        }

        private void DrawSettingsTab()
        {
            DrawInlineIconColored(FontAwesomeIcon.ExclamationTriangle, new Vector4(1.0f, 0.3f, 0.3f, 1.0f)); ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.3f, 0.3f, 1.0f)); 
            ImGui.TextWrapped(Lang.GetText("Anti-Resell Warning"));
            ImGui.PopStyleColor(); 
            ImGui.Dummy(new Vector2(0, 10));

            DrawInlineIconColored(FontAwesomeIcon.Globe, ACCENT_COLOR); ImGui.SameLine();
            ImGui.Text(Lang.GetText("Plugin Interface Language"));
            
            string[] supportedLangNames = { "简体中文", "繁體中文", "English", "日本語", "한국어", "Deutsch", "Français" };
            string[] supportedLangs = { "zh", "zh_TW", "en", "ja", "ko", "de", "fr" };
            int currentLangIndex = Math.Max(0, Array.IndexOf(supportedLangs, Config.UILanguage));

            ImGui.SetNextItemWidth(180); 
            if (ImGui.BeginCombo("##UILang", supportedLangNames[currentLangIndex]))
            {
                for (int i = 0; i < supportedLangs.Length; i++)
                {
                    DrawInlineIconColored(FontAwesomeIcon.Globe, ACCENT_COLOR); ImGui.SameLine();
                    if (ImGui.Selectable(supportedLangNames[i], currentLangIndex == i))
                    {
                        Config.UILanguage = supportedLangs[i];
                        Config.Save();
                        Lang.SetLanguage(Config.UILanguage);
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 5));

            DrawInlineIconColored(FontAwesomeIcon.CheckSquare, ACCENT_COLOR); ImGui.SameLine();
            if (ImGui.Checkbox(Lang.GetText("Apply Layout"), ref Config.ApplyLayout)) Config.Save();
            
            DrawInlineIconColored(FontAwesomeIcon.Tag, ACCENT_COLOR); ImGui.SameLine();
            if (ImGui.Checkbox(Lang.GetText("Label Furniture"), ref Config.DrawScreen)) Config.Save();
            
            DrawInlineIconColored(FontAwesomeIcon.InfoCircle, ACCENT_COLOR); ImGui.SameLine();
            if (ImGui.Checkbox(Lang.GetText("Show Tooltips"), ref Config.ShowTooltips)) Config.Save();

            DrawInlineIconColored(FontAwesomeIcon.Clock, ACCENT_COLOR); ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt(Lang.GetText("Placement Interval (ms)"), ref Config.LoadInterval)) Config.Save();

            if (Memory.Instance != null && Memory.Instance.GetCurrentTerritory() == Memory.HousingArea.Indoors && Memory.Instance.GetIndoorHouseSize() != "Apartment")
            {
                ImGui.Dummy(new Vector2(0, 5));
                DrawInlineIconColored(FontAwesomeIcon.LayerGroup, ACCENT_COLOR); ImGui.SameLine();
                ImGui.Text(Lang.GetText("Selected Floors"));
                
                if (ImGui.Checkbox(Lang.GetText("Basement"), ref Config.Basement)) Config.Save();
                ImGui.SameLine();
                if (ImGui.Checkbox(Lang.GetText("Ground Floor"), ref Config.GroundFloor)) Config.Save();
                ImGui.SameLine();
                if (Memory.Instance.HasUpperFloor() && ImGui.Checkbox(Lang.GetText("Upper Floor"), ref Config.UpperFloor)) Config.Save();
            }
        }

        public static void DrawIcon(ushort icon, Vector2 size)
        {
            if (icon < 65000)
            {
                try
                {
                    var iconTexture = DalamudApi.TextureProvider.GetFromGameIcon(new GameIconLookup(icon));
                    if (iconTexture?.GetWrapOrEmpty() != null)
                    {
                        ImGui.Image(iconTexture.GetWrapOrEmpty().Handle, size);
                    }
                }
                catch (Exception)
                {
                    ImGui.Dummy(size);
                }
            }
        }

        private bool CheckModeForSave() => true;

        private bool CheckModeForLoad(bool shouldApply)
        {
            if (shouldApply && !Memory.Instance.CanEditItem())
            {
                LogError(Lang.GetText("Unable to set position outside of Rotate Layout mode"));
                return false;
            }
            if (!shouldApply && !Memory.Instance.IsHousingMode())
            {
                LogError(Lang.GetText("Unable to load layouts outside of Layout mode"));
                return false;
            }
            return true;
        }

        private void CreateAutoBackup()
        {
            try
            {
                Plugin.GetGameLayout();
                string backupDir = Path.Combine(AlmondHousing.PluginDirectory, "Backups");
                if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);
                string backupFileName = $"AutoBackup_{DateTime.Now:yyyyMMdd_HHmmss}.almond";
                string backupPath = Path.Combine(backupDir, backupFileName);
                string originalSaveLocation = Config.SaveLocation;
                Config.SaveLocation = backupPath;
                AlmondHousing.LayoutManager.ExportLayout();
                Config.SaveLocation = originalSaveLocation;
                Log(string.Format(Lang.GetText("Auto-backup created: {0}"), backupFileName));
            }
            catch (Exception e) { LogError(string.Format(Lang.GetText("Backup Error: {0}"), e.Message)); }
        }

        private void ShowSaveDialog()
        {
            string saveName = Config.SaveLocation.IsNullOrEmpty() ? "save" : Path.GetFileNameWithoutExtension(Config.SaveLocation);
            FileDialogManager.SaveFileDialog(Lang.GetText("Select a Save Location"), ".almond", saveName, "almond", (bool ok, string res) =>
            {
                if (!ok) return;
                Config.SaveLocation = res; Config.Save(); SaveLayoutToFile();
            }, Path.GetDirectoryName(Config.SaveLocation));
        }

        private void ShowLoadDialog(bool shouldApply)
        {
            FileDialogManager.OpenFileDialog(Lang.GetText("Select a Layout File"), ".almond,.json", (bool ok, List<string> res) =>
            {
                if (!ok) return;
                if (shouldApply) CreateAutoBackup(); 
                Config.SaveLocation = res.FirstOrDefault(""); 
                Config.Save(); 
                LoadLayoutFromFile(shouldApply);
            }, 1, Path.GetDirectoryName(Config.SaveLocation));
        }

        private void SaveLayoutToFile()
        {
            try { Plugin.GetGameLayout(); AlmondHousing.LayoutManager.ExportLayout(); }
            catch (Exception e) { LogError(string.Format(Lang.GetText("Save Error: {0}"), e.Message), e.StackTrace ?? ""); }
        }

        private void LoadLayoutFromFile(bool shouldApply)
        {
            if (!CheckModeForLoad(shouldApply)) return;
            try { 
                SaveLayoutManager.ImportLayout(Config.SaveLocation); 
                Log(string.Format(Lang.GetText("Imported {0} items"), Plugin.InteriorItemList.Count + Plugin.ExteriorItemList.Count));
                
                Plugin.MatchLayout(); 
                Config.ResetRecord(); 

                InvalidateMaterialCache();
                
                if (shouldApply) Plugin.ApplyLayout(); 
            }
            catch (Exception e) { LogError(string.Format(Lang.GetText("Load Error: {0}"), e.Message), e.StackTrace ?? ""); }
        }

        private void DrawFixtureList(List<Fixture> fixtureList)
        {
            if (ImGui.Button(Lang.GetText("Clear") + "##Fixture")) { fixtureList.Clear(); Config.Save(); }
            ImGui.Columns(3, "FixtureList", true);
            ImGui.Separator();
            ImGui.Text(Lang.GetText("Level")); ImGui.NextColumn();
            ImGui.Text(Lang.GetText("Fixture")); ImGui.NextColumn();
            ImGui.Text(Lang.GetText("Item")); ImGui.NextColumn();
            ImGui.Separator();
            var itemSheet = DalamudApi.DataManager.GetExcelSheet<Item>();
            foreach (var fixture in fixtureList)
            {
                ImGui.Text(Lang.GetText(fixture.level ?? "")); ImGui.NextColumn();
                ImGui.Text(Lang.GetText(fixture.type ?? "")); ImGui.NextColumn();
                if (itemSheet.HasRow(fixture.itemId)) { DrawIcon(itemSheet.GetRow(fixture.itemId).Icon, new Vector2(20, 20)); ImGui.SameLine(); }
                ImGui.Text(fixture.name); ImGui.NextColumn();
                ImGui.Separator();
            }
            ImGui.Columns(1);
        }

        private void DrawItemList(List<HousingItem> itemList, bool isUnused = false)
        {
            var filteredList = itemList.Where(x => string.IsNullOrEmpty(searchQuery) || x.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)).ToList();
            if (ImGui.Button(Lang.GetText("Sort")))
            {
                itemList.Sort((x, y) => {
                    if (x.Name.CompareTo(y.Name) != 0) return x.Name.CompareTo(y.Name);
                    return x.X.CompareTo(y.X);
                });
                Config.Save();
            }
            ImGui.SameLine();
            if (ImGui.Button(Lang.GetText("Clear") + "##ItemClear")) { itemList.Clear(); Config.Save(); }

            var itemSheet = DalamudApi.DataManager.GetExcelSheet<Item>();
            var groupedItems = filteredList.GroupBy(housingItem => {
                if (itemSheet.HasRow(housingItem.ItemKey)) {
                    string catName = itemSheet.GetRow(housingItem.ItemKey).ItemUICategory.Value.Name.ToString();
                    return string.IsNullOrEmpty(catName) ? Lang.GetText("Unknown") : catName;
                }
                return Lang.GetText("Unknown");
            }).OrderBy(g => g.Key).ToList();

            foreach (var group in groupedItems) {
                ImGui.PushStyleColor(ImGuiCol.Text, ACCENT_COLOR);
                if (ImGui.TreeNodeEx($"{group.Key} ({group.Count()})###cat_{group.Key}_{isUnused}", ImGuiTreeNodeFlags.DefaultOpen)) {
                    ImGui.PopStyleColor();
                    ImGui.Columns(isUnused ? 4 : 5, $"Table_{group.Key}", true);
                    ImGui.Separator();
                    ImGui.Text(Lang.GetText("Item")); ImGui.NextColumn();
                    ImGui.Text(Lang.GetText("Position (X,Y,Z)")); ImGui.NextColumn();
                    ImGui.Text(Lang.GetText("Rotation")); ImGui.NextColumn();
                    ImGui.Text(Lang.GetText("Dye/Material")); ImGui.NextColumn();
                    if (!isUnused) { ImGui.Text(Lang.GetText("Set Position")); ImGui.NextColumn(); }
                    ImGui.Separator();
                    foreach (var housingItem in group) {
                        
                        if (showOnlyMisplaced && housingItem.CorrectLocation && housingItem.CorrectRotation) continue;
                        if (showOnlyMissing && InventoryScanner.GetOwnedCount(housingItem.ItemKey) > 0) continue;

                        int originalIndex = itemList.IndexOf(housingItem);
                        if (itemSheet.HasRow(housingItem.ItemKey)) { DrawIcon(itemSheet.GetRow(housingItem.ItemKey).Icon, new Vector2(20, 20)); ImGui.SameLine(); }
                        if (housingItem.ItemStruct == IntPtr.Zero) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1));
                        ImGui.Text(housingItem.Name); ImGui.NextColumn();
                        DrawRow(originalIndex, housingItem, !isUnused);
                        if (housingItem.ItemStruct == IntPtr.Zero) ImGui.PopStyleColor();
                        ImGui.Separator();
                    }
                    ImGui.Columns(1); ImGui.TreePop();
                } else ImGui.PopStyleColor();
            }
        }

        private void DrawRow(int i, HousingItem housingItem, bool showSetPosition = true)
        {
            if (!housingItem.CorrectLocation) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1));
            ImGui.Text($"{housingItem.X:N4}, {housingItem.Y:N4}, {housingItem.Z:N4}"); 
            if (!housingItem.CorrectLocation) ImGui.PopStyleColor();
            ImGui.NextColumn();
            if (!housingItem.CorrectRotation) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1));
            ImGui.Text($"{housingItem.Rotate:N3}"); 
            if (!housingItem.CorrectRotation) ImGui.PopStyleColor();
            ImGui.NextColumn();
            var stainSheet = DalamudApi.DataManager.GetExcelSheet<Stain>();
            if (housingItem.Stain != 0 && stainSheet.HasRow(housingItem.Stain)) {
                Utils.StainButton("dye_" + i, stainSheet.GetRow(housingItem.Stain), new Vector2(20)); ImGui.SameLine();
                if (!housingItem.DyeMatch) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1));
                ImGui.Text(stainSheet.GetRow(housingItem.Stain).Name.ToString());
                if (!housingItem.DyeMatch) ImGui.PopStyleColor();
            } else if (housingItem.MaterialItemKey != 0) {
                var it = DalamudApi.DataManager.GetExcelSheet<Item>();
                if (it.HasRow(housingItem.MaterialItemKey)) { 
                    if (!housingItem.DyeMatch) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1));
                    DrawIcon(it.GetRow(housingItem.MaterialItemKey).Icon, new Vector2(20, 20)); ImGui.SameLine(); 
                    ImGui.Text(it.GetRow(housingItem.MaterialItemKey).Name.ToString()); 
                    if (!housingItem.DyeMatch) ImGui.PopStyleColor();
                }
            }
            ImGui.NextColumn();
            if (showSetPosition) {
                if (housingItem.ItemStruct != IntPtr.Zero) {
                    if (ImGui.Button(Lang.GetText("Set") + "##" + i)) { Plugin.MatchLayout(); SetItemPosition(housingItem); }
                }
                ImGui.NextColumn();
            }
        }

        public unsafe void DrawItemOnScreen()
        {
            if (!Config.DrawScreen) return;
            if (Memory.Instance == null) return;
            var itemList = Memory.Instance.GetCurrentTerritory() == Memory.HousingArea.Indoors ? Plugin.InteriorItemList : Plugin.ExteriorItemList;
            for (int i = 0; i < itemList.Count(); i++)
            {
                var playerPos = DalamudApi.ObjectTable.LocalPlayer.Position;
                var housingItem = itemList[i];
                if (housingItem.ItemStruct == IntPtr.Zero) continue;
                var itemStruct = (HousingItemStruct*)housingItem.ItemStruct;
                var itemPos = new Vector3(itemStruct->Position.X, itemStruct->Position.Y, itemStruct->Position.Z);
                if (Config.HiddenScreenItemHistory.IndexOf(i) >= 0) continue;
                if (Config.DrawDistance > 0 && (playerPos - itemPos).Length() > Config.DrawDistance) continue;
                if (DalamudApi.GameGui.WorldToScreen(itemPos, out var screenCoords))
                {
                    ImGui.SetNextWindowPos(new Vector2(screenCoords.X, screenCoords.Y));
                    ImGui.SetNextWindowBgAlpha(0.8f);
                    if (ImGui.Begin("HousingItem" + i, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav))
                    {
                        float distance = Vector3.Distance(playerPos, itemPos);
                        Vector4 textColor;
                        FontAwesomeIcon icon;

                        if (housingItem.CorrectLocation && housingItem.CorrectRotation)
                        {
                            textColor = new Vector4(0.5f, 0.5f, 0.5f, 0.8f); 
                            icon = FontAwesomeIcon.CheckCircle;
                        }
                        else
                        {
                            textColor = new Vector4(1.0f, 0.65f, 0.0f, 1.0f); 
                            icon = FontAwesomeIcon.Crosshairs;
                        }

                        DrawInlineIconColored(icon, textColor);
                        ImGui.SameLine();
                        ImGui.TextColored(textColor, $"{housingItem.Name} [{distance:F1}m]");
                        
                        ImGui.SameLine();
                        if (ImGui.Button(Lang.GetText("Set") + "##ScreenItem" + i.ToString()))
                        {
                            if (!Memory.Instance.CanEditItem()) continue;
                            SetItemPosition(housingItem); Config.HiddenScreenItemHistory.Add(i); Config.Save();
                        }
                        ImGui.End();
                    }
                }
            }
        }
    }
}