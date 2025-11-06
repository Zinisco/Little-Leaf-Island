using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [SerializeField] GameObject inventoryPanel;
    [SerializeField] Transform slotContainer;
    [SerializeField] GameObject slotPrefab;

    void Start()
    {
        inventoryPanel.SetActive(false);
        IsOpen = false;

        for (int i = 0; i < InventorySystem.I.Slots.Count; i++)
        {
            var ui = Instantiate(slotPrefab, slotContainer);
            ui.GetComponent<InventorySlotUI>().Init(i);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventory();
    }

    void ToggleInventory()
    {
        // If entering inventory, auto-exit expansion mode
        if (!IsOpen && ExpansionModeManager.I != null && ExpansionModeManager.I.IsActive)
            ExpansionModeManager.I.ForceExitExpansion();

        IsOpen = !IsOpen;
        inventoryPanel.SetActive(IsOpen);

        if (IsOpen)
            ToolManager.I.ForceDefaultCursor();
        else
            ToolManager.I.ApplyCursor();
    }

}
