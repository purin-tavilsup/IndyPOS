namespace IndyPOS.Windows.Forms.UI;

/// <summary>
/// Puts the menu buttons a store shows into the menu's slots, top first, so a hidden button leaves no gap.
/// The slots are the tops the laid-out form gave its buttons, because display scaling moves them away from
/// the designer's numbers. The caller passes the shown buttons rather than this reading
/// <see cref="Control.Visible"/>, which is false whenever a parent is hidden.
/// </summary>
public static class MenuLayout
{
    public static void Stack(IReadOnlyList<Control> shownButtonsTopToBottom, IReadOnlyList<int> slotTops)
    {
        if (shownButtonsTopToBottom.Count > slotTops.Count)
            throw new ArgumentException("More menu buttons than menu slots.", nameof(shownButtonsTopToBottom));

        for (var i = 0; i < shownButtonsTopToBottom.Count; i++)
            shownButtonsTopToBottom[i].Top = slotTops[i];
    }

    /// <summary>The same for a row of tabs: puts the shown tabs into the row's slots, left first.</summary>
    public static void Row(IReadOnlyList<Control> shownLeftToRight, IReadOnlyList<int> slotLefts)
    {
        if (shownLeftToRight.Count > slotLefts.Count)
            throw new ArgumentException("More tabs than tab slots.", nameof(shownLeftToRight));

        for (var i = 0; i < shownLeftToRight.Count; i++)
            shownLeftToRight[i].Left = slotLefts[i];
    }
}
