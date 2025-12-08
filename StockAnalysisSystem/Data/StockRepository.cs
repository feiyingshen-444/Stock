using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using StockAnalysisSystem.Models;

namespace StockAnalysisSystem.Data
{
    public class StockRepository
    {
        private readonly string _connectionString;

        public StockRepository()
        {
            // 从配置文件读取连接字符串，这里使用默认值
            _connectionString = "Server=localhost;Database=StockAnalysisDB;User Id=sa;Password=336699;TrustServerCertificate=True;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // 创建数据库（如果不存在）
                string createDbSql = @"
                    IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'StockAnalysisDB')
                    BEGIN
                        CREATE DATABASE StockAnalysisDB;
                    END;
                ";

                // 切换到目标数据库
                var masterConnection = new SqlConnection("Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True;");
                masterConnection.Open();
                using (var cmd = new SqlCommand(createDbSql, masterConnection))
                {
                    cmd.ExecuteNonQuery();
                }
                masterConnection.Close();

                connection.ChangeDatabase("StockAnalysisDB");

                // 创建表
                string createFavoritesTable = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Favorites]') AND type in (N'U'))
                    CREATE TABLE [dbo].[Favorites] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [StockCode] NVARCHAR(50) NOT NULL,
                        [StockName] NVARCHAR(200) NOT NULL,
                        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
                        UNIQUE([StockCode])
                    );
                ";

                string createRecentStocksTable = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RecentStocks]') AND type in (N'U'))
                    CREATE TABLE [dbo].[RecentStocks] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [StockCode] NVARCHAR(50) NOT NULL,
                        [StockName] NVARCHAR(200) NOT NULL,
                        [QueryDate] DATETIME NOT NULL DEFAULT GETDATE()
                    );
                ";

                string createStockDataTable = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[StockData]') AND type in (N'U'))
                    CREATE TABLE [dbo].[StockData] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [StockCode] NVARCHAR(50) NOT NULL,
                        [StockName] NVARCHAR(200) NOT NULL,
                        [Price] DECIMAL(18,2) NOT NULL,
                        [ChangePercent] DECIMAL(18,2) NOT NULL,
                        [Volume] BIGINT NOT NULL,
                        [OpenPrice] DECIMAL(18,2) NOT NULL,
                        [HighPrice] DECIMAL(18,2) NOT NULL,
                        [LowPrice] DECIMAL(18,2) NOT NULL,
                        [ClosePrice] DECIMAL(18,2) NOT NULL,
                        [UpdateTime] DATETIME NOT NULL DEFAULT GETDATE()
                    );
                ";

                using (var cmd = new SqlCommand(createFavoritesTable, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand(createRecentStocksTable, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand(createStockDataTable, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据库初始化失败: {ex.Message}");
                // 如果数据库连接失败，应用程序仍然可以运行（使用内存存储）
            }
        }

        public void SaveFavoriteStock(string code, string name)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                connection.ChangeDatabase("StockAnalysisDB");

                string sql = @"
                    IF EXISTS (SELECT * FROM Favorites WHERE StockCode = @Code)
                        UPDATE Favorites SET StockName = @Name, CreatedDate = GETDATE() WHERE StockCode = @Code
                    ELSE
                        INSERT INTO Favorites (StockCode, StockName) VALUES (@Code, @Name);
                ";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Code", code);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存收藏股票失败: {ex.Message}");
            }
        }

        public void SaveRecentStock(string code, string name)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                connection.ChangeDatabase("StockAnalysisDB");

                string sql = @"
                    INSERT INTO RecentStocks (StockCode, StockName) VALUES (@Code, @Name);
                    
                    -- 保持最近20条记录
                    DELETE FROM RecentStocks 
                    WHERE Id NOT IN (
                        SELECT TOP 20 Id FROM RecentStocks ORDER BY QueryDate DESC
                    );
                ";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Code", code);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存最近查询失败: {ex.Message}");
            }
        }

        public List<StockItem> GetFavoriteStocks()
        {
            var favorites = new List<StockItem>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                connection.ChangeDatabase("StockAnalysisDB");

                string sql = "SELECT StockCode, StockName FROM Favorites ORDER BY CreatedDate DESC";

                using var cmd = new SqlCommand(sql, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    favorites.Add(new StockItem
                    {
                        Code = reader["StockCode"].ToString() ?? "",
                        Name = reader["StockName"].ToString() ?? "",
                        DisplayName = $"{reader["StockCode"]} - {reader["StockName"]}"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取收藏列表失败: {ex.Message}");
            }

            return favorites;
        }

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
    }
}
