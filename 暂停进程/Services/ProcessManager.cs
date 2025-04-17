using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Documents;

namespace ProcessSuspender.Services
{
    public class ProcessManager : IProcessManager
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

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

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private enum GetAncestorFlags
        {
            GetRoot = 2
        }

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            SuspendResume = 0x0800
        }

        private static readonly HashSet<string> ProtectedProcessNames = new HashSet<string>
        {
            "explorer", "quicker", "devenv"
        };

        /// 挂起指定窗口的进程
        public void SuspendProcess(IntPtr hwnd, bool includeChildProcesses = true)
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
                if (processId != currentProcessId && !IsProtectedProcess(processId))
                {
                    SuspendProcessCore(processId);
                }
            }
        }

        /// 恢复指定窗口的进程
        public void ResumeProcess(IntPtr hwnd, bool includeChildProcesses = true)
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
                if (processId != currentProcessId && !IsProtectedProcess(processId))
                {
                    ResumeProcessCore(processId, topLevelHwnd);
                }
            }
        }

        /// 检查进程是否受保护
        public bool IsProtectedProcess(int pid)
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

        /// 获取进程的所有可见窗口
        public List<IntPtr> GetProcessVisibleWindows(int processId)
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

        /// 获取子进程ID列表
        private List<int> GetChildProcessIds(int parentPid)
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

        /// 核心挂起进程逻辑
        private void SuspendProcessCore(int pid)
        {
            IntPtr handle = OpenProcess(ProcessAccessFlags.SuspendResume, false, pid);
            if (handle == IntPtr.Zero) return;
            NtSuspendProcess(handle);
            CloseHandle(handle);
        }

        /// 核心恢复进程逻辑
        private void ResumeProcessCore(int pid, IntPtr hwnd)
        {
            IntPtr handle = OpenProcess(ProcessAccessFlags.SuspendResume, false, pid);
            if (handle == IntPtr.Zero) return;
            NtResumeProcess(handle);
            CloseHandle(handle);
        }

        /// 获取窗口标题
        private string GetWindowTitle(IntPtr hwnd)
        {
            StringBuilder buff = new StringBuilder(256);
            return GetWindowText(hwnd, buff, buff.Capacity) > 0 ? buff.ToString() : string.Empty;
        }
    }
}