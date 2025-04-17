using System;
using System.Collections.ObjectModel;
using System.Windows;
using Gma.System.MouseKeyHook;
using ProcessSuspender.Services;
using ProcessSuspender.Models;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Linq;
using 暂停进程;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using System.Windows.Threading;

namespace ProcessSuspender
{
    public partial class MainWindow : Window
    {
        private readonly IProcessManager _processManager;
        private readonly IWindowManager _windowManager;
        private readonly ISettingsService _settingsService;
        private readonly ITrayService _trayService;
        private readonly IKeyboardMouseEvents _globalHook;
        private readonly DispatcherTimer _autoSuspendTimer;
        public ObservableCollection<WindowModel> WindowModels { get; } = new ObservableCollection<WindowModel>();

        // 构造函数，注入服务
        public MainWindow(IProcessManager processManager, IWindowManager windowManager,
            ISettingsService settingsService, ITrayService trayService)
        {
            InitializeComponent();
            _processManager = processManager;
            _windowManager = windowManager;
            _settingsService = settingsService;
            _trayService = trayService;

            windowsDataGrid.ItemsSource = WindowModels;
            UpdateShortcutText();

            _globalHook = Hook.GlobalEvents();
            _globalHook.KeyUp += GlobalHookKeyUp;

            _trayService.Initialize(OnTrayIconClick);

            // 初始化自动冻结定时器
            _autoSuspendTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _autoSuspendTimer.Tick += AutoSuspendTimer_Tick;
            _autoSuspendTimer.Start();
        }

        // 处理全局键盘释放事件，检测快捷键
        private void GlobalHookKeyUp(object sender, KeyEventArgs e)
        {
            var settings = _settingsService.GetSettings();
            bool isShortcutMatch = e.KeyCode == settings.Key &&
                                 e.Control == settings.Control &&
                                 e.Shift == settings.Shift &&
                                 e.Alt == settings.Alt;

            if (isShortcutMatch)
            {
                SuspendProcess();
            }
        }

        // 定时器检查自动冻结逻辑
        private void AutoSuspendTimer_Tick(object sender, EventArgs e)
        {
            IntPtr foregroundHwnd = _windowManager.GetTopLevelForegroundWindowHandle();
            int foregroundPid = _windowManager.GetWindowProcessId(foregroundHwnd);

            foreach (var model in WindowModels.ToList())
            {
                if (model.IsAutoSuspendEnabled && model.Status == "正常")
                {
                    if (model.ProcessId == foregroundPid)
                    {
                        model.WindowInfo.AutoSuspendTimer = 0; // 前台时重置计时器
                    }
                    else
                    {
                        model.WindowInfo.AutoSuspendTimer += 3; // 后台时计时器加3秒
                        if (model.WindowInfo.AutoSuspendTimer >= model.WindowInfo.AutoSuspendTime)
                        {
                            model.ToggleSuspendCommand.Execute(null); // 触发冻结
                        }
                    }
                }
            }
        }

        // 更新快捷键显示文本
        private void UpdateShortcutText()
        {
            var settings = _settingsService.GetSettings();
            string shortcut = "按 ";
            if (settings.Control) shortcut += "Ctrl + ";
            if (settings.Shift) shortcut += "Shift + ";
            if (settings.Alt) shortcut += "Alt + ";
            shortcut += settings.Key.ToString() + " 冻结窗口进程；双击窗口或关闭窗口解冻";
            SetValue(ShortcutTextProperty, shortcut);
        }

        public static readonly DependencyProperty ShortcutTextProperty =
            DependencyProperty.Register("ShortcutText", typeof(string), typeof(MainWindow), new PropertyMetadata(""));

        public string ShortcutText
        {
            get => (string)GetValue(ShortcutTextProperty);
            set => SetValue(ShortcutTextProperty, value);
        }

        // 挂起当前前台下的窗口进程
        public void SuspendProcess()
        {
            IntPtr topLevelHwnd = _windowManager.GetTopLevelForegroundWindowHandle();
            int processId = _windowManager.GetWindowProcessId(topLevelHwnd);

            if (processId == System.Diagnostics.Process.GetCurrentProcess().Id || _processManager.IsProtectedProcess(processId))
            {
                Console.WriteLine("不能挂起系统关键进程或程序自身进程。");
                return;
            }

            var windowModel = WindowModels.FirstOrDefault(wm => wm.ProcessId == processId);
            if (windowModel != null) // 若在主界面列表中找到了，先删除再添加
            {
                WindowModels.Remove(windowModel);
            }
            WindowInfo windowInfo = new WindowInfo
            {
                Handle = topLevelHwnd,
                Title = _windowManager.GetWindowTitle(topLevelHwnd),
                ProcessId = processId,
                WindowHandles = _processManager.GetProcessVisibleWindows(processId),
                AutoSuspendTimer = 0 // 重置计时器
            };
            CreateMockWindowFor(windowInfo);
            AddWindowInfo(windowInfo);
        }

