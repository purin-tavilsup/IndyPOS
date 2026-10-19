namespace IndyPOS.Windows.Forms.UI;

/// <summary>
/// Puts the menu buttons a store shows into the menu's slots, top first, so a hidden button leaves no gap.
/// The menu's slots come from <see cref="EvenSlots"/> over the laid-out panel's height, so display scaling is
/// already in them; tab rows still pass the tops or lefts the laid-out form gave them. The caller passes the
/// shown buttons rather than this reading <see cref="Control.Visible"/>, which is false whenever a parent is hidden.
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

    /// <summary>
    /// Splits the menu's height into equal slots, <paramref name="gap"/> apart, so every button the menu
    /// can hold fits. Taken from the laid-out panel's height, so display scaling is already in it.
    /// </summary>
    public static (IReadOnlyList<int> Tops, int Height) EvenSlots(int areaHeight, int count, int gap)
    {
        if (count == 0)
            return ([], 0);

        var step = (areaHeight - gap) / count;
        var tops = Enumerable.Range(0, count).Select(i => gap / 2 + i * step).ToArray();
        return (tops, step - gap);
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
