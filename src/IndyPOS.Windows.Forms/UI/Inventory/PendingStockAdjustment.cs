namespace IndyPOS.Windows.Forms.UI.Inventory;

/// <summary>
/// Tracks what the operator has done to a product's stock figure before saving.
/// The form shows DisplayedQuantity and sends Delta, never the target: a target computed
/// against a quantity read when the dialog opened would absorb any sale made in between.
/// </summary>
public class PendingStockAdjustment
{
    private readonly int _startingQuantity;

    public PendingStockAdjustment(int startingQuantity)
    {
        _startingQuantity = startingQuantity;
        DisplayedQuantity = startingQuantity;
    }

    public int DisplayedQuantity { get; private set; }

    public int Delta => DisplayedQuantity - _startingQuantity;

    public bool HasChange => Delta != 0;

    public void Increase(int amount) => DisplayedQuantity += amount;
}
