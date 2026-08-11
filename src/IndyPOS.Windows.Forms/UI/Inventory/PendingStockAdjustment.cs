namespace IndyPOS.Windows.Forms.UI.Inventory;

/// <summary>
/// Tracks what the operator has done to a product's stock figure before saving.
/// The form shows DisplayedQuantity and sends Delta, never the target: a target computed
/// against a quantity read when the dialog opened would absorb any sale made in between.
/// </summary>
public class PendingStockAdjustment
{
    private int _startingQuantity;

    public PendingStockAdjustment(int startingQuantity)
    {
        _startingQuantity = startingQuantity;
        DisplayedQuantity = startingQuantity;
    }

    public int DisplayedQuantity { get; private set; }

    public int Delta => DisplayedQuantity - _startingQuantity;

    public bool HasChange => Delta != 0;

    public void Increase(int amount) => DisplayedQuantity += amount;

    /// <summary>
    /// Forgets the pending delta by moving the baseline up to whatever is currently
    /// displayed. Used after a failed AdjustQuantityAsync call: the operator has just been
    /// told the quantity change did not go through, so a blind re-Save must not resend the
    /// same delta and double-apply it. Discarding the unapplied intent is the safe direction
    /// - the operator can redo it deliberately.
    /// </summary>
    public void RebaseToDisplayedQuantity() => _startingQuantity = DisplayedQuantity;
}
