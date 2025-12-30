using LiveCharts;
using LiveCharts.Wpf;
using OfficeOpenXml;
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

                // 在UI线程上更新图表
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateCharts();
                });
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

        // ============================================================
        // 修复后的 RefreshAndFetchMissingDataAsync 方法
        // 请替换 DataVisualizationWindow.xaml.cs 中的同名方法
        // ============================================================

        /// <summary>
        /// 【修复版】刷新时专用的数据加载方法 - 获取数据库中缺失的股票最新数据并保存，然后从数据库重新加载
        /// </summary>
        private async Task RefreshAndFetchMissingDataAsync()
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
                int fetchedFromApi = 0;
                int savedToDb = 0;        // 【新增】成功保存到数据库的计数
                int saveFailedCount = 0;  // 【新增】保存失败的计数
                List<string> failedStocks = new List<string>(); // 【新增】保存失败的股票列表

                System.Diagnostics.Debug.WriteLine($"🔄 开始刷新数据，检查 {_favorites.Count} 只股票...");

                // 第一步：检查并从API获取缺失的数据，保存到数据库
                foreach (var stock in _favorites)
                {
                    if (stock == null || string.IsNullOrEmpty(stock.Code)) continue;

                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"正在检查股票: {stock.Code} - {stock.Name}");

                        // 从数据库获取现有数据
                        List<HistoricalData> historyFromDb = null;
                        DateTime? latestDateInDb = null;

                        if (_repository != null)
                        {
                            try
                            {
                                historyFromDb = _repository.GetStockHistoryData(stock.Code, days);
                                if (historyFromDb != null && historyFromDb.Count > 0)
                                {
                                    latestDateInDb = historyFromDb.Max(h => h.Date);
                                    System.Diagnostics.Debug.WriteLine($"📊 数据库中 {stock.Code} 最新数据日期: {latestDateInDb:yyyy-MM-dd}，共 {historyFromDb.Count} 条");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"从数据库获取 {stock.Code} 失败: {ex.Message}");
                            }
                        }

                        // 判断是否需要从API获取新数据
                        bool needFetchFromApi = false;

                        if (historyFromDb == null || historyFromDb.Count < days / 2)
                        {
                            needFetchFromApi = true;
                            System.Diagnostics.Debug.WriteLine($"📡 {stock.Code} 数据库数据不足，需要从API获取");
                        }
                        else if (latestDateInDb.HasValue)
                        {
                            DateTime today = DateTime.Today;
                            int daysDiff = (today - latestDateInDb.Value).Days;
                            if (daysDiff > 1)
                            {
                                needFetchFromApi = true;
                                System.Diagnostics.Debug.WriteLine($"📡 {stock.Code} 数据库数据可能过期（{daysDiff}天前），需要从API更新");
                            }
                        }

                        // 从API获取数据并保存到数据库
                        if (needFetchFromApi && _apiService != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"📡 正在从API获取 {stock.Code} 的最新数据...");

                            var stockData = await _apiService.GetDataAsync(stock.Code, days);

                            // 【修复】检查返回的数据是否有效
                            if (stockData != null &&
                                stockData.HistoricalData != null &&
                                stockData.HistoricalData.Count > 0)
                            {
                                fetchedFromApi++;
                                System.Diagnostics.Debug.WriteLine($"✅ API返回 {stock.Code} 的 {stockData.HistoricalData.Count} 条数据");

                                // 保存新数据到数据库
                                if (_repository != null)
                                {
                                    try
                                    {
                                        bool saveResult = _repository.SaveStockHistoryData(stock.Code, stock.Name, stockData.HistoricalData);
                                        if (saveResult)
                                        {
                                            savedToDb++;
                                            System.Diagnostics.Debug.WriteLine($"💾 已保存 {stock.Code} 的数据到数据库");
                                        }
                                        else
                                        {
                                            saveFailedCount++;
                                            failedStocks.Add(stock.Code);
                                            System.Diagnostics.Debug.WriteLine($"⚠️ 保存 {stock.Code} 到数据库失败（SaveStockHistoryData 返回 false）");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        saveFailedCount++;
                                        failedStocks.Add(stock.Code);
                                        System.Diagnostics.Debug.WriteLine($"❌ 保存 {stock.Code} 到数据库异常: {ex.Message}");
                                    }
                                }
                                else
                                {
                                    saveFailedCount++;
                                    failedStocks.Add(stock.Code);
                                    System.Diagnostics.Debug.WriteLine($"❌ _repository 为 null，无法保存 {stock.Code}");
                                }
                            }
                            else
                            {
                                // 【修复】API未返回有效数据时给出明确提示
                                string reason = stockData == null ? "stockData 为 null" :
                                                stockData.HistoricalData == null ? "HistoricalData 为 null" :
                                                "HistoricalData 为空";
                                System.Diagnostics.Debug.WriteLine($"⚠️ {stock.Code} API未返回有效数据 ({reason})");
                                System.Diagnostics.Debug.WriteLine($"   提示: 可能是API调用频率超限，请等待1分钟后重试");
                            }

                            // API请求间隔，避免频率限制（Alpha Vantage 免费版每分钟5次）
                            await Task.Delay(1500);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ 处理 {stock.Code} 时出错: {ex.Message}");
                    }
                }

                // 第二步：从数据库重新加载所有数据
                _stockHistoryData.Clear();

                foreach (var stock in _favorites)
                {
                    if (stock == null || string.IsNullOrEmpty(stock.Code)) continue;

                    if (_repository != null)
                    {
                        try
                        {
                            var historyFromDb = _repository.GetStockHistoryData(stock.Code, days);
                            if (historyFromDb != null && historyFromDb.Count > 0)
                            {
                                _stockHistoryData[stock.Code] = historyFromDb;
                                System.Diagnostics.Debug.WriteLine($"✅ 从数据库加载 {stock.Code} 的 {historyFromDb.Count} 条历史数据");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"从数据库获取 {stock.Code} 失败: {ex.Message}");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"🔄 刷新完成：从API获取 {fetchedFromApi} 只，保存成功 {savedToDb} 只，保存失败 {saveFailedCount} 只，共 {_stockHistoryData.Count} 只股票有数据");

                // 第三步：在UI线程上更新图表
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateCharts();
                    System.Diagnostics.Debug.WriteLine($"📈 图表已刷新，LineChart.Series.Count = {LineChart?.Series?.Count ?? 0}");
                });

                // 【修复】显示更详细的刷新结果，包括保存失败的情况
                string message;
                MessageBoxImage icon;

                if (saveFailedCount > 0)
                {
                    message = $"数据刷新完成，但部分数据保存失败！\n\n" +
                              $"• 从API获取: {fetchedFromApi} 只股票\n" +
                              $"• 成功保存到数据库: {savedToDb} 只\n" +
                              $"• 保存失败: {saveFailedCount} 只\n" +
                              $"• 当前可显示: {_stockHistoryData.Count} 只\n\n" +
                              $"保存失败的股票: {string.Join(", ", failedStocks)}\n\n" +
                              $"请检查数据库连接或稍后重试。";
                    icon = MessageBoxImage.Warning;
                }
                else if (fetchedFromApi > 0)
                {
                    message = $"数据刷新完成！\n\n" +
                              $"• 从API获取并保存: {fetchedFromApi} 只股票\n" +
                              $"• 当前可显示: {_stockHistoryData.Count} 只股票";
                    icon = MessageBoxImage.Information;
                }
                else
                {
                    message = $"数据已是最新，无需从API获取\n\n" +
                              $"• 当前可显示: {_stockHistoryData.Count} 只股票";
                    icon = MessageBoxImage.Information;
                }

                MessageBox.Show(message, "刷新完成", MessageBoxButton.OK, icon);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新数据异常: {ex.Message}");
                MessageBox.Show($"刷新数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            System.Diagnostics.Debug.WriteLine($"UpdateCharts 被调用，数据源包含 {_stockHistoryData.Count} 只股票");

            if (_stockHistoryData == null || _stockHistoryData.Count == 0)
            {
                ShowNoData();
                return;
            }

            HideNoData();
            UpdateLineChart();
            UpdateBarChart();

            System.Diagnostics.Debug.WriteLine($"UpdateCharts 完成");
        }

        private void UpdateLineChart()
        {
            if (LineChart == null) return;

            try
            {
                // 创建新的SeriesCollection
                var newSeries = new SeriesCollection();

                if (_stockHistoryData.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("折线图：没有数据可显示");
                    LineChart.Series = newSeries;
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

                    newSeries.Add(lineSeries);
                    colorIndex++;
                }

                // 设置新的Series（这会触发图表重绘）
                LineChart.Series = newSeries;

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
                // 创建新的SeriesCollection
                var newSeries = new SeriesCollection();

                if (_stockHistoryData.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("柱状图：没有数据可显示");
                    BarChart.Series = newSeries;
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
                    BarChart.Series = newSeries;
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
                var allValues = new ChartValues<double>();
                foreach (var (name, volume) in stockDataList)
                {
                    allValues.Add(volume);
                }

                // 创建柱状图
                var columnSeries = new ColumnSeries
                {
                    Title = "成交量",
                    Values = allValues,
                    MaxColumnWidth = 60,
                    ColumnPadding = 5,
                    DataLabels = false,
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

                newSeries.Add(columnSeries);

                // 设置新的Series（这会触发图表重绘）
                BarChart.Series = newSeries;

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
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (LoadingOverlay != null)
                {
                    LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                }
            });
        }

        private void ShowNoData()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (txtNoData != null)
                    txtNoData.Visibility = Visibility.Visible;
                if (LineChart != null)
                    LineChart.Visibility = Visibility.Collapsed;
                if (BarChart != null)
                    BarChart.Visibility = Visibility.Collapsed;
            });
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

        /// <summary>
        /// 刷新按钮点击事件 - 检查并获取数据库中缺失的股票最新数据，保存到数据库后刷新显示
        /// </summary>
        private async void BtnRefreshChart_Click(object sender, RoutedEventArgs e)
        {
            // 禁用刷新按钮，防止重复点击
            if (btnRefreshChart != null)
            {
                btnRefreshChart.IsEnabled = false;
            }

            try
            {
                // 调用新的刷新方法，会检查数据库中缺失的数据并从API获取
                await RefreshAndFetchMissingDataAsync();
            }
            finally
            {
                // 重新启用刷新按钮
                if (btnRefreshChart != null)
                {
                    btnRefreshChart.IsEnabled = true;
                }
            }
        }

        #region 数据导出功能

        private void BtnExportData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 设置 EPPlus 许可证（非商业/个人使用）
                ExcelPackage.License.SetNonCommercialPersonal("StockAnalysisSystem");

                // 获取当前排行榜数据
                var exportData = PrepareExportData();

                if (exportData.Count == 0)
                {
                    MessageBox.Show("没有数据可导出", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 创建保存文件对话框
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel文件 (*.xlsx)|*.xlsx|CSV文件 (*.csv)|*.csv",
                    DefaultExt = ".xlsx",
                    FileName = $"股票数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    string extension = System.IO.Path.GetExtension(filePath).ToLower();

                    if (extension == ".xlsx")
                    {
                        ExportToExcel(exportData, filePath);
                    }
                    else if (extension == ".csv")
                    {
                        ExportToCsv(exportData, filePath);
                    }

                    MessageBox.Show($"数据已导出到: {filePath}", "导出成功",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<ExportStockItem> PrepareExportData()
        {
            var exportData = new List<ExportStockItem>();

            try
            {
                // 获取涨跌幅榜数据
                var allStocks = _favorites?
                    .Where(s => s != null)
                    .OrderByDescending(s => s.ChangePercent)
                    .ToList();

                if (allStocks == null) return exportData;

                int rank = 1;
                foreach (var stock in allStocks)
                {
                    // 获取最新价格数据
                    double latestPrice = GetLatestPrice(stock.Code);
                    double changeAmount = latestPrice * stock.ChangePercent / 100;

                    // 获取成交量
                    double volume = GetLatestVolume(stock.Code);

                    var exportItem = new ExportStockItem
                    {
                        Rank = rank++,
                        Symbol = stock.Code,
                        CompanyName = stock.Name,
                        Price = latestPrice,
                        ChangeAmount = changeAmount,
                        ChangePercentage = stock.ChangePercent,
                        Volume = (long)volume,
                        MarketCap = 0, // 如果没有市值数据可以设为0
                        LastUpdated = DateTime.Now
                    };

                    exportData.Add(exportItem);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"准备导出数据异常: {ex.Message}");
            }

            return exportData;
        }

        private double GetLatestPrice(string symbol)
        {
            try
            {
                if (_stockHistoryData.ContainsKey(symbol) && _stockHistoryData[symbol].Count > 0)
                {
                    var latest = _stockHistoryData[symbol]
                        .OrderByDescending(h => h.Date)
                        .FirstOrDefault();
                    return latest?.Close ?? 0;
                }
            }
            catch { }
            return 0;
        }

        private double GetLatestVolume(string symbol)
        {
            try
            {
                if (_stockHistoryData.ContainsKey(symbol) && _stockHistoryData[symbol].Count > 0)
                {
                    var latest = _stockHistoryData[symbol]
                        .OrderByDescending(h => h.Date)
                        .FirstOrDefault();
                    return latest?.Volume ?? 0;
                }
            }
            catch { }
            return 0;
        }

        private void ExportToExcel(List<ExportStockItem> data, string filePath)
        {
            try
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("股票数据");

                    // 设置标题行
                    string[] headers = { "排名", "股票代码", "公司名称", "价格(美元)", "涨跌额(美元)", "涨跌幅(%)", "成交量", "市值(美元)", "交易时间" };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    }

                    // 填充数据
                    for (int i = 0; i < data.Count; i++)
                    {
                        var item = data[i];
                        int row = i + 2;

                        worksheet.Cells[row, 1].Value = item.Rank;
                        worksheet.Cells[row, 2].Value = item.Symbol;
                        worksheet.Cells[row, 3].Value = item.CompanyName;
                        worksheet.Cells[row, 4].Value = item.Price;
                        worksheet.Cells[row, 4].Style.Numberformat.Format = "$#,##0.00";

                        worksheet.Cells[row, 5].Value = item.ChangeAmount;
                        worksheet.Cells[row, 5].Style.Numberformat.Format = "$#,##0.00";

                        worksheet.Cells[row, 6].Value = item.ChangePercentage / 100; // Excel百分比格式
                        worksheet.Cells[row, 6].Style.Numberformat.Format = "0.00%";

                        worksheet.Cells[row, 7].Value = item.Volume;
                        worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0";

                        worksheet.Cells[row, 8].Value = item.MarketCap;
                        worksheet.Cells[row, 8].Style.Numberformat.Format = "$#,##0";

                        worksheet.Cells[row, 9].Value = item.LastUpdated;
                        worksheet.Cells[row, 9].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                    }

                    // 自动调整列宽
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // 添加边框
                    var allCells = worksheet.Cells[1, 1, data.Count + 1, headers.Length];
                    allCells.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    allCells.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    allCells.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    allCells.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                    package.SaveAs(new System.IO.FileInfo(filePath));
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"导出Excel失败: {ex.Message}");
            }
        }

        private void ExportToCsv(List<ExportStockItem> data, string filePath)
        {
            try
            {
                using (var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    // 写入标题行
                    writer.WriteLine("排名,股票代码,公司名称,价格(美元),涨跌额(美元),涨跌幅(%),成交量,市值(美元),交易时间");

                    // 写入数据行
                    foreach (var item in data)
                    {
                        var line = string.Format("{0},{1},{2},{3:F2},{4:F2},{5:F2}%,{6},{7:F2},{8:yyyy-MM-dd HH:mm:ss}",
                            item.Rank,
                            item.Symbol,
                            item.CompanyName?.Replace(",", " ") ?? "", // 防止逗号干扰
                            item.Price,
                            item.ChangeAmount,
                            item.ChangePercentage,
                            item.Volume,
                            item.MarketCap,
                            item.LastUpdated);

                        writer.WriteLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"导出CSV失败: {ex.Message}");
            }
        }

        // 导出数据模型类
        public class ExportStockItem
        {
            public int Rank { get; set; }
            public string Symbol { get; set; }
            public string CompanyName { get; set; }
            public double Price { get; set; }
            public double ChangeAmount { get; set; }
            public double ChangePercentage { get; set; }
            public long Volume { get; set; }
            public double MarketCap { get; set; }
            public DateTime LastUpdated { get; set; }
        }

        #endregion

        #endregion
    }
}