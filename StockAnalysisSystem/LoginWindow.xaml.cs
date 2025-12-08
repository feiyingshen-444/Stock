using System.Windows;
using System.Windows.Controls;

namespace StockAnalysisSystem
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        private bool _isLoginMode = true; // true: 登录模式, false: 注册模式

        public LoginWindow()
        {
            InitializeComponent();
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
        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 在这里添加登录/注册的业务逻辑
            // 示例：
            // if (_isLoginMode)
            // {
            //     // 执行登录逻辑
            // }
            // else
            // {
            //     // 执行注册逻辑
            // }
            
            MessageBox.Show(_isLoginMode ? "登录功能待实现" : "注册功能待实现", 
                          "提示", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Information);
        }
    }
}

