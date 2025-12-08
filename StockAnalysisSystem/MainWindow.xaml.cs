using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Wpf;
using StockAnalysisSystem.Models;
using StockAnalysisSystem.Services;
using StockAnalysisSystem.Data;

namespace StockAnalysisSystem
{
    public partial class MainWindow : Window
    {
        // IDE0044：只读字段
        private readonly StockApiService _apiService;
        private readonly StockRepository _repository;
        private readonly SeriesCollection _seriesCollection;
        private ObservableCollection<StockItem> _favorites;
        private ObservableCollection<StockItem> _recentStocks;
        private StockData? _currentStock;

        public MainWindow()
        {
            InitializeComponent();
            _apiService = new StockApiService();
            _repository = new StockRepository();
            _favorites = new ObservableCollection<StockItem>();
            _recentStocks = new ObservableCollection<StockItem>();
            _seriesCollection = new SeriesCollection();

            PriceChart.Series = _seriesCollection;
            lstFavorites.ItemsSource = _favorites;
            lstRecent.ItemsSource = _recentStocks;

            cmbTimeRange.SelectionChanged += CmbTimeRange_SelectionChanged;

            LoadFavorites();
            LoadRecentStocks();
        }

        /*----------------  事件处理器  ----------------*/
        private async void CmbTimeRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentStock != null && !string.IsNullOrEmpty(_currentStock.Code))
                await SearchStockAsync();
        }

        private async void BtnSearch_Click(object sender, RoutedEventArgs e) => await SearchStockAsync();

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStock?.Code == null)
            {
                MessageBox.Show("请先查询股票", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            txtStockCode.Text = _currentStock.Code;
            await SearchStockAsync();
        }

        private void BtnAddToFavorites_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStock?.Code == null)
            {
                MessageBox.Show("请先查询股票", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var item = new StockItem
            {
                Code = _currentStock.Code,
                Name = _currentStock.Name,
                DisplayName = $"{_currentStock.Code} - {_currentStock.Name}"
            };

            if (_favorites.Any(f => f.Code == item.Code))
            {
                MessageBox.Show("该股票已在收藏列表中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _favorites.Add(item);
            _repository.SaveFavoriteStock(item.Code, item.Name);
        }
        private void BtnRegister_Click(object sender, RoutedEventArgs e) {
          //  MessageBox.Show("测试", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            LoginWindow loginWindow = new LoginWindow();

            //// 设置窗口属性（可选）
            //newWindow.Title = "股票详情";
            //newWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //newWindow.ResizeMode = ResizeMode.CanResize;

            // 打开窗口（非模态）
            loginWindow.Show();
        }
        
        private async void LstFavorites_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstFavorites.SelectedItem is StockItem s)
            {
                txtStockCode.Text = s.Code;
                await SearchStockAsync();
            }
        }

        private async void LstRecent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstRecent.SelectedItem is StockItem s)
            {
                txtStockCode.Text = s.Code;
                await SearchStockAsync();
            }
        }

        /*----------------  核心逻辑  ----------------*/
        private async Task SearchStockAsync()
        {
            string code = txtStockCode.Text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show("请输入股票代码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                txtStatus.Text = "正在查询...";

                // 取时间范围
                int days = GetSelectedDays();
                // ① 先拿全部数据（服务端只支持单参数时也能跑）
                StockData? data = await _apiService.GetDataAsync(code,days);
                if (data == null)
                {
                    MessageBox.Show("未找到股票信息", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    txtStatus.Text = "查询失败";
                    return;
                }

                // ② 本地过滤出最近 N 天
                DateTime cut = DateTime.Today.AddDays(-days);
                data.HistoricalData = data.HistoricalData?
                    .Where(h => h.Date >= cut)
                    .OrderBy(h => h.Date)
                    .ToList();

                _currentStock = data;
                DisplayStockInfo(_currentStock);
                UpdateChart(_currentStock, days);

                // 加入最近查询
                var item = new StockItem
                {
                    Code = _currentStock.Code,
                    Name = _currentStock.Name,
                    DisplayName = $"{_currentStock.Code} - {_currentStock.Name}"
                };
                AddToRecent(item);
                _repository.SaveRecentStock(item.Code, item.Name);

                txtStatus.Text = "查询成功";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查询失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "查询失败";
            }
        }

        private int GetSelectedDays()
        {
            return ((ComboBoxItem)cmbTimeRange.SelectedItem).Content.ToString() switch
            {
                "1天" => 1,
                "1周" => 7,
                "1月" => 30,
                "3月" => 90,
                "1年" => 365,
                _ => 30
            };
        }

        private void DisplayStockInfo(StockData stock)
        {
            txtStockName.Text = stock.Name;
            txtCurrentPrice.Text = $"¥{stock.CurrentPrice:F2}";
            txtChangePercent.Text = $"{stock.ChangePercent:F2}%";
            txtChangePercent.Foreground = stock.ChangePercent >= 0
                ? System.Windows.Media.Brushes.Red
                : System.Windows.Media.Brushes.Green;
            txtVolume.Text = stock.Volume.ToString("N0");
            txtNewDate.Text = $"{stock.NewDate.ToString("yyyy/MM/dd")}";
        }

        private void UpdateChart(StockData stock, int days)
        {
            _seriesCollection.Clear();
            if (stock.HistoricalData == null || stock.HistoricalData.Count == 0)
                return;

            var values = new ChartValues<double>(stock.HistoricalData.Select(h => h.Close));
            _seriesCollection.Add(new LineSeries
            {
                Title = "价格",
                Values = values,
                PointGeometry = null,
                LineSmoothness = 0
            });

            // 根据天数决定标签格式与密度
            string[] labels = days switch
            {
                <= 1 => stock.HistoricalData.Select(h => h.Date.ToString("MM/dd HH:mm")).ToArray(),
                <= 7 => stock.HistoricalData.Select(h => h.Date.ToString("MM/dd")).ToArray(),
                <= 30 => stock.HistoricalData.Select(h => h.Date.ToString("MM/dd")).ToArray(),
                _ => ReduceLabels(stock.HistoricalData.Select(h => h.Date.ToString("yyyy/MM")).ToList(), 12)
            };

            if (PriceChart.AxisX.Count == 0)
            {
                PriceChart.AxisX.Add(new Axis
                {
                    Labels = labels,
                    Separator = new LiveCharts.Wpf.Separator { Step = labels.Length > 20 ? labels.Length / 10 : 1 }
                });
            }
            else
            {
                PriceChart.AxisX[0].Labels = labels;
                PriceChart.AxisX[0].Separator.Step = labels.Length > 20 ? labels.Length / 10 : 1;
            }
        }

        private static string[] ReduceLabels(List<string> src, int max)
        {
            int step = Math.Max(1, src.Count / max);
            return src.Where((_, i) => i % step == 0).ToArray();
        }

        /*----------------  数据加载  ----------------*/
        private void LoadFavorites()
        {
            foreach (var f in _repository.GetFavoriteStocks())
                _favorites.Add(f);
        }

        private void LoadRecentStocks()
        {
            foreach (var r in _repository.GetRecentStocks())
                _recentStocks.Add(r);
        }

        private void AddToRecent(StockItem item)
        {
            var old = _recentStocks.FirstOrDefault(s => s.Code == item.Code);
            if (old != null) _recentStocks.Remove(old);
            _recentStocks.Insert(0, item);
            while (_recentStocks.Count > 20)
                _recentStocks.RemoveAt(_recentStocks.Count - 1);
        }


        
    }
}