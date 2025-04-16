using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace 暂停进程
{
    public static class PauseFunction
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        public static List<IntPtr> GetProcessVisibleWindows(int processId)
        {
            List<IntPtr> handles = new List<IntPtr>();
            EnumWindows((hWnd, lParam) =>
            {
                GetWindowThreadProcessId(hWnd, out int pid);
                if (pid == processId && IsWindowVisible(hWnd))
                {
                    handles.Add(hWnd);
                }
                return true;
            }, IntPtr.Zero);
            return handles;
        }

        private static readonly HashSet<string> ProtectedProcessNames = new HashSet<string>
        {
            "explorer", "quicker", "devenv"
        };

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            SuspendResume = 0x0800
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSuspendProcess(IntPtr processHandle);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtResumeProcess(IntPtr processHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(ProcessAccessFlags access, bool inheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        enum GetAncestorFlags
        {
            GetRoot = 2
        }

        public static void SuspendOrResumeProcess(bool suspend, IntPtr hwnd, List<int> ignoredPids = null, bool includeChildProcesses = true)
        {
            IntPtr topLevelHwnd = GetAncestor(hwnd, GetAncestorFlags.GetRoot);
            GetWindowThreadProcessId(topLevelHwnd, out int pid);
            int currentProcessId = Process.GetCurrentProcess().Id;

            List<int> processesToHandle = new List<int> { pid };
            if (includeChildProcesses)
            {
                processesToHandle.AddRange(GetChildProcessIds(pid));
            }

            foreach (int processId in processesToHandle)
            {
                if ((ignoredPids == null || !ignoredPids.Contains(processId)) &&
                    processId != currentProcessId &&
                    !IsProtectedProcess(processId))
                {
                    string originalTitle = GetWindowTitle(topLevelHwnd);
                    if (suspend)
                    {
                        if (!originalTitle.StartsWith("已挂起-"))
                        {
                            SetWindowText(topLevelHwnd, "已挂起-" + originalTitle);
                        }
                        SuspendProcess(processId);
                    }
                    else
                    {
                        ResumeProcess(processId, topLevelHwnd);
                    }
                }
            }
        }

        private static List<int> GetChildProcessIds(int parentPid)
        {
            List<int> childPids = new List<int>();
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT ProcessID FROM Win32_Process WHERE ParentProcessID = {parentPid}"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        childPids.Add(Convert.ToInt32(obj["ProcessID"]));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取子进程ID失败: {ex.Message}");
            }
            return childPids;
        }

        private static void SuspendProcess(int pid)
        {
            IntPtr handle = OpenProcess(ProcessAccessFlags.SuspendResume, false, pid);
            if (handle == IntPtr.Zero) return;
            NtSuspendProcess(handle);
            CloseHandle(handle);
        }

        private static void ResumeProcess(int pid, IntPtr hwnd)
        {
            IntPtr handle = OpenProcess(ProcessAccessFlags.SuspendResume, false, pid);
            if (handle == IntPtr.Zero) return;
            NtResumeProcess(handle);
            CloseHandle(handle);

            string currentTitle = GetWindowTitle(hwnd);
            if (currentTitle.StartsWith("已挂起-"))
            {
                SetWindowText(hwnd, currentTitle.Substring("已挂起-".Length));
            }
        }

        private static string GetWindowTitle(IntPtr hwnd)
        {
            StringBuilder buff = new StringBuilder(256);
            return GetWindowText(hwnd, buff, buff.Capacity) > 0 ? buff.ToString() : string.Empty;
        }

        public static bool EnumTheWindowsAndDisplay(IntPtr hWnd, IntPtr lParam)
        {
            if (hWnd == IntPtr.Zero) return true;

            string title = GetWindowTitle(hWnd);
            if (!string.IsNullOrEmpty(title) && title.StartsWith("已挂起-"))
            {
                SuspendOrResumeProcess(false, hWnd, null, true);
                ShowWindow(hWnd, 5); // SW_SHOW
            }
            return true;
        }

        public static bool IsProtectedProcess(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                return ProtectedProcessNames.Contains(process.ProcessName.ToLower());
            }
            catch
            {
                return false;
            }
        }
    }
}