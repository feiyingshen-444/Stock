# 股票分析系统 (Stock Analysis System)

## 项目简介

这是一个基于C#和WPF开发的桌面股票分析应用程序，提供了股票查询、数据可视化和数据存储等功能。

## 功能特性

- ✅ **股票查询**: 支持通过股票代码查询实时股票信息
- ✅ **数据可视化**: 使用图表展示股票价格走势
- ✅ **收藏功能**: 可以收藏常用股票，方便快速访问
- ✅ **历史记录**: 自动记录最近查询的股票
- ✅ **数据库存储**: 使用SQL Server存储股票数据和用户偏好
- ✅ **API集成**: 支持接入多种股票API接口

## 技术栈

- **开发语言**: C# (.NET 6.0)
- **UI框架**: WPF (Windows Presentation Foundation)
- **数据库**: SQL Server
- **图表库**: LiveCharts.Wpf
- **HTTP客户端**: HttpClient
- **JSON处理**: Newtonsoft.Json

## 项目结构

```
StockAnalysisSystem/
├── Models/                 # 数据模型
│   └── StockData.cs       # 股票数据模型
├── Services/              # 业务服务层
│   └── StockApiService.cs # 股票API服务
├── Data/                  # 数据访问层
│   └── StockRepository.cs # 数据库仓库类
├── MainWindow.xaml        # 主窗口界面
├── MainWindow.xaml.cs     # 主窗口逻辑
├── App.xaml              # 应用程序定义
├── App.xaml.cs           # 应用程序启动逻辑
├── appsettings.json      # 配置文件
└── StockAnalysisSystem.csproj # 项目文件
```

## 环境要求

### 开发环境
- Windows 10/11
- .NET 6.0 SDK 或更高版本
- Visual Studio 2022 或 Visual Studio Code
- SQL Server 2019 或更高版本（或 SQL Server Express）

### 运行时环境
- Windows 10/11
- .NET 6.0 Desktop Runtime

## 安装步骤

### 1. 克隆或下载项目

```bash
git clone <repository-url>
cd StockAnalysisSystem
```

### 2. 安装依赖

项目使用的NuGet包已在`.csproj`文件中定义，Visual Studio会自动还原。如果使用命令行：

```bash
dotnet restore
```

### 3. 配置数据库

#### 方法一：使用SQL Server Express（推荐用于开发）

