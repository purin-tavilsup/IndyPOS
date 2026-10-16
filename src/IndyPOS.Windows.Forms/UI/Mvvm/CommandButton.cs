using System.Windows.Input;

namespace IndyPOS.Windows.Forms.UI.Mvvm;

/// <summary>
/// Binds a button to a ViewModel command (spec §7's Click → Execute helper): Enabled follows
/// CanExecute, and a click runs the command only if it can execute now. The toolkit's Execute does
/// not check, so without this a double-click could run an async command twice.
/// </summary>
public static class CommandButton
{
    public static IDisposable Bind(Control button, ICommand command, Func<object?>? parameter = null)
    {
        object? Parameter() => parameter?.Invoke();
        void Refresh(object? sender, EventArgs e) => button.Enabled = command.CanExecute(Parameter());
        void Click(object? sender, EventArgs e) => TryExecute(command, Parameter());

        command.CanExecuteChanged += Refresh;
        button.Click += Click;
        Refresh(null, EventArgs.Empty);

        return new Unbinding(() =>
        {
            command.CanExecuteChanged -= Refresh;
            button.Click -= Click;
        });
    }

    public static bool TryExecute(ICommand command, object? parameter)
    {
        if (!command.CanExecute(parameter))
            return false;

        command.Execute(parameter);
        return true;
    }

    private sealed class Unbinding(Action unbind) : IDisposable
    {
        private Action? _unbind = unbind;

        public void Dispose()
        {
            _unbind?.Invoke();
            _unbind = null;
        }
    }
}
