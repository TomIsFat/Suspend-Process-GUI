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
        private string _toggleSuspendText;
        private readonly MainWindow _mainWindow;
        private readonly IProcessManager _processManager;
        private readonly IWindowManager _windowManager;


        /// 窗口标题
        /// </summary>
        public string Title { get; set; }


        /// 进程ID
        /// </summary>
        public int ProcessId { get; set; }


        /// 窗口信息
        /// </summary>
        public WindowInfo WindowInfo { get; set; }


        /// 状态（已挂起/正常）
        /// </summary>
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }


        /// 切换挂起/恢复按钮文本
        /// </summary>
        public string ToggleSuspendText
        {
            get => _toggleSuspendText;
            set
            {
                _toggleSuspendText = value;
                OnPropertyChanged(nameof(ToggleSuspendText));
            }
        }


        /// 切换挂起/恢复命令
        /// </summary>
        public ICommand ToggleSuspendCommand { get; }


        /// 移除命令
        /// </summary>
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
        /// </summary>
        public WindowModel(MainWindow mainWindow, IProcessManager processManager, IWindowManager windowManager)
        {
            _mainWindow = mainWindow;
            _processManager = processManager;
            _windowManager = windowManager;
            ToggleSuspendCommand = new RelayCommand(ToggleSuspend);
            RemoveCommand = new RelayCommand(RemoveProcess);
        }


        /// 切换进程挂起/恢复状态
        /// </summary>
        private void ToggleSuspend(object parameter)
        {
            if (Status == "已挂起")
            {
                _processManager.ResumeProcess(WindowInfo.Handle, true);
                _windowManager.ShowWindowNormal(WindowInfo.Handle);

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


        /// 移除进程并恢复
        /// </summary>
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

            _mainWindow.RemoveWindowInfo(WindowInfo);  // 移除窗口信息
        }

        public event PropertyChangedEventHandler PropertyChanged;


        /// 属性变更通知
        /// </summary>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}