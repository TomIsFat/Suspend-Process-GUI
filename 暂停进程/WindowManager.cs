using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.Imaging;

namespace 暂停进程
{
    public static class WindowManager
    {
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(Point p);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        private const int LOGPIXELSX = 88;

        enum GetAncestorFlags
        {
            GetRoot = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static float GetWindowDpiScale(IntPtr hwnd)
        {
            if (Environment.OSVersion.Version.Major >= 10)
            {
                try
                {
                    return GetDpiForWindow(hwnd) / 96f;
                }
                catch { }
            }

            IntPtr hdc = GetDC(IntPtr.Zero);
            if (hdc != IntPtr.Zero)
            {
                int dpiX = GetDeviceCaps(hdc, LOGPIXELSX);
                ReleaseDC(IntPtr.Zero, hdc);
                return dpiX / 96f;
            }
            return 1.0f;
        }

        public static Rectangle GetLogicalWindowRect(IntPtr hwnd)
        {
            GetWindowRect(hwnd, out RECT rect);
            float scale = GetWindowDpiScale(hwnd);
            return new Rectangle(
                (int)(rect.Left / scale),
                (int)(rect.Top / scale),
                (int)((rect.Right - rect.Left) / scale),
                (int)((rect.Bottom - rect.Top) / scale));
        }

        public static IntPtr GetWindowUnderCursor()
        {
            GetCursorPos(out Point point);
            return WindowFromPoint(point);
        }

        public static Bitmap CaptureWindow(IntPtr handle)
        {
            GetWindowRect(handle, out RECT rect);
            var bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
                return new Bitmap(bitmap);
            }
        }

        public static BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        public static IntPtr GetTopLevelWindowUnderCursor()
        {
            return GetAncestor(GetWindowUnderCursor(), GetAncestorFlags.GetRoot);
        }

        public static int GetWindowProcessId(IntPtr hwnd)
        {
            GetWindowThreadProcessId(hwnd, out int processId);
            return processId;
        }

        public static void HideWindow(IntPtr hwnd)
        {
            ShowWindow(hwnd, SW_HIDE);
        }

        public static void ShowWindowNormal(IntPtr hwnd)
        {
            ShowWindow(hwnd, SW_SHOW);
        }

        public static void MoveExternalWindow(IntPtr hwnd, int x, int y, int nWidth, int nHeight, bool bRepaint)
        {
            MoveWindow(hwnd, x, y, nWidth, nHeight, bRepaint);
        }

        public static string GetWindowTitle(IntPtr hwnd)
        {
            StringBuilder buff = new StringBuilder(256);
            return GetWindowText(hwnd, buff, buff.Capacity) > 0 ? buff.ToString() : string.Empty;
        }
    }
}