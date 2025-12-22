using LiveCharts;
using StockAnalysisSystem.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows;

namespace StockAnalysisSystem.Data
{
    public class StockRepository
    {
        private readonly string _connectionString =
 @"Data Source=(LocalDB)\MSSQLLocalDB;" +
 @"AttachDbFilename=C:\Users\zlh03\Documents\StockAnalysisDB.mdf;" +
 @"Integrated Security=True;" +
 @"Connect Timeout=30;";
        private SqlConnection sqlCon;

        public StockRepository()
        {
            sqlCon = new SqlConnection(_connectionString);
            TestConnection();
        }

        public bool TestConnection()
        {
            try
            {
                sqlCon.Open();
                return true;
            }
            catch (SqlException ex)
            {
                MessageBox.Show("内部错误，数据库连接失败", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("内部错误，数据库连接失败", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
        }

        #region 用户相关

        public bool SelectedUser(String username, String password)
        {
            string sql = "select * from Users where username=@Name and password=@Password";
            try
            {
                using (SqlCommand command = new SqlCommand(sql, sqlCon))
                {
                    command.Parameters.AddWithValue("@Name", username);
                    command.Parameters.AddWithValue("@Password", password);
                    object scalar = command.ExecuteScalar();
                    int count = 0;
                    if (scalar != null && int.TryParse(scalar.ToString(), out count))
                    {
                        return count > 0;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"登入失败：{ex.Message}");
                return false;
            }
        }

        public bool InsertUser(string username, string password)
        {
            string sql = "INSERT INTO Users (username, password) VALUES (@Name, @Password)";
            try
            {
                using (SqlCommand command = new SqlCommand(sql, sqlCon))
                {
                    command.Parameters.AddWithValue("@Name", username);
                    command.Parameters.AddWithValue("@Password", password);
                    int result = command.ExecuteNonQuery();
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"注册失败：{ex.Message}");
                return false;
            }
        }

        #endregion

        #region 收藏股票相关

        public bool InsertFavoriteStock(string username, string stockname, string stockcode)
        {
            string sql = "INSERT INTO FavoriteStock (favoritestockname, username, favoritestockcode) VALUES (@Stockname, @Name, @Stockcode)";
            try
            {
                using (SqlCommand command = new SqlCommand(sql, sqlCon))
                {
                    command.Parameters.AddWithValue("@Name", username);
                    command.Parameters.AddWithValue("@Stockname", stockname);
                    command.Parameters.AddWithValue("@Stockcode", stockcode);
                    int result = command.ExecuteNonQuery();
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"收藏失败：{ex.Message}");
                return false;
            }
        }

        public bool RemoveFavoriteStock(string username, string stockcode)
        {
            string sql = "DELETE FROM FavoriteStock WHERE username = @Name AND favoritestockcode = @Stockcode";
            try
            {
                using (SqlCommand command = new SqlCommand(sql, sqlCon))
                {
                    command.Parameters.AddWithValue("@Name", username);
                    command.Parameters.AddWithValue("@Stockcode", stockcode);
                    int result = command.ExecuteNonQuery();
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"取消收藏失败：{ex.Message}");
                return false;
            }
        }

        public List<StockItem> GetFavoriteStocks(String username)
        {
            var favorites = new List<StockItem>();
            try
            {
                string sql = "SELECT favoritestockname, favoritestockcode FROM FavoriteStock WHERE username=@Name";
                using (SqlCommand command = new SqlCommand(sql, sqlCon))
                {
                    command.Parameters.AddWithValue("@Name", username);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            favorites.Add(new StockItem
                            {
                                Name = reader["favoritestockname"].ToString() ?? "",
                                Code = reader["favoritestockcode"].ToString() ?? "",
                                DisplayName = $"{reader["favoritestockcode"]} - {reader["favoritestockname"]}"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取收藏列表失败: {ex.Message}");
            }
            return favorites;
        }

        #endregion

        #region 股票历史数据 - 新增方法

        /// <summary>
        /// 保存股票历史数据到数据库
        /// </summary>
        public bool SaveStockHistoryData(string stockCode, string stockName, List<HistoricalData> historyData)
        {
            if (historyData == null || historyData.Count == 0)
                return false;

            try
            {
                foreach (var data in historyData)
                {
                    // 先检查是否已存在
                    string checkSql = @"SELECT COUNT(*) FROM StockHistoryData 
                                        WHERE StockCode = @Code AND TradeDate = @TradeDate";

                    using (SqlCommand checkCmd = new SqlCommand(checkSql, sqlCon))
                    {
                        checkCmd.Parameters.AddWithValue("@Code", stockCode);
                        checkCmd.Parameters.AddWithValue("@TradeDate", data.Date.Date);

                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0)
                            continue; // 已存在，跳过
                    }

                    // 插入新数据
                    string insertSql = @"INSERT INTO StockHistoryData 
                                        (StockCode, StockName, TradeDate, OpenPrice, HighPrice, LowPrice, ClosePrice, Volume, CreateTime)
                                        VALUES (@Code, @Name, @TradeDate, @Open, @High, @Low, @Close, @Volume, @CreateTime)";

                    using (SqlCommand insertCmd = new SqlCommand(insertSql, sqlCon))
                    {
                        insertCmd.Parameters.AddWithValue("@Code", stockCode);
                        insertCmd.Parameters.AddWithValue("@Name", stockName ?? stockCode);
                        insertCmd.Parameters.AddWithValue("@TradeDate", data.Date.Date);
                        insertCmd.Parameters.AddWithValue("@Open", data.Open);
                        insertCmd.Parameters.AddWithValue("@High", data.High);
                        insertCmd.Parameters.AddWithValue("@Low", data.Low);
                        insertCmd.Parameters.AddWithValue("@Close", data.Close);
                        insertCmd.Parameters.AddWithValue("@Volume", data.Volume);
                        insertCmd.Parameters.AddWithValue("@CreateTime", DateTime.Now);

                        insertCmd.ExecuteNonQuery();
                    }
                }

                System.Diagnostics.Debug.WriteLine($"成功保存 {stockCode} 的 {historyData.Count} 条历史数据");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存股票历史数据失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从数据库获取股票历史数据
        /// </summary>
        public List<HistoricalData> GetStockHistoryData(string stockCode, int days = 30)
        {
            var history = new List<HistoricalData>();

            try
            {
                string sql = @"SELECT TOP (@Days) TradeDate, OpenPrice, HighPrice, LowPrice, ClosePrice, Volume
                              FROM StockHistoryData
                              WHERE StockCode = @Code
                              ORDER BY TradeDate DESC";

                using (SqlCommand cmd = new SqlCommand(sql, sqlCon))
                {
                    cmd.Parameters.AddWithValue("@Code", stockCode);
                    cmd.Parameters.AddWithValue("@Days", days);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            history.Add(new HistoricalData
                            {
                                Date = Convert.ToDateTime(reader["TradeDate"]),
                                Open = Convert.ToDouble(reader["OpenPrice"]),
                                High = Convert.ToDouble(reader["HighPrice"]),
                                Low = Convert.ToDouble(reader["LowPrice"]),
                                Close = Convert.ToDouble(reader["ClosePrice"]),
                                Volume = Convert.ToInt64(reader["Volume"])
                            });
                        }
                    }
                }

                // 按日期升序排列
                history = history.OrderBy(h => h.Date).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取股票历史数据失败: {ex.Message}");
            }

            return history;
        }

        /// <summary>
        /// 获取多只股票的历史数据
        /// </summary>
        public Dictionary<string, List<HistoricalData>> GetMultipleStockHistoryData(List<string> stockCodes, int days = 30)
        {
            var result = new Dictionary<string, List<HistoricalData>>();

            foreach (var code in stockCodes)
            {
                var history = GetStockHistoryData(code, days);
                if (history.Count > 0)
                {
                    result[code] = history;
                }
            }

            return result;
        }

        /// <summary>
        /// 清除过期的历史数据（保留最近N天）
        /// </summary>
        public int CleanOldHistoryData(int keepDays = 365)
        {
            try
            {
                string sql = @"DELETE FROM StockHistoryData 
                              WHERE TradeDate < @CutoffDate";

                using (SqlCommand cmd = new SqlCommand(sql, sqlCon))
                {
                    cmd.Parameters.AddWithValue("@CutoffDate", DateTime.Now.AddDays(-keepDays));
                    int deleted = cmd.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine($"清除了 {deleted} 条过期历史数据");
                    return deleted;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清除历史数据失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 更新收藏股票的最新价格和涨跌幅
        /// </summary>
        public bool UpdateFavoriteStockPrice(string username, string stockCode, double price, double changePercent)
        {
            try
            {
                string sql = @"UPDATE FavoriteStock 
                              SET CurrentPrice = @Price, ChangePercent = @ChangePercent, UpdateTime = @UpdateTime
                              WHERE username = @Name AND favoritestockcode = @Code";

                using (SqlCommand cmd = new SqlCommand(sql, sqlCon))
                {
                    cmd.Parameters.AddWithValue("@Name", username);
                    cmd.Parameters.AddWithValue("@Code", stockCode);
                    cmd.Parameters.AddWithValue("@Price", price);
                    cmd.Parameters.AddWithValue("@ChangePercent", changePercent);
                    cmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新收藏股票价格失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取收藏股票（包含价格和涨跌幅）
        /// </summary>
        public List<StockItem> GetFavoriteStocksWithPrice(string username)
        {
            var favorites = new List<StockItem>();
            try
            {
                string sql = @"SELECT favoritestockname, favoritestockcode, 
                              ISNULL(CurrentPrice, 0) as CurrentPrice, 
                              ISNULL(ChangePercent, 0) as ChangePercent,
                              UpdateTime
                              FROM FavoriteStock WHERE username=@Name";

                using (SqlCommand command = new SqlCommand(sql, sqlCon))
                {
                    command.Parameters.AddWithValue("@Name", username);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            favorites.Add(new StockItem
                            {
                                Name = reader["favoritestockname"].ToString() ?? "",
                                Code = reader["favoritestockcode"].ToString() ?? "",
                                DisplayName = $"{reader["favoritestockcode"]} - {reader["favoritestockname"]}",
                                CurrentPrice = Convert.ToDouble(reader["CurrentPrice"]),
                                ChangePercent = Convert.ToDouble(reader["ChangePercent"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取收藏列表失败: {ex.Message}");
                // 如果新字段不存在，回退到旧方法
                return GetFavoriteStocks(username);
            }
            return favorites;
        }

        #endregion

        #region 最近查询记录

        public List<StockItem> GetRecentStocks()
        {
            var recent = new List<StockItem>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                connection.ChangeDatabase("StockAnalysisDB");

                string sql = "SELECT TOP 20 StockCode, StockName FROM RecentStocks ORDER BY QueryDate DESC";

                using var cmd = new SqlCommand(sql, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    recent.Add(new StockItem
                    {
                        Code = reader["StockCode"].ToString() ?? "",
                        Name = reader["StockName"].ToString() ?? "",
                        DisplayName = $"{reader["StockCode"]} - {reader["StockName"]}"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取最近查询失败: {ex.Message}");
            }

            return recent;
        }

        #endregion

        #region 股票数据保存

        public void SaveStockData(StockData stockData)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                connection.ChangeDatabase("StockAnalysisDB");

                string sql = @"
                    INSERT INTO StockData (StockCode, StockName, Price, ChangePercent, Volume, 
                                          OpenPrice, HighPrice, LowPrice, ClosePrice, UpdateTime)
                    VALUES (@Code, @Name, @Price, @ChangePercent, @Volume, 
                            @Open, @High, @Low, @Close, @UpdateTime);
                ";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Code", stockData.Code);
                cmd.Parameters.AddWithValue("@Name", stockData.Name);
                cmd.Parameters.AddWithValue("@Price", stockData.CurrentPrice);
                cmd.Parameters.AddWithValue("@ChangePercent", stockData.ChangePercent);
                cmd.Parameters.AddWithValue("@Volume", stockData.Volume);
                cmd.Parameters.AddWithValue("@Open", stockData.Open);
                cmd.Parameters.AddWithValue("@High", stockData.High);
                cmd.Parameters.AddWithValue("@Low", stockData.Low);
                cmd.Parameters.AddWithValue("@Close", stockData.Close);
                cmd.Parameters.AddWithValue("@UpdateTime", stockData.UpdateTime);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存股票数据失败: {ex.Message}");
            }
        }

        public List<StockData> GetStockHistory(string code, int days = 30)
        {
            var history = new List<StockData>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                connection.ChangeDatabase("StockAnalysisDB");

                string sql = @"
                    SELECT TOP (@Days) StockCode, StockName, Price, ChangePercent, Volume,
                           OpenPrice, HighPrice, LowPrice, ClosePrice, UpdateTime
                    FROM StockData
                    WHERE StockCode = @Code
                    ORDER BY UpdateTime DESC
                ";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Code", code);
                cmd.Parameters.AddWithValue("@Days", days);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    history.Add(new StockData
                    {
                        Code = reader["StockCode"].ToString() ?? "",
                        Name = reader["StockName"].ToString() ?? "",
                        CurrentPrice = Convert.ToDouble(reader["Price"]),
                        ChangePercent = Convert.ToDouble(reader["ChangePercent"]),
                        Volume = Convert.ToInt64(reader["Volume"]),
                        Open = Convert.ToDouble(reader["OpenPrice"]),
                        High = Convert.ToDouble(reader["HighPrice"]),
                        Low = Convert.ToDouble(reader["LowPrice"]),
                        Close = Convert.ToDouble(reader["ClosePrice"]),
                        UpdateTime = Convert.ToDateTime(reader["UpdateTime"])
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取历史数据失败: {ex.Message}");
            }

            return history;
        }

        #endregion
    }
}
