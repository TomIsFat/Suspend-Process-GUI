using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Gma.System.MouseKeyHook;

namespace 挂起进程
{
    class Program
    {
        private static IKeyboardMouseEvents m_GlobalHook;
        private static List<int> suspendedPids = new List<int>(); // 存储挂起的PID
        private static Dictionary<int, string> suspendedTitles = new Dictionary<int, string>(); // 存储挂起的窗口标题
        private static Stack<int> minimizedSuspendedPids = new Stack<int>(); // 存储最小化后挂起的PID

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

        enum GetAncestorFlags
        {
            GetParent = 1,
            GetRoot = 2,
            GetRootOwner = 3
        }


        public static void Main()
        {
            m_GlobalHook = Hook.GlobalEvents();
            m_GlobalHook.KeyUp += GlobalHookKeyUp;
            Console.WriteLine("程序运行中... \r\n按 Ctrl+, 挂起窗口，按 Ctrl+. 恢复窗口，按 Ctrl+/ 恢复所有挂起的窗口 \r\n按 Ctrl+Shift+/ 遍历恢复所有挂起的窗口");
            Console.WriteLine("如果恢复没效,可能是卡主了,可以按Ctrl+/ 恢复所有挂起的窗口或者按 Ctrl+Shift+/遍历恢复窗口");
            Application.Run();

            // 程序退出前恢复所有挂起的窗口
            foreach (var pid in suspendedPids.ToArray())
            {
                IntPtr hwnd = IntPtr.Zero;
                try
                {
                    Process process = Process.GetProcessById(pid);
                    if (!process.HasExited)
                    {
                        hwnd = process.MainWindowHandle;
                        IntPtr topLevelHwnd = GetAncestor(hwnd, GetAncestorFlags.GetRoot);
                        ResumeProcess(pid, topLevelHwnd);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"无法恢复进程 {pid}: {ex.Message}");
                }
            }
        }
        // 定义一个字典来跟踪窗口句柄的调用次数
        private static Dictionary<IntPtr, int> hwndCallCount = new Dictionary<IntPtr, int>();
        private static void SmartSuspendOrResumeProcess(IntPtr topLevelHwnd)
        {
            // 检查窗口句柄是否已在字典中
            if (!hwndCallCount.ContainsKey(topLevelHwnd))
            {
                // 如果不在，添加到字典并设置调用次数为1
                hwndCallCount[topLevelHwnd] = 1;
            }
            else
            {
                // 如果已存在，增加调用次数
                hwndCallCount[topLevelHwnd]++;
            }

            // 判断调用次数，以确定传递给SuspendOrResumeProcess的布尔值
            bool suspend = (hwndCallCount[topLevelHwnd] % 2 != 0);

            // 调用原有的SuspendOrResumeProcess函数
            SuspendOrResumeProcess(suspend, topLevelHwnd);
        }

        private static void GlobalHookKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Oemcomma) // Ctrl+,
            {
                IntPtr hwnd = GetWindowUnderCursor();
                IntPtr topLevelHwnd = GetAncestor(hwnd, GetAncestorFlags.GetRoot);
               SuspendOrResumeProcess(true, topLevelHwnd); // 挂起进程
            }
            else if (e.Control && e.KeyCode == Keys.OemPeriod) // Ctrl+.
            {
                IntPtr hwnd = GetWindowUnderCursor();
                //IntPtr hwnd = SuspendedWindowFinder.FindTopMostSuspendedWindowUnderCursor();
                IntPtr topLevelHwnd = GetAncestor(hwnd, GetAncestorFlags.GetRoot);
                if (topLevelHwnd == IntPtr.Zero)
                {
                    Console.WriteLine("错误: 无法获取顶级窗口句柄,请按ctrl+shift+/ 遍历恢复全部窗口");
                  
                }
                else
                {
                    SuspendOrResumeProcess(false, topLevelHwnd); // 恢复进程
                }
             
            }
            else if (e.Control && e.KeyCode == Keys.OemQuestion) // Ctrl+/
            {
                // 恢复所有挂起的进程
                foreach (var pid in suspendedPids.ToArray())
                {
                    IntPtr hwnd = Process.GetProcessById(pid).MainWindowHandle;
                    IntPtr topLevelHwnd = GetAncestor(hwnd, GetAncestorFlags.GetRoot);
                    ResumeProcess(pid, topLevelHwnd);
                }
                Console.WriteLine("操作完毕...");
            }
            else if (e.Control && e.KeyCode == Keys.OemSemicolon) // Shift + Ctrl + ,
            {
                MinimizeAndSuspendProcess();
                Console.WriteLine("操作完毕...");
            }
            else if (e.Control && e.KeyCode == Keys.OemQuotes) // Shift + Ctrl + .
            {
                //Console.WriteLine("恢复窗口");
                RestoreMinimizedSuspendedProcess();
                Console.WriteLine("操作完毕...");
            }
            else if(e.KeyCode == Keys.Oemtilde) // `键
            {
                IntPtr hwnd = GetWindowUnderCursor();
                IntPtr topLevelHwnd = GetAncestor(hwnd, GetAncestorFlags.GetRoot);
                //SuspendOrResumeProcess(true, topLevelHwnd); // 挂起进程
                SmartSuspendOrResumeProcess(topLevelHwnd); //智能判断
            }
            //else if (e.KeyCode == Keys.F6) // Ctrl+.
            //{
            //    IntPtr hwnd = GetWindowUnderCursor();
            //    IntPtr topLevelHwnd = GetAncestor(hwnd, GetAncestorFlags.GetRoot);
            //    SuspendOrResumeProcess(false, topLevelHwnd); // 恢复进程
            //}
            //if (e.KeyCode == Keys.F7) // Ctrl + Shift + /
            //{
            //    //Console.WriteLine("遍历窗口..");
            //    EnumWindows(new EnumWindowsProc(EnumTheWindows), IntPtr.Zero);
            //}


