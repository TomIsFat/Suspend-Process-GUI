using System;

namespace ProcessSuspender.Services
{
    public interface ITrayService : IDisposable
    {
        /// <summary>
        /// 初始化托盘图标
        /// </summary>
        void Initialize(EventHandler clickHandler);

        /// <summary>
        /// 显示托盘图标
        /// </summary>
        void Show();

        /// <summary>
        /// 隐藏托盘图标
        /// </summary>
        void Hide();
    }
}