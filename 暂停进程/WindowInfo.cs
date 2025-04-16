using System.Collections.Generic;
using System;

public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; }
    public int ProcessId { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string UniqueId { get; set; }
    public IntPtr IconHandle { get; set; }
    public List<IntPtr> WindowHandles { get; set; } = new List<IntPtr>(); // 新增
}