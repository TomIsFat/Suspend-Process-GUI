using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using ProcessSuspender.Services;

namespace ProcessSuspender
{
    public partial class ShortcutBindingWindow : Window
    {
        private readonly ISettingsService _settingsService;
        private Settings _settings;


        /// 构造函数
        /// </summary>
        public ShortcutBindingWindow(Window owner, ISettingsService settingsService)
        {
            InitializeComponent();
            Owner = owner;
            _settingsService = settingsService;
            _settings = _settingsService.GetSettings();
            UpdateTextBox();
            ShortcutTextBox.Focus();
        }


        /// 更新快捷键显示文本
        /// </summary>
        private void UpdateTextBox()
        {
            string shortcut = "";
            if (_settings.Control) shortcut += "Ctrl + ";
            if (_settings.Shift) shortcut += "Shift + ";
            if (_settings.Alt) shortcut += "Alt + ";
            shortcut += _settings.Key.ToString();
            ShortcutTextBox.Text = shortcut;
        }


        /// 处理键盘按下事件
        /// </summary>
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;

            Keys key = (Keys)KeyInterop.VirtualKeyFromKey(e.Key);
            if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu)
                return;

            _settings.Control = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            _settings.Shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            _settings.Alt = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
            _settings.Key = key;

            UpdateTextBox();
        }


        /// 处理确定按钮点击
        /// </summary>
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            _settingsService.SaveSettings(_settings);
            DialogResult = true;
            Close();
        }


        /// 处理取消按钮点击
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}