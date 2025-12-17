using StockAnalysisSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace StockAnalysisSystem
{
    /// <summary>
    /// DataVisualizationWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DataVisualizationWindow : Window
    {
        // 在构造函数中添加调试：
        public DataVisualizationWindow(List<StockItem> favorites)
        {
            InitializeComponent();

            // 检查收到的数据
            if (favorites != null)
            {
                var nonZeroStocks = favorites.Where(s => s.ChangePercent != 0).ToList();
                MessageBox.Show($"收到 {favorites.Count} 只股票，其中 {nonZeroStocks.Count} 只有涨跌幅数据");
            }

            LoadRankings(favorites);
        }

        private void LoadRankings(List<StockItem> stocks)
        {
            // 按涨幅排序
            var gainers = stocks.Where(s => s.ChangePercent > 0)
                                .OrderByDescending(s => s.ChangePercent)
                                .Take(5);
            var losers = stocks.Where(s => s.ChangePercent < 0)
                               .OrderBy(s => s.ChangePercent)
                               .Take(5);

            foreach (var stock in gainers)
                AddStockItem(spnGainers, stock, true);

            foreach (var stock in losers)
                AddStockItem(spnLosers, stock, false);
        }

        private void AddStockItem(StackPanel panel, StockItem stock, bool isGainer)
        {
            var container = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };

            // 名称
            var name = new TextBlock
            {
                Text = stock.Name,
                Width = 100,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 百分比
            var percent = new TextBlock
            {
                Text = $"{stock.ChangePercent:F2}%",
                Width = 60,
                FontSize = 14,
                Foreground = new SolidColorBrush(isGainer ? Colors.Green : Colors.Red),
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right
            };

            // 进度条（模拟）
            double barWidth = Math.Abs(stock.ChangePercent) * 20; // 缩放因子
            if (barWidth > 200) barWidth = 200;

            var bar = new Rectangle
            {
                Width = barWidth,
                Height = 12,
                Fill = new SolidColorBrush(isGainer ? Colors.Green : Colors.Red),
                Style = (Style)FindResource("ChangeBarStyle")
            };

            container.Children.Add(name);
            container.Children.Add(percent);
            container.Children.Add(new TextBlock { Width = 10 }); // 间距
            container.Children.Add(bar);

            panel.Children.Add(container);
        }
    }
}
