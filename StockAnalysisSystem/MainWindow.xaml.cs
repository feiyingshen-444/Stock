using LiveCharts;
using LiveCharts.Wpf;
using OfficeOpenXml;
using StockAnalysisSystem.Data;
using StockAnalysisSystem.Models;
using StockAnalysisSystem.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;




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
                if (_isLoggedIn == false)
                {
                    MessageBox.Show("请先登入！");
                    return;
                }
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
            if (_isLoggedIn == false)
            {
                LoginWindow loginWindow = new LoginWindow();



                // 设置窗口关闭后的回调
                bool? result = loginWindow.ShowDialog();  // 阻塞直到窗口关闭

                if (result == true && loginWindow._isLoggedIn)
                {


                    Application.Current.Properties["CurrentUser"] = loginWindow.LoginUser;

                    Application.Current.Properties["CurrentLoggedIn"] = loginWindow._isLoggedIn;
                    _isLoggedIn = loginWindow._isLoggedIn;
                    LoginUser = loginWindow.LoginUser;
                    btnRegister.Content = "Hi, " + Application.Current.Properties["CurrentUser"];
                    LoadFavorites();
                }
            }
            else
            {
                MessageBoxResult result = MessageBox.Show(
            "确定要退出登录吗？",           // 消息内容
            "退出确认",                      // 标题
            MessageBoxButton.YesNo,          // 按钮类型
            MessageBoxImage.Question);       // 图标类型

                if (result == MessageBoxResult.Yes)
                {
                    // 清除登录状态
                    _isLoggedIn = false;
                    LoginUser = null;

                    // 清除应用程序属性
                    Application.Current.Properties["CurrentUser"] = null;
                    Application.Current.Properties["CurrentLoggedIn"] = false;

                    // 恢复按钮文本
                    btnRegister.Content = "登录/注册";

                    
                   _favorites.Clear();
                    LoadFavorites();

                    MessageBox.Show("已成功退出登录！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
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

        // 🔴 在这里添加右键菜单的事件处理方法

        private void MenuRemoveFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (lstFavorites.SelectedItem is StockItem selectedStock)
            {
                // 确认对话框
                var result = MessageBox.Show(
                    $"确定要取消收藏 {selectedStock.Name}({selectedStock.Code}) 吗？",
                    "确认取消收藏",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // 调用 Repository 删除收藏
                    bool success = _repository.RemoveFavoriteStock(
                        LoginUser,  // 当前登录用户ID
                        selectedStock.Code);

                    if (success)
                    {
                        // 从界面列表中移除
                        _favorites.Remove(selectedStock);
                        MessageBox.Show("已取消收藏", "成功",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("取消收藏失败", "错误",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("请先选择要取消收藏的股票", "提示");
            }
        }

        

       

        // 最近查询的右键菜单方法（如果需要）
        private void MenuRemoveRecent_Click(object sender, RoutedEventArgs e)
        {
            if (lstRecent.SelectedItem is StockItem selectedStock)
            {
                _recentStocks.Remove(selectedStock);
                MessageBox.Show("已从最近查询中移除", "成功");
            }
        }

        private void MenuAddToFavorites_Click(object sender, RoutedEventArgs e)
        {
            if (lstRecent.SelectedItem is StockItem selectedStock)
            {
                if (_favorites.Any(f => f.Code == selectedStock.Code))
                {
                    MessageBox.Show("该股票已在收藏列表中", "提示");
                    return;
                }

                bool addflag = _repository.InsertFavoriteStock(
                    LoginUser, selectedStock.Name, selectedStock.Code);

                if (addflag)
                {
                    _favorites.Add(selectedStock);
                    MessageBox.Show("收藏成功", "提示");
                }
            }
        }


        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 设置 EPPlus 许可证（非商业/个人使用）
                ExcelPackage.License.SetNonCommercialPersonal("StockAnalysisSystem");

                // 检查是否有当前股票数据
                if (_currentStock == null || string.IsNullOrEmpty(_currentStock.Code))
                {
                    MessageBox.Show("请先查询股票数据", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 创建保存文件对话框
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel文件 (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    FileName = $"{_currentStock.Code}_{_currentStock.Name}_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportStockDataToExcel(_currentStock, saveFileDialog.FileName);
                    MessageBox.Show($"数据已成功导出到:\n{saveFileDialog.FileName}", 
                                  "导出成功", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private void ExportStockDataToExcel(StockData stockData, string filePath)
        {
            using var package = new ExcelPackage();
            
            // 创建股票基本信息工作表
            var infoSheet = package.Workbook.Worksheets.Add("股票信息");
            infoSheet.Cells[1, 1].Value = "股票代码";
            infoSheet.Cells[1, 2].Value = stockData.Code;
            infoSheet.Cells[2, 1].Value = "股票名称";
            infoSheet.Cells[2, 2].Value = stockData.Name;
            infoSheet.Cells[3, 1].Value = "当前价格";
            infoSheet.Cells[3, 2].Value = stockData.CurrentPrice;
            infoSheet.Cells[3, 2].Style.Numberformat.Format = "$#,##0.00";
            infoSheet.Cells[4, 1].Value = "涨跌幅";
            infoSheet.Cells[4, 2].Value = stockData.ChangePercent / 100;
            infoSheet.Cells[4, 2].Style.Numberformat.Format = "0.00%";
            infoSheet.Cells[5, 1].Value = "成交量";
            infoSheet.Cells[5, 2].Value = stockData.Volume;
            infoSheet.Cells[5, 2].Style.Numberformat.Format = "#,##0";
            infoSheet.Cells[6, 1].Value = "开盘价";
            infoSheet.Cells[6, 2].Value = stockData.Open;
            infoSheet.Cells[6, 2].Style.Numberformat.Format = "$#,##0.00";
            infoSheet.Cells[7, 1].Value = "最高价";
            infoSheet.Cells[7, 2].Value = stockData.High;
            infoSheet.Cells[7, 2].Style.Numberformat.Format = "$#,##0.00";
            infoSheet.Cells[8, 1].Value = "最低价";
            infoSheet.Cells[8, 2].Value = stockData.Low;
            infoSheet.Cells[8, 2].Style.Numberformat.Format = "$#,##0.00";
            infoSheet.Cells[9, 1].Value = "收盘价";
            infoSheet.Cells[9, 2].Value = stockData.Close;
            infoSheet.Cells[9, 2].Style.Numberformat.Format = "$#,##0.00";
            infoSheet.Cells[10, 1].Value = "最新日期";
            infoSheet.Cells[10, 2].Value = stockData.NewDate;
            infoSheet.Cells[10, 2].Style.Numberformat.Format = "yyyy-mm-dd";
            infoSheet.Cells[11, 1].Value = "更新时间";
            infoSheet.Cells[11, 2].Value = stockData.UpdateTime;
            infoSheet.Cells[11, 2].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";

            // 设置标题行样式
            var titleRange = infoSheet.Cells[1, 1, 11, 1];
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            titleRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(219, 234, 252));

            // 自动调整列宽
            infoSheet.Cells[infoSheet.Dimension.Address].AutoFitColumns();

            // 创建历史数据工作表
            if (stockData.HistoricalData != null && stockData.HistoricalData.Count > 0)
            {
                var historySheet = package.Workbook.Worksheets.Add("历史数据");
                
                // 设置标题行
                string[] headers = { "日期", "开盘价", "最高价", "最低价", "收盘价", "成交量" };
                for (int i = 0; i < headers.Length; i++)
                {
                    historySheet.Cells[1, i + 1].Value = headers[i];
                    historySheet.Cells[1, i + 1].Style.Font.Bold = true;
                    historySheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    historySheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(33, 150, 243));
                    historySheet.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                }

                // 填充历史数据（按日期升序排列）
                var sortedData = stockData.HistoricalData.OrderBy(h => h.Date).ToList();
                for (int i = 0; i < sortedData.Count; i++)
                {
                    var data = sortedData[i];
                    int row = i + 2;

                    historySheet.Cells[row, 1].Value = data.Date;
                    historySheet.Cells[row, 1].Style.Numberformat.Format = "yyyy-mm-dd";
                    historySheet.Cells[row, 2].Value = data.Open;
                    historySheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
                    historySheet.Cells[row, 3].Value = data.High;
                    historySheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";
                    historySheet.Cells[row, 4].Value = data.Low;
                    historySheet.Cells[row, 4].Style.Numberformat.Format = "$#,##0.00";
                    historySheet.Cells[row, 5].Value = data.Close;
                    historySheet.Cells[row, 5].Style.Numberformat.Format = "$#,##0.00";
                    historySheet.Cells[row, 6].Value = data.Volume;
                    historySheet.Cells[row, 6].Style.Numberformat.Format = "#,##0";
                }

                // 添加边框
                var allCells = historySheet.Cells[1, 1, sortedData.Count + 1, headers.Length];
                allCells.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                allCells.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                allCells.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                allCells.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                allCells.Style.Border.Top.Color.SetColor(System.Drawing.Color.FromArgb(200, 200, 200));
                allCells.Style.Border.Bottom.Color.SetColor(System.Drawing.Color.FromArgb(200, 200, 200));
                allCells.Style.Border.Left.Color.SetColor(System.Drawing.Color.FromArgb(200, 200, 200));
                allCells.Style.Border.Right.Color.SetColor(System.Drawing.Color.FromArgb(200, 200, 200));

                // 自动调整列宽
                historySheet.Cells[historySheet.Dimension.Address].AutoFitColumns();

                // 冻结首行
                historySheet.View.FreezePanes(2, 1);
            }

            // 保存文件
            package.SaveAs(new FileInfo(filePath));
        }

    }
}
