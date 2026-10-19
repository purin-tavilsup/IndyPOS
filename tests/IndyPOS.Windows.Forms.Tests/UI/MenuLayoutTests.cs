using System.Drawing;
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

    // The control panel the designer gives the menu, and its nine buttons once ลิ้นชักเก็บเงิน joins (2026-10-19).
    private const int MenuAreaHeight = 972;
    private const int MenuButtons = 9;
    private const int MenuGap = 6;
    private const int IconHeight = 50;      // every menu icon is a *_50 resource
    private const int LongestCaption = 0;   // index into Captions
    private static readonly string[] Captions = ["ลิ้นชักเก็บเงิน", "ลูกหนี้การค้า", "ออกจากระบบ"];

    [Fact]
    public void EvenSlots_WithNoButtons_ReturnsNoSlots()
    {
        var (tops, _) = MenuLayout.EvenSlots(MenuAreaHeight, 0, MenuGap);

        tops.Should()
            .BeEmpty();
    }

    [Fact]
    public void EvenSlots_WithNineButtons_EndsInsideTheMenu()
    {
        var (tops, height) = MenuLayout.EvenSlots(MenuAreaHeight, MenuButtons, MenuGap);

        (tops[^1] + height).Should()
                           .BeLessThanOrEqualTo(MenuAreaHeight);
    }

    [Fact]
    public void EvenSlots_WithNineButtons_LeavesNoOverlap()
    {
        var (tops, height) = MenuLayout.EvenSlots(MenuAreaHeight, MenuButtons, MenuGap);

        tops.Zip(tops.Skip(1), (top, next) => next - top).Should()
                                                         .OnlyContain(step => step >= height);
    }

    // Pond, 2026-10-19: shorter slots are fine as long as each button's icon and caption still fit.
    [Fact]
    public void EvenSlots_WithNineButtons_FitsAnIconAndItsCaption()
    {
        using var font = new Font("FC Subject [Non-commercial] Reg", 12F);
        var captionHeight = Captions.Max(c => TextRenderer.MeasureText(c, font).Height);

        var (_, height) = MenuLayout.EvenSlots(MenuAreaHeight, MenuButtons, MenuGap);

        height.Should()
              .BeGreaterThanOrEqualTo(IconHeight + captionHeight + 2 * MenuGap, $"the longest caption is {Captions[LongestCaption]}");
    }

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

    private static readonly int[] TabSlots = [13, 262, 511, 755, 999];

    [Fact]
    public void Row_WithMoreButtonsThanSlots_Throws()
    {
        var act = () => MenuLayout.Row(Buttons(6), TabSlots);

        act.Should()
           .Throw<ArgumentException>();
    }

    [Fact]
    public void Row_WithATabLeftOut_MovesTheTabsAfterItLeft()
    {
        var tabs = Enumerable.Range(0, 5).Select(i => (Control)new Button { Left = TabSlots[i] }).ToList();

        MenuLayout.Row([tabs[0], tabs[1], tabs[2], tabs[4]], TabSlots);

        tabs[4].Left.Should()
                    .Be(755);
    }

    private static List<Control> Buttons(int count) =>
        Enumerable.Range(0, count)
                  .Select(i => (Control)new Button { Top = DesignedSlots.ElementAtOrDefault(i) })
                  .ToList();
}
