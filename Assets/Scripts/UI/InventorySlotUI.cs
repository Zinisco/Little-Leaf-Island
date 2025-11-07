using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
    IPointerClickHandler   // <-- NEW
{
    [SerializeField] Image slotImage;
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text qtyText;

    [Header("Hover Visuals")]
    [SerializeField] float hoverScale = 1.07f;
    [SerializeField] float scaleSpeed = 12f;
    [SerializeField] Color normalColor = new Color(1, 1, 1, 0.35f);
    [SerializeField] Color hoverColor = new Color(1, 1, 1, 0.6f);
    [SerializeField] float colorFadeSpeed = 12f;

    Vector3 originalScale;
    bool isHovered;
    int slotIndex;

    void Awake()
    {
        originalScale = transform.localScale;

        if (!slotImage)
            slotImage = GetComponent<Image>();

        if (iconImage) iconImage.raycastTarget = false;
        if (qtyText) qtyText.raycastTarget = false;

        if (slotImage)
        {
            slotImage.raycastTarget = true;
            slotImage.color = normalColor;
        }
    }

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

    void Update()
    {
        Vector3 targetScale = isHovered ? originalScale * hoverScale : originalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);

        if (slotImage)
        {
            Color target = isHovered ? hoverColor : normalColor;
            slotImage.color = Color.Lerp(slotImage.color, target, Time.unscaledDeltaTime * colorFadeSpeed);
        }

        if (isHovered && !RectTransformUtility.RectangleContainsScreenPoint(
            (RectTransform)transform, Input.mousePosition))
        {
            isHovered = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovered = true;
    public void OnPointerExit(PointerEventData eventData) => isHovered = false;

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
            qtyText.text = slot.quantity > 0 ? slot.quantity.ToString() : "";
        }
    }

    // ----- Click logic -----
    public void OnPointerClick(PointerEventData eventData)
    {
        var inv = InventorySystem.I;
        var dc = InventoryDragController.I;

        // --- SHIFT + LEFT: smart move to Shipping ---
        if (eventData.button == PointerEventData.InputButton.Left && dc.IsShiftDown && !dc.IsDragging)
        {
            inv.SmartMoveInventoryToShipping(slotIndex);
            Refresh();
            return;
        }

        // --- RIGHT: (existing) partial from Inventory ---
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            var slot = inv.Slots[slotIndex];
            if (slot.IsEmpty) return;

            if (!dc.IsDragging)
            {
                dc.BeginPartialFromInventory(slotIndex, iconImage.sprite, slot.item, slot.quantity);
                return;
            }

            if (dc.isPartial && dc.dragSource == InventoryDragController.Source.Inventory && dc.draggedFromIndex == slotIndex)
            {
                int currentAvail = inv.GetSlotQuantity(slotIndex);
                if (inv.GetSlotItem(slotIndex) == dc.draggedItem)
                    dc.IncrementPartialFromSameSlot(currentAvail);
            }
            return;
        }

        // --- LEFT: place partial dragged stack onto this Inventory slot (from either source) ---
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (dc.IsDragging && dc.isPartial)
            {
                int placed = 0;
                if (dc.dragSource == InventoryDragController.Source.Inventory)
                {
                    placed = InventorySystem.I.TransferInventoryPartial(dc.draggedFromIndex, slotIndex, dc.draggedCount);
                }
                else if (dc.dragSource == InventoryDragController.Source.Shipping)
                {
                    placed = InventorySystem.I.TransferShippingToInventoryPartial(dc.draggedFromIndex, slotIndex, dc.draggedCount);
                }

                if (placed > 0)
                {
                    dc.ReduceDraggedBy(placed);
                    Refresh();
                }

                if (dc.draggedCount <= 0) dc.EndDrag();
                return;
            }
        }
    }



    // ----- Drag & Drop (existing full-drag path) -----
    public void OnBeginDrag(PointerEventData eventData)
    {
        // If partial-drag is active, ignore the classic drag (we're using click-to-drop flow)
        if (InventoryDragController.I.isPartial) return;

        var slot = InventorySystem.I.Slots[slotIndex];
        if (slot.IsEmpty) return;

        InventoryDragController.I.BeginDrag(
            InventoryDragController.Source.Inventory,
            iconImage.sprite, slot.quantity, slotIndex);

        iconImage.enabled = false;
        qtyText.text = "";
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!InventoryDragController.I.isPartial) // only end the classic drag
        {
            InventoryDragController.I.EndDrag();
            Refresh();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        var dc = InventoryDragController.I;
        if (!dc.IsDragging) return;

        // Ignore partial — left click handles it
        if (dc.isPartial) return;

        if (dc.dragSource == InventoryDragController.Source.Inventory)
        {
            int moved = InventorySystem.I.MergeOrSwapInventory(dc.draggedFromIndex, slotIndex);

            var fromSlot = InventorySystem.I.Slots[dc.draggedFromIndex];

            if (fromSlot.IsEmpty)
            {
                dc.EndDrag();
            }
            else
            {
                dc.UpdateClassicDragQty(fromSlot.quantity);
            }

            Refresh();
        }
        else if (dc.dragSource == InventoryDragController.Source.Shipping)
        {
            InventorySystem.I.TransferShippingToInventory(dc.draggedFromIndex, slotIndex);
            dc.EndDrag();
            Refresh();
        }
    }

}
