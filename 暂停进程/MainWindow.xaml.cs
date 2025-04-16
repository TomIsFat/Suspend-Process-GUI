using Gma.System.MouseKeyHook;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms; // 用于 NotifyIcon 和 KeyEventArgs
using System.Windows.Interop; // 用于 Imaging
using System.Windows.Media.Imaging;

namespace 暂停进程
{
    public partial class MainWindow : Window
    {
        private Settings _settings;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private readonly IKeyboardMouseEvents _globalHook;
        private readonly List<WindowInfo> _windowInfos = new List<WindowInfo>();
        public ObservableCollection<WindowModel> WindowModels { get; } = new ObservableCollection<WindowModel>();
        private readonly NotifyIcon _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            windowsDataGrid.ItemsSource = WindowModels;
            _settings = Settings.Load();
            UpdateShortcutText();

            // Initialize global hook
            _globalHook = Hook.GlobalEvents();
            _globalHook.KeyUp += GlobalHookKeyUp;

            // 初始化托盘图标
            _notifyIcon = new NotifyIcon
            {
                Icon = LoadIconFromResource(),
                Visible = true
            };
            _notifyIcon.Click += (s, e) =>
            {
                Show();
                WindowState = WindowState.Normal;
            };
        }
        private Icon LoadIconFromResource()
        {
            try
            {
                // 获取嵌入资源的流
                using (var stream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/pm.ico"))?.Stream)
                {
                    if (stream == null)
                    {
                        throw new FileNotFoundException("无法找到嵌入资源：Resources/pm.ico");
                    }
                    return new Icon(stream);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载托盘图标失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                // 返回默认图标或处理错误
                return SystemIcons.Application;
            }
        }

        private void UpdateShortcutText()
        {
            string shortcut = "按 ";
            if (_settings.Control) shortcut += "Ctrl + ";
            if (_settings.Shift) shortcut += "Shift + ";
            if (_settings.Alt) shortcut += "Alt + ";
            shortcut += _settings.Key.ToString() + " 冻结窗口进程；双击窗口或者关闭窗口解冻";
            DataContext = this;
            SetValue(ShortcutTextProperty, shortcut);
        }

        // Add dependency property
        public static readonly DependencyProperty ShortcutTextProperty =
            DependencyProperty.Register("ShortcutText", typeof(string), typeof(MainWindow), new PropertyMetadata(""));

        public string ShortcutText
        {
            get { return (string)GetValue(ShortcutTextProperty); }
            set { SetValue(ShortcutTextProperty, value); }
        }

        // Replace GlobalHookKeyUp method
        private void GlobalHookKeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            bool isShortcutMatch = e.KeyCode == _settings.Key &&
                                 e.Control == _settings.Control &&
                                 e.Shift == _settings.Shift &&
                                 e.Alt == _settings.Alt;

            if (isShortcutMatch)
            {
                SuspendProcess();
            }
        }

        private void SetShortcut_Click(object sender, RoutedEventArgs e)
        {
            var shortcutWindow = new ShortcutBindingWindow(this);
            if (shortcutWindow.ShowDialog() == true)
            {
                _settings = Settings.Load();
                UpdateShortcutText();
            }
        }
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                _notifyIcon.Visible = true;
            }
            base.OnStateChanged(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _notifyIcon.Dispose();
            _globalHook.KeyUp -= GlobalHookKeyUp;
            _globalHook.Dispose();
            base.OnClosing(e);
        }


        public void SuspendProcess()
        {
            IntPtr hwnd = WindowManager.GetWindowUnderCursor();
            IntPtr topLevelHwnd = WindowManager.GetTopLevelWindowUnderCursor();
            int processId = WindowManager.GetWindowProcessId(topLevelHwnd);

            if (processId == Process.GetCurrentProcess().Id || PauseFunction.IsProtectedProcess(processId))
            {
                Console.WriteLine("不能挂起系统关键进程或程序自身进程。");
                return;
            }

            // 获取该进程所有可见窗口句柄
            var visibleWindows = PauseFunction.GetProcessVisibleWindows(processId);

            var windowModel = WindowModels.FirstOrDefault(wm => wm.ProcessId == processId);
            if (windowModel != null)
            {
                windowModel.Status = "已挂起";
                windowModel.ToggleSuspendText = "恢复";
                windowModel.WindowInfo.WindowHandles = visibleWindows; // 更新窗口句柄列表
                CreateMockWindowFor(windowModel.WindowInfo);
            }
            else
            {
                WindowInfo windowInfo = new WindowInfo
                {
                    Handle = topLevelHwnd,
                    Title = WindowManager.GetWindowTitle(topLevelHwnd),
                    ProcessId = processId,
                    WindowHandles = visibleWindows // 设置窗口句柄列表
                };
                CreateMockWindowFor(windowInfo);
                AddWindowInfo(windowInfo);
            }
        }

