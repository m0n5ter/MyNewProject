using System.Windows.Input;

namespace MyNewProject.Mvvm;

internal class UiCommand : UiCommand<object>
{
    public UiCommand(Action action, Func<bool>? canExecute = null) : base(_ => action(), canExecute== null? null: _ => canExecute())
    {
    }
}

internal class UiCommand<TParameter>: ICommand where TParameter : class
{
    private readonly Action<TParameter?> _action;
    private readonly Predicate<TParameter?>? _canExecute;

    public UiCommand(Action<TParameter?> action, Predicate<TParameter?>? canExecute = null)
    {
        _action = action;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        if (_canExecute == null)
            return true;

        return _canExecute(parameter as TParameter);
    }

    public void Execute(object? parameter)
    {
        _action(parameter as TParameter);
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}