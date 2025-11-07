using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [SerializeField] GameObject inventoryPanel;
    [SerializeField] Transform slotContainer;
    [SerializeField] GameObject slotPrefab;
    [SerializeField] UnityEngine.UI.Button sellNowButton;

    void Start()
    {
        inventoryPanel.SetActive(false);
        IsOpen = false;

        for (int i = 0; i < InventorySystem.I.Slots.Count; i++)
        {
            var ui = Instantiate(slotPrefab, slotContainer);
            ui.GetComponent<InventorySlotUI>().Init(i);
        }

        if (sellNowButton != null)
            sellNowButton.onClick.AddListener(DoSellNow);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventory();
    }

    void DoSellNow()
    {
        var result = InventorySystem.I.SellShippingContents();
        // For now, log a nice breakdown:
        Debug.Log(BuildBreakdown(result));
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

    string BuildBreakdown(InventorySystem.ShippingSaleResult result)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Sold:");
        foreach (var e in result.entries)
            sb.AppendLine($"{e.item.displayName} ×{e.quantity} -> {e.subtotal} coins");

        sb.AppendLine("-----------------------");
        sb.AppendLine($"Total: {result.total} coins");
        return sb.ToString();
    }


}