            if (e.Control && e.Shift && e.KeyCode == Keys.OemQuestion) // Ctrl + Shift + /
            {
                //Console.WriteLine("遍历窗口..");
                EnumWindows(new EnumWindowsProc(EnumTheWindows), IntPtr.Zero);
            }


        }

        private static void SuspendOrResumeProcess(bool suspend, IntPtr hwnd)
        {
            IntPtr topLevelHwnd = GetAncestor(hwnd, GetAncestorFlags.GetRoot);
            GetWindowThreadProcessId(topLevelHwnd, out int pid);
            int currentProcessId = Process.GetCurrentProcess().Id;

            if (pid != currentProcessId && !IsProtectedProcess(pid))
            {
                string originalTitle = GetWindowTitle(topLevelHwnd);
                if (suspend)
                {
    

                    // 检查标题是否已包含“已挂起-”，如果没有则添加
                    if (!originalTitle.StartsWith("已挂起-"))
                    {
                        SetWindowTitle(topLevelHwnd, "已挂起-" + originalTitle);
                    }

                    RunCommand($"pssuspend {pid}");
                    suspendedPids.Add(pid);
                    Console.WriteLine($"进程 {pid} ({originalTitle}) 已挂起");
                }
                else
                {
                    ResumeProcess(pid, topLevelHwnd);
                }
            }
            else
            {
                Console.WriteLine("不能挂起系统关键进程或程序自身的进程。");
            }
        }


        private static void ResumeProcess(int pid, IntPtr hwnd)
        {
            RunCommand($"pssuspend -r {pid}");
            suspendedPids.Remove(pid);

            // 检查并更新窗口标题
            string currentTitle = GetWindowTitle(hwnd);
            const string suspendPrefix = "已挂起-";
            if (currentTitle.StartsWith(suspendPrefix))
            {
                SetWindowTitle(hwnd, currentTitle.Substring(suspendPrefix.Length));
            }

            Console.WriteLine($"进程 {pid} 已恢复");

        }




        private static void RunCommand(string command)
        {
            var startInfo = new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true
            };
            var process = Process.Start(startInfo);
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                string error = process.StandardError.ReadToEnd();
                Console.WriteLine($"命令执行错误: {error}");
            }
        }

        private static void SetWindowTitle(IntPtr hwnd, string title)
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            Buff.Append(title);
            SetWindowText(hwnd, Buff.ToString());
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(System.Drawing.Point p);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        public static IntPtr GetWindowUnderCursor()
        {
            Point point;
            GetCursorPos(out point);
            IntPtr windowHandle = WindowFromPoint(point);

            // 检查是否是桌面窗口管理器
            if (IsDesktopWindowManager(windowHandle))
            {
                return SuspendedWindowFinder.FindTopMostSuspendedWindowUnderCursor();
            }

            return windowHandle;
        }

        private static bool IsDesktopWindowManager(IntPtr hwnd)
        {
            GetWindowThreadProcessId(hwnd, out int pid);
            using (var process = Process.GetProcessById(pid))
            {
                return process.ProcessName.Equals("dwm", StringComparison.OrdinalIgnoreCase);
            }
        }



        private static string GetWindowTitle(IntPtr hwnd)
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            if (GetWindowText(hwnd, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private static bool EnumTheWindows(IntPtr hWnd, IntPtr lParam)
        {
            if (hWnd == IntPtr.Zero)
            {
                return true; // 如果句柄无效，继续枚举其他窗口
            }

            string title = GetWindowTitle(hWnd);
            if (!string.IsNullOrEmpty(title) && title.StartsWith("已挂起-"))
            {
                GetWindowThreadProcessId(hWnd, out int pid);
                if (pid != 0 && ProcessExists(pid))
                {
                    ResumeProcess(pid, hWnd);
                }
            }
            return true; // 继续枚举
        }

        private static bool ProcessExists(int pid)
        {
            try
            {
                return Process.GetProcessById(pid) != null;
            }
            catch
            {
                return false; // 如果无法获取进程，说明进程不存在
            }
        }

        private static readonly HashSet<string> ProtectedProcessNames = new HashSet<string>
        {
            "explorer", // 添加任何其他需要保护的系统进程名称2
            // "其他进程名",
        };

        private static bool IsProtectedProcess(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                return ProtectedProcessNames.Contains(process.ProcessName.ToLower());
            }
            catch
            {
                return false; // 如果无法获取进程信息，假设它不是受保护的
            }
        }

        private static void MinimizeAndSuspendProcess()
        {
            IntPtr hwnd = GetWindowUnderCursor();
            IntPtr topLevelHwnd = GetAncestor(hwnd, GetAncestorFlags.GetRoot);
            GetWindowThreadProcessId(topLevelHwnd, out int pid);

            if (!IsProtectedProcess(pid) && !suspendedPids.Contains(pid))
            {
                string originalTitle = GetWindowTitle(topLevelHwnd);

                // 检查标题是否已包含“已挂起-”，如果没有则添加
                if (!originalTitle.StartsWith("已挂起-"))
                {
                    SetWindowTitle(topLevelHwnd, "已挂起-" + originalTitle);
                }

                MinimizeWindow(topLevelHwnd);
                RunCommand($"pssuspend {pid}");
                suspendedPids.Add(pid);
                minimizedSuspendedPids.Push(pid); // 添加到最小化后挂起的堆栈
                Console.WriteLine($"进程 {pid} 已最小化并挂起");
            }
        }


        private static void RestoreMinimizedSuspendedProcess()
        {
            if (minimizedSuspendedPids.Count > 0)
            {
                int pid = minimizedSuspendedPids.Pop();
                IntPtr hwnd = Process.GetProcessById(pid).MainWindowHandle;
                string currentTitle = GetWindowTitle(hwnd);

                ResumeProcess(pid, hwnd);

                const string suspendPrefix = "已挂起-";
                if (currentTitle.StartsWith(suspendPrefix))
                {
                    SetWindowTitle(hwnd, currentTitle.Substring(suspendPrefix.Length));
                }

                RestoreWindow(hwnd);
                Console.WriteLine($"进程 {pid} 已从最小化状态恢复并继续运行");
            }
        }



        private static void MinimizeWindow(IntPtr hwnd)
        {
            ShowWindow(hwnd, ShowWindowCommands.Minimize);
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

        enum ShowWindowCommands
        {
            Minimize = 6,
            Restore = 9
        }

        private static void RestoreWindow(IntPtr hwnd)
        {
            ShowWindow(hwnd, ShowWindowCommands.Restore);
        }

    }
    class SuspendedWindowFinder
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(Point p);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point lpPoint);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public static IntPtr FindTopMostSuspendedWindowUnderCursor()
        {
            Point cursorPosition;
            GetCursorPos(out cursorPosition);
            IntPtr topMostWindow = IntPtr.Zero;
            int highestZOrder = int.MinValue;

            EnumWindows(delegate (IntPtr hWnd, IntPtr param)
            {
                var title = GetWindowTitle(hWnd);
                if (IsWindowVisible(hWnd) && title != null && title.StartsWith("已挂起-"))
                {
                    if (IsWindowUnderCursor(hWnd, cursorPosition) && IsWindowOnTop(hWnd, ref highestZOrder))
                    {
                        topMostWindow = hWnd;
                    }
                }
                return true;
            }, IntPtr.Zero);

            return topMostWindow;
        }


        private static string GetWindowTitle(IntPtr hwnd)
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            if (GetWindowText(hwnd, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        private static bool IsWindowUnderCursor(IntPtr hWnd, Point cursorPosition)
        {
            RECT windowRect;
            GetWindowRect(hWnd, out windowRect);
            return windowRect.Contains(cursorPosition);
        }

        private static bool IsWindowOnTop(IntPtr hWnd, ref int highestZOrder)
        {
            var zOrder = GetWindowZOrder(hWnd);
            if (zOrder > highestZOrder)
            {
                highestZOrder = zOrder;
                return true;
            }
            return false;
        }

        private static int GetWindowZOrder(IntPtr hWnd)
        {
            var zOrder = 0;
            IntPtr hCurrent = hWnd;
            while ((hCurrent = GetWindow(hCurrent, GW_HWNDPREV)) != IntPtr.Zero)
            {
                zOrder++;
            }
            return zOrder;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, int uCmd);

        const int GW_HWNDPREV = 3;

        struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public bool Contains(Point point)
            {
                return point.X >= Left && point.X < Right && point.Y >= Top && point.Y < Bottom;
            }
        }
    }

}