        public void CreateMockWindowFor(WindowInfo windowInfo)
        {
            try
            {
                Rectangle logicalRect = WindowManager.GetLogicalWindowRect(windowInfo.Handle);
                BitmapSource screenshotSource;
                using (Bitmap screenshot = WindowManager.CaptureWindow(windowInfo.Handle))
                {
                    screenshotSource = WindowManager.ConvertBitmapToBitmapSource(screenshot);
                }

                IntPtr iconHandle = WindowsCloner.GetWindowIconHandle(windowInfo.Handle, true);
                windowInfo.IconHandle = iconHandle;
                windowInfo.Width = logicalRect.Width;
                windowInfo.Height = logicalRect.Height;
                windowInfo.X = logicalRect.X;
                windowInfo.Y = logicalRect.Y;
                windowInfo.UniqueId = $"MockWindow_{windowInfo.Handle}_{windowInfo.ProcessId}";

                _windowInfos.Add(windowInfo);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mockWindow = new ScreenshotWindow(screenshotSource, windowInfo, this)
                    {
                        Left = logicalRect.X,
                        Top = logicalRect.Y,
                        Width = logicalRect.Width,
                        Height = logicalRect.Height
                    };

                    if (iconHandle != IntPtr.Zero)
                    {
                        try
                        {
                            using (var originalIcon = System.Drawing.Icon.FromHandle(iconHandle))
                            using (var originalBitmap = originalIcon.ToBitmap())
                            using (var newBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height))
                            using (var graphics = Graphics.FromImage(newBitmap))
                            {
                                graphics.DrawImage(originalBitmap, 0, 0);
                                int snowflakeSize = originalBitmap.Width / 2;
                                int x = originalBitmap.Width - snowflakeSize;
                                int y = originalBitmap.Height - snowflakeSize;

                                using (var snowflakePen = new Pen(Color.FromArgb(252, 71, 39), (int)(snowflakeSize /5)))
                                {
                                    graphics.DrawLine(snowflakePen, x + snowflakeSize / 2, y, x + snowflakeSize / 2, y + snowflakeSize);
                                    graphics.DrawLine(snowflakePen, x, y + snowflakeSize / 2, x + snowflakeSize, y + snowflakeSize / 2);
                                    graphics.DrawLine(snowflakePen, x, y, x + snowflakeSize, y + snowflakeSize);
                                    graphics.DrawLine(snowflakePen, x + snowflakeSize, y, x, y + snowflakeSize);
                                }

                                IntPtr hIcon = newBitmap.GetHicon();
                                try
                                {
                                    mockWindow.Icon = Imaging.CreateBitmapSourceFromHIcon(
                                        hIcon,
                                        Int32Rect.Empty,
                                        BitmapSizeOptions.FromEmptyOptions());
                                }
                                finally
                                {
                                    if (hIcon != IntPtr.Zero)
                                        DestroyIcon(hIcon);
                                }
                            }
                        }
                        finally
                        {
                            if (iconHandle != IntPtr.Zero)
                                DestroyIcon(iconHandle);
                        }
                    }

                    mockWindow.Show();
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"创建模仿窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RemoveWindowInfo(WindowInfo windowInfo)
        {
            _windowInfos.Remove(windowInfo);
            var windowModel = WindowModels.FirstOrDefault(wm => wm.WindowInfo == windowInfo);
            if (windowModel != null)
            {
                WindowModels.Remove(windowModel);
            }
        }

        private void AddWindowInfo(WindowInfo windowInfo)
        {
            if (windowInfo == null || windowInfo.ProcessId <= 0 || string.IsNullOrEmpty(windowInfo.Title))
            {
                Console.WriteLine("无效的窗口信息");
                return;
            }

            var windowModel = new WindowModel(this)
            {
                Title = windowInfo.Title,
                ProcessId = windowInfo.ProcessId,
                Status = "已挂起",
                ToggleSuspendText = "恢复",
                WindowInfo = windowInfo
            };

            WindowModels.Add(windowModel);
        }

        private void RestoreAll_Click(object sender, RoutedEventArgs e)
        {
            // 关闭所有替代窗口
            foreach (var window in System.Windows.Application.Current.Windows.OfType<ScreenshotWindow>().ToList())
            {
                window.Close();
            }

            WindowModels.Clear();
            _windowInfos.Clear();
        }
    }
}