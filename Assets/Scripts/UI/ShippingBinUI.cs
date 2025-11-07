using UnityEngine;

public class ShippingBinUI : MonoBehaviour
{
    [SerializeField] Transform slotContainer;   // Grid parent
    [SerializeField] GameObject slotPrefab;      // ShippingSlotUI prefab

    void Start()
    {
        // Spawn one UI object for each shipping slot
        for (int i = 0; i < InventorySystem.I.ShippingSlots.Count; i++)
        {
            var ui = Instantiate(slotPrefab, slotContainer);
            ui.GetComponent<ShippingSlotUI>().Init(i);
        }
    }
}
