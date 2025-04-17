using System;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ProcessSuspender.Services
{
    public class SettingsService : ISettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ProcessSuspender",
            "settings.json");


        /// 获取当前设置
        /// </summary>
        public Settings GetSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载设置失败: {ex.Message}");
            }
            return new Settings();
        }


        /// 保存设置
        /// </summary>
        public void SaveSettings(Settings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存设置失败: {ex.Message}");
            }
        }
    }
}