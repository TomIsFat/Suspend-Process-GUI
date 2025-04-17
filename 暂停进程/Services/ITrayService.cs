using System;

namespace ProcessSuspender.Services
{
    public interface ITrayService : IDisposable
    {

        /// 初始化托盘图标
        /// </summary>
        void Initialize(EventHandler clickHandler);


        /// 显示托盘图标
        /// </summary>
        void Show();


        /// 隐藏托盘图标
        /// </summary>
        void Hide();
    }
}