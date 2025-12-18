using LiveCharts;
using LiveCharts.Wpf;
using StockAnalysisSystem.Data;
using StockAnalysisSystem.Models;
using StockAnalysisSystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StockAnalysisSystem
{
    /// <summary>
    /// DataVisualizationWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DataVisualizationWindow : Window
    {
        private readonly List<StockItem> _favorites;
        private StockRepository _repository;
        private StockApiService _apiService;
        private Dictionary<string, List<HistoricalData>> _stockHistoryData;

        // 图表颜色数组
        private readonly Color[] _chartColors = new Color[]
        {
            Color.FromRgb(33, 150, 243),   // 蓝色
            Color.FromRgb(76, 175, 80),    // 绿色
            Color.FromRgb(255, 152, 0),    // 橙色
            Color.FromRgb(156, 39, 176),   // 紫色
            Color.FromRgb(244, 67, 54),    // 红色
            Color.FromRgb(0, 188, 212),    // 青色
            Color.FromRgb(255, 193, 7),    // 黄色
            Color.FromRgb(121, 85, 72),    // 棕色
            Color.FromRgb(96, 125, 139),   // 蓝灰色
            Color.FromRgb(233, 30, 99)     // 粉色
        };

        public DataVisualizationWindow(List<StockItem> favorites)
        {
            _favorites = favorites ?? new List<StockItem>();
            _stockHistoryData = new Dictionary<string, List<HistoricalData>>();

            InitializeComponent();

            // 在 InitializeComponent 之后设置轴的格式化器
            SetupAxisFormatters();

            // 安全初始化仓储和服务
            InitializeServices();

            // 检查收到的数据
            LogReceivedData();

            // 所有初始化放在 Loaded 事件中执行
            Loaded += Window_Loaded;
        }

        private void SetupAxisFormatters()
        {
            try
            {
                // 设置Y轴（价格）格式化器
                if (AxisYLine != null)
                {
                    AxisYLine.LabelFormatter = value => value.ToString("F2");
                }

                // 设置Y轴（成交量）格式化器
                if (AxisYBar != null)
                {
                    AxisYBar.LabelFormatter = value => FormatVolume(value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetupAxisFormatters 异常: {ex.Message}");
            }
        }

        private void InitializeServices()
        {
            try
            {
                _repository = new StockRepository();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化 StockRepository 失败: {ex.Message}");
                _repository = null;
            }

            try
            {
                _apiService = new StockApiService();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化 StockApiService 失败: {ex.Message}");
                _apiService = null;
            }
        }

        private void LogReceivedData()
        {
            if (_favorites != null && _favorites.Count > 0)
            {
                var nonZeroStocks = _favorites.Where(s => s != null && s.ChangePercent != 0).ToList();
                System.Diagnostics.Debug.WriteLine($"收到 {_favorites.Count} 只股票，其中 {nonZeroStocks.Count} 只有涨跌幅数据");
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 加载涨跌排行榜
                LoadRankings(_favorites);

                // 异步加载图表数据
                await LoadChartDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Window_Loaded 异常: {ex.Message}");
            }
        }

        #region 涨跌排行榜

        private void LoadRankings(List<StockItem> stocks)
        {
            try
            {
                if (stocks == null || stocks.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("没有股票数据用于排行榜");
                    return;
                }

                if (spnGainers == null || spnLosers == null)
                {
                    System.Diagnostics.Debug.WriteLine("排行榜控件未初始化");
                    return;
                }

                var validStocks = stocks.Where(s => s != null).ToList();

                var gainers = validStocks.Where(s => s.ChangePercent >= 0)
                                         .OrderByDescending(s => s.ChangePercent)
                                         .Take(5);
                var losers = validStocks.Where(s => s.ChangePercent < 0)
                                        .OrderBy(s => s.ChangePercent)
                                        .Take(5);

                foreach (var stock in gainers)
                    AddStockItem(spnGainers, stock, true);

                foreach (var stock in losers)
                    AddStockItem(spnLosers, stock, false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadRankings 异常: {ex.Message}");
            }
        }

        private void AddStockItem(StackPanel panel, StockItem stock, bool isGainer)
        {
            if (panel == null || stock == null) return;

            try
            {
                var container = new StackPanel 
                { 
                    Orientation = Orientation.Horizontal, 
                    Margin = new Thickness(0, 0, 0, 12) 
                };

                var name = new TextBlock
                {
                    Text = stock.Name ?? stock.Code ?? "未知",
                    Width = 100,
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var percent = new TextBlock
                {
                    Text = $"{stock.ChangePercent:F2}%",
                    Width = 60,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(isGainer ? Colors.Green : Colors.Red),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Right
                };

                double barWidth = Math.Abs(stock.ChangePercent) * 20;
                if (barWidth > 200) barWidth = 200;
                if (barWidth < 1) barWidth = 1;

                var bar = new Rectangle
                {
                    Width = barWidth,
                    Height = 12,
                    Fill = new SolidColorBrush(isGainer ? Colors.Green : Colors.Red),
                    RadiusX = 2,
                    RadiusY = 2
                };

                container.Children.Add(name);
                container.Children.Add(percent);
                container.Children.Add(new TextBlock { Width = 10 });
                container.Children.Add(bar);

                panel.Children.Add(container);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddStockItem 异常: {ex.Message}");
            }
        }

        #endregion

        #region 图表数据加载

        private async Task LoadChartDataAsync()
        {
            if (_favorites == null || _favorites.Count == 0)
            {
                ShowNoData();
                return;
            }

            ShowLoading(true);

            try
            {
                int days = GetSelectedDays();
                _stockHistoryData.Clear();

                System.Diagnostics.Debug.WriteLine($"开始加载 {_favorites.Count} 只股票的数据，天数: {days}");

                foreach (var stock in _favorites)
                {
                    if (stock == null || string.IsNullOrEmpty(stock.Code)) continue;

                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"正在加载股票: {stock.Code} - {stock.Name}");

                        List<HistoricalData> historyFromDb = null;
                        if (_repository != null)
                        {
                            try
                            {
                                historyFromDb = _repository.GetStockHistoryData(stock.Code, days);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"从数据库获取 {stock.Code} 失败: {ex.Message}");
                            }
                        }

                        if (historyFromDb != null && historyFromDb.Count >= days / 2)
                        {
                            _stockHistoryData[stock.Code] = historyFromDb;
                            System.Diagnostics.Debug.WriteLine($"✅ 从数据库加载 {stock.Code} 的 {historyFromDb.Count} 条历史数据");
                        }
                        else if (_apiService != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"📡 从API获取 {stock.Code} 的数据...");
                            var stockData = await _apiService.GetDataAsync(stock.Code, days);

                            if (stockData?.HistoricalData != null && stockData.HistoricalData.Count > 0)
                            {
                                _stockHistoryData[stock.Code] = stockData.HistoricalData;

                                if (_repository != null)
                                {
                                    try
                                    {
                                        _repository.SaveStockHistoryData(stock.Code, stock.Name, stockData.HistoricalData);
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"保存 {stock.Code} 到数据库失败: {ex.Message}");
                                    }
                                }
                                System.Diagnostics.Debug.WriteLine($"✅ 从API加载 {stock.Code} 的 {stockData.HistoricalData.Count} 条历史数据");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ {stock.Code} 没有获取到历史数据");
                            }
                        }

                        await Task.Delay(1200);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ 加载 {stock.Code} 数据失败: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"数据加载完成，共 {_stockHistoryData.Count} 只股票有数据");

                UpdateCharts();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载图表数据异常: {ex.Message}");
                MessageBox.Show($"加载图表数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private int GetSelectedDays()
        {
            if (cmbChartRange == null) return 30;
            if (cmbChartRange.SelectedIndex == 0) return 7;
            if (cmbChartRange.SelectedIndex == 1) return 30;
            if (cmbChartRange.SelectedIndex == 2) return 90;
            return 30;
        }

        private void UpdateCharts()
        {
            System.Diagnostics.Debug.WriteLine($"更新图表，数据源包含 {_stockHistoryData.Count} 只股票");

            if (_stockHistoryData == null || _stockHistoryData.Count == 0)
            {
                ShowNoData();
                return;
            }

            HideNoData();
            UpdateLineChart();
            UpdateBarChart();
        }

        private void UpdateLineChart()
        {
            if (LineChart == null) return;

            try
            {
                LineChart.Series = new SeriesCollection();

                if (_stockHistoryData.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("折线图：没有数据可显示");
                    return;
                }

                var allDates = _stockHistoryData.Values
                    .SelectMany(h => h.Select(d => d.Date.Date))
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"折线图：日期范围 {allDates.FirstOrDefault():MM/dd} - {allDates.LastOrDefault():MM/dd}，共 {allDates.Count} 天");

                // 在代码中设置 X 轴标签
                var dateLabels = allDates.Select(d => d.ToString("MM/dd")).ToArray();
                if (AxisXLine != null)
                {
                    AxisXLine.Labels = dateLabels;
                }

                int colorIndex = 0;

                foreach (var kvp in _stockHistoryData)
                {
                    var stockCode = kvp.Key;
                    var history = kvp.Value.OrderBy(h => h.Date).ToList();
                    var stock = _favorites.FirstOrDefault(f => f != null && f.Code == stockCode);
                    var stockName = stock?.Name ?? stockCode;

                    System.Diagnostics.Debug.WriteLine($"折线图：添加 {stockName}（{stockCode}），{history.Count} 条数据");

                    var values = new ChartValues<double>();
                    foreach (var date in allDates)
                    {
                        var dataPoint = history.FirstOrDefault(h => h.Date.Date == date);
                        if (dataPoint != null)
                        {
                            values.Add(dataPoint.Close);
                        }
                        else
                        {
                            values.Add(double.NaN);
                        }
                    }

                    var lineSeries = new LineSeries
                    {
                        Title = stockName,
                        Values = values,
                        Stroke = new SolidColorBrush(_chartColors[colorIndex % _chartColors.Length]),
                        Fill = Brushes.Transparent,
                        StrokeThickness = 2,
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 6,
                        LineSmoothness = 0.3
                    };

                    LineChart.Series.Add(lineSeries);
                    colorIndex++;
                }

                System.Diagnostics.Debug.WriteLine($"折线图：共添加 {LineChart.Series.Count} 条折线");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateLineChart 异常: {ex.Message}");
            }
        }

        private void UpdateBarChart()
        {
            if (BarChart == null) return;

            try
            {
                BarChart.Series = new SeriesCollection();

                if (_stockHistoryData.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("柱状图：没有数据可显示");
                    return;
                }

                // 收集所有股票数据
                var stockDataList = new List<(string Name, double Volume)>();

                foreach (var kvp in _stockHistoryData)
                {
                    var stockCode = kvp.Key;
                    var history = kvp.Value;
                    var stock = _favorites.FirstOrDefault(f => f != null && f.Code == stockCode);
                    var stockName = stock?.Name ?? stockCode;

                    var latestData = history.OrderByDescending(h => h.Date).FirstOrDefault();

                    if (latestData != null)
                    {
                        stockDataList.Add((stockName, latestData.Volume));
                        System.Diagnostics.Debug.WriteLine($"柱状图：{stockName} 成交量 = {latestData.Volume}");
                    }
                }

                if (stockDataList.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("柱状图：没有有效数据");
                    return;
                }

                // 设置 X 轴标签
                var stockNames = stockDataList.Select(s => s.Name).ToArray();
                if (AxisXBar != null)
                {
                    AxisXBar.Labels = stockNames;
                }

                // 设置 Y 轴从 0 开始
                if (AxisYBar != null)
                {
                    AxisYBar.MinValue = 0;
                }

                // 禁用缩放和平移
                BarChart.Zoom = ZoomingOptions.None;
                BarChart.Pan = PanningOptions.None;

                // 使用单个 ColumnSeries，所有股票在同一个系列中
                // 这样 tooltip 只会显示当前悬停的那个值
                var allValues = new ChartValues<double>();
                foreach (var (name, volume) in stockDataList)
                {
                    allValues.Add(volume);
                }

                // 创建带有多种颜色的柱状图
                // 使用 ColumnSeries 的 Configuration 来为每个柱子设置不同颜色
                var columnSeries = new ColumnSeries
                {
                    Title = "成交量",
                    Values = allValues,
                    MaxColumnWidth = 60,
                    ColumnPadding = 5,
                    DataLabels = false,
                    // 使用渐变色或第一个颜色
                    Fill = new SolidColorBrush(_chartColors[0]),
                    LabelPoint = point =>
                    {
                        int index = (int)point.X;
                        if (index >= 0 && index < stockDataList.Count)
                        {
                            return $"{stockDataList[index].Name}: {FormatVolume(point.Y)}";
                        }
                        return FormatVolume(point.Y);
                    }
                };

                BarChart.Series.Add(columnSeries);

                // 如果想要多颜色，可以用多个系列（但tooltip会显示多个）
                // 这里选择单系列，tooltip更简洁

                System.Diagnostics.Debug.WriteLine($"柱状图：共 {stockDataList.Count} 只股票");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateBarChart 异常: {ex.Message}");
            }
        }

        #endregion

        #region 界面辅助方法

        private void ShowLoading(bool show)
        {
            if (LoadingOverlay != null)
            {
                LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ShowNoData()
        {
            if (txtNoData != null)
                txtNoData.Visibility = Visibility.Visible;
            if (LineChart != null)
                LineChart.Visibility = Visibility.Collapsed;
            if (BarChart != null)
                BarChart.Visibility = Visibility.Collapsed;
        }

        private void HideNoData()
        {
            if (txtNoData != null)
                txtNoData.Visibility = Visibility.Collapsed;
            UpdateChartVisibility();
        }

        private void UpdateChartVisibility()
        {
            if (LineChart != null && rbLineChart != null)
                LineChart.Visibility = rbLineChart.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (BarChart != null && rbBarChart != null)
                BarChart.Visibility = rbBarChart.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private string FormatVolume(double volume)
        {
            if (volume >= 1_000_000_000)
                return (volume / 1_000_000_000).ToString("F1") + "B";
            if (volume >= 1_000_000)
                return (volume / 1_000_000).ToString("F1") + "M";
            if (volume >= 1_000)
                return (volume / 1_000).ToString("F1") + "K";
            return volume.ToString("F0");
        }

        #endregion

        #region 事件处理

        private void ChartType_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            UpdateChartVisibility();
        }

        private void CmbChartRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            _ = LoadChartDataAsync();
        }

        private async void BtnRefreshChart_Click(object sender, RoutedEventArgs e)
        {
            _stockHistoryData.Clear();
            await LoadChartDataAsync();
        }

        #endregion
    }
}
