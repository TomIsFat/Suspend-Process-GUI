using System;
using System.Collections.Generic;

namespace ProcessSuspender.Services
{
    public interface IProcessManager
    {

        /// 挂起指定窗口的进程
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="includeChildProcesses">是否包含子进程</param>
        void SuspendProcess(IntPtr hwnd, bool includeChildProcesses = true);


        /// 恢复指定窗口的进程
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="includeChildProcesses">是否包含子进程</param>
        void ResumeProcess(IntPtr hwnd, bool includeChildProcesses = true);


        /// 检查进程是否受保护
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>是否受保护</returns>
        bool IsProtectedProcess(int pid);


        /// 获取进程的所有可见窗口
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <returns>可见窗口句柄列表</returns>
        List<IntPtr> GetProcessVisibleWindows(int pid);
    }
}