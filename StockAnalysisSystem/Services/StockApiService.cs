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
        //        // string url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={stockCode}&apikey={_apiKey}";

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
                historicalData = await GetHistoricalStockDataAsync(stockCode, days);
                historicalData = await GetRealStockDataAsync(stockCode,historicalData);
                return historicalData;
            }
            catch (Exception ex)
            {
                throw new Exception($"获取历史数据失败: {ex.Message}", ex);
            }
        }

        



        // 真实API调用示例（需要根据实际API文档调整）
        private async Task<StockData> GetRealStockDataAsync(string stockCode , StockData st)
        {
            try
            {
                // 示例：使用Alpha Vantage API
                string url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={stockCode}&apikey={_apiKey}";
                
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);
                
                if (json["Global Quote"] != null)
                {
                    var quote = json["Global Quote"];
                    if (quote != null)
                    {

                        st.Code = stockCode;
                        st.Name = quote["01. symbol"]?.ToString() ?? stockCode;
                        st.CurrentPrice = double.Parse(quote["05. price"]?.ToString() ?? "0");
                        st.ChangePercent = double.Parse(quote["10. change percent"]?.ToString()?.Replace("%", "") ?? "0");
                            //volume = long.parse(quote["06. volume"]?.tostring() ?? "0"),
                            //open = double.parse(quote["02. open"]?.tostring() ?? "0"),
                            //high = double.parse(quote["03. high"]?.tostring() ?? "0"),
                            //low = double.parse(quote["04. low"]?.tostring() ?? "0"),
                            //close = double.parse(quote["05. price"]?.tostring() ?? "0"),
                        st.UpdateTime = DateTime.Now;
                        
                    }
                    return st;
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
        private async Task<StockData> GetHistoricalStockDataAsync(string stockCode, int days)
        {
            var stockData = new StockData
            {
                Code = stockCode,
                HistoricalData = new List<HistoricalData>()
            };

            try
            {
                // Alpha Vantage 接口
                string url =
                    $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={stockCode}&outputsize=compact&apikey={_apiKey}";

                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                if (json["Time Series (Daily)"] != null)
                {
                    var timeSeries = json["Time Series (Daily)"];

                    int count = 0;

                    var sorted = timeSeries.Children<JProperty>()
                        .OrderByDescending(p => p.Name)
                        .Take(Math.Min(days * 2, timeSeries.Children().Count()));

                    foreach (var dateProperty in sorted)
                    {
                        if (count >= days) break;

                        var date = DateTime.Parse(dateProperty.Name);
                        var data = dateProperty.Value;

                        var record = new HistoricalData
                        {
                            Date = date,
                            Open = double.Parse(data["1. open"]?.ToString()),
                            High = double.Parse(data["2. high"]?.ToString()),
                            Low = double.Parse(data["3. low"]?.ToString()),
                            Close = double.Parse(data["4. close"]?.ToString()),
                            Volume = long.Parse(data["5. volume"]?.ToString())
                        };

                        stockData.HistoricalData.Add(record);
                        count++;
                    }

                    // 时间升序排列
                    stockData.HistoricalData = stockData.HistoricalData
                        .OrderBy(d => d.Date)
                        .ToList();

                    // 取最新一天数据填充 StockData 的其他字段
                    var latest = stockData.HistoricalData.LastOrDefault();
                    if (latest != null)
                    {
                        stockData.Open = latest.Open;
                        stockData.High = latest.High;
                        stockData.Low = latest.Low;
                        stockData.Close = latest.Close;
                        stockData.CurrentPrice = latest.Close;
                        stockData.Volume = latest.Volume;
                        stockData.UpdateTime = latest.Date;
                    }
                    stockData.Name = stockCode;
                }

                return stockData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Alpha Vantage 历史数据 API 调用失败: {ex.Message}");
                return stockData; // 返回空数据结构
            }
        }

    }
}
