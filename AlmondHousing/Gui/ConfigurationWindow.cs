using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Utility;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using AlmondHousing.Objects;
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
    public class ConfigurationWindow : Window
    {
        private AlmondHousing Plugin;
        public Configuration Config => Plugin.Config;

        public bool CanUpload { get; set; }
        public bool CanImport { get; set; }

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
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, THEME_BASE);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, THEME_HOVER);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, THEME_ACTIVE);
        }

        public override void PostDraw()
        {
            ImGui.PopStyleColor(3);
        }

        public override void Draw()
        {
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
                    case 5: DrawMaterialTab(); break;
                }
            }
            ImGui.EndChild();

            this.FileDialogManager.Draw();
        }

        private void DrawSidebarItem(FontAwesomeIcon icon, string label, int index)
        {
            bool isSelected = selectedTab == index;
            float height = 36f;
            Vector2 startPos = ImGui.GetCursorPos();

            if (ImGui.Selectable($"##{index}_{label}", isSelected, ImGuiSelectableFlags.None, new Vector2(0, height)))
            {
                selectedTab = index;
            }

            if (isSelected) 
            {
                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();
                ImGui.GetWindowDrawList().AddLine(new Vector2(min.X + 2, min.Y + 4), new Vector2(min.X + 2, max.Y - 4), ImGui.ColorConvertFloat4ToU32(ACCENT_COLOR), 4.0f);
            }

            ImGui.SetCursorPos(new Vector2(startPos.X + 10, startPos.Y + (height - ImGui.GetTextLineHeight()) / 2));
            ImGui.PushFont(UiBuilder.IconFont);
            if (isSelected) ImGui.PushStyleColor(ImGuiCol.Text, ACCENT_COLOR);
            ImGui.Text(icon.ToIconString());
            if (isSelected) ImGui.PopStyleColor();
            ImGui.PopFont();

            if (isSelected) ImGui.PushStyleColor(ImGuiCol.Text, ACCENT_COLOR);
            ImGui.SetCursorPos(new Vector2(startPos.X + 38, startPos.Y + (height - ImGui.GetTextLineHeight()) / 2));
            ImGui.Text(label);
            if (isSelected) ImGui.PopStyleColor();

            ImGui.SetCursorPos(new Vector2(startPos.X, startPos.Y + height + 4));
        }

        private void DrawInlineIcon(FontAwesomeIcon icon)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text(icon.ToIconString());
            ImGui.PopFont();
        }

        #region 各选项卡内容渲染

        private void DrawHomeTab()
        {
            ImGui.TextColored(ACCENT_COLOR, Lang.GetText("Welcome to AlmondHousing!"));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 10));

            ImGui.BeginChild("HomeInfo", new Vector2(0, 0), false);
            {
                ImGui.TextColored(ACCENT_COLOR, Lang.GetText("About this Plugin"));
                ImGui.TextWrapped(Lang.GetText("HomeDesc1"));
                ImGui.Dummy(new Vector2(0, 10));

                ImGui.TextColored(ACCENT_COLOR, Lang.GetText("Core Features"));
                ImGui.BulletText(Lang.GetText("Feat1"));
                ImGui.BulletText(Lang.GetText("Feat2"));
                ImGui.BulletText(Lang.GetText("Feat3"));
                ImGui.BulletText(Lang.GetText("Feat4"));
                ImGui.Dummy(new Vector2(0, 10));
                
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
            DrawInlineIcon(FontAwesomeIcon.Search); ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##SearchBox", Lang.GetText("Search furniture name..."), ref searchQuery, 256);
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

        private void DrawMaterialTab()
        {
            var itemSheet = DalamudApi.DataManager.GetExcelSheet<Item>();
            var allItems = Plugin.InteriorItemList.Concat(Plugin.ExteriorItemList).ToList();

            // =======================================
            // 🎨 模块 A：智能染色同步雷达 (Smart Dye Sync)
            // =======================================
            ImGui.TextColored(ACCENT_COLOR, Lang.GetText("Smart Dye Sync"));
            ImGui.Separator();
            
            // 🔥 这里就是控制染色总开关的地方！玩家想不染色可以直接点掉勾选框。
            bool syncPlaced = Config.EnableDyeSyncPlaced;
            if (ImGui.Checkbox(Lang.GetText("Sync placed furniture dyes"), ref syncPlaced)) {
                Config.EnableDyeSyncPlaced = syncPlaced;
                Config.Save();
            }
            
            ImGui.SameLine();
            
            bool dyeOnPlace = Config.EnableDyeOnPlacement;
            if (ImGui.Checkbox(Lang.GetText("Auto-dye during placement"), ref dyeOnPlace)) {
                Config.EnableDyeOnPlacement = dyeOnPlace;
                Config.Save();
            }
            
            ImGui.Dummy(new Vector2(0, 5));

            var dyeAudit = new Dictionary<uint, (string Name, int NeededPlaced, int NeededUnplaced, int Owned)>();
            foreach (var item in allItems)
            {
                if (item.Stain != 0)
                {
                    bool isPlaced = item.ItemStruct != IntPtr.Zero;
                    bool needsSync = false;
                    
                    if (isPlaced)
                    {
                        unsafe
                        {
                            // 💡 修复：使用 HousingGameObject 并且检查 color
                            var itemStruct = (HousingGameObject*)item.ItemStruct;
                            if (itemStruct->color != item.Stain) needsSync = true;
                        }
                    }
                    else
                    {
                        needsSync = true;
                    }

                    if (needsSync)
                    {
                        uint dyeItemId = Util.DyeManager.GetRequiredDyeItemId(item.Stain);
                        if (dyeItemId != 0 && itemSheet.HasRow(dyeItemId))
                        {
                            if (!dyeAudit.ContainsKey(dyeItemId))
                            {
                                string name = itemSheet.GetRow(dyeItemId).Name.ToString();
                                int owned = InventoryScanner.GetOwnedCount(dyeItemId);
                                dyeAudit[dyeItemId] = (name, 0, 0, owned);
                            }
                            var current = dyeAudit[dyeItemId];
                            if (isPlaced) current.NeededPlaced++;
                            else current.NeededUnplaced++;
                            dyeAudit[dyeItemId] = current;
                        }
                    }
                }
            }

            // 💡 修复：把 allDyesSufficient 移到循环外面，这样作用域就完全覆盖整个逻辑了
            bool allDyesSufficient = true;

            if (dyeAudit.Count > 0)
            {
                if (ImGui.BeginTable("DyeAuditTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn(Lang.GetText("Dye"), ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn(Lang.GetText("Needed (Placed)"), ImGuiTableColumnFlags.WidthFixed, 100f);
                    ImGui.TableSetupColumn(Lang.GetText("Needed (Unplaced)"), ImGuiTableColumnFlags.WidthFixed, 100f);
                    ImGui.TableSetupColumn(Lang.GetText("Owned"), ImGuiTableColumnFlags.WidthFixed, 80f);
                    ImGui.TableSetupColumn(Lang.GetText("Status"), ImGuiTableColumnFlags.WidthFixed, 80f);
                    ImGui.TableHeadersRow();

                    foreach (var kvp in dyeAudit)
                    {
                        var data = kvp.Value;
                        int totalNeeded = 0;
                        if (Config.EnableDyeSyncPlaced) totalNeeded += data.NeededPlaced;
                        if (Config.EnableDyeOnPlacement) totalNeeded += data.NeededUnplaced;

                        bool sufficient = data.Owned >= totalNeeded;
                        if (!sufficient && totalNeeded > 0) allDyesSufficient = false;

                        ImGui.TableNextRow();
                        ImGui.TableNextColumn(); ImGui.Text(data.Name);
                        ImGui.TableNextColumn(); ImGui.Text($"{data.NeededPlaced}");
                        ImGui.TableNextColumn(); ImGui.Text($"{data.NeededUnplaced}");
                        ImGui.TableNextColumn(); ImGui.Text($"{data.Owned}");
                        
                        ImGui.TableNextColumn();
                        if (totalNeeded == 0) ImGui.TextDisabled("-");
                        else if (sufficient) ImGui.TextColored(new Vector4(0.4f, 1f, 0.4f, 1f), "OK");
                        else ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), Lang.GetText("Missing"));
                    }
                    ImGui.EndTable();
                }

                ImGui.Dummy(new Vector2(0, 5));
                
                // 一键同步按钮 (带防呆锁定)
                if (Config.EnableDyeSyncPlaced)
                {
                    if (!allDyesSufficient) ImGui.BeginDisabled();
                    if (ImGui.Button(Lang.GetText("Execute Dye Sync"), new Vector2(200, 30)))
                    {
                        ExecuteDyeSync();
                    }
                    if (!allDyesSufficient) ImGui.EndDisabled();
                }
            }
            else
            {
                ImGui.TextDisabled(Lang.GetText("All placed furniture dyes are matched."));
            }

            ImGui.Dummy(new Vector2(0, 15));


            // =======================================
            // 📦 模块 B：常规库存材料盘点与预算计算
            // =======================================
            ImGui.TextColored(ACCENT_COLOR, Lang.GetText("Inventory Material Audit"));
            ImGui.Separator();
            ImGui.TextDisabled(Lang.GetText("Automatically scans Bag and Saddlebags."));
            ImGui.Dummy(new Vector2(0, 5));

            var materials = AggregateItems(Dalamud.Game.ClientLanguage.English);

            // --- 🚀 预算查价按钮区 ---
            if (Util.UniversalisClient.IsFetching)
            {
                ImGui.TextColored(new Vector4(0.4f, 0.8f, 1f, 1f), Lang.GetText("Fetching market prices from Universalis..."));
            }
            else
            {
                if (ImGui.Button(Lang.GetText("Calculate Budget"), new Vector2(200, 30)))
                {
                    uint worldId = 0;
                    if (DalamudApi.ObjectTable.Length > 0)
                    {
                        var pc = DalamudApi.ObjectTable[0] as Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter;
                        if (pc != null) worldId = pc.CurrentWorld.RowId;
                    }

                    if (worldId != 0)
                    {
                        var missingIds = new List<uint>();
                        foreach (var mat in materials.Values)
                        {
                            int missing = Math.Max(0, mat.NeededCount - mat.OwnedCount);
                            if (missing > 0 && itemSheet.HasRow(mat.ItemId))
                            {
                                var row = itemSheet.GetRow(mat.ItemId);
                                if (row.ItemSearchCategory.RowId != 0)
                                {
                                    missingIds.Add(mat.ItemId);
                                }
                            }
                        }
                        System.Threading.Tasks.Task.Run(() => Util.UniversalisClient.FetchPricesAsync(missingIds, worldId));
                    }
                    else
                    {
                        LogError(Lang.GetText("Cannot determine your current server."));
                    }
                }
            }
            ImGui.Dummy(new Vector2(0, 5));

            // --- 🚀 材料数据表格 ---
            long totalBudget = 0;

            if (ImGui.BeginTable("MaterialTable", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn(Lang.GetText("Item"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(Lang.GetText("Needed"), ImGuiTableColumnFlags.WidthFixed, 50f);
                ImGui.TableSetupColumn(Lang.GetText("Owned"), ImGuiTableColumnFlags.WidthFixed, 50f);
                ImGui.TableSetupColumn(Lang.GetText("Missing"), ImGuiTableColumnFlags.WidthFixed, 50f);
                ImGui.TableSetupColumn(Lang.GetText("Dye"), ImGuiTableColumnFlags.WidthFixed, 80f);
                ImGui.TableSetupColumn(Lang.GetText("Unit Price"), ImGuiTableColumnFlags.WidthFixed, 70f);
                ImGui.TableSetupColumn(Lang.GetText("Subtotal"), ImGuiTableColumnFlags.WidthFixed, 80f);
                ImGui.TableHeadersRow();

                foreach (var mat in materials.Values.OrderByDescending(m => Math.Max(0, m.NeededCount - m.OwnedCount)))
                {
                    int missing = Math.Max(0, mat.NeededCount - mat.OwnedCount);
                    bool isTradable = itemSheet.HasRow(mat.ItemId) && itemSheet.GetRow(mat.ItemId).ItemSearchCategory.RowId != 0;
                    
                    ImGui.TableNextRow();
                    
                    ImGui.TableNextColumn();
                    if (itemSheet.HasRow(mat.ItemId)) { DrawIcon(itemSheet.GetRow(mat.ItemId).Icon, new Vector2(20)); ImGui.SameLine(); }
                    ImGui.Text(mat.Name);
                    
                    ImGui.TableNextColumn(); ImGui.Text($"{mat.NeededCount}");
                    ImGui.TableNextColumn(); ImGui.TextColored(mat.OwnedCount >= mat.NeededCount ? new Vector4(0.4f, 1f, 0.4f, 1f) : new Vector4(1f, 0.4f, 0.4f, 1f), $"{mat.OwnedCount}");
                    ImGui.TableNextColumn(); ImGui.TextColored(missing > 0 ? new Vector4(1f, 0.8f, 0.2f, 1f) : new Vector4(0.5f, 0.5f, 0.5f, 1f), missing > 0 ? $"{missing}" : "OK");
                    ImGui.TableNextColumn(); ImGui.Text(mat.Dye == "" ? "-" : mat.Dye);

                    // 💰 单价显示
                    ImGui.TableNextColumn();
                    int minPrice = 0;
                    bool hasPriceData = Util.UniversalisClient.PriceCache.TryGetValue(mat.ItemId, out minPrice);

                    if (missing == 0) {
                        ImGui.TextDisabled("-");
                    } else if (!isTradable) {
                        ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), Lang.GetText("Untradable"));
                    } else if (hasPriceData) {
                        if (minPrice > 0) ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), $"{minPrice:N0}");
                        else ImGui.TextDisabled(Lang.GetText("Out of Stock"));
                    } else {
                        ImGui.TextDisabled("?");
                    }

                    // 💰 小计显示
                    ImGui.TableNextColumn();
                    if (missing == 0 || !isTradable || !hasPriceData || minPrice <= 0) {
                        ImGui.TextDisabled("-");
                    } else {
                        long cost = (long)minPrice * missing;
                        totalBudget += cost;
                        ImGui.TextColored(new Vector4(0.9f, 0.7f, 0.3f, 1f), $"{cost:N0}");
                    }
                }
                ImGui.EndTable();
            }

            if (totalBudget > 0)
            {
                ImGui.Dummy(new Vector2(0, 5));
                ImGui.TextColored(ACCENT_COLOR, $"{Lang.GetText("Total Estimated Budget:")} {totalBudget:N0} Gil");
            }
        }

        private void ExecuteDyeSync()
        {
            int successCount = 0;
            int failCount = 0;
            var allItems = Plugin.InteriorItemList.Concat(Plugin.ExteriorItemList).ToList();
            
            foreach (var item in allItems)
            {
                if (item.Stain != 0 && item.ItemStruct != IntPtr.Zero)
                {
                    unsafe
                    {
                        // 💡 修复：使用 HousingGameObject 并且检查 color
                        var itemStruct = (HousingGameObject*)item.ItemStruct;
                        if (itemStruct->color != item.Stain)
                        {
                            if (Util.DyeManager.DyePlacedFurniture(item, item.Stain))
                                successCount++;
                            else
                                failCount++;
                        }
                    }
                }
            }
            Log(string.Format(Lang.GetText("Dye Sync Complete: {0} success, {1} failed."), successCount, failCount));
        }

        private void DrawLayoutFileTab()
        {
            DrawInlineIcon(FontAwesomeIcon.FolderOpen); ImGui.SameLine();
            ImGui.TextColored(ACCENT_COLOR, Lang.GetText("Layout & Export"));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 10));

            ImGui.TextColored(ACCENT_COLOR, Lang.GetText("Save Layout"));
            if (!Config.SaveLocation.IsNullOrEmpty())
            {
                ImGui.TextWrapped($"{Lang.GetText("Current Path:")} {Config.SaveLocation}");
                string txtSave = Lang.GetText("Save");
                float bwSave = Math.Max(120f, ImGui.CalcTextSize(txtSave).X + 40f);
                if (ImGui.Button(txtSave, new Vector2(bwSave, 30))) SaveLayoutToFile();
                ImGui.SameLine();
            }
            
            string txtSaveAs = Lang.GetText("Save As");
            float bwSaveAs = Math.Max(120f, ImGui.CalcTextSize(txtSaveAs).X + 40f);
            if (ImGui.Button(txtSaveAs, new Vector2(bwSaveAs, 30))) ShowSaveDialog();

            ImGui.Dummy(new Vector2(0, 10));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 10));

            ImGui.TextColored(ACCENT_COLOR, Lang.GetText("Apply & Load Layout"));

            string btnApplyCurrent = Lang.GetText("Apply Current");
            string btnApplyFile = Lang.GetText("Apply from File");
            string btnLoadCurrent = Lang.GetText("Load Current");
            string btnLoadFile = Lang.GetText("Load from File");

            float buttonWidth = 160f; 

            void DrawHelpText(string text)
            {
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextDisabled(FontAwesomeIcon.InfoCircle.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine();
                ImGui.TextDisabled(text);
            }

            if (!Config.SaveLocation.IsNullOrEmpty())
            {
                if (ImGui.Button(btnApplyCurrent, new Vector2(buttonWidth, 30))) { CreateAutoBackup(); LoadLayoutFromFile(true); }
                DrawHelpText(Lang.GetText("Read from current path and place immediately"));
            }

            if (ImGui.Button(btnApplyFile, new Vector2(buttonWidth, 30))) { ShowLoadDialog(true); }
            DrawHelpText(Lang.GetText("Select a file and start placing immediately"));

            if (!Config.SaveLocation.IsNullOrEmpty())
            {
                if (ImGui.Button(btnLoadCurrent, new Vector2(buttonWidth, 30))) { LoadLayoutFromFile(false); }
                DrawHelpText(Lang.GetText("Update list only, do not move furniture"));
            }

            if (ImGui.Button(btnLoadFile, new Vector2(buttonWidth, 30))) { ShowLoadDialog(false); }
            DrawHelpText(Lang.GetText("Select file and update list, do not move"));

            ImGui.Dummy(new Vector2(0, 20));
            
            DrawInlineIcon(FontAwesomeIcon.ShoppingCart); ImGui.SameLine();
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
            if (ImGui.Button(Lang.GetText("Export for Teamcraft"))) ExportToTeamcraft(currentExportLang);
            ImGui.SameLine();
            if (ImGui.Button(Lang.GetText("Export to CSV"))) ExportToCSV(currentExportLang);
        }

        private void DrawSettingsTab()
        {
            DrawInlineIcon(FontAwesomeIcon.ExclamationTriangle); ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.3f, 0.3f, 1.0f)); 
            ImGui.TextWrapped(Lang.GetText("Anti-Resell Warning"));
            ImGui.PopStyleColor(); 
            ImGui.Dummy(new Vector2(0, 10));

            DrawInlineIcon(FontAwesomeIcon.Globe); ImGui.SameLine();
            ImGui.Text(Lang.GetText("Plugin Interface Language"));
            
            string[] supportedLangNames = { "简体中文", "繁體中文", "English", "日本語", "한국어", "Deutsch", "Français" };
            string[] supportedLangs = { "zh", "zh_TW", "en", "ja", "ko", "de", "fr" };
            int currentLangIndex = Math.Max(0, Array.IndexOf(supportedLangs, Config.UILanguage));

            ImGui.SetNextItemWidth(180); 
            if (ImGui.BeginCombo("##UILang", supportedLangNames[currentLangIndex]))
            {
                for (int i = 0; i < supportedLangs.Length; i++)
                {
                    DrawInlineIcon(FontAwesomeIcon.Globe); ImGui.SameLine();
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

            DrawInlineIcon(FontAwesomeIcon.CheckSquare); ImGui.SameLine();
            if (ImGui.Checkbox(Lang.GetText("Apply Layout"), ref Config.ApplyLayout)) Config.Save();
            
            DrawInlineIcon(FontAwesomeIcon.Tag); ImGui.SameLine();
            if (ImGui.Checkbox(Lang.GetText("Label Furniture"), ref Config.DrawScreen)) Config.Save();
            
            DrawInlineIcon(FontAwesomeIcon.InfoCircle); ImGui.SameLine();
            if (ImGui.Checkbox(Lang.GetText("Show Tooltips"), ref Config.ShowTooltips)) Config.Save();

            DrawInlineIcon(FontAwesomeIcon.Clock); ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt(Lang.GetText("Placement Interval (ms)"), ref Config.LoadInterval)) Config.Save();

            if (Memory.Instance != null && Memory.Instance.GetCurrentTerritory() == Memory.HousingArea.Indoors && Memory.Instance.GetIndoorHouseSize() != "Apartment")
            {
                ImGui.Dummy(new Vector2(0, 5));
                DrawInlineIcon(FontAwesomeIcon.LayerGroup); ImGui.SameLine();
                ImGui.Text(Lang.GetText("Selected Floors"));
                
                if (ImGui.Checkbox(Lang.GetText("Basement"), ref Config.Basement)) Config.Save();
                ImGui.SameLine();
                if (ImGui.Checkbox(Lang.GetText("Ground Floor"), ref Config.GroundFloor)) Config.Save();
                ImGui.SameLine();
                if (Memory.Instance.HasUpperFloor() && ImGui.Checkbox(Lang.GetText("Upper Floor"), ref Config.UpperFloor)) Config.Save();
            }
        }

        #endregion

        #region 核心功能逻辑区

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
                
                var itemStruct = (HousingGameObject*)housingItem.ItemStruct; 
                // 💡 修复：直接读取 X, Y, Z 属性，而不是从 Position 读取
                var itemPos = new Vector3(itemStruct->X, itemStruct->Y, itemStruct->Z);
                
                if (Config.HiddenScreenItemHistory.IndexOf(i) >= 0) continue;
                if (Config.DrawDistance > 0 && (playerPos - itemPos).Length() > Config.DrawDistance) continue;
                if (DalamudApi.GameGui.WorldToScreen(itemPos, out var screenCoords))
                {
                    ImGui.SetNextWindowPos(new Vector2(screenCoords.X, screenCoords.Y));
                    ImGui.SetNextWindowBgAlpha(0.8f);
                    if (ImGui.Begin("HousingItem" + i, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav))
                    {
                        ImGui.Text(housingItem.Name); ImGui.SameLine();
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

        private Dictionary<string, (uint ItemId, string Name, int NeededCount, int OwnedCount, string Dye)> AggregateItems(Dalamud.Game.ClientLanguage targetLang)
        {
            var results = new Dictionary<string, (uint ItemId, string Name, int NeededCount, int OwnedCount, string Dye)>();
            var allItems = Plugin.InteriorItemList.Concat(Plugin.ExteriorItemList).ToList();
            var itemSheet = DalamudApi.DataManager.GetExcelSheet<Item>(targetLang);
            var stainSheet = DalamudApi.DataManager.GetExcelSheet<Stain>(targetLang);
            
            foreach (var housingItem in allItems)
            {
                if (!itemSheet.HasRow(housingItem.ItemKey)) continue;
                var itemRow = itemSheet.GetRow(housingItem.ItemKey);
                string dyeName = (housingItem.Stain != 0 && stainSheet.HasRow(housingItem.Stain)) ? stainSheet.GetRow(housingItem.Stain).Name.ToString() : "";
                string uniqueKey = $"{itemRow.RowId}_{dyeName}";
                
                if (results.ContainsKey(uniqueKey)) 
                {
                    var cur = results[uniqueKey];
                    results[uniqueKey] = (cur.ItemId, cur.Name, cur.NeededCount + 1, cur.OwnedCount, cur.Dye);
                }
                else 
                {
                    // 💡 修复3：直接使用 InventoryScanner
                    int owned = InventoryScanner.GetOwnedCount(itemRow.RowId);
                    results[uniqueKey] = (itemRow.RowId, itemRow.Name.ToString(), 1, owned, dyeName);
                }
            }
            return results;
        }

        private void ExportToTeamcraft(Dalamud.Game.ClientLanguage targetLang)
        {
            var materials = AggregateItems(targetLang);
            string exportText = materials.Values
                .Select(m => {
                    int missing = Math.Max(0, m.NeededCount - m.OwnedCount);
                    return missing > 0 ? $"{m.Name} x{missing}\n" : "";
                })
                .Aggregate("", (c, s) => c + s);

            if (string.IsNullOrWhiteSpace(exportText)) Log(Lang.GetText("You already own all required items!"));
            else { ImGui.SetClipboardText(exportText); Log(Lang.GetText("List copied to clipboard! Paste it into Teamcraft.")); }
        }

        private void ExportToCSV(Dalamud.Game.ClientLanguage targetLang)
        {
            var materials = AggregateItems(targetLang);
            if (materials.Count == 0) return;
            string header = $"{Lang.GetText("Item ID")},{Lang.GetText("Item Name")},{Lang.GetText("Needed")},{Lang.GetText("Owned")},{Lang.GetText("Missing")},{Lang.GetText("Dye/Stain")}\n";
            string body = materials.Values.Aggregate("", (c, m) => c + $"{m.ItemId},{m.Name},{m.NeededCount},{m.OwnedCount},{Math.Max(0, m.NeededCount-m.OwnedCount)},{(m.Dye == "" ? Lang.GetText("None") : m.Dye)}\n");
            ImGui.SetClipboardText(header + body); Log(Lang.GetText("CSV copied to clipboard! Paste it into Excel."));
        }

        #endregion
    }
}