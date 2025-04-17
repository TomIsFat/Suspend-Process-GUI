using System;
using System.Collections.Generic;

namespace ProcessSuspender.Models
{
    public class WindowInfo
    {

        /// 窗口句柄

        public IntPtr Handle { get; set; }


        /// 窗口标题

        public string Title { get; set; }


        /// 进程ID

        public int ProcessId { get; set; }


        /// 窗口宽度

        public int Width { get; set; }


        /// 窗口高度

        public int Height { get; set; }


        /// 窗口X坐标

        public int X { get; set; }


        /// 窗口Y坐标

        public int Y { get; set; }


        /// 唯一标识

        public string UniqueId { get; set; }


        /// 图标句柄

        public IntPtr IconHandle { get; set; }


        /// 进程的所有窗口句柄

        public List<IntPtr> WindowHandles { get; set; } = new List<IntPtr>();

        /// 自动冻结开关
        public bool IsAutoSuspendEnabled { get; set; } = false;

        /// 自动冻结时间（秒）
        public int AutoSuspendTime { get; set; } = 60;

        /// 自动冻结计时器（秒）
        public int AutoSuspendTimer { get; set; } = 0;
    }
}