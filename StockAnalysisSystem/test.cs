// Program.cs - 精简版，只显示历史数据
using StockAnalysisSystem.Models;
using StockAnalysisSystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StockAnalysisSystem.test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "股票历史数据查询系统";
            Console.WriteLine("股票历史数据查询系统");
            Console.WriteLine("====================\n");

            var stockService = new StockApiService();

            while (true)
            {
                Console.Write("请输入股票代码 (如: AAPL, 000001.SZ)，输入 'q' 退出: ");
                string stockCode = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(stockCode) || stockCode.ToLower() == "q")
                {
                    Console.WriteLine("退出程序。");
                    break;
                }

                Console.Write("请输入查询天数 (默认30天): ");
                string daysInput = Console.ReadLine();
                int days = 30;

                if (!string.IsNullOrEmpty(daysInput) && int.TryParse(daysInput, out int inputDays) && inputDays > 0)
                {
                    days = inputDays;
                }

                Console.WriteLine($"\n正在查询 {stockCode} 最近 {days} 天的历史数据...\n");

                try
                {
                    // 获取历史数据
                    var historicalData = await stockService.GetHistoricalDataAsync(stockCode, days);

                    if (historicalData != null && historicalData.Count > 0)
                    {
                        DisplayHistoricalData(historicalData, stockCode);
                    }
                    else
                    {
                        Console.WriteLine("没有获取到历史数据。");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"查询失败: {ex.Message}");
                }

                Console.WriteLine("\n" + new string('=', 60) + "\n");
            }
        }

        static void DisplayHistoricalData(List<HistoricalData> historicalData, string stockCode)
        {
            // 按日期排序（从旧到新）
            var sortedData = historicalData
                .Where(h => h.Close > 0) // 过滤无效数据
                .OrderBy(h => h.Date)
                .ToList();

            if (sortedData.Count == 0)
            {
                Console.WriteLine("没有有效的历史数据。");
                return;
            }

            // 表头
            Console.WriteLine($"股票代码: {stockCode}");
            Console.WriteLine($"数据期间: {sortedData.First().Date:yyyy-MM-dd} 到 {sortedData.Last().Date:yyyy-MM-dd}");
            Console.WriteLine($"数据条数: {sortedData.Count} 条");
            Console.WriteLine();

            // 表头行
            Console.WriteLine("日期        开盘价    最高价    最低价    收盘价    涨跌幅      成交量");
            Console.WriteLine(new string('-', 70));

            // 显示数据
            double? previousClose = null;
            for (int i = 0; i < sortedData.Count; i++)
            {
                var data = sortedData[i];

                // 计算涨跌幅
                string changePercentStr = " - ";
                double changePercent = 0;

                if (previousClose.HasValue && previousClose.Value > 0)
                {
                    changePercent = (data.Close - previousClose.Value) / previousClose.Value * 100;
                    changePercentStr = $"{changePercent:+#.##%;-#.##%;0%}";
                }

                // 显示一行数据
                Console.Write($"{data.Date:yyyy-MM-dd}  ");
                Console.Write($"{data.Open,8:F2}  ");
                Console.Write($"{data.High,8:F2}  ");
                Console.Write($"{data.Low,8:F2}  ");
                Console.Write($"{data.Close,8:F2}  ");

                // 涨跌幅颜色
                if (changePercent > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else if (changePercent < 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                Console.Write($"{changePercentStr,10}  ");
                Console.ResetColor();

                // 成交量
                Console.Write($"{FormatVolume(data.Volume),12}");

                Console.WriteLine();

                previousClose = data.Close;
            }

            Console.WriteLine(new string('-', 70));

            // 统计信息
            DisplayStatistics(sortedData);
        }

        static void DisplayStatistics(List<HistoricalData> data)
        {
            if (data.Count < 2) return;

            var firstDay = data.First();
            var lastDay = data.Last();

            double totalChange = lastDay.Close - firstDay.Close;
            double totalChangePercent = totalChange / firstDay.Close * 100;

            double maxPrice = data.Max(d => d.High);
            double minPrice = data.Min(d => d.Low);
            double avgVolume = data.Average(d => d.Volume);

            Console.WriteLine("\n📊 统计信息:");
            Console.WriteLine($"总涨跌幅: {totalChange:+#.##;-#.##;0} ({totalChangePercent:+#.##%;-#.##%;0%})");
            Console.WriteLine($"最高价: {maxPrice:F2}");
            Console.WriteLine($"最低价: {minPrice:F2}");
            Console.WriteLine($"平均成交量: {FormatVolume(avgVolume)}");

            // 简单趋势判断
            Console.Write("趋势分析: ");
            if (totalChangePercent > 5)
                Console.WriteLine("📈 强势上涨");
            else if (totalChangePercent > 0)
                Console.WriteLine("↗️ 温和上涨");
            else if (totalChangePercent > -5)
                Console.WriteLine("↘️ 温和下跌");
            else
                Console.WriteLine("📉 明显下跌");
        }

        static string FormatVolume(double volume)
        {
            if (volume >= 100000000)
                return $"{(volume / 100000000.0):F2}亿";
            else if (volume >= 10000)
                return $"{(volume / 10000.0):F2}万";
            else
                return $"{volume:F0}";
        }
    }
}