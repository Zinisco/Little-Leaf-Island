using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
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

        // If not manually assigned, auto-grab the Image on this object
        if (!slotImage)
            slotImage = GetComponent<Image>();

        // Children shouldn’t block raycasts
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
        // Scale
        Vector3 targetScale = isHovered ? originalScale * hoverScale : originalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);

        // Color fade on root image
        if (slotImage)
        {
            Color target = isHovered ? hoverColor : normalColor;
            slotImage.color = Color.Lerp(slotImage.color, target, Time.unscaledDeltaTime * colorFadeSpeed);
        }

        // Scroll safety — remove hover if mouse moved off while scrolling
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
            qtyText.text = slot.quantity > 1 ? slot.quantity.ToString() : "";
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        var slot = InventorySystem.I.Slots[slotIndex];
        if (slot.IsEmpty) return;

        InventoryDragController.I.BeginDrag(iconImage.sprite, slot.quantity, slotIndex);

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
        int from = InventoryDragController.I.draggedFromIndex;
        int to = slotIndex;
        if (from < 0 || from == to) return;

        InventorySystem.I.MoveSlot(from, to);
    }
}