        // 为挂起的窗口创建截图窗口
        public void CreateMockWindowFor(WindowInfo windowInfo)
        {
            try
            {
                System.Drawing.Rectangle logicalRect = _windowManager.GetLogicalWindowRect(windowInfo.Handle);
                BitmapSource screenshotSource;
                using (System.Drawing.Bitmap screenshot = _windowManager.CaptureWindow(windowInfo.Handle))
                {
                    screenshotSource = _windowManager.ConvertBitmapToBitmapSource(screenshot);
                }

                IntPtr iconHandle = _windowManager.GetWindowIconHandle(windowInfo.Handle, true);
                windowInfo.IconHandle = iconHandle;
                windowInfo.Width = logicalRect.Width;
                windowInfo.Height = logicalRect.Height;
                windowInfo.X = logicalRect.X;
                windowInfo.Y = logicalRect.Y;
                windowInfo.UniqueId = $"MockWindow_{windowInfo.Handle}_{windowInfo.ProcessId}";

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mockWindow = new ScreenshotWindow(screenshotSource, windowInfo, this, _processManager, _windowManager)
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
                            using (var newBitmap = new System.Drawing.Bitmap(originalBitmap.Width, originalBitmap.Height))
                            using (var graphics = System.Drawing.Graphics.FromImage(newBitmap))
                            {
                                graphics.DrawImage(originalBitmap, 0, 0);
                                int snowflakeSize = originalBitmap.Width / 2;
                                int x = originalBitmap.Width - snowflakeSize;
                                int y = originalBitmap.Height - snowflakeSize;

                                using (var snowflakePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(252, 71, 39), (int)(snowflakeSize / 5)))
                                {
                                    graphics.DrawLine(snowflakePen, x + snowflakeSize / 2, y, x + snowflakeSize / 2, y + snowflakeSize);
                                    graphics.DrawLine(snowflakePen, x, y + snowflakeSize / 2, x + snowflakeSize, y + snowflakeSize / 2);
                                    graphics.DrawLine(snowflakePen, x, y, x + snowflakeSize, y + snowflakeSize);
                                    graphics.DrawLine(snowflakePen, x + snowflakeSize, y, x, y + snowflakeSize);
                                }

                                IntPtr hIcon = newBitmap.GetHicon();
                                try
                                {
                                    mockWindow.Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                        hIcon,
                                        System.Windows.Int32Rect.Empty,
                                        BitmapSizeOptions.FromEmptyOptions());
                                }
                                finally
                                {
                                    if (hIcon != IntPtr.Zero)
                                        _windowManager.DestroyIconSafe(hIcon);
                                }
                            }
                        }
                        finally
                        {
                            if (iconHandle != IntPtr.Zero)
                                _windowManager.DestroyIconSafe(iconHandle);
                        }
                    }

                    mockWindow.Show();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建模仿窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 添加窗口信息到DataGrid
        public void AddWindowInfo(WindowInfo windowInfo)
        {
            if (windowInfo == null || windowInfo.ProcessId <= 0 || string.IsNullOrEmpty(windowInfo.Title))
            {
                Console.WriteLine("无效的窗口信息");
                return;
            }

            var windowModel = new WindowModel(this, _processManager, _windowManager)
            {
                Title = windowInfo.Title,
                ProcessId = windowInfo.ProcessId,
                Status = "冻结",
                WindowInfo = windowInfo,
                IsAutoSuspendEnabled = windowInfo.IsAutoSuspendEnabled,
                AutoSuspendTime = windowInfo.AutoSuspendTime
            };

            WindowModels.Add(windowModel);
        }

        // 移除窗口信息
        public void RemoveWindowInfo(WindowInfo windowInfo)
        {
            var windowModel = WindowModels.FirstOrDefault(wm => wm.WindowInfo == windowInfo);
            if (windowModel != null)
            {
                WindowModels.Remove(windowModel);
            }
        }

        // 触发器设置快捷键
        private void SetShortcut_Click(object sender, RoutedEventArgs e)
        {
            var shortcutWindow = new ShortcutBindingWindow(this, _settingsService);
            if (shortcutWindow.ShowDialog() == true)
            {
                UpdateShortcutText();
            }
        }

        // 触发器处理恢复所有挂起按钮点击
        private void RestoreAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var window in Application.Current.Windows.OfType<ScreenshotWindow>().ToList())
            {
                window.Close();
            }

            WindowModels.Clear();
        }

        // 触发器处理系统托盘图标点击
        private void OnTrayIconClick(object sender, EventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
        }

        // 触发器窗口状态改变时处理
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                _trayService.Show();
            }
            base.OnStateChanged(e);
        }

        // 触发器窗口关闭时清理资源
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _autoSuspendTimer.Stop();
            _trayService.Dispose();
            _globalHook.KeyUp -= GlobalHookKeyUp;
            _globalHook.Dispose();
            base.OnClosing(e);
        }
    }
}