1. 下载并安装 [SQL Server Express](https://www.microsoft.com/zh-cn/sql-server/sql-server-downloads)
2. 安装时选择"默认实例"
3. 确保启用了"SQL Server身份验证"或使用Windows身份验证

#### 方法二：使用LocalDB

1. 安装Visual Studio时会自动安装LocalDB
2. 连接字符串会自动使用LocalDB

#### 修改连接字符串

编辑 `appsettings.json` 或直接在 `Data/StockRepository.cs` 中修改连接字符串：

```csharp
// Windows身份验证
Server=localhost;Database=StockAnalysisDB;Integrated Security=True;TrustServerCertificate=True;

// SQL Server身份验证
Server=localhost;Database=StockAnalysisDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

### 4. 配置API密钥

1. 注册股票API服务（推荐使用Alpha Vantage: https://www.alphavantage.co/）
2. 获取API密钥
3. 在 `Services/StockApiService.cs` 中修改 `_apiKey` 变量：

```csharp
_apiKey = "你的API密钥";
```

或者在 `appsettings.json` 中配置（需要添加配置读取功能）。

### 5. 构建项目

```bash
dotnet build
```

### 6. 运行项目

```bash
dotnet run
```

或在Visual Studio中按F5运行。

## 使用说明

### 查询股票

1. **输入股票代码**: 在顶部工具栏的文本框中输入股票代码（例如：AAPL, MSFT等）
2. **点击查询**: 点击"查询"按钮获取股票信息
3. **查看结果**: 
   - 基本信息会显示在右侧顶部（股票名称、当前价格、涨跌幅、成交量）
   - 价格走势图会显示在下方图表区域

### 收藏股票

1. 查询股票后，点击"添加到收藏"按钮
2. 收藏的股票会显示在左侧"收藏股票"列表中
3. 点击收藏列表中的股票可以快速查询

### 查看历史记录

- 最近查询的股票会自动记录在左侧"最近查询"列表中
- 点击列表中的股票可以快速查询

### 刷新数据

- 点击"刷新"按钮可以更新当前显示股票的最新数据

### 切换时间范围

- 使用顶部的时间范围下拉框选择不同的时间段（1天、1周、1月、3月、1年）
- 注意：当前版本的时间范围功能需要根据API支持进行调整

## 数据库结构

系统会自动创建以下数据表：

### Favorites（收藏表）
- `Id`: 主键
- `StockCode`: 股票代码
- `StockName`: 股票名称
- `CreatedDate`: 创建时间

### RecentStocks（最近查询表）
- `Id`: 主键
- `StockCode`: 股票代码
- `StockName`: 股票名称
- `QueryDate`: 查询时间

### StockData（股票数据表）
- `Id`: 主键
- `StockCode`: 股票代码
- `StockName`: 股票名称
- `Price`: 当前价格
- `ChangePercent`: 涨跌幅
- `Volume`: 成交量
- `OpenPrice`: 开盘价
- `HighPrice`: 最高价
- `LowPrice`: 最低价
- `ClosePrice`: 收盘价
- `UpdateTime`: 更新时间

## API集成说明

### 当前实现

项目目前使用模拟数据（`GetMockStockDataAsync`方法）。要使用真实API，需要：

1. 取消注释 `GetRealStockDataAsync` 方法的调用
2. 替换为实际的API端点
3. 根据API响应格式调整JSON解析逻辑

### 推荐API

1. **Alpha Vantage** (https://www.alphavantage.co/)
   - 免费层级：每分钟5次请求，每天500次
   - 支持全球股票市场

2. **Yahoo Finance API** (非官方)
   - 免费但可能有使用限制

3. **中国股票API**
   - 聚合数据 (https://www.juhe.cn/)
   - 新浪财经API

### API集成示例

```csharp
// 在 StockApiService.cs 中
public async Task<StockData?> GetStockDataAsync(string stockCode)
{
    // 使用真实API
    var stockData = await GetRealStockDataAsync(stockCode);
    
    if (stockData == null)
    {
        // 如果API调用失败，使用模拟数据
        return await GetMockStockDataAsync(stockCode);
    }
    
    return stockData;
}
```

## 常见问题

### 1. 数据库连接失败

**问题**: 应用程序启动时提示数据库连接错误

**解决方案**:
- 检查SQL Server服务是否运行
- 验证连接字符串是否正确
- 确保数据库服务允许TCP/IP连接
- 如果是首次运行，应用程序会尝试自动创建数据库

### 2. API调用失败

**问题**: 查询股票时提示API错误

**解决方案**:
- 检查网络连接
- 验证API密钥是否正确
- 检查API服务是否可用
- 如果API不可用，系统会使用模拟数据

### 3. 图表不显示

**问题**: 查询股票后图表区域为空

**解决方案**:
- 检查LiveCharts.Wpf包是否正确安装
- 确保股票数据包含历史数据
- 查看控制台是否有错误信息

### 4. 收藏列表为空

**问题**: 重启应用后收藏列表清空

**解决方案**:
- 检查数据库连接是否正常
- 查看数据库表是否正确创建
- 检查 `StockRepository.cs` 中的连接字符串

## 开发指南

### 添加新功能

1. **扩展数据模型**: 在 `Models/` 目录添加新的模型类
2. **添加服务**: 在 `Services/` 目录添加新的服务类
3. **更新数据库**: 在 `Data/StockRepository.cs` 中添加新的数据库操作方法
4. **更新UI**: 在 `MainWindow.xaml` 中添加新的界面元素

### 代码结构说明

- **Models**: 包含所有数据模型，定义数据结构
- **Services**: 包含业务逻辑，如API调用
- **Data**: 包含数据访问层，处理数据库操作
- **MainWindow**: 主窗口的UI和逻辑代码

## 许可证

本项目仅供学习和参考使用。

## 更新日志

### v1.0.0 (2024)
- 初始版本发布
- 实现基本的股票查询功能
- 实现数据可视化
- 实现收藏和历史记录功能
- 集成SQL Server数据库

## 贡献

欢迎提交Issue和Pull Request来改进这个项目。

## 联系方式

如有问题或建议，请通过Issue反馈。

---

**注意**: 本项目使用的股票数据仅供演示和学习用途。实际投资决策请使用官方和专业的金融数据服务。


