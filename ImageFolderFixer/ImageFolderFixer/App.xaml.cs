using ImageFolderFixer.ViewModels;
using ImageFolderFixer.Windows;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ImageFolderFixer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private MainWindow _mainWindow;      
        public MainWindow AppWindow { get { return _mainWindow; } }
          
        private MainViewModel _mainViewModel;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            var settings = ImageFolderFixer.Properties.Settings.Default;
            if (settings.NeedsUpgrade)
            {
                settings.Upgrade();
                settings.NeedsUpgrade = false;
                settings.Save();
            }

            var viewModel = new MainViewModel
            {
                InputDirectory = settings.LastInputDirectory,
                OutputDirectory = settings.LastOutputDirectory,
                RecurseInput = settings.RecurseInput
            };
            var window = new MainWindow();
            window.DataContext = viewModel;

            this._mainViewModel = viewModel;
            this._mainWindow = window;
            this.MainWindow = window;
            window.Show();
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            if (_mainViewModel != null)
            {
                var settings = ImageFolderFixer.Properties.Settings.Default;
                settings.LastInputDirectory = _mainViewModel.InputDirectory;
                settings.LastOutputDirectory = _mainViewModel.OutputDirectory;
                settings.RecurseInput = _mainViewModel.RecurseInput;
                try
                {
                    settings.Save();
                }
                catch { }

                _mainViewModel = null;
            }

            base.OnExit(e);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled error: {e.ToString()}");
        }

    }
}
