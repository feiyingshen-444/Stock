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
        public bool _isLoggedIn { get; private set; } = false;
        public string LoginUser { get; private set; } = "";
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
            if (_isLoggedIn != true)
            {
                MessageBox.Show("请先登入", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            bool addflag = addflag = _repository.InsertFavoriteStock(LoginUser, _currentStock.Name,_currentStock.Code);
            _favorites.Add(item);
            if (addflag)
            {
                MessageBox.Show("收藏成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
           
        }

        // 找到这个方法并修改：
        private async void BtnVision_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. 从数据库获取收藏股票
                var favoriteStocks = _repository.GetFavoriteStocks(LoginUser);

                if (favoriteStocks == null || favoriteStocks.Count == 0)
                {
                    MessageBox.Show("您还没有收藏任何股票！");
                    return;
                }

                // 2. 创建API服务获取实时数据
                var apiService = new StockApiService();
                var updatedStocks = new List<StockItem>();

                // 进度提示
                MessageBox.Show($"正在获取 {favoriteStocks.Count} 只股票的实时数据...");

                foreach (var stock in favoriteStocks)
                {
                    try
                    {
                        // 关键：调用API获取真实涨跌幅
                        var realData = await apiService.GetDataAsync(stock.Code, 1);

                        if (realData != null && realData.ChangePercent != 0)
                        {
                            // 更新涨跌幅
                            stock.ChangePercent = realData.ChangePercent;
                        }
                        updatedStocks.Add(stock);

                        // 避免API限制
                        await Task.Delay(1500);
                    }
                    catch
                    {
                        updatedStocks.Add(stock);
                    }
                }

                // 3. 打开可视化窗口
                var win = new DataVisualizationWindow(updatedStocks);
                win.Owner = this;
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"错误: {ex.Message}");
            }
        }





        private void BtnRegister_Click(object sender, RoutedEventArgs e) {
            //  MessageBox.Show("测试", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            LoginWindow loginWindow = new LoginWindow();



            // 设置窗口关闭后的回调
            bool? result = loginWindow.ShowDialog();  // 阻塞直到窗口关闭

            if (result == true && loginWindow._isLoggedIn)
            {

                
                Application.Current.Properties["CurrentUser"] = loginWindow.LoginUser;

                Application.Current.Properties["CurrentLoggedIn"] = loginWindow._isLoggedIn;
                _isLoggedIn= loginWindow._isLoggedIn;
                LoginUser= loginWindow.LoginUser;
                btnRegister.Content = "Hi, " + Application.Current.Properties["CurrentUser"];
                LoadFavorites();
            }

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
                
            

                // ✅ 移除日期过滤，直接排序使用API返回的数据
                data.HistoricalData = data.HistoricalData?
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

              //  System.Diagnostics.Debug.WriteLine($"API返回数据点数: {data.HistoricalData?.Count ?? 0}");
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
            txtCurrentPrice.Text = $"${stock.CurrentPrice:F2}";
            txtChangePercent.Text = $"{stock.ChangePercent:F2}%";
            txtChangePercent.Foreground = stock.ChangePercent >= 0
                ? System.Windows.Media.Brushes.Red
                : System.Windows.Media.Brushes.Green;
            txtVolume.Text = stock.Volume.ToString("N0");
            txtNewDate.Text = $"{stock.NewDate.ToString("yyyy/MM/dd")}";
        }

        //private void UpdateChart(StockData stock, int days)
        //{
        //    _seriesCollection.Clear();
        //    if (stock.HistoricalData == null || stock.HistoricalData.Count == 0)
        //        return;

        //    var historical = stock.HistoricalData.OrderBy(h => h.Date).ToList(); // 确保有序

        //    var values = new ChartValues<double>(historical.Select(h => h.Close));
        //    _seriesCollection.Add(new LineSeries
        //    {
        //        Title = "价格",
        //        Values = values,
        //        PointGeometry = null,
        //        LineSmoothness = 0
        //    });

        //    // ✅ 生成与数据点等长的标签
        //    string[] labels = historical.Select(h => days <= 1 ? h.Date.ToString("MM/dd HH:mm") : h.Date.ToString("MM/dd")).ToArray();

        //    // ✅ 动态步长，最多显示12个标签
        //    int step = labels.Length <= 12 ? 1 : (int)Math.Ceiling((double)labels.Length / 12);

        //    var axis = new Axis
        //    {
        //        Labels = labels, // ⚠️ 关键：长度必须 == values.Count
        //        Separator = new LiveCharts.Wpf.Separator { Step = step },
        //        LabelsRotation = -45,
        //        FontSize = 11
        //    };

        //    PriceChart.AxisX.Clear();
        //    PriceChart.AxisX.Add(axis);
        //}
        private void UpdateChart(StockData stock, int days)

        {
            _seriesCollection.Clear();  
            PriceChart.AxisX.Clear();  
            PriceChart.AxisY.Clear();
            if (days <= 1)
            {
                return;
            }
         
            //调试
            System.Diagnostics.Debug.WriteLine($"=== UpdateChart ===");
            System.Diagnostics.Debug.WriteLine($"days: {days}");
            System.Diagnostics.Debug.WriteLine($"HistoricalData is null: {stock.HistoricalData == null}");
            System.Diagnostics.Debug.WriteLine($"HistoricalData count: {stock.HistoricalData?.Count ?? 0}");



            if (stock.HistoricalData == null || stock.HistoricalData.Count == 0)
                return;

            var historical = stock.HistoricalData.OrderBy(h => h.Date).ToList();
            
            var values = new ChartValues<double>(historical.Select(h => h.Close));
            _seriesCollection.Add(new LineSeries
            {
                Title = "价格",
                Values = values,
                PointGeometry = null,  // ✅ 显示数据点
                PointGeometrySize = 8,
               
                LineSmoothness = 0,


                LabelPoint = point =>
                {
                    // ✅ 自定义 Tooltip 显示内容
                    int index = (int)point.X;
                    if (index >= 0 && index < historical.Count)
                    {
                        var data = historical[index];
                        return $"{data.Date:yyyy/MM/dd}\n价格: ¥{data.Close:F2}";
                    }
                    return point.Y.ToString("F2");
                }
            });
            // ✅ 使用 LabelFormatter 确保标签与数据点索引对应
            int count = historical.Count;
            int step = Math.Max(1, count / 15);  // 大约显示10个标签

            var axis = new Axis
            {
                LabelFormatter = value =>
                {
                    int index = (int)value;
                    if (index >= 0 && index < historical.Count)
                    {
                        return days <= 1
                            ? historical[index].Date.ToString("HH:mm")
                            : historical[index].Date.ToString("MM/dd");
                    }
                    return "";
                },
                Separator = new LiveCharts.Wpf.Separator { Step = step },
                LabelsRotation = -45,
                FontSize = 11,
                MinValue = 0,
                MaxValue = count - 1
            };

            PriceChart.AxisX.Clear();
            PriceChart.AxisX.Add(axis);
            //// ✅ 修复：创建稀疏标签数组，只在特定位置显示日期，其余位置为空
            //int maxLabels = 10;  // 最多显示10个标签
            //int count = historical.Count;
            //int step = count <= maxLabels ? 1 : (int)Math.Ceiling((double)count / maxLabels);

            //// 生成标签数组，大部分为空字符串，只在间隔位置显示日期
            //string[] labels = new string[count];
            //for (int i = 0; i < count; i++)
            //{
            //    if (i % step == 0 || i == count - 1)  // 间隔显示 + 最后一个点
            //    {
            //        labels[i] = days <= 1
            //            ? historical[i].Date.ToString("HH:mm")
            //            : historical[i].Date.ToString("MM/dd");
            //    }
            //    else
            //    {
            //        labels[i] = "";  // 空字符串，不显示
            //    }
            //}

            //var axis = new Axis
            //{
            //    Labels = labels,
            //    Separator = new LiveCharts.Wpf.Separator { Step = 1 },  // ✅ Step设为1，由标签数组控制显示
            //    LabelsRotation = -45,
            //    FontSize = 11
            //};

            //PriceChart.AxisX.Clear();
            //PriceChart.AxisX.Add(axis);
        }



        /*----------------  数据加载  ----------------*/
        //private void LoadFavorites()
        //{
        //    foreach (var f in _repository.GetFavoriteStocks(LoginUser))
        //    _favorites.Add(f);

        //}
        private void LoadFavorites()
        {
            // 先清空现有收藏列表
            _favorites.Clear();

            // 重新加载当前用户的收藏
            foreach (var f in _repository.GetFavoriteStocks(LoginUser))
            {
                _favorites.Add(f);
            }
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

        private void PriceChart_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}