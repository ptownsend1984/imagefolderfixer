using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ImageFolderFixer.Logic;
using System.Threading.Tasks;

namespace ImageFolderFixer.ViewModels
{
    public class MainViewModel : NotificationObject
    {

        private string _inputDirectory;
        public string InputDirectory
        {
            get { return _inputDirectory; }
            set
            {
                if (_inputDirectory == value) return;
                _inputDirectory = value;
                OnPropertyChanged(nameof(InputDirectory));
                _runCommand.RaiseCanExecuteChanged();
                _previewCommand.RaiseCanExecuteChanged();
            }
        }

        private string _outputDirectory;
        public string OutputDirectory
        {
            get { return _outputDirectory; }
            set
            {
                if (_outputDirectory == value) return;
                _outputDirectory = value;
                OnPropertyChanged(nameof(OutputDirectory));
                _runCommand.RaiseCanExecuteChanged();
                _previewCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _recurseInput;
        public bool RecurseInput
        {
            get { return _recurseInput; }
            set
            {
                if (_recurseInput == value) return;
                _recurseInput = value;
                OnPropertyChanged(nameof(RecurseInput));
            }
        }

        private string _log;
        public string Log
        {
            get { return _log; }
            set
            {
                if (_log == value) return;
                _log = value;
                OnPropertyChanged(nameof(Log));
            }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                if (_isRunning == value) return;
                _isRunning = value;
                OnPropertyChanged(nameof(IsRunning));

                _selectInputDirectoryCommand.RaiseCanExecuteChanged();
                _selectOutputDirectoryCommand.RaiseCanExecuteChanged();
                _runCommand.RaiseCanExecuteChanged();
                _previewCommand.RaiseCanExecuteChanged();
            }
        }

        private int _progressCount;
        public int ProgressCount
        {
            get { return _progressCount; }
            set
            {
                if (_progressCount == value) return;
                _progressCount = value;
                OnPropertyChanged(nameof(ProgressCount));
            }
        }

        private int _progressMax;
        public int ProgressMax
        {
            get { return _progressMax; }
            set
            {
                if (_progressMax == value) return;
                _progressMax = value;
                OnPropertyChanged(nameof(ProgressMax));
            }
        }

        private readonly DelegateCommand _selectInputDirectoryCommand;
        public ICommand SelectInputDirectoryCommand { get { return _selectInputDirectoryCommand; } }

        private readonly DelegateCommand _selectOutputDirectoryCommand;
        public ICommand SelectOutputDirectoryCommand { get { return _selectOutputDirectoryCommand; } }

        private readonly DelegateCommand _runCommand;
        public ICommand RunCommand { get { return _runCommand; } }

        private readonly DelegateCommand _previewCommand;
        public ICommand PreviewCommand { get { return _previewCommand; } }
    
        public MainViewModel()
        {
            _selectInputDirectoryCommand = new DelegateCommand(OnSelectInputDirectory, CanSelectInputDirectory);
            _selectOutputDirectoryCommand = new DelegateCommand(OnSelectOutputDirectory, CanSelectOutputDirectory);
            _runCommand = new DelegateCommand(OnRun, CanRun);
            _previewCommand = new DelegateCommand(OnPreview, CanPreview);
        }

        private void OnSelectInputDirectory()
        {
            if (!CanSelectInputDirectory())
                return;

            try
            {
                var path = GetDirectory(this.InputDirectory);
                if (string.IsNullOrEmpty(path))
                    return;

                this.InputDirectory = path;
            }
            catch (Exception ex)
            {
                AppendLog($"Exception: {ex.ToString()}");
            }
        }
        private bool CanSelectInputDirectory()
        {
            return !IsRunning;
        }
        private void OnSelectOutputDirectory()
        {
            if (!CanSelectOutputDirectory())
                return;

            try
            {
                var path = GetDirectory(this.OutputDirectory);
                if (string.IsNullOrEmpty(path))
                    return;

                this.OutputDirectory = path;
            }
            catch (Exception ex)
            {
                AppendLog($"Exception: {ex.ToString()}");
            }
        }
        private bool CanSelectOutputDirectory()
        {
            return !IsRunning;
        }
        private string GetDirectory(string currentValue)
        {
            if (string.IsNullOrEmpty(currentValue))
                currentValue = System.Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            var fileDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Choose folder",
                SelectedPath = currentValue
            };

            var owner = ((App)App.Current).AppWindow;
            if (fileDialog.ShowDialog(owner) != System.Windows.Forms.DialogResult.OK)
                return null;

            return fileDialog.SelectedPath;
        }

        private async void OnRun()
        {
            if (!CanRun())
                return;

            await RunLogic(false);
        }
        private bool CanRun()
        {
            return !IsRunning && !string.IsNullOrWhiteSpace(InputDirectory) && !string.IsNullOrWhiteSpace(OutputDirectory);
        }

        private async void OnPreview()
        {
            if (!CanPreview())
                return;

            await RunLogic(true);
        }
        private bool CanPreview()
        {
            return CanRun();
        }

        private async Task RunLogic(bool isPreview)
        {
            if (this.IsRunning)
                return;

            this.IsRunning = true;
            try
            {
                this.ProgressCount = 0;
                this.ProgressMax = 0;

                var parameters = new FixerLogicParameters(this.InputDirectory, this.OutputDirectory, this.RecurseInput, isPreview);
                var logic = new FixerLogic();
                logic.NotifyMessage += Logic_NotifyMessage;
                logic.NotifyMaxCount += Logic_NotifyMaxCount;
                logic.NotifyProcessed += Logic_NotifyProcessed;
                try
                {
                    await Task.Run(() => logic.Execute(parameters));
                }
                catch (Exception ex)
                {
                    AppendLog($"Exception: {ex.ToString()}");
                }
                finally
                {
                    logic.NotifyMessage -= Logic_NotifyMessage;
                    logic.NotifyMaxCount -= Logic_NotifyMaxCount;
                    logic.NotifyProcessed -= Logic_NotifyProcessed;
                }
            }
            finally
            {
                this.IsRunning = false;
            }
        }

        private void Logic_NotifyMessage(object sender, EventArgs<string> e)
        {
            this.AppendLog(e.Data);
        }
        private void Logic_NotifyMaxCount(object sender, EventArgs<int> e)
        {
            this.ProgressMax = e.Data;
        }
        private void Logic_NotifyProcessed(object sender, EventArgs e)
        {
            this.ProgressCount++;
        }

        private void AppendLog(string text)
        {
            if (string.IsNullOrEmpty(this.Log))
            {
                this.Log = text;
            }
            else
            {
                this.Log += Environment.NewLine + text;
            }
        }
    }
}