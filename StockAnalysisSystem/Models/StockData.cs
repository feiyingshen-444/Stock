using System;
using System.Collections.Generic;

namespace StockAnalysisSystem.Models
{
    public class StockData
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double CurrentPrice { get; set; }
        public double ChangePercent { get; set; }
        public long Volume { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public DateTime NewDate { get; set; }
        public DateTime UpdateTime { get; set; }
        public List<HistoricalData>? HistoricalData { get; set; }
    }

    public class HistoricalData
    {
        public DateTime Date { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public long Volume { get; set; }
    }

    public class StockItem
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public double CurrentPrice { get; set; }      // 新增：当前价格
        public double ChangePercent { get; set; }
    }
}
