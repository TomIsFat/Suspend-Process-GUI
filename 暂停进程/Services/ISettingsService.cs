using System.Windows.Forms;

namespace ProcessSuspender.Services
{
    public interface ISettingsService
    {

        /// 获取当前设置
        /// </summary>
        Settings GetSettings();


        /// 保存设置
        /// </summary>
        void SaveSettings(Settings settings);
    }

    public class Settings
    {
        public Keys Key { get; set; } = Keys.Oemtilde;
        public bool Control { get; set; } = true;
        public bool Shift { get; set; } = false;
        public bool Alt { get; set; } = false;
    }
}