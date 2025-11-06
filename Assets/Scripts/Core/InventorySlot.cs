[System.Serializable]
public class InventorySlot
{
    public ItemDefinition item;
    public int quantity;

    public bool IsEmpty => item == null || quantity <= 0;
    public bool IsFull => item != null && quantity >= item.maxStack;

    public void Clear()
    {
        item = null;
        quantity = 0;
    }
}
