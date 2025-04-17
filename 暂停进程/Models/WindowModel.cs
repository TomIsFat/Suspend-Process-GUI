using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ProcessSuspender.Services;
using 暂停进程;

namespace ProcessSuspender.Models
{
    public class WindowModel : INotifyPropertyChanged
    {
        private string _status;
        private string _toggleStatusText;
        private readonly MainWindow _mainWindow;
        private readonly IProcessManager _processManager;
        private readonly IWindowManager _windowManager;

        /// 窗口标题
        public string Title { get; set; }

        /// 进程ID
        public int ProcessId { get; set; }

        /// 窗口信息
        public WindowInfo WindowInfo { get; set; }

        /// 状态（运行中/冻结中）
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        /// 切换状态按钮文本
        public string ToggleStatusText
        {
            get => _toggleStatusText;
            set
            {
                _toggleStatusText = value;
                OnPropertyChanged(nameof(ToggleStatusText));
            }
        }

        /// 切换挂起/恢复命令
        public ICommand ToggleSuspendCommand { get; }

        /// 移除命令
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

        /// 构造函数
        public WindowModel(MainWindow mainWindow, IProcessManager processManager, IWindowManager windowManager)
        {
            _mainWindow = mainWindow;
            _processManager = processManager;
            _windowManager = windowManager;
            ToggleSuspendCommand = new RelayCommand(ToggleSuspend);
            RemoveCommand = new RelayCommand(RemoveProcess);
            Status = "运行中";
            ToggleStatusText = "冻结中";
        }

        /// 切换进程冻结/运行状态
        private void ToggleSuspend(object parameter)
        {
            if (Status == "冻结中")
            {
                _processManager.ResumeProcess(WindowInfo.Handle, true);
                _windowManager.ShowWindowNormal(WindowInfo.Handle);

                var screenshotWindow = Application.Current.Windows.OfType<ScreenshotWindow>()
                    .FirstOrDefault(w => w.DataContext == WindowInfo);
                screenshotWindow?.Close();

                _mainWindow.RemoveWindowInfo(WindowInfo);
            }
            else
            {
                _mainWindow.SuspendProcess();
            }
        }

        /// 移除进程并恢复
        private void RemoveProcess(object parameter)
        {
            _processManager.ResumeProcess(WindowInfo.Handle, true);
            foreach (var handle in WindowInfo.WindowHandles)
            {
                _windowManager.ShowWindowNormal(handle);
            }

            var screenshotWindow = Application.Current.Windows.OfType<ScreenshotWindow>()
                .FirstOrDefault(w => w.DataContext == WindowInfo);
            screenshotWindow?.Close();

            _mainWindow.RemoveWindowInfo(WindowInfo);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// 属性变更通知
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}