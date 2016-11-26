using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ImageFolderFixer
{
    public class DelegateCommand : ICommand
    {

        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public void Execute(object parameter)
        {
            if (_execute != null)
                _execute();
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        [System.Diagnostics.DebuggerStepThrough]
        public void RaiseCanExecuteChanged()
        {
            var Handler = this.CanExecuteChanged;
            if (Handler != null)
                Handler(this, EventArgs.Empty);
        }

    }
}