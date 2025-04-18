using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ProcessSuspender.Models;
using ProcessSuspender.Services;
using static ProcessSuspender.Services.WindowManager;

namespace ProcessSuspender
{
    public partial class ScreenshotWindow : Window
    {
        private readonly WindowInfo _windowInfo;
        private readonly MainWindow _mainWindow;
        private readonly IProcessManager _processManager;
        private readonly IWindowManager _windowManager;
        private byte[] _screenshotBytes; // 存储截图的字节数组（CPU内存）

        /// 构造函数
        public ScreenshotWindow(BitmapSource screenshotSource, WindowInfo windowInfo, MainWindow mainWindow,
            IProcessManager processManager, IWindowManager windowManager)
        {
            InitializeComponent();
            _windowInfo = windowInfo;
            _mainWindow = mainWindow;
            _processManager = processManager;
            _windowManager = windowManager;
            DataContext = windowInfo;

            // 将初始截图存储为字节数组并绑定
            StoreScreenshotAsBytes(screenshotSource);
            ScreenshotImage.Source = screenshotSource;

            Loaded += (s, e) =>
            {
                foreach (var handle in _windowInfo.WindowHandles)
                {
                    _windowManager.HideWindow(handle);
                }

                Task.Run(() => _processManager.SuspendProcess(_windowInfo.Handle, true));
            };

            // 监听窗口状态和可见性变化
            StateChanged += ScreenshotWindow_StateChanged;
            IsVisibleChanged += ScreenshotWindow_IsVisibleChanged;
        }

        /// 将BitmapSource存储为字节数组
        private void StoreScreenshotAsBytes(BitmapSource source)
        {
            using (var memoryStream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(memoryStream);/**/
                _screenshotBytes = memoryStream.ToArray();
            }
        }

        /// 从字节数组加载BitmapSource
        private BitmapSource LoadScreenshotFromBytes()
        {
            using (var memoryStream = new MemoryStream(_screenshotBytes))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // 冻结以提高性能
                return bitmapImage;
            }
        }

        /// 处理窗口状态变化（最小化/恢复）
        private void ScreenshotWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                // 最小化时释放GPU资源
                ScreenshotImage.Source = null;
            }
            else if (WindowState == WindowState.Normal || WindowState == WindowState.Maximized)
            {
                // 恢复时重新加载图像
                if (ScreenshotImage.Source == null && _screenshotBytes != null)
                {
                    ScreenshotImage.Source = LoadScreenshotFromBytes();
                }
            }
        }

        /// 处理窗口可见性变化
        private void ScreenshotWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                // 不可见时释放GPU资源
                ScreenshotImage.Source = null;
            }
            else
            {
                // 可见时重新加载图像
                if (ScreenshotImage.Source == null && _screenshotBytes != null)
                {
                    ScreenshotImage.Source = LoadScreenshotFromBytes();
                }
            }
        }

        /// 处理鼠标左键按下
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 1)
            {
                DragMove();
            }
        }

        /// 处理鼠标双击
        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        /// 处理键盘按下
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        /// 窗口关闭时处理
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            _processManager.ResumeProcess(_windowInfo.Handle, true);
            foreach (var handle in _windowInfo.WindowHandles)
            {
                _windowManager.ShowWindowNormal(handle);
            }

            GetWindowRect(_windowInfo.Handle, out RECT rect);
            float factor = _windowManager.GetWindowDpiScale(_windowInfo.Handle);
            _windowManager.MoveExternalWindow(_windowInfo.Handle, (int)(Left * factor), (int)(Top * factor),
                rect.Right - rect.Left, rect.Bottom - rect.Top, true);

            var windowModel = _mainWindow.WindowModels.FirstOrDefault(wm => wm.ProcessId == _windowInfo.ProcessId);
            if (windowModel != null)
            {
                windowModel.Status = "正常";
                windowModel.ToggleStatusText = "正常";
                windowModel.WindowInfo.AutoSuspendTimer = 0; // 重置计时器
            }
        }

        /// 窗口关闭后清理
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            ScreenshotImage.Source = null;
            _screenshotBytes = null; // 释放字节数组
        }
    }
}