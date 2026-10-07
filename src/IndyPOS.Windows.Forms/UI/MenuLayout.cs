namespace IndyPOS.Windows.Forms.UI;

/// <summary>
/// Stacks the menu buttons a store shows, one pitch apart, so a hidden button leaves no gap. The caller
/// passes the shown buttons rather than this reading <see cref="Control.Visible"/>, which is false
/// whenever a parent is hidden.
/// </summary>
public static class MenuLayout
{
    public static void Stack(IReadOnlyList<Control> shownButtonsTopToBottom, int top, int pitch)
    {
        var next = top;
        foreach (var button in shownButtonsTopToBottom)
        {
            button.Top = next;
            next += pitch;
        }
    }
}
