using System;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace ProcessSuspender.Services
{
    public interface IWindowManager
    {
        // 获取鼠标下的窗口句柄
        IntPtr GetWindowUnderCursor();

        // 获取前台窗口句柄
        IntPtr GetForegroundWindowHandle();

        // 获取鼠标下的顶级窗口句柄
        IntPtr GetTopLevelWindowUnderCursor();

        // 获取当前活动窗口句柄
        IntPtr GetTopLevelForegroundWindowHandle();

        // 获取窗口进程ID
        int GetWindowProcessId(IntPtr hwnd);

        // 获取窗口标题
        string GetWindowTitle(IntPtr hwnd);

        // 隐藏窗口
        void HideWindow(IntPtr hwnd);

        // 显示窗口
        void ShowWindowNormal(IntPtr hwnd);

        // 获取窗口逻辑矩形
        Rectangle GetLogicalWindowRect(IntPtr hwnd);

        // 捕获窗口截图
        Bitmap CaptureWindow(IntPtr handle);

        // 将Bitmap转换为BitmapSource
        BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap);

        // 获取窗口DPI缩放比例
        float GetWindowDpiScale(IntPtr hwnd);

        // 移动外部窗口
        void MoveExternalWindow(IntPtr hwnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

        // 获取窗口图标句柄
        IntPtr GetWindowIconHandle(IntPtr hWnd, bool bigIcon);

        // 销毁图标
        void DestroyIconSafe(IntPtr hIcon);
    }
}