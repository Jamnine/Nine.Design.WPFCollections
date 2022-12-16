using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WinInterop = System.Windows.Interop;


namespace Nine.Design.Client
{
    public partial class MainWindow : Window
    {
        #region 变量

        System.IntPtr handle;//当前窗体句柄，用于win32交互

        #region 贴边收缩计时器
        private bool blnTopHide = false;
        private bool IsMouseEnter = false;//窗体状态  true为显示 false为隐藏
        private System.Windows.Forms.Timer timer;//计时器  通过win32api实时获取鼠标位置
        #endregion

        #region 最大化、最小化、关闭、图片资源
        readonly BitmapImage Exit_Enter = new BitmapImage(new Uri("pack://application:,,,/Nine.Design.Resource;component/images/btn/close_696969.png", UriKind.RelativeOrAbsolute));
        readonly BitmapImage Exit_Leave = new BitmapImage(new Uri("pack://application:,,,/Nine.Design.Resource;component/images/btn/close_9ea0a2.png", UriKind.RelativeOrAbsolute));
        readonly BitmapImage Max_Enter = new BitmapImage(new Uri("pack://application:,,,/Nine.Design.Resource;component/images/btn/max_696969.png", UriKind.RelativeOrAbsolute));
        readonly BitmapImage Max_Leave = new BitmapImage(new Uri("pack://application:,,,/Nine.Design.Resource;component/images/btn/max_9ea0a2.png", UriKind.RelativeOrAbsolute));
        readonly BitmapImage Mini_Enter = new BitmapImage(new Uri("pack://application:,,,/Nine.Design.Resource;component/images/btn/mini_696969.png", UriKind.RelativeOrAbsolute));
        readonly BitmapImage Mini_Leave = new BitmapImage(new Uri("pack://application:,,,/Nine.Design.Resource;component/images/btn/mini_9ea0a2.png", UriKind.RelativeOrAbsolute));
        #endregion

        #endregion

        #region 构造方法
        public MainWindow()
        {
            InitializeComponent();

            //引發此事件以支援與 Win32 的交互操作
            //最大化不遮挡任务栏
            this.SourceInitialized += new EventHandler(CustomWindowsSourceInitialized);

            #region 贴边收缩计时器
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 300;
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Start();
            #endregion

            #region 关闭贴边放大，WindowStyle="None" 设置为none
            this.PreviewMouseLeftButtonDown += (sender, e) =>
            {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    this.ResizeMode = ResizeMode.NoResize;
                }
            };
            this.PreviewMouseLeftButtonUp += (sender, e) =>
            {
                if (this.ResizeMode == ResizeMode.NoResize)
                {
                    this.ResizeMode = ResizeMode.CanResize;
                }
            };
            #endregion

            #region 设置窗口屏幕居中置顶
            Top = 0;
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            #endregion
        }
        #endregion

        #region 最大化不遮挡任务栏
        public void CustomWindowsSourceInitialized(object sender, EventArgs e)
        {
            handle = (new WinInterop.WindowInteropHelper(this)).Handle;
            WinInterop.HwndSource.FromHwnd(handle).AddHook(new WinInterop.HwndSourceHook(Win32Interactive.WindowProc));
        }
        #endregion

