using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StockAnalysisSystem.Models;

namespace StockAnalysisSystem.Services
{
    public class StockApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey; // 可以配置API密钥

        public StockApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // 这里使用免费的股票API，实际使用时可以替换为其他API
            // 示例使用Alpha Vantage API或类似的免费API
            // 注意：需要注册获取API Key
            _apiKey = "EETLSYNXXZ61M6JL"; // 请替换为实际的API密钥
        }

        public async Task<StockData> GetDataAsync(string stockCode, int days)
        {
            try
            {
                // 获取实时数据
                StockData historicalData = await GetRealStockDataAsync(stockCode);

                // 【修复】确保返回的对象不为 null 且 HistoricalData 已初始化
                if (historicalData == null)
                {
                    historicalData = new StockData
                    {
                        Code = stockCode,
                        HistoricalData = new List<HistoricalData>()
                    };
                }

                if (historicalData.HistoricalData == null)
                {
                    historicalData.HistoricalData = new List<HistoricalData>();
                }

                if (days != 1)
                {
                    await Task.Delay(1500);
                    historicalData = await GetHistoricalStockDataAsync(historicalData, stockCode, days);
                }

                return historicalData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GetDataAsync 异常 [{stockCode}]: {ex.Message}");
                // 【修复】异常时返回带初始化列表的对象
                return new StockData
                {
                    Code = stockCode,
                    HistoricalData = new List<HistoricalData>()
                };
            }
        }

        /// <summary>
        /// 【修复版】获取实时股票数据 - 确保始终返回有效对象
        /// </summary>
        private async Task<StockData> GetRealStockDataAsync(string stockCode)
        {
            try
            {
                // 示例：使用Alpha Vantage API
                string url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={stockCode}&apikey={_apiKey}";

                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                // ✅ 打印请求的 URL
                System.Diagnostics.Debug.WriteLine($"=== 请求 URL ===");
                System.Diagnostics.Debug.WriteLine(url);

                // ✅ 打印原始返回内容
                System.Diagnostics.Debug.WriteLine($"=== API 原始返回 ===");
                System.Diagnostics.Debug.WriteLine(response.Length > 20 ? response.Substring(0, 20) : response);

                // ✅ 打印 JSON 的 key
                System.Diagnostics.Debug.WriteLine($"=== JSON Keys ===");
                foreach (var key in json.Properties().Select(p => p.Name))
                {
                    System.Diagnostics.Debug.WriteLine($"  - {key}");
                }

                // 检查 API 频率超限
                if (json["Note"] != null || json["Information"] != null)
                {
                    string errorMsg = json["Note"]?.ToString() ?? json["Information"]?.ToString() ?? "未知错误";
                    System.Diagnostics.Debug.WriteLine($"⚠️ API 调用频率超限: {errorMsg}");

                    // 【修复】返回带有初始化列表的对象，而不是空对象
                    return new StockData
                    {
                        Code = stockCode,
                        HistoricalData = new List<HistoricalData>()
                    };
                }

                if (json["Global Quote"] != null)
                {
                    var quote = json["Global Quote"];
                    if (quote != null && quote.HasValues)
                    {
                        StockData s = new StockData
                        {
                            Code = stockCode,
                            Name = quote["01. symbol"]?.ToString() ?? stockCode,
                            CurrentPrice = double.Parse(quote["05. price"]?.ToString() ?? "0"),
                            ChangePercent = ParsePercentString(quote["10. change percent"]?.ToString() ?? "0"),
                            Volume = long.Parse(quote["06. volume"]?.ToString() ?? "0"),
                            Open = double.Parse(quote["02. open"]?.ToString() ?? "0"),
                            High = double.Parse(quote["03. high"]?.ToString() ?? "0"),
                            Low = double.Parse(quote["04. low"]?.ToString() ?? "0"),
                            Close = double.Parse(quote["05. price"]?.ToString() ?? "0"),
                            NewDate = DateTime.TryParse(quote["07. latest trading day"]?.ToString(), out DateTime tradeDate) ? tradeDate : DateTime.MinValue,
                            UpdateTime = DateTime.Now,
                            HistoricalData = new List<HistoricalData>() // 【修复】确保初始化
                        };
                        return s;
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果API调用失败，记录日志
                System.Diagnostics.Debug.WriteLine($"❌ API调用失败 [{stockCode}]: {ex.Message}");
            }

            // 【修复】返回带有初始化列表的对象，而不是 null
            return new StockData
            {
                Code = stockCode,
                HistoricalData = new List<HistoricalData>()
            };
        }

        /// <summary>
        /// 【修复版】获取历史股票数据 - 确保返回正确初始化的对象
        /// </summary>
        private async Task<StockData> GetHistoricalStockDataAsync(StockData st, string stockCode, int days)
        {
            try
            {
                string url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={stockCode}&outputsize=compact&apikey={_apiKey}";
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                System.Diagnostics.Debug.WriteLine($"=== API 原始返回 (历史) ===");
                System.Diagnostics.Debug.WriteLine(response.Length > 15 ? response.Substring(0, 15) : response);
                System.Diagnostics.Debug.WriteLine($"=== JSON Keys ===");
                foreach (var key in json.Properties().Select(p => p.Name))
                {
                    System.Diagnostics.Debug.WriteLine($"  - {key}");
                }

                // 【修复】先检查是否有 API 错误（如频率超限）
                if (json["Note"] != null || json["Information"] != null)
                {
                    string errorMsg = json["Note"]?.ToString() ?? json["Information"]?.ToString() ?? "未知错误";
                    System.Diagnostics.Debug.WriteLine($"⚠️ API 调用频率超限（历史数据）: {errorMsg}");

                    // 【修复】返回传入的 st 对象（保留已有数据），确保 HistoricalData 已初始化
                    if (st != null)
                    {
                        if (st.HistoricalData == null)
                        {
                            st.HistoricalData = new List<HistoricalData>();
                        }
                        return st;
                    }

                    return new StockData
                    {
                        Code = stockCode,
                        HistoricalData = new List<HistoricalData>()
                    };
                }

                // 【修复】确保 st 对象和 HistoricalData 列表已初始化
                if (st == null)
                {
                    st = new StockData
                    {
                        Code = stockCode,
                        HistoricalData = new List<HistoricalData>()
                    };
                }

                if (st.HistoricalData == null)
                {
                    st.HistoricalData = new List<HistoricalData>();
                }

                // 处理正常的历史数据
                if (json["Time Series (Daily)"] != null)
                {
                    var timeSeries = json["Time Series (Daily)"];
                    int count = 0;

                    var sorted = timeSeries.Children<JProperty>()
                        .OrderByDescending(p =>
                        {
                            return DateTime.TryParse(p.Name, out DateTime dt) ? dt : DateTime.MinValue;
                        })
                        .Take(days);

                    foreach (var dateProperty in sorted)
                    {
                        if (count >= days) break;

                        if (!DateTime.TryParse(dateProperty.Name, out DateTime date))
                            continue;

                        var data = dateProperty.Value;

                        var record = new HistoricalData
                        {
                            Date = date,
                            Open = double.Parse(data["1. open"]?.ToString() ?? "0"),
                            High = double.Parse(data["2. high"]?.ToString() ?? "0"),
                            Low = double.Parse(data["3. low"]?.ToString() ?? "0"),
                            Close = double.Parse(data["4. close"]?.ToString() ?? "0"),
                            Volume = long.Parse(data["5. volume"]?.ToString() ?? "0")
                        };

                        st.HistoricalData.Add(record);
                        count++;
                    }

                    System.Diagnostics.Debug.WriteLine($"✅ 获取 {stockCode} 历史数据: {st.HistoricalData.Count} 条");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ {stockCode} API返回中没有 'Time Series (Daily)' 数据");
                }

                return st;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Alpha Vantage 历史数据 API 调用失败 [{stockCode}]: {ex.Message}");

                // 【修复】异常时返回传入的对象（确保 HistoricalData 已初始化）
                if (st != null)
                {
                    if (st.HistoricalData == null)
                    {
                        st.HistoricalData = new List<HistoricalData>();
                    }
                    return st;
                }

                return new StockData
                {
                    Code = stockCode,
                    HistoricalData = new List<HistoricalData>()
                };
            }
        }

        private double ParsePercentString(string percentStr)
        {
            try
            {
                if (string.IsNullOrEmpty(percentStr))
                    return 0;

                percentStr = percentStr.Replace("%", "");
                double value = double.Parse(percentStr);
                return value;
            }
            catch
            {
                return 0;
            }
        }
    }
}