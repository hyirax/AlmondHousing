using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlmondHousing.Util
{
    public static class UniversalisClient
    {
        private static readonly HttpClient client = new HttpClient();
        
        // 缓存：记录 物品ID -> 最低单价
        public static Dictionary<uint, int> PriceCache = new Dictionary<uint, int>();
        
        // 状态锁：防止因连续点击导致请求过快
        public static bool IsFetching { get; private set; } = false;

        public static async Task FetchPricesAsync(List<uint> itemIds, uint worldId)
        {
            if (itemIds == null || itemIds.Count == 0 || worldId == 0) return;
            
            IsFetching = true;

            try
            {
                // 去重，避免重复查询
                var uniqueIds = itemIds.Distinct().ToList();

                // Universalis 建议单次批量查询不超过 100 个物品，这里设定为 50 个一组
                int batchSize = 50;
                for (int i = 0; i < uniqueIds.Count; i += batchSize)
                {
                    var batch = uniqueIds.Skip(i).Take(batchSize).ToList();
                    string idsStr = string.Join(",", batch);
                    
                    // 构建 API URL：指定服务器 ID，且只要最低价 (listings=1)
                    string url = $"https://universalis.app/api/v2/{worldId}/{idsStr}?listings=1&entries=0";

                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(json))
                        {
                            var root = doc.RootElement;
                            
                            // 情况 A：查询多个物品时，结果在 "items" 节点下
                            if (root.TryGetProperty("items", out JsonElement itemsElement))
                            {
                                foreach (var property in itemsElement.EnumerateObject())
                                {
                                    uint id = uint.Parse(property.Name);
                                    int minPrice = property.Value.TryGetProperty("minPrice", out JsonElement minPriceEl) 
                                        && minPriceEl.ValueKind == JsonValueKind.Number 
                                        ? minPriceEl.GetInt32() : 0;
                                    PriceCache[id] = minPrice;
                                }
                            }
                            // 情况 B：如果批次里刚好只有一个物品，API 返回格式会略有不同
                            else if (root.TryGetProperty("itemID", out JsonElement idElement))
                            {
                                uint id = idElement.GetUInt32();
                                int minPrice = root.TryGetProperty("minPrice", out JsonElement minPriceEl) 
                                    && minPriceEl.ValueKind == JsonValueKind.Number 
                                    ? minPriceEl.GetInt32() : 0;
                                PriceCache[id] = minPrice;
                            }
                        }
                    }
                    // 访问频率保护：每组请求间隔 500ms
                    await Task.Delay(500); 
                }
            }
            catch (Exception ex)
            {
                // 这里暂时简单处理错误
                Console.WriteLine($"Universalis Error: {ex.Message}");
            }
            finally
            {
                IsFetching = false;
            }
        }
    }
}