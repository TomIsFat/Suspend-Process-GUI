using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using ProcessSuspender.Services;
using 暂停进程;

namespace ProcessSuspender
{
    public partial class App : Application
    {
        private static Mutex _mutex;

        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;


        /// 应用程序启动时执行
        /// </summary>
        /// <param name="e">启动参数</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "ProcessSuspender";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                Process current = Process.GetCurrentProcess();
                foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                {
                    if (process.Id != current.Id)
                    {
                        IntPtr hWnd = process.MainWindowHandle;
                        if (IsIconic(hWnd))
                        {
                            ShowWindowAsync(hWnd, SW_RESTORE);
                        }
                        SetForegroundWindow(hWnd);
                        break;
                    }
                }
                MessageBox.Show("应用程序已经在运行了！");
                Current.Shutdown();
                return;
            }

            base.OnStartup(e);

            // 初始化服务
            var processManager = new ProcessManager();
            var windowManager = new WindowManager();
            var settingsService = new SettingsService();
            var trayService = new TrayService();

            // 创建主窗口并注入服务
            new MainWindow(processManager, windowManager, settingsService, trayService).Show();
        }
    }
}