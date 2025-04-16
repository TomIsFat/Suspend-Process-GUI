using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace 暂停进程
{
    public partial class ShortcutBindingWindow : Window
    {
        private Settings _settings;

        public ShortcutBindingWindow(Window owner)
        {
            InitializeComponent();
            Owner = owner;
            _settings = Settings.Load();
            UpdateTextBox();
            ShortcutTextBox.Focus();
        }

        private void UpdateTextBox()
        {
            string shortcut = "";
            if (_settings.Control) shortcut += "Ctrl + ";
            if (_settings.Shift) shortcut += "Shift + ";
            if (_settings.Alt) shortcut += "Alt + ";
            shortcut += _settings.Key.ToString();
            ShortcutTextBox.Text = shortcut;
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;

            // Convert WPF Key to Windows Forms Keys
            Keys key = (Keys)KeyInterop.VirtualKeyFromKey(e.Key);

            // Ignore modifier keys alone
            if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu)
                return;

            _settings.Control = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            _settings.Shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            _settings.Alt = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
            _settings.Key = key;

            UpdateTextBox();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            _settings.Save();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}