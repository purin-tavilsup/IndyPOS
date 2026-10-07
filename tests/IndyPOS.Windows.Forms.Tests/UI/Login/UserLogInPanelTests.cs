using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Windows.Forms.UI;
using IndyPOS.Windows.Forms.UI.Login;
using IndyPOS.Windows.Forms.UI.ModernUI;
using Moq;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Login;

/// <summary>
/// The username box once held "ผู้ใช้งาน" as real text standing in for a placeholder: it sat in the
/// way while typing, and a Log In with nothing typed sent it as the username.
/// </summary>
public class UserLogInPanelTests
{
    [Fact]
    public void LogInPanel_WhenCreated_LeavesTheUsernameEmpty()
    {
        using var panel = CreatePanel(new Mock<IFirstLoginCoordinator>());

        UsernameOf(panel).Texts.Should()
                               .BeEmpty();
    }

    [Fact]
    public void LogIn_WithNoUsernameTyped_SendsAnEmptyUsername()
    {
        // A successful login, so no modal "login failed" dialog blocks the test thread.
        var coordinator = new Mock<IFirstLoginCoordinator>();
        coordinator.Setup(c => c.LogInAsync(It.IsAny<string>(), It.IsAny<string>()))
                   .ReturnsAsync(true);

        RunShown(coordinator, panel => ClickLogIn(panel));

        coordinator.Verify(c => c.LogInAsync(string.Empty, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void LogInPanel_WhenShown_FocusesTheUsername()
    {
        Control? focused = null;
        ModernComboBox? username = null;

        RunShown(new Mock<IFirstLoginCoordinator>(), panel =>
        {
            username = UsernameOf(panel);
            focused = panel.ActiveControl;
        });

        focused.Should()
               .BeSameAs(username);
    }

    private static UserLogInPanel CreatePanel(Mock<IFirstLoginCoordinator> coordinator) =>
        new(coordinator.Object, new MessageForm());

    private static ModernComboBox UsernameOf(Control panel) =>
        (ModernComboBox)panel.Controls.Find("UsersComboBox", searchAllChildren: true).Single();

    private static void ClickLogIn(Control panel)
    {
        ((Button)panel.Controls.Find("LogInButton", searchAllChildren: true).Single()).PerformClick();
        System.Windows.Forms.Application.DoEvents();
    }

    // Focus and PerformClick need a real, shown window on an STA thread.
    private static void RunShown(Mock<IFirstLoginCoordinator> coordinator, Action<UserLogInPanel> act)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new Form();
                var panel = CreatePanel(coordinator);
                form.Controls.Add(panel);
                form.Show();
                System.Windows.Forms.Application.DoEvents();
                act(panel);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
            throw new InvalidOperationException("The STA test body failed.", failure);
    }
}
