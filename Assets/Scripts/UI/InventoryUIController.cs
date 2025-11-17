using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [SerializeField] GameObject inventoryPanel;
    [SerializeField] Transform slotContainer;
    [SerializeField] GameObject slotPrefab;
    [SerializeField] UnityEngine.UI.Button sellNowButton;

    public static InventoryUIController Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

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
        // Toggle with I
        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventory();

        // Allow closing with Escape (only if open)
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseInventory();
    }

    void DoSellNow()
    {
        var result = InventorySystem.I.SellShippingContents();
        Debug.Log(BuildBreakdown(result));
    }

    void ToggleInventory()
    {
        IsOpen = !IsOpen;
        inventoryPanel.SetActive(IsOpen);

        if (TimeManager.I != null)
            TimeManager.I.SetPaused(IsOpen);

        if (IsOpen)
            ToolManager.I.ForceDefaultCursor();
        else if (!ExpansionModeManager.I || !ExpansionModeManager.I.IsActive)
            ToolManager.I.ApplyCursor();
    }

    string BuildBreakdown(InventorySystem.ShippingSaleResult result)
    {
        System.Text.StringBuilder sb = new();
        sb.AppendLine("Sold:");
        foreach (var e in result.entries)
            sb.AppendLine($"{e.item.displayName} ×{e.quantity} -> {e.subtotal} coins");

        sb.AppendLine("-----------------------");
        sb.AppendLine($"Total: {result.total} coins");
        return sb.ToString();
    }

    public static void CloseInventory()
    {
        if (Instance == null) return;

        Instance.inventoryPanel.SetActive(false);
        IsOpen = false;

        // Resume time so placement Update() works
        if (TimeManager.I != null)
            TimeManager.I.SetPaused(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public System.Collections.IEnumerator DelayedStartPlacement(ItemDefinition item)
    {
        yield return null; // wait one frame so the inventory panel hides first

        var placer = PlaceableItemPlacer.I;
        if (placer != null)
            placer.StartPlacing(item);
        else
            Debug.LogWarning("PlaceableItemPlacer reference not assigned in InventoryUIController!");

    }

}
