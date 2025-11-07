using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShippingSlotUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
    IPointerClickHandler  
{
    [SerializeField] Image slotImage;
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text qtyText;

    [Header("Hover Visuals")]
    [SerializeField] float hoverScale = 1.07f;
    [SerializeField] float scaleSpeed = 12f;
    [SerializeField] Color normalColor = new Color(1, 1, 1, 0.25f);
    [SerializeField] Color hoverColor = new Color(1, 1, 1, 0.5f);
    [SerializeField] float colorFadeSpeed = 12f;

    Vector3 originalScale;
    bool isHovered;
    int shipIndex;

    void Awake()
    {
        originalScale = transform.localScale;
        if (!slotImage) slotImage = GetComponent<Image>();
        if (iconImage) iconImage.raycastTarget = false;
        if (qtyText) qtyText.raycastTarget = false;
        if (slotImage) { slotImage.raycastTarget = true; slotImage.color = normalColor; }
    }

    public void Init(int index)
    {
        shipIndex = index;
        Refresh();
        InventorySystem.OnShippingChanged += Refresh;
    }

    void OnDestroy()
    {
        InventorySystem.OnShippingChanged -= Refresh;
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

        if (isHovered && !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, Input.mousePosition))
            isHovered = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var dc = InventoryDragController.I;

        // --- SHIFT + LEFT: smart move to Inventory ---
        if (eventData.button == PointerEventData.InputButton.Left && dc.IsShiftDown && !dc.IsDragging)
        {
            InventorySystem.I.SmartMoveShippingToInventory(shipIndex);
            Refresh();
            return;
        }

        // --- RIGHT: begin/increment partial from Shipping ---
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            var slot = InventorySystem.I.ShippingSlots[shipIndex];
            if (slot.IsEmpty) return;

            if (!dc.IsDragging)
            {
                dc.BeginPartialFromShipping(shipIndex, iconImage.sprite, slot.item, slot.quantity);
                return;
            }

            if (dc.isPartial && dc.dragSource == InventoryDragController.Source.Shipping && dc.draggedFromIndex == shipIndex)
            {
                int currentAvail = slot.quantity;
                if (slot.item == dc.draggedItem)
                    dc.IncrementPartialFromSameShipSlot(currentAvail);
            }
            return;
        }

        // --- LEFT: place partial dragged stack onto this Shipping slot (from either source) ---
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (dc.IsDragging && dc.isPartial)
            {
                int placed = 0;
                if (dc.dragSource == InventoryDragController.Source.Inventory)
                {
                    placed = InventorySystem.I.TransferInventoryToShippingPartial(dc.draggedFromIndex, shipIndex, dc.draggedCount);
                }
                else if (dc.dragSource == InventoryDragController.Source.Shipping)
                {
                    placed = InventorySystem.I.TransferShippingPartial(dc.draggedFromIndex, shipIndex, dc.draggedCount);
                }

                if (placed > 0) dc.ReduceDraggedBy(placed);
                if (dc.draggedCount <= 0) dc.EndDrag();
                Refresh();
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovered = true;
    public void OnPointerExit(PointerEventData eventData) => isHovered = false;

    void Refresh()
    {
        var slot = InventorySystem.I.ShippingSlots[shipIndex];

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

    // ---- Drag-n-drop ----
    public void OnBeginDrag(PointerEventData eventData)
    {
        var slot = InventorySystem.I.ShippingSlots[shipIndex];
        if (slot.IsEmpty) return;

        InventoryDragController.I.BeginDrag(
            InventoryDragController.Source.Shipping,
            iconImage.sprite, slot.quantity, shipIndex);

        iconImage.enabled = false;
        qtyText.text = "";
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        InventoryDragController.I.EndDrag();
        Refresh();
    }

    public void OnDrop(PointerEventData eventData)
    {
        var dc = InventoryDragController.I;
        if (!dc.IsDragging) return;

        if (dc.dragSource == InventoryDragController.Source.Inventory)
        {
            int moved = InventorySystem.I.MergeInventoryIntoSpecificShipping(dc.draggedFromIndex, shipIndex);

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
            if (dc.draggedFromIndex == shipIndex) return;
            InventorySystem.I.MoveShippingSlot(dc.draggedFromIndex, shipIndex);
            dc.EndDrag();
            Refresh();
        }
    }

}
