using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EcoUtils.Commands;

public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task>    _execute;
    private readonly Predicate<object?>?    _canExecute;
    private readonly Action<Exception>?     _onError;
    private bool _isExecuting;

    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            _isExecuting = value;
            RaiseCanExecuteChanged();
        }
    }

    public AsyncRelayCommand(
        Func<object?, Task> execute,
        Predicate<object?>? canExecute = null,
        Action<Exception>?  onError    = null)
    {
        _execute    = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _onError    = onError;
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public bool CanExecute(object? parameter) =>
        !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        IsExecuting = true;
        try
        {
            await _execute(parameter);
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
        finally
        {
            IsExecuting = false;
        }
    }
}
