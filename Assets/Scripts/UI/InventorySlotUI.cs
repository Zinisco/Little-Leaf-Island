using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour,
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
    [SerializeField] Color normalColor = new Color(1, 1, 1, 0.35f);
    [SerializeField] Color hoverColor = new Color(1, 1, 1, 0.6f);
    [SerializeField] float colorFadeSpeed = 12f;

    [SerializeField] private float clickHoldThreshold = 0.25f;
    private float clickStartTime;
    private bool pointerDown;



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

        // Quick use of placeable items
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            var slot = InventorySystem.I.Slots[slotIndex];
            if (!slot.IsEmpty && slot.item.isPlaceable && !InventoryDragController.I.IsDragging)
            {
                Debug.Log($"Clicked placeable {slot.item.displayName}");
                InventoryUIController.CloseInventory();
                InventoryUIController.Instance.StartCoroutine(
                    InventoryUIController.Instance.DelayedStartPlacement(slot.item)
                );

                return;
            }
        }


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
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // Don’t start drag if click was too short — placement will handle that
        if (Time.unscaledTime - clickStartTime < clickHoldThreshold)
            return;

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

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        pointerDown = true;
        clickStartTime = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!pointerDown || eventData.button != PointerEventData.InputButton.Left) return;
        pointerDown = false;

        Debug.Log($"PointerUp fired on slot {slotIndex}");

        float held = Time.unscaledTime - clickStartTime;
        var slot = InventorySystem.I.Slots[slotIndex];
        if (slot.IsEmpty) return;

        // Short click = placeable use
        if (held < clickHoldThreshold && slot.item.isPlaceable)
        {
            InventoryUIController.CloseInventory();
            StartCoroutine(StartPlacementNextFrame(slot.item));


            return;
        }

        // otherwise it’s a normal release (drag handled already)
    }

    private System.Collections.IEnumerator StartPlacementNextFrame(ItemDefinition item)
    {
        yield return null; // wait one frame so the UI fully hides first
        var placer = FindFirstObjectByType<PlaceableItemPlacer>();
        if (placer != null)
            placer.StartPlacing(item);
    }


}
