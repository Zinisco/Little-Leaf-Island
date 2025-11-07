using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryDragController : MonoBehaviour
{
    public enum Source { Inventory, Shipping }

    public static InventoryDragController I;

    [Header("Prefab & Parent")]
    public GameObject dragIconPrefab;
    public Transform dragLayer;

    RectTransform dragRect;
    Image dragImage;
    TMP_Text dragQty;

    public int draggedFromIndex = -1;
    public Source dragSource;

    // ---- Partial-drag state ----
    public bool isPartial { get; private set; } = false;          // are we in partial-click mode?
    public ItemDefinition draggedItem { get; private set; } = null; // which item is being dragged
    public int draggedCount { get; private set; } = 0;            // how many currently on cursor

    void Awake() { I = this; }

    void Update()
    {
        if (dragRect != null)
            dragRect.position = Input.mousePosition;
    }

    // Classic full-stack drag start (unchanged)
    public void BeginDrag(Source source, Sprite icon, int quantity, int fromIndex)
    {
        if (dragIconPrefab == null || dragLayer == null) return;

        dragSource = source;
        draggedFromIndex = fromIndex;

        GameObject go = Instantiate(dragIconPrefab, dragLayer);
        dragRect = go.GetComponent<RectTransform>();
        dragImage = go.GetComponentInChildren<Image>();
        dragQty = go.GetComponentInChildren<TMP_Text>();

        dragImage.sprite = icon;
        dragQty.text = quantity > 1 ? quantity.ToString() : "";
        isPartial = false;
        draggedItem = null;
        draggedCount = 0;
    }

    // ---- PARTIAL: start from an inventory slot with 1 item on the cursor (E3: do NOT alter the slot yet)
    public void BeginPartialFromInventory(int fromIndex, Sprite icon, ItemDefinition item, int availableInSlot)
    {
        if (dragIconPrefab == null || dragLayer == null) return;
        if (availableInSlot <= 0 || item == null) return;

        dragSource = Source.Inventory;
        draggedFromIndex = fromIndex;

        GameObject go = Instantiate(dragIconPrefab, dragLayer);
        dragRect = go.GetComponent<RectTransform>();
        dragImage = go.GetComponentInChildren<Image>();
        dragQty = go.GetComponentInChildren<TMP_Text>();

        dragImage.sprite = icon;

        isPartial = true;
        draggedItem = item;
        draggedCount = 1; // R-B: first right-click takes 1
        UpdateDragIconQty();
    }

    // ---- PARTIAL: add +1 more from SAME SOURCE SLOT while right-clicking repeatedly
    public void IncrementPartialFromSameSlot(int currentAvailableInSlot)
    {
        if (!isPartial) return;
        if (dragSource != Source.Inventory) return;
        if (draggedFromIndex < 0) return;

        // E3: source slot hasn't changed yet. Limit by what's currently there.
        if (draggedCount < currentAvailableInSlot)
        {
            draggedCount += 1;
            UpdateDragIconQty();
        }
        // else: already at cap; do nothing
    }

    // Reduce dragged count after a successful place
    public void ReduceDraggedBy(int amount)
    {
        if (!isPartial) return;
        draggedCount = Mathf.Max(0, draggedCount - amount);
        UpdateDragIconQty();
    }

    void UpdateDragIconQty()
    {
        if (dragQty == null) return;
        dragQty.text = draggedCount > 1 ? draggedCount.ToString() : (draggedCount == 1 ? "1" : "");
    }

    public void EndDrag()
    {
        if (dragLayer != null && dragLayer.childCount > 0)
            Destroy(dragLayer.GetChild(0).gameObject);

        dragRect = null;
        dragImage = null;
        dragQty = null;

        draggedFromIndex = -1;
        draggedItem = null;
        draggedCount = 0;
        isPartial = false;
    }

    // --- NEW: Update qty while classic-dragging ---
    public void UpdateClassicDragQty(int newQuantity)
    {
        if (isPartial) return;
        if (dragQty == null) return;

        dragQty.text = newQuantity > 1 ? newQuantity.ToString() : (newQuantity == 1 ? "1" : "");
    }

    // ===== NEW: Begin partial from Shipping =====
    public void BeginPartialFromShipping(int fromShipIndex, Sprite icon, ItemDefinition item, int availableInSlot)
    {
        if (dragIconPrefab == null || dragLayer == null) return;
        if (availableInSlot <= 0 || item == null) return;

        dragSource = Source.Shipping;
        draggedFromIndex = fromShipIndex;

        GameObject go = Instantiate(dragIconPrefab, dragLayer);
        dragRect = go.GetComponent<RectTransform>();
        dragImage = go.GetComponentInChildren<Image>();
        dragQty = go.GetComponentInChildren<TMP_Text>();

        dragImage.sprite = icon;

        isPartial = true;
        draggedItem = item;
        draggedCount = 1;
        UpdateDragIconQty();
    }

    // ===== NEW: Increment partial from SAME shipping slot =====
    public void IncrementPartialFromSameShipSlot(int currentAvailableInSlot)
    {
        if (!isPartial) return;
        if (dragSource != Source.Shipping) return;
        if (draggedFromIndex < 0) return;

        if (draggedCount < currentAvailableInSlot)
        {
            draggedCount += 1;
            UpdateDragIconQty();
        }
    }


    public bool IsDragging => dragRect != null;

    public bool IsShiftDown => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
}
