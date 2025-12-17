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

        //public async Task<StockData> GetStockDataAsync(string stockCode,StockData st)
        //{
        //    try
        //    {
        //        // 示例：使用Alpha Vantage API
        //        // 实际使用时，可以根据需要替换为其他股票API
        //        // 这里提供一个模拟的实现，实际使用时需要根据API文档调整

        //        // 方法1：使用Alpha Vantage API
        //        // string url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol=INTC&apikey=EETLSYNXXZ61M6JL";

        //        // 方法2：使用中国股票API（如聚合数据、新浪财经等）
        //        // 这里使用模拟数据，实际项目中需要接入真实API
        //        StockData stockData = null;
        //        stockData = await GetRealStockDataAsync(stockCode,st);
        //        return  stockData;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"获取股票数据失败: {ex.Message}", ex);
        //    }
        //}

        public async Task<StockData> GetDataAsync(string stockCode, int days )
        {
            try
            {
                // 实际实现中应该调用真实API获取历史数据
                // 这里提供模拟数据
                StockData historicalData = null;
                historicalData = await GetRealStockDataAsync(stockCode);
                
                if (days != 1)
                {
                    await Task.Delay(1500);
                    historicalData = await GetHistoricalStockDataAsync(historicalData, stockCode, days);

                }

                return historicalData;
            }
            catch (Exception ex)
            {
                throw new Exception($"获取历史数据失败: {ex.Message}", ex);
            }
        }

        



        // 真实API调用示例（需要根据实际API文档调整）
        private async Task<StockData> GetRealStockDataAsync(string stockCode )
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

          

                // ✅ 打印原始返回内容（只打印前500字符，避免太长）
                System.Diagnostics.Debug.WriteLine($"=== API 原始返回 ===");
                System.Diagnostics.Debug.WriteLine(response.Length > 20 ? response.Substring(0, 20) : response);

             

                // ✅ 打印 JSON 的 key
                System.Diagnostics.Debug.WriteLine($"=== JSON Keys ===");
                foreach (var key in json.Properties().Select(p => p.Name))
                {
                    System.Diagnostics.Debug.WriteLine($"  - {key}");
                }
                if (json["Note"] != null || json["Information"] != null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ API 调用频率超限，请稍后再试");
                    StockData s = new StockData();
                    return  s;  // 返回没有历史数据的对象
                }




                if (json["Global Quote"] != null)
                {
                    var quote = json["Global Quote"];
                    if (quote != null)
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
                            NewDate = DateTime.TryParse(quote["07. latest trading day"]?.ToString(), out DateTime tradeDate) ? tradeDate: DateTime.MinValue,
                            UpdateTime = DateTime.Now,
                            HistoricalData = new List<HistoricalData>()
                        };
                        return s;
                    }
                    
                }
            }
            catch (Exception ex)
            {
                // 如果API调用失败，可以回退到模拟数据
                System.Diagnostics.Debug.WriteLine($"API调用失败: {ex.Message}");
            }

            return null;
        }
        //private async Task<List<HistoricalData>> GetHistoricalStockDataAsync(string stockCode, int days)
        //{
        //    try
        //    {
        //        // Alpha Vantage TIME_SERIES_DAILY接口
        //        string url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={stockCode}&outputsize=compact&apikey={_apiKey}";

        //        var response = await _httpClient.GetStringAsync(url);
        //        var json = JObject.Parse(response);

        //        if (json["Time Series (Daily)"] != null)
        //        {
        //            var timeSeries = json["Time Series (Daily)"];
        //            var historicalData = new List<HistoricalData>();
        //            int count = 0;

        //            // 按日期排序，取最近的数据
        //            var sortedDates = timeSeries.Children<JProperty>()
        //                .OrderByDescending(p => p.Name)
        //                .Take(Math.Min(days * 2, timeSeries.Children().Count())); // 取两倍天数，然后过滤周末

        //            foreach (var dateProperty in sortedDates)
        //            {
        //                if (count >= days) break;

        //                var date = DateTime.Parse(dateProperty.Name);


        //                var data = dateProperty.Value;

        //                historicalData.Add(new HistoricalData
        //                {
        //                    Date = date,
        //                    Open = double.Parse(data["1. open"]?.ToString()),
        //                    High = double.Parse(data["2. high"]?.ToString()),
        //                    Low = double.Parse(data["3. low"]?.ToString()),
        //                    Close = double.Parse(data["4. close"]?.ToString()),
        //                    Volume = long.Parse(data["5. volume"]?.ToString())
        //                });

        //                count++;
        //            }

        //            return historicalData.OrderBy(d => d.Date).ToList();
        //        }

        //        return new List<HistoricalData>();
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"Alpha Vantage历史数据API调用失败: {ex.Message}");
        //        return new List<HistoricalData>();
        //    }
        //}
        private async Task<StockData> GetHistoricalStockDataAsync(StockData st, string stockCode, int days)
        {
            try
            {
                string url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={stockCode}&outputsize=compact&apikey={_apiKey}";
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                System.Diagnostics.Debug.WriteLine($"=== API 原始返回 ===");
                System.Diagnostics.Debug.WriteLine(response.Length > 15 ? response.Substring(0, 15) : response);
                System.Diagnostics.Debug.WriteLine($"=== JSON Keys ===");
                foreach (var key in json.Properties().Select(p => p.Name))
                {
                    System.Diagnostics.Debug.WriteLine($"  - {key}");
                }

                // ✅ 先检查是否有 API 错误（如频率超限）
                if (json["Note"] != null || json["Information"] != null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ API 调用频率超限，请稍后再试（历史）");
                    return new StockData(); // 立即返回，不继续
                }

                // ✅ 再处理正常的历史数据（注意：这行必须在 if 外面！）
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

                        var date = DateTime.Parse(dateProperty.Name);
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
                }

                // ✅ 所有正常路径最终都返回 st
                return st;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Alpha Vantage 历史数据 API 调用失败: {ex.Message}");
                return new StockData(); // 异常时也返回有效对象（避免 null）
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
                return value / 100.0; // 关键：除以100
            }
            catch
            {
                return 0;
            }
        }

    }
}
