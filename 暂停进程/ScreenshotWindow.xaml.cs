using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace 暂停进程
{
    public partial class ScreenshotWindow : Window
    {
        private readonly WindowInfo _windowInfo;
        private readonly MainWindow _mainWindow;

        public ScreenshotWindow(BitmapSource screenshotSource, WindowInfo windowInfo, MainWindow mainWindow)
        {
            InitializeComponent();
            ScreenshotImage.Source = screenshotSource;
            _windowInfo = windowInfo;
            _mainWindow = mainWindow;
            DataContext = windowInfo;

            Loaded += (s, e) =>
            {
                // 隐藏所有相关窗口
                foreach (var handle in _windowInfo.WindowHandles)
                {
                    WindowManager.HideWindow(handle);
                }

                // 异步挂起主窗口所属进程
                Task.Run(() => PauseFunction.SuspendOrResumeProcess(true, _windowInfo.Handle, null, true));
            };
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 1)
            {
                DragMove();
            }
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            // 恢复进程
            PauseFunction.SuspendOrResumeProcess(false, _windowInfo.Handle, null, true);

            // 显示所有相关窗口
            foreach (var handle in _windowInfo.WindowHandles)
            {
                WindowManager.ShowWindowNormal(handle);
            }

            // 恢复窗口位置
            WindowManager.GetWindowRect(_windowInfo.Handle, out WindowManager.RECT rect);
            float factor = WindowManager.GetWindowDpiScale(_windowInfo.Handle);
            WindowManager.MoveExternalWindow(_windowInfo.Handle, (int)(Left * factor), (int)(Top * factor),
                rect.Right - rect.Left, rect.Bottom - rect.Top, true);

            // 更新主窗口状态
            var windowModel = _mainWindow.WindowModels.FirstOrDefault(wm => wm.ProcessId == _windowInfo.ProcessId);
            if (windowModel != null)
            {
                windowModel.Status = "正常";
                windowModel.ToggleSuspendText = "挂起";
            }

            _mainWindow.RemoveWindowInfo(_windowInfo);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            ScreenshotImage.Source = null;
        }
    }
}
