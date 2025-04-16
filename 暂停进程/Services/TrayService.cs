using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ProcessSuspender.Services
{
    public class TrayService : ITrayService
    {
        private readonly NotifyIcon _notifyIcon;
        private bool _disposed;

        public TrayService()
        {
            _notifyIcon = new NotifyIcon();
        }

        /// <summary>
        /// 初始化托盘图标
        /// </summary>
        public void Initialize(EventHandler clickHandler)
        {
            _notifyIcon.Icon = LoadIconFromResource();
            _notifyIcon.Visible = false;
            _notifyIcon.Click += clickHandler;
        }

        /// <summary>
        /// 显示托盘图标
        /// </summary>
        public void Show()
        {
            _notifyIcon.Visible = true;
        }

        /// <summary>
        /// 隐藏托盘图标
        /// </summary>
        public void Hide()
        {
            _notifyIcon.Visible = false;
        }

        /// <summary>
        /// 加载托盘图标
        /// </summary>
        private Icon LoadIconFromResource()
        {
            try
            {
                using (var stream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/pm.ico"))?.Stream)
                {
                    if (stream == null)
                    {
                        throw new FileNotFoundException("无法找到嵌入资源：Resources/pm.ico");
                    }
                    return new Icon(stream);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载托盘图标失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return SystemIcons.Application;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _notifyIcon.Dispose();
                _disposed = true;
            }
        }
    }
}