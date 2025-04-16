using System;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace ProcessSuspender.Services
{
    public interface IWindowManager
    {
        /// <summary>
        /// 获取鼠标下的窗口句柄
        /// </summary>
        IntPtr GetWindowUnderCursor();

        /// <summary>
        /// 获取鼠标下的顶级窗口句柄
        /// </summary>
        IntPtr GetTopLevelWindowUnderCursor();

        /// <summary>
        /// 获取窗口进程ID
        /// </summary>
        int GetWindowProcessId(IntPtr hwnd);

        /// <summary>
        /// 获取窗口标题
        /// </summary>
        string GetWindowTitle(IntPtr hwnd);

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        void HideWindow(IntPtr hwnd);

        /// <summary>
        /// 显示窗口
        /// </summary>
        void ShowWindowNormal(IntPtr hwnd);

        /// <summary>
        /// 获取窗口逻辑矩形
        /// </summary>
        Rectangle GetLogicalWindowRect(IntPtr hwnd);

        /// <summary>
        /// 捕获窗口截图
        /// </summary>
        Bitmap CaptureWindow(IntPtr handle);

        /// <summary>
        /// 将Bitmap转换为BitmapSource
        /// </summary>
        BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap);

        /// <summary>
        /// 获取窗口DPI缩放比例
        /// </summary>
        float GetWindowDpiScale(IntPtr hwnd);

        /// <summary>
        /// 移动外部窗口
        /// </summary>
        void MoveExternalWindow(IntPtr hwnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

        /// <summary>
        /// 获取窗口图标句柄
        /// </summary>
        IntPtr GetWindowIconHandle(IntPtr hWnd, bool bigIcon);

        /// <summary>
        /// 销毁图标
        /// </summary>
        void DestroyIconSafe(IntPtr hIcon);
    }
}