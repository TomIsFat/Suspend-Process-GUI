using System;
using System.Runtime.InteropServices;

namespace 暂停进程
{
    public static class WindowsCloner
    {
        private const int WM_GETICON = 0x007F;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const int GCL_HICON = -14;
        private const int GCL_HICONSM = -34;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetClassLong")]
        private static extern IntPtr GetClassLong32(IntPtr hWnd, int nIndex);

        public static IntPtr GetWindowIconHandle(IntPtr hWnd, bool bigIcon)
        {
            IntPtr hIcon = SendMessage(hWnd, WM_GETICON, bigIcon ? ICON_BIG : ICON_SMALL, 0);
            if (hIcon == IntPtr.Zero)
            {
                hIcon = Environment.Is64BitProcess
                    ? GetClassLongPtr(hWnd, bigIcon ? GCL_HICON : GCL_HICONSM)
                    : GetClassLong32(hWnd, bigIcon ? GCL_HICON : GCL_HICONSM);
            }
            return hIcon;
        }
    }
}