        #region 加载事件
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //隐藏任务栏图标
            Win32Interactive.SetWindowLong(handle, -20, (IntPtr)0x8000000);
        }
        #endregion

        #region 关闭事件
        private void Window_Closed(object sender, EventArgs e)
        {
            Process[] myproc = Process.GetProcesses();
            foreach (Process item in myproc)
            {
                if (item.ProcessName == "Nine.Design.Client")
                {
                    item.Kill();
                }
            }
        }
        #endregion

        #region 窗口置顶
        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
            Console.WriteLine("工具条窗口置顶");
        }
        #endregion

        #region 鼠标点击拖动窗体
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
        #endregion

        #region 贴边收缩计时器实现
        private void Timer_Tick(object? sender, EventArgs e)
        {
            Point point = PointToScreen(Mouse.GetPosition(this));//获取鼠标相对桌面的位置
            if (point.X != 0 || point.Y != 0)
            {
                IsMouseEnter = true;    //鼠标在窗体内部
            }
            else if (point.X == 0 && point.Y == 0)
            {
                IsMouseEnter = false;   //鼠标离开窗体
            }
            if (this.Top <= 0)
            {
                if (blnTopHide)
                {
                    if (this.Top <= 0 && IsMouseEnter)
                    {
                        while (this.Top < -2)
                            this.Top++;
                        blnTopHide = false;
                    }
                }
                else
                {
                    if (this.Top <= 0 && !IsMouseEnter)
                    {
                        while (this.Top > -(this.Height - 12))
                            this.Top--;
                        blnTopHide = true;
                    }
                    return;
                }
            }
            else if (this.Left <= 0)
            {
                if (blnTopHide)
                {
                    Point b = Mouse.GetPosition(this);//获取鼠标相对窗口的位置
                    if (this.Left < 200 && IsMouseEnter)
                    {
                        while (this.Left < -2)
                            this.Left++;
                        blnTopHide = false;
                    }
                }
                else
                {
                    Point b = Mouse.GetPosition(this);//获取鼠标相对窗口的位置
                    if (this.Left < 5 && !IsMouseEnter)
                    {
                        while (this.Left > -(this.Width - 12))
                            this.Left--;
                        blnTopHide = true;
                    }
                    return;
                }
            }
            else if (this.Left + this.Width >= Win32Interactive.转换为DPI参数(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width))
            {
                var s = Win32Interactive.转换为DPI参数(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width);
                if (blnTopHide)
                {
                    if (point.Y >= this.Top && point.Y <= this.Top + this.Height && IsMouseEnter)
                    {
                        while (this.Left > Win32Interactive.转换为DPI参数(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width) - (this.Width - 1))
                            this.Left--;
                        blnTopHide = false;
                    }
                }
                else
                {
                    Point b = Mouse.GetPosition(this);//获取鼠标相对窗口的位置
                    if (this.Left + this.Width >= Win32Interactive.转换为DPI参数(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width) && !IsMouseEnter)
                    {
                        while (this.Left < Win32Interactive.转换为DPI参数(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width) - 12)
                            this.Left++;
                        blnTopHide = true;
                    }
                }
            }
        }
        #endregion

        #region 最小化
        private void Mini_Img_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void Mini_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Mini_Img.Source = Mini_Enter;
        }
        private void Mini_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Mini_Img.Source = Mini_Leave;
        }
        #endregion

        #region 最大化
        private void Max_Img_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                Storyboard storyboard = StartGrid.FindResource("StartTo") as Storyboard;
                //DoubleAnimation DoubleAnimation= storyboard.Children[0] as DoubleAnimation;
                Thickness myThickness = new Thickness
                {
                    Bottom = 10,
                    Left = 10,
                    Right = 10,
                    Top = 10
                };
                StartGrid.Margin = myThickness;
                (storyboard.Children[0] as DoubleAnimation).To = 300;
                storyboard.Begin(this);
                this.WindowState = WindowState.Normal;
            }
            else
            {
                Thickness myThickness = new Thickness
                {
                    Bottom = 0,
                    Left = 0,
                    Right = 0,
                    Top = 0
                };
                StartGrid.Margin = myThickness;
                Screen[] screens = Screen.AllScreens;
                Storyboard storyboard = StartGrid.FindResource("StartTo") as Storyboard;
                if (screens.Length >= 1)
                {
                    IntPtr handle = new WindowInteropHelper(this).Handle;
                    var currScreen = System.Windows.Forms.Screen.FromHandle(handle);
                    if (currScreen.Primary)
                    {
                        (storyboard.Children[0] as DoubleAnimation).To = SystemParameters.WorkArea.Width;
                    }
                    else
                    {
                        (storyboard.Children[0] as DoubleAnimation).To = currScreen.WorkingArea.Width;
                    }
                }
                //DoubleAnimation DoubleAnimation = storyboard.Children[0] as DoubleAnimation;
                //double x = SystemParameters.WorkArea.Width;//得到屏幕工作区域宽度
                //double y = SystemParameters.WorkArea.Height;//得到屏幕工作区域高度
                //double x1 = SystemParameters.PrimaryScreenWidth;//得到屏幕整体宽度
                //double y1 = SystemParameters.PrimaryScreenHeight;//得到屏幕整体高度

                //TimeSpan timeSpan = new TimeSpan(20000);

                //Duration duration = new Duration(timeSpan);
                //(storyboard.Children[0] as DoubleAnimation).Duration = duration;


                storyboard.Begin(this);
                this.WindowState = WindowState.Maximized;
            }

        }
        private void Max_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Max_Img.Source = Max_Enter;
        }
        private void Max_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Max_Img.Source = Max_Leave;
        }
        #endregion

        #region 关闭
        private void Exit_Img_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Process[] myproc = Process.GetProcesses();
            foreach (Process item in myproc)
            {
                if (item.ProcessName == "Nine.Design.Client")
                {
                    item.Kill();
                }
            }
        }
        private void Exit_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Exit_Img.Source = Exit_Enter;
        }
        private void Exit_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Exit_Img.Source = Exit_Leave;
        }
        #endregion

        #region 输出文本框文字滚动到最后
        private void ConsoleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ConsoleTextBox.ScrollToEnd();
        }
        #endregion
    }
}
