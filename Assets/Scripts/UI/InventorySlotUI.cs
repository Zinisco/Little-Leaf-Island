using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text qtyText;

    int slotIndex;

    public void Init(int index)
    {
        slotIndex = index;
        Refresh();
        InventorySystem.OnInventoryChanged += Refresh;
    }

    void OnDestroy()
    {
        InventorySystem.OnInventoryChanged -= Refresh;
    }

    void Refresh()
    {
        var slot = InventorySystem.I.Slots[slotIndex];

        if (slot.IsEmpty)
        {
            iconImage.enabled = false;
            qtyText.text = "";
        }
        else
        {
            iconImage.enabled = true;
            iconImage.sprite = slot.item.icon;
            qtyText.text = slot.quantity > 1 ? slot.quantity.ToString() : "";
        }
    }
}
