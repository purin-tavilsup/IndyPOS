using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Windows.Forms.UI;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI;

public class MenuLayoutTests
{
    private static readonly int[] DesignedSlots = [3, 124, 245, 366];

    // At 125% display scale WinForms scales the designer's tops (124 -> about 155), so the slots must
    // come from the laid-out form, never from the designer's numbers.
    private static readonly int[] ScaledSlots = [4, 155, 306, 457];

    [Fact]
    public void Stack_WithNoButtons_DoesNotThrow()
    {
        var act = () => MenuLayout.Stack([], DesignedSlots);

        act.Should()
           .NotThrow();
    }

    [Fact]
    public void Stack_WithMoreButtonsThanSlots_Throws()
    {
        var act = () => MenuLayout.Stack(Buttons(5), DesignedSlots);

        act.Should()
           .Throw<ArgumentException>();
    }

    [Fact]
    public void Stack_WithScaledSlots_PutsEachButtonInAScaledSlot()
    {
        var buttons = Buttons(4);

        MenuLayout.Stack([buttons[0], buttons[2], buttons[3]], ScaledSlots);

        new[] { buttons[0].Top, buttons[2].Top, buttons[3].Top }.Should()
                                                                .Equal(4, 155, 306);
    }

    [Fact]
    public void Stack_WithAButtonLeftOut_MovesTheButtonsBelowUp()
    {
        var buttons = Buttons(4);

        MenuLayout.Stack([buttons[0], buttons[2], buttons[3]], DesignedSlots);

        new[] { buttons[0].Top, buttons[2].Top, buttons[3].Top }.Should()
                                                                .Equal(3, 124, 245);
    }

    // Applied again on every login.
    [Fact]
    public void Stack_RunTwice_DoesNotMoveButtonsTwice()
    {
        var buttons = Buttons(4);
        MenuLayout.Stack([buttons[0], buttons[2], buttons[3]], DesignedSlots);

        MenuLayout.Stack([buttons[0], buttons[2], buttons[3]], DesignedSlots);

        buttons[3].Top.Should()
                      .Be(245);
    }

    // A later login at a store whose features now include the button puts it back in its slot.
    [Fact]
    public void Stack_WithEveryButtonShown_RestoresTheDesignedPositions()
    {
        var buttons = Buttons(4);
        MenuLayout.Stack([buttons[0], buttons[2], buttons[3]], DesignedSlots);

        MenuLayout.Stack(buttons, DesignedSlots);

        buttons.Select(b => b.Top).Should()
                                  .Equal(DesignedSlots);
    }

    private static List<Control> Buttons(int count) =>
        Enumerable.Range(0, count)
                  .Select(i => (Control)new Button { Top = DesignedSlots.ElementAtOrDefault(i) })
                  .ToList();
}
