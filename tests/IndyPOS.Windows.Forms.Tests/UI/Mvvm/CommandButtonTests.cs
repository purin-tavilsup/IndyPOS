using System.Windows.Forms;
using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using IndyPOS.Windows.Forms.UI.Mvvm;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Mvvm;

/// <summary>
/// The toolkit's Execute does not check CanExecute, and a disabled button can still receive a queued
/// second click. Every button bound to a ViewModel goes through here, so a double-click on พิมพ์ซ้ำ
/// records one reprint, not two.
/// </summary>
public class CommandButtonTests
{
    [Fact]
    public void Bind_WhenTheCommandCannotExecute_DisablesTheButton()
    {
        using var button = new Button();

        using var binding = CommandButton.Bind(button, new RelayCommand(() => { }, () => false));

        button.Enabled.Should()
                      .BeFalse();
    }

    [Fact]
    public void TryExecute_WhenTheCommandCannotExecute_DoesNotRunIt()
    {
        var runs = 0;
        var command = new RelayCommand(() => runs++, () => false);

        CommandButton.TryExecute(command, null);

        runs.Should()
            .Be(0);
    }

    [Fact]
    public void TryExecute_WhileAnAsyncCommandRuns_DoesNotRunItAgain()
    {
        var runs = 0;
        var pending = new TaskCompletionSource();
        var command = new AsyncRelayCommand(() => { runs++; return pending.Task; });
        CommandButton.TryExecute(command, null);

        CommandButton.TryExecute(command, null);
        // xunit waits for the command's async void to finish, so a never-completed task hangs the run.
        pending.SetResult();

        runs.Should()
            .Be(1);
    }

    [Fact]
    public void Dispose_AfterBind_StopsFollowingTheCommand()
    {
        using var button = new Button();
        var canExecute = true;
        var command = new RelayCommand(() => { }, () => canExecute);
        var binding = CommandButton.Bind(button, command);
        binding.Dispose();
        canExecute = false;

        command.NotifyCanExecuteChanged();

        button.Enabled.Should()
                      .BeTrue();
    }

    [Fact]
    public void Bind_WhenCanExecuteChanges_FollowsIt()
    {
        using var button = new Button();
        var canExecute = false;
        var command = new RelayCommand(() => { }, () => canExecute);
        using var binding = CommandButton.Bind(button, command);
        canExecute = true;

        command.NotifyCanExecuteChanged();

        button.Enabled.Should()
                      .BeTrue();
    }

    [Fact]
    public void TryExecute_WithAParameter_PassesIt()
    {
        object? received = null;
        var command = new RelayCommand<string>(value => received = value);

        CommandButton.TryExecute(command, "1042");

        received.Should()
                .Be("1042");
    }
}
