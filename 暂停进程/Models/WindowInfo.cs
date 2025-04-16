using System;
using System.Collections.Generic;

namespace ProcessSuspender.Models
{
    public class WindowInfo
    {
        /// <summary>
        /// 窗口句柄
        /// </summary>
        public IntPtr Handle { get; set; }

        /// <summary>
        /// 窗口标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 进程ID
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// 窗口宽度
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 窗口高度
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 窗口X坐标
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// 窗口Y坐标
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// 唯一标识
        /// </summary>
        public string UniqueId { get; set; }

        /// <summary>
        /// 图标句柄
        /// </summary>
        public IntPtr IconHandle { get; set; }

        /// <summary>
        /// 进程的所有窗口句柄
        /// </summary>
        public List<IntPtr> WindowHandles { get; set; } = new List<IntPtr>();
    }
}