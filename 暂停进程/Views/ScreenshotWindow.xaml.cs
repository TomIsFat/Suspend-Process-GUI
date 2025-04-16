using System;
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

        /// <summary>
        /// 构造函数
        /// </summary>
        public ScreenshotWindow(BitmapSource screenshotSource, WindowInfo windowInfo, MainWindow mainWindow,
            IProcessManager processManager, IWindowManager windowManager)
        {
            InitializeComponent();
            ScreenshotImage.Source = screenshotSource;
            _windowInfo = windowInfo;
            _mainWindow = mainWindow;
            _processManager = processManager;
            _windowManager = windowManager;
            DataContext = windowInfo;

            Loaded += (s, e) =>
            {
                foreach (var handle in _windowInfo.WindowHandles)
                {
                    _windowManager.HideWindow(handle);
                }

                Task.Run(() => _processManager.SuspendProcess(_windowInfo.Handle, true));
            };
        }

        /// <summary>
        /// 处理鼠标左键按下
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 1)
            {
                DragMove();
            }
        }

        /// <summary>
        /// 处理鼠标双击
        /// </summary>
        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 处理键盘按下
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        /// <summary>
        /// 窗口关闭时处理
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            _processManager.ResumeProcess(_windowInfo.Handle, true);
            foreach (var handle in _windowInfo.WindowHandles)
            {
                _windowManager.ShowWindowNormal(handle);
            }

            GetWindowRect(_windowInfo.Handle, out WindowManager.RECT rect);
            float factor = _windowManager.GetWindowDpiScale(_windowInfo.Handle);
            _windowManager.MoveExternalWindow(_windowInfo.Handle, (int)(Left * factor), (int)(Top * factor),
                rect.Right - rect.Left, rect.Bottom - rect.Top, true);

            var windowModel = _mainWindow.WindowModels.FirstOrDefault(wm => wm.ProcessId == _windowInfo.ProcessId);
            if (windowModel != null)
            {
                windowModel.Status = "正常";
                windowModel.ToggleSuspendText = "挂起";
            }

            _mainWindow.RemoveWindowInfo(_windowInfo);
        }

        /// <summary>
        /// 窗口关闭后清理
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            ScreenshotImage.Source = null;
        }
    }
}