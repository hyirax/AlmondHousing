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
    public partial class ConfigurationWindow
    {
        private Dictionary<string, (uint ItemId, string Name, int NeededCount, int OwnedCount, string Dye)> _materialCache = null;
        private bool _materialCacheDirty = true;

        public void InvalidateMaterialCache()
        {
            _materialCacheDirty = true;
        }

        private void RefreshMaterialCache(Dalamud.Game.ClientLanguage targetLang)
        {
            _materialCache = AggregateItems(targetLang);
            _materialCacheDirty = false;
        }

        private string GetRefreshButtonText()
        {
            return Config.UILanguage switch
            {
                "zh" => "刷新背包与列表",
                "zh_TW" => "重整背包與列表",
                "ja" => "所持品とリストを更新",
                "ko" => "소지품 및 목록 새로고침",
                "de" => "Inventar & Liste aktualisieren",
                "fr" => "Actualiser l'inventaire et la liste",
                _ => "Refresh Inventory & List"
            };
        }

        private string GetRefreshTooltipText()
        {
            return Config.UILanguage switch
            {
                "zh" => " 如果刚购买了物品，请点击刷新。",
                "zh_TW" => " 如果剛購買了物品，請點擊重整。",
                "ja" => " アイテムを購入したばかりの場合は、更新をクリックしてください。",
                "ko" => " 아이템을 방금 구매했다면 새로고침을 클릭하세요.",
                "de" => " Klicken Sie auf Aktualisieren, wenn Sie gerade Gegenstände gekauft haben.",
                "fr" => " Cliquez sur Actualiser si vous venez d'acheter des objets.",
                _ => " Click refresh if you just bought items."
            };
        }

        private void DrawMaterialTab()
        {
            DrawInlineIconColored(FontAwesomeIcon.ClipboardCheck, ACCENT_COLOR); ImGui.SameLine();
            ImGui.TextColored(ACCENT_COLOR, Lang.GetText("Inventory Material Audit"));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 5));

            string refreshText = GetRefreshButtonText();

            // 🚀【惊艳替换】换成了我们的动态高级按钮，带图标、发光、渐变！
            if (CustomUI.AnimatedButton("btn_refresh_mat", refreshText, new Vector2(240, 36), FontAwesomeIcon.Sync))
            {
                InvalidateMaterialCache();
            }
            
            ImGui.SameLine(0, 15);
            DrawInlineIcon(FontAwesomeIcon.InfoCircle); ImGui.SameLine();
            string baseDesc = Lang.GetText("Automatically scans Bag and Saddlebags.");
            string extraDesc = GetRefreshTooltipText();
            ImGui.TextDisabled(baseDesc + extraDesc);

            ImGui.Dummy(new Vector2(0, 10));

            if (_materialCache == null || _materialCacheDirty)
            {
                RefreshMaterialCache(Dalamud.Game.ClientLanguage.English);
            }

            var itemSheet = DalamudApi.DataManager.GetExcelSheet<Item>();

            if (Util.UniversalisClient.IsFetching)
            {
                DrawInlineIconColored(FontAwesomeIcon.Spinner, new Vector4(0.4f, 0.8f, 1f, 1f)); ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.4f, 0.8f, 1f, 1f), Lang.GetText("Fetching market prices from Universalis..."));
            }
            else
            {
                // 🚀【惊艳替换】计算预算按钮也换成高级动态按钮！
                if (CustomUI.AnimatedButton("btn_calc_budget", Lang.GetText("Calculate Budget"), new Vector2(240, 36), FontAwesomeIcon.Calculator))
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
                        foreach (var mat in _materialCache.Values)
                        {
                            int missing = Math.Max(0, mat.NeededCount - mat.OwnedCount);
                            if (missing > 0 && itemSheet.HasRow(mat.ItemId))
                            {
                                var row = itemSheet.GetRow(mat.ItemId);
                                if (row.ItemSearchCategory.RowId != 0) missingIds.Add(mat.ItemId);
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

                foreach (var mat in _materialCache.Values.OrderByDescending(m => Math.Max(0, m.NeededCount - m.OwnedCount)))
                {
                    int missing = Math.Max(0, mat.NeededCount - mat.OwnedCount);
                    bool isTradable = itemSheet.HasRow(mat.ItemId) && itemSheet.GetRow(mat.ItemId).ItemSearchCategory.RowId != 0;
                    
                    ImGui.TableNextRow();
                    
                    if (missing == 0) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 0.4f, 0.4f, 1.0f)); 
                    
                    ImGui.TableNextColumn();
                    if (itemSheet.HasRow(mat.ItemId)) { DrawIcon(itemSheet.GetRow(mat.ItemId).Icon, new Vector2(20)); ImGui.SameLine(); }
                    ImGui.Text(mat.Name);
                    
                    ImGui.TableNextColumn(); ImGui.Text($"{mat.NeededCount}");
                    ImGui.TableNextColumn(); ImGui.TextColored(mat.OwnedCount >= mat.NeededCount ? new Vector4(0.4f, 1f, 0.4f, 1f) : new Vector4(1f, 0.4f, 0.4f, 1f), $"{mat.OwnedCount}");
                    ImGui.TableNextColumn(); ImGui.TextColored(missing > 0 ? new Vector4(1f, 0.8f, 0.2f, 1f) : new Vector4(0.5f, 0.5f, 0.5f, 1f), missing > 0 ? $"{missing}" : "OK");
                    ImGui.TableNextColumn(); ImGui.Text(mat.Dye == "" ? "-" : mat.Dye);

                    ImGui.TableNextColumn();
                    int minPrice = 0;
                    bool hasPriceData = Util.UniversalisClient.PriceCache.TryGetValue(mat.ItemId, out minPrice);

                    if (missing == 0) { ImGui.TextDisabled("-"); } 
                    else if (!isTradable) { ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), Lang.GetText("Untradable")); } 
                    else if (hasPriceData) {
                        if (minPrice > 0) ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), $"{minPrice:N0}");
                        else ImGui.TextDisabled(Lang.GetText("Out of Stock"));
                    } else { ImGui.TextDisabled("?"); }

                    ImGui.TableNextColumn();
                    if (missing == 0 || !isTradable || !hasPriceData || minPrice <= 0) { ImGui.TextDisabled("-"); } 
                    else {
                        long cost = (long)minPrice * missing;
                        totalBudget += cost;
                        ImGui.TextColored(new Vector4(0.9f, 0.7f, 0.3f, 1f), $"{cost:N0}");
                    }

                    if (missing == 0) ImGui.PopStyleColor(); 
                }
                ImGui.EndTable();
            }

            if (totalBudget > 0)
            {
                ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
                ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.1f, 0.1f, 0.1f, 0.5f));
                ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 8.0f); 
                ImGui.BeginChild("BudgetPanel", new Vector2(-1, 40), true);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4); 

                DrawInlineIconColored(FontAwesomeIcon.Coins, new Vector4(1.0f, 0.84f, 0.0f, 1.0f)); ImGui.SameLine();
                ImGui.TextColored(new Vector4(1.0f, 0.84f, 0.0f, 1.0f), $"{Lang.GetText("Total Estimated Budget:")} {totalBudget:N0} Gil");
                
                ImGui.EndChild();
                ImGui.PopStyleVar();
                ImGui.PopStyleColor();
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
                    int owned = InventoryScanner.GetOwnedCount(itemRow.RowId);
                    results[uniqueKey] = (itemRow.RowId, itemRow.Name.ToString(), 1, owned, dyeName);
                }
            }
            return results;
        }

        private void ExportToTeamcraft(Dalamud.Game.ClientLanguage targetLang)
        {
            if (_materialCache == null) RefreshMaterialCache(targetLang);
            string exportText = _materialCache.Values
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
            if (_materialCache == null) RefreshMaterialCache(targetLang);
            if (_materialCache.Count == 0) return;
            string header = $"{Lang.GetText("Item ID")},{Lang.GetText("Item Name")},{Lang.GetText("Needed")},{Lang.GetText("Owned")},{Lang.GetText("Missing")},{Lang.GetText("Dye/Stain")}\n";
            string body = _materialCache.Values.Aggregate("", (c, m) => c + $"{m.ItemId},{m.Name},{m.NeededCount},{m.OwnedCount},{Math.Max(0, m.NeededCount-m.OwnedCount)},{(m.Dye == "" ? Lang.GetText("None") : m.Dye)}\n");
            ImGui.SetClipboardText(header + body); Log(Lang.GetText("CSV copied to clipboard! Paste it into Excel."));
        }
    }
}