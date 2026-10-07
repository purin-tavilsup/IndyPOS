using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Windows.Forms.UI;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI;

public class MenuLayoutTests
{
    private const int Top = 3;
    private const int Pitch = 121;

    [Fact]
    public void Stack_WithNoButtons_DoesNotThrow()
    {
        var act = () => MenuLayout.Stack([], Top, Pitch);

        act.Should()
           .NotThrow();
    }

    [Fact]
    public void Stack_WithAButtonLeftOut_MovesTheButtonsBelowUp()
    {
        var buttons = Buttons(4);

        MenuLayout.Stack([buttons[0], buttons[2], buttons[3]], Top, Pitch);

        new[] { buttons[0].Top, buttons[2].Top, buttons[3].Top }.Should()
                                                                .Equal(Top, Top + Pitch, Top + 2 * Pitch);
    }

    // Applied again on every login.
    [Fact]
    public void Stack_RunTwice_DoesNotMoveButtonsTwice()
    {
        var buttons = Buttons(4);
        MenuLayout.Stack([buttons[0], buttons[2], buttons[3]], Top, Pitch);

        MenuLayout.Stack([buttons[0], buttons[2], buttons[3]], Top, Pitch);

        buttons[3].Top.Should()
                      .Be(Top + 2 * Pitch);
    }

    // A later login at a store whose features now include the button puts it back in its slot.
    [Fact]
    public void Stack_WithEveryButtonShown_RestoresTheDesignedPositions()
    {
        var buttons = Buttons(4);
        MenuLayout.Stack([buttons[0], buttons[2], buttons[3]], Top, Pitch);

        MenuLayout.Stack(buttons, Top, Pitch);

        buttons.Select(b => b.Top).Should()
                                  .Equal(Top, Top + Pitch, Top + 2 * Pitch, Top + 3 * Pitch);
    }

    private static List<Control> Buttons(int count) =>
        Enumerable.Range(0, count)
                  .Select(i => (Control)new Button { Top = Top + i * Pitch })
                  .ToList();
}
