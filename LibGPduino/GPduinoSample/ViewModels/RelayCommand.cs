using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LibGPduinoTest.ViewModels
{
    class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;

        private readonly Func<object, bool> _canEcecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canEcecute = canExecute;
        }

        public RelayCommand(Action<object> execute, Func<bool> canExecute) : this(execute, p => canExecute()) { }

        public RelayCommand(Action execute, Func<object, bool> canExecute = null) : this(p => execute(), canExecute) { }

        public RelayCommand(Action execute, Func<bool> canExecute) : this(execute, p => canExecute()) { }

        public bool CanExecute(object parameter)
        {
            return _canEcecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            _execute?.Invoke(parameter);
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
