using StockAnalysisSystem.Data;
using System.Windows;
using System.Windows.Controls;

namespace StockAnalysisSystem
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly StockRepository _repository;
        private static bool _isLoginMode = true; // true: 登录模式, false: 注册模式

        public bool _isLoggedIn { get; private set; }=false;
        public string LoginUser { get; private set; } = "";
        public LoginWindow()
        {
            InitializeComponent();
            _repository = new StockRepository();
            SetLoginMode(true);
        }

        /// <summary>
        /// 设置登录/注册模式
        /// </summary>
        private void SetLoginMode(bool isLogin)
        {
            _isLoginMode = isLogin;
            
            if (isLogin)
            {
                txtTitle.Text = "用户登录";
                btnSubmit.Content = "登录";
                txtSwitchHint.Text = "还没有账号？";
                btnSwitch.Content = "立即注册";
                spnConfirmPassword.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtTitle.Text = "用户注册";
                btnSubmit.Content = "注册";
                txtSwitchHint.Text = "已有账号？";
                btnSwitch.Content = "立即登录";
                spnConfirmPassword.Visibility = Visibility.Visible;
            }

            // 清空输入框
            txtUsername.Clear();
            txtPassword.Clear();
            txtConfirmPassword.Clear();
            txtStatus.Text = "就绪";
        }

        /// <summary>
        /// 切换登录/注册模式
        /// </summary>
        private void BtnSwitch_Click(object sender, RoutedEventArgs e)
        {
            SetLoginMode(!_isLoginMode);
        }

        /// <summary>
        /// 提交登录/注册
        /// </summary>
        //private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        //{
        //    // TODO: 在这里添加登录/注册的业务逻辑
        //    // 示例：
        //    // if (_isLoginMode)
        //    // {
        //    //     // 执行登录逻辑
        //    // }
        //    // else
        //    // {
        //    //     // 执行注册逻辑
        //    // }

        //    MessageBox.Show(_isLoginMode ? "登录功能待实现" : "注册功能待实现", 
        //                  "提示", 
        //                  MessageBoxButton.OK, 
        //                  MessageBoxImage.Information);
        //}
        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();
            string confirm = txtConfirmPassword.Password.Trim();

            // 基础校验
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                txtStatus.Text = "用户名或密码不能为空";
                return;
            }

            // -------------------
            // 登录模式
            // -------------------
            if (_isLoginMode)
            {
                bool loginSuccess = _repository.SelectedUser(username, password);

                if (loginSuccess)
                {
                    txtStatus.Text = "登录成功！";
                    _isLoggedIn = true;
                    MessageBox.Show("欢迎回来：" + username, "登录成功", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 登录成功后可以打开主窗口
                    // MainWindow mw = new MainWindow();
                    // mw.Show();
                    LoginUser = username;  // 保存登录用户名

                    this.DialogResult = true; // 通知主窗口：登录成功
                   // this.Close();
                    
                }
                else
                {
                    txtStatus.Text = "登录失败：用户名或密码错误";
                    MessageBox.Show("用户名或密码错误！", "登录失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            // -------------------
            // 注册模式
            // -------------------
            else
            {
                // 检查两次输入密码是否一致
                if (password != confirm)
                {
                    txtStatus.Text = "两次密码不一致";
                    MessageBox.Show("两次密码不一致，请重新确认！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 调用插入函数
                bool registerSuccess = _repository.InsertUser(username, password);

                if (registerSuccess)
                {
                    txtStatus.Text = "注册成功！请登录";
                    MessageBox.Show("账号创建成功！请使用新账号登录。", "注册成功", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 注册成功后切回登录模式
                    SetLoginMode(true);
                }
                else
                {
                    txtStatus.Text = "注册失败：可能是该用户名已存在";
                    MessageBox.Show("该用户名已存在或数据库写入失败！", "注册失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

    }
}

