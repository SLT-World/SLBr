using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SLBr
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> commandHandler;
        private readonly Func<object, bool> canExecuteHandler;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<object> commandHandler, Func<object, bool> canExecuteHandler = null)
        {
            this.commandHandler = commandHandler;
            this.canExecuteHandler = canExecuteHandler;
        }
        public RelayCommand(Action commandHandler, Func<bool> canExecuteHandler = null)
            : this(_ => commandHandler(), canExecuteHandler == null ? null : new Func<object, bool>(_ => canExecuteHandler()))
        {
        }

        public void Execute(object parameter)
        {
            commandHandler(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return
                canExecuteHandler == null ||
                canExecuteHandler(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }

    public class RelayCommand<T> : RelayCommand
    {
        public RelayCommand(Action<T> commandHandler, Func<T, bool> canExecuteHandler = null)
            : base(o => commandHandler(o is T t ? t : default(T)), canExecuteHandler == null ? null : new Func<object, bool>(o => canExecuteHandler(o is T t ? t : default(T))))
        {
        }
    }
}
