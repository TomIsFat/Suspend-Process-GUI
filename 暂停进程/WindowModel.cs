using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace 暂停进程
{
    public class WindowModel : INotifyPropertyChanged
    {
        private string _status;
        private string _toggleSuspendText;
        private readonly MainWindow _mainWindow;

        public string Title { get; set; }
        public int ProcessId { get; set; }
        public WindowInfo WindowInfo { get; set; }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public string ToggleSuspendText
        {
            get => _toggleSuspendText;
            set
            {
                _toggleSuspendText = value;
                OnPropertyChanged(nameof(ToggleSuspendText));
            }
        }

        public ICommand ToggleSuspendCommand { get; }
        public ICommand RemoveCommand { get; }

        public class RelayCommand : ICommand
        {
            private readonly Action<object> _execute;

            public RelayCommand(Action<object> execute)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            }

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter) => _execute(parameter);

            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }

        public WindowModel(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            ToggleSuspendCommand = new RelayCommand(ToggleSuspend);
            RemoveCommand = new RelayCommand(RemoveProcess);
        }

        private void ToggleSuspend(object parameter)
        {
            if (Status == "已挂起")
            {
                PauseFunction.SuspendOrResumeProcess(false, WindowInfo.Handle, null, true);
                WindowManager.ShowWindowNormal(WindowInfo.Handle);

                // 查找并关闭对应的 ScreenshotWindow
                var screenshotWindow = Application.Current.Windows.OfType<ScreenshotWindow>()
                    .FirstOrDefault(w => w.DataContext == WindowInfo);
                screenshotWindow?.Close();

                Status = "正常";
                ToggleSuspendText = "挂起";
                _mainWindow.RemoveWindowInfo(WindowInfo);
            }
            else
            {
                _mainWindow.SuspendProcess();
            }
        }

        private void RemoveProcess(object parameter)
        {
            // 恢复进程
            PauseFunction.SuspendOrResumeProcess(false, WindowInfo.Handle, null, true);

            // 显示所有相关窗口
            foreach (var handle in WindowInfo.WindowHandles)
            {
                WindowManager.ShowWindowNormal(handle);
            }

            // 查找并关闭对应的 ScreenshotWindow
            var screenshotWindow = Application.Current.Windows.OfType<ScreenshotWindow>()
                .FirstOrDefault(w => w.DataContext == WindowInfo);
            screenshotWindow?.Close();

            // 从主窗口移除
            _mainWindow.RemoveWindowInfo(WindowInfo);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}